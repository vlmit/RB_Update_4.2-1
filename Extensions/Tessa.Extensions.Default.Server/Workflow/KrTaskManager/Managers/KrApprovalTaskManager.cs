#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <inheritdoc cref="IKrApprovalTaskManager"/>
    public class KrApprovalTaskManager :
        KrMultiTaskManagerBase<IKrApprovalTaskManagerDataProvider>,
        IKrApprovalTaskManager
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="krApprovalCoreTaskManager"><inheritdoc cref="IKrApprovalCoreTaskManager" path="/summary"/></param>
        /// <param name="krAdditionalApprovalTaskManager"><inheritdoc cref="IKrAdditionalApprovalTaskManager" path="/summary"/></param>
        /// <param name="krRequestCommentTaskManager"><inheritdoc cref="IKrRequestCommentTaskManager" path="/summary"/></param>
        public KrApprovalTaskManager(
            IKrApprovalCoreTaskManager krApprovalCoreTaskManager,
            IKrAdditionalApprovalTaskManager krAdditionalApprovalTaskManager,
            IKrRequestCommentTaskManager krRequestCommentTaskManager)
        {
            this.KrApprovalCoreTaskManager = NotNullOrThrow(krApprovalCoreTaskManager);
            this.KrAdditionalApprovalTaskManager = NotNullOrThrow(krAdditionalApprovalTaskManager);
            this.KrRequestCommentTaskManager = NotNullOrThrow(krRequestCommentTaskManager);

            this.TaskTypeIDSet = ImmutableHashSet.Create(
                // with some .NET SDK versions compiler might try to choose overload with "scoped ReadOnlySpan<T>",
                // resulting in error: The feature 'params collections' is currently in Preview and *unsupported*.

                // ReSharper disable once RedundantExplicitParamsArrayCreation
                new[]
                {
                    this.KrApprovalCoreTaskManager.TaskTypeID,
                    this.KrAdditionalApprovalTaskManager.TaskTypeID,
                    this.KrRequestCommentTaskManager.TaskTypeID
                });

            this.CompleteTaskActions[this.KrApprovalCoreTaskManager.TaskTypeID] = this.HandleCompleteApprovalTaskAsync;
            this.CompleteTaskActions[this.KrAdditionalApprovalTaskManager.TaskTypeID] = this.HandleCompleteAdditionalApprovalTaskAsync;
            this.CompleteTaskActions[this.KrRequestCommentTaskManager.TaskTypeID] = this.HandleCompleteCommentTaskAsync;

            this.DeleteTaskActions[this.KrApprovalCoreTaskManager.TaskTypeID] = this.HandleDeleteApprovalTaskAsync;
            this.DeleteTaskActions[this.KrAdditionalApprovalTaskManager.TaskTypeID] = this.HandleDeleteAdditionalApprovalTaskAsync;
            this.DeleteTaskActions[this.KrRequestCommentTaskManager.TaskTypeID] = this.HandleDeleteCommentTaskAsync;
        }

        #endregion

        #region Properties

        /// <inheritdoc cref="IKrApprovalCoreTaskManager" path="/summary"/>
        protected IKrApprovalCoreTaskManager KrApprovalCoreTaskManager { get; }

        /// <inheritdoc cref="IKrAdditionalApprovalTaskManager" path="/summary"/>
        protected IKrAdditionalApprovalTaskManager KrAdditionalApprovalTaskManager { get; }

        /// <inheritdoc cref="IKrRequestCommentTaskManager" path="/summary"/>
        protected IKrRequestCommentTaskManager KrRequestCommentTaskManager { get; }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override IReadOnlySet<Guid> TaskTypeIDSet { get; }

        /// <inheritdoc/>
        public override async ValueTask<Guid?> StartAsync(
            IKrTaskManagerContext context,
            IKrApprovalTaskManagerDataProvider dataProvider)
        {
            ThrowIfNull(context);
            ThrowIfNull(dataProvider);

            var result = await base.StartAsync(
                context,
                dataProvider);

            if (result.HasValue)
            {
                return result;
            }

            var krApprovalTaskManagerDataProvider =
                this.CreateApprovalCoreTaskManagerDataProvider(
                    dataProvider,
                    context);

            return await this.KrApprovalCoreTaskManager.StartAsync(
                context,
                krApprovalTaskManagerDataProvider);
        }

        /// <inheritdoc/>
        /// <remarks>Предотвращаем выполнение логики по умолчанию, т.к. она вызывается в рамках обработки конкретных используемых <see cref="IKrTaskManager{T}"/>.</remarks>
        protected override ValueTask CompleteTaskCoreAsync(
            IKrTaskManagerContext context,
            IKrApprovalTaskManagerDataProvider dataProvider,
            CardTask task) =>
            ValueTask.CompletedTask;

        /// <inheritdoc/>
        /// <remarks>Предотвращаем выполнение логики по умолчанию, т.к. она вызывается в рамках обработки конкретных используемых <see cref="IKrTaskManager{T}"/>.</remarks>
        protected override ValueTask DeleteTaskCoreAsync(
            IKrTaskManagerContext context,
            IKrApprovalTaskManagerDataProvider dataProvider,
            CardTask task) =>
            ValueTask.CompletedTask;

        /// <inheritdoc/>
        protected override async ValueTask<Guid?> CompleteSubTaskCoreAsync(
            IKrTaskManagerContext context,
            IKrApprovalTaskManagerDataProvider dataProvider,
            CardTask task,
            CardTask parentTask)
        {
            if (task.TypeID == this.KrAdditionalApprovalTaskManager.TaskTypeID)
            {
                var result = await this.KrAdditionalApprovalTaskManager.CompleteTaskAsync(
                    context,
                    this.CreateAdditionalApprovalFromParentTaskManagerDataProvider(
                        dataProvider,
                        parentTask),
                    task);

                await this.CompleteSubtasksAsync(
                    context,
                    dataProvider,
                    task);
                return result;
            }

            if (task.TypeID == this.KrRequestCommentTaskManager.TaskTypeID)
            {
                var result = await this.KrRequestCommentTaskManager.CompleteTaskAsync(
                    context,
                    this.CreateRequestCommentTaskManagerDataProvider(
                        dataProvider,
                        parentTask),
                    task);

                await this.CompleteSubtasksAsync(
                    context,
                    dataProvider,
                    task);
                return result;
            }

            return await base.CompleteSubTaskCoreAsync(
                context,
                dataProvider,
                task,
                parentTask);
        }

        /// <inheritdoc/>
        protected override async ValueTask<Guid?> DeleteSubTaskCoreAsync(
            IKrTaskManagerContext context,
            IKrApprovalTaskManagerDataProvider dataProvider,
            CardTask task,
            CardTask parentTask)
        {
            if (task.TypeID == this.KrAdditionalApprovalTaskManager.TaskTypeID)
            {
                var result = await this.KrAdditionalApprovalTaskManager.DeleteTaskAsync(
                    context,
                    this.CreateAdditionalApprovalFromParentTaskManagerDataProvider(
                        dataProvider,
                        parentTask),
                    task);

                await this.DeleteSubtasksAsync(
                    context,
                    dataProvider,
                    task);
                return result;
            }

            if (task.TypeID == this.KrRequestCommentTaskManager.TaskTypeID)
            {
                var result = await this.KrRequestCommentTaskManager.DeleteTaskAsync(
                    context,
                    this.CreateRequestCommentTaskManagerDataProvider(
                        dataProvider,
                        parentTask),
                    task);

                await this.DeleteSubtasksAsync(
                    context,
                    dataProvider,
                    task);
                return result;
            }

            return await base.DeleteSubTaskCoreAsync(
                context,
                dataProvider,
                task,
                parentTask);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Обрабатывает завершение задания запроса комментария.
        /// </summary>
        /// <inheritdoc cref="IKrTaskManager{T}.CompleteTaskAsync(IKrTaskManagerContext, T, CardTask)"/>
        protected virtual async ValueTask<Guid?> HandleCompleteCommentTaskAsync(
            IKrTaskManagerContext context,
            IKrApprovalTaskManagerDataProvider dataProvider,
            CardTask task)
        {
            var requestCommentTaskManagerDataProvider =
                this.CreateRequestCommentTaskManagerDataProvider(
                    dataProvider,
                    task);

            return await this.KrRequestCommentTaskManager.CompleteTaskAsync(
                context,
                requestCommentTaskManagerDataProvider,
                task);
        }

        /// <summary>
        /// Обрабатывает удаление задания запроса комментария.
        /// </summary>
        /// <inheritdoc cref="IKrTaskManager{T}.DeleteTaskAsync(IKrTaskManagerContext, T, CardTask)"/>
        protected virtual async ValueTask<Guid?> HandleDeleteCommentTaskAsync(
            IKrTaskManagerContext context,
            IKrApprovalTaskManagerDataProvider dataProvider,
            CardTask task)
        {
            var requestCommentTaskManagerDataProvider =
                this.CreateRequestCommentTaskManagerDataProvider(
                    dataProvider,
                    task);

            return await this.KrRequestCommentTaskManager.DeleteTaskAsync(
                context,
                requestCommentTaskManagerDataProvider,
                task);
        }

        /// <summary>
        /// Обрабатывает завершение задания дополнительного согласования.
        /// </summary>
        /// <inheritdoc cref="IKrTaskManager{T}.CompleteTaskAsync(IKrTaskManagerContext, T, CardTask)"/>
        protected virtual async ValueTask<Guid?> HandleCompleteAdditionalApprovalTaskAsync(
            IKrTaskManagerContext context,
            IKrApprovalTaskManagerDataProvider dataProvider,
            CardTask task)
        {
            var additionalApprovalTaskManagerDataProvider = this.CreateAdditionalApprovalFromParentTaskManagerDataProvider(
                dataProvider,
                task);

            var actionOptionID = await this.KrAdditionalApprovalTaskManager.CompleteTaskAsync(
                context,
                additionalApprovalTaskManagerDataProvider,
                task);

            if (actionOptionID == KrTaskManagerCompletionOptions.PositiveResult
                || actionOptionID == KrTaskManagerCompletionOptions.NegativeResult
                || actionOptionID == KrTaskManagerCompletionOptions.Cancel)
            {
                await this.CompleteSubtasksAsync(
                    context,
                    dataProvider,
                    task);
            }
            else if (actionOptionID == KrTaskManagerCompletionOptions.Delegate)
            {
                await this.CompleteSubTasksAsync(
                    context,
                    dataProvider,
                    task,
                    [
                        this.KrRequestCommentTaskManager.TaskTypeID,
                    ],
                    static async task =>
                    {
                        task.OptionID = DefaultCompletionOptions.Cancel;
                        task.Result = "$ApprovalHistory_ParentTaskIsCompleted";
                    });
            }
            else if (actionOptionID == KrTaskManagerCompletionOptions.RequestAdditionalApproval)
            {
                await this.KrAdditionalApprovalTaskManager.StartAsync(
                    context,
                    additionalApprovalTaskManagerDataProvider);
            }
            else if (actionOptionID == KrTaskManagerCompletionOptions.RequestComment)
            {
                await this.SendRequestCommentTaskAsync(
                    context,
                    dataProvider,
                    task);
            }

            return actionOptionID;
        }

        /// <summary>
        /// Обрабатывает удаление задания дополнительного согласования.
        /// </summary>
        /// <inheritdoc cref="IKrTaskManager{T}.DeleteTaskAsync(IKrTaskManagerContext, T, CardTask)"/>
        protected virtual async ValueTask<Guid?> HandleDeleteAdditionalApprovalTaskAsync(
            IKrTaskManagerContext context,
            IKrApprovalTaskManagerDataProvider dataProvider,
            CardTask task)
        {
            var additionalApprovalTaskManagerDataProvider = this.CreateAdditionalApprovalFromParentTaskManagerDataProvider(
                dataProvider,
                task);

            var actionOptionID = await this.KrAdditionalApprovalTaskManager.DeleteTaskAsync(
                context,
                additionalApprovalTaskManagerDataProvider,
                task);

            if (actionOptionID == KrTaskManagerCompletionOptions.Delete)
            {
                await this.DeleteSubtasksAsync(
                    context,
                    dataProvider,
                    task);
            }

            return actionOptionID;
        }

        /// <summary>
        /// Обрабатывает завершение задания согласования.
        /// </summary>
        /// <inheritdoc cref="IKrTaskManager{T}.CompleteTaskAsync(IKrTaskManagerContext, T, CardTask)"/>
        protected virtual async ValueTask<Guid?> HandleCompleteApprovalTaskAsync(
            IKrTaskManagerContext context,
            IKrApprovalTaskManagerDataProvider dataProvider,
            CardTask task)
        {
            var krApprovalTaskManagerDataProvider =
                this.CreateApprovalCoreTaskManagerDataProvider(
                    dataProvider,
                    context);

            var actionOptionID = await this.KrApprovalCoreTaskManager.CompleteTaskAsync(
                context,
                krApprovalTaskManagerDataProvider,
                task);

            if (actionOptionID == KrTaskManagerCompletionOptions.PositiveResult
                || actionOptionID == KrTaskManagerCompletionOptions.NegativeResult
                || actionOptionID == KrTaskManagerCompletionOptions.SendNextTask
                || actionOptionID == KrTaskManagerCompletionOptions.IntermediatePositiveResult
                || actionOptionID == KrTaskManagerCompletionOptions.IntermediateNegativeResult
                || actionOptionID == KrTaskManagerCompletionOptions.EditAfterPositiveResult
                || actionOptionID == KrTaskManagerCompletionOptions.RevokeResult)
            {
                await this.CompleteSubtasksAsync(
                    context,
                    dataProvider,
                    task);
            }
            else if (actionOptionID == KrTaskManagerCompletionOptions.Delegate)
            {
                await this.CompleteSubTasksAsync(
                    context,
                    dataProvider,
                    task,
                    [
                        this.KrRequestCommentTaskManager.TaskTypeID,
                    ],
                    static async task =>
                    {
                        task.OptionID = DefaultCompletionOptions.Cancel;
                        task.Result = "$ApprovalHistory_ParentTaskIsCompleted";
                    });
            }
            else if (actionOptionID == KrTaskManagerCompletionOptions.RequestAdditionalApproval)
            {
                var additionalApprovalTaskManagerDataProvider = this.CreateAdditionalApprovalFromParentTaskManagerDataProvider(
                    dataProvider,
                    task);

                await this.KrAdditionalApprovalTaskManager.StartAsync(
                    context,
                    additionalApprovalTaskManagerDataProvider);
            }
            else if (actionOptionID == KrTaskManagerCompletionOptions.RequestComment)
            {
                await this.SendRequestCommentTaskAsync(
                    context,
                    dataProvider,
                    task);
            }

            return actionOptionID;
        }

        /// <summary>
        /// Обрабатывает удаление задания согласования.
        /// </summary>
        /// <inheritdoc cref="IKrTaskManager{T}.DeleteTaskAsync(IKrTaskManagerContext, T, CardTask)"/>
        protected virtual async ValueTask<Guid?> HandleDeleteApprovalTaskAsync(
            IKrTaskManagerContext context,
            IKrApprovalTaskManagerDataProvider dataProvider,
            CardTask task)
        {
            var krApprovalTaskManagerDataProvider =
                this.CreateApprovalCoreTaskManagerDataProvider(
                    dataProvider,
                    context);

            var actionOptionID = await this.KrApprovalCoreTaskManager.DeleteTaskAsync(
                context,
                krApprovalTaskManagerDataProvider,
                task);

            if (actionOptionID == KrTaskManagerCompletionOptions.Delete
                || actionOptionID == KrTaskManagerCompletionOptions.SendNextTask
                || actionOptionID == KrTaskManagerCompletionOptions.PositiveResult
                || actionOptionID == KrTaskManagerCompletionOptions.NegativeResult
                || actionOptionID == KrTaskManagerCompletionOptions.EditAfterPositiveResult)
            {
                await this.DeleteSubtasksAsync(
                    context,
                    dataProvider,
                    task);
            }

            return actionOptionID;
        }

        /// <summary>
        /// Создаёт <see cref="IKrApprovalCoreTaskManagerDataProvider"/>.
        /// </summary>
        /// <param name="dataProvider"><inheritdoc cref="IKrApprovalTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="taskManagerContext"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <returns><inheritdoc cref="IKrApprovalCoreTaskManagerDataProvider" path="/summary"/></returns>
        protected virtual IKrApprovalCoreTaskManagerDataProvider CreateApprovalCoreTaskManagerDataProvider(
            IKrApprovalTaskManagerDataProvider dataProvider,
            IKrTaskManagerContext taskManagerContext) =>
            dataProvider.CreateNested<IKrApprovalCoreTaskManagerDataProvider>(
                configureAction: (nestedDataProvider) =>
                {
                    nestedDataProvider.CreateTaskActionAsync = async (task, taskPerformer, ct) =>
                    {
                        if (dataProvider.CreateTaskActionAsync is not null)
                        {
                            await dataProvider.CreateTaskActionAsync(
                                task,
                                taskPerformer,
                                ct);
                        }

                        var additionalApprovalTaskManagerDataProvider = this.CreateAdditionalApprovalTaskManagerDataProvider(
                            dataProvider,
                            task,
                            taskPerformer!);

                        await this.KrAdditionalApprovalTaskManager.StartAsync(
                            taskManagerContext,
                            additionalApprovalTaskManagerDataProvider);
                    };

                    nestedDataProvider.DelegateTaskActionAsync = dataProvider.DelegateTaskActionAsync;

                    nestedDataProvider.CompleteTaskActionAsync = dataProvider.CompleteTaskActionAsync;
                });

        /// <summary>
        /// Создаёт <see cref="IKrAdditionalApprovalTaskManagerDataProvider"/>, настроенный на получение информации из родительского задания, управляемого <see cref="KrApprovalCoreTaskManager"/>.
        /// </summary>
        /// <param name="dataProvider"><inheritdoc cref="IKrApprovalTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="parentTask">Родительское задание.</param>
        /// <param name="parentTaskPerformer">Исполнитель родительского задания.</param>
        /// <returns><inheritdoc cref="IKrAdditionalApprovalTaskManagerDataProvider" path="/summary"/></returns>
        protected virtual IKrAdditionalApprovalTaskManagerDataProvider CreateAdditionalApprovalTaskManagerDataProvider(
            IKrApprovalTaskManagerDataProvider dataProvider,
            CardTask parentTask,
            RoleEntryStorage parentTaskPerformer) =>
            dataProvider.CreateNested<IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider>(
                configureAction: (nestedDataProvider) =>
                {
                    nestedDataProvider.ParentTask = parentTask;
                    nestedDataProvider.ParentTaskPerformer = parentTaskPerformer;

                    nestedDataProvider.CreateTaskActionAsync = (task, taskPerformer, ct) =>
                    {
                        if (dataProvider.CreateTaskActionAsync is not null)
                        {
                            return dataProvider.CreateTaskActionAsync(
                                task,
                                taskPerformer,
                                ct);
                        }

                        return ValueTask.CompletedTask;
                    };

                    nestedDataProvider.DelegateTaskActionAsync = (originalTask, delegatedTask, performer, ct) =>
                    {
                        if (dataProvider.DelegateTaskActionAsync is not null)
                        {
                            return dataProvider.DelegateTaskActionAsync(
                                originalTask,
                                delegatedTask,
                                performer,
                                ct);
                        }

                        return ValueTask.CompletedTask;
                    };

                    nestedDataProvider.CompleteTaskActionAsync = dataProvider.CompleteTaskActionAsync;
                });

        /// <summary>
        /// Создаёт <see cref="IKrAdditionalApprovalTaskManagerDataProvider"/>, настроенный на получение информации из родительского задания, управляемого <see cref="KrAdditionalApprovalTaskManager"/>.
        /// </summary>
        /// <param name="baseDataProvider"><inheritdoc cref="IKrApprovalTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="parentTask">Родительское задание.</param>
        /// <returns><inheritdoc cref="IKrAdditionalApprovalTaskManagerDataProvider" path="/summary"/></returns>
        protected virtual IKrAdditionalApprovalTaskManagerDataProvider CreateAdditionalApprovalFromParentTaskManagerDataProvider(
            IKrApprovalTaskManagerDataProvider baseDataProvider,
            CardTask parentTask) =>
            baseDataProvider
                .CreateNested<IKrAdditionalApprovalTaskManagerWithParentTaskDataProvider>(
                    configureAction: dataProvider =>
                    {
                        dataProvider.ParentTask = parentTask;

                        dataProvider.CreateTaskActionAsync = (task, taskPerformer, ct) =>
                        {
                            if (baseDataProvider.CreateTaskActionAsync is not null)
                            {
                                return baseDataProvider.CreateTaskActionAsync(
                                    task,
                                    taskPerformer,
                                    ct);
                            }

                            return ValueTask.CompletedTask;
                        };

                        dataProvider.DelegateTaskActionAsync = (originalTask, delegatedTask, performer, ct) =>
                        {
                            if (baseDataProvider.DelegateTaskActionAsync is not null)
                            {
                                return baseDataProvider.DelegateTaskActionAsync(
                                    originalTask,
                                    delegatedTask,
                                    performer,
                                    ct);
                            }

                            return ValueTask.CompletedTask;
                        };

                        dataProvider.CompleteTaskActionAsync = baseDataProvider.CompleteTaskActionAsync;
                    });

        /// <summary>
        /// Создаёт <see cref="IKrRequestCommentTaskManagerDataProvider"/>.
        /// </summary>
        /// <param name="baseDataProvider"><inheritdoc cref="IKrApprovalTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="parentTask">Родительское задание.</param>
        /// <returns><inheritdoc cref="IKrRequestCommentTaskManagerDataProvider" path="/summary"/></returns>
        protected virtual IKrRequestCommentTaskManagerDataProvider CreateRequestCommentTaskManagerDataProvider(
            IKrApprovalTaskManagerDataProvider baseDataProvider,
            CardTask parentTask) =>
            baseDataProvider
                .CreateNested<IKrRequestCommentTaskManagerWithParentTaskDataProvider>(
                    configureAction: dataProvider =>
                    {
                        dataProvider.ParentTask = parentTask;

                        dataProvider.CreateTaskActionAsync = (task, taskPerformer, ct) =>
                        {
                            if (baseDataProvider.CreateTaskActionAsync is not null)
                            {
                                return baseDataProvider.CreateTaskActionAsync(
                                    task,
                                    taskPerformer,
                                    ct);
                            }

                            return ValueTask.CompletedTask;
                        };

                        dataProvider.DelegateTaskActionAsync = (originalTask, delegatedTask, performer, ct) =>
                        {
                            if (baseDataProvider.DelegateTaskActionAsync is not null)
                            {
                                return baseDataProvider.DelegateTaskActionAsync(
                                    originalTask,
                                    delegatedTask,
                                    performer,
                                    ct);
                            }

                            return ValueTask.CompletedTask;
                        };

                        dataProvider.CompleteTaskActionAsync = baseDataProvider.CompleteTaskActionAsync;
                    });

        /// <summary>
        /// Создаёт задание запроса комментария.
        /// </summary>
        /// <param name="taskManagerContext"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="baseDataProvider"><inheritdoc cref="IKrApprovalTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="parentTask">Родительское задание.</param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        protected virtual async ValueTask SendRequestCommentTaskAsync(
            IKrTaskManagerContext taskManagerContext,
            IKrApprovalTaskManagerDataProvider baseDataProvider,
            CardTask parentTask)
        {
            var requestCommentTaskManagerDataProvider =
                this.CreateRequestCommentTaskManagerDataProvider(
                    baseDataProvider,
                    parentTask);

            await this.KrRequestCommentTaskManager.StartAsync(
                taskManagerContext,
                requestCommentTaskManagerDataProvider);
        }

        /// <summary>
        /// Отзывает дочерние задания.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrApprovalTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="parentTask">Родительское задание.</param>
        /// <returns>Асинхронная задача.</returns>
        protected virtual ValueTask CompleteSubtasksAsync(
            IKrTaskManagerContext context,
            IKrApprovalTaskManagerDataProvider dataProvider,
            CardTask parentTask) =>
            this.CompleteSubTasksAsync(
                context,
                dataProvider,
                parentTask,
                [
                    this.KrAdditionalApprovalTaskManager.TaskTypeID,
                    this.KrRequestCommentTaskManager.TaskTypeID,
                ],
                async task =>
                {
                    task.OptionID = task.TypeID == this.KrAdditionalApprovalTaskManager.TaskTypeID
                        ? DefaultCompletionOptions.Revoke
                        : DefaultCompletionOptions.Cancel;
                    task.Result = "$ApprovalHistory_ParentTaskIsCompleted";
                });

        /// <summary>
        /// Удаляет дочерние задания.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrApprovalTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="parentTask">Родительское задание.</param>
        /// <returns>Асинхронная задача.</returns>
        protected virtual ValueTask DeleteSubtasksAsync(
            IKrTaskManagerContext context,
            IKrApprovalTaskManagerDataProvider dataProvider,
            CardTask parentTask) =>
            this.DeleteSubTasksAsync(
                context,
                dataProvider,
                parentTask,
                [
                    this.KrAdditionalApprovalTaskManager.TaskTypeID,
                    this.KrRequestCommentTaskManager.TaskTypeID,
                ]);

        #endregion
    }
}
