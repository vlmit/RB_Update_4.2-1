#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess.Formatters;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Json;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Scheme;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization
{
    /// <inheritdoc cref="IKrStageSerializer"/>
    public sealed class KrStageSerializer :
        IKrStageSerializer
    {
        #region Nested Types

        [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
        private sealed class StageNode
        {
            #region Constructors

            public StageNode(CardRow row)
            {
                this.Row = row;
                this.ParentStageRowID = row.TryGet<Guid?>(KrConstants.KrStages.ParentStageRowID);
            }

            #endregion

            #region Properties

            public CardRow Row { get; }

            public Guid? ParentStageRowID { get; }

            public List<StageNode> Children { get; } = new List<StageNode>();

            public int Level { get; set; }

            #endregion

            #region Private Methods

            private string GetDebuggerDisplay() =>
                $"{DebugHelper.GetTypeName(this)}:"
                + $" Name=\"{this.Row.Get<string>(KrConstants.KrStages.NameField)}\","
                + $" RowID={this.Row.RowID:B},"
                + $" ChildrenCount={this.Children.Count}";

            #endregion
        }

        #endregion

        #region Constants And Static Fields

        // Это абсолютно случайный и ни на что не влияющий идентификатор
        // нужен только для удачного восстановления структуры
        private static readonly Guid fakeCardID =
            new Guid(0xCECE4CEE, 0x5B29, 0x4FE5, 0x8A, 0xE0, 0x43, 0x49, 0xDF, 0xF0, 0x01, 0x81);

        #endregion

        #region Fields

        private readonly ICardRepairManager repairManager;
        private readonly ICardMetadata cardMetadata;
        private readonly IStageTypeFormatterContainer formatterContainer;
        private readonly ISession session;
        private readonly IKrProcessContainer processContainer;
        private readonly IExtensionContainer extensionContainer;
        private readonly IKrStageSerializerSettingsCache krStageSerializerSettingsCache;

        #endregion

        #region Constructors

        public KrStageSerializer(
            ICardMetadata cardMetadata,
            [Dependency(CardRepairManagerNames.Default)]
            ICardRepairManager repairManager,
            IStageTypeFormatterContainer formatterContainer,
            ISession session,
            IKrProcessContainer processContainer,
            IExtensionContainer extensionContainer,
            IKrStageSerializerSettingsCache krStageSerializerSettingsCache)
        {
            this.cardMetadata = NotNullOrThrow(cardMetadata);
            this.repairManager = NotNullOrThrow(repairManager);
            this.formatterContainer = NotNullOrThrow(formatterContainer);
            this.session = NotNullOrThrow(session);
            this.processContainer = NotNullOrThrow(processContainer);
            this.extensionContainer = NotNullOrThrow(extensionContainer);
            this.krStageSerializerSettingsCache = NotNullOrThrow(krStageSerializerSettingsCache);
        }

        #endregion

        #region IKrStageSerializer Members

        /// <inheritdoc />
        public ValueTask<IKrStageSerializerSettings> GetSettingsAsync(
            CancellationToken cancellationToken = default) =>
            this.krStageSerializerSettingsCache.GetAsync(cancellationToken);

        /// <inheritdoc />
        public string Serialize(object value) =>
            StorageHelper.SerializeToJson(value, TessaSerializer.JsonTyped);

        /// <inheritdoc />
        public T? Deserialize<T>(string json) =>
            StorageHelper.DeserializeFromJson<T>(json, TessaSerializer.JsonTyped);

        /// <inheritdoc />
        public async ValueTask<IDictionary<Guid, IDictionary<string, object?>>> MergeStageSettingsAsync(
            CardSection krStagesSection,
            StringDictionaryStorage<CardSection> updatedSections,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(krStagesSection);
            ThrowIfNull(updatedSections);

            var stageStorages = new Dictionary<Guid, IDictionary<string, object?>>();
            foreach (var sectionName in (await this.GetSettingsAsync(cancellationToken)).SettingsSectionNames)
            {
                if (!updatedSections.TryGetValue(sectionName, out var updatedSection))
                {
                    continue;
                }

                foreach (var updatedRowOriginal in updatedSection.Rows)
                {
                    // Копируем строку, чтобы не портить основную.
                    var updatedRow = updatedRowOriginal.Clone();

                    if (!updatedRow.TryGetValue(KrConstants.Keys.ParentStageRowID, out var stageRowIDObj)
                        || stageRowIDObj is not Guid stageRowID)
                    {
                        // Не знаем, что это за строка. Пропускаем.
                        continue;
                    }

                    // Получаем storage для необходимого этапа.
                    // Если его нет(а точнее строки), то создадим новое хранилище.
                    if (!stageStorages.TryGetValue(stageRowID, out var stageStorage))
                    {
                        var originalRow = krStagesSection.Rows.FirstOrDefault(p => p.RowID == stageRowID);
                        stageStorage = originalRow is not null
                            ? await this.DeserializeSettingsStorageAsync(
                                originalRow,
                                cancellationToken: cancellationToken)
                            : new Dictionary<string, object?>(StringComparer.Ordinal);
                        stageStorages[stageRowID] = stageStorage;
                    }

                    if (sectionName == KrConstants.KrStages.Virtual)
                    {
                        // Поля из строки KrStagesVirtual располагаются на верхнем уровне
                        PrepareUpdatedRow(updatedRow);
                        var updatedRowStorage = updatedRow.GetStorage();
                        foreach (var plainSetting in (await this.GetSettingsAsync(cancellationToken)).SettingsFieldNames)
                        {
                            if (updatedRowStorage.TryGetValue(plainSetting, out var newValue))
                            {
                                stageStorage[plainSetting] = newValue;
                            }
                        }
                    }
                    else
                    {
                        // Получаем секцию из storage конкретного этапа
                        // Для упрощения - секция это сразу массив строк.
                        var sectionRows = stageStorage.TryGet<IList>(sectionName);
                        if (sectionRows is null)
                        {
                            sectionRows = new List<object>();
                            stageStorage[sectionName] = sectionRows;
                        }

                        ProcessUpdatedRow(updatedRow, sectionRows);
                    }
                }
            }

            foreach (var rowStorages in stageStorages.Values)
            {
                foreach (var empty in (await this.GetSettingsAsync(cancellationToken)).SettingsSectionNames.Where(p => !rowStorages.ContainsKey(p)))
                {
                    rowStorages.Add(empty, Array.Empty<object>());
                }
            }

            return stageStorages;
        }

        /// <inheritdoc />
        public async ValueTask SerializeSettingsStorageAsync(
            CardRow stageRow,
            IDictionary<string, object?> settingsStorage,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(stageRow);
            ThrowIfNull(settingsStorage);

            if (settingsStorage is not Dictionary<string, object?> settingsDict)
            {
                throw new ArgumentException($"Parameter {nameof(settingsStorage)} is required type Dictionary<string, object>.", nameof(settingsStorage));
            }

            foreach (var orderColumn in (await this.GetSettingsAsync(cancellationToken)).OrderColumns)
            {
                if (settingsDict.TryGetValue(orderColumn.SectionName, out var rowsObj)
                    && rowsObj is IList rows
                    && rowsObj is not byte[])
                {
                    KrProcessSharedHelper.RepairStorageRowsOrders(
                        rows,
                        orderColumn.OrderFieldName);
                }
            }

            settingsDict.Remove(KrConstants.Keys.ParentStageRowID);
            stageRow.Fields[KrConstants.KrStages.Settings] = this.Serialize(settingsDict);
        }

        /// <inheritdoc />
        public async ValueTask<IDictionary<string, object?>> DeserializeSettingsStorageAsync(
            string? settings,
            Guid rowID,
            bool repairStorage = true,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(settings))
            {
                return new Dictionary<string, object?>(StringComparer.Ordinal);
            }

            var stageStorage = this.Deserialize<Dictionary<string, object?>>(settings)
                ?? new Dictionary<string, object?>(StringComparer.Ordinal);

            if (!repairStorage)
            {
                return stageStorage;
            }

            var doubleSize = 2 * stageStorage.Count;
            var redundantKeys = new List<string>(stageStorage.Count);
            var stageRowStorage = new Dictionary<string, object?>(doubleSize, StringComparer.Ordinal)
            {
                [KrConstants.KrStages.RowID] = rowID,
            };
            var sectionsStorage = new Dictionary<string, object?>(doubleSize, StringComparer.Ordinal);
            var serializerSettings = await this.GetSettingsAsync(cancellationToken);

            foreach (var pair in stageStorage)
            {
                if (pair.Key == KrConstants.KrStages.Virtual)
                {
                    continue;
                }

                if (pair.Value is IList collectionRows
                    && collectionRows is not byte[]
                    && serializerSettings.SettingsSectionNames.Contains(pair.Key))
                {
                    var sectionStorage = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        [CardSection.SystemTableTypeKey] = Int32Boxes.Box((int) CardTableType.Collection),
                        [CardSection.RowsKey] = collectionRows,
                    };

                    sectionsStorage[pair.Key] = sectionStorage;
                }
                else if (serializerSettings.SettingsFieldNames.Contains(pair.Key))
                {
                    stageRowStorage[pair.Key] = pair.Value;
                }
                else
                {
                    redundantKeys.Add(pair.Key);
                }
            }

            var stagesSection = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [CardSection.SystemTableTypeKey] = Int32Boxes.Box((int) CardTableType.Collection),
                [CardSection.RowsKey] = new List<object> { stageRowStorage },
            };
            sectionsStorage[KrConstants.KrStages.Virtual] = stagesSection;

            var cardToRepair = CreateFakeRepairableCard(null, sectionsStorage);

            var repairResult = await this.repairManager.RepairAsync(
                cardToRepair,
                new CardRepairOptions
                {
                    ErrorsOnly = true,
                },
                cancellationToken: cancellationToken);

            if (!repairResult.IsSuccessful)
            {
                throw new ValidationException(repairResult);
            }

            foreach (var settingsFieldName in serializerSettings.SettingsFieldNames)
            {
                stageStorage[settingsFieldName] = stageRowStorage[settingsFieldName];
            }

            foreach (var settingsSectionName in serializerSettings.SettingsSectionNames)
            {
                if (!stageStorage.ContainsKey(settingsSectionName))
                {
                    stageStorage[settingsSectionName] = new List<object>();
                }
            }

            foreach (var redundantKey in redundantKeys)
            {
                stageStorage.Remove(redundantKey);
            }

            return stageStorage;
        }

        /// <inheritdoc />
        public ValueTask<IDictionary<string, object?>> DeserializeSettingsStorageAsync(
            CardRow stageRow,
            bool repairStorage = true,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(stageRow);

            if (!stageRow.TryGetValue(KrConstants.KrStages.Settings, out var settingsObj)
                || settingsObj is not string settings
                || string.IsNullOrWhiteSpace(settings))
            {
                return new ValueTask<IDictionary<string, object?>>(new Dictionary<string, object?>(StringComparer.Ordinal));
            }

            return this.DeserializeSettingsStorageAsync(settings, stageRow.RowID, repairStorage, cancellationToken);
        }

        /// <inheritdoc />
        public async Task DeserializeSectionsAsync(
            Card sourceCard,
            Card destinationCard,
            IDictionary<Guid, IDictionary<string, object?>>? predeserializedSettings = null,
            KrProcessSerializerHiddenStageMode hiddenStageMode = KrProcessSerializerHiddenStageMode.Ignore,
            ICardExtensionContext? cardExtensionContext = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(sourceCard);
            ThrowIfNull(destinationCard);

            if (!destinationCard.Sections.ContainsKey(KrConstants.KrStages.Virtual))
            {
                return;
            }

            var isCreateStagePositionInfo = KrProcessSharedHelper.RuntimeCard(sourceCard.TypeID);
            var cardToRepair = CreateFakeRepairableCard(destinationCard, null);
            var sections = cardToRepair.Sections;
            var sourceRows = sourceCard.GetStagesSection().Rows;
            var stagePositions = new List<KrStagePositionInfo>(sourceRows.Count);

            await this.ParseSettingsAsync(
                sourceRows,
                sections,
                stagePositions,
                predeserializedSettings,
                hiddenStageMode,
                isCreateStagePositionInfo,
                cancellationToken);

            var cardType = (await this.cardMetadata.GetCardTypesAsync(cancellationToken))[destinationCard.TypeID];
            var routeCardType = KrProcessSharedHelper.DesignTimeCard(cardType.ID)
                ? RouteCardType.Template
                : RouteCardType.Document;

            var context = new KrStageRowExtensionContext(
                null,
                cardToRepair,
                sourceCard,
                destinationCard,
                cardType,
                routeCardType,
                cardExtensionContext,
                cancellationToken);

            await using var executor = await this.extensionContainer.ResolveExecutorAsync<IKrStageRowExtension>();
            await executor.ExecuteAsync(nameof(IKrStageRowExtension.DeserializationBeforeRepair), context);

            var repairResult = await this.repairManager.RepairAsync(
                cardToRepair,
                new CardRepairOptions
                {
                    ErrorsOnly = true
                },
                cancellationToken: cancellationToken);

            if (!repairResult.IsSuccessful)
            {
                throw new ValidationException(repairResult);
            }

            var sectionsStorage = sections.GetStorage();
            var serializerSettings = await this.GetSettingsAsync(cancellationToken);

            foreach (var section in destinationCard.Sections)
            {
                if (!sectionsStorage.ContainsKey(section.Key)
                    || !serializerSettings.SettingsSectionNames.Contains(section.Key))
                {
                    sectionsStorage[section.Key] = section.Value.GetStorage();
                }
            }

            destinationCard.Sections = new StringDictionaryStorage<CardSection>(
                sectionsStorage,
                CardComponentHelper.SectionFactory);

            if (isCreateStagePositionInfo)
            {
                destinationCard.SetStagePositions(stagePositions);
            }

            await executor.ExecuteAsync(nameof(IKrStageRowExtension.DeserializationAfterRepair), context);
        }

        /// <inheritdoc />
        public async Task UpdateStageSettingsAsync(
            Card innerCard,
            Card outerCard,
            IDictionary<Guid, IDictionary<string, object?>> stageStorages,
            IKrProcessCache krProcessCache,
            ICardExtensionContext? cardExtensionContext = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(innerCard);
            ThrowIfNull(outerCard);
            ThrowIfNull(krProcessCache);

            if (stageStorages is null)
            {
                return;
            }

            if (!innerCard.TryGetStagesSection(out var satelliteStages))
            {
                return;
            }

            var cardType = (await this.cardMetadata.GetCardTypesAsync(cancellationToken))[innerCard.TypeID];
            var routeCardType = KrProcessSharedHelper.DesignTimeCard(cardType.ID)
                ? RouteCardType.Template
                : RouteCardType.Document;

            var context = new KrStageRowExtensionContext(
                stageStorages,
                null,
                innerCard,
                outerCard,
                cardType,
                routeCardType,
                cardExtensionContext,
                cancellationToken);

            await using (var executor = await this.extensionContainer.ResolveExecutorAsync<IKrStageRowExtension>())
            {
                await executor.ExecuteAsync(nameof(IKrStageRowExtension.BeforeSerialization), context);
            }

            foreach (var row in satelliteStages.Rows)
            {
                if (row.State == CardRowState.Deleted
                    || !stageStorages.TryGetValue(row.RowID, out var storage))
                {
                    continue;
                }

                if (row.State == CardRowState.Modified)
                {
                    if (KrProcessSharedHelper.CanBeSkipped(row)
                        && row.IsChanged(KrConstants.KrStages.Skip)
                        && !row.TryGet<bool>(KrConstants.KrStages.Skip))
                    {
                        var runtimeStages = (await krProcessCache.GetAllStagesByTemplatesAsync(cancellationToken))
                            .GetValueOrDefault(row.TryGet<Guid>(KrConstants.KrStages.BasedOnStageTemplateID), ImmutableList<IKrRuntimeStage>.Empty);

                        var runtimeStage = runtimeStages.Single(i => i.StageID == row.TryGet<Guid>(KrConstants.KrStages.BasedOnStageRowID));

                        row.Fields[KrConstants.KrStages.Hidden] = BooleanBoxes.Box(runtimeStage.Hidden);
                    }
                }

                await this.FormatRowAsync(
                    row,
                    innerCard,
                    storage,
                    cancellationToken);

                await this.SerializeSettingsStorageAsync(
                    row,
                    storage,
                    cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async ValueTask FillStageSettingsAsync(
            CardRow row,
            IDictionary<string, object?> storage,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(row);
            ThrowIfNull(storage);

            PrepareUpdatedRow(row);

            var updatedRowStorage = row.GetStorage();
            var serializerSettings = await this.GetSettingsAsync(cancellationToken);

            foreach (var plainSetting in serializerSettings.SettingsFieldNames)
            {
                if (updatedRowStorage.TryGetValue(plainSetting, out var newValue))
                {
                    storage[plainSetting] = newValue;
                }
            }
        }

        #endregion

        #region Private Methods

        private static void AppendLevelLabel(
            StageNode node,
            CardRow rowCopy,
            int[] stairs)
        {
            const int spacesPerLevel = 2;
            if (node.Level <= 0)
            {
                return;
            }

            var sb = StringBuilderHelper.Acquire();
            sb
                .Append(' ', node.Level * spacesPerLevel)
                .Append('(');

            if (node.Level > 1)
            {
                sb.Append(stairs[1] + 1);
            }

            for (var i = 2; i < node.Level; i++)
            {
                sb.Append('.');
                sb.Append(stairs[i] + 1);
            }

            if (node.Level > 1)
            {
                sb.Append('.');
            }

            sb.Append(rowCopy.Get<int>(KrConstants.KrStages.NestedOrder) + 1);
            sb.Append(") ");
            sb.Append(LocalizationManager.EscapeIfLocalizationString(
                rowCopy.Get<string>(KrConstants.KrStages.NameField)));

            rowCopy[KrConstants.KrStages.NameField] = sb.ToStringAndRelease();
        }

        private static void SetTreeStructureMark(
            StageNode node,
            CardRow rowCopy)
        {
            rowCopy[KrConstants.Keys.RootStage] = BooleanBoxes.Box(node.Level == 0 && node.Children.Count > 0);
            rowCopy[KrConstants.Keys.NestedStage] =
                BooleanBoxes.Box(rowCopy.TryGet<Guid?>(KrConstants.KrStages.NestedProcessID).HasValue);
        }

        private static IEnumerable<StageNode> CreateHierarchicalListOfNodes(ICollection<CardRow> rows)
        {
            var nodes = new List<StageNode>(rows.Count);
            var nodesTable = new HashSet<Guid, StageNode>(p => p.Row.RowID);
            var hasChild = false;
            foreach (var row in rows)
            {
                var node = new StageNode(row);

                nodes.Add(node);
                nodesTable.Add(node);

                if (node.ParentStageRowID.HasValue)
                {
                    hasChild = true;
                }
            }

            if (!hasChild)
            {
                return nodes;
            }

            foreach (var node in nodes)
            {
                if (node.ParentStageRowID.HasValue
                    && nodesTable.TryGetItem(node.ParentStageRowID.Value, out var parentNode))
                {
                    parentNode.Children.Add(node);
                }
            }

            foreach (var node in nodes)
            {
                if (node.Children.Count > 0)
                {
                    node.Children.Sort(CompareStageNodes);
                }
            }

            return nodes
                .Where(p => !p.ParentStageRowID.HasValue);
        }

        private static int CompareStageNodes(
            StageNode x,
            StageNode y)
        {
            var xNest = x.Row.Get<int>(KrConstants.KrStages.NestedOrder);
            var yNest = y.Row.Get<int>(KrConstants.KrStages.NestedOrder);
            var byNested = xNest.CompareTo(yNest);
            if (byNested != 0)
            {
                return byNested;
            }

            var xOrder = x.Row.Get<int>(KrConstants.KrStages.Order);
            var yOrder = y.Row.Get<int>(KrConstants.KrStages.Order);
            return xOrder.CompareTo(yOrder);
        }

        private static Card CreateFakeRepairableCard(
            Card? card,
            IDictionary<string, object?>? sectionsStorage)
        {
            Card fakeCard;
            if (card?.TypeID == DefaultCardTypes.KrStageTemplateTypeID)
            {
                fakeCard = new Card
                {
                    ID = fakeCardID,
                    TypeID = DefaultCardTypes.KrStageTemplateTypeID,
                    TypeName = DefaultCardTypes.KrStageTemplateTypeName,
                    TypeCaption = DefaultCardTypes.KrStageTemplateTypeName,
                };
            }
            else if (card?.TypeID == DefaultCardTypes.KrSecondaryProcessTypeID)
            {
                fakeCard = new Card
                {
                    ID = fakeCardID,
                    TypeID = DefaultCardTypes.KrSecondaryProcessTypeID,
                    TypeName = DefaultCardTypes.KrSecondaryProcessTypeName,
                    TypeCaption = DefaultCardTypes.KrSecondaryProcessTypeName,
                };
            }
            else
            {
                fakeCard = new Card
                {
                    ID = fakeCardID,
                    TypeID = DefaultCardTypes.KrCardTypeID,
                    TypeName = DefaultCardTypes.KrCardTypeName,
                    TypeCaption = DefaultCardTypes.KrCardTypeName,
                };
            }

            if (sectionsStorage is not null)
            {
                fakeCard.Sections =
                    new StringDictionaryStorage<CardSection>(sectionsStorage, CardComponentHelper.SectionFactory);
            }

            return fakeCard;
        }

        private static void PrepareUpdatedRow(
            CardRow updatedRow)
        {
            updatedRow.ClearChanges();

            var storage = updatedRow.GetStorage();
            storage.Remove(CardRow.SystemStateKey);
            storage.Remove(KrConstants.Keys.ParentStageRowID);
        }

        private static bool DictContainsSameRowID(
            object? dictObj,
            Guid rowID) =>
            dictObj is IDictionary<string, object?> dict
            && dict.TryGetValue(Names.Table_RowID, out var rowIDObj)
            && (rowIDObj is Guid rid || rowIDObj is string rids && Guid.TryParse(rids, out rid))
            && rid == rowID;

        private static void ProcessUpdatedRow(
            CardRow updatedRow,
            IList sectionRows)
        {
            switch (updatedRow.State)
            {
                case CardRowState.Inserted:
                    PrepareUpdatedRow(updatedRow);
                    sectionRows.Add(updatedRow.GetStorage());
                    break;
                case CardRowState.Modified:
                    PrepareUpdatedRow(updatedRow);
                    var sectionRow = sectionRows.Cast<object>()
                        .FirstOrDefault(p => DictContainsSameRowID(p, updatedRow.RowID));
                    if (sectionRow is IDictionary<string, object?> sectionRowStorage)
                    {
                        var settingsStorage = updatedRow.GetStorage();
                        StorageHelper.Merge(settingsStorage, sectionRowStorage);
                    }

                    break;

                case CardRowState.Deleted:
                    for (var i = sectionRows.Count - 1; i >= 0; i--)
                    {
                        if (DictContainsSameRowID(sectionRows[i], updatedRow.RowID))
                        {
                            sectionRows.RemoveAt(i);
                        }
                    }

                    break;
            }
        }

        private async Task FormatRowAsync(
            CardRow innerRow,
            Card innerCard,
            IDictionary<string, object?> settings,
            CancellationToken cancellationToken = default)
        {
            if (!innerRow.TryGetValue(KrConstants.KrStages.StageTypeID, out var stageTypeIDObj)
                || stageTypeIDObj is not Guid stageTypeID)
            {
                return;
            }

            var formatter = this.formatterContainer.ResolveFormatter(stageTypeID);
            var innerRowFields = innerRow.Fields;

            if (formatter is null)
            {
                innerRowFields[KrConstants.KrStages.DisplaySettings] = string.Empty;
                return;
            }

            var info = new Dictionary<string, object?>(StringComparer.Ordinal);
            var ctx = new StageTypeFormatterContext(
                this.session,
                info,
                innerCard,
                innerRow,
                settings,
                cancellationToken)
            {
                DisplayTimeLimit = innerRow.TryGet<string>(KrConstants.KrStages.DisplayTimeLimit) ?? string.Empty,
                DisplayParticipants = innerRow.TryGet<string>(KrConstants.KrStages.DisplayParticipants) ?? string.Empty,
                DisplaySettings = innerRow.TryGet<string>(KrConstants.KrStages.DisplaySettings) ?? string.Empty
            };
            await formatter.FormatServerAsync(ctx);

            innerRowFields[KrConstants.KrStages.DisplayTimeLimit] = ctx.DisplayTimeLimit;
            innerRowFields[KrConstants.KrStages.DisplayParticipants] = ctx.DisplayParticipants;
            innerRowFields[KrConstants.KrStages.DisplaySettings] = ctx.DisplaySettings;
        }

        /// <summary>
        /// Асинхронно переносит информацию об этапах в <paramref name="destSections"/>. При необходимости формирует информацию о позиции этапов.
        /// </summary>
        /// <param name="srcRows">Коллекция обрабатываемых строк - этапов.</param>
        /// <param name="destSections">Словарь секций карточки в которую выполняется запись информации.</param>
        /// <param name="stagePositions">Список содержащий информацию о позициях этапов.</param>
        /// <param name="predeserializedSettings">Словарь содержащий информацию о параметрах этапов.</param>
        /// <param name="hiddenStageMode"><inheritdoc cref="KrProcessSerializerHiddenStageMode" path="/summary"/></param>
        /// <param name="isCreateStagePositionInfo">Значение <see langword="true"/>, если для этапов должна быть сформирована информация о позиции, иначе - <see langword="false"/>.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        private async ValueTask ParseSettingsAsync(
            ListStorage<CardRow> srcRows,
            StringDictionaryStorage<CardSection> destSections,
            List<KrStagePositionInfo> stagePositions,
            IDictionary<Guid, IDictionary<string, object?>>? predeserializedSettings,
            KrProcessSerializerHiddenStageMode hiddenStageMode,
            bool isCreateStagePositionInfo,
            CancellationToken cancellationToken = default)
        {
            var descriptors = this.processContainer.GetHandlerDescriptors();
            var saveRows = hiddenStageMode == KrProcessSerializerHiddenStageMode.ConsiderWithStoringCardRows;
            var showOnlySkipStages = hiddenStageMode == KrProcessSerializerHiddenStageMode.ConsiderOnlySkippedStages;
            var visibleRows = destSections.GetOrAddTable(KrConstants.KrStages.Virtual).Rows; // Коллекция отображаемых строк - этапов.
            var shiftedOrder = 0;

            var nodes = CreateHierarchicalListOfNodes(srcRows);
            var nodeStack = new Stack<StageNode>(srcRows.Count);
            // Стек необходимо заполнять с конца массива, чтобы выстроить в правильном порядке
            foreach (var node in nodes.OrderByDescending(p => p.Row.Get<int>(KrConstants.KrStages.Order)))
            {
                nodeStack.Push(node);
            }

            // "Ступеньки" вложенности этапов
            // Верхний с индексом 0 - всегда -1, остальные это текущие этапы на определенном уровне
            var stairs = new int[srcRows.Count];
            while (nodeStack.Count != 0)
            {
                var node = nodeStack.Pop();
                stairs[node.Level] = node.Row.TryGet<int?>(KrConstants.KrStages.NestedOrder) ?? -1;
                if (node.Children.Count != 0)
                {
                    // Стек необходимо заполнять с конца массива, чтобы выстроить в правильном порядке
                    for (var i = node.Children.Count - 1; i >= 0; i--)
                    {
                        var child = node.Children[i];
                        child.Level = node.Level + 1;
                        nodeStack.Push(child);
                    }
                }

                var row = node.Row;
                var absoluteOrder = row.Get<int?>(KrConstants.KrStages.Order) ?? 0;
                var hidden = false;
                var skip = false;

                if (isCreateStagePositionInfo)
                {
                    // Определение этапа который должен быть скрыт из карточки.
                    hidden = row.TryGet<bool?>(KrConstants.KrStages.Hidden) == true
                        && KrProcessSharedHelper.CanBeHidden(row, descriptors);

                    skip = row.TryGet<bool>(KrConstants.KrStages.Skip)
                        && KrProcessSharedHelper.CanBeSkipped(row);

                    if ((hidden || !showOnlySkipStages && skip)
                        && hiddenStageMode != KrProcessSerializerHiddenStageMode.Ignore)
                    {
                        stagePositions.Add(
                            new KrStagePositionInfo(
                                row,
                                absoluteOrder,
                                null,
                                saveRows,
                                hidden,
                                skip));

                        continue;
                    }
                }

                // Создание отображаемой строки - этапа.
                var visibleRow = row.Clone();
                AppendLevelLabel(node, visibleRow, stairs);
                SetTreeStructureMark(node, visibleRow);
                visibleRow.Remove(KrConstants.KrStages.Info);
                visibleRow.Remove(KrConstants.KrStages.Settings);

                var stageStorage = predeserializedSettings is not null
                    && predeserializedSettings.TryGetValue(row.RowID, out var dict)
                    ? dict
                    : await this.DeserializeSettingsStorageAsync(row, false, cancellationToken);

                var serializerSettings = await this.GetSettingsAsync(cancellationToken);

                foreach (var settingsFieldName in serializerSettings.SettingsFieldNames)
                {
                    if (stageStorage.TryGetValue(settingsFieldName, out var fieldValue))
                    {
                        visibleRow[settingsFieldName] = fieldValue;
                    }
                }

                if (isCreateStagePositionInfo)
                {
                    stagePositions.Add(
                        new KrStagePositionInfo(
                            row,
                            absoluteOrder,
                            shiftedOrder,
                            false,
                            hidden,
                            skip));
                }

                visibleRow[KrConstants.KrStages.Order] = Int32Boxes.Box(shiftedOrder++);

                visibleRows.Add(visibleRow);

                // Перенос секций-настроек в destSections из stageStorage.
                foreach (var settingsSectionName in serializerSettings.SettingsSectionNames)
                {
                    if (!stageStorage.TryGetValue(settingsSectionName, out var srcSec)
                        || !(srcSec is IList sRows && srcSec is not byte[]))
                    {
                        continue;
                    }

                    var destSec = destSections.GetOrAddTable(settingsSectionName);

                    foreach (var srcRowObj in sRows)
                    {
                        if (srcRowObj is IDictionary<string, object?> srcRow)
                        {
                            var destRow = destSec.Rows.Add();
                            StorageHelper.Merge(srcRow, destRow.GetStorage());
                        }
                    }
                }
            }
        }

        #endregion
    }
}
