#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Server.Workflow.WorkflowEngine;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Tessa.Scheme;
using Tessa.Workflow;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Реализация <see cref="IKrEditInterjectTaskManagerDataProvider"/>, обеспечивающая взаимодействие с подсистемой Workflow Engine и родительским заданием.
    /// </summary>
    public class KrEditInterjectTaskManagerWithParentTaskWorkflowDataProvider(
        IRoleGetStrategy roleGetStrategy,
        IContextRoleManager contextRoleManager,
        ICardContextRoleCache contextRoleCache,
        IKrGetSqlPerformersStrategy getSqlPerformersStrategy)
        :
            KrTaskWithParametersManagerDataProviderBase<IWorkflowEngineContext, IRoleUser>,
            IKrEditInterjectTaskManagerWithParentTaskDataProvider<IWorkflowEngineContext>
    {
        #region Constants And Static Fields

        /// <summary>
        /// Значение по умолчанию для параметра "Длительность, рабочие дни". Данное значение используется только,
        /// если в схеме не указано значение по умолчанию для поля <see cref="WorkflowConstants.KrWeEditInterjectOptionsVirtual.Period"/>.
        /// </summary>
        protected const double PeriodInDaysDefaultValue = 1.0;

        #endregion

        #region Fields

        private CardTask? parentTask;

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

        #region Base overrides

        /// <inheritdoc/>
        public override async ValueTask<IReadOnlyList<IRoleUser>> GetPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var performer = await this.GetPerformerFromSettingsAsync();

            if (performer is not null)
            {
                return [performer];
            }

            await CardComponentHelper.FillTaskAssignedRolesAsync(
                this.ParentTask,
                this.ExternalContext.DbScope,
                cancellationToken: cancellationToken);

            var parentTaskAuthor = this.ParentTask
                .TryGetTaskAssignedRoles()
                ?.FirstOrDefault(static x =>
                    x.TaskRoleID == CardFunctionRoles.AuthorID
                    && x.ParentRowID is null);

            return parentTaskAuthor is null
                ? []
                :
                [
                    new RoleEntryStorage(
                        parentTaskAuthor.RoleID,
                        parentTaskAuthor.RoleName)
                ];
        }

        /// <inheritdoc/>
        public override async ValueTask<Guid?> GetAuthorIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var authorID = await this.ExternalContext.GetAsync<Guid?>(
                WorkflowConstants.KrWeEditInterjectOptionsVirtual.SectionName,
                WorkflowConstants.KrWeEditInterjectOptionsVirtual.Author,
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
                WorkflowConstants.KrWeEditInterjectOptionsVirtual.SectionName,
                WorkflowConstants.KrWeEditInterjectOptionsVirtual.Kind,
                Names.Table_ID);

            if (!id.HasValue)
            {
                return default;
            }

            var caption = await this.ExternalContext.GetAsync<string>(
                WorkflowConstants.KrWeEditInterjectOptionsVirtual.SectionName,
                WorkflowConstants.KrWeEditInterjectOptionsVirtual.Kind,
                WorkflowConstants.Table_Field_Caption);

            return (id, caption);
        }

        /// <inheritdoc/>
        public override ValueTask<string?> GetDigestAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.GetAsync<string>(
                WorkflowConstants.KrWeEditInterjectOptionsVirtual.SectionName,
                WorkflowConstants.KrWeEditInterjectOptionsVirtual.Digest);

        /// <inheritdoc/>
        public override async ValueTask<double?> GetPlannedWorkingDaysAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            (await this.ExternalContext.GetAsync<double?>(
                    WorkflowConstants.KrWeEditInterjectOptionsVirtual.SectionName,
                    WorkflowConstants.KrWeEditInterjectOptionsVirtual.Period)
                ?? (double?) (await this.ExternalContext.CardMetadata.GetSectionsAsync(
                    this.ExternalContext.CancellationToken))[WorkflowConstants.KrWeEditInterjectOptionsVirtual.SectionName]
                .Columns[WorkflowConstants.KrWeEditInterjectOptionsVirtual.Period]
                .DefaultValue)
            ?? PeriodInDaysDefaultValue;

        /// <inheritdoc/>
        public override ValueTask<DateTime?> GetPlannedAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.GetAsync<DateTime?>(
                WorkflowConstants.KrWeEditInterjectOptionsVirtual.SectionName,
                WorkflowConstants.KrWeEditInterjectOptionsVirtual.Planned);

        /// <inheritdoc/>
        public override ValueTask<Guid?> GetParentTaskRowIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Guid?>(this.ParentTask.RowID);

        #endregion

        #region IKrEditInterjectTaskManagerWithParentTaskDataProvider<T> Members

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

        #region Private Methods

        private async ValueTask<RoleEntryStorage?> GetPerformerFromSettingsAsync()
        {
            var performerID = await this.ExternalContext.GetAsync<Guid?>(
                WorkflowConstants.KrWeEditInterjectOptionsVirtual.SectionName,
                WorkflowConstants.KrWeEditInterjectOptionsVirtual.Role,
                Names.Table_ID);

            if (!performerID.HasValue)
            {
                return null;
            }

            var performerName = await this.ExternalContext.GetAsync<string>(
                WorkflowConstants.KrWeEditInterjectOptionsVirtual.SectionName,
                WorkflowConstants.KrWeEditInterjectOptionsVirtual.Role,
                WorkflowConstants.Table_Field_Name);

            return new RoleEntryStorage(
                performerID.Value,
                performerName);
        }

        #endregion
    }
}
