#nullable enable

using System;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <inheritdoc cref="IKrRequestCommentTaskManager"/>
    public class KrRequestCommentTaskManager :
        KrSingleTaskManagerBase<IKrRequestCommentTaskManagerDataProvider>,
        IKrRequestCommentTaskManager
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="historyStrategy"><inheritdoc cref="HistoryStrategy" path="/summary"/></param>
        public KrRequestCommentTaskManager(
            IKrHistoryStrategy historyStrategy)
        {
            this.HistoryStrategy = NotNullOrThrow(historyStrategy);

            this.CompleteTaskActions.Add(
                DefaultCompletionOptions.AddComment,
                this.HandleAddCommentCompletionOptionsAsync);

            this.CompleteTaskActions.Add(
                DefaultCompletionOptions.Cancel,
                this.HandleCancelCompletionOptionsAsync);
        }

        #endregion

        #region Properties

        /// <inheritdoc cref="IKrHistoryStrategy" path="/summary"/>
        protected IKrHistoryStrategy HistoryStrategy { get; }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override Guid TaskTypeID => DefaultTaskTypes.KrRequestCommentTypeID;

        /// <inheritdoc/>
        public override async ValueTask<Guid?> StartAsync(
            IKrTaskManagerContext context,
            IKrRequestCommentTaskManagerDataProvider dataProvider)
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
            IKrRequestCommentTaskManagerDataProvider dataProvider,
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

            if (!context.ValidationResult.IsSuccessful())
            {
                return null;
            }

            return result;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Отправляет задание <see cref="TaskTypeID"/> в соответствии с <paramref name="dataProvider"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrRequestCommentTaskManagerDataProvider" path="/summary"/></param>
        /// <returns>Созданное задание или значение <see langword="null"/>, если произошла ошибка.</returns>
        protected virtual async ValueTask<CardTask?> SendTaskAsync(
            IKrTaskManagerContext context,
            IKrRequestCommentTaskManagerDataProvider dataProvider)
        {
            var performers = await dataProvider.GetPerformersAsync(
                context.ValidationResult,
                context.CancellationToken);

            if (performers.Count == 0)
            {
                context.ValidationResult.AddError(
                    this,
                    "$KrMessages_NeedToSpecifyRespondent");
                return null;
            }

            var authorID = await dataProvider.GetAuthorIDAsync(
                context.ValidationResult,
                context.CancellationToken);

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

            var digest = await dataProvider.GetDigestAsync(
                context.ValidationResult,
                context.CancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                return null;
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
                DefaultTaskTypes.KrInfoRequestCommentTypeID,
                DefaultTaskTypes.KrInfoRequestCommentTypeName,
                "$CardTypes_TypesNames_KrInfoRequestComment",
                DefaultCompletionOptions.RequestComments,
                digest,
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
                null,
                null,
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

            for (var i = 0; i < performers.Count; i++)
            {
                var performer = performers[i];
                task.AddPerformer(
                    performer.ID,
                    performer.Name,
                    i == 0);
            }

            task.Flags |= CardTaskFlags.CreateHistoryItem;
            task.ParentRowID = taskHistoryItem.ParentRowID;
            task.HistoryItemParentRowID = taskHistoryItem.RowID;

            if (authorID.HasValue)
            {
                task.AddAuthor(authorID.Value);
            }

            var sections = task.Card.Sections;
            sections[KrConstants.KrRequestComment.Name].Fields[KrConstants.KrRequestComment.AuthorComment] = task.Digest;

            context.AddTaskToNext(task);

            if (dataProvider.CreateTaskActionAsync is not null)
            {
                await dataProvider.CreateTaskActionAsync(
                    task,
                    null,
                    context.CancellationToken);
            }

            return task;
        }

        /// <summary>
        /// Обрабатывает вариант завершения <see cref="DefaultCompletionOptions.AddComment"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrRequestCommentTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="task">Завершаемое задание.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если вариант завершения задания не обработан.</returns>
        protected virtual async ValueTask<Guid?> HandleAddCommentCompletionOptionsAsync(
            IKrTaskManagerContext context,
            IKrRequestCommentTaskManagerDataProvider dataProvider,
            CardTask task)
        {
            task.Result = task
                .TryGetCard()
                ?.TryGetSections()
                ?.TryGet(KrConstants.KrRequestComment.Name)
                ?.TryGetRawFields()
                ?.TryGet<string>(KrConstants.KrRequestComment.Comment);

            await context.UpdateTaskHistoryResultAsync(
                task,
                context.CancellationToken);

            await context.TryRemoveActiveTaskAsync(
                task.RowID,
                context.ValidationResult,
                context.CancellationToken);

            return KrTaskManagerCompletionOptions.AddComment;
        }

        /// <summary>
        /// Обрабатывает вариант завершения <see cref="DefaultCompletionOptions.Cancel"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrRequestCommentTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="task">Завершаемое задание.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если вариант завершения задания не обработан.</returns>
        protected virtual async ValueTask<Guid?> HandleCancelCompletionOptionsAsync(
            IKrTaskManagerContext context,
            IKrRequestCommentTaskManagerDataProvider dataProvider,
            CardTask task)
        {
            await context.TryRemoveActiveTaskAsync(
                task.RowID,
                context.ValidationResult,
                context.CancellationToken);

            return KrTaskManagerCompletionOptions.Cancel;
        }

        #endregion
    }
}
