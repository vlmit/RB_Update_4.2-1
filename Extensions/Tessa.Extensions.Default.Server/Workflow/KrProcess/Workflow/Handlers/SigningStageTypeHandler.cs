#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
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
    /// Обработчик этапа <see cref="StageTypeDescriptors.SigningDescriptor"/>.
    /// </summary>
    public class SigningStageTypeHandler :
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
        /// <param name="krSigningTaskManager"><inheritdoc cref="IKrSigningTaskManager" path="/summary"/></param>
        public SigningStageTypeHandler(
            IStageTasksRevoker tasksRevoker,
            [Dependency(NotificationManagerNames.DeferredWithoutTransaction)]
            INotificationManager notificationManager,
            IKrScope krScope,
            IKrTaskManagerContextFactory krTaskManagerContextFactory,
            IKrTaskManagerDataProviderFactory krTaskManagerDataProviderFactory,
            IKrSigningTaskManager krSigningTaskManager)
            : base(
                  tasksRevoker,
                  notificationManager)
        {
            this.KrScope = NotNullOrThrow(krScope);
            this.KrTaskManagerContextFactory = NotNullOrThrow(krTaskManagerContextFactory);
            this.KrTaskManagerDataProviderFactory = NotNullOrThrow(krTaskManagerDataProviderFactory);
            this.KrSigningTaskManager = NotNullOrThrow(krSigningTaskManager);
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

        /// <inheritdoc cref="IKrSigningCoreTaskManager" path="/summary"/>
        protected IKrSigningTaskManager KrSigningTaskManager { get; }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Обрабатывает подписание.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrSigningCoreTaskManagerDataProvider" path="/summary"/></param>
        /// <returns><inheritdoc cref="StageHandlerResult" path="/summary"/></returns>
        protected virtual async ValueTask<StageHandlerResult> SignAndCompleteAsync(
            IStageTypeHandlerContext context,
            IKrSigningCoreTaskManagerDataProvider dataProvider)
        {
            var stage = context.Stage;
            var (hasPreviouslyDisapproved, hasNext) = StagePositionInGroup(
                context.WorkflowProcess.Stages,
                stage);

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
                    context.WorkflowProcess.State = KrState.Declined;
                }
                else if (!returnToAuthor || notReturnEdit)
                {
                    context.WorkflowProcess.State = KrState.Signed;
                }
            }

            if (!notReturnEdit)
            {
                if (hasPreviouslyDisapproved
                    && !hasNext)
                {
                    // Последний этап завершен. Этот подписан, но предыдущие могли быть и не подписаны.
                    // Если были неподписанные, то возвращаемся в начало на доработку.
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
        /// Обрабатывает отказ в подписании.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrSigningCoreTaskManagerDataProvider" path="/summary"/></param>
        /// <returns><inheritdoc cref="StageHandlerResult" path="/summary"/></returns>
        protected virtual async Task<StageHandlerResult> DeclineAndCompleteAsync(
            IStageTypeHandlerContext context,
            IKrSigningCoreTaskManagerDataProvider dataProvider)
        {
            var stage = context.Stage;

            if (!await dataProvider.GetReturnWhenNegativeActionResultAsync(
                    context.ValidationResult,
                    context.CancellationToken))
            {
                (_, var hasNext) = StagePositionInGroup(
                    context.WorkflowProcess.Stages,
                    stage);

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
                context.WorkflowProcess.State = KrState.Declined;
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
                if (context.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.ChangeStateOnEnd) ?? false)
                {
                    context.WorkflowProcess.State = KrState.Signed;
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
                this.CreateSigningTaskManagerDataProvider(context);

            var result = await this.KrSigningTaskManager.StartAsync(
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
            var krSigningTaskManagerDataProvider =
                this.CreateSigningTaskManagerDataProvider(context);

            var actionOptionID = await this.KrSigningTaskManager.CompleteTaskAsync(
                taskManagerContext,
                krSigningTaskManagerDataProvider,
                task);

            if (task.TypeID == DefaultTaskTypes.KrSigningTypeID)
            {
                if (actionOptionID == KrTaskManagerCompletionOptions.SendNextTask
                    || actionOptionID == KrTaskManagerCompletionOptions.IntermediatePositiveResult
                    || actionOptionID == KrTaskManagerCompletionOptions.IntermediateNegativeResult)
                {
                    return StageHandlerResult.InProgressResult;
                }

                if (actionOptionID == KrTaskManagerCompletionOptions.PositiveResult
                    || actionOptionID == KrTaskManagerCompletionOptions.EditAfterPositiveResult)
                {
                    return await this.SignAndCompleteAsync(
                        context,
                        krSigningTaskManagerDataProvider.CreateNested<IKrSigningCoreTaskManagerDataProvider>());
                }

                if (actionOptionID == KrTaskManagerCompletionOptions.NegativeResult)
                {
                    return await this.DeclineAndCompleteAsync(
                        context,
                        krSigningTaskManagerDataProvider.CreateNested<IKrSigningCoreTaskManagerDataProvider>());
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
                    .. this.KrSigningTaskManager.TaskTypeIDSet,
                ],
                static t => t.Result = "$ApprovalHistory_TaskCancelled");

        #endregion

        #region Private Methods

        /// <summary>
        /// Определяет в текущей группе этапов:
        /// <list type="number">
        /// <item>
        ///     <description>Были ли до текущего этапа отклонённые этапы "Подписания".</description>
        /// </item>
        /// <item>
        ///     <description>Есть ли этапы "Подписания" после этого этапа.</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="processStages">Коллекция этапов текущего процесса.</param>
        /// <param name="currentStage">Текущий этап.</param>
        /// <returns>Кортеж &lt;Значение <see langword="true"/>, если до текущего этапа были не согласованные этапы "Подписания", иначе - <see langword="false"/>; Значение <see langword="true"/>, если есть этапы "Подписания" после текущего этапа, иначе - <see langword="false"/>&gt;.</returns>
        private static (bool HasPreviouslyDisapproved, bool HasNext) StagePositionInGroup(
            IList<Stage> processStages,
            Stage currentStage)
        {
            var hasPreviouslyDisapprovedClosure = false;
            var hasNextClosure = false;

            // Признак нахождения текущего этапа среди этапов "Подписания" в текущей группе этапов.
            var equator = false;
            processStages.ForEachStageInGroup(
                currentStage.StageGroupID,
                currStage =>
                {
                    if (currStage.ID == currentStage.ID)
                    {
                        equator = true;
                        return;
                    }

                    if (!equator
                        && currStage.State == KrStageState.Completed
                        && currStage.StageTypeID == StageTypeDescriptors.SigningDescriptor.ID
                        && currStage.InfoStorage.TryGet<bool?>(Disapproved) == true)
                    {
                        hasPreviouslyDisapprovedClosure = true;
                    }

                    if (!hasNextClosure
                        && equator
                        && currStage.StageTypeID == StageTypeDescriptors.SigningDescriptor.ID)
                    {
                        hasNextClosure = true;
                    }
                });

            return (hasPreviouslyDisapprovedClosure, hasNextClosure);
        }

        private IKrTaskManagerContext CreateTaskManagerContext(
            IStageTypeHandlerContext context) =>
            this.KrTaskManagerContextFactory.Create<IKrTaskManagerContext<IStageTypeHandlerContext>, IStageTypeHandlerContext>(
                context);

        private IKrSigningTaskManagerDataProvider CreateSigningTaskManagerDataProvider(
            IStageTypeHandlerContext context) =>
            this.KrTaskManagerDataProviderFactory.Create<IKrSigningTaskManagerDataProvider<IStageTypeHandlerContext>, IStageTypeHandlerContext>(
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
