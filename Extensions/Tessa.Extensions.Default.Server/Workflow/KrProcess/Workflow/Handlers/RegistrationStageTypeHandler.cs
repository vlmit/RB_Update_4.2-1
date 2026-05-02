#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.BusinessCalendar;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.ComponentModel;
using Tessa.Cards.Extensions;
using Tessa.Cards.Numbers;
using Tessa.Cards.Workflow;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Events;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Notices;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
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
    /// Обработчик этапа <see cref="StageTypeDescriptors.RegistrationDescriptor"/>.
    /// </summary>
    public class RegistrationStageTypeHandler(
        INumberDirectorContainer numberDirectorContainer,
        IKrScope krScope,
        ISession session,
        ICardMetadata cardMetadata,
        IKrStageSerializer serializer,
        IKrEventManager eventManager,
        IBusinessCalendarService calendarService,
        IRoleGetStrategy roleGetStrategy,
        IContextRoleManager contextRoleManager,
        ICardContextRoleCache contextRoleCache,
        IStageTasksRevoker tasksRevoker,
        [Dependency(NotificationManagerNames.DeferredWithoutTransaction)] INotificationManager notificationManager)
        : StageTypeHandlerBase
    {
        #region Protected Properties

        /// <inheritdoc cref="INumberDirectorContainer" path="/summary"/>
        protected INumberDirectorContainer NumberDirectorContainer { get; set; } = NotNullOrThrow(numberDirectorContainer);

        /// <inheritdoc cref="IKrScope" path="/summary"/>
        protected IKrScope KrScope { get; set; } = NotNullOrThrow(krScope);

        /// <inheritdoc cref="ISession" path="/summary"/>
        protected ISession Session { get; set; } = NotNullOrThrow(session);

        /// <inheritdoc cref="ICardMetadata" path="/summary"/>
        protected ICardMetadata CardMetadata { get; set; } = NotNullOrThrow(cardMetadata);

        /// <inheritdoc cref="IKrStageSerializer" path="/summary"/>
        protected IKrStageSerializer Serializer { get; set; } = NotNullOrThrow(serializer);

        /// <inheritdoc cref="IKrEventManager" path="/summary"/>
        protected IKrEventManager EventManager { get; set; } = NotNullOrThrow(eventManager);

        /// <inheritdoc cref="IBusinessCalendarService" path="/summary"/>
        protected IBusinessCalendarService CalendarService { get; set; } = NotNullOrThrow(calendarService);

        /// <inheritdoc cref="IRoleGetStrategy" path="/summary"/>
        protected IRoleGetStrategy RoleGetStrategy { get; set; } = NotNullOrThrow(roleGetStrategy);

        /// <inheritdoc cref="IContextRoleManager" path="/summary"/>
        protected IContextRoleManager ContextRoleManager { get; set; } = NotNullOrThrow(contextRoleManager);

        /// <inheritdoc cref="ICardContextRoleCache" path="/summary"/>
        protected ICardContextRoleCache ContextRoleCache { get; } = NotNullOrThrow(contextRoleCache);

        /// <inheritdoc cref="IStageTasksRevoker" path="/summary"/>
        protected IStageTasksRevoker TasksRevoker { get; set; } = NotNullOrThrow(tasksRevoker);

        /// <inheritdoc cref="INotificationManager" path="/summary"/>
        protected INotificationManager NotificationManager { get; } = NotNullOrThrow(notificationManager);

        #endregion

        #region Protected Methods

        /// <summary>
        /// Выполняет регистрацию документа.
        /// </summary>
        /// <param name="context">Контекст обработчика этапа.</param>
        /// <param name="taskInfo">Регистрация производится после задания.</param>
        /// <returns>Результат обработки этапа.</returns>
        protected virtual async Task<StageHandlerResult> SyncRegistrationAsync(
            IStageTypeHandlerContext context,
            IWorkflowTaskInfo? taskInfo = null)
        {
            // Непосредственная регистрация карточки.
            if (context is { MainCardID: not null, MainCardType: not null })
            {
                var mainCard = await this.KrScope.GetMainCardAsync(
                    context.MainCardID.Value,
                    cancellationToken: context.CancellationToken);

                if (mainCard is null)
                {
                    return StageHandlerResult.EmptyResult;
                }

                // выделение номера при регистрации
                var numberProvider = this.NumberDirectorContainer.GetProvider(context.MainCardType.ID);
                var numberDirector = numberProvider.GetDirector();
                var numberComposer = numberProvider.GetComposer();
                var numberContext = await numberDirector.CreateContextAsync(
                    numberComposer,
                    mainCard,
                    context.MainCardType,
                    CardServiceType.Default,
                    context.CardExtensionContext is ICardStoreExtensionContext storeContext
                        ? storeContext.Request.Info
                        : null,
                    context.CardExtensionContext,
                    transactionMode: NumberTransactionMode.SeparateTransaction,
                    context.CancellationToken);

                await numberDirector.NotifyOnRegisteringCardAsync(numberContext, context.CancellationToken);
                context.ValidationResult.Add(numberContext.ValidationResult);

                await this.EventManager.RaiseAsync(DefaultEventTypes.RegistrationEvent, context, cancellationToken: context.CancellationToken);

                var cycle = this.GetCycle(context);
                if (taskInfo is not null)
                {
                    context.ContextualSatellite.AddToHistory(taskInfo.Task.RowID, cycle);
                }
                else
                {
                    var fakeHistoryRecord = await this.CreateRegistrationTaskHistoryItemAsync(context);

                    if (fakeHistoryRecord is null)
                    {
                        return StageHandlerResult.EmptyResult;
                    }

                    mainCard.TaskHistory.Add(fakeHistoryRecord);
                    context.ContextualSatellite.AddToHistory(fakeHistoryRecord.RowID, cycle);
                }
            }

            context.WorkflowProcess.State = KrState.Registered;
            return StageHandlerResult.CompleteResult;
        }

        /// <summary>
        /// Выполняет регистрацию документа с предварительной отправкой задания регистрации.
        /// </summary>
        /// <param name="context">Контекст обработчика этапа.</param>
        /// <returns>Результат обработки этапа.</returns>
        protected virtual async Task<StageHandlerResult> AsyncRegistrationAsync(
            IStageTypeHandlerContext context)
        {
            var api = context.WorkflowAPI!;

            // Получаем исполнителя, указанного в настройках этапа.
            var performer = context.Stage.Performer;
            if (performer is null)
            {
                context.ValidationResult.AddError(this, "$KrStages_Registration_PerformerNotSpecified");
                return StageHandlerResult.EmptyResult;
            }

            var performerID = performer.PerformerID;
            var performerName = performer.PerformerName;

            // Установка в карточке состояния "На регистрации"
            context.WorkflowProcess.State = KrState.Registration;

            var digest = context.Stage.SettingsStorage.TryGet<string>(KrRegistrationStageSettingsVirtual.Comment)
                ?? context.Stage.Name;

            var groupID = await HandlerHelper.GetTaskHistoryGroupAsync(
                context,
                this.KrScope,
                context.ValidationResult,
                context.CancellationToken);

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
            var (kindID, kindCaption) = HandlerHelper.GetTaskKind(context);
            // Отправка задания регистрации
            var taskInfo = await api.SendTaskAsync(
                DefaultTaskTypes.KrRegistrationTypeID,
                digest,
                performerID,
                performerName,
                context.ValidationResult,
                modifyTaskAction: (t, _) =>
                {
                    t.AddAuthor(authorID);
                    t.Planned = context.Stage.Planned;
                    t.PlannedWorkingDays = context.Stage.Planned.HasValue ? null : context.Stage.TimeLimitOrDefault;
                    t.GroupRowID = groupID;
                    t.Flags |= CardTaskFlags.CreateHistoryItem;
                    WorkflowCommonHelper.SetTaskKind(t, kindID, kindCaption, context);

                    return ValueTask.CompletedTask;
                },
                cancellationToken: context.CancellationToken);

            if (taskInfo is null)
            {
                return StageHandlerResult.EmptyResult;
            }

            // Добавление задания в список активных заданий,
            // которые будут отображаться в таблице над заданиями.
            await api.AddActiveTaskAsync(
                taskInfo.Task.RowID,
                context.ValidationResult,
                context.CancellationToken);

            if (context.CardExtensionContext is ICardStoreExtensionContext storeContext)
            {
                await CardComponentHelper.FillTaskAssignedRolesAsync(taskInfo.Task, storeContext.DbScope!, cancellationToken: context.CancellationToken);
            }

            context.ValidationResult.Add(
                await this.NotificationManager.SendAsync(
                    DefaultNotifications.TaskNotification,
                    taskInfo.Task.TaskAssignedRoles.Where(x => x.TaskRoleID == CardFunctionRoles.PerformerID).Select(x => x.RoleID).ToArray(),
                    new NotificationSendContext
                    {
                        MainCardID = context.MainCardID ?? Guid.Empty,
                        TaskTypeID = taskInfo.Task.TypeID,
                        Info = DefaultNotificationHelper.GetInfoWithTask(taskInfo.Task),
                        ModifyEmailActionAsync = async (email, ct) =>
                        {
                            DefaultNotificationHelper.ModifyTaskCaption(
                                email,
                                taskInfo.Task);
                        },
                        GetCardFuncAsync = (validationResult, ct) =>
                            context.MainCardAccessStrategy.GetCardAsync(
                                validationResult: validationResult,
                                cancellationToken: ct),
                    },
                    context.CancellationToken));

            // Результат говорит подсистеме маршрутов о том, что этап находится в процессе выполнения
            return StageHandlerResult.InProgressResult;
        }

        /// <summary>
        /// Создаёт запись в истории действий о выполнении регистрации.
        /// </summary>
        /// <param name="context">Контекст обработчика этапа.</param>
        /// <returns>Запись истории заданий или значение <see langword="null"/>, если произошла ошибка.</returns>
        protected virtual async Task<CardTaskHistoryItem?> CreateRegistrationTaskHistoryItemAsync(IStageTypeHandlerContext context)
        {
            if (!(await this.CardMetadata.GetCardTypesAsync(context.CancellationToken))
                .TryGetValue(DefaultTaskTypes.KrRegistrationTypeID, out var taskType))
            {
                return null;
            }

            const string result = "$ApprovalHistory_DocumentRegistered";
            var optionID = DefaultCompletionOptions.RegisterDocument;
            var userID = this.Session.User.ID;
            var userName = this.Session.User.Name;

            var option = (await this.CardMetadata.GetEnumerationsAsync(context.CancellationToken)).CompletionOptions[optionID];
            var groupID = await HandlerHelper.GetTaskHistoryGroupAsync(
                context,
                this.KrScope,
                context.ValidationResult,
                context.CancellationToken);

            // Временная зона текущего сотрудника и календарь, для записи в историю заданий
            var userZoneInfo = await this.CalendarService.GetRoleTimeZoneInfoAsync(userID, context.CancellationToken);
            var userCalendarInfo = await this.CalendarService.GetRoleCalendarInfoAsync(userID, context.CancellationToken);
            if (userCalendarInfo is null)
            {
                context.ValidationResult.AddError(this, await LocalizeFormatAsync("$KrMessages_NoRoleCalendar", userID));
                return null;
            }

            var utcNow = DateTime.UtcNow;
            var settings = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [TaskHistorySettingsKeys.PerformerID] = userID,
                [TaskHistorySettingsKeys.PerformerName] = userName
            };

            var item = new CardTaskHistoryItem
            {
                State = CardTaskHistoryState.Inserted,
                RowID = Guid.NewGuid(),
                TypeID = DefaultTaskTypes.KrRegistrationTypeID,
                TypeName = taskType.Name,
                TypeCaption = taskType.Caption,
                Created = utcNow,
                Planned = utcNow,
                InProgress = utcNow,
                Completed = utcNow,
                UserID = userID,
                UserName = userName,
                AuthorID = userID,
                AuthorName = userName,
                Result = result,
                OptionID = optionID,
                OptionCaption = option.Caption,
                OptionName = option.Name,
                ParentRowID = null,
                CompletedByID = userID,
                CompletedByName = userName,
                CompletedByRole = userName,
                GroupRowID = groupID,
                TimeZoneID = userZoneInfo.TimeZoneID,
                TimeZoneUtcOffsetMinutes = (int?) userZoneInfo.TimeZoneUtcOffset.TotalMinutes,
                CalendarID = userCalendarInfo.CalendarID,
                Settings = settings,
                AssignedOnRole = userName
            };

            return item;
        }

        /// <summary>
        /// Возвращает номер текущего цикла согласования.
        /// </summary>
        /// <param name="context">Контекст обработчика этапа.</param>
        /// <returns>Номер текущего цикла согласования.</returns>
        protected virtual int GetCycle(IStageTypeHandlerContext context)
        {
            if (context is { RunnerMode: KrProcessRunnerMode.Async, ProcessInfo.ProcessTypeName: KrProcessName })
            {
                // Для основного процесса цикл лежит в его инфо.
                return context.WorkflowProcess.InfoStorage.TryGet<int?>(Keys.Cycle) ?? 1;
            }

            return ProcessInfoCacheHelper.Get(this.Serializer, context.ContextualSatellite)?.TryGet<int?>(Keys.Cycle)
                ?? 0;
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task BeforeInitializationAsync(IStageTypeHandlerContext context)
        {
            await base.BeforeInitializationAsync(context);

            HandlerHelper.ClearCompletedTasks(context.Stage);
        }

        /// <inheritdoc />
        public override async Task<StageHandlerResult> HandleStageStartAsync(IStageTypeHandlerContext context)
        {
            // Запоминаем состояние до начала регистрации.
            var info = ProcessInfoCacheHelper.Get(this.Serializer, context.ContextualSatellite);
            info[Keys.StateBeforeRegistration] = Int32Boxes.Box((int) context.WorkflowProcess.State);

            // При запуске этапа определяем, в каком режиме сейчас идет выполнение
            switch (context.RunnerMode)
            {
                case KrProcessRunnerMode.Sync:
                    // Выполнение в синхронном режиме, отправка заданий запрещена
                    // Выполняем регистрацию
                    return await this.SyncRegistrationAsync(context);
                case KrProcessRunnerMode.Async:
                    var withoutTask =
                        context.Stage.SettingsStorage.TryGet<bool?>(KrRegistrationStageSettingsVirtual.WithoutTask);
                    return await (withoutTask == true
                        ? this.SyncRegistrationAsync(context)
                        : this.AsyncRegistrationAsync(context));
                default:
                    throw ArgumentOutOfRange(context.RunnerMode);
            }
        }

        /// <inheritdoc />
        public override async Task<StageHandlerResult> HandleTaskCompletionAsync(IStageTypeHandlerContext context)
        {
            var baseResult = await base.HandleTaskCompletionAsync(context);
            if (baseResult != StageHandlerResult.EmptyResult)
            {
                return baseResult;
            }

            var taskInfo = NotNullOrThrow(context.TaskInfo);
            var task = taskInfo.Task;

            HandlerHelper.AppendToCompletedTasksWithPreparing(context.Stage, task);

            if (task.TypeID == DefaultTaskTypes.KrRegistrationTypeID
                && task.OptionID == DefaultCompletionOptions.RegisterDocument)
            {
                await context.WorkflowAPI!.TryRemoveActiveTaskAsync(
                    task.RowID,
                    context.ValidationResult,
                    context.CancellationToken);

                await this.SyncRegistrationAsync(context, taskInfo);
                return StageHandlerResult.CompleteResult;
            }

            return StageHandlerResult.EmptyResult;
        }

        /// <inheritdoc />
        public override Task<bool> HandleStageInterruptAsync(IStageTypeHandlerContext context) =>
            this.TasksRevoker.RevokeAllStageTasksAsync(
                new StageTaskRevokerContext(
                    context,
                    context.ValidationResult,
                    context.CancellationToken));

        #endregion
    }
}
