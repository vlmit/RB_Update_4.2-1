#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Реализация <see cref="IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider{T}"/>, обеспечивающая взаимодействие с подсистемой маршрутов.
    /// </summary>
    public class KrAdditionalApprovalTaskManagerWithParentApprovalTaskRoutesDataProvider :
        KrTaskWithParametersManagerDataProviderBase<IStageTypeHandlerContext, AdditionalRoleEntryStorage>,
        IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider<IStageTypeHandlerContext>
    {
        #region Fields

        private CardTask? parentTask;

        private RoleEntryStorage? parentTaskPerformer;

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override ValueTask<IReadOnlyList<AdditionalRoleEntryStorage>> GetPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(validationResult);

            var rowsObj = this.ExternalContext
                .Stage
                .SettingsStorage
                .TryGet<IList>(KrConstants.KrAdditionalApprovalUsersCardVirtual.Synthetic);

            if (rowsObj is not { Count: > 0 })
            {
                return ValueTask.FromResult<IReadOnlyList<AdditionalRoleEntryStorage>>([]);
            }

            var rows = rowsObj.Cast<Dictionary<string, object?>>();

            var performers = new List<AdditionalRoleEntryStorage>();
            var setIsResponsible = false;

            foreach (var row in rows)
            {
                var isResponsible = row.Get<bool>(KrConstants.KrAdditionalApprovalUsersCardVirtual.IsResponsible);

                if (isResponsible)
                {
                    if (setIsResponsible)
                    {
                        validationResult.AddError(
                            this,
                            "$KrMessages_MoreThenOneResponsible");

                        return ValueTask.FromResult<IReadOnlyList<AdditionalRoleEntryStorage>>([]);
                    }

                    setIsResponsible = true;
                }

                var mainApproverRowID = row.Get<Guid>(KrConstants.KrAdditionalApprovalUsersCardVirtual.MainApproverRowID);

                if (mainApproverRowID != this.ParentTaskPerformer.RowID)
                {
                    continue;
                }

                performers.Add(new AdditionalRoleEntryStorage(
                    row.Get<Guid>(KrConstants.KrAdditionalApprovalUsersCardVirtual.RowID),
                    row.Get<Guid>(KrConstants.KrAdditionalApprovalUsersCardVirtual.RoleID),
                    row.Get<string>(KrConstants.KrAdditionalApprovalUsersCardVirtual.RoleName) ?? string.Empty,
                    isResponsible,
                    mainApproverRowID));
            }

            return ValueTask.FromResult<IReadOnlyList<AdditionalRoleEntryStorage>>(performers);
        }

        /// <inheritdoc/>
        public override ValueTask<Guid?> GetAuthorIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ParentTask
                .TryGetTaskAssignedRoles()
                ?.FirstOrDefault(static p =>
                    p.TaskRoleID == CardFunctionRoles.AuthorID
                    && p.ParentRowID is null)
                ?.RoleID);

        /// <inheritdoc/>
        public override ValueTask<double?> GetPlannedWorkingDaysAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ParentTask.PlannedWorkingDays);

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

        #region IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider<T> Members

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

        /// <inheritdoc/>
        public RoleEntryStorage ParentTaskPerformer
        {
            get => NotNullOrThrow(this.parentTaskPerformer);
            set
            {
                ThrowIfSealed(this);
                this.parentTaskPerformer = NotNullOrThrow(value);
            }
        }

        #endregion
    }
}
