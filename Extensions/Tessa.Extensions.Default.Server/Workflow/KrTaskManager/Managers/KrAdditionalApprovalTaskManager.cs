#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Notices;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Localization;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Scheme;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <inheritdoc cref="IKrAdditionalApprovalTaskManager"/>
    public class KrAdditionalApprovalTaskManager :
        KrSingleTaskManagerBase<IKrAdditionalApprovalTaskManagerDataProvider>,
        IKrAdditionalApprovalTaskManager
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="krHistoryStrategy"><inheritdoc cref="HistoryStrategy" path="/summary"/></param>
        /// <param name="notificationManager"><inheritdoc cref="NotificationManager" path="/summary"/></param>
        public KrAdditionalApprovalTaskManager(
            IKrHistoryStrategy krHistoryStrategy,
            INotificationManager notificationManager)
        {
            this.HistoryStrategy = NotNullOrThrow(krHistoryStrategy);
            this.NotificationManager = NotNullOrThrow(notificationManager);

            this.CompleteTaskActions.Add(
                DefaultCompletionOptions.Approve,
                this.HandleMainCompletionOptionsAsync);

            this.CompleteTaskActions.Add(
                DefaultCompletionOptions.Disapprove,
                this.HandleMainCompletionOptionsAsync);

            this.CompleteTaskActions.Add(
                DefaultCompletionOptions.Revoke,
                this.HandleRevokeCompletionOptionAsync);

            this.CompleteTaskActions.Add(
                DefaultCompletionOptions.AdditionalApproval,
                this.HandleAdditionalApprovalCompletionOptionAsync);

            this.CompleteTaskActions.Add(
                DefaultCompletionOptions.RequestComments,
                this.HandleRequestCommentsCompletionOptionAsync);
        }

        #endregion

        #region Properties

        /// <inheritdoc cref="IKrHistoryStrategy" path="/summary"/>
        protected IKrHistoryStrategy HistoryStrategy { get; }

        /// <inheritdoc cref="INotificationManager" path="/summary"/>
        protected INotificationManager NotificationManager { get; }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override Guid TaskTypeID => DefaultTaskTypes.KrAdditionalApprovalTypeID;

        /// <inheritdoc/>
        public override async ValueTask<Guid?> StartAsync(
            IKrTaskManagerContext context,
            IKrAdditionalApprovalTaskManagerDataProvider dataProvider)
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

            await this.SendTaskAsync(
                context,
                dataProvider);

            return context.ValidationResult.IsSuccessful()
                ? KrTaskManagerCompletionOptions.SendNextTask
                : null;
        }

        /// <inheritdoc/>
        public override async ValueTask<Guid?> DeleteTaskAsync(
            IKrTaskManagerContext context,
            IKrAdditionalApprovalTaskManagerDataProvider dataProvider,
            CardTask task)
        {
            var result = await base.DeleteTaskAsync(
                context,
                dataProvider,
                task);

            if (!result.HasValue)
            {
                return null;
            }

            await context.TryRemoveActiveTaskAsync(
                task.RowID,
                context.ValidationResult,
                context.CancellationToken);

            return context.ValidationResult.IsSuccessful()
                ? result
                : null;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Отправляет задания <see cref="TaskTypeID"/> в соответствии с <paramref name="dataProvider"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrAdditionalApprovalTaskManagerDataProvider" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        protected virtual async ValueTask SendTaskAsync(
            IKrTaskManagerContext context,
            IKrAdditionalApprovalTaskManagerDataProvider dataProvider)
        {
            var performers = await dataProvider.GetPerformersAsync(
                context.ValidationResult,
                context.CancellationToken);

            if (performers.Count == 0
                || !context.ValidationResult.IsSuccessful())
            {
                return;
            }

            foreach (var performer in performers)
            {
                var task = await this.SendTaskAsync(
                    context,
                    dataProvider,
                    performer.ID,
                    performer.Name,
                    performer.IsResponsible);

                if (task is null
                    || !context.ValidationResult.IsSuccessful())
                {
                    return;
                }

                if (dataProvider.CreateTaskActionAsync is not null)
                {
                    await dataProvider.CreateTaskActionAsync(
                        task,
                        performer,
                        context.CancellationToken);
                }
            }
        }

        /// <summary>
        /// Отправляет задание <see cref="TaskTypeID"/> в соответствии с <paramref name="dataProvider"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrAdditionalApprovalTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="roleID">Идентификатор роли, на которую отправляется задание.</param>
        /// <param name="roleName">Название роли, на которую отправляется задание.</param>
        /// <param name="isResponsible">Значение <see langword="true"/>, если <paramref name="roleID"/> является ответственным исполнителем, иначе - <see langword="false"/>.</param>
        /// <returns>Созданное задание или значение <see langword="null"/>, если произошла ошибка.</returns>
        protected virtual async ValueTask<CardTask?> SendTaskAsync(
            IKrTaskManagerContext context,
            IKrAdditionalApprovalTaskManagerDataProvider dataProvider,
            Guid roleID,
            string? roleName,
            bool isResponsible)
        {
            var authorID = await dataProvider.GetAuthorIDAsync(
                context.ValidationResult,
                context.CancellationToken);

            if (!authorID.HasValue)
            {
                return null;
            }

            var planned = await dataProvider.GetPlannedAsync(
                context.ValidationResult,
                context.CancellationToken);

            var plannedQuants = await dataProvider.GetPlannedQuantsAsync(
                context.ValidationResult,
                context.CancellationToken);

            var plannedWorkingDays = await dataProvider.GetPlannedWorkingDaysAsync(
                context.ValidationResult,
                context.CancellationToken);

            var (kindID, kindCaption) = await dataProvider.GetKindAsync(
                context.ValidationResult,
                context.CancellationToken);

            var parentTaskRowID = await dataProvider.GetParentTaskRowIDAsync(
                context.ValidationResult,
                context.CancellationToken);

            var taskHistoryGroupID = await context.GetTaskHistoryGroupIDAsync(
                context.ValidationResult,
                context.CancellationToken);

            var cycle = await context.GetProcessCycleAsync(
                context.ValidationResult,
                context.CancellationToken);

            var comment = await dataProvider.GetDigestAsync(
                context.ValidationResult,
                context.CancellationToken);

            var authorComment = await context.GetAuthorCommentAsync(
                context.ValidationResult,
                context.CancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                return null;
            }

            var digest = string.IsNullOrWhiteSpace(authorComment)
                ? comment
                : LocalizationManager.EscapeIfLocalizationString(comment)
                    + Environment.NewLine
                    + LocalizationManager.EscapeIfLocalizationString(authorComment);

            if (isResponsible)
            {
                digest = string.IsNullOrWhiteSpace(digest)
                    ? "{$KrMessages_ResponsibleAdditionalDigest}"
                    : $"{{$KrMessages_ResponsibleAdditionalDigest}}.{Environment.NewLine}{LocalizationManager.EscapeIfLocalizationString(digest)}";
            }

            var mainCard = await context.GetCardAsync(
                context.MainCardID,
                context.ValidationResult,
                cancellationToken: context.CancellationToken);

            if (mainCard is null)
            {
                return null;
            }

            var taskHistoryItem = await this.HistoryStrategy.CreateTaskHistoryAsync(
                DefaultTaskTypes.KrInfoAdditionalApprovalTypeID,
                DefaultTaskTypes.KrInfoAdditionalApprovalTypeName,
                "$CardTypes_TypesNames_KrAdditionalApproval",
                DefaultCompletionOptions.AdditionalApproval,
                comment ?? (isResponsible
                    ? "{$KrMessages_ResponsibleAdditionalApprovalComment}"
                    : "{$KrMessages_DefaultAdditionalApprovalComment}"),
                context.ValidationResult,
                groupRowID: taskHistoryGroupID,
                storeDateTime: context.StoreDateTime,
                cancellationToken: context.CancellationToken);

            if (taskHistoryItem is null)
            {
                return null;
            }

            taskHistoryItem.ParentRowID = parentTaskRowID;

            mainCard.TaskHistory.Add(taskHistoryItem);

            var task = await context.SendTaskAsync(
                this.TaskTypeID,
                digest,
                planned,
                plannedQuants,
                plannedWorkingDays,
                roleID,
                roleName,
                context.ValidationResult,
                cancellationToken: context.CancellationToken);

            if (task is null
                || !context.ValidationResult.IsSuccessful())
            {
                return null;
            }

            context.ValidationResult.Add(
                WorkflowCommonHelper.SetTaskKind(
                    task,
                    kindID,
                    kindCaption,
                    this));

            if (!context.ValidationResult.IsSuccessful())
            {
                return null;
            }

            await context.AddActiveTaskAsync(
                task.RowID,
                context.ValidationResult,
                context.CancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                return null;
            }

            await context.AddToHistoryAsync(
                task.RowID,
                cycle,
                context.ValidationResult,
                cancellationToken: context.CancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                return null;
            }

            task.Flags |= CardTaskFlags.CreateHistoryItem;
            task.ParentRowID = parentTaskRowID;
            task.HistoryItemParentRowID = taskHistoryItem.RowID;
            task.AddAuthor(authorID.Value);

            var taskSections = task.Card.Sections;
            taskSections[KrConstants.KrAdditionalApproval.Name].Fields[KrConstants.KrAdditionalApproval.AuthorComment] = comment;
            taskSections[KrConstants.KrAdditionalApprovalTaskInfo.Name].Fields[KrConstants.KrAdditionalApprovalTaskInfo.IsResponsible] = BooleanBoxes.Box(isResponsible);

            context.AddTaskToNext(task);

            return task;
        }

        /// <summary>
        /// Обрабатывает варианты завершения <see cref="DefaultCompletionOptions.Approve"/> и <see cref="DefaultCompletionOptions.Disapprove"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrAdditionalApprovalTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="task">Завершаемое задание.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если вариант завершения задания не обработан.</returns>
        protected virtual async ValueTask<Guid?> HandleMainCompletionOptionsAsync(
            IKrTaskManagerContext context,
            IKrAdditionalApprovalTaskManagerDataProvider dataProvider,
            CardTask task)
        {
            await context.TryRemoveActiveTaskAsync(
                task.RowID,
                context.ValidationResult,
                context.CancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                return null;
            }

            var comment = task
                .TryGetCard()
                ?.TryGetSections()
                ?.TryGet(KrConstants.KrAdditionalApprovalTaskInfo.Name)
                ?.TryGetRawFields()
                ?.TryGet<string>(KrConstants.KrAdditionalApprovalTaskInfo.Comment);

            if (!string.IsNullOrEmpty(comment))
            {
                task.Result = comment;
            }

            await context.UpdateTaskHistoryResultAsync(
                task,
                context.CancellationToken);

            var parentTaskRowID = task.ParentRowID;
            if (!parentTaskRowID.HasValue)
            {
                context.ValidationResult.AddError(
                    this,
                    "$KrStages_Approval_AdditionalApprovalTaskParentRowIDNotSpecified",
                    task.RowID);

                return null;
            }

            Guid? approverID;
            Guid? responsibleID;
            int notCompleted;

            await using (context.DbScope.Create())
            {
                approverID = await context.DbScope.Db
                    .SetCommand(
                        context.DbScope.BuilderFactory
                            .Select().C("UserID")
                            .From(Names.Tasks).NoLock()
                            .Where().C(Names.Table_RowID).Equals().P("RowID")
                            .Build(),
                        context.DbScope.Db.Parameter("RowID", parentTaskRowID.Value))
                    .LogCommand()
                    .ExecuteAsync<Guid?>(context.CancellationToken);

                var isResponsible = task
                    .Card
                    .Sections[KrConstants.KrAdditionalApprovalTaskInfo.Name]
                    .TryGetRawFields()
                    ?.TryGet<bool>(KrConstants.KrAdditionalApprovalTaskInfo.IsResponsible) ?? false;

                responsibleID = isResponsible
                    ? await context.DbScope.Db
                        .SetCommand(
                            context.DbScope.BuilderFactory
                                .Select().C("t", "UserID")
                                .From(Names.Tasks, "t").NoLock()
                                .InnerJoin(KrConstants.KrAdditionalApprovalInfo.Name, "i").NoLock()
                                // ReSharper disable AccessToStaticMemberViaDerivedType
                                .On().C("i", KrConstants.KrAdditionalApprovalInfo.RowID).Equals().C("t", Names.Table_RowID)
                                .Where().C("i", KrConstants.KrAdditionalApprovalInfo.ID).Equals().P(Names.Table_RowID)
                                // ReSharper restore AccessToStaticMemberViaDerivedType
                                .And().C("i", KrConstants.KrAdditionalApprovalInfo.IsResponsible).Equals().V(true)
                                .Build(),
                            context.DbScope.Db.Parameter(Names.Table_RowID, parentTaskRowID.Value))
                        .LogCommand()
                        .ExecuteAsync<Guid?>(context.CancellationToken)
                    : null;

                notCompleted = await context.DbScope.Db
                    .SetCommand(
                        context.DbScope.BuilderFactory
                            .Select().Count().Substract(1)
                            .From(KrConstants.KrAdditionalApprovalInfo.Name).NoLock()
                            // ReSharper disable AccessToStaticMemberViaDerivedType
                            .Where().C(KrConstants.KrAdditionalApprovalInfo.ID).Equals().P("RowID")
                            .And().C(KrConstants.KrAdditionalApprovalInfo.Completed).IsNull()
                            // ReSharper restore AccessToStaticMemberViaDerivedType
                            .Build(),
                        context.DbScope.Db.Parameter("RowID", parentTaskRowID.Value))
                    .LogCommand()
                    .ExecuteAsync<int>(context.CancellationToken);
            }

            var roleList = new List<Guid>();
            if (approverID.HasValue)
            {
                roleList.Add(approverID.Value);
            }

            if (responsibleID.HasValue)
            {
                roleList.Add(responsibleID.Value);
            }

            var isNegativeResult = task.OptionID == DefaultCompletionOptions.Disapprove;

            if (roleList.Count > 0)
            {
                var isCompleted = notCompleted == 0;

                context.ValidationResult.Add(
                    await this.NotificationManager
                        .SendAsync(
                            isCompleted
                                ? DefaultNotifications.AdditionalApprovalNotificationCompleted
                                : DefaultNotifications.AdditionalApprovalNotification,
                            roleList,
                            new NotificationSendContext
                            {
                                MainCardID = context.MainCardID,
                                TaskTypeID = task.TypeID,
                                Info = DefaultNotificationHelper.GetInfoWithTask(task),
                                GetCardFuncAsync = (validationResult, ct) =>
                                    context.GetCardAsync(
                                        context.MainCardID,
                                        validationResult,
                                        cancellationToken: ct),
                                ModifyEmailActionAsync = async (email, _) =>
                                {
                                    if (!isCompleted)
                                    {
                                        email.PlaceholderAliases.SetReplacement(
                                            "subjectLabel",
                                            isNegativeResult
                                                ? "$DisapprovedAdditionalApprovalNotificationTemplate_SubjectLabel"
                                                : "$ApprovedAdditionalApprovalNotificationTemplate_SubjectLabel");

                                        email.PlaceholderAliases.SetReplacement(
                                            "taskCount",
                                            $"text:{notCompleted}");
                                    }

                                    email.PlaceholderAliases.SetReplacement(
                                        "resultLabel",
                                        isNegativeResult
                                            ? "$DisapprovedNotificationTemplate_BodyLabel"
                                            : "$ApprovedNotificationTemplate_BodyLabel");
                                }
                            },
                            context.CancellationToken));
            }

            return isNegativeResult
                ? KrTaskManagerCompletionOptions.NegativeResult
                : KrTaskManagerCompletionOptions.PositiveResult;
        }

        /// <summary>
        /// Обрабатывает вариант завершения <see cref="DefaultCompletionOptions.Revoke"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrAdditionalApprovalTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="task">Завершаемое задание.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если вариант завершения задания не обработан.</returns>
        protected virtual async ValueTask<Guid?> HandleRevokeCompletionOptionAsync(
            IKrTaskManagerContext context,
            IKrAdditionalApprovalTaskManagerDataProvider dataProvider,
            CardTask task)
        {
            await context.TryRemoveActiveTaskAsync(
                task.RowID,
                context.ValidationResult,
                context.CancellationToken);

            return KrTaskManagerCompletionOptions.Cancel;
        }

        /// <summary>
        /// Обрабатывает вариант завершения <see cref="DefaultCompletionOptions.AdditionalApproval"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrAdditionalApprovalTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="task">Обрабатываемое задание.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если вариант завершения не обработан.</returns>
        protected virtual ValueTask<Guid?> HandleAdditionalApprovalCompletionOptionAsync(
            IKrTaskManagerContext context,
            IKrAdditionalApprovalTaskManagerDataProvider dataProvider,
            CardTask task) =>
            ValueTask.FromResult<Guid?>(KrTaskManagerCompletionOptions.RequestAdditionalApproval);

        /// <summary>
        /// Обрабатывает вариант завершения <see cref="DefaultCompletionOptions.RequestComments"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrAdditionalApprovalTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="task">Обрабатываемое задание.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если вариант завершения не обработан.</returns>
        protected virtual ValueTask<Guid?> HandleRequestCommentsCompletionOptionAsync(
            IKrTaskManagerContext context,
            IKrAdditionalApprovalTaskManagerDataProvider dataProvider,
            CardTask task) =>
            ValueTask.FromResult<Guid?>(KrTaskManagerCompletionOptions.RequestComment);

        #endregion
    }
}
