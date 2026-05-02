#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Roles;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Реализация <see cref="IKrApprovalCoreTaskManagerDataProvider"/>, обеспечивающая взаимодействие с подсистемой маршрутов.
    /// </summary>
    public class KrApprovalCoreTaskManagerRoutesDataProvider(
        IRoleGetStrategy roleGetStrategy,
        IContextRoleManager contextRoleManager,
        ICardContextRoleCache contextRoleCache,
        ISession session)
        :
            KrTaskWithParametersManagerDataProviderBase<IStageTypeHandlerContext, RoleEntryStorage>,
            IKrApprovalCoreTaskManagerDataProvider<IStageTypeHandlerContext>
    {
        #region Constants And Static Fields

        /// <summary>
        /// Ключ, по которому в <see cref="Stage.InfoStorage"/> содержится текущий порядковый номер исполнителя. Тип значения: <see cref="int"/>.
        /// </summary>
        protected const string CurrentPerformerCount = nameof(CurrentPerformerCount);

        #endregion

        #region Properties

        /// <inheritdoc cref="IRoleGetStrategy" path="/summary"/>
        protected IRoleGetStrategy RoleGetStrategy { get; } = NotNullOrThrow(roleGetStrategy);

        /// <inheritdoc cref="IContextRoleManager" path="/summary"/>
        protected IContextRoleManager ContextRoleManager { get; } = NotNullOrThrow(contextRoleManager);

        /// <inheritdoc cref="ICardContextRoleCache" path="/summary"/>
        protected ICardContextRoleCache ContextRoleCache { get; } = NotNullOrThrow(contextRoleCache);

        /// <inheritdoc cref="ISession" path="/summary"/>
        protected ISession Session { get; } = NotNullOrThrow(session);

        #endregion

        #region IKrApprovalTaskManagerDataProvider<T> Members

        /// <inheritdoc/>
        public bool IsNegativeActionResult
        {
            get => this.ExternalContext.Stage.InfoStorage.TryGet<bool>(KrConstants.Keys.Disapproved);
            set => this.ExternalContext.Stage.InfoStorage[KrConstants.Keys.Disapproved] = BooleanBoxes.Box(value);
        }

        /// <inheritdoc/>
        public int CurrentPerformerIndex
        {
            get => this.ExternalContext.Stage.InfoStorage.TryGet<int>(CurrentPerformerCount);
            set => this.ExternalContext.Stage.InfoStorage[CurrentPerformerCount] = Int32Boxes.Box(value);
        }

        /// <inheritdoc/>
        public ValueTask<bool> GetIsParallelAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrApprovalSettingsVirtual.IsParallel) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetReturnWhenPositiveActionResultAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrApprovalSettingsVirtual.ReturnToAuthor) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetReturnWhenNegativeActionResultAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrApprovalSettingsVirtual.ReturnWhenDisapproved) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetCanEditCardAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrApprovalSettingsVirtual.CanEditCard) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetCanEditAnyFilesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrApprovalSettingsVirtual.CanEditFiles) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetChangeStateOnStartAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrApprovalSettingsVirtual.ChangeStateOnStart) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetChangeStateOnEndAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrApprovalSettingsVirtual.ChangeStateOnEnd) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetIsAdvisoryAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrApprovalSettingsVirtual.Advisory) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetIsDisableAutoApprovalAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrApprovalSettingsVirtual.DisableAutoApproval) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetExpectAllPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(!this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrApprovalSettingsVirtual.ReturnWhenDisapproved) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetNotReturnEditAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult((NotNullOrThrow(this.ExternalContext.ProcessInfo).ProcessParameters.TryGet<bool?>(KrConstants.Keys.NotReturnEdit, true) ?? true)
                && (this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrApprovalSettingsVirtual.NotReturnEdit) ?? false));

        /// <inheritdoc/>
        public ValueTask<bool> GetNotCreateReturnEditTaskHistoryRecordAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrApprovalSettingsVirtual.NotCreateReturnEditTaskHistoryRecord) ?? false);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override ValueTask<IReadOnlyList<RoleEntryStorage>> GetPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<RoleEntryStorage>>(
                this.ExternalContext
                    .Stage
                    .Performers
                    .Select(static i => new RoleEntryStorage(
                        i.RowID,
                        i.PerformerID,
                        i.PerformerName ?? string.Empty))
                    .ToArray());

        /// <inheritdoc/>
        public override async ValueTask<Guid?> GetAuthorIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            (await HandlerHelper.GetStageAuthorAsync(
                this.ExternalContext,
                this.RoleGetStrategy,
                this.ContextRoleManager,
                this.ContextRoleCache,
                this.Session,
                validationResult,
                cancellationToken))?.AuthorID;

        /// <inheritdoc/>
        public override ValueTask<(Guid? ID, string? Caption)> GetKindAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(HandlerHelper.GetTaskKind(this.ExternalContext));

        /// <inheritdoc/>
        public override ValueTask<string?> GetDigestAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext
                .Stage
                .SettingsStorage
                .TryGet<string>(KrConstants.KrApprovalSettingsVirtual.Comment));

        /// <inheritdoc/>
        public override ValueTask<double?> GetPlannedWorkingDaysAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<double?>(
                this.ExternalContext.Stage.Planned.HasValue
                    ? null
                    : this.ExternalContext.Stage.TimeLimitOrDefault);

        /// <inheritdoc/>
        public override ValueTask<DateTime?> GetPlannedAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.Planned);

        /// <inheritdoc/>
        public override ValueTask<Guid?> GetParentTaskRowIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.TaskInfo?.Task.RowID);

        #endregion
    }
}
