#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Collections;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Tessa.Workflow.ApprovalProcess.Nodes;

namespace Tessa.Extensions.Default.Server.Workflow.ApprovalProcess
{
    /// <summary>
    /// Реализация <see cref="IKrApprovalCoreTaskManagerDataProvider"/>, обеспечивающая взаимодействие с подсистемой процессов согласования.
    /// </summary>
    /// <remarks>
    /// Инициализирует новый экземпляр класса.
    /// </remarks>
    /// <param name="roleGetStrategy"><inheritdoc cref="RoleGetStrategy" path="/summary"/></param>
    /// <param name="contextRoleManager"><inheritdoc cref="ContextRoleManager" path="/summary"/></param>
    /// <param name="session"><inheritdoc cref="Session" path="/summary"/></param>
    public class KrApprovalProcessCoreTaskManagerDataProvider(
        IRoleGetStrategy roleGetStrategy,
        IContextRoleManager contextRoleManager,
        ISession session) :
        KrTaskWithParametersManagerDataProviderBase<KrApprovalProcessExternalContext, RoleEntryStorage>,
        IKrApprovalCoreTaskManagerDataProvider<KrApprovalProcessExternalContext>
    {
        #region Properties

        /// <inheritdoc cref="IRoleGetStrategy" path="/summary"/>
        protected IRoleGetStrategy RoleGetStrategy { get; } = NotNullOrThrow(roleGetStrategy);

        /// <inheritdoc cref="IContextRoleManager" path="/summary"/>
        protected IContextRoleManager ContextRoleManager { get; } = NotNullOrThrow(contextRoleManager);

        /// <inheritdoc cref="ISession" path="/summary"/>
        protected ISession Session { get; } = NotNullOrThrow(session);

        #endregion

        #region IKrApprovalTaskManagerDataProvider<T> Members

        /// <inheritdoc/>
        public bool IsNegativeActionResult { get; set; }

        /// <inheritdoc/>
        public int CurrentPerformerIndex { get; set; }

        /// <inheritdoc/>
        public ValueTask<bool> GetIsParallelAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(true);

        /// <inheritdoc/>
        public ValueTask<bool> GetReturnWhenPositiveActionResultAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(false);

        /// <inheritdoc/>
        public ValueTask<bool> GetReturnWhenNegativeActionResultAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(false);

        /// <inheritdoc/>
        public ValueTask<bool> GetCanEditCardAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(false);

        /// <inheritdoc/>
        public ValueTask<bool> GetCanEditAnyFilesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(false);

        /// <inheritdoc/>
        public async ValueTask<bool> GetChangeStateOnStartAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var instance = await this.ExternalContext.Context.GetProcessInstanceAsync(
                validationResult,
                cancellationToken);

            if (instance is null
                || !instance.Settings.ChangeStateOnStart)
            {
                return false;
            }

            var currentNodeID = NotNullOrThrow(this.ExternalContext.Context.CurrentNode).NodeID;
            if (instance.Process.Edges.TryFirst(x => x.Target == currentNodeID, out var inboundEdge)
                && instance.Process.Nodes.TryFirst(x => inboundEdge.Source == x.ID, out var previousNode)
                && previousNode.Type.Equals(NodeTypes.Start, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public async ValueTask<bool> GetChangeStateOnEndAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var instance = await this.ExternalContext.Context.GetProcessInstanceAsync(
                validationResult,
                cancellationToken);

            if (instance is null
                || !instance.Settings.ChangeStateOnEnd)
            {
                return false;
            }

            var currentNodeID = NotNullOrThrow(this.ExternalContext.Context.CurrentNode).NodeID;
            if (instance.Process.Edges.TryFirst(x => x.Source == currentNodeID, out var outboundEdge)
                && instance.Process.Nodes.TryFirst(x => outboundEdge.Target == x.ID, out var nextNode)
                && nextNode.Type.Equals(NodeTypes.Finish, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public ValueTask<bool> GetIsAdvisoryAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(false);

        /// <inheritdoc/>
        public ValueTask<bool> GetIsDisableAutoApprovalAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(true);

        /// <inheritdoc/>
        public async ValueTask<bool> GetExpectAllPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var instance = await this.ExternalContext.Context.GetProcessInstanceAsync(validationResult, cancellationToken);
            if (instance is null)
            {
                return false;
            }

            return !instance.Settings.ReturnAfterDisapproval;
        }

        /// <inheritdoc/>
        public ValueTask<bool> GetNotReturnEditAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(true);

        /// <inheritdoc/>
        public ValueTask<bool> GetNotCreateReturnEditTaskHistoryRecordAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(true);

        #endregion

        #region Base overrides

        /// <inheritdoc/>
        public override async ValueTask<IReadOnlyList<RoleEntryStorage>> GetPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(validationResult);

            List<RoleEntryStorage> result = [];
            foreach (var approver in this.ExternalContext.NodeData.Approvers)
            {
                if (approver.SkipOnCurrentCycle)
                {
                    continue;
                }

                result.Add(
                    new RoleEntryStorage(
                        approver.ID,
                        approver.Role.ID,
                        approver.Role.Name));
            }

            return result;
        }

        /// <inheritdoc/>
        public override ValueTask ResetStoredPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        /// <inheritdoc/>
        public override async ValueTask<Guid?> GetAuthorIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var krSatellite = await this.ExternalContext.Context.GetSatelliteAsync(
                DefaultCardTypes.KrSatelliteTypeID,
                this.ExternalContext.Context.CardID,
                null,
                validationResult,
                cancellationToken);

            if (krSatellite is null)
            {
                return null;
            }

            var aciSection = krSatellite.GetApprovalInfoSection();
            var authorID = aciSection.RawFields.TryGet<Guid?>(KrConstants.KrProcessCommonInfo.AuthorID);

            return authorID ?? this.Session.User.ID;
        }

        /// <inheritdoc/>
        public override ValueTask<(Guid? ID, string? Caption)> GetKindAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(((Guid?) null, (string?) null));

        /// <inheritdoc/>
        public override ValueTask<string?> GetDigestAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.NodeData.Text);

        /// <inheritdoc/>
        public override ValueTask<double?> GetPlannedWorkingDaysAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult((double?) this.ExternalContext.NodeData.Duration);

        /// <inheritdoc/>
        public override ValueTask<DateTime?> GetPlannedAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult((DateTime?) null);

        /// <inheritdoc/>
        public override ValueTask<Guid?> GetParentTaskRowIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult((Guid?) null);

        #endregion
    }
}
