#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrCompilers;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrObjectModel
{
    /// <summary>
    /// Предоставляет информацию о этапе маршрута.
    /// </summary>
    public sealed class Stage :
        IEquatable<Stage>,
        ISealable
    {
        #region Nested Types

        private sealed class PerformerObjectComparer :
            IComparer<object>
        {
            #region IComparer<object> Members

            /// <inheritdoc />
            public int Compare(
                object? x,
                object? y)
            {
                var firstOrder = (x as IDictionary<string, object?>)?.TryGet(KrConstants.KrPerformersVirtual.Order, 0) ?? 0;
                var secondOrder = (y as IDictionary<string, object?>)?.TryGet(KrConstants.KrPerformersVirtual.Order, 0) ?? 0;
                return firstOrder.CompareTo(secondOrder);
            }

            #endregion

            #region Instance Static Property

            /// <doc path='info[@type="object" and @item="Instance"]'/>
            public static PerformerObjectComparer Instance { get; } = new PerformerObjectComparer();

            #endregion
        }

        #endregion

        #region Constants And Static Fields

        private const double DefaultTimeLimit = 1.0;

        private const double Epsilon = 0.01;

        private static readonly IStorageValueFactory<int, Performer> multiPerformerFactory =
            new DictionaryStorageValueFactory<int, Performer>((key, storage) => new MultiPerformer(storage));

        #endregion

        #region Fields

        private string name = string.Empty;
        private double? timeLimit;
        private DateTime? planned;
        private KrStageState state = KrStageState.Inactive;
        private int? templateStageOrder;
        private Guid? stageTypeID;
        private string? stageTypeCaption;

        private IDictionary<string, object?>? settingsStorage;
        private volatile bool settingsDynamicObjectIsInitialized;
        private dynamic? settingsDynamicObject;

        private IDictionary<string, object?>? infoStorage;
        private volatile bool infoDynamicObjectIsInitialized;
        private dynamic? infoDynamicObject;

        private bool hidden;
        private ListStorage<Performer>? performers;
        private AuthorProxy? author;
        private SinglePerformerProxy? performer;
        private bool skip;
        private bool canBeSkipped;
        private bool isGroupPositionUnspecified;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый пустой экземпляр класса <see cref="Stage"/>.
        /// </summary>
        public Stage()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Stage"/> на основе другого экземпляра. Запечатанность не переносится.
        /// </summary>
        /// <param name="stage">Объект, на основании которого выполняется инициализация.</param>
        public Stage(Stage stage)
        {
            ThrowIfNull(stage);

            this.TemplateID = stage.TemplateID;
            this.TemplateName = stage.TemplateName;
            this.GroupPosition = stage.GroupPosition;
            this.CanChangeOrder = stage.CanChangeOrder;
            this.TemplateOrder = stage.TemplateOrder;
            this.IsStageReadonly = stage.IsStageReadonly;

            this.RowID = stage.RowID;
            this.ID = stage.ID;

            this.StageTypeID = stage.StageTypeID;
            this.StageTypeCaption = stage.StageTypeCaption;

            this.StageGroupID = stage.StageGroupID;
            this.StageGroupName = stage.StageGroupName;
            this.StageGroupOrder = stage.StageGroupOrder;

            this.BasedOnTemplateStage = stage.BasedOnTemplateStage;
            this.Name = stage.Name;
            this.TimeLimit = stage.TimeLimit;
            this.Planned = stage.Planned;
            this.Hidden = stage.Hidden;
            this.State = stage.State;
            this.SqlPerformers = stage.SqlPerformers;
            this.SqlPerformersIndex = stage.SqlPerformersIndex;
            this.Skip = stage.Skip;
            this.CanBeSkipped = stage.CanBeSkipped;

            this.RowChanged = stage.RowChanged;
            this.OrderChanged = stage.OrderChanged;

            this.SettingsStorage = StorageHelper.Clone(stage.SettingsStorage);
            this.InfoStorage = StorageHelper.Clone(stage.InfoStorage);

            this.InitialStage = stage.InitialStage;
            this.Ancestor = stage.Ancestor;
            this.TemplateStageOrder = stage.TemplateStageOrder;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Stage"/>.
        /// </summary>
        /// <param name="name"><inheritdoc cref="Name" path="/summary"/></param>
        /// <param name="stageTypeID"><inheritdoc cref="StageTypeID" path="/summary"/></param>
        /// <param name="stageTypeCaption"><inheritdoc cref="StageTypeCaption" path="/summary"/></param>
        public Stage(
            string name,
            Guid stageTypeID,
            string stageTypeCaption)
            : this(
                  Guid.NewGuid(),
                  name,
                  stageTypeID,
                  stageTypeCaption,
                  Guid.Empty,
                  string.Empty,
                  0,
                  null,
                  null,
                  null,
                  false,
                  GroupPosition.Unspecified)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="Stage"/> с привязкой к шаблону этапов.
        /// </summary>
        /// <param name="id"><inheritdoc cref="ID" path="/summary"/></param>
        /// <param name="name"><inheritdoc cref="Name" path="/summary"/></param>
        /// <param name="stageTypeID"><inheritdoc cref="StageTypeID" path="/summary"/></param>
        /// <param name="stageTypeCaption"><inheritdoc cref="StageTypeCaption" path="/summary"/></param>
        /// <param name="stageGroupID"><inheritdoc cref="StageGroupID" path="/summary"/></param>
        /// <param name="stageGroupName"><inheritdoc cref="StageGroupName" path="/summary"/></param>
        /// <param name="stageGroupOrder"><inheritdoc cref="StageGroupOrder" path="/summary"/></param>
        /// <param name="templateID"><inheritdoc cref="TemplateID" path="/summary"/></param>
        /// <param name="templateName"><inheritdoc cref="TemplateName" path="/summary"/></param>
        /// <param name="templateOrder"><inheritdoc cref="TemplateOrder" path="/summary"/></param>
        /// <param name="canChangeOrder"><inheritdoc cref="CanChangeOrder" path="/summary"/></param>
        /// <param name="groupPosition"><inheritdoc cref="GroupPosition" path="/summary"/></param>
        /// <param name="ancestor"><inheritdoc cref="Ancestor" path="/summary"/></param>
        /// <param name="isStageReadonly"><inheritdoc cref="IsStageReadonly" path="/summary"/></param>
        /// <param name="timeLimit"><inheritdoc cref="TimeLimit" path="/summary"/></param>
        /// <param name="planned"><inheritdoc cref="Planned" path="/summary"/></param>
        /// <param name="hidden"><inheritdoc cref="Hidden" path="/summary"/></param>
        /// <param name="skip"><inheritdoc cref="Skip" path="/summary"/></param>
        /// <param name="canBeSkipped"><inheritdoc cref="CanBeSkipped" path="/summary"/></param>
        public Stage(
            Guid id,
            string name,
            Guid stageTypeID,
            string stageTypeCaption,
            Guid stageGroupID,
            string stageGroupName,
            int stageGroupOrder,
            Guid? templateID,
            string? templateName,
            int? templateOrder,
            bool canChangeOrder,
            GroupPosition groupPosition,
            Stage? ancestor = null,
            bool isStageReadonly = true,
            int timeLimit = 1,
            DateTime? planned = null,
            bool hidden = false,
            bool skip = false,
            bool canBeSkipped = false)
        {
            this.RowID = id;
            this.ID = id;
            this.Name = name;

            this.TemplateID = templateID;
            this.TemplateName = templateName;
            this.TemplateOrder = templateOrder;
            this.TemplateStageOrder = 0;

            this.StageTypeID = stageTypeID;
            this.StageTypeCaption = NotEmptyOrThrow(stageTypeCaption);

            this.StageGroupID = stageGroupID;
            this.StageGroupName = NotNullOrThrow(stageGroupName);
            this.StageGroupOrder = stageGroupOrder;

            this.CanChangeOrder = canChangeOrder;
            this.GroupPosition = groupPosition;
            this.Ancestor = ancestor;
            this.IsStageReadonly = isStageReadonly;
            this.TimeLimit = timeLimit;
            this.Planned = planned;
            this.Hidden = hidden;
            this.Skip = skip;
            this.CanBeSkipped = canBeSkipped;

            this.SettingsStorage = new Dictionary<string, object?>(StringComparer.Ordinal);
            this.InfoStorage = new Dictionary<string, object?>(StringComparer.Ordinal);

            this.BasedOnTemplateStage = false;
            this.InitialStage = false;
            this.SqlPerformers = string.Empty;
            this.SqlPerformersIndex = null;

            this.RowChanged = false;
            this.OrderChanged = false;
        }

        #endregion

        #region Create Instance Methods

        /// <summary>
        /// Создаёт новый экземпляр класса <see cref="Stage"/> по шаблону этапов (<paramref name="templateStage"/>).
        /// </summary>
        /// <param name="stageGroup">Группа этапов с которой связан <paramref name="stageTemplate"/>.</param>
        /// <param name="stageTemplate"><inheritdoc cref="IKrStageTemplate" path="/summary"/></param>
        /// <param name="templateStage">Объект, содержащий информацию об этапе из шаблона этапов <paramref name="stageTemplate"/>.</param>
        /// <param name="initialStage"><inheritdoc cref="InitialStage" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Новый экземпляр класса <see cref="Stage"/>.</returns>
        public static async ValueTask<Stage> CreateFromStageTemplateAsync(
            IKrStageGroup stageGroup,
            IKrStageTemplate stageTemplate,
            IKrRuntimeStage templateStage,
            bool initialStage = false,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(stageGroup);
            ThrowIfNull(templateStage);
            ThrowIfNull(stageTemplate);

            // При создании этапа по шаблону группа этапов должна соответствовать значению указанному в шаблоне этапов.
            if (templateStage.TemplateID != stageTemplate.ID)
            {
                throw new ArgumentException(
                    $"The identifier of the stage template does not match the identifier specified in the template stage."
                    + $" {nameof(templateStage)}.{nameof(templateStage.TemplateID)}={templateStage.TemplateID:B}."
                    + $" {nameof(stageTemplate)}.{nameof(stageTemplate.ID)}={stageTemplate.ID:B}.");
            }

            if (stageGroup.ID != stageTemplate.StageGroupID)
            {
                throw new ArgumentException(
                    $"The identifier of the stages template does not match the identifier specified in the stage group."
                    + $" {nameof(stageGroup)}.{nameof(stageGroup.ID)}={stageGroup.ID:B}."
                    + $" {nameof(stageTemplate)}.{nameof(stageTemplate.StageGroupID)}={stageTemplate.StageGroupID:B}.");
            }

            var stage = new Stage();
            stage.FillStageProperties(templateStage);
            stage.InitialStage = initialStage;

            // Создавая по IKrRuntimeStage не будет объекта-предшественника.
            stage.Ancestor = null;

            stage.SettingsStorage = await templateStage.GetSettingsAsync(cancellationToken);
            stage.InfoStorage = new Dictionary<string, object?>(StringComparer.Ordinal);

            // Полное создание по шаблону.
            // StageRow - строка из карточки KrStageTemplates
            stage.ID = templateStage.StageID;
            stage.BasedOnTemplateStage = true;
            // Только создаем строку - ID для новой строки в документе
            stage.RowID = Guid.NewGuid();

            stage.SqlPerformersIndex = stage.GetSqlPerformersIndex();
            stage.RemoveSqlApproverRole();
            stage.SqlPerformers = templateStage.SqlRoles;

            stage.TemplateID = stageTemplate.ID;
            stage.TemplateName = stageTemplate.Name;
            stage.TemplateOrder = stageTemplate.Order;
            stage.GroupPosition = stageTemplate.Position;

            stage.SetPermissionsFlags(
                stageGroup,
                stageTemplate);

            stage.TemplateStageOrder = templateStage.Order;

            return stage;
        }

        /// <summary>
        /// Создаёт новый экземпляр класса <see cref="Stage"/> по строке, содержащей информацию о этапе.
        /// </summary>
        /// <param name="stageRow">Строковое представление этапа.</param>
        /// <param name="krStageSerializer"><inheritdoc cref="IKrStageSerializer" path="/summary"/></param>
        /// <param name="stageGroup">Группа этапов с идентификатором <see cref="KrConstants.KrStages.StageGroupID"/> или значение <see langword="null"/>, если группа этапов не найдена.</param>
        /// <param name="stageTemplate">Шаблон этапов с идентификатором <see cref="KrConstants.KrStages.BasedOnStageTemplateID"/> или значение <see langword="null"/>, если шаблон этапов не найден или значение заданное по ключу <see cref="KrConstants.KrStages.BasedOnStageTemplateID"/> равно <see langword="null"/>.</param>
        /// <param name="templateStages">Коллекция этапов, содержащихся в шаблоне этапов <paramref name="stageTemplate"/>, или пустая коллекция, если параметр <paramref name="stageTemplate"/> не задан.</param>
        /// <param name="initialStage"><inheritdoc cref="InitialStage" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Новый экземпляр класса <see cref="Stage"/>.</returns>
        public static async ValueTask<Stage> CreateFromStageRowAsync(
            CardRow stageRow,
            IKrStageSerializer krStageSerializer,
            IKrStageGroup? stageGroup,
            IKrStageTemplate? stageTemplate,
            IReadOnlyCollection<IKrRuntimeStage> templateStages,
            bool initialStage = false,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(stageRow);
            ThrowIfNull(krStageSerializer);
            ThrowIfNull(templateStages);

            // Группа этапов определяется по значению из строки этапа, т.к. шаблона этапов может не быть.
            var stageGroupID = stageRow.Get<Guid>(KrConstants.KrStages.StageGroupID);

            if (stageGroup is not null
                && stageGroup.ID != stageGroupID)
            {
                throw new ArgumentException(
                    $"The identifier of the stage group in stage row does not match the identifier specified in the stage group."
                    + $" {nameof(stageGroup)}.{nameof(stageGroup.ID)}={stageGroup.ID:B}."
                    + $" {KrConstants.KrStages.StageGroupID}={stageGroupID:B}.",
                    nameof(stageGroup));
            }

            var stage = new Stage();

            stage.StageGroupID = stageGroupID;
            stage.StageGroupName = stageRow.Get<string>(KrConstants.KrStages.StageGroupName) ?? string.Empty;
            stage.StageGroupOrder = stageRow.Get<int>(KrConstants.KrStages.StageGroupOrder);
            stage.State = (KrStageState) stageRow.TryGet<int>(KrConstants.KrStages.StateID);
            stage.Name = stageRow.Get<string>(KrConstants.KrStages.NameField) ?? string.Empty;
            stage.TimeLimit = stageRow.TryGet<object>(KrConstants.KrStages.TimeLimit) as double?;
            stage.Planned = stageRow.TryGet<object>(KrConstants.KrStages.Planned) as DateTime?;
            stage.Hidden = stageRow.TryGet<bool?>(KrConstants.KrStages.Hidden) ?? false;
            stage.Skip = stageRow.TryGet<bool?>(KrConstants.KrStages.Skip) ?? false;
            stage.CanBeSkipped = stageRow.TryGet<bool?>(KrConstants.KrStages.CanBeSkipped) ?? false;

            stage.RowChanged = stageRow.Get<bool>(KrConstants.KrStages.RowChanged);
            stage.OrderChanged = stageRow.Get<bool>(KrConstants.KrStages.OrderChanged);

            stage.StageTypeID = stageRow.Get<Guid?>(KrConstants.KrStages.StageTypeID);
            stage.StageTypeCaption = stageRow.Get<string>(KrConstants.KrStages.StageTypeCaption);

            stage.InitialStage = initialStage;

            // Создавая по CardRow не будет объекта-предшественника.
            stage.Ancestor = null;

            stage.SettingsStorage = await krStageSerializer.DeserializeSettingsStorageAsync(
                stageRow,
                cancellationToken: cancellationToken);

            stage.InfoStorage = GetInfoStorage(
                krStageSerializer,
                stageRow);

            var basedOnStageTemplateID = stageRow.TryGet<Guid?>(KrConstants.KrStages.BasedOnStageTemplateID);
            var basedOnStageRowID = stageRow.TryGet<Guid?>(KrConstants.KrStages.BasedOnStageRowID);

            if (basedOnStageTemplateID.HasValue
                && stageTemplate is not null)
            {
                if (stageTemplate.ID != basedOnStageTemplateID.Value)
                {
                    throw new ArgumentException(
                        $"The identifiers of the stages template (ID={stageTemplate.ID:B}) does not match the value specified in key {KrConstants.KrStages.BasedOnStageTemplateID}={basedOnStageTemplateID.Value:B}.",
                        nameof(stageTemplate));
                }

                // Флаг, показывающий, что строка этапа была создана по шаблону.
                var stageFromTemplate = false;

                // Поиск этапа, использованного в качестве шаблона для создания этого этапа.
                var stagePrototype = templateStages.FirstOrDefault(p =>
                    p.StageID == stageRow.RowID);

                // RowID строки текущего этапа соответствует RowID строки этапа в шаблоне этапов?
                // stageRow - строка из карточки KrStageTemplates.
                // Следовательно, это создание этапа по шаблону.
                if (stagePrototype is not null)
                {
                    stage.ID = stageRow.RowID;

                    // Только создаем строку - ID для новой строки в документе
                    stage.RowID = Guid.NewGuid();
                    stage.BasedOnTemplateStage = true;

                    stageFromTemplate = true;
                }
                else if (basedOnStageRowID.HasValue)
                {
                    // Этап в карточке был ранее создан подстановкой из таблицы этапов в шаблоне
                    // в таблицу этапов карточки
                    stage.ID = basedOnStageRowID.Value;

                    // ID для строки в документе
                    stage.RowID = stageRow.RowID;
                    stage.BasedOnTemplateStage = true;

                    // Значение null будет, если в карточке шаблона этапов больше не содержится этап использованный в качестве шаблона для создания этого этапа.
                    stagePrototype = templateStages.FirstOrDefault(p =>
                        p.StageID == basedOnStageRowID.Value);
                }
                else // basedOnStageTemplateID.HasValue
                {
                    // Этап был создан с привязкой к карточке,
                    // но без привязки к конкретному этапу в шаблоне (этап был добавлен вручную).
                    stage.ID = stageRow.RowID;
                    stage.RowID = stageRow.RowID;
                    stage.BasedOnTemplateStage = false;
                }

                stage.SqlPerformersIndex = stage.GetSqlPerformersIndex();
                stage.RemoveSqlApproverRole();
                stage.SqlPerformers = stagePrototype?.SqlRoles ?? string.Empty;

                stage.TemplateID = stageTemplate.ID;
                stage.TemplateName = stageFromTemplate
                    ? stageTemplate.Name
                    : stageRow.Fields.TryGet<string>(KrConstants.KrStages.BasedOnStageTemplateName);
                stage.TemplateOrder = stageFromTemplate
                    ? stageTemplate.Order
                    : stageRow.Fields.TryGet<int?>(KrConstants.KrStages.BasedOnStageTemplateOrder);
                stage.GroupPosition = stageFromTemplate
                    ? stageTemplate.Position
                    : GroupPosition.GetByID(stageRow.Fields.TryGet<int?>(KrConstants.KrStages.BasedOnStageTemplateGroupPositionID));

                stage.SetPermissionsFlags(
                    stageGroup,
                    stageTemplate);

                if (stageFromTemplate)
                {
                    stage.TemplateStageOrder = stagePrototype!.Order;
                }
            }
            else
            {
                // stageTemplate == null - этап ручной или шаблон удален.
                stage.ID = stageRow.RowID;
                stage.RowID = stageRow.RowID;
                stage.BasedOnTemplateStage = false;
                stage.TemplateID = null;
                stage.TemplateName = null;
                stage.TemplateOrder = null;
                stage.TemplateStageOrder = null;
                stage.GroupPosition = GroupPosition.Unspecified;
                stage.CanChangeOrder = true;
                stage.IsStageReadonly = false;

                if (basedOnStageTemplateID.HasValue)
                {
                    // Этап был создан по шаблону, но сам шаблон уже удален.
                    if (basedOnStageRowID.HasValue)
                    {
                        // Это этап из таблицы карточки шаблона этапов, которая была удалена.
                        stage.ID = basedOnStageRowID.Value;
                        stage.BasedOnTemplateStage = true;
                    }

                    stage.TemplateID = basedOnStageTemplateID;
                    stage.TemplateName = stageRow.TryGet<string>(KrConstants.KrStages.BasedOnStageTemplateName);
                }
            }

            return stage;
        }

        #endregion

        #region Properties

        #region Template properties

        /// <summary>
        /// Идентификатор шаблона этапов или значение <see langword="null"/>, если этап не связан с шаблоном этапов.
        /// </summary>
        public Guid? TemplateID { get; private set; }

        /// <summary>
        /// Название шаблона этапов или значение <see langword="null"/>, если этап не связан с шаблоном этапов.
        /// </summary>
        public string? TemplateName { get; private set; }

        /// <inheritdoc cref="Shared.Workflow.KrCompilers.GroupPosition" path="/summary"/>
        public GroupPosition GroupPosition { get; private set; } = GroupPosition.Unspecified;

        /// <summary>
        /// Значение, показывающее, может ли пользователь менять порядок текущего этапа.
        /// </summary>
        /// <remarks>
        /// Если изменение порядка запрещено, то для этапов с <see cref="GroupPosition"/> = <see cref="GroupPosition.AtFirst"/> этап будет расположен перед теми, для которых разрешено менять порядок; для этапов с <see cref="GroupPosition"/> = <see cref="GroupPosition.AtLast"/> этапы, для которых разрешено менять порядок, будут выше, чем строго зафиксированные.
        /// </remarks>
        public bool CanChangeOrder { get; private set; }

        /// <summary>
        /// Порядок этапа в шаблоне этапов или значение поля <see cref="KrConstants.KrStages.BasedOnStageTemplateOrder"/> или значение <see langword="null"/>, если порядок этапа был изменён или если этап больше не связан с шаблоном этапов.
        /// </summary>
        public int? TemplateOrder { get; private set; }

        /// <summary>
        /// Значение, показывающее, может ли пользователь редактировать этап.
        /// </summary>
        public bool IsStageReadonly { get; private set; }

        #endregion

        #region Stage properties

        /// <summary>
        /// Идентификатор строки этапа (<see cref="CardRow.RowID"/>) в конкретном документе.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Если этап создан по шаблону, то данное свойство равно новому идентификатору под которым строка этапа будет сохранена в документе.</item>
        /// <item>Во всех остальных случаях здесь содержится идентификатор строки этапа из карточки документа.</item>
        /// </list>
        /// </remarks>
        public Guid RowID { get; private set; }

        /// <summary>
        /// Идентификатор этапа.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Если этап создан по шаблону, то данное свойство равно идентификатору строки (<see cref="CardRow.RowID"/>) этапа из карточки шаблона этапов.</item>
        /// <item>Если этап создан вручную, то данное свойство равно идентификатору строки (<see cref="CardRow.RowID"/>) из карточки документа.</item>
        /// </list>
        /// <para/>
        /// </remarks>
        public Guid ID { get; private set; }

        /// <summary>
        /// Идентификатор группы этапов, к которой принадлежит этап.
        /// </summary>
        public Guid StageGroupID { get; private set; }

        /// <summary>
        /// Название группы этапов, к которой принадлежит этап.
        /// </summary>
        public string StageGroupName { get; private set; } = string.Empty;

        /// <summary>
        /// Порядок сортировки группы этапов, к которой принадлежит этап.
        /// </summary>
        public int StageGroupOrder { get; private set; }

        /// <summary>
        /// Значение, показывающее, что этап добавлен из шаблона этапов - задан шаблон этапов.
        /// </summary>
        [JsonIgnore]
        public bool BasedOnTemplate => this.TemplateID.HasValue;

        /// <summary>
        /// Значение, показывающее, что этап добавлен из шаблона этапов - задан идентификатор строки шаблона этапов из карточки шаблона этапов.
        /// </summary>
        public bool BasedOnTemplateStage { get; private set; }

        /// <summary>
        /// Название этапа.
        /// </summary>
        public string Name
        {
            get => this.name;
            set
            {
                ThrowIfSealed(this);
                this.name = NotEmptyOrThrow(value);
            }
        }

        /// <inheritdoc cref="KrStageState" path="/summary"/>
        /// <remarks>
        /// Значение актуально только при выполнении процесса.
        /// </remarks>
        public KrStageState State
        {
            get => this.state;
            set
            {
                ThrowIfSealed(this);
                this.state = value;
            }
        }

        /// <summary>
        /// Срок (рабочие дни) или значение <see langword="null"/>, если значение не задано.
        /// </summary>
        public double? TimeLimit
        {
            get => this.timeLimit;
            set
            {
                ThrowIfSealed(this);
                this.timeLimit = value;

                if (value.HasValue)
                {
                    this.planned = null;
                }
            }
        }

        /// <summary>
        /// Срок (рабочие дни), если указан, иначе стандартное значение <see cref="DefaultTimeLimit"/>.
        /// </summary>
        [JsonIgnore]
        public double TimeLimitOrDefault => this.TimeLimit ?? DefaultTimeLimit;

        /// <summary>
        /// Дата выполнения или значение <see langword="null"/>, если значение не задано.
        /// </summary>
        public DateTime? Planned
        {
            get => this.planned;
            set
            {
                ThrowIfSealed(this);
                this.planned = value;

                if (value.HasValue)
                {
                    this.timeLimit = null;
                }
            }
        }

        /// <summary>
        /// Значение, показывающее, что этап является скрытым.
        /// </summary>
        public bool Hidden
        {
            get => this.hidden;
            set
            {
                ThrowIfSealed(this);
                this.hidden = value;
            }
        }

        /// <summary>
        /// Запрос на получение SQL-согласующих.
        /// </summary>
        public string SqlPerformers { get; private set; } = string.Empty;

        /// <summary>
        /// Положение SQL-согласующих.
        /// </summary>
        /// <remarks>
        /// Значение задаётся при создании на основе строки карточки с указанием шаблона этапов.<para/>
        ///
        /// Не показывает куда были подставлены SQL согласующие за предыдущий пересчет. Чтобы это узнать, нужно найти индекс первого согласующего с флагом <see cref="KrConstants.KrPerformersVirtual.SQLApprover"/> = <see langword="true"/>, однако если этап изменялся вручную (<see cref="RowChanged"/> = <see langword="true"/>), о предыдущей подстановке SQL согласующих делать выводы нельзя.
        /// </remarks>
        public int? SqlPerformersIndex { get; private set; }

        /// <summary>
        /// Признак того, что порядок менялся пользователем.
        /// </summary>
        /// <remarks>Не зависит от изменения порядка в коде.</remarks>
        public bool OrderChanged { get; private set; }

        /// <summary>
        /// Признак того, что этап менялся пользователем.
        /// </summary>
        /// <remarks>Не зависит от изменений параметров в коде.</remarks>
        public bool RowChanged { get; private set; }

        /// <summary>
        /// Порядок сортировки этапа в рамках шаблона этапов или значение <see langword="null"/>, если этап ручной или шаблон этапов удалён.
        /// </summary>
        /// <remarks>
        /// Значение заполняется и используется только при построении маршрута.<para/>
        /// Значение используется для определения изменений в дочернем маршруте из конкретного шаблона этапа. Например, если в шаблон был добавлен еще один этап на первое место, при построении этот этап необходимо поместить также выше.
        /// </remarks>
        public int? TemplateStageOrder
        {
            get => this.templateStageOrder;
            set
            {
                ThrowIfSealed(this);
                this.templateStageOrder = value;
            }
        }

        /// <summary>
        /// Идентификатор типа этапа.
        /// </summary>
        public Guid? StageTypeID
        {
            get => this.stageTypeID;
            set
            {
                ThrowIfSealed(this);
                this.stageTypeID = value;
            }
        }

        /// <summary>
        /// Отображаемое имя типа этапа.
        /// </summary>
        public string? StageTypeCaption
        {
            get => this.stageTypeCaption;
            set
            {
                ThrowIfSealed(this);
                this.stageTypeCaption = value;
            }
        }

        /// <summary>
        /// Параметры этапа.
        /// </summary>
        public IDictionary<string, object?> SettingsStorage
        {
            get => this.settingsStorage ??= new Dictionary<string, object?>(StringComparer.Ordinal);
            set
            {
                ThrowIfSealed(this);
                this.settingsStorage = NotNullOrThrow(value);
                this.settingsDynamicObjectIsInitialized = false;

                this.performer = null;
                this.performers = null;
                this.author = null;
            }
        }

        /// <summary>
        /// Dynamic-обёртка над <see cref="SettingsStorage"/>.
        /// </summary>
        [JsonIgnore]
        public dynamic Settings
        {
            get
            {
                if (this.settingsDynamicObjectIsInitialized)
                {
                    return this.settingsDynamicObject!;
                }

                // если объект в кэше и к нему идут обращения из нескольких потоков, то сначала надо проставить
                // свойство с dynamic-ом, и уже потом - флажок на инициализацию
                var result = this.settingsDynamicObject = DynamicStorageAccessor.Create(this.SettingsStorage);
                this.settingsDynamicObjectIsInitialized = true;

                return result;
            }
        }

        /// <summary>
        /// Дополнительная информация о этапе.
        /// </summary>
        public IDictionary<string, object?> InfoStorage
        {
            get => this.infoStorage ??= new Dictionary<string, object?>(StringComparer.Ordinal);
            set
            {
                ThrowIfSealed(this);
                this.infoStorage = NotNullOrThrow(value);
                this.infoDynamicObjectIsInitialized = false;
            }
        }

        /// <summary>
        /// Dynamic-обёртка над <see cref="InfoStorage"/>.
        /// </summary>
        [JsonIgnore]
        public dynamic Info
        {
            get
            {
                if (this.infoDynamicObjectIsInitialized)
                {
                    return this.infoDynamicObject!;
                }

                // если объект в кэше и к нему идут обращения из нескольких потоков, то сначала надо проставить
                // свойство с dynamic-ом, и уже потом - флажок на инициализацию
                var result = this.infoDynamicObject = DynamicStorageAccessor.Create(this.InfoStorage);
                this.infoDynamicObjectIsInitialized = true;

                return result;
            }
        }

        /// <summary>
        /// Исполнитель текущего этапа или значение <see langword="null"/>, если он не задан.
        /// </summary>
        /// <remarks>
        /// Значение актуально только для режима <see cref="PerformerUsageMode.Single"/>.
        /// </remarks>
        [JsonIgnore]
        public Performer? Performer
        {
            get
            {
                return this.SettingsStorage.TryGet<Guid?>(KrConstants.KrSinglePerformerVirtual.PerformerID).HasValue
                    ? (this.performer ??= new SinglePerformerProxy(this.SettingsStorage))
                    : null;
            }
            set
            {
                ThrowIfSealed(this);

                if (value is not null)
                {
                    this.SettingsStorage[KrConstants.KrSinglePerformerVirtual.PerformerID] = value.PerformerID;
                    this.SettingsStorage[KrConstants.KrSinglePerformerVirtual.PerformerName] = value.PerformerName;
                }
                else
                {
                    this.SettingsStorage[KrConstants.KrSinglePerformerVirtual.PerformerID] = null;
                    this.SettingsStorage[KrConstants.KrSinglePerformerVirtual.PerformerName] = null;
                }
            }
        }

        /// <summary>
        /// Список исполнителей текущего этапа.
        /// </summary>
        /// <remarks>
        /// Значение актуально только для режима <see cref="PerformerUsageMode.Multiple"/>.
        /// </remarks>
        [JsonIgnore]
        public ListStorage<Performer> Performers
        {
            get
            {
                if (this.performers is null)
                {
                    if (!this.SettingsStorage.TryGetValue(KrConstants.KrPerformersVirtual.Synthetic, out var kpvObj)
                        || kpvObj is not IList kvp)
                    {
                        kvp = new List<object>();
                    }
                    else
                    {
                        kvp = kvp
                            .Cast<object>()
                            .OrderBy(static x => x, PerformerObjectComparer.Instance)
                            .ToList();
                    }

                    this.SettingsStorage[KrConstants.KrPerformersVirtual.Synthetic] = kvp;

                    this.performers = new ListStorage<Performer>(kvp, multiPerformerFactory);
                }

                return this.performers;
            }
        }

        /// <summary>
        /// Автор этапа или значение <see langword="null"/>, если он не задан. Переопределяет автора заданного в параметрах этапа.
        /// </summary>
        [JsonIgnore]
        public Author? Author
        {
            get
            {
                return this.SettingsStorage.TryGet<Guid?>(KrConstants.KrAuthorSettingsVirtual.AuthorID).HasValue
                    ? (this.author ??= new AuthorProxy(this.SettingsStorage))
                    : null;
            }
            set
            {
                ThrowIfSealed(this);

                if (value is null)
                {
                    this.SettingsStorage[KrConstants.KrAuthorSettingsVirtual.AuthorID] = null;
                    this.SettingsStorage[KrConstants.KrAuthorSettingsVirtual.AuthorName] = null;
                }
                else
                {
                    this.SettingsStorage[KrConstants.KrAuthorSettingsVirtual.AuthorID] = value.AuthorID;
                    this.SettingsStorage[KrConstants.KrAuthorSettingsVirtual.AuthorName] = value.AuthorName;
                }
            }
        }

        /// <summary>
        /// Значение, определяющее, в каком объеме информация о заданиях будет указываться по ключу <see cref="KrConstants.Keys.Tasks"/> в <see cref="InfoStorage"/>. Если указано <see langword="true"/> - информация будет полной, включая карточку задания. Иначе перед записью будут удалены некоторые поля.
        /// </summary>
        /// <remarks>Является оберткой над флагом, расположенным в <see cref="InfoStorage"/> по ключу, равному названию свойства. Отсутствие значения в <see cref="InfoStorage"/> трактуется как <see langword="false"/>.</remarks>
        /// <seealso cref="KrProcess.Workflow.Handlers.HandlerHelper.AppendToCompletedTasksWithPreparing"/>
        [JsonIgnore]
        public bool WriteTaskFullInformation
        {
            get => this.InfoStorage.TryGet<bool>(nameof(this.WriteTaskFullInformation));
            set => this.InfoStorage[nameof(this.WriteTaskFullInformation)] = BooleanBoxes.Box(value);
        }

        /// <summary>
        /// Признак пропуска этапа.
        /// </summary>
        public bool Skip
        {
            get => this.skip;
            set
            {
                ThrowIfSealed(this);
                this.skip = value;
            }
        }

        /// <summary>
        /// Значение, показывающее, разрешен ли пропуск этапа.
        /// </summary>
        public bool CanBeSkipped
        {
            get => this.canBeSkipped;
            set
            {
                ThrowIfSealed(this);
                this.canBeSkipped = value;
            }
        }

        /// <summary>
        /// Признак того, что значения <see cref="GroupPosition"/>, <see cref="TemplateOrder"/> и <see cref="TemplateStageOrder"/> не учитываются при сортировке этапов.
        /// </summary>
        [JsonIgnore]
        public bool IsPositionUnspecified
        {
            get => this.isGroupPositionUnspecified;
            set
            {
                ThrowIfSealed(this);
                this.isGroupPositionUnspecified = value;
            }
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Объект создан при первичном построении исходного маршрута.
        /// </summary>
        internal bool InitialStage { get; set; }

        /// <summary>
        /// Этап который был изначально в маршруте вместо текущего этапа или значение <see langword="null"/>, если такого этапа не было.
        /// </summary>
        internal Stage? Ancestor { get; private set; }

        /// <summary>
        /// Сообщение runner-у о том, что необходимо для данного этапа
        /// попытаться переключить контекст на указанную карточку.
        /// </summary>
        internal Guid? ChangeContextToCardID { get; set; }

        /// <summary>
        /// Признак того, что при обработке <see cref="ChangeContextToCardID"/>
        /// обработка будет переключена на всю группу.
        /// </summary>
        internal bool ChangeContextWholeGroupToDifferentCard { get; set; }

        /// <summary>
        /// Дополнительная пользовательская информация для процесса, который будет создан при переключении контекста.
        /// </summary>
        internal IDictionary<string, object?>? ChangeContextProcessInfo { get; set; }

        #endregion

        #endregion

        #region Public Methods

        /// <summary>
        /// Переносит служебную информацию из указанного этапа в этот экземпляр.
        /// </summary>
        /// <param name="stage">Этап, из которого переносится информация.</param>
        /// <remarks>
        /// При пересчете, когда имеются новая и старая версия этапа,
        /// нужно сохранить информацию о том, как пользователь воздействовал на этап,
        /// а также актуализировать поле <see cref="GroupPosition"/> для корректной сортировки этапов.
        /// Помимо этого переносятся SQL-согласующие, поскольку иначе информация о них будет утеряна.
        /// Переносить SQL согласующих нужно для определения изменений в выборке SQL согласующих.
        /// </remarks>
        public void Inherit(Stage stage)
        {
            ThrowIfNull(stage);
            ThrowIfSealed(this);

            this.RowID = stage.RowID;
            this.State = stage.State;
            StorageHelper.Merge(stage.InfoStorage, this.InfoStorage);

            if (stage.CanChangeOrder
                && stage.GroupPosition == GroupPosition.Unspecified)
            {
                this.GroupPosition = GroupPosition.Unspecified;
                this.TemplateOrder = null;
                this.TemplateStageOrder = null;
            }

            this.CanChangeOrder = stage.CanChangeOrder;
            this.IsStageReadonly = stage.IsStageReadonly;

            this.RowChanged = stage.RowChanged;
            this.OrderChanged = stage.OrderChanged;

            this.Ancestor = null;
            if (stage.Ancestor?.InitialStage == true)
            {
                this.Ancestor = stage.Ancestor;
            }
            else if (stage.Ancestor is null && stage.InitialStage)
            {
                this.Ancestor = stage;
            }

            this.Skip = stage.Skip;
            if (this.Skip)
            {
                this.Hidden = stage.Hidden;
            }
        }

        /// <summary>
        /// Переносит информацию о положении этапа из указанного этапа.
        /// </summary>
        /// <param name="stage">Этап, из которого переносится информация.</param>
        public void InheritPosition(Stage stage)
        {
            ThrowIfNull(stage);
            ThrowIfSealed(this);

            this.GroupPosition = stage.GroupPosition;
            this.TemplateOrder = stage.TemplateOrder;
            this.TemplateStageOrder = stage.TemplateStageOrder;
        }

        /// <summary>
        /// Возвращает значение, показывающее, что изменилась дополнительная информация этапа (<see cref="InfoStorage"/>).
        /// </summary>
        /// <param name="currentStageFromThePast">Этап, содержащий дополнительную информацию (<see cref="Stage.InfoStorage"/>), с которой выполняется сравнение.</param>
        /// <returns>Значение <see langword="true"/>, если изменилась пользовательская информация внутри этапа, иначе - <see langword="false"/>.</returns>
        public bool IsInfoChanged(Stage currentStageFromThePast)
        {
            ThrowIfNull(currentStageFromThePast);

            if (currentStageFromThePast.ID != this.ID)
            {
                throw new ArgumentException("Can compare only with the same stage.", nameof(currentStageFromThePast));
            }

            return !StorageHelper.Equals(this.InfoStorage, currentStageFromThePast.InfoStorage);
        }

        #endregion

        #region AutomaticallyChangedProperties

        /// <summary>
        /// Добавляет указанное свойство в список автоматически изменённых значений этапа.
        /// </summary>
        /// <param name="name">Имя добавляемого значения.</param>
        /// <remarks>Не выполняет действий, если указанное значение содержится в списке.</remarks>
        public void AddAutomaticallyChangedValue(
            string name)
        {
            ThrowIfNullOrWhiteSpace(name);

            var list = this.GetAutomaticallyChangedValues();

            if (!list.Contains(name))
            {
                list.Add(name);
            }
        }

        /// <summary>
        /// Удаляет указанное свойство из списка автоматически изменённых значений этапа.
        /// </summary>
        /// <param name="name">Имя удаляемого значения.</param>
        public void RemoveAutomaticallyChangedValue(
            string name)
        {
            var list = this.GetAutomaticallyChangedValues();
            list.Remove(name);
        }

        /// <summary>
        /// Очищает список автоматически изменённых значений этапа.
        /// </summary>
        public void ClearAutomaticallyChangedValues() =>
            this.InfoStorage.Remove(KrConstants.Keys.AutomaticallyChangedValues);

        /// <summary>
        /// Сравнивает этап по значимым полям с учётом автоматически изменённых значений.
        /// </summary>
        /// <param name="other">Объект, с которым выполняется сравнение.</param>
        /// <returns>Значение <see langword="true"/>, если объекты равны, иначе - <see langword="false"/>.</returns>
        /// <remarks>
        /// Не выполняет сравнение <see cref="InfoStorage"/>. Для его сравнения используйте метод <see cref="IsInfoChanged(Stage)"/>.<para/>
        /// Автоматически изменённое значение - это значение изменённое при обработке этапа в обработчике из-за наличия соответствующего параметра. Его изменение не оказывает влияния при определении равенства этапов с помощью этого метода. Пример автоматически изменяемого значения: свойство <see cref="Hidden"/> в этапе "Доработка" (<see cref="KrProcess.Workflow.Handlers.EditStageTypeHandler"/>) при изменении его в соответствии с параметром "Управлять видимостью этапа" (<see cref="KrConstants.KrEditSettingsVirtual.ManageStageVisibility"/>).
        /// </remarks>
        public bool EqualsWithAutomaticallyChangedValues(
            Stage? other)
        {
            var automaticallyChangedProperties = this.TryGetAutomaticallyChangedValues();
            return this.EqualsWithAutomaticallyChangedValuesCore(other, automaticallyChangedProperties);
        }

        #endregion

        #region Operators

        /// <summary>
        /// Сравнивает два экземпляра объектов типа <see cref="Stage"/>.
        /// </summary>
        /// <param name="left">Первый объект.</param>
        /// <param name="right">Второй объект.</param>
        /// <returns>Значение <see langword="true"/>, если объекты равны, иначе - <see langword="false"/>.</returns>
        public static bool operator ==(Stage? left, Stage? right)
        {
            if (left is null
                && right is null)
            {
                return true;
            }
            return left?.Equals(right) == true;
        }

        /// <summary>
        /// Сравнивает два экземпляра объектов типа <see cref="Stage"/>.
        /// </summary>
        /// <param name="left">Первый объект.</param>
        /// <param name="right">Второй объект.</param>
        /// <returns>Значение <see langword="true"/>, если объекты не равны, иначе - <see langword="false"/>.</returns>
        public static bool operator !=(Stage? left, Stage? right) => !(left == right);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override string ToString()
        {
            return
                $"{DebugHelper.GetTypeName(this)}: "
                + $"{nameof(this.RowID)}={this.RowID:B}"
                + $", {nameof(this.ID)}={this.ID:B}"
                + $", {nameof(this.Name)}={this.Name}"
                + $", {nameof(this.TemplateName)}=\"{this.TemplateName}\""
                + $", {nameof(this.BasedOnTemplateStage)}={this.BasedOnTemplateStage}"
                + $", {nameof(this.Performers)}={this.Performers.Count}";
        }

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is Stage stage && this.Equals(stage);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            // ID setter используется при десериализации. В процесс работы не меняется.
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            this.ID.GetHashCode();

        #endregion

        #region IEquatable<T> Members

        /// <summary>
        /// Сравнивает этап по значимым полям.
        /// </summary>
        /// <param name="other">Объект, с которым выполняется сравнение.</param>
        /// <returns>Значение <see langword="true"/>, если объекты равны, иначе - <see langword="false"/>.</returns>
        /// <remarks>Не выполняет сравнение <see cref="InfoStorage"/>. Для его сравнения используйте метод <see cref="IsInfoChanged(Stage)"/>.</remarks>
        public bool Equals(Stage? other) =>
            this.EqualsWithAutomaticallyChangedValuesCore(other, null);

        #endregion

        #region ISealable Members

        /// <inheritdoc/>
        public bool IsSealed { get; private set; }

        /// <inheritdoc/>
        public void Seal() => this.IsSealed = true;

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NullableDoubleNumbersIsEqual(
            double? first,
            double? second) =>
            first is null && second is null || Math.Abs((first - second) ?? Epsilon) < Epsilon;

        private void FillStageProperties(IKrRuntimeStage runtimeStage)
        {
            this.StageGroupID = runtimeStage.GroupID;
            this.StageGroupName = runtimeStage.GroupName;
            this.StageGroupOrder = runtimeStage.GroupOrder;
            this.Name = runtimeStage.StageName;
            this.TimeLimit = runtimeStage.TimeLimit;
            this.Planned = runtimeStage.Planned;
            this.Hidden = runtimeStage.Hidden;
            this.Skip = runtimeStage.Skip;
            this.CanBeSkipped = runtimeStage.CanBeSkipped;

            this.RowChanged = false;
            this.OrderChanged = false;

            this.StageTypeID = runtimeStage.StageTypeID;
            this.StageTypeCaption = runtimeStage.StageTypeCaption;
        }

        /// <summary>
        /// Устанавливает разрешения на редактирование параметров этапа.
        /// </summary>
        /// <param name="stageGroup">Группа этапов или значение <see langword="null"/>, если она недоступна. Если группа не указана, то, задаваемые в ней разрешения, разрешают редактирование.</param>
        /// <param name="stageTemplate"><inheritdoc cref="IKrStageTemplate" path="/summary"/></param>
        private void SetPermissionsFlags(
            IKrStageGroup? stageGroup,
            IKrStageTemplate stageTemplate)
        {
            // Флаг "Все этапы нередактируемые", из группы этапов, имеет приоритет над параметрами из шаблона этапов.
            this.CanChangeOrder = stageGroup?.IsGroupReadonly != true
                && stageTemplate.CanChangeOrder;

            this.IsStageReadonly = stageGroup?.IsGroupReadonly == true
                || stageTemplate.IsStagesReadonly;

            // Исправление состояния этапа в соответствии с доступными разрешениями на редактирование.
            if (!this.CanChangeOrder
                && this.OrderChanged)
            {
                this.OrderChanged = false;
            }

            if (this.IsStageReadonly
                && this.RowChanged)
            {
                this.RowChanged = false;
            }
        }

        /// <summary>
        /// Возвращает список автоматически изменённых значений этапа.
        /// </summary>
        /// <returns>Список автоматически изменённых значений этапа. Если список отсутствовал в <see cref="InfoStorage"/>, то создаётся список в <see cref="InfoStorage"/> и возвращается. Тип элемента: <see cref="string"/>.</returns>
        private IList GetAutomaticallyChangedValues()
        {
            var list = this.TryGetAutomaticallyChangedValues();

            if (list is null)
            {
                list = new List<string>();

                this.InfoStorage[KrConstants.Keys.AutomaticallyChangedValues] = list;
            }

            return list;
        }

        /// <summary>
        /// Возвращает список автоматически изменённых значений этапа или значение <see langword="null"/>, если он не определён.
        /// </summary>
        /// <returns>Список автоматически изменённых значений этапа или значение <see langword="null"/>, если он не определён. Тип элемента: <see cref="string"/>.</returns>
        private IList? TryGetAutomaticallyChangedValues() =>
            this.InfoStorage.TryGet<IList>(KrConstants.Keys.AutomaticallyChangedValues);

        /// <inheritdoc cref="EqualsWithAutomaticallyChangedValues"/>
        private bool EqualsWithAutomaticallyChangedValuesCore(
            Stage? other,
            IList? automaticallyChangedProperties = null)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // Состояние при этом сравнении не учитывается,
            // т.к. при выполнении оно может отличаться, но по факту этапы одинаковы.

            var equal =
                this.ID == other.ID
                && this.StageTypeID == other.StageTypeID
                && this.TemplateID == other.TemplateID
                && this.TemplateOrder == other.TemplateOrder
                && this.StageGroupID == other.StageGroupID
                && this.StageGroupOrder == other.StageGroupOrder
                && this.GroupPosition == other.GroupPosition
                && this.OrderChanged == other.OrderChanged
                && this.RowChanged == other.RowChanged
                && this.BasedOnTemplateStage == other.BasedOnTemplateStage
                && this.IsStageReadonly == other.IsStageReadonly
                && this.CanChangeOrder == other.CanChangeOrder
                && this.SqlPerformersIndex == other.SqlPerformersIndex
                && string.Equals(this.SqlPerformers, other.SqlPerformers, StringComparison.Ordinal)
                && string.Equals(this.StageTypeCaption, other.StageTypeCaption, StringComparison.Ordinal)
                && string.Equals(this.TemplateName, other.TemplateName, StringComparison.Ordinal)
                && string.Equals(this.Name, other.Name, StringComparison.Ordinal);

            if (!equal)
            {
                return false;
            }

            // Сравнение свойств, которые могли быть изменены автоматически.
            var hasAutomaticallyChangedProperties = automaticallyChangedProperties?.Count > 0;

            equal = (this.Planned == other.Planned
                    || hasAutomaticallyChangedProperties
                    && automaticallyChangedProperties!.Contains(nameof(this.Planned)))
                && (this.Hidden == other.Hidden
                    || hasAutomaticallyChangedProperties
                    && automaticallyChangedProperties!.Contains(nameof(this.Hidden)))
                && (this.CanBeSkipped == other.CanBeSkipped
                    || hasAutomaticallyChangedProperties
                    && automaticallyChangedProperties!.Contains(nameof(this.CanBeSkipped)))
                && (NullableDoubleNumbersIsEqual(this.TimeLimit, other.TimeLimit)
                    || hasAutomaticallyChangedProperties
                    && automaticallyChangedProperties!.Contains(nameof(this.TimeLimit)));

            if (!equal)
            {
                return false;
            }

            return StorageHelper.Equals(this.SettingsStorage, other.SettingsStorage);
        }

        /// <summary>
        /// Возвращает <see cref="InfoStorage"/> из строки этапа.
        /// </summary>
        /// <param name="stageRow">Строка, содержащие данные этапа.</param>
        /// <returns>Десериализованные данные или новый словарь, если его не удалось получить из <paramref name="stageRow"/>.</returns>
        private static Dictionary<string, object?> GetInfoStorage(
            IKrStageSerializer krStageSerializer,
            CardRow stageRow)
        {
            Dictionary<string, object?>? infoStorage = null;
            if (stageRow.TryGetValue(KrConstants.KrStages.Info, out var stateObj)
                && stateObj is string state
                && !string.IsNullOrWhiteSpace(state))
            {
                infoStorage = krStageSerializer.Deserialize<Dictionary<string, object?>>(state);
            }

            return infoStorage ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        }

        private int? GetSqlPerformersIndex()
        {
            var performersObj = this.SettingsStorage[KrConstants.KrPerformersVirtual.Synthetic];
            if (performersObj is IList perfList)
            {
                for (var i = 0; i < perfList.Count; i++)
                {
                    if (perfList[i] is IDictionary<string, object?> perf
                        && perf.TryGet<Guid?>(KrConstants.KrPerformersVirtual.PerformerID) == KrConstants.SqlApproverRoleID
                        && perf.TryGetValue(KrConstants.KrPerformersVirtual.Order, out var ord)
                        && ord is int order)
                    {
                        return order;
                    }
                }
            }

            return null;
        }

        private void RemoveSqlApproverRole()
        {
            this.Performers.RemoveAll(static p =>
                p.PerformerID == KrConstants.SqlApproverRoleID);
        }

        #endregion

    }
}
