#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Normalization;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
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

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    /// <summary>
    /// Обработчик действия <see cref="KrDescriptors.KrUniversalTaskDescriptor"/>.
    /// </summary>
    public sealed class KrUniversalTaskAction(IWorkflowTaskActionDeps deps)
        : KrWorkflowTaskActionBase(KrDescriptors.KrUniversalTaskDescriptor, deps)
    {
        #region Constants

        /// <summary>
        /// Имя ключа, по которому в параметрах действия содержится идентификатор обрабатываемого задания данным экземпляром действия. Тип значения: <see cref="Guid"/>.
        /// </summary>
        private const string TaskParamKey = StorageHelper.SystemKeyPrefix + "TaskID";

        private const string MainSectionName = KrUniversalTaskActionVirtual.SectionName;

        /// <summary>
        /// Значение по умолчанию для параметра "Длительность, рабочие дни". Данное значение используется только, если в схеме не указано значение по умолчанию для поля <see cref="KrUniversalTaskActionVirtual.Period"/>.
        /// </summary>
        private const double PeriodInDaysDefaultValue = 1d;

        #endregion

        #region Fields

        /// <summary>
        /// Идентификатор типа задания согласования.
        /// </summary>
        private static readonly Guid taskTypeID = DefaultTaskTypes.KrUniversalTaskTypeID;

        /// <summary>
        /// Массив типов обрабатываемых сигналов.
        /// </summary>
        private static readonly string[] signalTypes =
        [
            WorkflowSignalTypes.CompleteTask,
            WorkflowSignalTypes.DeleteTask,
            WorkflowSignalTypes.UpdateTask,
        ];

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
        public override void PrepareForExecute(WorkflowActionStateStorage actionState, IWorkflowEngineContext context)
        {
            ThrowIfNull(actionState);

            base.PrepareForExecute(actionState, context);
            actionState.Hash.Remove(KrUniversalTaskActionButtonsVirtual.SectionName);
            actionState.Hash.Remove(WorkflowActionTypes.TaskRolesSectionName);
            actionState.Hash.Remove(WorkflowActionTypes.TaskRoleRolesSectionName);
            actionState.Hash.Remove(WorkflowActionTypes.NotificationsSectionName);
            actionState.Hash.Remove(WorkflowActionTypes.NotificationTaskRolesSectionName);
            actionState.Hash.Remove(WorkflowActionTypes.CompletionNotificationsSectionName);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Схема выполнения действия задания:<para/>
        /// 1. Получаем ID привязанного к данному действию заданию<para/>
        /// 2. Если есть задание, то очищаем список переходов<para/>
        /// 3. Если тип сигнала - default<para/>
        /// 3.1. Если задания нет, создаём задание, создаём все необходимые подписки<para/>
        /// 3.2. Если задание есть, игнорируем создание задания<para/>
        /// 3.3. В любом случае очищаем список переходов<para/>
        /// 4. Если тип сигнала из списка обрабатываемых типов сигналов<para/>
        /// 4.1. Если задания нет, игнорируем<para/>
        /// 4.2. Если задание есть, обрабатываем сигнал<para/>
        /// 4.3. Если по окончанию обработки задания оно все еще есть, ставим KeepAlive = true.
        /// </remarks>
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
                        await this.SendTaskAsync(context, scriptObject);

                        foreach (var newSignalType in signalTypes)
                        {
                            CreateSubscription(context, newSignalType);
                        }

                        SubscribeOnEvents(context);
                    }

                    context.Links.Clear();
                }
                else if (currentTaskID.HasValue)
                {
                    switch (signalType)
                    {
                        case WorkflowSignalTypes.CompleteTask:
                            await this.CompleteTaskAsync(context, scriptObject, currentTaskID.Value);
                            break;

                        case WorkflowSignalTypes.DeleteTask:
                            if (await this.DeleteTaskAsync(context, currentTaskID.Value))
                            {
                                WorkflowEngineHelper.Set<object>(context.ActionInstance.Hash, null, TaskParamKey);
                            }

                            break;

                        case WorkflowSignalTypes.UpdateTask:
                            await this.UpdateTaskAsync(context, currentTaskID.Value);
                            break;

                        default:
                            await this.PerformEvent(context, scriptObject, currentTaskID.Value);
                            break;
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override WorkflowNormalizationSettings GetNormalizationSettings()
        {
            return new WorkflowNormalizationSettings(
            [
                new WorkflowNormalizationSetting(
                    DefaultNormalizationSources.TaskKinds,
                    KrUniversalTaskActionVirtual.SectionName,
                    KrUniversalTaskActionVirtual.Kind,
                    "Caption")
            ]);
        }

        #endregion

        #region WorkflowTaskActionBase Overrides

        /// <inheritdoc/>
        protected override async Task<string?> GetResultAsync(
            IWorkflowEngineContext context,
            CardTask task)
        {
            return
                await GetWithPlaceholdersAsync(
                    context,
                    await context.GetAsync<string>(MainSectionName, KrUniversalTaskActionVirtual.Result),
                    task);
        }

        /// <inheritdoc/>
        protected override async Task CompleteTaskCoreAsync(
            IWorkflowEngineContext context,
            CardTask task,
            Guid optionID,
            IWorkflowEngineCompiled? scriptObject)
        {
            var resultOptionID = await CompleteTaskPredefinedActionProcessingAsync(
                task,
                optionID);

            // Вариант завершения был обработан?
            if (resultOptionID.HasValue)
            {
                optionID = resultOptionID.Value;

                var optionRows = await context.GetAllRowsAsync(context.ActionTemplate!.Hash, KrUniversalTaskActionButtonsVirtual.SectionName);

                if (optionRows?.Count > 0)
                {
                    var optionsRows = optionRows
                        .Where(x => WorkflowEngineHelper.Get<Guid>(x, KrUniversalTaskActionButtonsVirtual.OptionID) == optionID);

                    var linksForPerforming = new HashSet<Guid>();
                    var taskRolesRows = await context.GetAllRowsAsync(KrUniversalTaskActionButtonTaskRolesVirtual.SectionName);

                    foreach (var optionRow in optionsRows)
                    {
                        var rowID = WorkflowEngineHelper.Get<Guid>(optionRow, Names.Table_RowID);

                        var taskRoleIDs = new List<Guid>();
                        if (taskRolesRows is not null)
                        {
                            foreach (var taskRoleRow in taskRolesRows)
                            {
                                if (WorkflowEngineHelper.Get<Guid?>(taskRoleRow, KrUniversalTaskActionButtonTaskRolesVirtual.TaskButton, "RowID") == rowID)
                                {
                                    var taskRoleID = WorkflowEngineHelper.Get<Guid?>(taskRoleRow, KrUniversalTaskActionButtonTaskRolesVirtual.TaskRole, "ID");
                                    if (taskRoleID.HasValue)
                                    {
                                        taskRoleIDs.Add(taskRoleID.Value);
                                    }
                                }
                            }
                        }

                        if (taskRoleIDs.All(p => task.TaskSessionRoles.All(q => q.FunctionRoleID != p)))
                        {
                            context.ValidationResult.AddError(this,
                                await LocalizeFormatAsync("$WorkflowEngine_Actions_InvalidCompletionOptionFunctionRoles", optionID));
                            return;
                        }

                        var completeTaskNotificationRows =
                            await context.GetAllRowsAsync(context.ActionTemplate.Hash, WorkflowActionTypes.CompletionNotificationsSectionName);
                        List<WorkflowTaskNotificationInfo>? notificationInfos = null;
                        if (completeTaskNotificationRows is not null)
                        {
                            notificationInfos = [];
                            foreach (var row in completeTaskNotificationRows.Where(x => WorkflowEngineHelper.Get<Guid>(x, "TaskButton", "RowID") == rowID))
                            {
                                var notificationInfo =
                                    new WorkflowTaskNotificationInfo(
                                        context,
                                        row,
                                        "TaskCompletionNotifications",
                                        notificationParentColumnName: "CompletionNotification",
                                        notificationID: await context.GetAsync<Guid?>(row, "Notification", "ID"),
                                        excludeDeputies: await context.GetAsync<bool?>(row, "ExcludeDeputies"),
                                        excludeSubscribers: await context.GetAsync<bool?>(row, "ExcludeSubscribers"));
                                notificationInfos.Add(notificationInfo);
                            }
                        }

                        if (scriptObject is not null)
                        {
                            await scriptObject.ExecuteActionAsync(
                                KrWorkflowActionMethods.KrUniversalTaskOptionMethod.GetMethodName(optionRow),
                                KrWorkflowActionMethods.KrUniversalTaskOptionMethod,
                                task,
                                task.Card.DynamicEntries, task.Card.DynamicTables,
                                notificationInfos?.FirstOrDefault(),
                                notificationInfos);
                        }

                        if (notificationInfos?.Count > 0)
                        {
                            await this.SendCompleteTaskNotificationsAsync(
                                context,
                                scriptObject,
                                task,
                                notificationInfos);
                        }

                        if (!context.Cancel)
                        {
                            var linkRows = context.ActionTemplate.Hash.TryGet<IList>(KrUniversalTaskActionButtonLinksVirtual.SectionName);

                            var linkIDs =
                                linkRows?
                                    .Cast<Dictionary<string, object>?>()
                                    .Where(x => WorkflowEngineHelper.Get<Guid?>(x!, KrUniversalTaskActionButtonLinksVirtual.Button, Names.Table_RowID) == rowID)
                                    .Select(x => WorkflowEngineHelper.Get<Guid>(x!, KrUniversalTaskActionButtonLinksVirtual.Link, Names.Table_ID))
                                ?? [];

                            foreach (var linkID in linkIDs)
                            {
                                linksForPerforming.Add(linkID);
                            }
                        }

                        context.Cancel = false;
                    }

                    foreach (var linkID in linksForPerforming)
                    {
                        context.Links[linkID] = WorkflowEngineSignal.CreateDefaultSignal(StorageHelper.Clone(context.Signal!.Hash));
                    }
                }
            }

            if (task.State == CardRowState.Deleted)
            {
                WorkflowEngineHelper.Set<object>(context.ActionInstance!.Hash, null, TaskParamKey);
            }
        }

        /// <inheritdoc/>
        protected override bool CheckActive(IWorkflowEngineContext context)
        {
            return context.ActionInstance!.Hash.TryGet<Guid?>(TaskParamKey).HasValue;
        }

        /// <inheritdoc/>
        public override ValueTask<ValidationResult> ValidateAsync(
            WorkflowActionStorage action,
            WorkflowNodeStorage node,
            WorkflowProcessStorage process,
            CancellationToken cancellationToken = default)
        {
            var validationResult = new ValidationResultBuilder();
            if (WorkflowEngineHelper.Get<object>(action.Hash, MainSectionName, KrUniversalTaskActionVirtual.Role, Names.Table_ID) is null)
            {
                validationResult.AddWarning(
                    this,
                    WorkflowEngineHelper.GetValidateFieldMessage(action, node, "$CardTypes_Controls_Role"));
            }

            var list = WorkflowEngineHelper.Get<IList>(action.Hash, KrUniversalTaskActionButtonsVirtual.SectionName);
            if (list is null
                || list.Count == 0)
            {
                validationResult.AddWarning(
                    this,
                    WorkflowEngineHelper.GetValidateFieldMessage(
                        action,
                        node,
                        "$CardTypes_Controls_CompletionOptions",
                        template: "$WorkflowEngine_Actions_TableEmptyTemplate"));
            }

            return ValueTask.FromResult(validationResult.Build());
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Обрабатывает завершение заданий в соответствии с предопределённой логикой обработки.
        /// </summary>
        /// <param name="task">Завершаемое задание.</param>
        /// <param name="optionID">Идентификатор варианта завершения с которым завершается задание. Может отличаться от <see cref="CardTask.OptionID"/> из <paramref name="task"/>, если он был изменён в скрипте, обрабатывающим завершение задания.</param>
        /// <returns>Идентификатор варианта завершения соответствующий выбранному настраиваемому варианту завершения или значение по умолчанию для типа, если задание не было завершено или произошла ошибка при обработке.</returns>
        private static async Task<Guid?> CompleteTaskPredefinedActionProcessingAsync(
            CardTask task,
            Guid? optionID)
        {
            if (task.TypeID == taskTypeID)
            {
                if (optionID == DefaultCompletionOptions.Approve)
                {
                    optionID = task.Info.TryGet<Guid?>(KrUniversalTaskStoreExtension.OptionIDKey);
                }

                var taskSections = task.Card.Sections;
                var completionResult = taskSections.GetOrAdd(KrTask.Name).Fields.TryGet<string>(KrTask.Comment);

                if (!string.IsNullOrWhiteSpace(completionResult))
                {
                    task.Result = completionResult;
                }

                return optionID;
            }

            return null;
        }

        /// <summary>
        /// Асинхронно отправляет задание.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="scriptObject"><inheritdoc cref="IWorkflowEngineCompiled" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        private async Task SendTaskAsync(IWorkflowEngineContext context, IWorkflowEngineCompiled? scriptObject)
        {
            var buttonRows = await context.GetAllRowsAsync(context.ActionTemplate!.Hash, KrUniversalTaskActionButtonsVirtual.SectionName);
            var allButtonTaskRoles = await context.GetAllRowsAsync(context.ActionTemplate.Hash, KrUniversalTaskActionButtonTaskRolesVirtual.SectionName);
            var taskAssignedRoles = await WorkflowEngineHelper.GetTaskAssignedRolesAsync(context, scriptObject);

            if (!(buttonRows?.Count > 0))
            {
                context.ValidationResult.AddError(
                    this,
                    WorkflowEngineHelper.GetValidateFieldMessage(
                        context.ActionTemplate,
                        context.NodeTemplate!,
                        "$CardTypes_Controls_CompletionOptions",
                        template: "$WorkflowEngine_Actions_TableEmptyTemplate"));
            }

            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            var digest = await context.GetAsync<string>(MainSectionName, KrUniversalTaskActionVirtual.Digest);

            var periodInDays = (double?) (await context.GetAsync<object>(MainSectionName, KrUniversalTaskActionVirtual.Period)
                    ?? (await context.CardMetadata.GetSectionsAsync(context.CancellationToken))[MainSectionName]
                    .Columns[KrUniversalTaskActionVirtual.Period]
                    .DefaultValue)
                ?? PeriodInDaysDefaultValue;
            var planned = await context.GetAsync<DateTime?>(MainSectionName, KrUniversalTaskActionVirtual.Planned);

            var parentTaskRowID = context.Signal!.As<WorkflowEngineTaskSignal>().ParentTaskRowID;

            digest = await GetWithPlaceholdersAsync(
                context,
                digest,
                context.Task);

            var cardTask =
                await context.SendTaskAsync(
                    taskTypeID,
                    digest,
                    planned,
                    null,
                    periodInDays,
                    null,
                    null,
                    parentRowID: parentTaskRowID,
                    cancellationToken: context.CancellationToken);

            if (cardTask is null
                || !context.ValidationResult.IsSuccessful())
            {
                return;
            }

            var kindID = await context.GetAsync<Guid?>(MainSectionName, KrUniversalTaskActionVirtual.Kind, Names.Table_ID);
            var kindCaption = await context.GetAsync<string>(MainSectionName, KrUniversalTaskActionVirtual.Kind, Table_Field_Caption);

            context.ValidationResult.Add(WorkflowCommonHelper.SetTaskKind(cardTask, kindID, kindCaption, this));

            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            if (await context.GetAsync<bool?>(MainSectionName, KrUniversalTaskActionVirtual.CanEditCard) is true)
            {
                cardTask.Settings![WorkflowCommonConstants.CanEditCard] = BooleanBoxes.True;
            }

            if (await context.GetAsync<bool?>(MainSectionName, KrUniversalTaskActionVirtual.CanEditAnyFiles) is true)
            {
                cardTask.Settings![WorkflowCommonConstants.CanEditAnyFiles] = BooleanBoxes.True;
            }

            cardTask.HistorySettings ??= [];
            cardTask.HistorySettings[TaskHistorySettingsKeys.ProcessKind] = cardTask.TryGetInfo()?.TryGet<string>(CardHelper.TaskProcessKindKey);

            foreach (var role in taskAssignedRoles)
            {
                cardTask.AddRole(
                    role.RoleID,
                    role.RoleName,
                    role.TaskRoleID,
                    null,
                    role.ShowInTaskDetails,
                    role.Master);
            }

            var optionsSection = cardTask.Card.Sections.GetOrAddTable(KrUniversalTaskOptions.Name);

            foreach (var buttonRow in buttonRows!)
            {
                var buttonRowID = WorkflowEngineHelper.Get<Guid>(buttonRow, "RowID");
                var buttonCaption = WorkflowEngineHelper.Get<object>(buttonRow, KrUniversalTaskActionButtonsVirtual.Caption);
                var currentButtonTaskRoles =
                    allButtonTaskRoles?.Where(p =>
                        WorkflowEngineHelper.Get<Guid>(p, KrUniversalTaskActionButtonTaskRolesVirtual.TaskButton, "RowID") == buttonRowID).ToList();

                if (!(currentButtonTaskRoles?.Count > 0))
                {
                    context.ValidationResult.AddError(
                        this,
                        await LocalizeFormatAsync(
                            "$WorkflowEngine_Actions_ButtonFunctionRolesIsEmpty",
                            context.ActionTemplate.Name,
                            context.NodeTemplate!.GetObjectName(),
                            buttonCaption));

                    // Соберём все ошибки для всех кнопок завершения, а после прервёмся, если !context.ValidationResult.IsSuccessful()
                    continue;
                }

                var settings =
                    new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        [Keys.OptionFunctionRoles] =
                            currentButtonTaskRoles
                                .Select(p => WorkflowEngineHelper.Get<Guid>(p, KrUniversalTaskActionButtonTaskRolesVirtual.TaskRole, "ID"))
                                .ToList()
                    };

                var newRow = optionsSection.Rows.Add();
                newRow.RowID = Guid.NewGuid();
                newRow[KrUniversalTaskOptions.OptionID] = WorkflowEngineHelper.Get<object>(buttonRow, KrUniversalTaskActionButtonsVirtual.OptionID);
                newRow[KrUniversalTaskOptions.Caption] = buttonCaption;
                newRow[KrUniversalTaskOptions.ShowComment] =
                    BooleanBoxes.Box(WorkflowEngineHelper.Get<bool?>(buttonRow, KrUniversalTaskActionButtonsVirtual.IsShowComment) ?? default);
                newRow[KrUniversalTaskOptions.Additional] =
                    BooleanBoxes.Box(WorkflowEngineHelper.Get<bool?>(buttonRow, KrUniversalTaskActionButtonsVirtual.IsAdditionalOption) ?? default);
                newRow[KrUniversalTaskOptions.Order] = Int32Boxes.Box(WorkflowEngineHelper.Get<int?>(buttonRow, KrUniversalTaskActionButtonsVirtual.Order) ?? default);
                newRow[KrUniversalTaskOptions.Message] = WorkflowEngineHelper.Get<string>(buttonRow, KrUniversalTaskActionButtonsVirtual.Digest);
                newRow[KrUniversalTaskOptions.Settings] = StorageHelper.SerializeToTypedJson(settings);
                newRow.State = CardRowState.Inserted;
            }

            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            context.AddTaskToNextContextTasks(cardTask);
            context.ActionInstance!.Hash[TaskParamKey] = cardTask.RowID;
            await context.AddActiveTaskAsync(
                cardTask.RowID,
                context.ValidationResult,
                context.CancellationToken);
            await context.AddToHistoryAsync(
                cardTask.RowID,
                WorkflowHelper.GetProcessCycle(context.ProcessInstance!.Hash),
                context.ValidationResult,
                cancellationToken: context.CancellationToken);

            if (scriptObject is not null)
            {
                await scriptObject.ExecuteActionAsync(
                    KrWorkflowActionMethods.KrUniversalTaskInitMethod.MethodName,
                    KrWorkflowActionMethods.KrUniversalTaskInitMethod,
                    cardTask,
                    cardTask.Card.DynamicEntries,
                    cardTask.Card.DynamicTables);
            }

            if (cardTask.TaskAssignedRoles.All(p => p.TaskRoleID != CardFunctionRoles.PerformerID))
            {
                context.ValidationResult.AddError(
                    this,
                    WorkflowEngineHelper.GetValidateFieldMessage(context.ActionTemplate, context.NodeTemplate!, "$CardTypes_Controls_TaskFunctionRoles"));
                return;
            }

            await this.SendStartTaskNotificationsAsync(
                context,
                scriptObject,
                cardTask,
                WorkflowActionTypes.NotificationsSectionName);
        }

        #endregion
    }
}
