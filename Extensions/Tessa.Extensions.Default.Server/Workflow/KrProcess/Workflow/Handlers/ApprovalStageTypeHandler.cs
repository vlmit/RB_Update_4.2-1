#nullable enable

using System;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers
{
    /// <summary>
    /// Обработчик этапа <see cref="StageTypeDescriptors.ApprovalDescriptor"/>.
    /// </summary>
    public class ApprovalStageTypeHandler :
        SubtaskStageTypeHandler
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="tasksRevoker"><inheritdoc cref="IStageTasksRevoker" path="/summary"/></param>
        /// <param name="notificationManager"><inheritdoc cref="INotificationManager" path="/summary"/></param>
        /// <param name="krScope"><inheritdoc cref="IKrScope" path="/summary"/></param>
        /// <param name="krTaskManagerContextFactory"><inheritdoc cref="IKrTaskManagerContextFactory" path="/summary"/></param>
        /// <param name="krTaskManagerDataProviderFactory"><inheritdoc cref="IKrTaskManagerDataProviderFactory" path="/summary"/></param>
        /// <param name="krApprovalTaskManager"><inheritdoc cref="IKrApprovalTaskManager" path="/summary"/></param>
        public ApprovalStageTypeHandler(
            IStageTasksRevoker tasksRevoker,
            [Dependency(NotificationManagerNames.DeferredWithoutTransaction)]
            INotificationManager notificationManager,
            IKrScope krScope,
            IKrTaskManagerContextFactory krTaskManagerContextFactory,
            IKrTaskManagerDataProviderFactory krTaskManagerDataProviderFactory,
            IKrApprovalTaskManager krApprovalTaskManager)
            : base(
                  tasksRevoker,
                  notificationManager)
        {
            this.KrScope = NotNullOrThrow(krScope);
            this.KrTaskManagerContextFactory = NotNullOrThrow(krTaskManagerContextFactory);
            this.KrTaskManagerDataProviderFactory = NotNullOrThrow(krTaskManagerDataProviderFactory);
            this.KrApprovalTaskManager = NotNullOrThrow(krApprovalTaskManager);
        }

        #endregion

        #region Protected Properties and Constants

        /// <inheritdoc cref="KrConstants.Keys.Disapproved"/>
        protected const string Disapproved = KrConstants.Keys.Disapproved;

        /// <inheritdoc cref="IKrScope" path="/summary"/>
        protected IKrScope KrScope { get; }

        /// <inheritdoc cref="IKrTaskManagerContextFactory" path="/summary"/>
        protected IKrTaskManagerContextFactory KrTaskManagerContextFactory { get; }

        /// <inheritdoc cref="IKrTaskManagerDataProviderFactory" path="/summary"/>
        protected IKrTaskManagerDataProviderFactory KrTaskManagerDataProviderFactory { get; }

        /// <inheritdoc cref="IKrApprovalTaskManager" path="/summary"/>
        protected IKrApprovalTaskManager KrApprovalTaskManager { get; }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Обрабатывает согласование.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrApprovalCoreTaskManagerDataProvider" path="/summary"/></param>
        /// <returns><inheritdoc cref="StageHandlerResult" path="/summary"/></returns>
        protected virtual async ValueTask<StageHandlerResult> ApproveAndCompleteAsync(
            IStageTypeHandlerContext context,
            IKrApprovalCoreTaskManagerDataProvider dataProvider)
        {
            var stage = context.Stage;
            var (hasPreviouslyDisapproved, hasNext) = HandlerHelper.GetApprovalStagesInfo(
                context.WorkflowProcess.Stages,
                stage,
                KrConstants.DefaultApprovalStageTypeIDList);

            var returnToAuthor = await dataProvider.GetReturnWhenPositiveActionResultAsync(
                context.ValidationResult,
                context.CancellationToken);
            var notReturnEdit = await dataProvider.GetNotReturnEditAsync(
                context.ValidationResult,
                context.CancellationToken);

            if (await dataProvider.GetChangeStateOnEndAsync(
                    context.ValidationResult,
                    context.CancellationToken))
            {
                if (hasPreviouslyDisapproved)
                {
                    this.KrScope.Info[KrConstants.Keys.IgnoreChangeState] = BooleanBoxes.True;
                    context.WorkflowProcess.State = KrState.Disapproved;
                }
                else if (!returnToAuthor || notReturnEdit)
                {
                    context.WorkflowProcess.State = KrState.Approved;
                }
            }

            if (!notReturnEdit)
            {
                if (hasPreviouslyDisapproved
                    && !hasNext)
                {
                    // Последний этап завершен. Этот согласован, но предыдущие могли быть и не согласованы.
                    // Если были несогласованные, то возвращаемся в начало текущей группы на доработку.
                    return StageHandlerResult.CurrentGroupTransition();
                }

                if (returnToAuthor)
                {
                    // Выполнить переход к этапу "Доработка" расположенному в текущей группе этапов.
                    Guid? editStageID = null;
                    var cycle = GetCycle(context);
                    context.WorkflowProcess.Stages.ForEachStageInGroup(
                        stage.StageGroupID,
                        currentStage =>
                        {
                            if (currentStage.ID == stage.ID)
                            {
                                return false;
                            }

                            if (currentStage.StageTypeID == StageTypeDescriptors.EditDescriptor.ID)
                            {
                                stage.InfoStorage[Interjected] = Int32Boxes.Box(cycle);
                                currentStage.InfoStorage[EditStageTypeHandler.ReturnToStage] = stage.ID;
                                editStageID = currentStage.ID;
                                return false;
                            }

                            return true;
                        });

                    // Решарпер считает, что т.к. editStageID замкнуто в лямбде, то оно может быть изменено в другом потоке
                    // Дадим ему уверенность, переприсвоив в локальную переменную
                    var localStageEditID = editStageID;
                    if (localStageEditID.HasValue)
                    {
                        return this.StageCompleted(
                            context,
                            StageHandlerResult.Transition(
                                localStageEditID.Value,
                                keepStageStates: true));
                    }

                    context.ValidationResult.AddError(
                        this,
                        "$KrMessages_NoEditStage");
                }
            }

            return this.StageCompleted(
                context,
                StageHandlerResult.CompleteResult);
        }

        /// <summary>
        /// Обрабатывает несогласование.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrApprovalCoreTaskManagerDataProvider" path="/summary"/></param>
        /// <returns><inheritdoc cref="StageHandlerResult" path="/summary"/></returns>
        protected virtual async Task<StageHandlerResult> DisapproveAndCompleteAsync(
            IStageTypeHandlerContext context,
            IKrApprovalCoreTaskManagerDataProvider dataProvider)
        {
            var stage = context.Stage;

            if (!await dataProvider.GetReturnWhenNegativeActionResultAsync(
                    context.ValidationResult,
                    context.CancellationToken))
            {
                (_, var hasNext) = HandlerHelper.GetApprovalStagesInfo(
                    context.WorkflowProcess.Stages,
                    stage,
                    KrConstants.DefaultApprovalStageTypeIDList);

                if (hasNext)
                {
                    context.WorkflowProcess.InfoStorage[Disapproved] = BooleanBoxes.True;

                    return this.StageCompleted(
                        context,
                        StageHandlerResult.CompleteResult);
                }
            }

            if (await dataProvider.GetChangeStateOnEndAsync(
                    context.ValidationResult,
                    context.CancellationToken))
            {
                context.WorkflowProcess.State = KrState.Disapproved;
                this.KrScope.Info[KrConstants.Keys.IgnoreChangeState] = BooleanBoxes.True;
            }

            if (await dataProvider.GetNotReturnEditAsync(
                    context.ValidationResult,
                    context.CancellationToken))
            {
                return this.StageCompleted(
                    context,
                    StageHandlerResult.CompleteResult);
            }

            return this.StageCompleted(
                context,
                StageHandlerResult.GroupTransition(stage.StageGroupID));
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task<StageHandlerResult> HandleStageStartAsync(
            IStageTypeHandlerContext context)
        {
            if (IsInterjected(context))
            {
                if (context.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrApprovalSettingsVirtual.ChangeStateOnEnd) ?? false)
                {
                    context.WorkflowProcess.State = KrState.Approved;
                }

                context.Stage.InfoStorage.Remove(Interjected);
                return StageHandlerResult.CompleteResult;
            }

            var baseResult = await base.HandleStageStartAsync(context);
            if (baseResult != StageHandlerResult.EmptyResult)
            {
                return baseResult;
            }

            var taskManagerContext = this.CreateTaskManagerContext(context);
            var krApprovalTaskManagerDataProvider =
                this.CreateApprovalTaskManagerDataProvider(context);

            var result = await this.KrApprovalTaskManager.StartAsync(
                taskManagerContext,
                krApprovalTaskManagerDataProvider);

            return !result.HasValue
                ? StageHandlerResult.EmptyResult
                : result == KrTaskManagerCompletionOptions.Complete
                ? StageHandlerResult.CompleteResult
                : StageHandlerResult.InProgressResult;
        }

        /// <inheritdoc/>
        public override async Task<StageHandlerResult> HandleTaskCompletionAsync(
            IStageTypeHandlerContext context)
        {
            var task = NotNullOrThrow(context.TaskInfo).Task;
            HandlerHelper.AppendToCompletedTasksWithPreparing(
                context.Stage,
                task);

            var baseResult = await base.HandleTaskCompletionAsync(context);
            if (baseResult != StageHandlerResult.EmptyResult)
            {
                return baseResult;
            }

            var taskManagerContext = this.CreateTaskManagerContext(context);
            var krApprovalTaskManagerDataProvider =
                this.CreateApprovalTaskManagerDataProvider(context);

            var actionOptionID = await this.KrApprovalTaskManager.CompleteTaskAsync(
                taskManagerContext,
                krApprovalTaskManagerDataProvider,
                task);

            if (task.TypeID == DefaultTaskTypes.KrApproveTypeID)
            {
                if (actionOptionID == KrTaskManagerCompletionOptions.SendNextTask
                    || actionOptionID == KrTaskManagerCompletionOptions.IntermediatePositiveResult
                    || actionOptionID == KrTaskManagerCompletionOptions.IntermediateNegativeResult
                    || actionOptionID == KrTaskManagerCompletionOptions.RevokeResult)
                {
                    return StageHandlerResult.InProgressResult;
                }

                if (actionOptionID == KrTaskManagerCompletionOptions.PositiveResult
                    || actionOptionID == KrTaskManagerCompletionOptions.EditAfterPositiveResult)
                {
                    return await this.ApproveAndCompleteAsync(
                        context,
                        krApprovalTaskManagerDataProvider.CreateNested<IKrApprovalCoreTaskManagerDataProvider>());
                }

                if (actionOptionID == KrTaskManagerCompletionOptions.NegativeResult
                    || actionOptionID == KrTaskManagerCompletionOptions.ReturnAfterNegativeResult)
                {
                    return await this.DisapproveAndCompleteAsync(
                        context,
                        krApprovalTaskManagerDataProvider.CreateNested<IKrApprovalCoreTaskManagerDataProvider>());
                }

                if (actionOptionID == KrTaskManagerCompletionOptions.RequestAdditionalApproval)
                {
                    return StageHandlerResult.InProgressResult;
                }

                if (actionOptionID == KrTaskManagerCompletionOptions.RequestComment)
                {
                    return StageHandlerResult.InProgressResult;
                }

                return StageHandlerResult.EmptyResult;
            }

            if (task.TypeID == DefaultTaskTypes.KrAdditionalApprovalTypeID)
            {
                if (actionOptionID == KrTaskManagerCompletionOptions.PositiveResult
                    || actionOptionID == KrTaskManagerCompletionOptions.NegativeResult
                    || actionOptionID == KrTaskManagerCompletionOptions.Cancel)
                {
                    return await this.SubTaskCompleted(context);
                }

                if (actionOptionID == KrTaskManagerCompletionOptions.RequestAdditionalApproval)
                {
                    return StageHandlerResult.InProgressResult;
                }

                if (actionOptionID == KrTaskManagerCompletionOptions.RequestComment)
                {
                    return StageHandlerResult.InProgressResult;
                }

                return StageHandlerResult.EmptyResult;
            }

            if (task.TypeID == DefaultTaskTypes.KrRequestCommentTypeID)
            {
                return await this.SubTaskCompleted(context);
            }

            return StageHandlerResult.EmptyResult;
        }

        /// <inheritdoc/>
        public override Task<bool> HandleStageInterruptAsync(
            IStageTypeHandlerContext context) =>
            this.HandleStageInterruptAsync(
                context,
                [
                    .. this.KrApprovalTaskManager.TaskTypeIDSet,
                ],
                static t => t.Result = "$ApprovalHistory_TaskCancelled");

        #endregion

        #region Private Methods

        private IKrTaskManagerContext CreateTaskManagerContext(
            IStageTypeHandlerContext context) =>
            this.KrTaskManagerContextFactory.Create<IKrTaskManagerContext<IStageTypeHandlerContext>, IStageTypeHandlerContext>(
                context);

        private IKrApprovalTaskManagerDataProvider CreateApprovalTaskManagerDataProvider(
            IStageTypeHandlerContext context) =>
            this.KrTaskManagerDataProviderFactory.Create<IKrApprovalTaskManagerDataProvider<IStageTypeHandlerContext>, IStageTypeHandlerContext>(
                context,
                configureAction: dataProvider =>
                {
                    dataProvider.CreateTaskActionAsync = async (task, _, _) =>
                    {
                        await this.SendTaskNotificationAsync(
                            context,
                            task);

                        if (task.TypeID == DefaultTaskTypes.KrRequestCommentTypeID
                            || task.TypeID == DefaultTaskTypes.KrAdditionalApprovalTypeID)
                        {
                            SetSubtaskCount(context, TryGetSubtaskCount(context) + 1);
                        }
                    };

                    dataProvider.DelegateTaskActionAsync = async (_, delegatedTask, _, _) =>
                    {
                        await this.SendTaskNotificationAsync(
                            context,
                            delegatedTask);
                    };

                    dataProvider.CompleteTaskActionAsync = async (task, _) =>
                    {
                        // Завершается обрабатываемое задание?
                        // В маршрутах завершение дочернего задания при отзыве приведёт к повторному выполнению текущего этапа. Т.о. при обработке фактического завершения задания будет актуализировано число дочерних заданий.
                        if (NotNullOrThrow(context.TaskInfo).Task.RowID == task.RowID
                            && (task.TypeID == DefaultTaskTypes.KrRequestCommentTypeID
                                || task.TypeID == DefaultTaskTypes.KrAdditionalApprovalTypeID)
                                && task.State == CardRowState.Deleted)
                        {
                            SetSubtaskCount(context, TryGetSubtaskCount(context) - 1);
                        }

                        await this.SendTaskCompletedNotificationAsync(context, task);
                    };
                });

        #endregion
    }
}
