#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Normalization;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
using Tessa.Normalization;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Scheme;
using Tessa.Workflow;
using Tessa.Workflow.Actions;
using Tessa.Workflow.Actions.Descriptors;
using Tessa.Workflow.Compilation;
using Tessa.Workflow.Helpful;
using Tessa.Workflow.Normalization;
using Tessa.Workflow.Signals;
using Tessa.Workflow.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    /// <summary>
    /// Обработчик действия <see cref="KrDescriptors.KrSigningDescriptor"/>.
    /// </summary>
    public sealed class KrSigningAction(
        IWorkflowTaskActionDeps deps,
        IKrWorkflowStateStrategy stateStrategy,
        IKrTaskManagerContextFactory krTaskManagerContextFactory,
        IKrTaskManagerDataProviderFactory krTaskManagerDataProviderFactory,
        IKrSigningTaskManager krSigningTaskManager,
        IKrEditInterjectTaskManager krEditInterjectTaskManager)
        : KrWorkflowMultiTaskActionBase(KrDescriptors.KrSigningDescriptor, deps)
    {
        #region Constants And Static Fields

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

        #region Fields

        private readonly IKrWorkflowStateStrategy stateStrategy = NotNullOrThrow(stateStrategy);
        private readonly IKrTaskManagerContextFactory krTaskManagerContextFactory = NotNullOrThrow(krTaskManagerContextFactory);
        private readonly IKrTaskManagerDataProviderFactory krTaskManagerDataProviderFactory = NotNullOrThrow(krTaskManagerDataProviderFactory);
        private readonly IKrSigningTaskManager krSigningTaskManager = NotNullOrThrow(krSigningTaskManager);
        private readonly IKrEditInterjectTaskManager krEditInterjectTaskManager = NotNullOrThrow(krEditInterjectTaskManager);

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
                CompileEvents(
                    builder,
                    action);

                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override void PrepareForExecute(
            WorkflowActionStateStorage actionState,
            IWorkflowEngineContext context)
        {
            ThrowIfNull(actionState);

            base.PrepareForExecute(actionState, context);
            actionState.Hash.Remove(WorkflowConstants.KrSigningActionOptionsVirtual.SectionName);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Схема выполнения действия:<para/>
        /// 1. Получаем список идентификаторов обрабатываемых действием заданий;<para/>
        /// 2. Если задания есть, то очищаем список переходов;<para/>
        /// 3. Если тип сигнала - default, то:<para/>
        /// 3.1. Очищаем список переходов.<para/>
        /// 3.1. Если заданий нет, то:<para/>
        /// 3.1.1 Есть исполнители, то создаем задание и все необходимые подписки;<para/>
        /// 3.1.2. Нет исполнителей, то завершаем действие с вариантом завершения <see cref="ActionCompletionOptions.Signed"/>;
        /// 3.2. Если задания есть, игнорируем создание задания;<para/>
        /// 4. Если тип сигнала из списка обрабатываемых типов сигналов, то:<para/>
        /// 4.1. Если заданий нет, то игнорируем;<para/>
        /// 4.2. Если задания есть, то для каждого задания обрабатываем сигнал.<para/>
        /// </remarks>
        protected override async Task ExecuteAsync(
            IWorkflowEngineContext context,
            IWorkflowEngineCompiled? scriptObject)
        {
            await base.ExecuteAsync(context, scriptObject);

            var taskIDList = GetProcessingTaskIDList(context);
            var hasTaskIDList = taskIDList?.Count > 0;

            if (hasTaskIDList)
            {
                context.Links.Clear();
            }

            using (context.CreateTasksContext())
            {
                var signalType = context.Signal!.Type;
                if (signalType == WorkflowSignalTypes.Default)
                {
                    context.Links.Clear();

                    if (!hasTaskIDList)
                    {
                        var taskManagerContext = this.CreateTaskManagerContext(context);
                        var krSigningTaskManagerDataProvider =
                            this.CreateSigningTaskManagerDataProvider(
                                context,
                                scriptObject);

                        await this.krSigningTaskManager.StartAsync(
                            taskManagerContext,
                            krSigningTaskManagerDataProvider);

                        if (!context.ValidationResult.IsSuccessful())
                        {
                            return;
                        }

                        foreach (var newSignalType in signalTypes)
                        {
                            CreateSubscription(
                                context,
                                newSignalType);
                        }

                        SubscribeOnEvents(context);
                    }
                }
                else if (hasTaskIDList)
                {
                    switch (signalType)
                    {
                        case WorkflowSignalTypes.CompleteTask:
                            foreach (var taskID in taskIDList!.Cast<Guid>().ToArray())
                            {
                                await this.CompleteTaskAsync(
                                    context,
                                    scriptObject,
                                    taskID);
                            }

                            break;

                        case WorkflowSignalTypes.DeleteTask:
                            var taskManagerContext = this.CreateTaskManagerContext(context);

                            foreach (var taskID in taskIDList!.Cast<Guid>().ToArray())
                            {
                                await this.DeleteTaskCoreAsync(
                                    context,
                                    scriptObject,
                                    taskManagerContext,
                                    taskID);
                            }

                            break;

                        case WorkflowSignalTypes.UpdateTask:
                            foreach (var taskID in taskIDList!.Cast<Guid>())
                            {
                                await this.UpdateTaskAsync(
                                    context,
                                    taskID);
                            }

                            break;

                        default:
                            foreach (var taskID in taskIDList!.Cast<Guid>())
                            {
                                await this.PerformEvent(
                                    context,
                                    scriptObject,
                                    taskID);
                            }

                            break;
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override async Task<string?> GetResultAsync(
            IWorkflowEngineContext context,
            CardTask task)
        {
            if (task.TypeID == DefaultTaskTypes.KrSigningTypeID)
            {
                return await GetWithPlaceholdersAsync(
                    context,
                    await context.GetAsync<string>(
                        WorkflowConstants.KrSigningActionVirtual.SectionName,
                        WorkflowConstants.KrSigningActionVirtual.Result),
                    task);
            }

            return task.Result;
        }

        /// <inheritdoc/>
        protected override async Task CompleteTaskCoreAsync(
            IWorkflowEngineContext context,
            CardTask task,
            Guid _,
            IWorkflowEngineCompiled? scriptObject)
        {
            var actionOptionID = await this.CompleteTaskPredefinedActionProcessingAsync(
                context,
                scriptObject,
                task);

            await this.CompleteTaskScriptAsync(
                context,
                task,
                scriptObject);

            if (actionOptionID.HasValue
                && !context.Cancel)
            {
                await this.CompleteActionAsync(
                    context,
                    scriptObject,
                    actionOptionID.Value);
            }

            context.Cancel = false;

            if (task.State == CardRowState.Deleted)
            {
                GetProcessingTaskIDList(context)?.Remove(task.RowID);
            }
        }
        
        /// <inheritdoc/>
        protected override WorkflowNormalizationSettings GetNormalizationSettings()
        {
            return new WorkflowNormalizationSettings(
            [
                new WorkflowNormalizationSetting(
                    PlatformNormalizationSources.Roles,
                    WorkflowConstants.KrWeRolesVirtual.SectionName,
                    WorkflowConstants.KrWeRolesVirtual.Role,
                    "Name",
                    true),
                new WorkflowNormalizationSetting(
                    PlatformNormalizationSources.Roles,
                    WorkflowConstants.KrSigningActionVirtual.SectionName,
                    WorkflowConstants.KrSigningActionVirtual.Author,
                    "Name"),
                new WorkflowNormalizationSetting(
                    DefaultNormalizationSources.TaskKinds,
                    WorkflowConstants.KrSigningActionVirtual.SectionName,
                    WorkflowConstants.KrSigningActionVirtual.Kind,
                    "Caption"),
                new WorkflowNormalizationSetting(
                    PlatformNormalizationSources.Roles,
                    WorkflowConstants.KrWeEditInterjectOptionsVirtual.SectionName,
                    WorkflowConstants.KrWeEditInterjectOptionsVirtual.Role,
                    "Name"),
                new WorkflowNormalizationSetting(
                    PlatformNormalizationSources.Roles,
                    WorkflowConstants.KrWeEditInterjectOptionsVirtual.SectionName,
                    WorkflowConstants.KrWeEditInterjectOptionsVirtual.Author,
                    "Name"),
                new WorkflowNormalizationSetting(
                    DefaultNormalizationSources.TaskKinds,
                    WorkflowConstants.KrWeEditInterjectOptionsVirtual.SectionName,
                    WorkflowConstants.KrWeEditInterjectOptionsVirtual.Kind,
                    "Caption")
            ]);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Обрабатывает завершение заданий в соответствии с предопределённой логикой обработки.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="scriptObject"><inheritdoc cref="IWorkflowEngineCompiled" path="/summary"/></param>
        /// <param name="task">Завершаемое задание.</param>
        /// <returns>
        /// Идентификатор варианта завершения действия, для которого должны быть обработаны связи, выполняющиеся после завершения обработки этого действия, или значение <see langword="null"/>, если действие не завершается.
        /// </returns>
        private async Task<Guid?> CompleteTaskPredefinedActionProcessingAsync(
            IWorkflowEngineContext context,
            IWorkflowEngineCompiled? scriptObject,
            CardTask task)
        {
            if (this.krSigningTaskManager.TaskTypeIDSet.Contains(task.TypeID))
            {
                var taskManagerContext = this.CreateTaskManagerContext(context);
                var krApprovalTaskManagerDataProvider =
                    this.CreateSigningTaskManagerDataProvider(
                        context,
                        scriptObject,
                        task.RowID);

                var actionOptionID = await this.krSigningTaskManager.CompleteTaskAsync(
                    taskManagerContext,
                    krApprovalTaskManagerDataProvider,
                    task);

                if (task.TypeID == DefaultTaskTypes.KrSigningTypeID)
                {
                    if (actionOptionID == KrTaskManagerCompletionOptions.PositiveResult)
                    {
                        return ActionCompletionOptions.Signed;
                    }

                    if (actionOptionID == KrTaskManagerCompletionOptions.NegativeResult)
                    {
                        return ActionCompletionOptions.Declined;
                    }

                    if (actionOptionID == KrTaskManagerCompletionOptions.EditAfterPositiveResult)
                    {
                        var editInterjectTaskManagerDataProvider =
                            this.CreateEditInterjectTaskManagerDataProvider(
                                context,
                                scriptObject,
                                task);

                        await this.krEditInterjectTaskManager.StartAsync(
                            taskManagerContext,
                            editInterjectTaskManagerDataProvider);

                        return null;
                    }
                }

                return null;
            }

            if (task.TypeID == this.krEditInterjectTaskManager.TaskTypeID)
            {
                var taskManagerContext = this.CreateTaskManagerContext(context);

                var editInterjectTaskManagerDataProvider =
                    this.CreateEditInterjectTaskManagerDataProvider(
                        context,
                        scriptObject,
                        null,
                        task.RowID);

                var actionOptionID = await this.krEditInterjectTaskManager.CompleteTaskAsync(
                    taskManagerContext,
                    editInterjectTaskManagerDataProvider,
                    task);

                if (actionOptionID == KrTaskManagerCompletionOptions.Complete)
                {
                    return ActionCompletionOptions.Signed;
                }
            }

            return null;
        }

        /// <summary>
        /// Обрабатывает скрипт завершения задания и отправляет уведомления.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="task">Завершаемое задание.</param>
        /// <param name="scriptObject"><inheritdoc cref="IWorkflowEngineCompiled" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        private async Task CompleteTaskScriptAsync(
            IWorkflowEngineContext context,
            CardTask task,
            IWorkflowEngineCompiled? scriptObject)
        {
            var optionRows = await context.GetAllRowsAsync(context.ActionTemplate!.Hash, WorkflowConstants.KrSigningActionOptionsVirtual.SectionName);

            if (optionRows?.Count > 0)
            {
                var taskTypeID = task.TypeID;
                var optionID = task.OptionID;

                var processRows = optionRows
                    .Where(x =>
                        WorkflowEngineHelper.Get<Guid?>(
                            x,
                            WorkflowConstants.ActionSeveralTaskTypesOptionsBase.TaskType,
                            Names.Table_ID) == taskTypeID
                        && WorkflowEngineHelper.Get<Guid?>(
                            x,
                            WorkflowConstants.ActionOptionsBase.Option,
                            Names.Table_ID) == optionID);

                foreach (var processRow in processRows)
                {
                    var completionResult = processRow.TryGet<string>(WorkflowConstants.ActionSeveralTaskTypesOptionsBase.Result);
                    if (!string.IsNullOrWhiteSpace(completionResult))
                    {
                        if (this.BindingParser.IsBinding(completionResult))
                        {
                            completionResult = await this.BindingExecutor.GetAsync<string>(
                                context,
                                completionResult);
                        }

                        task.Result = await GetWithPlaceholdersAsync(
                            context,
                            completionResult,
                            task);
                    }

                    var info = new WorkflowTaskNotificationInfo(
                        context,
                        processRow,
                        WorkflowConstants.ActionNotificationRolesBase.Option,
                        WorkflowConstants.KrSigningActionNotificationRolesVirtual.SectionName);

                    if (scriptObject is not null)
                    {
                        await scriptObject.ExecuteActionAsync(
                            KrWorkflowActionMethods.KrSigningOptionMethod.GetMethodName(processRow),
                            KrWorkflowActionMethods.KrSigningOptionMethod,
                            task,
                            task.Card.DynamicEntries, task.Card.DynamicTables,
                            info,
                            new List<WorkflowTaskNotificationInfo> { info });
                    }

                    await this.SendCompleteTaskNotificationAsync(
                        context,
                        scriptObject,
                        task,
                        info);
                }
            }
        }

        /// <summary>
        /// Обрабатывает завершение действия.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="scriptObject"><inheritdoc cref="IWorkflowEngineCompiled" path="/summary"/></param>
        /// <param name="actionOptionID">Идентификатор варианта завершения действия.</param>
        /// <returns>Асинхронная задача.</returns>
        private async Task CompleteActionAsync(
            IWorkflowEngineContext context,
            IWorkflowEngineCompiled? scriptObject,
            Guid actionOptionID)
        {
            if (await context.GetAsync<bool?>(WorkflowConstants.KrSigningActionVirtual.SectionName, WorkflowConstants.KrSigningActionVirtual.ChangeStateOnEnd) is true)
            {
                await this.stateStrategy.SetStateIDAsync(
                    context,
                    actionOptionID == ActionCompletionOptions.Signed
                        ? KrState.Signed
                        : KrState.Declined,
                    context.ValidationResult,
                    context.CancellationToken);
            }

            var optionActionRows = await context.GetAllRowsAsync(context.ActionTemplate!.Hash, WorkflowConstants.KrSigningActionOptionsActionVirtual.SectionName);

            if (optionActionRows?.Count > 0)
            {
                var processRows = optionActionRows
                    .Where(x => WorkflowEngineHelper.Get<Guid?>(
                        x, WorkflowConstants.ActionOptionsActionBase.ActionOption, Names.Table_ID) == actionOptionID);

                var linksForPerforming = new HashSet<Guid>();

                foreach (var processRow in processRows)
                {
                    var info = new WorkflowTaskNotificationInfo(
                        context,
                        processRow,
                        WorkflowConstants.KrSigningActionNotificationActionRolesVirtual.Option,
                        WorkflowConstants.KrSigningActionNotificationActionRolesVirtual.SectionName);

                    if (scriptObject is not null)
                    {
                        await scriptObject.ExecuteActionAsync(
                            KrWorkflowActionMethods.KrSigningActionOptionActionMethod.GetMethodName(info.Row),
                            KrWorkflowActionMethods.KrSigningActionOptionActionMethod,
                            info);
                    }

                    await this.SendCompleteActionNotificationAsync(
                        context,
                        scriptObject,
                        info,
                        KrWorkflowActionMethods.KrSigningCompleteActionNotificationMethod.GetMethodName(info.Row),
                        KrWorkflowActionMethods.KrSigningCompleteActionNotificationMethod);

                    var linkRows = context.ActionTemplate.Hash.TryGet<IList>(WorkflowConstants.KrSigningActionOptionLinksVirtual.SectionName);
                    var rowID = WorkflowEngineHelper.Get<Guid>(processRow, Names.Table_RowID);
                    var linkIDs =
                        linkRows?
                            .Cast<Dictionary<string, object?>>()
                            .Where(x => WorkflowEngineHelper.Get<Guid?>(
                                x, WorkflowConstants.ActionOptionActionLinksBase.ActionOption, Names.Table_RowID) == rowID)
                            .Select(x => WorkflowEngineHelper.Get<Guid>(
                                x, WorkflowConstants.ActionOptionActionLinksBase.Link, Names.Table_ID))
                        ?? [];

                    foreach (var linkID in linkIDs)
                    {
                        linksForPerforming.Add(linkID);
                    }
                }

                foreach (var linkID in linksForPerforming)
                {
                    context.Links[linkID] = WorkflowEngineSignal.CreateDefaultSignal(StorageHelper.Clone(context.Signal!.Hash));
                }
            }
        }

        private IKrTaskManagerContext CreateTaskManagerContext(
            IWorkflowEngineContext context) =>
            this.krTaskManagerContextFactory.Create<IKrTaskManagerContext<IWorkflowEngineContext>, IWorkflowEngineContext>(
                context);

        private IKrSigningTaskManagerDataProvider CreateSigningTaskManagerDataProvider(
            IWorkflowEngineContext context,
            IWorkflowEngineCompiled? scriptObject,
            Guid? completedTaskRowID = null) =>
            this.krTaskManagerDataProviderFactory.Create<IKrSigningTaskManagerDataProvider<IWorkflowEngineContext>, IWorkflowEngineContext>(
                context,
                configureAction: dataProvider =>
                {
                    dataProvider.CreateTaskActionAsync = async (task, _, _) =>
                    {
                        AddNewProcessingTaskID(
                            context,
                            task.RowID);

                        if (task.TypeID == DefaultTaskTypes.KrSigningTypeID)
                        {
                            if (scriptObject is not null)
                            {
                                await scriptObject.ExecuteActionAsync(
                                    KrWorkflowActionMethods.KrSigningInitMethod.MethodName,
                                    KrWorkflowActionMethods.KrSigningInitMethod,
                                    task,
                                    task.Card.DynamicEntries,
                                    task.Card.DynamicTables);
                            }

                            await this.SendStartTaskNotificationAsync(
                                context,
                                scriptObject,
                                task,
                                WorkflowConstants.KrSigningActionVirtual.SectionName,
                                methodDescriptor: KrWorkflowActionMethods.KrSigningStartNotificationMethod);
                        }
                        else if (task.TypeID == DefaultTaskTypes.KrAdditionalApprovalTypeID)
                        {
                            if (scriptObject is not null)
                            {
                                await scriptObject.ExecuteActionAsync(
                                    KrWorkflowActionMethods.AdditionalApprovalTaskInitMethod.MethodName,
                                    KrWorkflowActionMethods.AdditionalApprovalTaskInitMethod,
                                    task,
                                    task.Card.DynamicEntries,
                                    task.Card.DynamicTables);
                            }

                            await this.SendStartTaskNotificationAsync(
                                context,
                                scriptObject,
                                task,
                                WorkflowConstants.KrWeAdditionalApprovalOptionsVirtual.SectionName,
                                methodDescriptor: KrWorkflowActionMethods.AdditionalApprovalTaskStartNotificationMethod);
                        }
                        else if (task.TypeID == DefaultTaskTypes.KrRequestCommentTypeID)
                        {
                            if (scriptObject is not null)
                            {
                                await scriptObject.ExecuteActionAsync(
                                    KrWorkflowActionMethods.RequestCommentTaskInitMethod.MethodName,
                                    KrWorkflowActionMethods.RequestCommentTaskInitMethod,
                                    task,
                                    task.Card.DynamicEntries,
                                    task.Card.DynamicTables);
                            }

                            await this.SendStartTaskNotificationAsync(
                                context,
                                scriptObject,
                                task,
                                WorkflowConstants.KrWeRequestCommentOptionsVirtual.SectionName,
                                methodDescriptor: KrWorkflowActionMethods.RequestCommentTaskStartNotificationMethod);
                        }
                    };

                    dataProvider.DelegateTaskActionAsync = async (originalTask, delegatedTask, _, _) =>
                    {
                        AddNewProcessingTaskID(
                            context,
                            delegatedTask.RowID);

                        if (delegatedTask.TypeID == DefaultTaskTypes.KrSigningTypeID)
                        {
                            if (scriptObject is not null)
                            {
                                await scriptObject.ExecuteActionAsync(
                                    KrWorkflowActionMethods.KrSigningInitMethod.MethodName,
                                    KrWorkflowActionMethods.KrSigningInitMethod,
                                    delegatedTask,
                                    delegatedTask.Card.DynamicEntries,
                                    delegatedTask.Card.DynamicTables);
                            }

                            await this.SendStartTaskNotificationAsync(
                                context,
                                scriptObject,
                                delegatedTask,
                                WorkflowConstants.KrSigningActionVirtual.SectionName,
                                methodDescriptor: KrWorkflowActionMethods.KrSigningStartNotificationMethod);
                        }
                    };

                    dataProvider.CompleteTaskActionAsync = async (task, _) =>
                    {
                        // Текущее задание (completedTaskRowID) отличается от завершаемого обработчиком?
                        // Если задания различаются, то это означает, что выполняется завершение задания обработчиком и для него надо выполнить сценарии, иначе сценарии уже были выполнены при выполнении CompleteTaskAsync.
                        if (completedTaskRowID != task.RowID)
                        {
                            task.Info[WorkflowEngineHelper.TaskCompletedByWorkflowEngineKey] = BooleanBoxes.True;

                            if (scriptObject is not null)
                            {
                                await scriptObject.ExecuteActionAsync(
                                    nameof(TaskActions.Complete),
                                    WorkflowActionMethods.TaskEventMethod,
                                    task,
                                    task.Card.DynamicEntries,
                                    task.Card.DynamicTables);
                            }

                            await this.CompleteTaskScriptAsync(
                                context,
                                task,
                                scriptObject);

                            if (task.State == CardRowState.Deleted)
                            {
                                GetProcessingTaskIDList(context)?.Remove(task.RowID);
                            }
                        }
                    };
                });

        private IKrEditInterjectTaskManagerDataProvider CreateEditInterjectTaskManagerDataProvider(
            IWorkflowEngineContext context,
            IWorkflowEngineCompiled? scriptObject,
            CardTask? parentTask,
            Guid? completedTaskRowID = null) =>
            this.krTaskManagerDataProviderFactory
                .Create<IKrEditInterjectTaskManagerWithParentTaskDataProvider<IWorkflowEngineContext>, IWorkflowEngineContext>(
                    context,
                    configureAction: dataProvider =>
                    {
                        if (parentTask is not null)
                        {
                            dataProvider.ParentTask = parentTask;
                        }

                        dataProvider.CreateTaskActionAsync = async (task, _, _) =>
                        {
                            AddNewProcessingTaskID(context, task.RowID);

                            if (scriptObject is not null)
                            {
                                await scriptObject.ExecuteActionAsync(
                                    KrWorkflowActionMethods.EditInterjectTaskInitMethod.MethodName,
                                    KrWorkflowActionMethods.EditInterjectTaskInitMethod,
                                    task,
                                    task.Card.DynamicEntries,
                                    task.Card.DynamicTables);
                            }

                            await this.SendStartTaskNotificationAsync(
                                context,
                                scriptObject,
                                task,
                                WorkflowConstants.KrWeEditInterjectOptionsVirtual.SectionName,
                                methodDescriptor: KrWorkflowActionMethods.EditInterjectTaskStartNotificationMethod);
                        };

                        dataProvider.CompleteTaskActionAsync = async (task, _) =>
                        {
                            // Текущее задание (completedTaskRowID) отличается от завершаемого обработчиком?
                            // Если задания различаются, то это означает, что выполняется завершение задания обработчиком и для него надо выполнить сценарии, иначе сценарии уже были выполнены при выполнении CompleteTaskAsync.
                            if (completedTaskRowID != task.RowID)
                            {
                                task.Info[WorkflowEngineHelper.TaskCompletedByWorkflowEngineKey] = BooleanBoxes.True;

                                if (scriptObject is not null)
                                {
                                    await scriptObject.ExecuteActionAsync(
                                        nameof(TaskActions.Complete),
                                        WorkflowActionMethods.TaskEventMethod,
                                        task,
                                        task.Card.DynamicEntries,
                                        task.Card.DynamicTables);
                                }

                                await this.CompleteTaskScriptAsync(
                                    context,
                                    task,
                                    scriptObject);

                                if (task.State == CardRowState.Deleted)
                                {
                                    GetProcessingTaskIDList(context)?.Remove(task.RowID);
                                }
                            }
                        };
                    });

        /// <summary>
        /// Удаляет задание с заданным идентификатором.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="scriptObject"><inheritdoc cref="IWorkflowEngineCompiled" path="/summary"/></param>
        /// <param name="taskManagerContext"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="taskID">Идентификатор удаляемого задания.</param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        private async ValueTask DeleteTaskCoreAsync(
            IWorkflowEngineContext context,
            IWorkflowEngineCompiled? scriptObject,
            IKrTaskManagerContext taskManagerContext,
            Guid taskID)
        {
            var task = await this.GetExecutedTaskAsync(
                context,
                taskID);

            if (task is null)
            {
                return;
            }

            var actionOptionID = await this.DeleteTaskPredefinedActionProcessingAsync(
                context,
                scriptObject,
                taskManagerContext,
                task);

            if (actionOptionID.HasValue
                && !context.Cancel)
            {
                await this.CompleteActionAsync(
                    context,
                    scriptObject,
                    actionOptionID.Value);
            }

            context.Cancel = false;

            GetProcessingTaskIDList(context)?.Remove(task.RowID);
        }

        /// <summary>
        /// Удаляет заданное задание.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="scriptObject"><inheritdoc cref="IWorkflowEngineCompiled" path="/summary"/></param>
        /// <param name="taskManagerContext"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="task">Удаляемое задание.</param>
        /// <returns>
        /// Идентификатор варианта завершения действия, для которого должны быть обработаны связи, выполняющиеся после завершения обработки этого действия, или значение <see langword="null"/>, если действие не завершается.
        /// </returns>
        private async ValueTask<Guid?> DeleteTaskPredefinedActionProcessingAsync(
            IWorkflowEngineContext context,
            IWorkflowEngineCompiled? scriptObject,
            IKrTaskManagerContext taskManagerContext,
            CardTask task)
        {
            if (this.krSigningTaskManager.TaskTypeIDSet.Contains(task.TypeID))
            {
                var krSigningTaskManagerDataProvider =
                    this.CreateSigningTaskManagerDataProvider(
                        context,
                        scriptObject,
                        task.RowID);

                var actionOptionID = await this.krSigningTaskManager.DeleteTaskAsync(
                    taskManagerContext,
                    krSigningTaskManagerDataProvider,
                    task);

                if (!actionOptionID.HasValue)
                {
                    return null;
                }

                if (task.TypeID == DefaultTaskTypes.KrSigningTypeID)
                {
                    if (actionOptionID == KrTaskManagerCompletionOptions.PositiveResult)
                    {
                        return ActionCompletionOptions.Signed;
                    }

                    if (actionOptionID == KrTaskManagerCompletionOptions.NegativeResult)
                    {
                        return ActionCompletionOptions.Declined;
                    }

                    if (actionOptionID == KrTaskManagerCompletionOptions.EditAfterPositiveResult)
                    {
                        var editInterjectTaskManagerDataProvider =
                            this.CreateEditInterjectTaskManagerDataProvider(
                                context,
                                scriptObject,
                                task);

                        await this.krEditInterjectTaskManager.StartAsync(
                            taskManagerContext,
                            editInterjectTaskManagerDataProvider);

                        return null;
                    }
                }

                return null;
            }

            if (task.TypeID == this.krEditInterjectTaskManager.TaskTypeID)
            {
                var editInterjectTaskManagerDataProvider =
                    this.CreateEditInterjectTaskManagerDataProvider(
                        context,
                        scriptObject,
                        null,
                        task.RowID);

                var actionOptionID = await this.krEditInterjectTaskManager.DeleteTaskAsync(
                    taskManagerContext,
                    editInterjectTaskManagerDataProvider,
                    task);

                return actionOptionID == KrTaskManagerCompletionOptions.Delete
                    ? ActionCompletionOptions.Signed
                    : null;
            }

            // Удаление задания неизвестного типа.
            if (await this.DeleteTaskAsync(context, task.RowID))
            {
                await context.TryRemoveActiveTaskAsync(
                    task.RowID,
                    context.ValidationResult,
                    context.CancellationToken);
            }

            return null;
        }

        #endregion
    }
}
