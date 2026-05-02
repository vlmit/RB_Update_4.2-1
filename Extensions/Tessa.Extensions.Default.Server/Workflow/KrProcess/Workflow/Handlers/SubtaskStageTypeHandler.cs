#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Notices;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers
{
    /// <summary>
    /// Представляет абстрактный обработчик этапа поддерживающий дочерние задания.
    /// </summary>
    public abstract class SubtaskStageTypeHandler :
        StageTypeHandlerBase
    {
        #region Constants And Static Fields

        protected const string SubtaskCount = nameof(SubtaskCount);
        protected const string ResultAction = nameof(ResultAction);
        protected const string ResultTransitTo = nameof(ResultTransitTo);
        protected const string ResultKeepStates = nameof(ResultKeepStates);
        protected const string Interjected = nameof(Interjected);

        private static readonly Guid[] subTaskTypeIDs = [DefaultTaskTypes.KrRequestCommentTypeID];

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="tasksRevoker"><inheritdoc cref="IStageTasksRevoker" path="/summary"/></param>
        /// <param name="notificationManager"><inheritdoc cref="INotificationManager" path="/summary"/></param>
        protected SubtaskStageTypeHandler(
            IStageTasksRevoker tasksRevoker,
            INotificationManager notificationManager)
        {
            this.TasksRevoker = NotNullOrThrow(tasksRevoker);
            this.NotificationManager = NotNullOrThrow(notificationManager);
        }

        #endregion

        #region Properties

        /// <inheritdoc cref="IStageTasksRevoker" path="/summary"/>
        protected IStageTasksRevoker TasksRevoker { get; }

        /// <inheritdoc cref="INotificationManager" path="/summary"/>
        protected INotificationManager NotificationManager { get; }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Возвращает массив идентификаторов типов дочерних заданий которые должны быть завершены.
        /// </summary>
        protected virtual Guid[] GetSubTaskTypesToRevoke() => subTaskTypeIDs;

        /// <summary>
        /// Возвращает массив идентификаторов типов дочерних заданий которые должны быть завершены при делегировании задания.
        /// По умолчанию возвращает то же значение, что и <see cref="GetSubTaskTypesToRevoke"/>.
        /// </summary>
        protected virtual Guid[] GetSubTaskTypesForDelegateToRevoke() => this.GetSubTaskTypesToRevoke();

        /// <summary>
        /// Обрабатывает отмену этапа.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="taskTypeIDs">Массив идентификаторов типов завершаемых заданий.</param>
        /// <param name="revoke">Действие, выполняемое при завершении задания.</param>
        /// <returns>Значение <see langword="true"/>, если не было завершено заданий, иначе - <see langword="false"/>.</returns>
        protected virtual async Task<bool> HandleStageInterruptAsync(
            IStageTypeHandlerContext context,
            Guid[] taskTypeIDs,
            Action<CardTask> revoke)
        {
            SetSubtaskCount(context, 0);
            return await this.RevokeTasksAsync(context, taskTypeIDs, revoke, removeFromActive: true) == 0;
        }

        /// <summary>
        /// Отправляет задание указанного типа.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="typeID">Идентификатор типа задания.</param>
        /// <param name="digest">Дайджест задания.</param>
        /// <param name="performerID">Идентификатор роли, на которую отправляется задание или значение <see langword="null"/>, если он будет задан в <paramref name="modifyTask"/>.</param>
        /// <param name="performerName">Имя роли, на которую отправляется задание.</param>
        /// <param name="modifyTask">Метод модифицирующий задание.</param>
        /// <param name="createHistory">Значение <see langword="true"/>, если в историю процесса должна быть добавлена информация о задании, иначе - <see langword="false"/>.</param>
        /// <returns>Созданное задание или значение по умолчанию для типа, если при создании задания произошла ошибка.</returns>
        protected virtual async Task<CardTask?> SendTaskAsync(
            IStageTypeHandlerContext context,
            Guid typeID,
            string? digest,
            Guid? performerID,
            string? performerName,
            Func<CardTask, CancellationToken, ValueTask>? modifyTask = null,
            bool createHistory = true)
        {
            var taskInfo = await context.WorkflowAPI!.SendTaskAsync(
                typeID,
                digest,
                performerID,
                performerName,
                context.ValidationResult,
                modifyTaskAction: modifyTask,
                cancellationToken: context.CancellationToken);

            if (taskInfo is null)
            {
                return null;
            }

            var task = taskInfo.Task;

            if (createHistory)
            {
                var advisory = context.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrApprovalSettingsVirtual.Advisory) ?? false;
                task.Flags |= CardTaskFlags.CreateHistoryItem;
                context.ContextualSatellite.AddToHistory(
                    task.RowID,
                    context.WorkflowProcess.InfoStorage.TryGet(KrConstants.Keys.Cycle, 1),
                    advisory);
            }

            await context.WorkflowAPI.AddActiveTaskAsync(
                task.RowID,
                context.ValidationResult,
                context.CancellationToken);

            if (context.CardExtensionContext is ICardStoreExtensionContext storeContext)
            {
                await CardComponentHelper.FillTaskAssignedRolesAsync(
                    task,
                    storeContext.DbScope!,
                    cancellationToken: context.CancellationToken);
            }

            await this.SendTaskNotificationAsync(
                context,
                task);

            return task;
        }

        /// <summary>
        /// Отправляет уведомление <see cref="DefaultNotifications.TaskNotification"/> по <paramref name="task"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="task">Задание, по которому отправляется уведомление.</param>
        /// <returns>Асинхронная задача.</returns>
        protected virtual async Task SendTaskNotificationAsync(
            IStageTypeHandlerContext context,
            CardTask task) =>
            context.ValidationResult.Add(
                await this.NotificationManager.SendAsync(
                    DefaultNotifications.TaskNotification,
                    task.TaskAssignedRoles
                        .Where(static x => x.TaskRoleID == CardFunctionRoles.PerformerID)
                        .Select(static x => x.RoleID)
                        .ToArray(),
                    new NotificationSendContext
                    {
                        MainCardID = context.MainCardID ?? Guid.Empty,
                        TaskTypeID = task.TypeID,
                        Info = DefaultNotificationHelper.GetInfoWithTask(task),
                        ModifyEmailActionAsync = async (email, _) =>
                        {
                            DefaultNotificationHelper.ModifyTaskCaption(
                                email,
                                task);
                        },
                        GetCardFuncAsync = (validationResult, ct) =>
                            context.MainCardAccessStrategy.GetCardAsync(
                                validationResult: validationResult,
                                cancellationToken: ct),
                    },
                    context.CancellationToken));

        /// <summary>
        /// Отправляет уведомление о  завершении задания по <paramref name="task"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="task">Задание, по которому отправляется уведомление.</param>
        /// <returns>Асинхронная задача.</returns>
        protected virtual async Task SendTaskCompletedNotificationAsync(
            IStageTypeHandlerContext context,
            CardTask task)
        {
            if (task.TypeID == DefaultTaskTypes.KrRequestCommentTypeID
                && task.OptionID == DefaultCompletionOptions.AddComment)
            {
                if (context.CardExtensionContext is ICardStoreExtensionContext storeContext)
                {
                    await CardComponentHelper.FillTaskAssignedRolesAsync(
                        task,
                        storeContext.DbScope!,
                        cancellationToken: context.CancellationToken);
                }

                context.ValidationResult.Add(
                    await this.NotificationManager
                        .SendAsync(
                            DefaultNotifications.CommentNotification,
                            task.TaskAssignedRoles.Where(x => x.TaskRoleID == CardFunctionRoles.AuthorID).Select(x => x.RoleID).ToArray(),
                            new NotificationSendContext
                            {
                                MainCardID = context.MainCardID ?? Guid.Empty,
                                TaskTypeID = task.TypeID,
                                GetCardFuncAsync = (validationResult, ct) =>
                                    context.MainCardAccessStrategy.GetCardAsync(
                                        validationResult: validationResult,
                                        cancellationToken: ct),
                                Info = DefaultNotificationHelper.GetInfoWithTask(task),
                            },
                            context.CancellationToken));
            }
        }

        /// <summary>
        /// Завершает задания указанных типов.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="taskTypeIDs">Массив идентификаторов типов завершаемых заданий.</param>
        /// <param name="revoke">Действие, выполняемое при завершении задания.</param>
        /// <param name="removeFromActive">Значение <see langword="true"/>, если необходимо удалить задание из списка активных, иначе - <see langword="false"/>.</param>
        /// <returns>Число завершённых заданий.</returns>
        protected virtual async Task<int> RevokeTasksAsync(
            IStageTypeHandlerContext context,
            Guid[] taskTypeIDs,
            Action<CardTask> revoke,
            bool removeFromActive = false)
        {
            var storeContext = (ICardStoreExtensionContext) context.CardExtensionContext!;
            var scope = storeContext.DbScope!;
            await using (scope.Create())
            {
                var db = scope.Db;
                var query = scope.BuilderFactory
                    .Select().C("t", "RowID")
                    .From("Tasks", "t").NoLock()
                    .InnerJoin("WorkflowTasks", "wt").NoLock()
                        .On().C("wt", "RowID").Equals().C("t", "RowID")
                    .Where().C("t", "TypeID").Q(" IN (");

                var index = 0;
                var parameters = new DataParameter[taskTypeIDs.Length + 1]; // Число отзываемых типов заданий + ProcessID.
                while (index < taskTypeIDs.Length)
                {
                    var parameterName = $"TypeID{index}";
                    var parameter = db.Parameter(parameterName, taskTypeIDs[index]);
                    query.Parameter(parameterName);
                    parameters[index++] = parameter;
                }

                parameters[index] = db.Parameter("ProcessID", context.ProcessInfo!.ProcessID);

                var tasksToRevoke = await db
                    .SetCommand(
                        query
                            .Q(")")
                            .And().C("wt", "ProcessRowID").Equals().P("ProcessID")
                            .Build(),
                        parameters)
                    .LogCommand()
                    .ExecuteListAsync<Guid>(context.CancellationToken);
                return await this.RevokeTasksCoreAsync(context, tasksToRevoke, revoke, removeFromActive);
            }
        }

        /// <summary>
        /// Завершает дочерние задания типов возвращаемых <see cref="GetSubTaskTypesForDelegateToRevoke"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="parentTask">Родительское задание.</param>
        /// <returns>Число завершённых заданий.</returns>
        protected virtual Task<int> RevokeSubTasksForDelegateAsync(
            IStageTypeHandlerContext context,
            CardTask parentTask) =>
            this.RevokeSubTasksAsync(
                context,
                parentTask,
                this.GetSubTaskTypesForDelegateToRevoke(),
                t =>
                {
                    t.OptionID = DefaultCompletionOptions.Cancel;
                    t.Result = "$ApprovalHistory_ParentTaskIsCompleted";
                });

        /// <summary>
        /// Завершает дочерние задания указанных типов.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="parentTask">Родительское задание.</param>
        /// <param name="taskTypeIDs">Массив типов завершаемых заданий.</param>
        /// <param name="revoke">Действие, выполняемое при завершении задания.</param>
        /// <param name="removeFromActive">Значение <see langword="true"/>, если необходимо удалить задание из списка активных, иначе - <see langword="false"/>.</param>
        /// <returns>Число завершённых заданий.</returns>
        protected virtual async Task<int> RevokeSubTasksAsync(
            IStageTypeHandlerContext context,
            CardTask parentTask,
            Guid[] taskTypeIDs,
            Action<CardTask> revoke,
            bool removeFromActive = false)
        {
            var storeContext = (ICardStoreExtensionContext) context.CardExtensionContext!;
            var scope = storeContext.DbScope!;
            await using (scope.Create())
            {
                var db = scope.Db;
                var query = scope.BuilderFactory
                    .Select().C("RowID")
                    .From("Tasks").NoLock()
                    .Where().C("TypeID").Q(" IN (");

                var index = 0;
                var parameters = new DataParameter[taskTypeIDs.Length + 1];
                while (index < taskTypeIDs.Length)
                {
                    var parameterName = $"TypeID{index}";
                    var parameter = db.Parameter(parameterName, taskTypeIDs[index]);
                    query.Parameter(parameterName);
                    parameters[index++] = parameter;
                }

                parameters[index] = db.Parameter("ParentApprovalID", parentTask.RowID);

                var tasksToRevoke = await db
                    .SetCommand(
                        query.Q(")")
                            .And().C("ParentID").Equals().P("ParentApprovalID")
                            .Build(),
                        parameters)
                    .LogCommand()
                    .ExecuteListAsync<Guid>(context.CancellationToken);
                return await this.RevokeTasksCoreAsync(context, tasksToRevoke, revoke, removeFromActive);
            }
        }

        /// <summary>
        /// Завершает дочерние задания типов возвращаемых <see cref="GetSubTaskTypesToRevoke"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="parentTask">Родительское задание.</param>
        /// <returns>Число завершённых заданий.</returns>
        protected virtual Task<int> RevokeSubTasksAsync(
            IStageTypeHandlerContext context,
            CardTask parentTask) =>
            this.RevokeSubTasksAsync(
                context,
                parentTask,
                this.GetSubTaskTypesToRevoke(),
                t =>
                {
                    t.OptionID = DefaultCompletionOptions.Cancel;
                    t.Result = "$ApprovalHistory_ParentTaskIsCompleted";
                });

        /// <summary>
        /// Завершает задания имеющие идентификаторы из указанного списка.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="tasksToRevoke">Список идентификаторов завершаемых заданий.</param>
        /// <param name="revoke">Действие, выполняемое при завершении задания.</param>
        /// <param name="removeFromActive">Значение <see langword="true"/>, если необходимо удалить задание из списка активных, иначе - <see langword="false"/>.</param>
        /// <returns>Число завершённых заданий.</returns>
        protected virtual async Task<int> RevokeTasksCoreAsync(
            IStageTypeHandlerContext context,
            List<Guid> tasksToRevoke,
            Action<CardTask> revoke,
            bool removeFromActive) =>
            await this.TasksRevoker.RevokeTasksAsync(new StageTaskRevokerContext(
                context,
                context.ValidationResult,
                context.CancellationToken)
            {
                CardID = context.MainCardID ?? Guid.Empty,
                TaskIDs = tasksToRevoke,
                RemoveFromActive = removeFromActive,
                TaskModificationAction = task =>
                {
                    const string revokedByParent = StorageHelper.SystemKeyPrefix + "revokedByParent";
                    task.Info[revokedByParent] = BooleanBoxes.True;
                    revoke(task);
                }
            });

        /// <summary>
        /// Обрабатывает завершение дочерних заданий.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns><inheritdoc cref="StageHandlerResult" path="/summary"/></returns>
        protected virtual async ValueTask<StageHandlerResult> SubTaskCompleted(IStageTypeHandlerContext context)
        {
            var info = context.Stage.InfoStorage;
            var count = TryGetSubtaskCount(context);

            if (count == 0
                && info.TryGetValue(ResultAction, out var actionUntyped))
            {
                var action = (StageHandlerAction) (int) NotNullOrThrow(actionUntyped);
                var transitTo = info.TryGet<Guid?>(ResultTransitTo);
                var keepStates = info.TryGet<bool?>(ResultKeepStates);

                info.Remove(ResultAction);
                info.Remove(ResultTransitTo);
                info.Remove(ResultKeepStates);

                return new StageHandlerResult(action, transitTo, keepStates);
            }

            return StageHandlerResult.InProgressResult;
        }

        /// <summary>
        /// Обрабатывает завершение этапа.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="result">Результат, с которым завершается этап.</param>
        /// <returns><inheritdoc cref="StageHandlerResult" path="/summary"/></returns>
        protected virtual StageHandlerResult StageCompleted(
            IStageTypeHandlerContext context,
            StageHandlerResult result)
        {
            if (TryGetSubtaskCount(context) == 0)
            {
                return result;
            }

            var info = context.Stage.InfoStorage;
            info.Add(ResultAction, Int32Boxes.Box((int) result.Action));

            var transitTo = result.TransitionID;
            if (transitTo.HasValue)
            {
                info.Add(ResultTransitTo, transitTo.Value);
            }

            var keepStates = result.KeepStageStates;
            if (keepStates.HasValue)
            {
                info.Add(ResultKeepStates, BooleanBoxes.Box(keepStates.Value));
            }

            return StageHandlerResult.InProgressResult;
        }

        /// <summary>
        /// Возвращает номер текущего цикла согласования из <see cref="WorkflowProcess.InfoStorage"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns>Номер цикла согласования.</returns>
        protected static int GetCycle(IStageTypeHandlerContext context) =>
            context.WorkflowProcess.InfoStorage.TryGet<int>(KrConstants.Keys.Cycle);

        /// <summary>
        /// Возвращает значение, показывающее, что управление передано после завершения доработки автором.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если управление передано после завершения доработки автором, иначе - <see langword="false"/>.</returns>
        protected static bool IsInterjected(
            IStageTypeHandlerContext context)
        {
            if (!context.Stage.InfoStorage.TryGetValue(Interjected, out var interjected))
            {
                return false;
            }

            switch (interjected)
            {
                // Легаси, когда хранился только флажок
                case bool interjectedBool:
                    return interjectedBool;
                // Хранится цикл интерджекта, чтобы не было пропуска этапа при вернулось->доработка->отзыв->запуск процесса
                case int interjectedInt:
                    var cycle = GetCycle(context);
                    return interjectedInt == cycle;
                case null:
                    return false;
                default:
                    throw new InvalidOperationException($"Invalid value of interjected key in approval stage {interjected}");
            }
        }

        /// <summary>
        /// Возвращает число дочерних заданий.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns>Число дочерних заданий.</returns>
        protected static int TryGetSubtaskCount(IStageTypeHandlerContext context) =>
            context.Stage.InfoStorage.TryGet<int>(SubtaskCount);

        /// <summary>
        /// Задаёт число дочерних заданий.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="count">Число дочерних заданий.</param>
        protected static void SetSubtaskCount(
            IStageTypeHandlerContext context,
            int count)
        {
            // Отрицательное значение свидетельствует об ошибке в логике работы со значением SubtaskCount.
            // Из-за этого его нельзя обрабатывать также как значение "0".
            ThrowIfNegative(count);

            if (count == 0)
            {
                context.Stage.InfoStorage.Remove(SubtaskCount);
            }
            else
            {
                context.Stage.InfoStorage[SubtaskCount] = Int32Boxes.Box(count);
            }
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task BeforeInitializationAsync(
            IStageTypeHandlerContext context)
        {
            await base.BeforeInitializationAsync(context);

            HandlerHelper.ClearCompletedTasks(context.Stage);
        }

        #endregion
    }
}
