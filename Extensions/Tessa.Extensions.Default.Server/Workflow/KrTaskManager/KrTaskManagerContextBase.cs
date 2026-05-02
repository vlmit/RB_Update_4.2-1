#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Shared.Notices;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Workflow.Helpful;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Базовая абстрактная реализация <see cref="IKrTaskManagerContext"/>.
    /// </summary>
    public abstract class KrTaskManagerContextBase :
        IKrTaskManagerContext
    {
        #region IKrTaskManagerContext Members

        /// <inheritdoc/>
        public abstract IValidationResultBuilder ValidationResult { get; }

        /// <inheritdoc/>
        public abstract CancellationToken CancellationToken { get; }

        /// <inheritdoc/>
        public abstract Guid MainCardID { get; }

        /// <inheritdoc/>
        public abstract IDbScope DbScope { get; }

        /// <inheritdoc/>
        public abstract ISession Session { get; }

        /// <inheritdoc/>
        public abstract ICardMetadata CardMetadata { get; }

        /// <inheritdoc/>
        public abstract IUnityContainer UnityContainer { get; }

        /// <inheritdoc/>
        public abstract IPlaceholderManager PlaceholderManager { get; }

        /// <inheritdoc/>
        public abstract bool IsMainCardLoaded { get; }

        /// <inheritdoc/>
        public abstract DateTime StoreDateTime { get; }

        /// <inheritdoc/>
        public abstract Card? StoreCard { get; }

        /// <inheritdoc/>
        public virtual bool CreateDigestWithPlaceholders { get; }

        /// <inheritdoc/>
        public abstract ValueTask<Card?> GetCardAsync(
            Guid cardID,
            IValidationResultBuilder validationResult,
            bool forceLoadTasks = false,
            CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract ValueTask<ICardFileContainer?> GetCardFileContainerAsync(
            Guid cardID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract ValueTask<Card?> GetCardSatelliteAsync(
            Guid cardID,
            Guid satelliteTypeID,
            IValidationResultBuilder validationResult,
            Guid? taskID = null,
            CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract ValueTask<CardTask?> SendTaskAsync(
            Guid taskTypeID,
            string? digest,
            DateTime? planned,
            int? plannedQuants,
            double? plannedWorkingDays,
            Guid? roleID,
            string? roleName,
            IValidationResultBuilder validationResult,
            Action<CardTask>? modifyTaskAction = null,
            CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract ValueTask<KrState> GetStateAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract ValueTask SetStateAsync(
            KrState state,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract ValueTask<Guid?> GetTaskHistoryGroupIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract ValueTask AddToHistoryAsync(
            Guid taskRowID,
            int cycle,
            IValidationResultBuilder validationResult,
            bool isAdvisory = false,
            CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract ValueTask<int> GetProcessCycleAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract ValueTask<string?> GetAuthorCommentAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract ValueTask SetAuthorCommentAsync(
            string? comment,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract ValueTask AddActiveTaskAsync(
            Guid taskRowID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract ValueTask<bool> TryRemoveActiveTaskAsync(
            Guid taskRowID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract ValueTask<IReadOnlyList<Guid>> GetActiveTasksAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public virtual void AddTaskToNext(CardTask task)
        {
        }

        /// <inheritdoc/>
        public virtual async ValueTask<string?> GetWithPlaceholdersAsync(
            string? text,
            CardTask? task,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(validationResult);

            var mainCard = this.IsMainCardLoaded
                ? await this.GetCardAsync(
                    this.MainCardID,
                    validationResult,
                    cancellationToken: cancellationToken)
                : null;

            return await this.PlaceholderManager.ReplaceTextAsync(
                text,
                this.Session,
                this.UnityContainer,
                this.DbScope,
                null,
                mainCard,
                cardID: this.MainCardID,
                task: task,
                info: this.CreatePlaceholderInfo(task),
                withScripts: true,
                cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public virtual Dictionary<string, object?> CreatePlaceholderInfo(
            CardTask? task = null) =>
            DefaultNotificationHelper.GetInfoWithTask(task);

        /// <inheritdoc/>
        public virtual async ValueTask<string?> CreateDigestAsync(
            string? baseDigest,
            CardTask? oldTask,
            string? additionalComment,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            const int capacityDefault = 64;

            var authorComment = await this.GetAuthorCommentAsync(
                validationResult,
                cancellationToken);

            if (!validationResult.IsSuccessful())
            {
                return null;
            }

            var isNullOrEmptyAuthorComment = string.IsNullOrEmpty(authorComment);
            var isNullOrEmptyAdditionalComment = string.IsNullOrEmpty(additionalComment);

            if (this.CreateDigestWithPlaceholders)
            {
                baseDigest = await this.GetWithPlaceholdersAsync(
                    baseDigest,
                    oldTask,
                    validationResult,
                    cancellationToken);
            }

            if (!validationResult.IsSuccessful()
                || isNullOrEmptyAuthorComment
                && isNullOrEmptyAdditionalComment)
            {
                return baseDigest;
            }

            var sb = StringBuilderHelper.Acquire(capacityDefault)
                .Append(baseDigest);

            if (!isNullOrEmptyAuthorComment)
            {
                AppendDigestPartsSeparator(sb)
                    .Append(LocalizationManager.EscapeIfLocalizationString(authorComment));
            }

            if (!isNullOrEmptyAdditionalComment)
            {
                AppendDigestPartsSeparator(sb)
                    .Append(LocalizationManager.EscapeIfLocalizationString(additionalComment));
            }

            return sb.ToStringAndRelease();

            static StringBuilder AppendDigestPartsSeparator(StringBuilder sb)
            {
                if (sb.Length > 0)
                {
                    sb
                        .AppendLine()
                        .AppendLine("{$KrActions_TaskDigestPartsSeparator}");
                }

                return sb;
            }
        }

        /// <inheritdoc/>
        public virtual async ValueTask<CardTask?> DelegateTaskAsync(
            CardTask task,
            string? digest,
            IValidationResultBuilder validationResult,
            Guid? taskKindID = null,
            string? taskKindCaption = null,
            CancellationToken cancellationToken = default)
        {
            const int capacityDefault = 128;

            ThrowIfNull(task);
            ThrowIfNull(validationResult);

            await this.TryRemoveActiveTaskAsync(
                task.RowID,
                validationResult,
                cancellationToken);

            var krTaskRawFields = task.Card.Sections[KrConstants.KrTask.Name].RawFields;
            var result = StringBuilderHelper.Acquire(capacityDefault)
                .Append("{$ApprovalHistory_TaskIsDelegated} \"")
                .Append(LocalizationManager.EscapeIfLocalizationString(krTaskRawFields.Get<string>(KrConstants.KrTask.DelegateName)))
                .Append('"');

            var comment = krTaskRawFields.TryGet<string>(KrConstants.KrTask.Comment)?.Trim();

            if (string.IsNullOrWhiteSpace(comment))
            {
                comment = null;
            }
            else
            {
                result
                    .Append(". {$ApprovalHistory_Comment}: ")
                    .Append(LocalizationManager.EscapeIfLocalizationString(comment));
            }

            task.Result = result.ToStringAndRelease();

            await this.UpdateTaskHistoryResultAsync(
                task,
                cancellationToken);

            digest = await this.CreateDigestAsync(
                digest,
                task,
                comment,
                validationResult,
                cancellationToken);

            if (!validationResult.IsSuccessful())
            {
                return null;
            }

            var delegatedTask = await this.SendTaskAsync(
                task.TypeID,
                digest,
                task.Planned,
                null,
                null,
                krTaskRawFields.Get<Guid>(KrConstants.KrTask.DelegateID),
                krTaskRawFields.Get<string>(KrConstants.KrTask.DelegateName),
                validationResult,
                cancellationToken: cancellationToken);

            if (delegatedTask is null
                || !validationResult.IsSuccessful())
            {
                return null;
            }

            delegatedTask.ParentRowID = task.RowID;
            delegatedTask.HistoryItemParentRowID = task.RowID;
            delegatedTask.GroupRowID = await this.GetTaskHistoryGroupIDAsync(
                validationResult,
                cancellationToken);

            if (!validationResult.IsSuccessful())
            {
                return null;
            }

            await CardComponentHelper.FillTaskAssignedRolesAsync(
                task,
                this.DbScope,
                cancellationToken: this.CancellationToken);

            foreach (var author in task
                .TaskAssignedRoles
                .Where(static x =>
                    x.TaskRoleID == CardFunctionRoles.AuthorID))
            {
                delegatedTask.AddAuthor(
                    author.RoleID,
                    author.RoleName,
                    author.Position);
            }

            delegatedTask.Info[CardHelper.TaskKindIDKey] = taskKindID;
            delegatedTask.Info[CardHelper.TaskKindCaptionKey] = taskKindCaption;

            if (task.Card.Sections.TryGetValue(KrConstants.KrAdditionalApprovalInfo.Name, out var oldSection))
            {
                var additionalApprovalInfoSection = new CardSection(
                    KrConstants.KrAdditionalApprovalInfo.Name,
                    oldSection.GetStorage())
                {
                    Type = CardSectionType.Table
                };

                foreach (var row in additionalApprovalInfoSection.Rows)
                {
                    // ReSharper disable AccessToStaticMemberViaDerivedType
                    row.Fields[KrConstants.KrAdditionalApprovalInfo.ID] = delegatedTask.RowID;
                    // ReSharper restore AccessToStaticMemberViaDerivedType
                    row.State = CardRowState.Inserted;
                }

                delegatedTask
                    .Card
                    .Sections[KrConstants.KrAdditionalApprovalInfo.Name]
                    .Set(additionalApprovalInfoSection);
            }

            this.AddTaskToNext(delegatedTask);

            return delegatedTask;
        }

        /// <inheritdoc/>
        public virtual ValueTask UpdateTaskHistoryResultAsync(
            CardTask task,
            CancellationToken cancellationToken = default) =>
            WorkflowEngineHelper.UpdateTaskHistoryResultAsync(
                this.DbScope,
                task,
                cancellationToken);

        #endregion
    }
}
