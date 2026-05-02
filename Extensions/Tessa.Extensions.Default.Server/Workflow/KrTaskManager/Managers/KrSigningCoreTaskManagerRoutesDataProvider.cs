#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Files;
using Tessa.Platform;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Roles;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Реализация <see cref="IKrSigningCoreTaskManagerDataProvider"/>, обеспечивающая взаимодействие с подсистемой маршрутов.
    /// </summary>
    public class KrSigningCoreTaskManagerRoutesDataProvider(
        IRoleGetStrategy roleGetStrategy,
        IContextRoleManager contextRoleManager,
        ICardContextRoleCache contextRoleCache,
        ISession session)
        :
            KrTaskWithParametersManagerDataProviderBase<IStageTypeHandlerContext, RoleEntryStorage>,
            IKrSigningCoreTaskManagerDataProvider<IStageTypeHandlerContext>
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

        #region IKrSigningTaskManagerDataProvider<T> Members

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
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.IsParallel) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetReturnWhenPositiveActionResultAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.ReturnToAuthor) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetReturnWhenNegativeActionResultAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.ReturnWhenDeclined) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetCanEditCardAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.CanEditCard) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetCanEditAnyFilesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.CanEditFiles) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetChangeStateOnStartAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.ChangeStateOnStart) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetChangeStateOnEndAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.ChangeStateOnEnd) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetExpectAllPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(!this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.ReturnWhenDeclined) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetNotReturnEditAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult((NotNullOrThrow(this.ExternalContext.ProcessInfo).ProcessParameters.TryGet<bool?>(KrConstants.Keys.NotReturnEdit, true) ?? true)
                && (this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.NotReturnEdit) ?? false));

        /// <inheritdoc/>
        public ValueTask<bool> GetAllowAdditionalApprovalAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.AllowAdditionalApproval) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetNotCreateReturnEditTaskHistoryRecordAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.NotCreateReturnEditTaskHistoryRecord) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetSignFilesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.SignCardFiles) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetNoSignFileDialogAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.NoSignFilesDialog) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetDoNotSignFileCopiesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.DoNotSignFileCopies) ?? false);

        /// <inheritdoc/>
        public ValueTask<bool> GetNoCommentDialogAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<bool?>(KrConstants.KrSigningStageSettingsVirtual.NoCommentDialog) ?? false);

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<IFileCategory>> GetFileCategoriesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            if (!this.ExternalContext.Stage.SettingsStorage.TryGetValue(KrConstants.KrSigningStageSettingsFileCategoriesVirtual.Synthetic, out var catObj)
                || catObj is not IList categories)
            {
                return ValueTask.FromResult<IReadOnlyList<IFileCategory>>([]);
            }

            return ValueTask.FromResult<IReadOnlyList<IFileCategory>>(
                categories.Cast<Dictionary<string, object?>>()
                    .Select(x => new FileCategory(x.Get<Guid?>(KrConstants.KrSigningStageSettingsFileCategoriesVirtual.FileCategoryID), 
                        x.Get<string?>(KrConstants.KrSigningStageSettingsFileCategoriesVirtual.FileCategoryName) ?? string.Empty)).ToArray());
        }

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<IFileCategory>> GetHiddenFileCategoriesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            if (!this.ExternalContext.Stage.SettingsStorage.TryGetValue(KrConstants.KrSigningStageSettingsHiddenFileCategoriesVirtual.Synthetic, out var catObj)
                || catObj is not IList categories)
            {
                return ValueTask.FromResult<IReadOnlyList<IFileCategory>>([]);
            }

            return ValueTask.FromResult<IReadOnlyList<IFileCategory>>(
                categories.Cast<Dictionary<string,object?>>()
                    .Select(x => new FileCategory(x.Get<Guid?>(KrConstants.KrSigningStageSettingsHiddenFileCategoriesVirtual.FileCategoryID), 
                        x.Get<string?>(KrConstants.KrSigningStageSettingsHiddenFileCategoriesVirtual.FileCategoryName) ?? string.Empty)).ToArray());
        }

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<Guid>> GetFilesToSelectAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<Guid>>((this.ExternalContext.Stage.InfoStorage.TryGet<IList>(KrConstants.Keys.SelectedFiles)?.Cast<Guid>()
                ?? []).ToArray());
        
        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<Guid>> GetFilesToHideAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<Guid>>((this.ExternalContext.Stage.InfoStorage.TryGet<IList>(KrConstants.Keys.HiddenFiles)?.Cast<Guid>()
                ?? []).ToArray());

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
            ValueTask.FromResult(this.ExternalContext.Stage.SettingsStorage.TryGet<string>(KrConstants.KrSigningStageSettingsVirtual.Comment));

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
