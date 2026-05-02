#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Tessa.BusinessCalendar;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Notices;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Roles;
using Tessa.Workflow.Helpful;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers
{
    /// <summary>
    /// Обработчик этапа <see cref="StageTypeDescriptors.EditDescriptor"/>.
    /// </summary>
    /// <param name="krScope"><inheritdoc cref="KrScope" path="/summary"/></param>
    /// <param name="calendarService"><inheritdoc cref="CalendarService" path="/summary"/></param>
    /// <param name="session"><inheritdoc cref="Session" path="/summary"/></param>
    /// <param name="roleGetStrategy"><inheritdoc cref="RoleGetStrategy" path="/summary"/></param>
    /// <param name="contextRoleManager"><inheritdoc cref="ContextRoleManager" path="/summary"/></param>
    /// <param name="tasksRevoker"><inheritdoc cref="TasksRevoker" path="/summary"/></param>
    /// <param name="notificationManager"><inheritdoc cref="NotificationManager" path="/summary"/></param>
    /// <param name="cardCache"><inheritdoc cref="CardCache" path="/summary"/></param>
    /// <param name="dbScope"><inheritdoc cref="DbScope" path="/summary"/></param>
    public class EditStageTypeHandler(
        IKrScope krScope,
        IBusinessCalendarService calendarService,
        ISession session,
        IRoleGetStrategy roleGetStrategy,
        IContextRoleManager contextRoleManager,
        ICardContextRoleCache contextRoleCache,
        IStageTasksRevoker tasksRevoker,
        [Dependency(NotificationManagerNames.DeferredWithoutTransaction)] INotificationManager notificationManager,
        ICardCache cardCache,
        IDbScope dbScope) :
        StageTypeHandlerBase
    {
        #region Constants

        /// <summary>
        /// Имя ключа, по которому в <see cref="Stage.InfoStorage"/> содержится идентификатор этапа на который необходимо выполнить переход после завершения этапа "Доработка". Используется при возврате на этап согласование или подписание после доработки автором. Тип значения: <see cref="Guid"/>.
        /// </summary>
        public const string ReturnToStage = nameof(ReturnToStage);

        #endregion

        #region Protected Properties

        /// <inheritdoc cref="IKrScope" path="/summary"/>
        protected IKrScope KrScope { get; } = NotNullOrThrow(krScope);

        /// <inheritdoc cref="IBusinessCalendarService" path="/summary"/>
        protected IBusinessCalendarService CalendarService { get; } = NotNullOrThrow(calendarService);

        /// <inheritdoc cref="ISession" path="/summary"/>
        protected ISession Session { get; } = NotNullOrThrow(session);

        /// <inheritdoc cref="IRoleGetStrategy" path="/summary"/>
        protected IRoleGetStrategy RoleGetStrategy { get; } = NotNullOrThrow(roleGetStrategy);

        /// <inheritdoc cref="IContextRoleManager" path="/summary"/>
        protected IContextRoleManager ContextRoleManager { get; } = NotNullOrThrow(contextRoleManager);

        /// <inheritdoc cref="ICardContextRoleCache" path="/summary"/>
        protected ICardContextRoleCache ContextRoleCache { get; } = NotNullOrThrow(contextRoleCache);

        /// <inheritdoc cref="IStageTasksRevoker" path="/summary"/>
        protected IStageTasksRevoker TasksRevoker { get; } = NotNullOrThrow(tasksRevoker);

        /// <inheritdoc cref="INotificationManager" path="/summary"/>
        protected INotificationManager NotificationManager { get; } = NotNullOrThrow(notificationManager);

        /// <inheritdoc cref="ICardCache" path="/summary"/>
        protected ICardCache CardCache { get; } = NotNullOrThrow(cardCache);

        /// <inheritdoc cref="IDbScope" path="/summary"/>
        protected IDbScope DbScope { get; } = NotNullOrThrow(dbScope);

        #endregion

        #region Protected Methods

        /// <summary>
        /// Начинает новый цикл согласования.
        /// </summary>
        /// <param name="context">Контекст обработчика этапа.</param>
        /// <param name="result">Результат выполнения или значение <see langword="null"/>, если необходимо завершить этап, если не выполняется обработка доработки автором, то в этом случае игнорируется. Если не задан, то этап завершается с результатом выполнения <see cref="StageHandlerResult.CompleteResult"/>.</param>
        /// <returns>Результат выполнения этапа.</returns>
        protected virtual StageHandlerResult StartApproval(
            IStageTypeHandlerContext context,
            StageHandlerResult? result = null)
        {
            var returnToStage = context.Stage.InfoStorage.TryGet<Guid?>(ReturnToStage);
            if (returnToStage.HasValue)
            {
                context.Stage.InfoStorage.Remove(ReturnToStage);
                return StageHandlerResult.Transition(returnToStage.Value, keepStageStates: true);
            }

            var fields = context.ContextualSatellite!.Sections[KrConstants.KrApprovalCommonInfo.Name].Fields;
            fields[KrConstants.KrApprovalCommonInfo.ApprovedBy] = string.Empty;
            fields[KrConstants.KrApprovalCommonInfo.DisapprovedBy] = string.Empty;

            return result ?? StageHandlerResult.CompleteResult;
        }

        /// <summary>
        /// Увеличивает номер цикла согласования, если это разрешено настройками этапа.
        /// </summary>
        /// <param name="context">Контекст обработчика этапа.</param>
        protected virtual void TryIncrementCycle(
            IStageTypeHandlerContext context)
        {
            if (context.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrEditSettingsVirtual.IncrementCycle) == true)
            {
                var info = context.WorkflowProcess.InfoStorage;
                info[KrConstants.Keys.Cycle] = Int32Boxes.Box(info.TryGet<int>(KrConstants.Keys.Cycle) + 1);
            }
        }

        /// <summary>
        /// Возвращает значение, показывающее, выполнялся ли предыдущий этап из другой группы этапов или карточки.
        /// </summary>
        /// <param name="context">Контекст обработчика этапа.</param>
        /// <returns>Значение <see langword="true"/>, если отсутствует информация о ходе выполнения процесса или, если есть этап выполнявшийся в другой группе или карточке или такой не найден, иначе - <see langword="false"/>, если найден не скрытый предыдущий этап выполнявшийся в группе и текущей карточке, что и текущий этап.</returns>
        protected virtual bool TransitFromDifferentGroup(
            IStageTypeHandlerContext context)
        {
            var trace = this.KrScope.GetKrProcessRunnerTrace();

            if (trace is null
                || trace.Count == 0)
            {
                return true;
            }

            var cardID = context.MainCardID;
            var currentStageGroupID = context.Stage.StageGroupID;

            for (var i = trace.Count - 1; 0 <= i; i--)
            {
                var traceItem = trace[i];

                if (traceItem.ProcessID != context.ProcessInfo!.ProcessID)
                {
                    continue;
                }

                var prevStage = traceItem.Stage;

                if (prevStage.StageGroupID != currentStageGroupID
                    || traceItem.CardID != cardID)
                {
                    return true;
                }

                if (!prevStage.Hidden)
                {
                    return false;
                }
            }

            return true;
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

        /// <inheritdoc/>
        public override async Task<StageHandlerResult> HandleStageStartAsync(
            IStageTypeHandlerContext context)
        {
            var stage = context.Stage;
            var settings = stage.SettingsStorage;
            var returnToStage = stage.InfoStorage.ContainsKey(ReturnToStage);
            if (!returnToStage)
            {
                this.TryIncrementCycle(context);
            }

            if (settings.TryGet<bool?>(KrConstants.KrEditSettingsVirtual.DoNotSkipStage) != true
                && this.TransitFromDifferentGroup(context))
            {
                SetVisibility(false);
                return this.StartApproval(context, StageHandlerResult.SkipResult);
            }

            SetVisibility(true);

            var author = await HandlerHelper.GetStageAuthorAsync(
                context,
                this.RoleGetStrategy,
                this.ContextRoleManager,
                this.ContextRoleCache,
                this.Session,
                context.ValidationResult,
                context.CancellationToken);
            if (author is null)
            {
                return StageHandlerResult.EmptyResult;
            }

            var authorID = author.AuthorID;

            var digest = settings.TryGet<string>(KrConstants.KrEditSettingsVirtual.Comment);
            var groupID = await HandlerHelper.GetTaskHistoryGroupAsync(
                context,
                this.KrScope,
                context.ValidationResult,
                context.CancellationToken);
            var incrementCycle = settings.TryGet<bool?>(KrConstants.KrEditSettingsVirtual.IncrementCycle) == true;

            var taskInfo = await context.WorkflowAPI!.SendTaskAsync(
                returnToStage || !incrementCycle
                    ? DefaultTaskTypes.KrEditInterjectTypeID
                    : DefaultTaskTypes.KrEditTypeID,
                string.Empty,
                settings.Get<Guid>(KrConstants.KrSinglePerformerVirtual.PerformerID),
                settings.Get<string>(KrConstants.KrSinglePerformerVirtual.PerformerName),
                context.ValidationResult,
                modifyTaskAction: (p, _) =>
                {
                    if (settings.TryGet<bool>(KrConstants.KrEditSettingsVirtual.HasEditApprovalSchemeAccess))
                    {
                        p.Settings ??= [];
                        p.Settings[WorkflowConstants.NamesKeys.CanEditApprovalScheme] = BooleanBoxes.True;
                    }

                    p.AddAuthor(authorID);
                    p.Planned = stage.Planned;
                    p.PlannedWorkingDays = context.Stage.Planned.HasValue ? null : context.Stage.TimeLimitOrDefault;
                    p.GroupRowID = groupID;
                    if (!string.IsNullOrWhiteSpace(digest))
                    {
                        p.Digest = digest;
                    }

                    var (kindID, kindCaption) = HandlerHelper.GetTaskKind(context);
                    WorkflowCommonHelper.SetTaskKind(p, kindID, kindCaption, context);

                    return ValueTask.CompletedTask;
                },
                cancellationToken: context.CancellationToken);

            if (taskInfo is null)
            {
                return StageHandlerResult.EmptyResult;
            }

            var sentTask = taskInfo.Task;
            sentTask.Flags |= CardTaskFlags.CreateHistoryItem;
            context.ContextualSatellite.AddToHistory(sentTask.RowID, context.WorkflowProcess.InfoStorage.TryGet(KrConstants.Keys.Cycle, 1));
            await context.WorkflowAPI.AddActiveTaskAsync(
                sentTask.RowID,
                context.ValidationResult,
                context.CancellationToken);

            await CardComponentHelper.FillTaskAssignedRolesAsync(
                sentTask,
                this.DbScope,
                cancellationToken: context.CancellationToken);

            context.ValidationResult.Add(
                await this.NotificationManager.SendAsync(
                    DefaultNotifications.TaskNotification,
                    sentTask.TaskAssignedRoles
                        .Where(static x => x.TaskRoleID == CardFunctionRoles.PerformerID)
                        .Select(static x => x.RoleID)
                        .ToArray(),
                    new NotificationSendContext
                    {
                        MainCardID = context.MainCardID ?? Guid.Empty,
                        TaskTypeID = sentTask.TypeID,
                        Info = DefaultNotificationHelper.GetInfoWithTask(sentTask),
                        ModifyEmailActionAsync = (email, _) =>
                        {
                            DefaultNotificationHelper.ModifyTaskCaption(
                                email,
                                sentTask);

                            return Task.CompletedTask;
                        },
                        GetCardFuncAsync = (validationResult, ct) =>
                            context.MainCardAccessStrategy.GetCardAsync(
                                validationResult: validationResult,
                                cancellationToken: ct),
                    },
                    context.CancellationToken));

            if ((settings.TryGet<bool?>(KrConstants.KrEditSettingsVirtual.ChangeState) ?? default)
                && !returnToStage
                && !this.KrScope.Info.TryGet<bool>(KrConstants.Keys.IgnoreChangeState))
            {
                context.WorkflowProcess.State = KrState.Editing;
            }

            return StageHandlerResult.InProgressResult;

            void SetVisibility(
                bool visible)
            {
                if (settings.TryGet<bool?>(KrConstants.KrEditSettingsVirtual.ManageStageVisibility) == true)
                {
                    context.Stage.Hidden = !visible;
                    context.Stage.AddAutomaticallyChangedValue(nameof(Stage.Hidden));
                }
            }
        }

        /// <inheritdoc/>
        public override async Task<StageHandlerResult> HandleTaskCompletionAsync(
            IStageTypeHandlerContext context)
        {
            var task = context.TaskInfo!.Task;
            var taskTypeID = task.TypeID;
            if (taskTypeID != DefaultTaskTypes.KrEditTypeID
                 && taskTypeID != DefaultTaskTypes.KrEditInterjectTypeID)
            {
                return StageHandlerResult.EmptyResult;
            }

            if (task.Card.Sections.TryGetValue(KrConstants.KrTask.Name, out var commSec))
            {
                var comment = commSec.RawFields.TryGet<string>(KrConstants.KrTask.Comment);

                if (taskTypeID != DefaultTaskTypes.KrEditInterjectTypeID)
                {
                    context.WorkflowProcess.AuthorComment = comment;
                }

                if (!string.IsNullOrEmpty(comment))
                {
                    task.Result = comment;

                    await WorkflowEngineHelper.UpdateTaskHistoryResultAsync(
                        this.DbScope,
                        task,
                        context.CancellationToken);
                }
            }

            await context.WorkflowAPI!.RemoveActiveTaskAsync(
                context.TaskInfo.Task.RowID,
                context.ValidationResult,
                context.CancellationToken);

            HandlerHelper.AppendToCompletedTasksWithPreparing(
                context.Stage,
                task);

            return this.StartApproval(context);
        }

        /// <inheritdoc/>
        public override async Task<bool> HandleStageInterruptAsync(
            IStageTypeHandlerContext context)
        {
            context.Stage.InfoStorage.Remove(ReturnToStage);
            return await this.TasksRevoker.RevokeAllStageTasksAsync(
                new StageTaskRevokerContext(
                    context,
                    context.ValidationResult,
                    context.CancellationToken));
        }

        #endregion
    }
}
