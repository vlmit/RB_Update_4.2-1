#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Server.Workflow.WorkflowEngine;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
using Tessa.Files;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Tessa.Scheme;
using Tessa.Workflow;
using Tessa.Workflow.Helpful;
using Tessa.Workflow.Signals;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Реализация <see cref="IKrSigningCoreTaskManagerDataProvider"/>, обеспечивающая взаимодействие с подсистемой Workflow Engine.
    /// </summary>
    public class KrSigningCoreTaskManagerWorkflowDataProvider(
        IRoleGetStrategy roleGetStrategy,
        IContextRoleManager contextRoleManager,
        ICardContextRoleCache contextRoleCache,
        IKrGetSqlPerformersStrategy getSqlPerformersStrategy)
        :
            KrTaskWithParametersManagerDataProviderBase<IWorkflowEngineContext, RoleEntryStorage>,
            IKrSigningCoreTaskManagerDataProvider<IWorkflowEngineContext>
    {
        #region Constants And Static Fields

        /// <summary>
        /// Значение по умолчанию для параметра "Длительность, рабочие дни". Данное значение используется только,
        /// если в схеме не указано значение по умолчанию для поля <see cref="WorkflowConstants.KrSigningActionVirtual.Period"/>.
        /// </summary>
        protected const double PeriodInDaysDefaultValue = 1.0;

        #endregion

        #region Properties

        /// <inheritdoc cref="IRoleGetStrategy" path="/summary"/>
        protected IRoleGetStrategy RoleGetStrategy { get; } = NotNullOrThrow(roleGetStrategy);

        /// <inheritdoc cref="IContextRoleManager" path="/summary"/>
        protected IContextRoleManager ContextRoleManager { get; } = NotNullOrThrow(contextRoleManager);

        /// <inheritdoc cref="ICardContextRoleCache" path="/summary"/>
        protected ICardContextRoleCache ContextRoleCache { get; } = NotNullOrThrow(contextRoleCache);

        /// <inheritdoc cref="IKrGetSqlPerformersStrategy" path="/summary"/>
        protected IKrGetSqlPerformersStrategy GetSqlPerformersStrategy { get; } = NotNullOrThrow(getSqlPerformersStrategy);

        #endregion

        #region IKrSigningTaskManagerDataProvider<T> Members

        /// <inheritdoc/>
        public bool IsNegativeActionResult
        {
            get => NotNullOrThrow(this.ExternalContext.ActionInstance).Hash.TryGet<bool>(WorkflowConstants.NamesKeys.IsNegativeActionResult);
            set => NotNullOrThrow(this.ExternalContext.ActionInstance).Hash[WorkflowConstants.NamesKeys.IsNegativeActionResult] = BooleanBoxes.Box(value);
        }

        /// <inheritdoc/>
        public int CurrentPerformerIndex
        {
            get => NotNullOrThrow(this.ExternalContext.ActionInstance).Hash.TryGet<int>(WorkflowConstants.NamesKeys.CurrentPerformerIndex);
            set => NotNullOrThrow(this.ExternalContext.ActionInstance).Hash[WorkflowConstants.NamesKeys.CurrentPerformerIndex] = Int32Boxes.Box(value);
        }

        /// <inheritdoc/>
        public async ValueTask<bool> GetIsParallelAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.IsParallel) ?? false;

        /// <inheritdoc/>
        public async ValueTask<bool> GetReturnWhenPositiveActionResultAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.ReturnWhenApproved) ?? false;

        /// <inheritdoc/>
        public ValueTask<bool> GetReturnWhenNegativeActionResultAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(false);

        /// <inheritdoc/>
        public async ValueTask<bool> GetCanEditCardAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.CanEditCard) ?? false;

        /// <inheritdoc/>
        public async ValueTask<bool> GetCanEditAnyFilesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.CanEditAnyFiles) ?? false;

        /// <inheritdoc/>
        public async ValueTask<bool> GetChangeStateOnStartAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.ChangeStateOnStart) ?? false;

        /// <inheritdoc/>
        public async ValueTask<bool> GetChangeStateOnEndAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.ChangeStateOnEnd) ?? false;

        /// <inheritdoc/>
        public async ValueTask<bool> GetExpectAllPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.ExpectAllSigners) ?? false;

        /// <inheritdoc/>
        public ValueTask<bool> GetNotReturnEditAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(false);

        /// <inheritdoc/>
        public async ValueTask<bool> GetAllowAdditionalApprovalAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.AllowAdditionalApproval) ?? false;

        /// <inheritdoc/>
        public async ValueTask<bool> GetNotCreateReturnEditTaskHistoryRecordAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.NotCreateReturnEditTaskHistoryRecord) ?? false;
        
        /// <inheritdoc/>
        public async ValueTask<bool> GetSignFilesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.SignCardFiles) ?? false;
        
        /// <inheritdoc/>
        public async ValueTask<bool> GetNoSignFileDialogAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.NoSignFilesDialog) ?? false;
        
        /// <inheritdoc/>
        public async ValueTask<bool> GetDoNotSignFileCopiesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.DoNotSignFileCopies) ?? false;

        /// <inheritdoc/>
        public async ValueTask<bool> GetNoCommentDialogAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.NoCommentDialog) ?? false;

        /// <inheritdoc/>
        public async ValueTask<IReadOnlyList<IFileCategory>> GetFileCategoriesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var rows = await this.ExternalContext
                    .GetAllRowsAsync(WorkflowConstants.KrSigningActionFileCategoriesVirtual.SectionName)
                        ?? Enumerable.Empty<Dictionary<string, object?>>();
            return rows
                .Select(static x => new FileCategory(
                    WorkflowEngineHelper.Get<Guid>(x, WorkflowEngineHelper.WildCardHashMark, Names.Table_ID),
                    WorkflowEngineHelper.Get<string?>(x, WorkflowEngineHelper.WildCardHashMark, "Name") ?? string.Empty))
                .ToArray();
        }

        /// <inheritdoc/>
        public async ValueTask<IReadOnlyList<IFileCategory>> GetHiddenFileCategoriesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var rows = await this.ExternalContext
                    .GetAllRowsAsync(WorkflowConstants.KrSigningActionHiddenFileCategoriesVirtual.SectionName)
                        ?? Enumerable.Empty<Dictionary<string, object?>>();
            return rows
                .Select(static x => new FileCategory(
                    WorkflowEngineHelper.Get<Guid>(x, WorkflowEngineHelper.WildCardHashMark, Names.Table_ID),
                    WorkflowEngineHelper.Get<string?>(x, WorkflowEngineHelper.WildCardHashMark, "Name") ?? string.Empty))
                .ToArray();
        }

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<Guid>> GetFilesToSelectAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<Guid>>((this.ExternalContext.ActionInstance?.Hash.TryGet<IList>(KrConstants.Keys.SelectedFiles)?.Cast<Guid>()
                ?? []).ToArray());

        /// <inheritdoc/>
        public ValueTask<IReadOnlyList<Guid>> GetFilesToHideAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<Guid>>((this.ExternalContext.ActionInstance?.Hash.TryGet<IList>(KrConstants.Keys.HiddenFiles)?.Cast<Guid>()
                ?? []).ToArray());

        #endregion

        #region Base overrides

        /// <inheritdoc/>
        public override async ValueTask<IReadOnlyList<RoleEntryStorage>> GetPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(validationResult);

            // может быть List<object> или List<Dictionary<string, object?>>, поэтому используем ковариантный интерфейс
            var roleList = NotNullOrThrow(this.ExternalContext.ActionInstance)
                .Hash
                .TryGet<IReadOnlyCollection<object>>(WorkflowConstants.NamesKeys.RoleList);

            if (roleList is not null)
            {
                return roleList
                    .Cast<Dictionary<string, object?>>()
                    .Select(static i => new RoleEntryStorage(i))
                    .ToArray();
            }

            var sqlPerformerScript = await this.ExternalContext.GetAsync<string>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.SqlPerformersScript);

            var sqlPerformers = await this.GetSqlPerformersStrategy.GetAsync(
                sqlPerformerScript,
                this.ExternalContext.ProcessInstance!.CardID,
                validationResult,
                cancellationToken);

            if (!validationResult.IsSuccessful())
            {
                return [];
            }

            var performersRows = await this.ExternalContext
                    .GetAllRowsAsync(WorkflowConstants.KrWeRolesVirtual.SectionName)
                ?? Enumerable.Empty<Dictionary<string, object?>>();

            var performers = WorkflowCommonHelper.CombinePerformers(
                performersRows.Select(static i => new RoleEntryStorage(
                    WorkflowEngineHelper.Get<Guid>(
                        i,
                        Names.Table_RowID),
                    WorkflowEngineHelper.Get<Guid>(
                        i,
                        WorkflowEngineHelper.WildCardHashMark,
                        Names.Table_ID),
                    WorkflowEngineHelper.Get<string>(
                        i,
                        WorkflowEngineHelper.WildCardHashMark,
                        WorkflowConstants.Table_Field_Name) ?? string.Empty)),
                sqlPerformers,
                KrConstants.SqlApproverRoleID);

            if (performers.Count == 0)
            {
                validationResult.AddError(
                    this,
                    WorkflowHelper.GetValidatePerformerNotSpecifiedMessage(
                        this.ExternalContext.ActionTemplate!,
                        this.ExternalContext.NodeTemplate!));

                return [];
            }

            WorkflowEngineHelper.Set(
                this.ExternalContext.ActionInstance.Hash,
                performers.Select(static i => i.GetStorage()).ToArray(),
                WorkflowConstants.NamesKeys.RoleList);

            return performers;
        }

        /// <inheritdoc/>
        public override ValueTask ResetStoredPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            WorkflowEngineHelper.Set<object>(
                NotNullOrThrow(this.ExternalContext.ActionInstance).Hash,
                null,
                WorkflowConstants.NamesKeys.RoleList);

            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override async ValueTask<Guid?> GetAuthorIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var authorID = await this.ExternalContext.GetAsync<Guid?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.Author,
                Names.Table_ID);

            return await this.ExternalContext.GetAuthorIDAsync(
                this.RoleGetStrategy,
                this.ContextRoleManager,
                this.ContextRoleCache,
                authorID);
        }

        /// <inheritdoc/>
        public override async ValueTask<(Guid? ID, string? Caption)> GetKindAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var id = await this.ExternalContext.GetAsync<Guid?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.Kind,
                Names.Table_ID);

            if (!id.HasValue)
            {
                return default;
            }

            var caption = await this.ExternalContext.GetAsync<string>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.Kind,
                WorkflowConstants.Table_Field_Caption);

            return (id, caption);
        }

        /// <inheritdoc/>
        public override ValueTask<string?> GetDigestAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.GetAsync<string>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.Digest);

        /// <inheritdoc/>
        public override async ValueTask<double?> GetPlannedWorkingDaysAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            (await this.ExternalContext.GetAsync<double?>(
                    WorkflowConstants.KrSigningActionVirtual.SectionName,
                    WorkflowConstants.KrSigningActionVirtual.Period)
                ?? (double?) (await this.ExternalContext.CardMetadata.GetSectionsAsync(this.ExternalContext.CancellationToken))[
                    WorkflowConstants.KrSigningActionVirtual.SectionName]
                .Columns[WorkflowConstants.KrSigningActionVirtual.Period]
                .DefaultValue)
            ?? PeriodInDaysDefaultValue;

        /// <inheritdoc/>
        public override ValueTask<DateTime?> GetPlannedAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.GetAsync<DateTime?>(
                WorkflowConstants.KrSigningActionVirtual.SectionName,
                WorkflowConstants.KrSigningActionVirtual.Planned);

        /// <inheritdoc/>
        public override ValueTask<Guid?> GetParentTaskRowIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Signal?.As<WorkflowEngineTaskSignal>().ParentTaskRowID);

        #endregion
    }
}
