#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <inheritdoc cref="IKrApprovalCoreTaskManager"/>
    public class KrApprovalCoreTaskManager :
        KrSingleTaskManagerBase<IKrApprovalCoreTaskManagerDataProvider>,
        IKrApprovalCoreTaskManager
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="krHistoryStrategy"><inheritdoc cref="HistoryStrategy" path="/summary"/></param>
        /// <param name="cardCache"><inheritdoc cref="CardCache" path="/summary"/></param>
        public KrApprovalCoreTaskManager(
            IKrHistoryStrategy krHistoryStrategy,
            ICardCache cardCache)
        {
            this.HistoryStrategy = NotNullOrThrow(krHistoryStrategy);
            this.CardCache = NotNullOrThrow(cardCache);

            this.CompleteTaskActions.Add(
                DefaultCompletionOptions.Approve,
                this.HandleMainCompletionOptionsAsync);

            this.CompleteTaskActions.Add(
                DefaultCompletionOptions.Disapprove,
                this.HandleMainCompletionOptionsAsync);

            this.CompleteTaskActions.Add(
                DefaultCompletionOptions.AdditionalApproval,
                this.HandleAdditionalApprovalCompletionOptionAsync);

            this.CompleteTaskActions.Add(
                DefaultCompletionOptions.Delegate,
                this.HandleDelegateCompletionOptionAsync);

            this.CompleteTaskActions.Add(
                DefaultCompletionOptions.RequestComments,
                this.HandleRequestCommentsCompletionOptionAsync);

            this.CompleteTaskActions.Add(
                DefaultCompletionOptions.Revoke,
                this.HandleRevokeCompletionOptionsAsync);

        }

        #endregion

        #region Properties

        /// <inheritdoc cref="IKrHistoryStrategy" path="/summary"/>
        protected IKrHistoryStrategy HistoryStrategy { get; }

        /// <inheritdoc cref="ICardCache" path="/summary"/>
        protected ICardCache CardCache { get; }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override Guid TaskTypeID => DefaultTaskTypes.KrApproveTypeID;

        /// <inheritdoc/>
        public override async ValueTask<Guid?> StartAsync(
            IKrTaskManagerContext context,
            IKrApprovalCoreTaskManagerDataProvider dataProvider)
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

            await dataProvider.ResetStoredPerformersAsync(
                context.ValidationResult,
                context.CancellationToken);

            dataProvider.CurrentPerformerIndex = 0;
            dataProvider.IsNegativeActionResult = false;

            var performers = await dataProvider.GetPerformersAsync(
                context.ValidationResult,
                context.CancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                return null;
            }

            if (performers.Count == 0)
            {
                if (await dataProvider.GetChangeStateOnEndAsync(
                    context.ValidationResult,
                    context.CancellationToken))
                {
                    await context.SetStateAsync(
                        KrState.Approved,
                        context.ValidationResult,
                        context.CancellationToken);
                }

                return KrTaskManagerCompletionOptions.Complete;
            }

            if (await dataProvider.GetChangeStateOnStartAsync(
                    context.ValidationResult,
                    context.CancellationToken))
            {
                await context.SetStateAsync(
                    KrState.Active,
                    context.ValidationResult,
                    context.CancellationToken);
            }

            var isParallel = await dataProvider.GetIsParallelAsync(
                context.ValidationResult,
                context.CancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                return null;
            }

            if (isParallel)
            {
                await this.SendTaskAsync(
                    context,
                    dataProvider,
                    performers);
            }
            else
            {
                await this.SendTaskAsync(
                    context,
                    dataProvider,
                    performers.Take(1));
            }

            return context.ValidationResult.IsSuccessful()
                ? KrTaskManagerCompletionOptions.SendNextTask
                : null;
        }

        /// <inheritdoc/>
        public override async ValueTask<Guid?> DeleteTaskAsync(
            IKrTaskManagerContext context,
            IKrApprovalCoreTaskManagerDataProvider dataProvider,
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

            var currentPerformerIndex = ++dataProvider.CurrentPerformerIndex;

            // Завершено последнее задание?
            var roleList = await dataProvider.GetPerformersAsync(
                context.ValidationResult,
                context.CancellationToken);
            if (currentPerformerIndex >= roleList.Count)
            {
                return dataProvider.IsNegativeActionResult
                    && !await dataProvider.GetIsAdvisoryAsync(
                        context.ValidationResult,
                        context.CancellationToken)
                    ? await this.NegativeResultProcessingAsync(
                        context,
                        dataProvider,
                        false)
                    : await this.PositiveResultProcessingAsync(
                        context,
                        dataProvider,
                        false);
            }

            if (!await dataProvider.GetIsParallelAsync(
                    context.ValidationResult,
                    context.CancellationToken))
            {
                await this.SendTaskAsync(
                    context,
                    dataProvider,
                    roleList
                        .Skip(currentPerformerIndex)
                        .Take(1));

                return KrTaskManagerCompletionOptions.SendNextTask;
            }

            return KrTaskManagerCompletionOptions.Delete;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Отправляет задания <see cref="TaskTypeID"/> в соответствии с <paramref name="dataProvider"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrApprovalCoreTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="performers">Коллекция ролей, на которые должны быть отправлены задания.</param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        protected virtual async ValueTask SendTaskAsync(
            IKrTaskManagerContext context,
            IKrApprovalCoreTaskManagerDataProvider dataProvider,
            IEnumerable<RoleEntryStorage> performers)
        {
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
            }
        }

        /// <summary>
        /// Отправляет задание <see cref="TaskTypeID"/> в соответствии с <paramref name="dataProvider"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrApprovalCoreTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="performer"><inheritdoc cref="RoleEntryStorage" path="/summary"/></param>
        /// <returns>Созданное задание или значение <see langword="null"/>, если произошла ошибка.</returns>
        protected virtual async ValueTask<CardTask?> SendTaskAsync(
            IKrTaskManagerContext context,
            IKrApprovalCoreTaskManagerDataProvider dataProvider,
            RoleEntryStorage performer)
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

            var canEditCard = await dataProvider.GetCanEditCardAsync(
                context.ValidationResult,
                context.CancellationToken);

            var canEditAnyFiles = await dataProvider.GetCanEditAnyFilesAsync(
                context.ValidationResult,
                context.CancellationToken);

            var isDisableAutoApproval = await dataProvider.GetIsDisableAutoApprovalAsync(
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

            digest = await context.CreateDigestAsync(
                digest,
                null,
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
            task.ParentRowID = parentTaskRowID;
            task.GroupRowID = taskHistoryGroupID;
            task.AddAuthor(authorID.Value);

            if (canEditCard)
            {
                task.Settings ??= [];
                task.Settings[WorkflowCommonConstants.CanEditCard] = BooleanBoxes.True;
            }

            if (canEditAnyFiles)
            {
                task.Settings ??= [];
                task.Settings[WorkflowCommonConstants.CanEditAnyFiles] = BooleanBoxes.True;
            }

            if (isDisableAutoApproval)
            {
                task.Settings ??= [];
                task.Settings[WorkflowCommonConstants.IsDisableAutoApproval] = BooleanBoxes.True;
            }

            context.AddTaskToNext(task);

            if (dataProvider.CreateTaskActionAsync is not null)
            {
                await dataProvider.CreateTaskActionAsync(
                    task,
                    performer,
                    context.CancellationToken);
            }

            return task;
        }

        /// <summary>
        /// Обрабатывает варианты завершения <see cref="DefaultCompletionOptions.Approve"/> и <see cref="DefaultCompletionOptions.Disapprove"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrApprovalCoreTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="task">Завершаемое задание.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если вариант завершения задания не обработан.</returns>
        protected virtual async ValueTask<Guid?> HandleMainCompletionOptionsAsync(
            IKrTaskManagerContext context,
            IKrApprovalCoreTaskManagerDataProvider dataProvider,
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

            var sCard = await context.GetCardSatelliteAsync(
                context.MainCardID,
                DefaultCardTypes.KrSatelliteTypeID,
                context.ValidationResult,
                cancellationToken: context.CancellationToken);

            if (sCard is null)
            {
                return null;
            }

            var isNegativeResult = task.OptionID == DefaultCompletionOptions.Disapprove;

            await WorkflowCommonHelper.AppendApprovalInfoUserCompleteTaskAsync(
                sCard.Sections,
                context.DbScope,
                context.Session.User,
                task,
                isNegativeResult,
                context.CancellationToken);

            if (isNegativeResult)
            {
                dataProvider.IsNegativeActionResult = true;
            }

            var isSetCommentTaskResult =
                isNegativeResult
                || !(await this.CardCache.Cards.GetAsync(
                        KrConstants.KrSettings.Name,
                        context.CancellationToken))
                    .GetValue()
                    .Sections[KrConstants.KrSettings.Name]
                    .RawFields
                    .Get<bool>(KrConstants.KrSettings.HideCommentForApprove);

            var comment =
                isSetCommentTaskResult
                    ? task
                        .TryGetCard()
                        ?.TryGetSections()
                        ?.TryGet(KrConstants.KrTask.Name)
                        ?.TryGetRawFields()
                        ?.TryGet<string>(KrConstants.KrTask.Comment)
                        ?.Trim()
                    : null;

            var isAdvisory = await dataProvider.GetIsAdvisoryAsync(
                context.ValidationResult,
                context.CancellationToken);

            if (isAdvisory)
            {
                comment = await LocalizeFormatAsync(
                    "$KrProcess_AdvisoryApprovalCommentFormat",
                    comment?.Trim());
            }

            if (!string.IsNullOrEmpty(comment))
            {
                task.Result = comment;
            }

            await context.UpdateTaskHistoryResultAsync(
                task,
                context.CancellationToken);

            var currentPerformerIndex = ++dataProvider.CurrentPerformerIndex;

            // Завершено последнее задание?
            var roleList = await dataProvider.GetPerformersAsync(
                context.ValidationResult,
                context.CancellationToken);
            if (currentPerformerIndex >= roleList.Count)
            {
                return !isAdvisory
                    && dataProvider.IsNegativeActionResult
                    ? await this.NegativeResultProcessingAsync(
                        context,
                        dataProvider,
                        true)
                    : await this.PositiveResultProcessingAsync(
                        context,
                        dataProvider,
                        true);
            }

            if (!await dataProvider.GetIsParallelAsync(
                    context.ValidationResult,
                    context.CancellationToken))
            {
                if (!isAdvisory
                    && isNegativeResult
                    && !await dataProvider.GetExpectAllPerformersAsync(
                            context.ValidationResult,
                            context.CancellationToken))
                {
                    return await this.NegativeResultProcessingAsync(
                        context,
                        dataProvider,
                        true);
                }

                await this.SendTaskAsync(
                    context,
                    dataProvider,
                    roleList
                        .Skip(currentPerformerIndex)
                        .Take(1));

                return KrTaskManagerCompletionOptions.SendNextTask;
            }

            return isNegativeResult
                ? KrTaskManagerCompletionOptions.IntermediateNegativeResult
                : KrTaskManagerCompletionOptions.IntermediatePositiveResult;
        }

        /// <summary>
        /// Обрабатывает положительный вариант завершения задания.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrApprovalCoreTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="isCompleteTask">Значение <see langword="true"/>, если выполняется обработка при завершении задания, иначе - <see langword="false"/>.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если вариант завершения не обработан.</returns>
        protected virtual async ValueTask<Guid?> PositiveResultProcessingAsync(
            IKrTaskManagerContext context,
            IKrApprovalCoreTaskManagerDataProvider dataProvider,
            bool isCompleteTask)
        {
            if (await dataProvider.GetReturnWhenPositiveActionResultAsync(
                    context.ValidationResult,
                    context.CancellationToken))
            {
                return KrTaskManagerCompletionOptions.EditAfterPositiveResult;
            }

            return KrTaskManagerCompletionOptions.PositiveResult;
        }

        /// <summary>
        /// Обрабатывает вариант завершения <see cref="DefaultCompletionOptions.RequestComments"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrApprovalCoreTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="task">Обрабатываемое задание.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если вариант завершения не обработан.</returns>
        protected virtual ValueTask<Guid?> HandleRequestCommentsCompletionOptionAsync(
            IKrTaskManagerContext context,
            IKrApprovalCoreTaskManagerDataProvider dataProvider,
            CardTask task) =>
            ValueTask.FromResult<Guid?>(KrTaskManagerCompletionOptions.RequestComment);

        /// <summary>
        /// Обрабатывает вариант завершения <see cref="DefaultCompletionOptions.Revoke"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrApprovalCoreTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="task">Завершаемое задание.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если вариант завершения задания не обработан.</returns>
        protected virtual async ValueTask<Guid?> HandleRevokeCompletionOptionsAsync(
            IKrTaskManagerContext context,
            IKrApprovalCoreTaskManagerDataProvider dataProvider,
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

            var currentPerformerIndex = ++dataProvider.CurrentPerformerIndex;

            // Завершено последнее задание?
            var roleList = await dataProvider.GetPerformersAsync(
                context.ValidationResult,
                context.CancellationToken);
            if (currentPerformerIndex >= roleList.Count)
            {
                return dataProvider.IsNegativeActionResult
                    && !await dataProvider.GetIsAdvisoryAsync(
                        context.ValidationResult,
                        context.CancellationToken)
                    ? await this.NegativeResultProcessingAsync(
                        context,
                        dataProvider,
                        true)
                    : await this.PositiveResultProcessingAsync(
                        context,
                        dataProvider,
                        true);
            }

            if (!await dataProvider.GetIsParallelAsync(
                    context.ValidationResult,
                    context.CancellationToken))
            {
                await this.SendTaskAsync(
                    context,
                    dataProvider,
                    roleList
                        .Skip(currentPerformerIndex)
                        .Take(1));

                return KrTaskManagerCompletionOptions.SendNextTask;
            }

            return KrTaskManagerCompletionOptions.RevokeResult;
        }

        /// <summary>
        /// Обрабатывает отрицательный вариант завершения.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrApprovalCoreTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="isCompleteTask">Значение <see langword="true"/>, если выполняется обработка при завершении задания, иначе - <see langword="false"/>.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если вариант завершения не обработан.</returns>
        protected virtual async ValueTask<Guid?> NegativeResultProcessingAsync(
            IKrTaskManagerContext context,
            IKrApprovalCoreTaskManagerDataProvider dataProvider,
            bool isCompleteTask)
        {
            if (isCompleteTask
                && !await dataProvider.GetNotReturnEditAsync(
                        context.ValidationResult,
                        context.CancellationToken)
                && !await dataProvider.GetNotCreateReturnEditTaskHistoryRecordAsync(
                        context.ValidationResult,
                        context.CancellationToken))
            {
                var taskHistoryItem = await this.HistoryStrategy.CreateTaskHistoryAsync(
                    DefaultTaskTypes.KrRebuildTypeID,
                    DefaultTaskTypes.KrRebuildTypeName,
                    "$CardTypes_TypesNames_KrRebuild",
                    DefaultCompletionOptions.RebuildDocument,
                    null,
                    context.ValidationResult,
                    groupRowID: await context.GetTaskHistoryGroupIDAsync(
                        context.ValidationResult,
                        context.CancellationToken),
                    storeDateTime: context.StoreDateTime,
                    cancellationToken: context.CancellationToken);

                if (taskHistoryItem is null)
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

                mainCard.TaskHistory.Add(taskHistoryItem);

                await context.AddToHistoryAsync(
                    taskHistoryItem.RowID,
                    await context.GetProcessCycleAsync(
                        context.ValidationResult,
                        context.CancellationToken),
                    context.ValidationResult,
                    cancellationToken: context.CancellationToken);

                if (!context.ValidationResult.IsSuccessful())
                {
                    return null;
                }
            }

            return KrTaskManagerCompletionOptions.NegativeResult;
        }

        /// <summary>
        /// Обрабатывает вариант завершения <see cref="DefaultCompletionOptions.AdditionalApproval"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrApprovalCoreTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="task">Обрабатываемое задание.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если вариант завершения не обработан.</returns>
        protected virtual ValueTask<Guid?> HandleAdditionalApprovalCompletionOptionAsync(
            IKrTaskManagerContext context,
            IKrApprovalCoreTaskManagerDataProvider dataProvider,
            CardTask task) =>
            ValueTask.FromResult<Guid?>(KrTaskManagerCompletionOptions.RequestAdditionalApproval);

        /// <summary>
        /// Обрабатывает вариант завершения <see cref="DefaultCompletionOptions.Delegate"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrTaskManagerContext" path="/summary"/></param>
        /// <param name="dataProvider"><inheritdoc cref="IKrApprovalCoreTaskManagerDataProvider" path="/summary"/></param>
        /// <param name="task">Обрабатываемое задание.</param>
        /// <returns>Идентификатор варианта завершения действия или значение <see langword="null"/>, если вариант завершения не обработан.</returns>
        protected virtual async ValueTask<Guid?> HandleDelegateCompletionOptionAsync(
            IKrTaskManagerContext context,
            IKrApprovalCoreTaskManagerDataProvider dataProvider,
            CardTask task)
        {
            var digest = await dataProvider.GetDigestAsync(
                context.ValidationResult,
                context.CancellationToken);
            if (!context.ValidationResult.IsSuccessful())
            {
                return null;
            }

            var (kindID, kindCaption) = await dataProvider.GetKindAsync(
                context.ValidationResult,
                context.CancellationToken);
            if (!context.ValidationResult.IsSuccessful())
            {
                return null;
            }

            var delegatedTask = await context.DelegateTaskAsync(
                task,
                digest,
                context.ValidationResult,
                kindID,
                kindCaption,
                context.CancellationToken);

            if (delegatedTask is null)
            {
                return null;
            }

            await context.AddActiveTaskAsync(
                delegatedTask.RowID,
                context.ValidationResult,
                context.CancellationToken);
            if (!context.ValidationResult.IsSuccessful())
            {
                return null;
            }

            if (task.Card.Sections.TryGetValue(KrConstants.KrAdditionalApprovalInfo.Name, out var oldSection))
            {
                var newSection = new CardSection(KrConstants.KrAdditionalApprovalInfo.Name, oldSection.GetStorage())
                {
                    Type = CardSectionType.Table
                };

                foreach (var row in newSection.Rows)
                {
                    // ReSharper disable AccessToStaticMemberViaDerivedType
                    row.Fields[KrConstants.KrAdditionalApprovalInfo.ID] = delegatedTask.RowID;
                    // ReSharper restore AccessToStaticMemberViaDerivedType
                    row.State = CardRowState.Inserted;
                }

                delegatedTask.Card.Sections[KrConstants.KrAdditionalApprovalInfo.Name].Set(newSection);
            }

            if (task.Settings is { } originalTaskSettings)
            {
                if (originalTaskSettings.TryGet<bool>(WorkflowCommonConstants.CanEditCard))
                {
                    delegatedTask.Settings ??= [];
                    delegatedTask.Settings[WorkflowCommonConstants.CanEditCard] = BooleanBoxes.True;
                }

                if (originalTaskSettings.TryGet<bool>(WorkflowCommonConstants.CanEditAnyFiles))
                {
                    delegatedTask.Settings ??= [];
                    delegatedTask.Settings[WorkflowCommonConstants.CanEditAnyFiles] = BooleanBoxes.True;
                }

                if (originalTaskSettings.TryGet<bool>(WorkflowCommonConstants.IsDisableAutoApproval))
                {
                    delegatedTask.Settings ??= [];
                    delegatedTask.Settings[WorkflowCommonConstants.IsDisableAutoApproval] = BooleanBoxes.True;
                }
            }

            task.Info.Add(
                KrReassignAdditionalApprovalStoreExtension.ReassignTo,
                delegatedTask.RowID);

            await context.AddToHistoryAsync(
                delegatedTask.RowID,
                await context.GetProcessCycleAsync(
                    context.ValidationResult,
                    context.CancellationToken),
                context.ValidationResult,
                cancellationToken: context.CancellationToken);
            if (!context.ValidationResult.IsSuccessful())
            {
                return null;
            }

            delegatedTask.Flags |= CardTaskFlags.CreateHistoryItem;

            if (dataProvider.DelegateTaskActionAsync is not null)
            {
                var performer = delegatedTask
                    .TaskAssignedRoles
                    .First(static i =>
                        i.TaskRoleID == CardFunctionRoles.PerformerID);

                await dataProvider.DelegateTaskActionAsync(
                    task,
                    delegatedTask,
                    new RoleEntryStorage(
                        performer.RoleID,
                        performer.RoleName),
                    context.CancellationToken);
            }

            return KrTaskManagerCompletionOptions.Delegate;
        }

        #endregion
    }
}
