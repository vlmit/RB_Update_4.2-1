#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Server.Workflow.WorkflowEngine;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
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
    /// Реализация <see cref="IKrApprovalCoreTaskManagerDataProvider"/>, обеспечивающая взаимодействие с подсистемой Workflow Engine.
    /// </summary>
    public class KrApprovalCoreTaskManagerWorkflowDataProvider(
        IRoleGetStrategy roleGetStrategy,
        IContextRoleManager contextRoleManager,
        ICardContextRoleCache contextRoleCache,
        IKrGetSqlPerformersStrategy getSqlPerformersStrategy)
        :
            KrTaskWithParametersManagerDataProviderBase<IWorkflowEngineContext, RoleEntryStorage>,
            IKrApprovalCoreTaskManagerDataProvider<IWorkflowEngineContext>
    {
        #region Constants And Static Fields

        /// <summary>
        /// Значение по умолчанию для параметра "Длительность, рабочие дни". Данное значение используется только,
        /// если в схеме не указано значение по умолчанию для поля <see cref="WorkflowConstants.KrApprovalActionVirtual.Period"/>.
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

        #region IKrApprovalTaskManagerDataProvider<T> Members

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
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.IsParallel) ?? false;

        /// <inheritdoc/>
        public async ValueTask<bool> GetReturnWhenPositiveActionResultAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.ReturnWhenApproved) ?? false;

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
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.CanEditCard) ?? false;

        /// <inheritdoc/>
        public async ValueTask<bool> GetCanEditAnyFilesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.CanEditAnyFiles) ?? false;

        /// <inheritdoc/>
        public async ValueTask<bool> GetChangeStateOnStartAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.ChangeStateOnStart) ?? false;

        /// <inheritdoc/>
        public async ValueTask<bool> GetChangeStateOnEndAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.ChangeStateOnEnd) ?? false;

        /// <inheritdoc/>
        public async ValueTask<bool> GetIsAdvisoryAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.IsAdvisory) ?? false;

        /// <inheritdoc/>
        public async ValueTask<bool> GetIsDisableAutoApprovalAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.IsDisableAutoApproval) ?? false;

        /// <inheritdoc/>
        public async ValueTask<bool> GetExpectAllPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.ExpectAllApprovers) ?? false;

        /// <inheritdoc/>
        public ValueTask<bool> GetNotReturnEditAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(false);

        /// <inheritdoc/>
        public async ValueTask<bool> GetNotCreateReturnEditTaskHistoryRecordAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.ExternalContext.GetAsync<bool?>(
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.NotCreateReturnEditTaskHistoryRecord) ?? false;

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
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.SqlPerformersScript);

            var sqlPerformers = await this.GetSqlPerformersStrategy.GetAsync(
                sqlPerformerScript,
                this.ExternalContext.ProcessInstance!.CardID,
                validationResult,
                cancellationToken);

            if (!validationResult.IsSuccessful())
            {
                return [];
            }

            var performersRows = (await this.ExternalContext
                    .GetAllRowsAsync(WorkflowConstants.KrWeRolesVirtual.SectionName))?
                .OrderBy(p => WorkflowEngineHelper.Get<int>(p, WorkflowConstants.KrWeRolesVirtual.Order))
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
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.Author,
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
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.Kind,
                Names.Table_ID);

            if (!id.HasValue)
            {
                return default;
            }

            var caption = await this.ExternalContext.GetAsync<string>(
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.Kind,
                WorkflowConstants.Table_Field_Caption);

            return (id, caption);
        }

        /// <inheritdoc/>
        public override ValueTask<string?> GetDigestAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.GetAsync<string>(
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.Digest);

        /// <inheritdoc/>
        public override async ValueTask<double?> GetPlannedWorkingDaysAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            return (await this.ExternalContext.GetAsync<double?>(
                        WorkflowConstants.KrApprovalActionVirtual.SectionName,
                        WorkflowConstants.KrApprovalActionVirtual.Period)
                    ?? (double?) (await this.ExternalContext.CardMetadata.GetSectionsAsync(this.ExternalContext.CancellationToken))[
                        WorkflowConstants.KrApprovalActionVirtual.SectionName]
                    .Columns[WorkflowConstants.KrApprovalActionVirtual.Period]
                    .DefaultValue)
                ?? PeriodInDaysDefaultValue;
        }

        /// <inheritdoc/>
        public override ValueTask<DateTime?> GetPlannedAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            return this.ExternalContext.GetAsync<DateTime?>(
                WorkflowConstants.KrApprovalActionVirtual.SectionName,
                WorkflowConstants.KrApprovalActionVirtual.Planned);
        }

        /// <inheritdoc/>
        public override ValueTask<Guid?> GetParentTaskRowIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.Signal?.As<WorkflowEngineTaskSignal>().ParentTaskRowID);

        #endregion
    }
}
