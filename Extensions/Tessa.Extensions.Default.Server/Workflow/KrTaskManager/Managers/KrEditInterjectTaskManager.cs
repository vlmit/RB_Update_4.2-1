#nullable enable

using System;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Roles;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <inheritdoc cref="IKrEditInterjectTaskManager"/>
    public class KrEditInterjectTaskManager :
        KrSingleTaskManagerBase<IKrEditInterjectTaskManagerDataProvider>,
        IKrEditInterjectTaskManager
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        public KrEditInterjectTaskManager() =>
            this.CompleteTaskActions.Add(
                DefaultCompletionOptions.Continue,
                this.HandleContinueCompletionOptionsAsync);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override Guid TaskTypeID => DefaultTaskTypes.KrEditInterjectTypeID;

        /// <inheritdoc/>
        public override async ValueTask<Guid?> StartAsync(
            IKrTaskManagerContext context,
            IKrEditInterjectTaskManagerDataProvider dataProvider)
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

        #endregion

        #region Protected Methods

        /// <summary>
        /// Отправляет задания <see cref="TaskTypeID"/> в соответствии с <paramref name="dataProvider"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrEditInterjectTaskManagerDataProvider" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        protected virtual async ValueTask SendTaskAsync(
            IKrTaskManagerContext context,
            IKrEditInterjectTaskManagerDataProvider dataProvider)
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
                    performer);

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
        /// <param name="dataProvider"><inheritdoc cref="IKrEditInterjectTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="performer"><inheritdoc cref="IRoleUser" path="/summary"/></param>
        /// <returns>Созданное задание или значение <see langword="null"/>, если произошла ошибка.</returns>
        protected virtual async ValueTask<CardTask?> SendTaskAsync(
            IKrTaskManagerContext context,
            IKrEditInterjectTaskManagerDataProvider dataProvider,
            IRoleUser performer)
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

            var taskHistoryGroupID = await context.GetTaskHistoryGroupIDAsync(
                context.ValidationResult,
                context.CancellationToken);

            var cycle = await context.GetProcessCycleAsync(
                context.ValidationResult,
                context.CancellationToken);

            var digest = await dataProvider.GetDigestAsync(
                context.ValidationResult,
                context.CancellationToken);

            digest = await context.GetWithPlaceholdersAsync(
                digest,
                null,
                context.ValidationResult,
                context.CancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                return null;
            }

            var task = await context.SendTaskAsync(
                this.TaskTypeID,
                digest,
                planned,
                plannedQuants,
                plannedWorkingDays,
                performer.ID,
                performer.Name,
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
            task.GroupRowID = taskHistoryGroupID;
            task.AddAuthor(authorID.Value);

            context.AddTaskToNext(task);

            return task;
        }

        /// <summary>
        /// Обрабатывает вариант завершения <see cref="DefaultCompletionOptions.Continue"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrEditInterjectTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="task">Обрабатываемое задание.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если вариант завершения не обработан.</returns>
        protected virtual async ValueTask<Guid?> HandleContinueCompletionOptionsAsync(
            IKrTaskManagerContext context,
            IKrEditInterjectTaskManagerDataProvider dataProvider,
            CardTask task)
        {
            var comment = task
                .TryGetCard()
                ?.TryGetSections()
                ?.TryGet(KrConstants.KrTask.Name)
                ?.TryGetRawFields()
                ?.TryGet<string>(KrConstants.KrTask.Comment)
                ?.Trim();

            if (!string.IsNullOrEmpty(comment))
            {
                task.Result = comment;
            }

            await context.UpdateTaskHistoryResultAsync(
                task,
                context.CancellationToken);

            return KrTaskManagerCompletionOptions.Complete;
        }

        #endregion
    }
}
