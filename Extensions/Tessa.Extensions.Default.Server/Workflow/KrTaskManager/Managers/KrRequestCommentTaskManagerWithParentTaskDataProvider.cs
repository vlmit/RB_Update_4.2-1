#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Roles;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <inheritdoc cref="IKrRequestCommentTaskManagerWithParentTaskDataProvider"/>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    public class KrRequestCommentTaskManagerWithParentTaskDataProvider(
        ISession session)
        : KrTaskWithParametersManagerDataProviderBase<IRoleUser>,
        IKrRequestCommentTaskManagerWithParentTaskDataProvider
    {
        #region Fields

        private CardTask? parentTask;

        #endregion

        #region Properties

        /// <inheritdoc cref="ISession" path="/summary"/>
        protected ISession Session { get; } = NotNullOrThrow(session);

        #endregion

        #region Base overrides

        /// <inheritdoc/>
        public override ValueTask<IReadOnlyList<IRoleUser>> GetPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult((IReadOnlyList<IRoleUser>?) this.ParentTask
                .TryGetCard()
                ?.TryGetSections()
                ?.TryGet(KrConstants.KrCommentators.Name)
                ?.TryGetRows()
                ?.Select(static c =>
                    c.TryGetValue(KrConstants.KrCommentators.CommentatorID, out var idObj)
                    && idObj is Guid id
                    && c.TryGetValue(KrConstants.KrCommentators.CommentatorName, out var nameObj)
                    && nameObj is string name
                    ? new RoleUser(id, name)
                    : (IRoleUser) null!)
                .Where(static c => c is not null)
                .Distinct(RoleUserIDComparer<IRoleUser>.Instance)
                .ToArray() ?? []);

        /// <inheritdoc/>
        public override ValueTask<Guid?> GetAuthorIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Guid?>(this.ParentTask.UserID ?? this.Session.User.ID);

        /// <inheritdoc/>
        public override ValueTask<string?> GetDigestAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ParentTask.TypeID == DefaultTaskTypes.KrAdditionalApprovalTypeID
                ? this.ParentTask
                    .TryGetCard()
                    ?.TryGetSections()
                    ?.TryGet(KrConstants.KrAdditionalApprovalTaskInfo.Name)
                    ?.TryGetRawFields()
                    ?.TryGet<string>(KrConstants.KrAdditionalApprovalTaskInfo.Comment)
                : this.ParentTask
                    .TryGetCard()
                    ?.TryGetSections()
                    ?.TryGet(KrConstants.KrTask.Name)
                    ?.TryGetRawFields()
                    ?.TryGet<string>(KrConstants.KrTask.Comment));

        /// <inheritdoc/>
        public override ValueTask<double?> GetPlannedWorkingDaysAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ParentTask.PlannedWorkingDays);

        public override ValueTask<int?> GetPlannedQuantsAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ParentTask.PlannedQuants);

        /// <inheritdoc/>
        public override ValueTask<DateTime?> GetPlannedAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ParentTask.Planned);

        /// <inheritdoc/>
        public override ValueTask<Guid?> GetParentTaskRowIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Guid?>(this.ParentTask.RowID);

        #endregion

        #region IKrRequestCommentTaskManagerWithParentTaskDataProvider Members

        /// <inheritdoc/>
        public CardTask ParentTask
        {
            get => NotNullOrThrow(this.parentTask);
            set
            {
                ThrowIfSealed(this);
                this.parentTask = NotNullOrThrow(value);
            }
        }

        #endregion
    }
}
