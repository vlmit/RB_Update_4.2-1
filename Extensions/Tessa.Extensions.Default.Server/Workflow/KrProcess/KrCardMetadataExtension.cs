#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Cards.Metadata;
using Tessa.Cards.TypeSettings;
using Tessa.Cards.Validation;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Scheme;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess
{
    /// <summary>
    /// Серверное расширение метаданных для карточек типового решения.
    /// Расширяет карточки KrCard, KrStageTemplate, KrSecondaryProcess.
    /// Добавляет этапы маршрутов.
    /// </summary>
    public sealed class KrCardMetadataExtension :
        KrCardMetadataExtensionBase
    {
        #region Nested Types

        /// <summary>
        /// Единичная связь между существующим и созданным элементом метаданных.
        /// </summary>
        /// <param name="OriginalID">Идентификатор существующего объекта.</param>
        /// <param name="NewID">Идентификатор нового объекта.</param>
        private readonly record struct CardMetadataLink(Guid OriginalID, Guid NewID);

        /// <summary>
        /// Связь между существующей и новой секцией/колонкой метаданных с учетом маппинга полей.
        /// </summary>
        /// <param name="originalID"><inheritdoc cref="OriginalID" path="/summary"/></param>
        /// <param name="newID"><inheritdoc cref="NewID" path="/summary"/></param>
        private sealed class CardMetadataContainerLink(
            Guid originalID,
            Guid newID) :
            IReadOnlyCollection<CardMetadataContainerLink>
        {
            #region Fields

            private readonly HashSet<Guid, CardMetadataContainerLink> mapping = new(static x => x.link.OriginalID);

            private readonly CardMetadataLink link = new(originalID, newID);

            #endregion

            #region Properties

            /// <summary>
            /// Идентификатор существующего объекта.
            /// </summary>
            public Guid OriginalID => this.link.OriginalID;

            /// <summary>
            /// Идентификатор нового объекта.
            /// </summary>
            public Guid NewID => this.link.NewID;

            #endregion

            #region Public Methods

            /// <summary>
            /// Возвращает <see cref="CardMetadataContainerLink"/> по идентификатору существующего объекта.
            /// </summary>
            /// <param name="originalID">Идентификатор существующего объекта.</param>
            /// <returns><inheritdoc cref="CardMetadataContainerLink" path="/summary"/></returns>
            public CardMetadataContainerLink GetLink(Guid originalID) =>
                this.mapping[originalID];

            /// <summary>
            /// Возвращает <see cref="CardMetadataContainerLink"/> по идентификатору существующего объекта.
            /// </summary>
            /// <param name="originalID">Идентификатор существующего объекта.</param>
            /// <param name="link"><see cref="CardMetadataContainerLink"/> или <see langword="null"/>, если такого объекта нет.</param>
            /// <returns>Значение <see langword="true"/>, если значение найдено, иначе - <see langword="false"/>.</returns>
            public bool TryGetLink(
                Guid originalID,
                [MaybeNullWhen(false)] out CardMetadataContainerLink link) =>
                this.mapping.TryGetItem(originalID, out link);

            /// <summary>
            /// Возвращает значение, показывающее наличие связи с существующей секцией/колонкой.
            /// </summary>
            /// <param name="originalID">Идентификатор существующего объекта.</param>
            /// <returns>Значение <see langword="true"/>, если связь найдена, иначе - <see langword="false"/>.</returns>
            public bool ContainsLink(Guid originalID) =>
                this.mapping.ContainsKey(originalID);

            /// <summary>
            /// Добавляет новую связь между объектами с идентификаторами <paramref name="originalID"/> и <paramref name="newID"/>.
            /// </summary>
            /// <param name="originalID">Идентификатор существующего объекта.</param>
            /// <param name="newID">Идентификатор нового объекта.</param>
            /// <returns>Созданная связь.</returns>
            public CardMetadataContainerLink AddLink(
                Guid originalID,
                Guid newID)
            {
                var link = new CardMetadataContainerLink(
                    originalID,
                    newID);

                this.mapping.Replace(link);
                return link;
            }

            #endregion

            #region IReadOnlyCollection<T> Members

            /// <inheritdoc />
            public int Count => this.mapping.Count;

            /// <inheritdoc />
            public IEnumerator<CardMetadataContainerLink> GetEnumerator() =>
                this.mapping.GetEnumerator();

            /// <inheritdoc />
            IEnumerator IEnumerable.GetEnumerator() =>
                this.GetEnumerator();

            #endregion
        }

        /// <summary>
        /// Помощник настройки типа карточки.
        /// Выделяет тэги из имён блоков/контролов (в формате [Tag],..) и помещает их
        /// в настройки соответствующего элемента TagsListSetting.
        /// Корректирует настройки контролов, валидаторов и расширений типов, чтобы они ссылались на новые секции и колонки метаданных.
        /// </summary>
        private sealed class ReplaceFieldControlVisitor :
            TagParserCardTypeVisitor
        {
            #region Fields

            private readonly CardMetadataContainerLink mapping;
            private readonly HashSet<Guid> mergedIntoKrStages;
            private readonly ICardMetadata metadata;
            private readonly KrStageSerializerSettings settings;

            #endregion

            #region Constructors

            /// <summary>
            /// Инициализирует новый экземпляр класса.
            /// </summary>
            /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
            /// <param name="mapping"><inheritdoc cref="CardMetadataContainerLink" path="/summary"/></param>
            /// <param name="mergedIntoKrStages">Перечень идентификаторов оригинальных секций, помещённых в секцию KrStagesVirtual (все строковые секции).</param>
            /// <param name="metadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
            /// <param name="settings"><inheritdoc cref="KrStageSerializerSettings" path="/summary"/></param>
            public ReplaceFieldControlVisitor(
                IValidationResultBuilder validationResult,
                CardMetadataContainerLink mapping,
                HashSet<Guid> mergedIntoKrStages,
                ICardMetadata metadata,
                KrStageSerializerSettings settings)
                : base(validationResult)
            {
                this.mapping = mapping;
                this.mergedIntoKrStages = mergedIntoKrStages;
                this.metadata = metadata;
                this.settings = settings;
            }

            #endregion

            #region Base Overrides

            /// <inheritdoc/>
            public override async ValueTask VisitControlAsync(
                CardTypeControl control,
                CardTypeBlock? block,
                CardTypeForm? form,
                CardType type,
                CancellationToken cancellationToken = default)
            {
                await base.VisitControlAsync(control, block, form, type, cancellationToken);
                // заменяем данные о секциях и колонках для автокомплита.
                this.ModifyAutocompleteMappingIfNeeded(control);

                CardMetadataContainerLink link;
                switch (control)
                {
                    case CardTypeEntryControl entryControl:
                        link = this.mapping.GetLink(entryControl.SectionID);
                        this.ModifyEntryControl(link, entryControl);
                        ModifyEntryControlSettings(link, entryControl);
                        break;
                    case CardTypeTableControl tableControl:
                        link = this.mapping.GetLink(tableControl.SectionID);
                        this.ModifyTableControl(link, tableControl);
                        await this.ModifyTableSettingsAsync(link, tableControl, cancellationToken);
                        break;
                    case CardTypeTabControl:
                        // Нечего исправлять.
                        break;
                    case CardTypeCustomControl customControl:
                        if (control.Type == CardControlTypes.Numerator)
                        {
                            this.ReplaceNumeratorSettings(customControl);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(control),
                            $"{nameof(control)} type {control.GetType().Name} is unknown.");
                }
            }

            /// <inheritdoc/>
            public override async ValueTask VisitExtensionAsync(
                CardTypeExtension extension,
                CardType type,
                CancellationToken cancellationToken = default)
            {
                await base.VisitExtensionAsync(extension, type, cancellationToken);
                var sectionLink = this.ModifyIfPresent(
                    extension.ExtensionSettings, this.mapping, CardTypeExtensionSettings.SectionIDSetting);
                this.ModifyIfPresent(extension.ExtensionSettings, sectionLink, CardTypeExtensionSettings.ColumnIDSetting);
                if (extension.Type != CardTypeExtensionTypes.SortRows)
                {
                    throw new ArgumentException(
                        $"Unknown extension type {extension.Type}.",
                        nameof(extension));
                }
            }

            /// <inheritdoc/>
            public override async ValueTask VisitValidatorAsync(
                CardTypeValidator validator,
                CardType type,
                CancellationToken cancellationToken = default)
            {
                await base.VisitValidatorAsync(validator, type, cancellationToken);
                var sectionLink = this.ModifyIfPresent(
                    validator.ValidatorSettings, this.mapping, CardValidatorSettings.SectionIDSetting);
                this.ModifyIfPresent(validator.ValidatorSettings, sectionLink, CardValidatorSettings.ColumnIDSetting);
                this.ModifyIfPresent(validator.ValidatorSettings, sectionLink, CardValidatorSettings.OrderColumnIDSetting);
                var parentLink = this.ModifyIfPresent(
                    validator.ValidatorSettings, this.mapping, CardValidatorSettings.ParentSectionIDSetting);
                this.ModifyIfPresent(validator.ValidatorSettings, parentLink, CardValidatorSettings.ParentColumnIDSetting);

                // Проверка для обнаружения неизвестных типов валидаторов
                // Чтобы ошибка вскрылась раньше.
                if (validator.Type != CardValidatorTypes.NotNullField
                    && validator.Type != CardValidatorTypes.NotNullTable
                    && validator.Type != CardValidatorTypes.Unique)
                {
                    throw new ArgumentException(
                        $"Unknown validator type {validator.Type}.",
                        nameof(validator));
                }
            }

            #endregion

            #region Private Methods

            #region Modify Entry Control

            /// <summary>
            /// Модифицирует идентификаторы секций и колонок контрола.
            /// </summary>
            /// <param name="link"><inheritdoc cref="CardMetadataContainerLink" path="/summary"/></param>
            /// <param name="entryControl"><inheritdoc cref="CardTypeEntryControl" path="/summary"/></param>
            private void ModifyEntryControl(
                CardMetadataContainerLink link,
                CardTypeEntryControl entryControl)
            {
                entryControl.SectionID = this.mergedIntoKrStages.Contains(link.NewID)
                    ? DefaultSchemeHelper.KrStagesVirtual
                    : link.NewID;
                if (entryControl.ComplexColumnID.HasValue)
                {
                    entryControl.ComplexColumnID = link.GetLink(entryControl.ComplexColumnID.Value).NewID;
                }

                entryControl.PhysicalColumnIDList = entryControl
                    .PhysicalColumnIDList
                    .Select(p => link.GetLink(p).NewID)
                    .ToSealableList();
            }

            /// <summary>
            /// Модифицирует идентификатор колонки в настройках контрола.
            /// </summary>
            /// <param name="link"><inheritdoc cref="CardMetadataContainerLink" path="/summary"/></param>
            /// <param name="entryControl"><inheritdoc cref="CardTypeEntryControl" path="/summary"/></param>
            private static void ModifyEntryControlSettings(
                CardMetadataContainerLink link,
                CardTypeEntryControl entryControl)
            {
                var manualInputColumnID =
                    entryControl.ControlSettings.TryGet<Guid?>(CardControlSettings.ManualInputColumnIDSetting);
                if (manualInputColumnID.HasValue)
                {
                    var manualInputLink = link.GetLink(manualInputColumnID.Value);
                    entryControl.ControlSettings[CardControlSettings.ManualInputColumnIDSetting] =
                        manualInputLink.NewID;
                }
            }

            #endregion

            #region Modify Table Control

            /// <summary>
            /// Модифицирует идентификаторы секций и колонок контрола.
            /// </summary>
            /// <param name="link"><inheritdoc cref="CardMetadataContainerLink" path="/summary"/></param>
            /// <param name="tableControl"><inheritdoc cref="CardTypeTableControl" path="/summary"/></param>
            private void ModifyTableControl(
                CardMetadataContainerLink link,
                CardTypeTableControl tableControl)
            {
                tableControl.SectionID = link.NewID;
                foreach (var column in tableControl.Columns)
                {
                    if (column.ComplexColumnID.HasValue)
                    {
                        column.ComplexColumnID = link.GetLink(column.ComplexColumnID.Value).NewID;
                    }

                    if (column.OwnedSectionID.HasValue)
                    {
                        var ownedSectionLink = this.mapping.GetLink(column.OwnedSectionID.Value);
                        column.OwnedSectionID = ownedSectionLink.NewID;
                        if (column.OwnedComplexColumnID.HasValue)
                        {
                            column.OwnedComplexColumnID =
                                ownedSectionLink.GetLink(column.OwnedComplexColumnID.Value).NewID;
                        }

                        if (column.OwnedOrderColumnID.HasValue)
                        {
                            column.OwnedOrderColumnID =
                                ownedSectionLink.GetLink(column.OwnedOrderColumnID.Value).NewID;
                        }

                        column.OwnedPhysicalColumnIDList = column
                            .OwnedPhysicalColumnIDList
                            .Select(p => ownedSectionLink.GetLink(p).NewID)
                            .ToSealableList();
                    }

                    column.PhysicalColumnIDList = column
                        .PhysicalColumnIDList
                        .Select(p => link.GetLink(p).NewID)
                        .ToSealableList();
                }
            }

            /// <summary>
            /// Модифицирует идентификаторы колонок в настройках контрола.
            /// </summary>
            /// <param name="link"><inheritdoc cref="CardMetadataContainerLink" path="/summary"/></param>
            /// <param name="tableControl"><inheritdoc cref="CardTypeTableControl" path="/summary"/></param>
            /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
            private async ValueTask ModifyTableSettingsAsync(
                CardMetadataContainerLink link,
                CardTypeTableControl tableControl,
                CancellationToken cancellationToken = default)
            {
                var orderID =
                    tableControl.ControlSettings.TryGet<Guid?>(CardControlSettings.OrderColumnIDSetting);
                if (orderID.HasValue)
                {
                    var orderLink = link.GetLink(orderID.Value);
                    tableControl.ControlSettings[CardControlSettings.OrderColumnIDSetting] =
                        orderLink.NewID;
                    // Необходимо запомнить ордер, чтобы затем восстанавливать его.
                    var section = (await this.metadata.GetSectionsAsync(cancellationToken))[link.NewID];
                    var sectionName = section.Name;
                    var orderFieldName = section.Columns[orderLink.NewID].Name;
                    this.settings.OrderColumns.Add(new OrderColumn(sectionName, orderFieldName));
                }

                var manualInputColumnID =
                    tableControl.ControlSettings.TryGet<Guid?>(CardControlSettings.ManualInputColumnIDSetting);
                if (manualInputColumnID.HasValue)
                {
                    var manualInputLink = link.GetLink(manualInputColumnID.Value);
                    tableControl.ControlSettings[CardControlSettings.ManualInputColumnIDSetting] =
                        manualInputLink.NewID;
                }
            }

            #endregion

            /// <summary>
            /// Модифицирует идентификаторы секции и колонок в настройках контрола номера документа.
            /// </summary>
            /// <param name="numeratorControl"><inheritdoc cref="CardTypeCustomControl" path="/summary"/></param>
            private void ReplaceNumeratorSettings(
                CardTypeCustomControl numeratorControl)
            {
                var ctrlSettings = numeratorControl.ControlSettings;
                if (!ctrlSettings.TryGetValue(CardControlSettings.SectionIDSetting, out var secIDObj)
                    || secIDObj is not Guid sectionID)
                {
                    return;
                }

                var link = this.mapping.GetLink(sectionID);
                ctrlSettings[CardControlSettings.SectionIDSetting] = this.mergedIntoKrStages.Contains(link.NewID)
                    ? DefaultSchemeHelper.KrStagesVirtual
                    : link.NewID;

                if (ctrlSettings.TryGetValue(CardControlSettings.SequenceColumnIDSetting, out var seqIDObj)
                    && seqIDObj is Guid sequenceColumnID)
                {
                    var seqLink = link.GetLink(sequenceColumnID);
                    ctrlSettings[CardControlSettings.SequenceColumnIDSetting] = seqLink.NewID;
                }

                if (ctrlSettings.TryGetValue(CardControlSettings.NumberColumnIDSetting, out var numberColIDObj)
                    && numberColIDObj is Guid numberColumnID)
                {
                    var numberLink = link.GetLink(numberColumnID);
                    ctrlSettings[CardControlSettings.NumberColumnIDSetting] = numberLink.NewID;
                }

                if (ctrlSettings.TryGetValue(CardControlSettings.FullNumberColumnIDSetting, out var fullNumberColIDObj)
                    && fullNumberColIDObj is Guid fullNumberColumnID)
                {
                    var fullNumberLink = link.GetLink(fullNumberColumnID);
                    ctrlSettings[CardControlSettings.FullNumberColumnIDSetting] = fullNumberLink.NewID;
                }
            }

            /// <summary>
            /// Настраивает маппинг автокомплита на новые секции/колонки метаданных.
            /// </summary>
            /// <param name="control"><inheritdoc cref="CardTypeControl" path="/summary"/></param>
            /// <exception cref="ArgumentOutOfRangeException">Неизвестный тип колонки.</exception>
            private void ModifyAutocompleteMappingIfNeeded(
                CardTypeControl control)
            {
                // получить настройки представления в автокомплите.
                var mapSettingsList = control.ControlSettings.TryGet<IList>(CardControlSettings.ViewMapSetting);
                if (mapSettingsList is null)
                {
                    return;
                }

                foreach (var mappingSettings in mapSettingsList.Cast<Dictionary<string, object?>>())
                {
                    var mappingColumnType = (ViewMapColumnType?) mappingSettings.Get<int?>(CardControlSettings.MappingColumnTypeSetting);
                    switch (mappingColumnType)
                    {
                        case ViewMapColumnType.CardColumn:
                            var mappingCardSection = mappingSettings.Get<Guid?>(CardControlSettings.MappingCardSectionSetting);
                            var mappingCardColumn = mappingSettings.Get<Guid?>(CardControlSettings.MappingCardColumnSetting);
                            if (mappingCardSection.HasValue
                                && mappingCardColumn.HasValue)
                            {
                                var link = this.mapping.GetLink(mappingCardSection.Value);
                                var columnLink = link.GetLink(mappingCardColumn.Value);
                                mappingSettings[CardControlSettings.MappingCardSectionSetting] =
                                    this.mergedIntoKrStages.Contains(link.NewID)
                                        ? DefaultSchemeHelper.KrStagesVirtual
                                        : link.NewID;
                                mappingSettings[CardControlSettings.MappingCardColumnSetting] = columnLink.NewID;
                            }

                            break;
                        case ViewMapColumnType.CardID:
                        case ViewMapColumnType.CardType:
                        case ViewMapColumnType.CardTypeAlias:
                        case ViewMapColumnType.CurrentUser:
                        case ViewMapColumnType.ConstantValue:
                            // В данных случаях ничего не меняем, т.к. не затронуты секции.
                            break;
                        case null:
                            // Маппинг отключён.
                            break;
                        default:
                            throw ArgumentOutOfRange(mappingColumnType);
                    }
                }
            }

            private CardMetadataContainerLink? ModifyIfPresent(
                ISerializableObject dict,
                CardMetadataContainerLink? link,
                string key)
            {
                if (link is not null &&
                    dict.TryGetValue(key, out var objValue) &&
                    objValue is Guid value)
                {
                    var embeddedLink = link.GetLink(value);
                    var newID = this.mergedIntoKrStages.Contains(embeddedLink.NewID)
                        ? DefaultSchemeHelper.KrStagesVirtual
                        : embeddedLink.NewID;
                    dict[key] = newID;
                    return embeddedLink;
                }

                return null;
            }

            #endregion
        }

        /// <summary>
        /// Контекст настроек контролов, копируемых из KrAuthorSettings, KrPerformersSettings, KrTaskKindSettings, KrHistoryManagementStageTypeSettings
        /// в расширяемые типы в соответствии с данными из карточек настроек этапов <see cref="StageTypeDescriptor.SettingsCardTypeID"/>.
        /// </summary>
        private sealed record VisibilityContext(
            List<Guid> UseDefaultTimeLimit,
            List<Guid> UseDefaultPlanned,
            List<Guid> CanBeHidden,
            List<Guid> SinglePerformers,
            List<Guid> MultiplePerformers,
            Dictionary<string, object> PerformerCaptions,
            List<Guid> CheckPerformers,
            List<Guid> CanOverrideAuthor,
            List<Guid> CanOverrideTaskHistoryGroup,
            List<Guid> UseTaskKind,
            List<Guid> CanBeSkipped)
        {
            public VisibilityContext(
                int cnt)
                : this(
                    new List<Guid>(cnt),
                    new List<Guid>(cnt),
                    new List<Guid>(cnt),
                    new List<Guid>(cnt),
                    new List<Guid>(cnt),
                    [],
                    new List<Guid>(cnt),
                    new List<Guid>(cnt),
                    new List<Guid>(cnt),
                    new List<Guid>(cnt),
                    new List<Guid>(cnt))
            {
            }

            public void Init(ICollection<StageTypeDescriptor> handlerDescriptors)
            {
                foreach (var descriptor in handlerDescriptors)
                {
                    if (descriptor.UseTimeLimit)
                    {
                        this.UseDefaultTimeLimit.Add(descriptor.ID);
                    }

                    if (descriptor.UsePlanned)
                    {
                        this.UseDefaultPlanned.Add(descriptor.ID);
                    }

                    if (descriptor.CanBeHidden)
                    {
                        this.CanBeHidden.Add(descriptor.ID);
                    }

                    switch (descriptor.PerformerUsageMode)
                    {
                        case PerformerUsageMode.Single:
                            this.SinglePerformers.Add(descriptor.ID);
                            break;
                        case PerformerUsageMode.Multiple:
                            this.MultiplePerformers.Add(descriptor.ID);
                            break;
                    }

                    if (descriptor.PerformerUsageMode != PerformerUsageMode.None
                        && !string.IsNullOrWhiteSpace(descriptor.PerformerCaption))
                    {
                        this.PerformerCaptions.Add(descriptor.ID.ToString("D"), descriptor.PerformerCaption);
                    }

                    if (descriptor.PerformerUsageMode != PerformerUsageMode.None
                        && descriptor.PerformerIsRequired)
                    {
                        this.CheckPerformers.Add(descriptor.ID);
                    }

                    if (descriptor.CanOverrideAuthor)
                    {
                        this.CanOverrideAuthor.Add(descriptor.ID);
                    }

                    if (descriptor.CanOverrideTaskHistoryGroup)
                    {
                        this.CanOverrideTaskHistoryGroup.Add(descriptor.ID);
                    }

                    if (descriptor.UseTaskKind)
                    {
                        this.UseTaskKind.Add(descriptor.ID);
                    }

                    if (descriptor.CanBeSkipped)
                    {
                        this.CanBeSkipped.Add(descriptor.ID);
                    }
                }
            }
        }

        /// <summary>
        /// Данные о добавляемых в карточку контролах карточек настроек.
        /// </summary>
        /// <param name="CardType">Карточка - источник контролов.</param>
        /// <param name="PrepareAction">Действие по подготовке экспортируемого типа карточки к экспорту.
        /// Должно вызываться один раз перед модификацией.</param>
        /// <param name="ModifyAction">Действие по модификации целевой карточки.
        /// (Тип целевой (модифицируемой) карточки, тип карточки источника <see cref="CardType"/>).</param>
        private readonly record struct InjectCardTypeInfo(
            CardType? CardType,
            Action<CardType, ICardMetadataExtensionContext, VisibilityContext>? PrepareAction,
            Action<CardType, CardType>? ModifyAction);

        #endregion

        #region Fields

        // Расширение регистрируется как PerResolve,
        // поэтому данные от одного выполнения хранятся в полях.

        private readonly IDbScope dbScope;

        private readonly IKrProcessContainer processContainer;

        private readonly IKrStageSerializerSettingsCache krStageSerializerSettingsCache;

        /// <summary>
        /// Связь ID существующих секций и добавленных в KrCard.
        /// Единый для всех расширяемых типов.
        /// </summary>
        private readonly CardMetadataContainerLink sectionMapping = new(Guid.Empty, Guid.Empty);

        /// <summary>
        /// ID секций, попавших в sectionMapping как NewID, но влитые в KrStagesVirtual.
        /// </summary>
        private readonly HashSet<Guid> mergedIntoKrStages = [];

        /// <summary>
        /// Новые имена для созданных секций.
        /// </summary>
        private readonly Dictionary<Guid, string> newSectionNames = [];

        /// <summary>
        /// Класс для обхода UI типов и подмены секций.
        /// </summary>
        private ReplaceFieldControlVisitor visitor = null!;

        /// <summary>
        /// Словарь соответствий описаний обработчиков этапов к их идентификаторам.
        /// </summary>
        private Dictionary<Guid, List<StageTypeDescriptor>> stageSettingsWithDescriptors = null!;

        /// <summary>
        /// Секция метаданных, описывающая табличные данные секции KrStagesVirtual.
        /// </summary>
        private CardMetadataSection krStageSection = null!;

        /// <summary>
        /// Идентификатор новой (не занятой) комплексной колонки.
        /// </summary>
        private short krStageComplexColumnIndex;

        /// <summary>
        /// Список расширяемых типов.
        /// </summary>
        private IList<CardType> krTypes = null!;

        /// <summary>
        /// Список <see cref="CardTypeSchemeItem"/> для KrStagesVirtual расширяемых типов.
        /// </summary>
        private List<CardTypeSchemeItem> krStagesVirtualSchemeItem = null!;

        private readonly KrStageSerializerSettings stageSerializerSettings = new();

        #endregion

        #region Constructors

        /// <inheritdoc />
        public KrCardMetadataExtension(
            ICardMetadata clientCardMetadata)
            : base(clientCardMetadata) =>
            throw new InvalidOperationException("This constructor only for client-side.");

        /// <summary>
        /// Создаёт экземпляр класса на сервере.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="processContainer"><inheritdoc cref="IKrProcessContainer" path="/summary"/></param>
        /// <param name="krStageSerializerSettingsCache"><inheritdoc cref="IKrStageSerializerSettingsCache" path="/summary"/></param>
        public KrCardMetadataExtension(
            IDbScope dbScope,
            IKrProcessContainer processContainer,
            IKrStageSerializerSettingsCache krStageSerializerSettingsCache)
            : base()
        {
            this.dbScope = NotNullOrThrow(dbScope);
            this.processContainer = NotNullOrThrow(processContainer);
            this.krStageSerializerSettingsCache = NotNullOrThrow(krStageSerializerSettingsCache);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override async Task ExtendKrTypesAsync(
            IList<CardType> types,
            ICardMetadataExtensionContext context)
        {
            this.krTypes = types;
            var cardMetadata = context.CardMetadata!;

            this.visitor = new ReplaceFieldControlVisitor(
                context.ValidationResult,
                this.sectionMapping,
                this.mergedIntoKrStages,
                cardMetadata,
                this.stageSerializerSettings);

            this.krStagesVirtualSchemeItem = new List<CardTypeSchemeItem>(this.krTypes.Count);
            foreach (var krType in this.krTypes)
            {
                this.krStagesVirtualSchemeItem.Add(
                    krType.SchemeItems.First(static p => p.SectionID == DefaultSchemeHelper.KrStagesVirtual));
            }

            var metadataSections = await cardMetadata.GetSectionsAsync(context.CancellationToken);
            this.krStageSection = metadataSections[DefaultSchemeHelper.KrStagesVirtual];
            this.krStageComplexColumnIndex = (short) Math.Min(
                this.krStageSection.Columns.Max(static p => p.ComplexColumnIndex) + 1,
                short.MaxValue);

            // получаем дескрипторы этапов.
            var descriptors = this.processContainer.GetHandlerDescriptors();

            // запоминаем данные о дескрипторах.
            this.stageSettingsWithDescriptors = [];
            foreach (var descriptor in descriptors)
            {
                if (!descriptor.SettingsCardTypeID.HasValue)
                {
                    continue;
                }

                if (!this.stageSettingsWithDescriptors.TryGetValue(descriptor.SettingsCardTypeID.Value, out var descriptorList))
                {
                    descriptorList = [];
                    this.stageSettingsWithDescriptors[descriptor.SettingsCardTypeID.Value] = descriptorList;
                }

                descriptorList.Add(descriptor);
            }

            // и получаем контекст видимости контролов на основании информации об этапах.
            var visibilityContext = GetVisibilityContext(descriptors);

            // получаем карточки с шаблонами этапов, которые будут далее внедрятся в расширяемые типы krTypes.
            List<CardType> cardTypes = [];
            foreach (var cardType in context.CardTypes.Where(p => this.stageSettingsWithDescriptors.ContainsKey(p.ID)))
            {
                var clonedCardType = await cardType.DeepCloneAsync(context.CancellationToken);
                cardTypes.Add(clonedCardType);
            }

            // резервируем новые идентификаторы для элементов схемы.
            this.BuildMapping(cardTypes);

            // подготовка дополнительных типов к копированию в заданные типы с настройкой маппинга.
            var injectableInfo = await this.GetInjectableCardsAsync(context);

            // работа с метаданными.
            var sections = new CardMetadataSection[this.sectionMapping.Count];

            // Предварительное создание имен для корректного восстановления связей.
            var sectionsIndex = 0;
            foreach (var link in this.sectionMapping)
            {
                // Получаем перемещаемые секции и создаём новые имена для них.
                var section = metadataSections[link.OriginalID];
                sections[sectionsIndex++] = section;
                this.newSectionNames.Add(
                    link.NewID,
                    section.SectionType == CardSectionType.Entry
                        ? KrConstants.KrStages.Virtual
                        : NewSectionName(section));
            }

            // выполняем перенос данных в KrConstants.KrStages.Virtual или создаём новые секции метаданных.
            sectionsIndex = 0;
            foreach (var link in this.sectionMapping)
            {
                var section = sections[sectionsIndex++];
                switch (section.SectionType)
                {
                    case CardSectionType.Entry:
                        // перенос данных в виртуальную секцию KrStagesVirtual.
                        this.ExtendKrStagesVirtual(link, section);
                        break;
                    case CardSectionType.Table:
                        // создание и добавление в krTypes новых коллекционных виртуальных секций.
                        await this.CreateCollectionAsync(
                            cardMetadata,
                            link,
                            section,
                            context.CancellationToken);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unknown section type. Section type: {section.SectionType}.");
                }
            }

            this.stageSerializerSettings.SettingsSectionNames.Add(this.krStageSection.Name);

            // добавление всех типов этапов к krTypes
            foreach (var settingsCardType in cardTypes)
            {
                // Переносит данные из типа этапа в krTypes с поддержкой помещения нужных элементов в глобальный кэш элементов.
                await this.InjectStateSettingsAsync(
                    settingsCardType,
                    context);
            }

            // копирование контролов из типов карточек injectableInfo в krTypes и их окончательная настройка...
            await this.InjectOptionalControlsAndSetVisibilityAsync(
                injectableInfo,
                visibilityContext,
                context,
                context.CancellationToken);

            await this.krStageSerializerSettingsCache.SetAsync(
                this.stageSerializerSettings,
                context.CancellationToken);
        }

        /// <inheritdoc />
        protected override async Task<List<Guid>> GetCardTypeIDsAsync(
            CancellationToken cancellationToken = default)
        {
            await using (this.dbScope.Create())
            {
                var db = this.dbScope.Db;
                return await db
                    .SetCommand(
                        this.dbScope.BuilderFactory
                            .Select()
                            .C(KrConstants.KrSettingsCardTypes.CardTypeID)
                            .From(KrConstants.KrSettingsCardTypes.Name).NoLock()
                            .Where().C(KrConstants.KrSettingsCardTypes.CardTypeID).NotEquals().P("KrCardTypeID")
                            .Build(),
                        db.Parameter("KrCardTypeID", DefaultCardTypes.KrCardTypeID))
                    .LogCommand()
                    .ExecuteListAsync<Guid>(cancellationToken);
            }
        }

        /// <inheritdoc />
        protected override ValueTask<CardMetadataSectionCollection> GetAllSectionsAsync(
            ICardMetadataExtensionContext context) =>
            context.CardMetadata!.GetSectionsAsync(context.CancellationToken);

        #endregion

        #region Private Methods

        /// <summary>
        /// Составляет маппинг старых элементов схемы на новые.
        /// Для всех секций и колонок типов резервируются новые идентификаторы,
        /// для которых далее создаются новые секции метаданных.
        /// </summary>
        /// <param name="cardTypes">Типы карточек.</param>
        private void BuildMapping(IEnumerable<CardType> cardTypes)
        {
            foreach (var cardType in cardTypes)
            {
                foreach (var schemeItem in cardType.SchemeItems)
                {
                    if (!this.sectionMapping.TryGetLink(schemeItem.SectionID, out var link))
                    {
                        link = this.sectionMapping.AddLink(
                            schemeItem.SectionID,
                            Guid.NewGuid());
                    }

                    foreach (var columnID in schemeItem.ColumnIDList)
                    {
                        if (!link.ContainsLink(columnID))
                        {
                            link.AddLink(columnID, Guid.NewGuid());
                        }
                    }
                }
            }
        }

        #region Metadata copying helpers

        /// <summary>
        /// Перенос указанной строковой секции в KrStagesVirtual.
        /// </summary>
        /// <param name="sectionLink"><inheritdoc cref="CardMetadataContainerLink" path="/summary"/></param>
        /// <param name="originalSection">Оригинальная секция.</param>
        private void ExtendKrStagesVirtual(
            CardMetadataContainerLink sectionLink,
            CardMetadataSection originalSection)
        {
            this.mergedIntoKrStages.Add(sectionLink.NewID);
            this.MoveColumns(
                sectionLink,
                originalSection,
                newSection: this.krStageSection,
                newSchemeItems: this.krStagesVirtualSchemeItem,
                addToSerializer: true,
                complexColumnIndex: ref this.krStageComplexColumnIndex,
                hasReferenceToOwner: out _);
        }

        /// <summary>
        /// Создание и добавление к уже существующим новой коллекционной виртуальной секции табличных метаданных,
        /// добавление её ко всем расширяемым типам карточек.
        /// </summary>
        /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
        /// <param name="sectionLink">Информация о новых идентификаторах секции и её колонках.</param>
        /// <param name="originalSection">Оригинальная секция.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        private async Task CreateCollectionAsync(
            ICardMetadata cardMetadata,
            CardMetadataContainerLink sectionLink,
            CardMetadataSection originalSection,
            CancellationToken cancellationToken = default)
        {
            // сознание новых ссылок на элементы схемы.
            var newSchemeItems = new List<CardTypeSchemeItem>(this.krTypes.Count);
            for (var i = 0; i < this.krTypes.Count; i++)
            {
                newSchemeItems.Add(new CardTypeSchemeItem { SectionID = sectionLink.NewID });
            }

            var newSection = new CardMetadataSection
            {
                ID = sectionLink.NewID,
                Name = this.newSectionNames[sectionLink.NewID],
                SectionType = CardSectionType.Table,
                TableType = SchemeTableContentType.Collections,
                SectionTableType = CardTableType.Collection,
                Order = originalSection.Order,
                Flags = originalSection.Flags | CardMetadataSectionFlags.Virtual,
                CardTypeIDList = this.krTypes.Select(p => p.ID).ToSealableList()
            };

            short complexColumnIndex = 0;
            this.MoveColumns(
                sectionLink,
                originalSection,
                newSection,
                newSchemeItems,
                /*addToSerializer: */false,
                ref complexColumnIndex,
                out var hasReferenceToOwner);
            if (!hasReferenceToOwner)
            {
                this.AddReferenceToOwner(newSection, newSchemeItems, ref complexColumnIndex);
            }

            this.stageSerializerSettings.SettingsSectionNames.Add(newSection.Name);
            (await cardMetadata.GetSectionsAsync(cancellationToken)).Add(newSection);
            for (var i = 0; i < this.krTypes.Count; i++)
            {
                this.krTypes[i].SchemeItems.Add(newSchemeItems[i]);
            }
        }

        /// <summary>
        /// Добавить все секции и колонки из <paramref name="originalSection"/> в <paramref name="newSection"/>.
        /// При переносе используются идентификаторы из <paramref name="sectionLink"/>.
        /// </summary>
        /// <param name="sectionLink"><inheritdoc cref="CardMetadataContainerLink" path="/summary"/></param>
        /// <param name="originalSection">Оригинальная секция данных.</param>
        /// <param name="newSection">Новая секция.</param>
        /// <param name="newSchemeItems">Ссылки на новую секцию метаданных о таблице и её колонках, использующиеся в расширяемых типах карточек.</param>
        /// <param name="addToSerializer">Признак, что колонки нужно добавлять в сериализатор.</param>
        /// <param name="complexColumnIndex">Ссылка на индекс ссылочной колонки (автоинкрементируется при использовании).</param>
        /// <param name="hasReferenceToOwner">Есть ли ссылка на родительскую секцию.</param>
        private void MoveColumns(
            CardMetadataContainerLink sectionLink,
            CardMetadataSection originalSection,
            CardMetadataSection newSection,
            IList<CardTypeSchemeItem> newSchemeItems,
            bool addToSerializer,
            ref short complexColumnIndex,
            out bool hasReferenceToOwner)
        {
            void AddToSerializer(CardMetadataColumn newColumn)
            {
                if (addToSerializer)
                {
                    this.stageSerializerSettings.SettingsFieldNames.Add(newColumn.Name);
                }
            }

            hasReferenceToOwner = false;
            for (var columnIndex = 0; columnIndex < originalSection.Columns.Count; columnIndex++)
            {
                var column = originalSection.Columns[columnIndex];
                var newColumn = this.CreateColumn(sectionLink, originalSection, column, newSection);
                newSection.Columns.Add(newColumn);

                if (column.ColumnType == CardMetadataColumnType.Complex)
                {
                    foreach (var newSchemeItem in newSchemeItems)
                    {
                        newSchemeItem.ColumnIDList.Add(newColumn.ID);
                    }

                    newColumn.ComplexColumnIndex = complexColumnIndex;
                    CardMetadataColumn physColumn;
                    while (columnIndex + 1 < originalSection.Columns.Count
                           && (physColumn = originalSection.Columns[columnIndex + 1]).ComplexColumnIndex == column.ComplexColumnIndex)
                    {
                        columnIndex++;
                        sectionLink.AddLink(physColumn.ID, Guid.NewGuid());
                        var newPhysColumn = this.CreateColumn(
                            sectionLink,
                            originalSection,
                            physColumn,
                            newSection,
                            complexColumnIndex);
                        newSection.Columns.Add(newPhysColumn);
                        AddToSerializer(newPhysColumn);
                    }

                    complexColumnIndex++;
                }
                else
                {
                    AddToSerializer(newColumn);

                    foreach (var newSchemeItem in newSchemeItems)
                    {
                        newSchemeItem.ColumnIDList.Add(newColumn.ID);
                    }
                }

                if (newColumn.ParentRowSection is not null
                    && !hasReferenceToOwner)
                {
                    hasReferenceToOwner = true;
                }
            }
        }

        private CardMetadataColumn CreateColumn(
            CardMetadataContainerLink sectionLink,
            CardMetadataSection originalSection,
            CardMetadataColumn originalColumn,
            CardMetadataSection newSection,
            short complexColumnIndex = -1)
        {
            var newColumn = new CardMetadataColumn
            {
                ID = sectionLink.GetLink(originalColumn.ID).NewID,
                Name = NewColumnName(originalSection, originalColumn, newSection),
                CardTypeIDList = newSection.CardTypeIDList.ToSealableList(),
                ColumnType = originalColumn.ColumnType,
                DefaultValidValue = originalColumn.DefaultValidValue,
                DefaultValue = originalColumn.DefaultValue,
                IsReference = originalColumn.IsReference,
                MetadataType = originalColumn.MetadataType,
                ComplexColumnIndex = complexColumnIndex,
            };
            if (originalColumn.ReferencedSection is not null)
            {
                newColumn.ReferencedSection = this.CopyMetadataReference(originalColumn.ReferencedSection);
            }

            if (originalColumn.ParentRowSection is not null)
            {
                newColumn.ParentRowSection = this.CopyMetadataReference(originalColumn.ParentRowSection);
                if (originalColumn.ParentRowSection.ID == DefaultSchemeHelper.KrStagesVirtual
                    && originalColumn.ColumnType == CardMetadataColumnType.Physical)
                {
                    this.stageSerializerSettings.ReferencesToStages.Add(new ReferenceToStage(newSection.Name, newColumn.Name));
                }
            }

            return newColumn;
        }

        private CardMetadataSectionReference CopyMetadataReference(CardMetadataSectionReference referencedSection)
        {
            var reference = new CardMetadataSectionReference
            {
                ID = referencedSection.ID,
                Name = referencedSection.Name,
            };
            if (this.sectionMapping.TryGetLink(referencedSection.ID, out var link))
            {
                reference.ID = link.NewID;
                if (this.newSectionNames.TryGetValue(link.NewID, out var newName))
                {
                    reference.Name = newName;
                }
            }

            return reference;
        }

        private void AddReferenceToOwner(
            CardMetadataSection newSection,
            IList<CardTypeSchemeItem> newSchemeItems,
            ref short maxComplexColumnIndex)
        {
            var stageColumn = new CardMetadataColumn
            {
                ID = Guid.NewGuid(),
                Name = KrConstants.StageReferenceToOwner,
                CardTypeIDList = newSection.CardTypeIDList.ToSealableList(),
                ColumnType = CardMetadataColumnType.Complex,
                ComplexColumnIndex = maxComplexColumnIndex,
                DefaultValidValue = null,
                DefaultValue = null,
                IsReference = false,
                MetadataType = new CardMetadataType(SchemeType.ReferenceTypified),
                ReferencedSection = new CardMetadataSectionReference
                {
                    ID = this.krStageSection.ID,
                    Name = this.krStageSection.Name,
                },
                ParentRowSection = new CardMetadataSectionReference
                {
                    ID = this.krStageSection.ID,
                    Name = this.krStageSection.Name,
                },
            };
            newSection.Columns.Add(stageColumn);
            foreach (var newSchemeItem in newSchemeItems)
            {
                newSchemeItem.ColumnIDList.Add(stageColumn.ID);
            }

            var stageRowIDColumn = new CardMetadataColumn
            {
                ID = Guid.NewGuid(),
                Name = KrConstants.StageRowIDReferenceToOwner,
                CardTypeIDList = newSection.CardTypeIDList.ToSealableList(),
                ColumnType = CardMetadataColumnType.Physical,
                ComplexColumnIndex = maxComplexColumnIndex,
                DefaultValidValue = Guid.Empty,
                DefaultValue = null,
                IsReference = true,
                MetadataType = CardMetadataTypes.Guid,
                ReferencedSection = null,
                ParentRowSection = new CardMetadataSectionReference
                {
                    ID = this.krStageSection.ID,
                    Name = this.krStageSection.Name,
                },
            };
            newSection.Columns.Add(stageRowIDColumn);

            this.stageSerializerSettings.ReferencesToStages.Add(
                new ReferenceToStage(newSection.Name, stageRowIDColumn.Name));
        }

        private static string NewSectionName(CardMetadataSection originalSection) =>
            StageTypeSettingsNaming.SectionName(originalSection.Name);

        private static string NewColumnName(
            CardMetadataSection originalSection,
            CardMetadataColumn originalColumn,
            CardMetadataSection newSection) =>
            newSection.ID == DefaultSchemeHelper.KrStagesVirtual
                ? StageTypeSettingsNaming.PlainColumnName(originalSection.Name, originalColumn.Name)
                : originalColumn.Name;

        #endregion

        /// <summary>
        /// Готовит тип карточки этапа к переносу в целевые расширяемые типы карточек <see cref="krTypes"/>.
        /// Регистрирует элементы в глобальном кэше.
        /// </summary>
        /// <param name="settingsCardType">Тип карточки с настройками этапа.</param>
        /// <param name="context">Контекст выполнения операции.</param>
        private async Task InjectStateSettingsAsync(
            CardType settingsCardType,
            ICardMetadataExtensionContext context)
        {
            // настраивает контролы на новые секции метаданных.
            await settingsCardType.VisitAsync(this.visitor, context.CancellationToken);
            using var ctx = new CardGlobalReferencesContext(context, settingsCardType);
            // регистрируем объекты в глобальном кэше.
            // согласно логике работы MoveForms копируются блоки форм, значит их и нужно регистрировать.
            foreach (var form in settingsCardType.Forms)
            {
                form.Blocks.MakeGlobal(ctx, form);
            }
            // все расширения и валидаторы просто переносятся, значит они тоже должны быть глобальными.
            // регистрируем валидаторы.
            settingsCardType.Validators.MakeGlobal(ctx);
            // регистрируем расширения.
            settingsCardType.Extensions.MakeGlobal(ctx);

            foreach (var krType in this.krTypes)
            {
                // простое копирование ссылок на блоки форм в табличный контрол блока SettingsType, krType.
                this.InjectForms(
                    krType,
                    settingsCardType);

                // копирование ссылок на валидаторы.
                krType.Validators.AddRange(settingsCardType.Validators);

                // копирование ссылок на расширения типов.
                krType.Extensions.AddRange(settingsCardType.Extensions);
            }
        }

        /// <summary>
        /// Перенос всех блоков из форм типа карточки этапа <paramref name="settingsCardType"/> в целевой тип карточки <paramref name="krType"/>.
        /// </summary>
        /// <param name="krType">Целевой тип карточки.</param>
        /// <param name="settingsCardType">Тип карточки этапа.</param>
        private void InjectForms(
            CardType krType,
            CardType settingsCardType)
        {
            var settingsBlock = TryGetBlock(
                krType,
                KrConstants.Ui.KrStageSettingsBlockAlias);

            if (settingsBlock is null)
            {
                return;
            }

            var tabControl = new CardTypeTabControl();
            settingsBlock.Controls.Add(tabControl);
            tabControl.ControlSettings[KrConstants.Ui.StageHandlerDescriptorIDSetting] =
                this.stageSettingsWithDescriptors[settingsCardType.ID]
                    .Select(static i => (object) i.ID)
                    .ToList();

            tabControl.Type = settingsCardType.Forms.Count > 1
                ? CardControlTypes.TabControl
                : CardControlTypes.Container;

            foreach (var form in settingsCardType.Forms)
            {
                CardTypeTabControlForm tab = new()
                {
                    TabCaption = form.TabCaption,
                    Name = form.Name,
                };
                tab.Blocks.AddRange(form.Blocks);
                tabControl.Forms.Add(tab);
            }
        }

        #region Optional Injectable Card Types

        /// <summary>
        /// Возвращает данные о типах карточек настроек:
        /// <see cref="DefaultCardTypes.KrPerformersSettingsTypeName"/>, <see cref="DefaultCardTypes.KrAuthorSettingsTypeName"/>, <see cref="DefaultCardTypes.KrTaskKindSettingsTypeName"/>, <see cref="DefaultCardTypes.KrHistoryManagementStageTypeSettingsTypeName"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="ICardMetadataExtensionContext" path="/summary"/></param>
        /// <returns>Информация о дополнительно внедряемых типах карточек с операциями копирования их элементов.</returns>
        private async ValueTask<InjectCardTypeInfo[]> GetInjectableCardsAsync(
            ICardMetadataExtensionContext context) =>
            [
                new InjectCardTypeInfo(await this.PrepareInjectableCardTypeAsync(
                        DefaultCardTypes.KrPerformersSettingsTypeID,
                        context.CardTypes,
                        context.CancellationToken), PreparePerformersSettings, InjectPerformers),
                new InjectCardTypeInfo(await this.PrepareInjectableCardTypeAsync(
                        DefaultCardTypes.KrAuthorSettingsTypeID,
                        context.CardTypes,
                        context.CancellationToken), PrepareAuthorSettings, InjectAuthor),
                new InjectCardTypeInfo(await this.PrepareInjectableCardTypeAsync(
                        DefaultCardTypes.KrTaskKindSettingsTypeID,
                        context.CardTypes,
                        context.CancellationToken), PrepareTaskKindSettings, InjectTaskKind),
                new InjectCardTypeInfo(await this.PrepareInjectableCardTypeAsync(
                        DefaultCardTypes.KrHistoryManagementStageTypeSettingsTypeID,
                        context.CardTypes,
                        context.CancellationToken), PrepareTaskHistoryGroupSettings, InjectTaskHistoryGroup),
            ];

        /// <summary>
        /// Подготовить тип карточки для вставки (копия с исходного типа), в том числе зарезервировать
        /// новые идентификаторы для секций и колонок, используемых в типе.
        /// </summary>
        /// <param name="typeID">Идентификатор типа.</param>
        /// <param name="cardTypes">Доступные типы карточек.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Копия заданного типа или значение <see langword="null"/>, если он не найден в <paramref name="cardTypes"/>.</returns>
        private async ValueTask<CardType?> PrepareInjectableCardTypeAsync(
            Guid typeID,
            CardTypeCollection cardTypes,
            CancellationToken cancellationToken = default)
        {
            CardType? type = null;
            if (cardTypes.TryGetValue(typeID, out var t))
            {
                type = await t.DeepCloneAsync(cancellationToken);
                this.BuildMapping([type]);
            }

            return type;
        }

        #region KrPerformersSettings Configuration Methods

        /// <summary>
        /// Настроить контролы типа карточки KrPerformersSettings.
        /// </summary>
        /// <param name="cardType">Тип карточки</param>
        /// <param name="context">Контекст выполнения операции.</param>
        /// <param name="visibilityContext">Контекст настроек видимости контролов (<see cref="VisibilityContext"/>).</param>
        private static void PreparePerformersSettings(
            CardType cardType,
            ICardMetadataExtensionContext context,
            VisibilityContext visibilityContext)
        {
            // Блок быть должен, как и тип.
            var performersControls = cardType.Forms[0].Blocks[0].Controls;

            // т.к. в InjectPerformers мы копируем все контролы, то они должны быть помещены в глобальный кэш.
            using var ctx = new CardGlobalReferencesContext(context, cardType);
            performersControls.MakeGlobal(ctx);

            var singlePerformerControl =
                performersControls.FirstOrDefault(p => p.Name == KrConstants.Ui.KrSinglePerformerEntryAcAlias);
            if (singlePerformerControl is not null)
            {
                singlePerformerControl.ControlSettings[KrConstants.Ui.VisibleForTypesSetting] =
                    visibilityContext.SinglePerformers;
                singlePerformerControl.ControlSettings[KrConstants.Ui.RequiredForTypesSetting] =
                    visibilityContext.CheckPerformers;
                singlePerformerControl.ControlSettings[KrConstants.Ui.ControlCaptionsSetting] =
                    visibilityContext.PerformerCaptions;
            }

            var multiplePerformersControl =
                performersControls.FirstOrDefault(p => p.Name == KrConstants.Ui.KrMultiplePerformersTableAcAlias);
            if (multiplePerformersControl is not null)
            {
                multiplePerformersControl.ControlSettings[KrConstants.Ui.VisibleForTypesSetting] =
                    visibilityContext.MultiplePerformers;
                multiplePerformersControl.ControlSettings[KrConstants.Ui.ControlCaptionsSetting] =
                    visibilityContext.PerformerCaptions;
            }
        }

        /// <summary>
        /// Метод вставки всех контролов первой формы первого блока типа карточки KrPerformersSettings (<paramref name="cardType"/>)
        /// в целевой тип карточки <paramref name="krType"/>.
        /// </summary>
        /// <param name="krType">Целевой тип карточки.</param>
        /// <param name="cardType">Тип карточки KrPerformersSettings.</param>
        private static void InjectPerformers(
            CardType krType,
            CardType cardType)
        {
            var performersBlock = TryGetBlock(
                krType,
                KrConstants.Ui.KrPerformersBlockAlias);

            if (performersBlock is null)
            {
                return;
            }

            // Блок быть должен, как и тип.
            var performersControls = cardType
                .Forms[0]
                .Blocks[0]
                .Controls;

            performersBlock.Controls.AddRange(performersControls);
        }

        #endregion

        #region KrAuthorSettings Configuration Methods

        /// <summary>
        /// Настроить контролы типа карточки KrAuthorSettings (AuthorEntryAC).
        /// </summary>
        /// <param name="authorCardType">Тип карточки.</param>
        /// <param name="context">Контекст выполнения операции.</param>
        /// <param name="visibilityContext">Контекст настроек видимости контролов (<see cref="VisibilityContext"/>).</param>
        private static void PrepareAuthorSettings(
            CardType authorCardType,
            ICardMetadataExtensionContext context,
            VisibilityContext visibilityContext)
        {
            var control = authorCardType
                .Forms[0]
                .Blocks[0]
                .Controls
                .FirstOrDefault(static p => p.Name == KrConstants.Ui.AuthorEntryAlias);

            if (control is not null)
            {
                // помещаем контрол в кэш.
                using var ctx = new CardGlobalReferencesContext(context, authorCardType);
                control.MakeGlobal(ctx);
                control.ControlSettings[KrConstants.Ui.VisibleForTypesSetting] =
                    visibilityContext.CanOverrideAuthor;
            }
        }

        /// <summary>
        /// Метод вставки контрола AuthorEntryAC первой формы первого блока типа карточки KrAuthorSettings (<paramref name="authorCardType"/>)
        /// в целевой тип карточки <paramref name="krType"/>.
        /// </summary>
        /// <param name="krType">Целевой тип карточки.</param>
        /// <param name="authorCardType">Тип карточки KrAuthorSettings.</param>
        private static void InjectAuthor(
            CardType krType,
            CardType authorCardType)
        {
            var authorBlock = TryGetBlock(krType, KrConstants.Ui.AuthorBlockAlias);

            if (authorBlock is null)
            {
                return;
            }

            var control = authorCardType.Forms[0]
                .Blocks[0]
                .Controls
                .FirstOrDefault(p => p.Name == KrConstants.Ui.AuthorEntryAlias);

            if (control is not null)
            {
                authorBlock.Controls.AddRange(control);
            }
        }

        #endregion

        #region KrTaskKindSettings Configuration Methods

        /// <summary>
        /// Настроить контролы типа карточки KrTaskKindSettings (TaskKindEntryAC).
        /// </summary>
        /// <param name="taskKindCardType">Тип карточки.</param>
        /// <param name="context">Контекст выполнения операции.</param>
        /// <param name="visibilityContext">Контекст настроек видимости контролов (<see cref="VisibilityContext"/>).</param>
        private static void PrepareTaskKindSettings(
            CardType taskKindCardType,
            ICardMetadataExtensionContext context,
            VisibilityContext visibilityContext)
        {
            var control = taskKindCardType.Forms[0]
                .Blocks[0]
                .Controls
                .FirstOrDefault(static p => p.Name == KrConstants.Ui.TaskKindEntryAlias);

            if (control is not null)
            {
                // помещаем контрол в кэш.
                using var ctx = new CardGlobalReferencesContext(context, taskKindCardType);
                control.MakeGlobal(ctx);
                control.ControlSettings[KrConstants.Ui.VisibleForTypesSetting] = visibilityContext.UseTaskKind;
            }
        }

        /// <summary>
        /// Метод вставки контрола TaskKindEntryAC первой формы первого блока типа карточки KrTaskKindSettings (<paramref name="taskKindCardType"/>)
        /// в целевой тип карточки <paramref name="krType"/>.
        /// </summary>
        /// <param name="krType">Целевой тип карточки.</param>
        /// <param name="taskKindCardType">Тип карточки KrTaskKindSettings.</param>
        private static void InjectTaskKind(
            CardType krType,
            CardType taskKindCardType)
        {
            var taskKindBlock = TryGetBlock(
                krType,
                KrConstants.Ui.TaskKindBlockAlias);

            if (taskKindBlock is null)
            {
                return;
            }

            var taskKindControl = taskKindCardType.Forms[0]
                .Blocks[0]
                .Controls
                .FirstOrDefault(static p => p.Name == KrConstants.Ui.TaskKindEntryAlias);

            if (taskKindControl is not null)
            {
                taskKindBlock.Controls.AddRange(taskKindControl);
            }
        }

        #endregion

        #region KrHistoryManagementStageTypeSettings Configuration Methods

        /// <summary>
        /// Настроить контролы типа карточки KrHistoryManagementStageTypeSettings (KrTaskHistoryGroupTypeControlAlias, KrParentTaskHistoryGroupTypeControlAlias, KrTaskHistoryGroupNewIterationControlAlias).
        /// </summary>
        /// <param name="taskHistoryType">Тип карточки.</param>
        /// <param name="context">Контекст выполнения операции.</param>
        /// <param name="visibilityContext">Контекст настроек видимости контролов (<see cref="VisibilityContext"/>).</param>
        private static void PrepareTaskHistoryGroupSettings(
            CardType taskHistoryType,
            ICardMetadataExtensionContext context,
            VisibilityContext visibilityContext)
        {
            var controls = taskHistoryType.Forms[0].Blocks[0].Controls;

            // т.к. в InjectTaskHistoryGroup мы копируем все контролы, то они должны быть помещены в глобальный кэш.
            using var ctx = new CardGlobalReferencesContext(context, taskHistoryType);
            controls.MakeGlobal(ctx);

            foreach (var controlName in
                     new[]
                     {
                         KrConstants.Ui.KrTaskHistoryGroupTypeControlAlias,
                         KrConstants.Ui.KrParentTaskHistoryGroupTypeControlAlias,
                         KrConstants.Ui.KrTaskHistoryGroupNewIterationControlAlias,
                     })
            {
                var control = controls.FirstOrDefault(p => p.Name == controlName);

                if (control is not null)
                {
                    control.ControlSettings[KrConstants.Ui.VisibleForTypesSetting] =
                        visibilityContext.CanOverrideTaskHistoryGroup;
                }
            }
        }

        /// <summary>
        /// Метод вставки контрола TaskKindEntryAC первой формы первого блока типа карточки KrHistoryManagementStageTypeSettings (<paramref name="taskHistoryType"/>)
        /// в целевой тип карточки <paramref name="krType"/>.
        /// </summary>
        /// <param name="krType">Целевой тип карточки.</param>
        /// <param name="taskHistoryType">Тип карточки KrHistoryManagementStageTypeSettings.</param>
        private static void InjectTaskHistoryGroup(
            CardType krType,
            CardType taskHistoryType)
        {
            var historyBlock = TryGetBlock(
                krType,
                KrConstants.Ui.KrTaskHistoryBlockAlias);

            if (historyBlock is null)
            {
                return;
            }

            var controls = taskHistoryType
                .Forms[0]
                .Blocks[0]
                .Controls;

            historyBlock.Controls.AddRange(controls);
        }

        #endregion

        /// <summary>
        /// Добавляет в <see cref="krTypes"/> данные о типах карточек:
        /// KrAuthorSettings, KrPerformersSettings, KrTaskKindSettings, KrHistoryManagementStageTypeSettings.
        /// </summary>
        /// <param name="injectableInfo">Преднастроенные типы карточек (KrAuthorSettings, KrPerformersSettings, KrTaskKindSettings, KrHistoryManagementStageTypeSettings) и операции их копирования в целевые типы.</param>
        /// <param name="visibilityContext"><inheritdoc cref="VisibilityContext" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="ICardMetadataExtensionContext" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        private async Task InjectOptionalControlsAndSetVisibilityAsync(
            InjectCardTypeInfo[] injectableInfo,
            VisibilityContext visibilityContext,
            ICardMetadataExtensionContext context,
            CancellationToken cancellationToken = default)
        {
            // настройка контролов в типах: KrAuthorSettings, KrPerformersSettings, KrTaskKindSettings, KrHistoryManagementStageTypeSettings.
            foreach (var info in injectableInfo)
            {
                if (info.CardType is null)
                {
                    continue;
                }

                await info.CardType.VisitAsync(this.visitor, cancellationToken);

                // ВНИМАНИЕ! Подготовку вызывать только после VisitAsync, т.к.
                // в нём настраиваются имена контролов, которыми оперирует PrepareAction.
                info.PrepareAction?.Invoke(info.CardType, context, visibilityContext);
            }
            // окончательная настройка контролов с учётом видимости этапов
            // и копирование контролов из injectableInfo
            foreach (var krType in this.krTypes)
            {
                foreach (var info in injectableInfo)
                {
                    if (info.CardType is not null
                        && info.ModifyAction is not null)
                    {
                        info.ModifyAction(krType, info.CardType);
                    }
                }

                var commonBlock = TryGetBlock(krType, KrConstants.Ui.KrStageCommonInfoBlock);

                if (commonBlock is null)
                {
                    continue;
                }

                var timeLimitControl =
                    commonBlock.Controls.FirstOrDefault(p => p.Name == KrConstants.Ui.KrTimeLimitInputAlias);
                if (timeLimitControl is not null)
                {
                    timeLimitControl.ControlSettings[KrConstants.Ui.VisibleForTypesSetting] = visibilityContext.UseDefaultTimeLimit;
                }

                var plannedControl =
                    commonBlock.Controls.FirstOrDefault(p => p.Name == KrConstants.Ui.KrPlannedInputAlias);
                if (plannedControl is not null)
                {
                    plannedControl.ControlSettings[KrConstants.Ui.VisibleForTypesSetting] = visibilityContext.UseDefaultPlanned;
                }

                var canBeHiddenCheckbox =
                    commonBlock.Controls.FirstOrDefault(p => p.Name == KrConstants.Ui.KrHiddenStageCheckboxAlias);
                if (canBeHiddenCheckbox is not null)
                {
                    canBeHiddenCheckbox.ControlSettings[KrConstants.Ui.VisibleForTypesSetting] = visibilityContext.CanBeHidden;
                }

                var canBeSkippedCheckbox =
                    commonBlock.Controls.FirstOrDefault(p => p.Name == KrConstants.Ui.KrCanBeSkippedCheckboxAlias);
                if (canBeSkippedCheckbox is not null)
                {
                    canBeSkippedCheckbox.ControlSettings[KrConstants.Ui.VisibleForTypesSetting] = visibilityContext.CanBeSkipped;
                }
            }
        }

        /// <summary>
        /// Создает контекст <see cref="VisibilityContext"/>.
        /// </summary>
        /// <param name="descriptors">Коллекция зарегистрированных дескрипторов типов этапов.</param>
        /// <returns><inheritdoc cref="VisibilityContext" path="/summary"/></returns>
        private static VisibilityContext GetVisibilityContext(ICollection<StageTypeDescriptor> descriptors)
        {
            var context = new VisibilityContext(descriptors.Count);
            context.Init(descriptors);
            return context;
        }

        #endregion

        /// <summary>
        /// Возвращает блок с заданным именем из таблицы <see cref="KrConstants.Ui.KrApprovalStagesControlAlias"/> (ApprovalStagesTable)
        /// блока <see cref="KrConstants.Ui.KrApprovalStagesBlockAlias"/> (ApprovalStagesBlock)
        /// формы <see cref="KrConstants.Ui.KrApprovalProcessFormAlias"/> (ApprovalProcess)
        /// переданного типа карточки.
        /// </summary>
        /// <param name="cardType"><inheritdoc cref="CardType" path="/summary"/></param>
        /// <param name="blockName">Имя искомого блока.</param>
        /// <returns>Ссылка на найденный блок, или <see langword="null"/>, если блок не удалось найти.</returns>
        private static CardTypeBlock? TryGetBlock(
            CardType cardType,
            string blockName)
        {
            var tableControl = cardType
                .Forms
                .FirstOrDefault(p => p.Name == KrConstants.Ui.KrApprovalProcessFormAlias)
                ?.Blocks
                .FirstOrDefault(p => p.Name == KrConstants.Ui.KrApprovalStagesBlockAlias)
                ?.Controls
                .FirstOrDefault(p => p.Name == KrConstants.Ui.KrApprovalStagesControlAlias) as CardTypeTableControl;

            return tableControl
                ?.Form
                .Blocks
                .FirstOrDefault(p => p.Name == blockName);
        }

        #endregion
    }
}
