#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Cards;
using Tessa.Cards.Workflow;
using Tessa.Extensions.Default.Server.Normalization;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.Wf;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
using Tessa.Normalization;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Scheme;
using Tessa.Workflow;
using Tessa.Workflow.Actions;
using Tessa.Workflow.Compilation;
using Tessa.Workflow.Helpful;
using Tessa.Workflow.Normalization;
using Tessa.Workflow.Signals;
using Tessa.Workflow.Storage;
using static Tessa.Extensions.Default.Shared.Workflow.KrProcess.KrConstants;
using static Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine.WorkflowConstants;
using WorkflowSignalTypes = Tessa.Workflow.Signals.WorkflowSignalTypes;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    /// <summary>
    /// Обработчик действия <see cref="KrDescriptors.KrResolutionDescriptor"/>.
    /// </summary>
    public sealed class KrResolutionAction(
        IWorkflowTaskActionDeps deps,
        IKrTokenProvider tokenProvider,
        IKrGetSqlPerformersStrategy getSqlPerformersStrategy)
        : KrWorkflowTaskActionBase(KrDescriptors.KrResolutionDescriptor, deps)
    {
        #region Fields

        /// <summary>
        /// Имя ключа, по которому в параметрах действия содержится идентификатор обрабатываемого задания данным экземпляром действия. Тип значения: <see cref="Guid"/>.
        /// </summary>
        private const string TaskParamKey = StorageHelper.SystemKeyPrefix + "TaskID";

        private const string MainSectionName = KrResolutionActionVirtual.SectionName;

        /// <summary>
        /// Значение по умолчанию для параметра "Длительность, рабочие дни". Данное значение используется,
        /// только если в схеме не указано значение по умолчанию для поля <see cref="KrResolutionActionVirtual.Period"/>.
        /// </summary>
        private const double PeriodInDaysDefaultValue = 1d;

        /// <summary>
        /// Массив типов обрабатываемых сигналов.
        /// </summary>
        private static readonly string[] signalTypes =
        [
            WorkflowSignalTypes.CompleteTask,
            WorkflowSignalTypes.DeleteTask
        ];

        private readonly IKrTokenProvider tokenProvider = NotNullOrThrow(tokenProvider);

        private readonly IKrGetSqlPerformersStrategy getSqlPerformersStrategy = NotNullOrThrow(getSqlPerformersStrategy);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override bool Compile(
            IWorkflowCompilationSyntaxTreeBuilder builder,
            WorkflowActionStorage action)
        {
            ThrowIfNull(builder);
            ThrowIfNull(action);

            if (base.Compile(builder, action))
            {
                CompileEvents(builder, action);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(IWorkflowEngineContext context, IWorkflowEngineCompiled? scriptObject)
        {
            await base.ExecuteAsync(context, scriptObject);

            var currentTaskID = context.ActionInstance!.Hash.TryGet<Guid?>(TaskParamKey);
            var signalType = context.Signal!.Type;

            if (currentTaskID.HasValue)
            {
                context.Links.Clear();
            }

            using (context.CreateTasksContext())
            {
                if (signalType == WorkflowSignalTypes.Default)
                {
                    if (!currentTaskID.HasValue)
                    {
                        var sqlPerformerScript = await context.GetAsync<string>(
                            MainSectionName,
                            KrApprovalActionVirtual.SqlPerformersScript);

                        var sqlPerformers = await this.getSqlPerformersStrategy.GetAsync(
                            sqlPerformerScript,
                            context.ProcessInstance!.CardID,
                            context.ValidationResult,
                            context.CancellationToken);

                        if (!context.ValidationResult.IsSuccessful())
                        {
                            return;
                        }

                        var performersRows = await context.GetAllRowsAsync(KrWeRolesVirtual.SectionName) ?? Enumerable.Empty<Dictionary<string, object?>>();
                        var performers = WorkflowCommonHelper.CombinePerformers(
                            performersRows.Select(i => new RoleEntryStorage(
                                WorkflowEngineHelper.Get<Guid>(i, Names.Table_RowID),
                                WorkflowEngineHelper.Get<Guid>(i, WorkflowEngineHelper.WildCardHashMark, Names.Table_ID),
                                WorkflowEngineHelper.Get<string>(i, WorkflowEngineHelper.WildCardHashMark, Table_Field_Name) ?? string.Empty)),
                            sqlPerformers,
                            SqlApproverRoleID);

                        await this.SendTaskAsync(context, performers);

                        foreach (var newSignalType in signalTypes)
                        {
                            CreateSubscription(context, newSignalType);
                        }
                    }

                    context.Links.Clear();
                }
                else if (currentTaskID.HasValue)
                {
                    switch (signalType)
                    {
                        case WorkflowSignalTypes.CompleteTask:
                            var taskSignal = context.Signal.As<WorkflowEngineTaskSignal>();
                            if (taskSignal.Hash.TryGetValue(NamesKeys.ResolutionFinished, out var resolutionFinishedObj)
                                && (resolutionFinishedObj as bool? ?? false))
                            {
                                if (!taskSignal.TaskExists(currentTaskID.Value))
                                {
                                    break;
                                }

                                WorkflowEngineHelper.Set<object?>(context.ActionInstance!.Hash, default, TaskParamKey);
                                context.Links[Guid.Empty] = WorkflowEngineSignal.CreateDefaultSignal(StorageHelper.Clone(context.Signal!.Hash));
                            }
                            else
                            {
                                List<Guid> taskRowIDs;
                                await using (context.DbScope.Create())
                                {
                                    var db = context.DbScope.Db;
                                    taskRowIDs = await db
                                        .SetCommand(
                                            context.DbScope.BuilderFactory
                                                .With("ChildTaskHistory", e => e
                                                        .Select().C("t", "RowID").V(0).As("Level")
                                                        .From("TaskHistory", "t").NoLock()
                                                        .Where().C("t", "ParentRowID").Equals().P("RootRowID")
                                                        .UnionAll()
                                                        .Select().C("t", "RowID").C("p", "Level").Add(1)
                                                        .From("TaskHistory", "t").NoLock()
                                                        .InnerJoin("ChildTaskHistory", "p")
                                                        .On().C("p", "RowID").Equals().C("t", "ParentRowID"),
                                                    columnNames: ["RowID", "Level"],
                                                    recursive: true)
                                                .Select().C("h", "RowID")
                                                .From("ChildTaskHistory", "h")
                                                .OrderBy("h", "Level", SortOrder.Descending)
                                                .Build(),
                                            db.Parameter("RootRowID", currentTaskID.Value))
                                        .LogCommand()
                                        .ExecuteListAsync<Guid>(context.CancellationToken);
                                }

                                foreach (var taskRowID in taskRowIDs)
                                {
                                    await this.CompleteTaskAsync(context, scriptObject, taskRowID, true);
                                }
                            }

                            break;

                        case WorkflowSignalTypes.DeleteTask:
                        {
                            List<Guid> deleteTaskIDs;
                            await using (context.DbScope.Create())
                            {
                                var db = context.DbScope.Db;
                                deleteTaskIDs = await db
                                    .SetCommand(
                                        context.DbScope.BuilderFactory
                                            .With("ChildTaskHistory", e => e
                                                    .Select().C("t", "RowID").V(0).As("Level")
                                                    .From("TaskHistory", "t").NoLock()
                                                    .Where().C("t", "ParentRowID").Equals().P("RootRowID")
                                                    .UnionAll()
                                                    .Select().C("t", "RowID").C("p", "Level").Add(1)
                                                    .From("TaskHistory", "t").NoLock()
                                                    .InnerJoin("ChildTaskHistory", "p")
                                                    .On().C("p", "RowID").Equals().C("t", "ParentRowID"),
                                                columnNames: ["RowID", "Level"],
                                                recursive: true)
                                            .Select().C("h", "RowID")
                                            .From("ChildTaskHistory", "h")
                                            .OrderBy("h", "Level", SortOrder.Descending)
                                            .Build(),
                                        db.Parameter("RootRowID", currentTaskID.Value))
                                    .LogCommand()
                                    .ExecuteListAsync<Guid>(context.CancellationToken);
                            }

                            if (deleteTaskIDs.Any())
                            {
                                var mainCard = await context.GetMainCardAsync(context.CancellationToken);

                                if (mainCard is null)
                                {
                                    return;
                                }

                                foreach (var deleteTaskID in deleteTaskIDs)
                                {
                                    var task = mainCard.Tasks.FirstOrDefault(x => x.RowID == deleteTaskID);
                                    if (task is not null)
                                    {
                                        task.State = CardRowState.Deleted;
                                        context.AddTaskToNextContextTasks(task);
                                    }

                                    await this.DeleteTaskCoreAsync(context, deleteTaskID);
                                }

                                await this.DeleteTaskCoreAsync(context, currentTaskID.Value);

                                WorkflowEngineHelper.Set<object?>(context.ActionInstance.Hash, default, TaskParamKey);
                            }

                            break;
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override bool CheckActive(IWorkflowEngineContext context)
        {
            return context.ActionInstance!.Hash.TryGet<Guid?>(TaskParamKey).HasValue;
        }

        /// <inheritdoc/>
        protected override WorkflowNormalizationSettings GetNormalizationSettings()
        {
            return new WorkflowNormalizationSettings(
            [
                new WorkflowNormalizationSetting(
                    PlatformNormalizationSources.Roles,
                    KrWeRolesVirtual.SectionName,
                    KrWeRolesVirtual.Role,
                    "Name",
                    true),
                new WorkflowNormalizationSetting(
                    PlatformNormalizationSources.Roles,
                    KrResolutionActionVirtual.SectionName,
                    KrResolutionActionVirtual.Author,
                    "Name"),
                new WorkflowNormalizationSetting(
                    PlatformNormalizationSources.Roles,
                    KrResolutionActionVirtual.SectionName,
                    KrResolutionActionVirtual.Sender,
                    "Name"),
                new WorkflowNormalizationSetting(
                    DefaultNormalizationSources.TaskKinds,
                    KrResolutionActionVirtual.SectionName,
                    KrResolutionActionVirtual.Kind,
                    "Caption"),
                new WorkflowNormalizationSetting(
                    PlatformNormalizationSources.Roles,
                    KrResolutionActionVirtual.SectionName,
                    KrResolutionActionVirtual.Controller,
                    "Name"),
            ]);
        }

        #endregion

        #region WorkflowTaskActionBase Overrides

        /// <inheritdoc/>
        protected override Task<string?> GetResultAsync(
            IWorkflowEngineContext context,
            CardTask task)
        {
            return Task.FromResult(task.Result);
        }

        /// <inheritdoc/>
        protected override async Task CompleteTaskCoreAsync(
            IWorkflowEngineContext context,
            CardTask task,
            Guid optionID,
            IWorkflowEngineCompiled? scriptObject)
        {
            if (task.State == CardRowState.Deleted)
            {
                WorkflowEngineHelper.Set<object?>(context.ActionInstance!.Hash, default, TaskParamKey);

                context.Links[Guid.Empty] = WorkflowEngineSignal.CreateDefaultSignal(StorageHelper.Clone(context.Signal!.Hash));
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Асинхронно отправляет задание.
        /// </summary>
        /// <param name="context">Контекст обработчика.</param>
        /// <param name="roles">Коллекция ролей на которые должны быть отправлены задания.</param>
        /// <returns>Асинхронная задача.</returns>
        private async Task SendTaskAsync(IWorkflowEngineContext context, IReadOnlyList<RoleEntryStorage> roles)
        {
            var digest = await context.GetAsync<string>(
                MainSectionName,
                KrResolutionActionVirtual.Digest);

            var periodInDays = (double?) (await context.GetAsync<object>(
                        MainSectionName,
                        KrResolutionActionVirtual.Period)
                    ?? (await context.CardMetadata.GetSectionsAsync(context.CancellationToken))[MainSectionName]
                    .Columns[KrResolutionActionVirtual.Period]
                    .DefaultValue)
                ?? PeriodInDaysDefaultValue;
            var planned = await context.GetAsync<DateTime?>(
                MainSectionName,
                KrResolutionActionVirtual.Planned);

            var senderID = await context.GetAsync<Guid?>(
                MainSectionName,
                KrResolutionActionVirtual.Sender,
                Names.Table_ID);

            senderID = await context.GetAuthorIDAsync(this.RoleGetStrategy, this.ContextRoleManager, this.ContextRoleCache, senderID);
            if (!senderID.HasValue)
            {
                return;
            }

            var authorID = await context.GetAsync<Guid?>(
                MainSectionName,
                KrResolutionActionVirtual.Author,
                Names.Table_ID);
            if (authorID is null)
            {
                authorID = senderID;
            }
            else
            {
                authorID = await context.GetAuthorIDAsync(this.RoleGetStrategy, this.ContextRoleManager, this.ContextRoleCache, authorID);
                if (!authorID.HasValue)
                {
                    return;
                }
            }

            var kindID = await context.GetAsync<Guid?>(
                MainSectionName,
                KrResolutionActionVirtual.Kind,
                Names.Table_ID);
            var kindCaption = await context.GetAsync<string>(
                MainSectionName,
                KrResolutionActionVirtual.Kind,
                Table_Field_Caption);

            var withControl = await context.GetAsync<bool?>(MainSectionName, KrResolutionActionVirtual.WithControl) ?? false;

            Guid? controllerID;
            string? controllerName;
            if (withControl)
            {
                controllerID = await context.GetAsync<Guid?>(
                    MainSectionName,
                    KrResolutionActionVirtual.Controller,
                    Names.Table_ID);
                controllerName = await context.GetAsync<string>(
                    MainSectionName,
                    KrResolutionActionVirtual.Controller,
                    Table_Field_Name);
            }
            else
            {
                controllerID = default;
                controllerName = default;
            }

            var isMassCreation = await context.GetAsync<bool?>(MainSectionName, KrResolutionActionVirtual.IsMassCreation) ?? false;
            var isMajorPerformer = await context.GetAsync<bool?>(MainSectionName, KrResolutionActionVirtual.IsMajorPerformer) ?? false;

            digest = await GetWithPlaceholdersAsync(
                context,
                digest,
                context.Task);

            var task = new CardTask { TypeID = DefaultTaskTypes.WfResolutionProjectTypeID };

            var resolutionFields = task.Card.Sections.GetOrAddEntry(WfHelper.ResolutionSection).Fields;
            resolutionFields[WfHelper.ResolutionKindIDField] = kindID;
            resolutionFields[WfHelper.ResolutionKindCaptionField] = kindCaption;

            resolutionFields[WfHelper.ResolutionAuthorIDField] = authorID;
            resolutionFields[WfHelper.ResolutionSenderIDField] = senderID;

            resolutionFields[WfHelper.ResolutionWithControlField] = BooleanBoxes.Box(withControl);
            resolutionFields[WfHelper.ResolutionControllerIDField] = controllerID;
            resolutionFields[WfHelper.ResolutionControllerNameField] = controllerName;

            resolutionFields[WfHelper.ResolutionCommentField] = digest;

            if (planned.HasValue)
            {
                resolutionFields[WfHelper.ResolutionPlannedField] = planned;
                resolutionFields[WfHelper.ResolutionDurationInDaysField] = default;
            }
            else
            {
                resolutionFields[WfHelper.ResolutionDurationInDaysField] = DoubleBoxes.Box(periodInDays);
            }

            resolutionFields[WfHelper.ResolutionMassCreationField] = BooleanBoxes.Box(isMassCreation);
            resolutionFields[WfHelper.ResolutionMajorPerformerField] = BooleanBoxes.Box(isMajorPerformer);

            var performerRows = task.Card.Sections.GetOrAddTable(WfHelper.ResolutionPerformersSection).Rows;
            for (var i = 0; i < roles.Count; i++)
            {
                var performerRow = performerRows.Add();
                performerRow.RowID = Guid.NewGuid();
                performerRow.State = CardRowState.Inserted;

                var performer = roles[i];
                performerRow[WfHelper.ResolutionPerformerRoleIDField] = performer.ID;
                performerRow[WfHelper.ResolutionPerformerRoleNameField] = performer.Name;
                performerRow[WfHelper.ResolutionPerformerOrderField] = Int32Boxes.Box(i);
            }

            var mainCard = await context.GetMainCardAsync(context.CancellationToken);
            if (mainCard is null)
            {
                return;
            }

            var storedCard = mainCard.Clone();

            // Дайджест нужно получить до очистки секций и остальных полей карточки
            var cardDigest = await this.CardRepository.GetDigestAsync(storedCard, CardDigestEventNames.ActionHistoryStoreWorkflowEngineProcess, context.CancellationToken);

            var token = this.tokenProvider.CreateFullToken(storedCard);
            storedCard.Sections.Clear();
            storedCard.Files.Clear();
            storedCard.Tasks.Clear();
            storedCard.TaskHistory.Clear();
            storedCard.TaskHistoryGroups.Clear();
            storedCard.Info.Clear();
            storedCard.Permissions.Clear();
            token.Set(storedCard.Info);

            var taskRowID = Guid.NewGuid();

            var request = new CardStoreRequest { Card = storedCard }
                .SetStartingProcessTaskGroupRowID(context.ProcessInstance!.GetHistoryGroup())
                .SetStartingProcessNextTask(task)
                .SetStartingProcessName(WfHelper.ResolutionProcessName)
                .SetStartingProcessTaskRowID(taskRowID);
            if (cardDigest is not null)
            {
                request.SetDigest(cardDigest);
            }

            var response = await this.CardRepository.StoreAsync(request, context.CancellationToken);

            context.ValidationResult.Add(response.ValidationResult);
            if (!response.ValidationResult.IsSuccessful())
            {
                return;
            }

            await using (context.DbScope.Create())
            {
                var db = context.DbScope.Db;

                var settingsJson =
                    await db
                        .SetCommand(
                            context.DbScope.BuilderFactory
                                .Select()
                                .C("th", "Settings")
                                .From("TaskHistory", "th").NoLock()
                                .Where()
                                .C("th", "RowID").Equals().P("RowID")
                                .Build(),
                            db.Parameter("RowID", taskRowID))
                        .LogCommand()
                        .ExecuteStringAsync(context.CancellationToken);

                var settings =
                    (string.IsNullOrEmpty(settingsJson)
                        ? null
                        : StorageHelper.DeserializeFromTypedJson(settingsJson))
                    ?? new Dictionary<string, object?>(StringComparer.Ordinal);

                settings[TaskHistorySettingsKeys.ProcessID] = context.ProcessInstance!.ID;
                settings[TaskHistorySettingsKeys.ProcessName] = context.ProcessInstance.Name;
                settings[TaskHistorySettingsKeys.ProcessKind] = WorkflowEngineHelper.WorkflowEngineProcessName;

                await db
                    .SetCommand(
                        context.DbScope.BuilderFactory
                            .Update("TaskHistory")
                            .C("Settings").Assign().P("Settings")
                            .Where()
                            .C("RowID").Equals().P("RowID")
                            .Build(),
                        db.Parameter("RowID", taskRowID, DataType.Guid),
                        db.Parameter("Settings", StorageHelper.SerializeToTypedJson(settings), DataType.BinaryJson))
                    .LogCommand()
                    .ExecuteNonQueryAsync(context.CancellationToken);
            }

            context.ActionInstance!.Hash[TaskParamKey] = taskRowID;

            context.TaskSubscriptions.Add(new WorkflowTaskSubscriptionStorage(context.NodeInstance!.ID, context.ProcessInstance.ID, taskRowID));

            await context.AddActiveTaskAsync(
                taskRowID,
                context.ValidationResult,
                context.CancellationToken);
        }

        #endregion
    }
}
