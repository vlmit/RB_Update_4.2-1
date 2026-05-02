#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <inheritdoc cref="IKrAdditionalApprovalTaskManagerWithParentTaskDataProvider"/>
    public class KrAdditionalApprovalTaskManagerWithParentTaskDataProvider :
        KrTaskWithParametersManagerDataProviderBase<AdditionalRoleEntryStorage>,
        IKrAdditionalApprovalTaskManagerWithParentTaskDataProvider
    {
        #region Fields

        private CardTask? parentTask;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="session"><inheritdoc cref="Session" path="/summary"/></param>
        public KrAdditionalApprovalTaskManagerWithParentTaskDataProvider(
            ISession session) =>
            this.Session = NotNullOrThrow(session);

        #endregion

        #region Properties

        /// <inheritdoc cref="ISession" path="/summary"/>
        protected ISession Session { get; }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override ValueTask<IReadOnlyList<AdditionalRoleEntryStorage>> GetPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(validationResult);

            var taskSections = this.ParentTask
                .TryGetCard()
                ?.TryGetSections();

            var roleRows = taskSections
                ?.TryGet(KrConstants.KrAdditionalApprovalUsers.Name)
                ?.TryGetRows();

            if (roleRows is not { Count: > 0 })
            {
                return ValueTask.FromResult<IReadOnlyList<AdditionalRoleEntryStorage>>([]);
            }

            var firstIsResponsible = taskSections
                ?.TryGet(KrConstants.KrAdditionalApproval.Name)
                ?.TryGetRawFields()
                ?.TryGet<bool>(KrConstants.KrAdditionalApproval.FirstIsResponsible) ?? false;

            var roles = new AdditionalRoleEntryStorage[roleRows.Count];
            var i = 0;

            foreach (var roleRow in roleRows
                .OrderBy(static i => i.Get<int>(KrConstants.KrAdditionalApprovalUsers.Order)))
            {
                roles[i++] = new AdditionalRoleEntryStorage(
                    roleRow.RowID,
                    roleRow.Get<Guid>(KrConstants.KrAdditionalApprovalUsers.RoleID),
                    roleRow.Get<string>(KrConstants.KrAdditionalApprovalUsers.RoleName) ?? string.Empty,
                    firstIsResponsible);

                if (firstIsResponsible)
                {
                    firstIsResponsible = false;
                }
            }

            return ValueTask.FromResult<IReadOnlyList<AdditionalRoleEntryStorage>>(roles);
        }

        /// <inheritdoc/>
        public override ValueTask<Guid?> GetAuthorIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Guid?>(this.Session.User.ID);

        /// <inheritdoc/>
        public override ValueTask<string?> GetDigestAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ParentTask
                .TryGetCard()
                ?.TryGetSections()
                ?.TryGet(KrConstants.KrAdditionalApproval.Name)
                ?.TryGetRawFields()
                ?.TryGet<string>(KrConstants.KrAdditionalApproval.Comment)
                ?.Trim());

        /// <inheritdoc/>
        public override ValueTask<double?> GetPlannedWorkingDaysAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ParentTask
                .TryGetCard()
                ?.TryGetSections()
                ?.TryGet(KrConstants.KrAdditionalApproval.Name)
                ?.TryGetRawFields()
                ?.TryGet<double?>(KrConstants.KrAdditionalApproval.TimeLimitation));

        /// <inheritdoc/>
        public override ValueTask<Guid?> GetParentTaskRowIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Guid?>(this.ParentTask.RowID);

        #endregion

        #region IKrAdditionalApprovalTaskManagerWithParentTaskDataProvider Members

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
