using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.BusinessCalendar;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.ComponentModel;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Notices;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Unity;
using static Tessa.Extensions.Default.Shared.Workflow.KrProcess.KrConstants;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers
{
    /// <summary>
    /// Обработчик этапа <see cref="Shared.Workflow.KrProcess.StageTypeDescriptors.UniversalTaskDescriptor"/>.
    /// </summary>
    public class UniversalTaskStageTypeHandler(
        IKrScope krScope,
        IRoleGetStrategy roleGetStrategy,
        IContextRoleManager contextRoleManager,
        ICardContextRoleCache contextRoleCache,
        ISession session,
        IBusinessCalendarService calendarService,
        IStageTasksRevoker tasksRevoker,
        [Dependency(NotificationManagerNames.DeferredWithoutTransaction)] INotificationManager notificationManager,
        ICardMetadata cardMetadata)
        : StageTypeHandlerBase
    {
        #region Protected Properties and Constants

        /// <summary>
        /// Возвращает объект предоставляющий методы для работы с текущим контекстом расширений типового расширения и использования разделяемых объектов карточек.
        /// </summary>
        protected IKrScope KrScope { get; } = krScope;

        /// <summary>
        /// Стратегия для получения информации о ролях.
        /// </summary>
        protected IRoleGetStrategy RoleGetStrategy { get; } = roleGetStrategy;

        /// <summary>
        /// Возвращает обработчик контекстных ролей.
        /// </summary>
        protected IContextRoleManager ContextRoleManager { get; } = contextRoleManager;

        /// <inheritdoc cref="ICardContextRoleCache" path="/summary"/>
        protected ICardContextRoleCache ContextRoleCache { get; } = NotNullOrThrow(contextRoleCache);

        /// <summary>
        /// Возвращает сессию пользователя.
        /// </summary>
        protected ISession Session { get; } = session;

        /// <summary>
        /// Возвращает объект предоставляющий методы для работы с бизнес календарём.
        /// </summary>
        protected IBusinessCalendarService CalendarService { get; } = calendarService;

        /// <summary>
        /// Возвращает объект выполняющий отзыв заданий этапа.
        /// </summary>
        protected IStageTasksRevoker TasksRevoker { get; } = tasksRevoker;

        /// <summary>
        /// Возвращает объект для отправки уведомлений, построенных по карточке уведомления.
        /// </summary>
        protected INotificationManager NotificationManager { get; } = notificationManager;

        /// <summary>
        /// Возвращает метаинформацию, необходимую для использования типов карточек совместно с пакетом карточек.
        /// </summary>
        protected ICardMetadata CardMetadata { get; } = cardMetadata;

        /// <summary>
        /// Ключ по которому в <see cref="Stage.InfoStorage"/> содержится общее число заданий. Тип значения: <see cref="int"/>.
        /// </summary>
        protected const string TotalTasksCountKey = StorageHelper.SystemKeyPrefix + "TotalTasksCount";

        /// <summary>
        /// Ключ по которому в <see cref="Stage.InfoStorage"/> содержится число завершённых заданий. Тип значения: <see cref="int"/>.
        /// </summary>
        protected const string CompletedTasksCountKey = StorageHelper.SystemKeyPrefix + "CompletedTasksCount";

        /// <inheritdoc cref="Keys.Tasks"/>
        protected const string TasksKey = Keys.Tasks;

        #endregion

        #region Protected Methods

        /// <summary>
        /// Асинхронно отправляет настраиваемое задание.
        /// </summary>
        /// <param name="context">Контекст обработчика этапа.</param>
        /// <returns>Результат обработки этапа.</returns>
        protected virtual async Task<StageHandlerResult> SendUniversalTaskAsync(IStageTypeHandlerContext context)
        {
            var performers = context.Stage.Performers;

            context.Stage.InfoStorage[CompletedTasksCountKey] = Int32Boxes.Zero;
            context.Stage.InfoStorage[TotalTasksCountKey] = Int32Boxes.Box(performers.Count);
            context.Stage.InfoStorage[TasksKey] = null;

            if (performers.Count == 0)
            {
                return StageHandlerResult.CompleteResult;
            }

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
            var stageOptionsRows = context.Stage.SettingsStorage.TryGet<IList>(KrUniversalTaskOptionsSettingsVirtual.Synthetic);

            if (stageOptionsRows is null
                || stageOptionsRows.Count == 0)
            {
                context.ValidationResult.AddError(this, "$KrProcess_UniversalTask_NoCompletionOptions");
                return StageHandlerResult.EmptyResult;
            }

            var taskGroupRowID = await HandlerHelper.GetTaskHistoryGroupAsync(
                context,
                this.KrScope,
                context.ValidationResult,
                context.CancellationToken);

            foreach (var performer in performers)
            {
                var taskInfo = await context.WorkflowAPI.SendTaskAsync(
                    DefaultTaskTypes.KrUniversalTaskTypeID,
                    context.Stage.SettingsStorage.TryGet<string>(KrUniversalTaskSettingsVirtual.Digest),
                    performer.PerformerID,
                    performer.PerformerName,
                    context.ValidationResult,
                    modifyTaskAction: (t, ct) =>
                    {
                        t.AddAuthor(authorID);
                        t.GroupRowID = taskGroupRowID;
                        t.Planned = context.Stage.Planned;
                        t.PlannedWorkingDays = context.Stage.Planned.HasValue ? null : context.Stage.TimeLimitOrDefault;

                        var (kindID, kindCaption) = HandlerHelper.GetTaskKind(context);
                        WorkflowCommonHelper.SetTaskKind(t, kindID, kindCaption, context);

                        return new ValueTask();
                    },
                    cancellationToken: context.CancellationToken);

                if (taskInfo is null)
                {
                    return StageHandlerResult.EmptyResult;
                }

                await context.WorkflowAPI.AddActiveTaskAsync(
                    taskInfo.Task.RowID,
                    context.ValidationResult,
                    context.CancellationToken);

                var task = taskInfo.Task;
                task.Flags |= CardTaskFlags.CreateHistoryItem;
                context.ContextualSatellite.AddToHistory(task.RowID, context.WorkflowProcess.InfoStorage.TryGet(Keys.Cycle, 1));

                var optionsSection = task.Card.Sections.GetOrAddTable(KrUniversalTaskOptions.Name);

                foreach (Dictionary<string, object> row in stageOptionsRows)
                {
                    var newRow = optionsSection.Rows.Add();
                    newRow.RowID = Guid.NewGuid();
                    newRow[KrUniversalTaskOptions.OptionID] = row.TryGet(KrUniversalTaskOptionsSettingsVirtual.OptionID, GuidBoxes.Empty);
                    newRow[KrUniversalTaskOptions.Caption] = row.TryGet(KrUniversalTaskOptionsSettingsVirtual.Caption, default(object));
                    newRow[KrUniversalTaskOptions.ShowComment] = row.TryGet(KrUniversalTaskOptionsSettingsVirtual.ShowComment, BooleanBoxes.False);
                    newRow[KrUniversalTaskOptions.Additional] = row.TryGet(KrUniversalTaskOptionsSettingsVirtual.Additional, BooleanBoxes.False);
                    newRow[KrUniversalTaskOptions.Order] = row.TryGet(KrUniversalTaskOptionsSettingsVirtual.Order, default(object));
                    newRow[KrUniversalTaskOptions.Message] = row.TryGet(KrUniversalTaskOptionsSettingsVirtual.Message, default(object));
                    newRow.State = CardRowState.Inserted;
                }

                if (context.CardExtensionContext is ICardStoreExtensionContext storeContext)
                {
                    await CardComponentHelper.FillTaskAssignedRolesAsync(task, storeContext.DbScope!, cancellationToken: context.CancellationToken);
                }

                context.ValidationResult.Add(
                    await this.NotificationManager.SendAsync(
                        DefaultNotifications.TaskNotification,
                        task.TaskAssignedRoles.Where(x => x.TaskRoleID == CardFunctionRoles.PerformerID).Select(x => x.RoleID).ToArray(),
                        new NotificationSendContext
                        {
                            MainCardID = context.MainCardID ?? Guid.Empty,
                            TaskTypeID = task.TypeID,
                            Info = DefaultNotificationHelper.GetInfoWithTask(task),
                            ModifyEmailActionAsync = async (email, ct) =>
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
            }

            return StageHandlerResult.InProgressResult;
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task BeforeInitializationAsync(IStageTypeHandlerContext context)
        {
            await base.BeforeInitializationAsync(context);

            HandlerHelper.ClearCompletedTasks(context.Stage);
        }

        /// <inheritdoc/>
        public override Task<StageHandlerResult> HandleStageStartAsync(IStageTypeHandlerContext context) =>
            this.SendUniversalTaskAsync(context);

        /// <inheritdoc/>
        public override async Task<StageHandlerResult> HandleTaskCompletionAsync(IStageTypeHandlerContext context)
        {
            var totalCount = context.Stage.InfoStorage.TryGet<int>(TotalTasksCountKey);
            var completedCount = context.Stage.InfoStorage.TryGet<int>(CompletedTasksCountKey) + 1;
            var task = context.TaskInfo.Task;
            var optionID = task.Info.TryGet<Guid>(KrUniversalTaskStoreExtension.OptionIDKey);
            var comment = task.Card.Sections.TryGet(KrTask.Name)?.RawFields.TryGet<string>(KrTask.Comment);

            var stageOptionsRows = context.Stage.SettingsStorage.TryGet<IList>(KrUniversalTaskOptionsSettingsVirtual.Synthetic);
            var optionRow =
                stageOptionsRows.Cast<Dictionary<string, object>>()
                    .FirstOrDefault(row =>
                        row.TryGet<Guid?>(KrUniversalTaskOptionsSettingsVirtual.OptionID) == optionID);

            string optionName;

            if (optionRow is null)
            {
                if (((ICardStoreExtensionContext) context.CardExtensionContext).Request.ServiceType != CardServiceType.Default)
                {
                    context.ValidationResult.AddError(
                        this,
                        "$KrProcess_UniversalTask_NotFoundCompletionOption",
                        optionID.ToString());

                    return StageHandlerResult.EmptyResult;
                }

                if (task.Info.TryGetValue(KrUniversalTaskStoreExtension.OptionCaptionKey, out var optionCaptionObj))
                {
                    optionName = (string) optionCaptionObj;
                }
                else
                {
                    if (!(await this.CardMetadata.GetEnumerationsAsync(context.CancellationToken))
                        .CompletionOptions.TryGetValue(optionID, out var completionOption))
                    {
                        ValidationSequence
                            .Begin(context.ValidationResult)
                            .SetObjectName(this)
                            .Error(CardValidationKeys.UnknownTaskOption, task.RowID, optionID)
                            .End();

                        return StageHandlerResult.EmptyResult;
                    }

                    optionName = completionOption.Caption;
                }
            }
            else
            {
                optionName = optionRow.TryGet<string>(KrUniversalTaskOptionsSettingsVirtual.Caption);
            }

            HandlerHelper.AppendToCompletedTasksWithPreparing(
                context.Stage,
                task,
                storedTask =>
                {
                    storedTask.OptionID = optionID;

                    var storedTaskStorage = storedTask.GetStorage();

                    storedTaskStorage["Comment"] = comment;
                    storedTaskStorage["OptionName"] = optionName;
                    storedTaskStorage["CompletedByID"] = this.Session.User.ID;
                    storedTaskStorage["CompletedByName"] = this.Session.User.Name;
                    storedTaskStorage["Completed"] = context.CardExtensionContext is CardStoreExtensionContext storeContext
                        ? storeContext.StoreDateTime
                        : DateTime.UtcNow;
                });

            context.Stage.InfoStorage[CompletedTasksCountKey] = Int32Boxes.Box(completedCount);

            return totalCount <= completedCount
                ? StageHandlerResult.CompleteResult
                : StageHandlerResult.InProgressResult;
        }

        /// <inheritdoc/>
        public override Task<bool> HandleStageInterruptAsync(IStageTypeHandlerContext context) =>
            this.TasksRevoker.RevokeAllStageTasksAsync(
                new StageTaskRevokerContext(
                    context,
                    context.ValidationResult,
                    context.CancellationToken));

        #endregion
    }
}
