#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.ApprovalProcess
{
    /// <summary>
    /// Реализация <see cref="IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider{T}"/>, обеспечивающая взаимодействие с подсистемой процессов согласования.
    /// </summary>
    public class KrApprovalProcessAdditionalApprovalTaskManagerDataProvider :
        KrTaskWithParametersManagerDataProviderBase<KrApprovalProcessExternalContext, AdditionalRoleEntryStorage>,
        IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider<KrApprovalProcessExternalContext>
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
            return ValueTask.FromResult((IReadOnlyList<AdditionalRoleEntryStorage>) Array.Empty<AdditionalRoleEntryStorage>());
        }

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
