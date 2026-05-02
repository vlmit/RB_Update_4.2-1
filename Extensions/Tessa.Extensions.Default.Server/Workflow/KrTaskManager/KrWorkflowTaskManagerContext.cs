#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.WorkflowEngine;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
using Tessa.Platform.Data;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Workflow;
using Tessa.Workflow.Helpful;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Реализация <see cref="IKrTaskManagerContext"/>, предназначенная для совместной работы с подсистемой Workflow Engine.
    /// </summary>
    /// <param name="krWorkflowStateStrategy"><inheritdoc cref="IKrWorkflowStateStrategy" path="/summary"/></param>
    /// <param name="placeholderManager"><inheritdoc cref="PlaceholderManager" path="/summary"/></param>
    public sealed class KrWorkflowTaskManagerContext(
        IKrWorkflowStateStrategy krWorkflowStateStrategy,
        IPlaceholderManager placeholderManager)
        : KrTaskManagerContextBase<IWorkflowEngineContext>
    {
        #region Fields

        /// <inheritdoc cref="IKrWorkflowStateStrategy" path="/summary"/>
        private readonly IKrWorkflowStateStrategy krWorkflowStateStrategy = NotNullOrThrow(krWorkflowStateStrategy);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override IValidationResultBuilder ValidationResult =>
            this.ExternalContext.ValidationResult;

        /// <inheritdoc/>
        public override CancellationToken CancellationToken =>
            this.ExternalContext.CancellationToken;

        /// <inheritdoc/>
        public override Guid MainCardID =>
            NotNullOrThrow(this.ExternalContext.ProcessInstance).CardID;

        /// <inheritdoc/>
        public override IDbScope DbScope =>
            this.ExternalContext.DbScope;

        /// <inheritdoc/>
        public override ISession Session =>
            this.ExternalContext.Session;

        /// <inheritdoc/>
        public override ICardMetadata CardMetadata =>
            this.ExternalContext.CardMetadata;

        /// <inheritdoc/>
        public override IUnityContainer UnityContainer =>
            this.ExternalContext.Container;

        /// <inheritdoc/>
        public override IPlaceholderManager PlaceholderManager { get; } = NotNullOrThrow(placeholderManager);

        /// <inheritdoc/>
        public override bool IsMainCardLoaded =>
            this.ExternalContext.IsMainCardLoaded;

        /// <inheritdoc/>
        public override DateTime StoreDateTime =>
            this.ExternalContext.StoreDateTime;

        /// <inheritdoc/>
        public override Card? StoreCard =>
            this.ExternalContext.StoreCard;

        /// <inheritdoc/>
        public override bool CreateDigestWithPlaceholders => true;

        /// <inheritdoc/>
        public override ValueTask<Card?> GetCardAsync(
            Guid cardID,
            IValidationResultBuilder validationResult,
            bool forceLoadTasks = false, // опция игнорируется, т.к. задания загружаются всегда
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.CardsScope.GetCardAsync(
                cardID,
                static cardID =>
                {
                    var getRequest = new CardGetRequest()
                    {
                        CardID = cardID,
                        GetTaskMode = CardGetTaskMode.All
                    };

                    getRequest.SetForbidStoringHistory(true);

                    return getRequest;
                },
                validationResult,
                cancellationToken: cancellationToken);

        /// <inheritdoc/>
        public override ValueTask<ICardFileContainer?> GetCardFileContainerAsync(
            Guid cardID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.CardsScope.GetFileContainerAsync(
                cardID,
                validationResult,
                cancellationToken);

        /// <inheritdoc/>
        public override ValueTask<Card?> GetCardSatelliteAsync(
            Guid cardID,
            Guid satelliteTypeID,
            IValidationResultBuilder validationResult,
            Guid? taskID = null,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.CardsScope.GetSatelliteAsync(
                cardID,
                taskID,
                satelliteTypeID,
                validationResult,
                cancellationToken);

        /// <inheritdoc/>
        public override async ValueTask<CardTask?> SendTaskAsync(
            Guid taskTypeID,
            string? digest,
            DateTime? planned,
            int? plannedQuants,
            double? plannedWorkingDays,
            Guid? roleID,
            string? roleName,
            IValidationResultBuilder validationResult,
            Action<CardTask>? modifyTaskAction = null,
            CancellationToken cancellationToken = default)
        {
            var cardTask = await this.ExternalContext.SendTaskAsync(
                taskTypeID,
                digest,
                planned,
                plannedQuants,
                plannedWorkingDays,
                roleID,
                roleName,
                taskRowID: null,
                parentRowID: null,
                modifyTaskAction,
                validationResult: NotNullOrThrow(validationResult),
                cancellationToken: cancellationToken);

            if (cardTask is not null)
            {
                cardTask.HistorySettings ??= [];
                cardTask.HistorySettings[KrConstants.TaskHistorySettingsKeys.ProcessKind] = cardTask.TryGetInfo()?.TryGet<string>(CardHelper.TaskProcessKindKey);
            }

            return cardTask;
        }

        /// <inheritdoc/>
        public override async ValueTask<KrState> GetStateAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var card = await this.GetCardAsync(
                this.MainCardID,
                validationResult,
                cancellationToken: cancellationToken);

            if (card is null)
            {
                return KrState.Draft;
            }

            var aci = card.GetApprovalInfoSection();
            return (KrState) aci.RawFields.Get<int>(KrConstants.KrApprovalCommonInfo.StateID);
        }

        /// <inheritdoc/>
        public override ValueTask SetStateAsync(
            KrState state,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            this.krWorkflowStateStrategy.SetStateIDAsync(
                this.ExternalContext,
                state,
                validationResult,
                cancellationToken);

        /// <inheritdoc/>
        public override ValueTask<Guid?> GetTaskHistoryGroupIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(NotNullOrThrow(this.ExternalContext.ProcessInstance).GetHistoryGroup());

        /// <inheritdoc/>
        public override ValueTask AddToHistoryAsync(
            Guid taskRowID,
            int cycle,
            IValidationResultBuilder validationResult,
            bool isAdvisory = false,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.AddToHistoryAsync(
                taskRowID,
                cycle,
                validationResult,
                isAdvisory,
                cancellationToken);

        /// <inheritdoc/>
        public override ValueTask<int> GetProcessCycleAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(
                WorkflowHelper.GetProcessCycle(
                    NotNullOrThrow(this.ExternalContext.ProcessInstance).Hash));

        /// <inheritdoc/>
        public override async ValueTask<string?> GetAuthorCommentAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var sCard = await this.ExternalContext.GetKrSatelliteAsync(
                validationResult,
                cancellationToken);

            if (sCard is null)
            {
                return null;
            }

            return sCard
                .Sections[KrConstants.KrApprovalCommonInfo.Name]
                .RawFields
                .TryGet<string>(KrConstants.KrApprovalCommonInfo.AuthorComment)
                ?.Trim();
        }

        /// <inheritdoc/>
        public override async ValueTask SetAuthorCommentAsync(
            string? comment,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var sCard = await this.ExternalContext.GetKrSatelliteAsync(
                validationResult,
                cancellationToken);

            if (sCard is null)
            {
                return;
            }

            sCard
                .Sections[KrConstants.KrApprovalCommonInfo.Name]
                .Fields[KrConstants.KrApprovalCommonInfo.AuthorComment] = comment?.Trim();
        }

        /// <inheritdoc/>
        public override ValueTask AddActiveTaskAsync(
            Guid taskRowID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.AddActiveTaskAsync(
                taskRowID,
                validationResult,
                cancellationToken);

        /// <inheritdoc/>
        public override ValueTask<bool> TryRemoveActiveTaskAsync(
            Guid taskRowID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.TryRemoveActiveTaskAsync(
                taskRowID,
                validationResult,
                cancellationToken);

        /// <inheritdoc/>
        public override ValueTask<IReadOnlyList<Guid>> GetActiveTasksAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.GetActiveTasksAsync(
                validationResult,
                cancellationToken);

        /// <inheritdoc/>
        public override void AddTaskToNext(CardTask task) =>
            this.ExternalContext.AddTaskToNextContextTasks(task);

        /// <inheritdoc/>
        public override Dictionary<string, object?> CreatePlaceholderInfo(
            CardTask? task = null)
        {
            var info = base.CreatePlaceholderInfo(task);
            return this.ExternalContext.AddPlaceholderInfoWithoutTask(
                info,
                false);
        }

        #endregion
    }
}
