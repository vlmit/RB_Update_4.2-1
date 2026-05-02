#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
using Tessa.Platform.Validation;
using Tessa.Scheme;
using Tessa.Workflow;
using Tessa.Workflow.Helpful;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Реализация <see cref="IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider{T}"/>, обеспечивающая взаимодействие с подсистемой Workflow Engine.
    /// </summary>
    public class KrAdditionalApprovalTaskManagerWithParentApprovalTaskWorkflowDataProvider :
        KrTaskWithParametersManagerDataProviderBase<IWorkflowEngineContext, AdditionalRoleEntryStorage>,
        IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider<IWorkflowEngineContext>
    {
        #region Fields

        private CardTask? parentTask;

        private RoleEntryStorage? parentTaskPerformer;

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async ValueTask<IReadOnlyList<AdditionalRoleEntryStorage>> GetPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(validationResult);

            var rows = await this.ExternalContext.GetAllRowsAsync(WorkflowConstants.KrApprovalActionAdditionalPerformersVirtual.SectionName);

            if (rows is not { Count: > 0 })
            {
                return [];
            }

            var performers = new List<AdditionalRoleEntryStorage>();
            var setIsResponsible = false;

            foreach (var row in rows)
            {
                var isResponsible = WorkflowEngineHelper.Get<bool>(
                    row,
                    WorkflowConstants.KrApprovalActionAdditionalPerformersVirtual.IsResponsible);

                if (isResponsible)
                {
                    if (setIsResponsible)
                    {
                        validationResult.AddError(
                            this,
                            "$KrMessages_MoreThenOneResponsible");

                        return [];
                    }

                    setIsResponsible = true;
                }

                var mainApproverRowID = WorkflowEngineHelper.Get<Guid>(
                    row,
                    WorkflowConstants.KrApprovalActionAdditionalPerformersVirtual.MainApprover,
                    Names.Table_RowID);

                if (mainApproverRowID != this.ParentTaskPerformer.RowID)
                {
                    continue;
                }

                performers.Add(new AdditionalRoleEntryStorage(
                    WorkflowEngineHelper.Get<Guid>(
                        row,
                        Names.Table_RowID),
                    WorkflowEngineHelper.Get<Guid>(
                        row,
                        WorkflowConstants.KrApprovalActionAdditionalPerformersVirtual.Role,
                        Names.Table_ID),
                    WorkflowEngineHelper.Get<string>(
                        row,
                        WorkflowConstants.KrApprovalActionAdditionalPerformersVirtual.Role,
                        WorkflowConstants.Table_Field_Name) ?? string.Empty,
                    isResponsible,
                    mainApproverRowID));
            }

            return performers;
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
