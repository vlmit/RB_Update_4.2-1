#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Workflow;
using Tessa.Extensions.Default.Server.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrCompilers;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrObjectModel
{
    /// <inheritdoc cref="IObjectModelMapper"/>
    public sealed class ObjectModelMapper :
        IObjectModelMapper
    {
        #region Nested Types

        /// <summary>
        /// Контекст, используемый при обработке данных в методе <see cref="ObjectModelToCardRowsAsync(WorkflowProcess, Card, NestedProcessCommonInfo?, CancellationToken)"/>.
        /// </summary>
        private sealed class ObjectModelToCardRowContext
        {
            #region Constructors

            /// <summary>
            /// Инициализирует новый экземпляр класса.
            /// </summary>
            /// <param name="process">Переносимый процесс.</param>
            /// <param name="baseCard">Карточка, в которую необходимо перенести процесс.</param>
            /// <param name="npci">Информация о вложенном процессе. Может иметь значение <see langword="null"/>, если текущий процесс не является вложенным.</param>
            public ObjectModelToCardRowContext(
                WorkflowProcess process,
                Card baseCard,
                NestedProcessCommonInfo? npci)
            {
                ThrowIfNull(process);
                ThrowIfNull(baseCard);

                this.CardStoreMode = baseCard.StoreMode;
                this.Stages = NotNullOrThrow(process.Stages);
                this.InitialStages = NotNullOrThrow(NotNullOrThrow(process.InitialWorkflowProcess).Stages);

                // Строки этапов будут использоваться только от текущего вложенного процесса, но отдаем всю коллекцию, чтобы была возможность модифицировать ее по ссылке.
                this.StageRows = baseCard.GetStagesSection().Rows;

                this.NestedProcessID = npci?.NestedProcessID;
                this.ParentStageRowID = npci?.ParentStageRowID;
                this.NestedOrder = npci?.NestedOrder;

                // Построение хеш-таблицы по текущему вложенному процессу.
                this.StageRowsTable = new HashSet<Guid, CardRow>(
                    static p => p.RowID,
                    this.StageRows.Where(p => p.Get<Guid?>(KrConstants.KrStages.NestedProcessID) == this.NestedProcessID));
            }

            #endregion

            #region Properties

            /// <summary>
            /// Способ сохранения карточки, в которую необходимо перенести процесс.
            /// </summary>
            public CardStoreMode CardStoreMode { get; }

            /// <summary>
            /// Коллекция этапов процесса.
            /// </summary>
            public IList<Stage> Stages { get; }

            /// <summary>
            /// Коллекция этапов процесса до выполнения обработчиков этапов.
            /// </summary>
            public IList<Stage> InitialStages { get; }

            /// <summary>
            /// Коллекция строк этапов процесса, расположенных в карточке, в которую необходимо перенести процесс.
            /// </summary>
            public ListStorage<CardRow> StageRows { get; }

            /// <summary>
            /// Хэш-таблица построенная по <see cref="StageRows"/>.<para/>
            /// Ключ - <see cref="CardRow.RowID"/>.
            /// </summary>
            public HashSet<Guid, CardRow> StageRowsTable { get; }

            /// <summary>
            /// Идентификатор дочернего процесса.
            /// </summary>
            /// <remarks>Используется только, если обрабатываемый процесс является вложенным.</remarks>
            public Guid? NestedProcessID { get; }

            /// <summary>
            /// Идентификатор родительского этапа.
            /// </summary>
            /// <remarks>Используется только, если обрабатываемый процесс является вложенным.</remarks>
            public Guid? ParentStageRowID { get; }

            /// <summary>
            /// Порядковый номер вложенного процесса.
            /// </summary>
            /// <remarks>Используется только, если обрабатываемый процесс является вложенным.</remarks>
            public int? NestedOrder { get; }

            #endregion
        }

        #endregion

        #region Fields

        private readonly IKrStageSerializer krStageSerializer;

        private readonly ICardMetadata cardMetadata;

        private readonly IKrScope krScope;

        private readonly IKrDocumentStateManager krDocumentStateManager;

        private readonly IKrProcessCache krProcessCache;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ObjectModelMapper"/>.
        /// </summary>
        /// <param name="krStageSerializer"><inheritdoc cref="IKrStageSerializer" path="/summary"/></param>
        /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
        /// <param name="krScope"><inheritdoc cref="IKrScope" path="/summary"/></param>
        /// <param name="krDocumentStateManager"><inheritdoc cref="IKrDocumentStateManager" path="/summary"/></param>
        /// <param name="krProcessCache"><inheritdoc cref="IKrProcessCache" path="/summary"/></param>
        public ObjectModelMapper(
            IKrStageSerializer krStageSerializer,
            ICardMetadata cardMetadata,
            IKrScope krScope,
            IKrDocumentStateManager krDocumentStateManager,
            IKrProcessCache krProcessCache)
        {
            this.krStageSerializer = NotNullOrThrow(krStageSerializer);
            this.cardMetadata = NotNullOrThrow(cardMetadata);
            this.krScope = NotNullOrThrow(krScope);
            this.krDocumentStateManager = NotNullOrThrow(krDocumentStateManager);
            this.krProcessCache = NotNullOrThrow(krProcessCache);
        }

        #endregion

        #region IObjectModelMapper Members

        /// <inheritdoc />
        public MainProcessCommonInfo? GetMainProcessCommonInfo(
            Card processHolderSatellite,
            bool withInfo = true)
        {
            ThrowIfNull(processHolderSatellite);

            if (!processHolderSatellite.TryGetKrApprovalCommonInfoSection(out var commonInfo))
            {
                return null;
            }

            // Берем значение инфо процесса, которое могло быть ранее помещено в кэш
            // Таким образом, из объектной модели и кэша будет ссылка на один объект
            // далее при сохранении кэш будет сброшен и единственное актуальное значение будет в секции
            var info = withInfo
                ? ProcessInfoCacheHelper.Get(this.krStageSerializer, processHolderSatellite)
                : null;

            var fields = commonInfo.RawFields;

            return new MainProcessCommonInfo(
                fields.TryGet<Guid?>(KrConstants.KrProcessCommonInfo.CurrentApprovalStageRowID),
                info,
                fields.TryGet<Guid?>(KrConstants.KrSecondaryProcessCommonInfo.SecondaryProcessID),
                fields.TryGet<Guid?>(KrConstants.KrApprovalCommonInfo.AuthorID),
                fields.TryGet<string>(KrConstants.KrApprovalCommonInfo.AuthorName),
                fields.TryGet<string>(KrConstants.KrApprovalCommonInfo.AuthorComment),
                fields.TryGet<int>(KrConstants.KrApprovalCommonInfo.StateID),
                fields.TryGet<Guid?>(KrConstants.KrApprovalCommonInfo.ProcessOwnerID),
                fields.TryGet<string>(KrConstants.KrApprovalCommonInfo.ProcessOwnerName));
        }

        /// <inheritdoc />
        public async ValueTask SetMainProcessCommonInfoAsync(
            Card mainCard,
            Card processHolderSatellite,
            MainProcessCommonInfo processCommonInfo,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(mainCard);
            ThrowIfNull(processHolderSatellite);
            ThrowIfNull(processCommonInfo);

            if (!processHolderSatellite.TryGetKrApprovalCommonInfoSection(out var aci))
            {
                return;
            }

            var aciFields = aci.Fields;
            aciFields[KrConstants.KrProcessCommonInfo.CurrentApprovalStageRowID] = processCommonInfo.CurrentStageRowID;

            ProcessInfoCacheHelper.Update(this.krStageSerializer, processHolderSatellite);

            aciFields[KrConstants.KrProcessCommonInfo.AuthorID] = processCommonInfo.AuthorID;
            aciFields[KrConstants.KrProcessCommonInfo.AuthorName] = processCommonInfo.AuthorName;

            aciFields[KrConstants.KrProcessCommonInfo.ProcessOwnerID] = processCommonInfo.ProcessOwnerID;
            aciFields[KrConstants.KrProcessCommonInfo.ProcessOwnerName] = processCommonInfo.ProcessOwnerName;

            if (processHolderSatellite.TypeID == DefaultCardTypes.KrSecondarySatelliteTypeID)
            {
                aciFields[KrConstants.KrSecondaryProcessCommonInfo.SecondaryProcessID] = processCommonInfo.SecondaryProcessID;
                return;
            }

            aciFields[KrConstants.KrApprovalCommonInfo.AuthorComment] = processCommonInfo.AuthorComment;

            if (processCommonInfo.StateTimestamp > 0)
            {
                var (_, hasMainSatelliteChanges, _) = await this.krDocumentStateManager.SetStateAsync(
                    mainCard,
                    processHolderSatellite,
                    (KrState) processCommonInfo.State,
                    cancellationToken);

                if (hasMainSatelliteChanges)
                {
                    aci.Fields[KrConstants.KrApprovalCommonInfo.StateChangedDateTimeUTC] = DateTime.UtcNow;

                    if (processCommonInfo.AffectMainCardVersionWhenStateChanged)
                    {
                        this.krScope.ForceIncrementMainCardVersion(mainCard.ID);
                    }
                }
            }
        }

        /// <inheritdoc />
        public List<NestedProcessCommonInfo>? GetNestedProcessCommonInfos(
            Card processHolderSatellite)
        {
            ThrowIfNull(processHolderSatellite);

            if (processHolderSatellite.TryGetKrApprovalCommonInfoSection(out var aci)
                && aci.Fields.TryGetValue(KrConstants.KrProcessCommonInfo.NestedWorkflowProcesses, out var nestedWorkflowProcessesJsonObj))
            {
                if (nestedWorkflowProcessesJsonObj is string nestedWorkflowProcessesJson
                    && !string.IsNullOrWhiteSpace(nestedWorkflowProcessesJson))
                {
                    var nestedWorkflowProcessesStorage = this.krStageSerializer.Deserialize<List<object>>(nestedWorkflowProcessesJson);

                    if (nestedWorkflowProcessesStorage is not null)
                    {
                        return nestedWorkflowProcessesStorage
                            .Select(static p => new NestedProcessCommonInfo((Dictionary<string, object?>) p))
                            .ToList();
                    }
                }

                return [];
            }

            return null;
        }

        /// <inheritdoc />
        public void SetNestedProcessCommonInfos(
            Card processHolderSatellite,
            IReadOnlyCollection<NestedProcessCommonInfo>? nestedProcessCommonInfos)
        {
            ThrowIfNull(processHolderSatellite);

            if (nestedProcessCommonInfos is null)
            {
                return;
            }

            // Выпиливаем завершенные процессы
            var activeInfos = new List<object>(nestedProcessCommonInfos.Count);

            foreach (var info in nestedProcessCommonInfos)
            {
                if (info.CurrentStageRowID.HasValue)
                {
                    activeInfos.Add(info.GetStorage());
                }
            }

            var nestedWorkflowProcessesJson = this.krStageSerializer.Serialize(activeInfos);

            processHolderSatellite
                .GetApprovalInfoSection()
                .Fields[KrConstants.KrProcessCommonInfo.NestedWorkflowProcesses] = nestedWorkflowProcessesJson;
        }

        /// <inheritdoc />
        public void FillWorkflowProcessFromPci(
            WorkflowProcess workflowProcess,
            ProcessCommonInfo? commonInfo,
            MainProcessCommonInfo? primaryProcessCommonInfo)
        {
            ThrowIfNull(workflowProcess);

            if (primaryProcessCommonInfo is null)
            {
                workflowProcess.SetState(KrState.Draft, false);
            }
            else
            {
                if (primaryProcessCommonInfo.AuthorID.HasValue)
                {
                    workflowProcess.SetAuthor(
                        new Author(
                            primaryProcessCommonInfo.AuthorID.Value,
                            primaryProcessCommonInfo.AuthorName),
                        false);
                }

                if (primaryProcessCommonInfo.ProcessOwnerID.HasValue)
                {
                    workflowProcess.SetProcessOwner(
                        new Author(
                            primaryProcessCommonInfo.ProcessOwnerID.Value,
                            primaryProcessCommonInfo.ProcessOwnerName),
                        false);
                }

                workflowProcess.SetAuthorComment(primaryProcessCommonInfo.AuthorComment, false);
                workflowProcess.SetState((KrState) primaryProcessCommonInfo.State, false);
            }

            if (commonInfo is not null)
            {
                if (commonInfo.AuthorID.HasValue)
                {
                    workflowProcess.SetAuthorCurrentProcess(
                        new Author(
                            commonInfo.AuthorID.Value,
                            commonInfo.AuthorName),
                        false);
                }

                if (commonInfo.ProcessOwnerID.HasValue)
                {
                    workflowProcess.SetProcessOwnerCurrentProcess(
                        new Author(
                            commonInfo.ProcessOwnerID.Value,
                            commonInfo.ProcessOwnerName),
                        false);
                }

                workflowProcess.CurrentApprovalStageRowID = commonInfo.CurrentStageRowID;
            }
        }

        /// <inheritdoc />
        public async ValueTask<WorkflowProcess> CardRowsToObjectModelAsync(
            IKrStageTemplate stageTemplate,
            IReadOnlyCollection<IKrRuntimeStage> templateStages,
            MainProcessCommonInfo? primaryPci = null,
            bool initialStage = true,
            bool saveInitialStages = false,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(stageTemplate);
            ThrowIfNull(templateStages);

            var stageGroups = await this.krProcessCache
                .GetAllStageGroupsAsync(cancellationToken);

            if (!stageGroups.TryGetValue(stageTemplate.StageGroupID, out var stageGroup))
            {
                throw new InvalidOperationException($"Not found group with ID={stageTemplate.StageGroupID:B}.");
            }

            var stages = new SealableObjectList<Stage>(templateStages.Count);

            foreach (var templateStage in templateStages.OrderBy(static p => p.Order))
            {
                var stage = await Stage.CreateFromStageTemplateAsync(
                    stageGroup,
                    stageTemplate,
                    templateStage,
                    initialStage,
                    cancellationToken);

                stages.Add(stage);
            }

            var primaryPciInfo = primaryPci?.Info ?? new Dictionary<string, object?>(StringComparer.Ordinal);

            var wp = new WorkflowProcess(
                primaryPciInfo,
                primaryPciInfo,
                stages,
                nestedProcessID: null,
                isMainProcess: true);

            if (saveInitialStages)
            {
                wp.UpdateInitialWorkflowProcess();
            }

            return wp;
        }

        /// <inheritdoc />
        public async ValueTask<WorkflowProcess> CardRowsToObjectModelAsync(
            Card processHolder,
            ProcessCommonInfo pci,
            MainProcessCommonInfo mainPci,
            string processTypeName,
            bool initialStage = true,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(processHolder);
            ThrowIfNull(pci);
            ThrowIfNull(mainPci);

            var pciNested = pci as NestedProcessCommonInfo;
            var nestedProcessID = pciNested?.NestedProcessID;
            var stagesRows = processHolder.GetStagesSection().Rows;
            var stages = new SealableObjectList<Stage>(stagesRows.Count);
            var stageTemplates = await this.krProcessCache
                .GetAllStageTemplatesAsync(cancellationToken);
            var stagesByTemplates = await this.krProcessCache
                .GetAllStagesByTemplatesAsync(cancellationToken);
            var groups = await this.krProcessCache
                .GetAllStageGroupsAsync(cancellationToken);

            foreach (var stageRow in stagesRows
                .Where(p => p.State != CardRowState.Deleted
                    && p.TryGet<Guid?>(KrConstants.KrStages.NestedProcessID) == nestedProcessID)
                .OrderBy(static row => row.Get<int>(KrConstants.KrStages.Order)))
            {
                var stageGroupID = stageRow.Get<Guid>(KrConstants.KrStages.StageGroupID);
                var stageGroup = groups.GetValueOrDefault(stageGroupID);

                IKrStageTemplate? stageTemplate = null;
                IReadOnlyList<IKrRuntimeStage> templateStages = [];

                var basedOnStageTemplateID = stageRow.TryGet<Guid?>(KrConstants.KrStages.BasedOnStageTemplateID);
                if (basedOnStageTemplateID.HasValue)
                {
                    stageTemplate = stageTemplates.GetValueOrDefault(
                        basedOnStageTemplateID.Value);

                    templateStages = stagesByTemplates.GetValueOrDefault(
                        basedOnStageTemplateID.Value,
                        []);
                }

                var stage = await Stage.CreateFromStageRowAsync(
                    stageRow,
                    this.krStageSerializer,
                    stageGroup,
                    stageTemplate,
                    templateStages,
                    initialStage,
                    cancellationToken);

                stages.Add(stage);
            }

            var wp = new WorkflowProcess(
                pci.Info,
                mainPci.Info,
                stages,
                nestedProcessID: nestedProcessID,
                isMainProcess: string.Equals(processTypeName, KrConstants.KrProcessName, StringComparison.Ordinal));

            wp.UpdateInitialWorkflowProcess();
            return wp;
        }

        /// <inheritdoc />
        public void ObjectModelToPci(
            WorkflowProcess process,
            ProcessCommonInfo pci,
            MainProcessCommonInfo mainPci,
            MainProcessCommonInfo? primaryPci)
        {
            ThrowIfNull(process);
            ThrowIfNull(pci);
            ThrowIfNull(mainPci);

            pci.CurrentStageRowID = process.CurrentApprovalStageRowID;

            if (primaryPci is not null)
            {
                if (primaryPci.AuthorTimestamp < process.AuthorTimestamp)
                {
                    primaryPci.AuthorID = process.Author?.AuthorID;
                    primaryPci.AuthorName = process.Author?.AuthorName;
                    primaryPci.AuthorTimestamp = process.AuthorTimestamp;
                }

                if (primaryPci.AuthorCommentTimestamp < process.AuthorCommentTimestamp)
                {
                    primaryPci.AuthorComment = process.AuthorComment;
                    primaryPci.AuthorCommentTimestamp = process.AuthorCommentTimestamp;
                }

                if (primaryPci.StateTimestamp < process.StateTimestamp)
                {
                    primaryPci.State = process.State.ID;
                    primaryPci.StateTimestamp = process.StateTimestamp;
                }

                if (primaryPci.AffectMainCardVersionWhenStateChangedTimestamp < process.AffectMainCardVersionWhenStateChangedTimestamp)
                {
                    primaryPci.AffectMainCardVersionWhenStateChanged = process.AffectMainCardVersionWhenStateChanged;
                    primaryPci.AffectMainCardVersionWhenStateChangedTimestamp = process.AffectMainCardVersionWhenStateChangedTimestamp;
                }

                if (primaryPci.ProcessOwnerTimestamp < process.ProcessOwnerTimestamp)
                {
                    primaryPci.ProcessOwnerID = process.ProcessOwner?.AuthorID;
                    primaryPci.ProcessOwnerName = process.ProcessOwner?.AuthorName;
                    primaryPci.ProcessOwnerTimestamp = process.ProcessOwnerTimestamp;
                }
            }

            pci.Info = process.InfoStorage;

            if (!ReferenceEquals(pci, mainPci))
            {
                StorageHelper.Merge(process.MainProcessInfoStorage, mainPci.Info);
            }

            if (pci.AuthorTimestamp < process.AuthorCurrentProcessTimestamp)
            {
                pci.AuthorID = process.AuthorCurrentProcess?.AuthorID;
                pci.AuthorName = process.AuthorCurrentProcess?.AuthorName;
                pci.AuthorTimestamp = process.AuthorCurrentProcessTimestamp;
            }

            if (pci.ProcessOwnerTimestamp < process.ProcessOwnerCurrentProcessTimestamp)
            {
                pci.ProcessOwnerID = process.ProcessOwnerCurrentProcess?.AuthorID;
                pci.ProcessOwnerName = process.ProcessOwnerCurrentProcess?.AuthorName;
                pci.ProcessOwnerTimestamp = process.ProcessOwnerCurrentProcessTimestamp;
            }
        }

        /// <inheritdoc />
        public async ValueTask<List<RouteDiff>> ObjectModelToCardRowsAsync(
            WorkflowProcess process,
            Card baseCard,
            NestedProcessCommonInfo? npci = null,
            CancellationToken cancellationToken = default)
        {
            // Параметры будут проверены в ctor.
            var ctx = new ObjectModelToCardRowContext(
                process,
                baseCard,
                npci);

            return await this.MoveStagesIntoCardRowsAsync(
                ctx,
                cancellationToken);
        }

        /// <inheritdoc/>
        public async ValueTask RepairSettingsAsync(
            Stage stage,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(stage);

            var storage = stage.SettingsStorage;
            var serializerSettings = await this.krStageSerializer.GetSettingsAsync(cancellationToken);

            foreach (var referenceToStages in serializerSettings.ReferencesToStages)
            {
                if (storage.TryGetValue(referenceToStages.SectionName, out var rowsObj)
                    && rowsObj is IList rows
                    && rows is not byte[])
                {
                    foreach (var row in rows)
                    {
                        if (row is IDictionary<string, object?> rowStorage)
                        {
                            Guid? oldStageRowID;
                            if ((oldStageRowID = rowStorage.TryGet<Guid?>(referenceToStages.RowIDFieldName)).HasValue
                                && oldStageRowID != stage.RowID)
                            {
                                rowStorage[referenceToStages.RowIDFieldName] = stage.RowID;
                            }
                        }
                    }
                }
            }

            if (stage.Performers is not null)
            {
                for (var i = 0; i < stage.Performers.Count; i++)
                {
                    var performer = stage.Performers[i];
                    performer.GetStorage()[KrConstants.KrPerformersVirtual.Order] = Int32Boxes.Box(i);
                }
            }
        }

        /// <inheritdoc/>
        public async ValueTask ObjectModelToCardRowsAsync(
            ProcessHolder processHolder,
            Card processHolderSatellite,
            Card contextualSatellite,
            Card mainCard,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(processHolder);
            ThrowIfNull(processHolder.MainProcessCommonInfo);
            ThrowIfNull(processHolder.PrimaryProcessCommonInfo);
            ThrowIfNull(processHolderSatellite);
            ThrowIfNull(contextualSatellite);
            ThrowIfNull(mainCard);

            foreach (var nested in processHolder.NestedWorkflowProcesses)
            {
                var process = nested.Value;
                var npci = NotNullOrThrow(processHolder.NestedProcessCommonInfos)[nested.Key];

                await this.ObjectModelToCardRowsAsync(
                    process,
                    processHolderSatellite,
                    npci,
                    cancellationToken);

                this.ObjectModelToPci(
                    process,
                    npci,
                    processHolder.MainProcessCommonInfo,
                    processHolder.PrimaryProcessCommonInfo);
            }

            if (processHolder.MainWorkflowProcess is not null)
            {
                await this.ObjectModelToCardRowsAsync(
                    processHolder.MainWorkflowProcess,
                    processHolderSatellite,
                    null,
                    cancellationToken);

                this.ObjectModelToPci(
                    processHolder.MainWorkflowProcess,
                    processHolder.MainProcessCommonInfo,
                    processHolder.MainProcessCommonInfo,
                    processHolder.PrimaryProcessCommonInfo);
            }

            // Перенос информации по основному процессу в контекстуальный сателлит.
            await this.SetMainProcessCommonInfoAsync(
                mainCard,
                contextualSatellite,
                processHolder.PrimaryProcessCommonInfo,
                cancellationToken);

            // Если это не основной процесс, то перенести информацию по текущему процессу в его карточку-холдер.
            if (!ReferenceEquals(contextualSatellite, processHolderSatellite))
            {
                await this.SetMainProcessCommonInfoAsync(
                    mainCard,
                    processHolderSatellite,
                    processHolder.MainProcessCommonInfo,
                    cancellationToken);
            }

            this.SetNestedProcessCommonInfos(
                processHolderSatellite,
                processHolder.NestedProcessCommonInfos);
        }

        /// <inheritdoc/>
        public ValueTask<(WorkflowProcess WorkflowProcess, ProcessCommonInfo ProcessCommonInfo)> GetWorkflowProcessAsync(
            IWorkflowProcessInfo info,
            StartingSecondaryProcessInfo? startingSecondaryProcess,
            Card contextualSatellite,
            Card processHolderSatellite,
            ProcessHolder processHolder,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(info);
            ThrowIfNull(contextualSatellite);
            ThrowIfNull(processHolderSatellite);
            ThrowIfNull(processHolder);

            return this.GetOrUpdateWorkflowProcessAsync(
                info,
                startingSecondaryProcess,
                contextualSatellite,
                processHolderSatellite,
                processHolder,
                false,
                cancellationToken);
        }

        /// <inheritdoc/>
        public async ValueTask UpdateWorkflowProcessAsync(
            IWorkflowProcessInfo info,
            StartingSecondaryProcessInfo? startingSecondaryProcess,
            Card contextualSatellite,
            Card processHolderSatellite,
            ProcessHolder processHolder,
            WorkflowProcess updateWorkflowProcess,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(info);
            ThrowIfNull(contextualSatellite);
            ThrowIfNull(processHolderSatellite);
            ThrowIfNull(processHolder);

            (var workflowProcess, _) = await this.GetOrUpdateWorkflowProcessAsync(
                info,
                startingSecondaryProcess,
                contextualSatellite,
                processHolderSatellite,
                processHolder,
                true,
                cancellationToken);

            updateWorkflowProcess.Update(
                workflowProcess,
                true);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Заполняет поля строки содержащей информацию по этапу.
        /// </summary>
        /// <param name="stage">Информация о новом этапе (источник данных).</param>
        /// <param name="initialStage">Информация о заменяемом этапе (этапе до пересчёта). Может быть не задана.</param>
        /// <param name="stageRow">Строка этапа (куда записать данные).</param>
        /// <param name="ctx"><inheritdoc cref="ObjectModelToCardRowContext" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        private async ValueTask FillStageSectionsAsync(
            Stage stage,
            Stage? initialStage,
            CardRow stageRow,
            ObjectModelToCardRowContext ctx,
            CancellationToken cancellationToken = default)
        {
            if (initialStage is null
                || !StorageHelper.Equals(stage.SettingsStorage, initialStage.SettingsStorage))
            {
                await this.krStageSerializer.SerializeSettingsStorageAsync(
                    stageRow,
                    stage.SettingsStorage,
                    cancellationToken);
            }

            var stageRowFields = stageRow.Fields;

            stageRowFields[KrConstants.KrStages.NameField] = stage.Name;
            stageRowFields[KrConstants.KrStages.TimeLimit] = DoubleBoxes.Box(stage.TimeLimit);
            stageRowFields[KrConstants.KrStages.Planned] = stage.Planned;
            stageRowFields[KrConstants.KrStages.Hidden] = BooleanBoxes.Box(stage.Hidden);
            stageRowFields[KrConstants.KrStages.Skip] = BooleanBoxes.Box(stage.Skip);
            stageRowFields[KrConstants.KrStages.CanBeSkipped] = BooleanBoxes.Box(stage.CanBeSkipped);

            stageRowFields[KrConstants.KrStages.StageGroupID] = stage.StageGroupID;
            stageRowFields[KrConstants.KrStages.StageGroupName] = stage.StageGroupName;
            stageRowFields[KrConstants.KrStages.StageGroupOrder] = Int32Boxes.Box(stage.StageGroupOrder);

            stageRowFields[KrConstants.KrStages.RowChanged] = BooleanBoxes.Box(stage.RowChanged);
            stageRowFields[KrConstants.KrStages.OrderChanged] = BooleanBoxes.Box(stage.OrderChanged);

            stageRowFields[KrConstants.KrStages.StageTypeID] = stage.StageTypeID;
            stageRowFields[KrConstants.KrStages.StageTypeCaption] = stage.StageTypeCaption;

            stageRowFields[KrConstants.KrStages.NestedProcessID] = ctx.NestedProcessID;
            stageRowFields[KrConstants.KrStages.ParentStageRowID] = ctx.ParentStageRowID;
            stageRowFields[KrConstants.KrStages.NestedOrder] = Int32Boxes.Box(ctx.NestedOrder);

            // Этап может быть не привязанным вообще, в случае для этапа, добавленного пользователем
            // Этап может быть привязан только к шаблону этапов, это этап, добавленный в пользовательских скриптах
            // Этап может быть привязан шаблону этапов и к конкретному этапу внутри шаблона.
            if (stage.BasedOnTemplate)
            {
                stageRow.Fields[KrConstants.KrStages.BasedOnStageTemplateID] = stage.TemplateID;
                stageRow.Fields[KrConstants.KrStages.BasedOnStageTemplateName] = stage.TemplateName;
                stageRow.Fields[KrConstants.KrStages.BasedOnStageTemplateOrder] = Int32Boxes.Box(stage.TemplateOrder);
                stageRow.Fields[KrConstants.KrStages.BasedOnStageTemplateGroupPositionID] = Int32Boxes.Box(stage.GroupPosition.ID);
            }

            if (stage.BasedOnTemplateStage)
            {
                stageRow.Fields[KrConstants.KrStages.BasedOnStageRowID] = stage.ID;
            }
        }

        private async ValueTask<List<RouteDiff>> MoveNewStagesIntoCardRowsAsync(
            ObjectModelToCardRowContext ctx,
            CancellationToken cancellationToken = default)
        {
            var diffs = new List<RouteDiff>(ctx.Stages.Count);
            for (var stageIndex = 0; stageIndex < ctx.Stages.Count; stageIndex++)
            {
                var newStage = ctx.Stages[stageIndex];
                var cardRow = AddRow(ctx.StageRows, newStage.RowID);

                await this.RepairSettingsAsync(
                    newStage,
                    cancellationToken);

                await this.FillStageSectionsAsync(
                    newStage,
                    null,
                    cardRow,
                    ctx,
                    cancellationToken);

                var cardRowFields = cardRow.Fields;
                cardRowFields[KrConstants.KrStages.Info] = this.krStageSerializer.Serialize(newStage.InfoStorage);
                cardRowFields[KrConstants.KrStages.StateID] = Int32Boxes.Box(newStage.State.ID);
                cardRowFields[KrConstants.KrStages.StateName] = await this.cardMetadata.GetStageStateNameAsync(
                    newStage.State,
                    cancellationToken);

                UpdateRowOrder(cardRow, stageIndex, KrConstants.KrStages.Order, onlyIfNeeded: false);
                diffs.Add(RouteDiff.NewStage(cardRow.RowID, newStage.Name, newStage.Hidden));
            }

            return diffs;
        }

        private static List<RouteDiff> DeleteAllStageRows(
            ObjectModelToCardRowContext ctx)
        {
            if (ctx.InitialStages is not null)
            {
                var diffs = new List<RouteDiff>(ctx.InitialStages.Count);
                foreach (var oldStage in ctx.InitialStages)
                {
                    var cardRow = ctx.StageRowsTable[oldStage.RowID];
                    cardRow.State = CardRowState.Deleted;
                    diffs.Add(RouteDiff.DeleteStage(cardRow.RowID, oldStage.Name, oldStage.Hidden));
                }

                return diffs;
            }

            return [];
        }

        private static void DeleteRedundantStageRows(
            ObjectModelToCardRowContext ctx,
            HashSet<Guid, Stage> initialStagesTable,
            ICollection<RouteDiff> diffs)
        {
            var redundantIDs = ctx.InitialStages
                .Select(static x => (x.ID, x.RowID))
                .Except(ctx.Stages.Select(static x => (x.ID, x.RowID)))
                .Select(static x => x.RowID);

            foreach (var redundantRowID in redundantIDs)
            {
                var redundantCardRowStage = ctx.StageRowsTable[redundantRowID];
                redundantCardRowStage.State = CardRowState.Deleted;

                var initialStage = initialStagesTable[redundantRowID];
                diffs.Add(
                    RouteDiff.DeleteStage(
                        redundantCardRowStage.RowID,
                        initialStage.Name,
                        initialStage.Hidden));
            }
        }

        private async ValueTask<List<RouteDiff>> MoveStagesIntoCardRowsAsync(
            ObjectModelToCardRowContext ctx,
            CancellationToken cancellationToken = default)
        {
            // Новых этапов нет, маршрут пустой, нужно удалить все старые этапы
            if (ctx.Stages.Count == 0)
            {
                return DeleteAllStageRows(ctx);
            }

            // Старых этапов нет, нужно просто создать новые
            if (ctx.InitialStages is null
                || ctx.InitialStages.Count == 0)
            {
                return await this.MoveNewStagesIntoCardRowsAsync(ctx, cancellationToken);
            }

            var initialStagesTable = new HashSet<Guid, Stage>(static x => x.RowID, ctx.InitialStages);
            var diffs = new List<RouteDiff>(ctx.Stages.Count + ctx.InitialStages.Count);

            for (var stageIndex = 0; stageIndex < ctx.Stages.Count; stageIndex++)
            {
                var stage = ctx.Stages[stageIndex];
                _ = initialStagesTable.TryGetItem(stage.RowID, out var oldStage);
                _ = ctx.StageRowsTable.TryGetItem(stage.RowID, out var cardRow);

                // Для корректного восстановления и сравнения с существующими нужно восстановить
                // StageRowID конкретной карточки.
                if (cardRow is not null)
                {
                    await this.RepairSettingsAsync(
                        stage,
                        cancellationToken);
                }

                // Если текущий этап не совпадает с этапом до пересчета по значению или порядку
                // то этап можно считать добавленным или измененным,
                // в зависимости от того, был ли уже этап с таким ID
                if (ctx.InitialStages.Count <= stageIndex
                    || stage != ctx.InitialStages[stageIndex])
                {
                    if (oldStage is not null
                        && cardRow is not null
                        && stage.RowID == cardRow.RowID)
                    {
                        // изменен этап oldStage -> stage
                        if (cardRow.State == CardRowState.None)
                        {
                            cardRow.State = CardRowState.Modified;
                        }

                        diffs.Add(RouteDiff.ModifyStage(
                            cardRow.RowID,
                            stage.Name,
                            oldStage.Name,
                            stage.Hidden));
                    }
                    else
                    {
                        // добавлен этап stage
                        cardRow = AddRow(ctx.StageRows, stage.RowID);
                        oldStage = default;

                        diffs.Add(RouteDiff.NewStage(
                            cardRow.RowID,
                            stage.Name,
                            stage.Hidden));
                    }

                    await this.FillStageSectionsAsync(
                        stage,
                        oldStage,
                        cardRow,
                        ctx,
                        cancellationToken);
                }

                if (cardRow is not null)
                {
                    if (oldStage is null
                        || stage.IsInfoChanged(oldStage))
                    {
                        cardRow.Fields[KrConstants.KrStages.Info] = this.krStageSerializer.Serialize(stage.InfoStorage);
                    }

                    cardRow.Fields[KrConstants.KrStages.StateID] = Int32Boxes.Box(stage.State.ID);
                    cardRow.Fields[KrConstants.KrStages.StateName] = await this.cardMetadata.GetStageStateNameAsync(
                        stage.State,
                        cancellationToken);

                    UpdateRowOrder(
                        cardRow,
                        stageIndex,
                        KrConstants.KrStages.Order);
                }
            }

            // Пометка лишних на удаление.
            // Лишние определяются как разность множеств начальных и текущих этапов.
            DeleteRedundantStageRows(ctx, initialStagesTable, diffs);

            // Если возникает кейс, когда операция производится над еще не сохраненной карточкой,
            // Но в ней уже был какой-то маршрут, то после пересчета этапы могут пропасть.
            // Их нужно удалить из коллекции, а не отправить на сохранение с состоянием Deleted.
            // Возникает, например, при создании копии
            if (ctx.CardStoreMode == CardStoreMode.Insert)
            {
                ctx.StageRows.RemoveAll(static p => p.State == CardRowState.Deleted);
            }

            return diffs;
        }

        /// <summary>
        /// Задаёт порядковый номер строки этапа.
        /// </summary>
        /// <param name="row">Строка содержащая информацию по этапу.</param>
        /// <param name="order">Порядковый номер строки этапа.</param>
        /// <param name="alias">Алиас поля содержащего порядковый номер строки этапа.</param>
        /// <param name="onlyIfNeeded">Значение <see langword="true"/>, если необходимо установить новое значение порядкового номера строки этапа только если оно отличается от старого значение, иначе - <see langword="false"/> - задать новое значение без проверки.</param>
        private static void UpdateRowOrder(
            CardRow row,
            int order,
            string alias,
            bool onlyIfNeeded = true)
        {
            if (!onlyIfNeeded)
            {
                row.Fields[alias] = Int32Boxes.Box(order);
                return;
            }

            if (!row.Fields.TryGetValue(alias, out var oldOrderObj)
                || oldOrderObj is not int oldOrder
                || oldOrder != order)
            {
                row.Fields[alias] = Int32Boxes.Box(order);

                if (row.State == CardRowState.None)
                {
                    row.State = CardRowState.Modified;
                }
            }
        }

        private static CardRow AddRow(
            ListStorage<CardRow> rows,
            Guid rowID)
        {
            var row = rows.Add();
            row.State = CardRowState.Inserted;
            row.RowID = rowID;
            return row;
        }

        private async ValueTask<(WorkflowProcess WorkflowProcess, ProcessCommonInfo ProcessCommonInfo)> GetWorkflowProcessForNestedProcessAsync(
            IWorkflowProcessInfo info,
            StartingSecondaryProcessInfo? startingSecondaryProcess,
            Card processHolderSatellite,
            ProcessHolder processHolder,
            bool isUpdate,
            CancellationToken cancellationToken = default)
        {
            if (isUpdate
                || processHolder.NestedProcessCommonInfos is null)
            {
                processHolder.SetNestedProcessCommonInfosList(
                    this.GetNestedProcessCommonInfos(
                        processHolderSatellite));
            }

            var nestedProcessID = GetOrCreateNestedProcessID(info);

            // При обновлении не требуется пересоздавать заново объект.
            // Он был задан в блоке выше в SetNestedProcessCommonInfosList.
            if (!processHolder.NestedProcessCommonInfos!.TryGetItem(nestedProcessID, out var npci))
            {
                npci = CreateNestedProcessCommonInfo(
                    nestedProcessID,
                    startingSecondaryProcess);
                processHolder.NestedProcessCommonInfos!.Add(npci);
            }

            if (isUpdate
                || !processHolder.NestedWorkflowProcesses.TryGetValue(nestedProcessID, out var workflowProcess))
            {
                workflowProcess = await this.CreateNestedWorkflowProcessAsync(
                    processHolderSatellite,
                    npci,
                    processHolder.MainProcessCommonInfo,
                    processHolder.PrimaryProcessCommonInfo,
                    info.ProcessTypeName,
                    cancellationToken);

                processHolder.NestedWorkflowProcesses[nestedProcessID] = workflowProcess;
            }

            return (workflowProcess, npci);
        }

        private static Guid GetOrCreateNestedProcessID(
            IWorkflowProcessInfo info)
        {
            var nestedProcessID = info.ProcessParameters.TryGet<Guid?>(KrConstants.Keys.NestedProcessID);

            if (!nestedProcessID.HasValue)
            {
                nestedProcessID = Guid.NewGuid();
                info.ProcessParameters[KrConstants.Keys.NestedProcessID] = nestedProcessID;
                info.PendingProcessParametersUpdate = true;
            }

            return nestedProcessID.Value;
        }

        private static NestedProcessCommonInfo CreateNestedProcessCommonInfo(
            Guid nestedProcessID,
            StartingSecondaryProcessInfo? startingSecondaryProcess) =>
            new(
                null,
                null,
                startingSecondaryProcess?.SecondaryProcessID,
                nestedProcessID,
                startingSecondaryProcess?.ParentStageRowID ?? Guid.Empty,
                startingSecondaryProcess?.NestedOrder ?? 0);

        private async ValueTask<WorkflowProcess> CreateNestedWorkflowProcessAsync(
            Card processHolderSatellite,
            NestedProcessCommonInfo npci,
            MainProcessCommonInfo? mainProcessCommonInfo,
            MainProcessCommonInfo? primaryProcessCommonInfo,
            string processTypeName,
            CancellationToken cancellationToken = default)
        {
            var workflowProcess = await this.CardRowsToObjectModelAsync(
                processHolderSatellite,
                npci,
                mainProcessCommonInfo!,
                processTypeName,
                cancellationToken: cancellationToken);

            this.FillWorkflowProcessFromPci(
                workflowProcess,
                npci,
                primaryProcessCommonInfo);

            return workflowProcess;
        }

        private async ValueTask<(WorkflowProcess WorkflowProcess, ProcessCommonInfo ProcessCommonInfo)> GetOrUpdateWorkflowProcessAsync(
            IWorkflowProcessInfo info,
            StartingSecondaryProcessInfo? startingSecondaryProcess,
            Card contextualSatellite,
            Card processHolderSatellite,
            ProcessHolder processHolder,
            bool isUpdate,
            CancellationToken cancellationToken = default)
        {
            if (isUpdate
                || processHolder.MainProcessCommonInfo is null)
            {
                processHolder.MainProcessCommonInfo = NotNullOrThrow(this.GetMainProcessCommonInfo(processHolderSatellite));
            }

            if (isUpdate
                || processHolder.PrimaryProcessCommonInfo is null)
            {
                processHolder.PrimaryProcessCommonInfo = processHolder.MainProcessType == KrConstants.KrProcessName
                    ? processHolder.MainProcessCommonInfo
                    : this.GetMainProcessCommonInfo(contextualSatellite, false);
            }

            WorkflowProcess? workflowProcess;
            ProcessCommonInfo processCommonInfo;

            if (info.ProcessTypeName == KrConstants.KrNestedProcessName)
            {
                (workflowProcess, processCommonInfo) = await this.GetWorkflowProcessForNestedProcessAsync(
                    info,
                    startingSecondaryProcess,
                    processHolderSatellite,
                    processHolder,
                    isUpdate,
                    cancellationToken);
            }
            else
            {
                workflowProcess = processHolder.MainWorkflowProcess;

                if (isUpdate
                    || workflowProcess is null)
                {
                    workflowProcess = await this.CardRowsToObjectModelAsync(
                        processHolderSatellite,
                        processHolder.MainProcessCommonInfo,
                        processHolder.MainProcessCommonInfo,
                        info.ProcessTypeName,
                        cancellationToken: cancellationToken);

                    this.FillWorkflowProcessFromPci(
                        workflowProcess,
                        processHolder.MainProcessCommonInfo,
                        processHolder.PrimaryProcessCommonInfo);
                }

                if (!isUpdate)
                {
                    processHolder.MainWorkflowProcess = workflowProcess;
                }

                if (info.ProcessTypeName == KrConstants.KrSecondaryProcessName
                    && startingSecondaryProcess?.SecondaryProcessID.HasValue == true)
                {
                    processHolder.MainProcessCommonInfo.SecondaryProcessID = startingSecondaryProcess.SecondaryProcessID;
                }

                processCommonInfo = processHolder.MainProcessCommonInfo;
            }

            return (workflowProcess, processCommonInfo);
        }

        #endregion
    }
}
