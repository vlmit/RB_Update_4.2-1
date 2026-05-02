#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Workflow.ApprovalProcess;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.ApprovalProcess
{
    /// <summary>
    /// Реализация <see cref="IKrTaskManagerContext"/>, предназначенная для совместной работы с подсистемой процессов согласования.
    /// </summary>
    /// <param name="dbScope"><inheritdoc cref="DbScope" path="/summary"/></param>
    /// <param name="session"><inheritdoc cref="Session" path="/summary"/></param>
    /// <param name="cardMetadata"><inheritdoc cref="CardMetadata" path="/summary"/></param>
    /// <param name="unityContainer"><inheritdoc cref="UnityContainer" path="/summary"/></param>
    /// <param name="placeholderManager"><inheritdoc cref="PlaceholderManager" path="/summary"/></param>
    /// <param name="krDocumentStateManager"><inheritdoc cref="DocumentStateManager" path="/summary"/></param>
    public sealed class KrApprovalProcessTaskManagerContext(
        IDbScope dbScope,
        ISession session,
        ICardMetadata cardMetadata,
        IUnityContainer unityContainer,
        IPlaceholderManager placeholderManager,
        IKrDocumentStateManager krDocumentStateManager)
        : KrTaskManagerContextBase<KrApprovalProcessExternalContext>
    {
        #region Properties

        /// <inheritdoc cref="IKrDocumentStateManager" path="/summary"/>
        public IKrDocumentStateManager DocumentStateManager { get; } = NotNullOrThrow(krDocumentStateManager);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override IValidationResultBuilder ValidationResult =>
            this.ExternalContext.Context.ValidationResult;

        /// <inheritdoc/>
        public override CancellationToken CancellationToken =>
            this.ExternalContext.Context.CancellationToken;

        /// <inheritdoc/>
        public override Guid MainCardID =>
            this.ExternalContext.Context.CardID;

        /// <inheritdoc/>
        public override IDbScope DbScope { get; } = NotNullOrThrow(dbScope);

        /// <inheritdoc/>
        public override ISession Session { get; } = NotNullOrThrow(session);

        /// <inheritdoc/>
        public override ICardMetadata CardMetadata { get; } = NotNullOrThrow(cardMetadata);

        /// <inheritdoc/>
        public override IUnityContainer UnityContainer { get; } = NotNullOrThrow(unityContainer);

        /// <inheritdoc/>
        public override IPlaceholderManager PlaceholderManager { get; } = NotNullOrThrow(placeholderManager);

        /// <inheritdoc/>
        public override bool IsMainCardLoaded =>
            this.ExternalContext.Context.IsMainCardLoaded;

        /// <inheritdoc/>
        public override DateTime StoreDateTime =>
            this.ExternalContext.Context.ExecutionDateTime;

        /// <inheritdoc/>
        public override Card? StoreCard =>
            this.ExternalContext.Context.StoreCard;

        /// <inheritdoc/>
        public override ValueTask<Card?> GetCardAsync(
            Guid cardID,
            IValidationResultBuilder validationResult,
            bool forceLoadTasks = false,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.Context.GetCardAsync(
                cardID,
                validationResult,
                forceLoadTasks,
                cancellationToken: cancellationToken);

        /// <inheritdoc/>
        public override ValueTask<ICardFileContainer?> GetCardFileContainerAsync(
            Guid cardID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.Context.GetCardFileContainerAsync(
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
            this.ExternalContext.Context.GetSatelliteAsync(
                satelliteTypeID,
                cardID,
                taskID,
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
            var task = await this.ExternalContext.Context
                .SendTaskAsync(
                    taskTypeID,
                    cancellationToken: cancellationToken);

            if (task is null)
            {
                return null;
            }

            task.Digest = digest;
            task.Planned = planned;
            task.PlannedQuants = plannedQuants;
            task.PlannedWorkingDays = plannedWorkingDays;

            if (roleID is not null)
            {
                task.AddPerformer(
                    roleID.Value,
                    roleName,
                    true);
            }

            if (modifyTaskAction is not null)
            {
                modifyTaskAction(task);
            }

            return task;
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
        public override async ValueTask SetStateAsync(
            KrState state,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var mainCard = await this.GetCardAsync(
                this.MainCardID,
                validationResult,
                cancellationToken: cancellationToken);

            if (mainCard is null)
            {
                return;
            }

            var krSatellite = await this.GetCardSatelliteAsync(
                this.MainCardID,
                DefaultCardTypes.KrSatelliteTypeID,
                validationResult,
                cancellationToken: cancellationToken);

            if (krSatellite is null)
            {
                return;
            }

            var (_, hasMainSatelliteChanges, _) = await this.DocumentStateManager.SetStateAsync(
                mainCard,
                krSatellite,
                state,
                cancellationToken);

            if (hasMainSatelliteChanges)
            {
                var approvalCommonInfoFields = krSatellite.GetApprovalInfoSection().Fields;
                approvalCommonInfoFields[KrConstants.KrApprovalCommonInfo.StateChangedDateTimeUTC] = DateTime.UtcNow;

                this.ExternalContext.Context.ModifyStoreRequest(
                    this.MainCardID,
                    static i => i.AffectVersion = true);
            }
        }

        /// <inheritdoc/>
        public override async ValueTask<Guid?> GetTaskHistoryGroupIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var processInstance = await this.ExternalContext.Context.GetProcessInstanceAsync(validationResult, cancellationToken);
            return processInstance?.Settings.HistoryGroupID;
        }

        /// <inheritdoc/>
        public override async ValueTask AddToHistoryAsync(
            Guid taskRowID,
            int cycle,
            IValidationResultBuilder validationResult,
            bool isAdvisory = false,
            CancellationToken cancellationToken = default)
        {
            var sCard = await this.GetCardSatelliteAsync(
                this.MainCardID,
                DefaultCardTypes.KrSatelliteTypeID,
                validationResult,
                cancellationToken: cancellationToken);

            if (sCard is null)
            {
                return;
            }

            sCard.AddToHistory(
                taskRowID,
                cycle: cycle,
                advisory: isAdvisory);
        }

        /// <inheritdoc/>
        public override async ValueTask<int> GetProcessCycleAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var processInstance = await this.ExternalContext.Context.GetProcessInstanceAsync(validationResult, cancellationToken);
            return processInstance?.Settings.Cycle ?? 0;
        }

        /// <inheritdoc/>
        public override async ValueTask<string?> GetAuthorCommentAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var sCard = await this.GetCardSatelliteAsync(
                this.MainCardID,
                DefaultCardTypes.KrSatelliteTypeID,
                validationResult,
                cancellationToken: cancellationToken);

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
            var sCard = await this.GetCardSatelliteAsync(
                this.MainCardID,
                DefaultCardTypes.KrSatelliteTypeID,
                validationResult,
                cancellationToken: cancellationToken);

            if (sCard is null)
            {
                return;
            }

            sCard
                .Sections[KrConstants.KrApprovalCommonInfo.Name]
                .Fields[KrConstants.KrApprovalCommonInfo.AuthorComment] = comment?.Trim();
        }

        /// <inheritdoc/>
        public override async ValueTask AddActiveTaskAsync(
            Guid taskRowID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            this.ExternalContext.Context.CurrentNodeTasks.Add(
                new ApprovalProcessTaskInfo
                {
                    ID = Guid.NewGuid(),
                    NodeID = NotNullOrThrow(this.ExternalContext.Context.CurrentNode).ID,
                    TaskID = taskRowID,
                });

            var sCard = await this.GetCardSatelliteAsync(
                this.MainCardID,
                DefaultCardTypes.KrSatelliteTypeID,
                validationResult,
                null,
                cancellationToken);

            if (sCard is null)
            {
                return;
            }

            var activeTasksSection = sCard.GetActiveTasksSection();

            if (activeTasksSection.Rows.Any(p => taskRowID.Equals(p.Fields[KrConstants.KrActiveTasks.TaskID])))
            {
                throw new InvalidOperationException($"Task with id \"{taskRowID:B}\" is already active.");
            }

            var row = activeTasksSection.Rows.Add();
            row.State = CardRowState.Inserted;
            row.RowID = Guid.NewGuid();
            row.Fields[KrConstants.KrActiveTasks.TaskID] = taskRowID;
        }

        /// <inheritdoc/>
        public override async ValueTask<bool> TryRemoveActiveTaskAsync(
            Guid taskRowID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            this.ExternalContext.Context.CurrentNodeTasks.RemoveAll(x => x.TaskID == taskRowID);

            var sCard = await this.GetCardSatelliteAsync(
                this.MainCardID,
                DefaultCardTypes.KrSatelliteTypeID,
                validationResult,
                null,
                cancellationToken);

            if (sCard is null)
            {
                return false;
            }

            var activeTasksSection = sCard.GetActiveTasksSection();
            var activeTaskRow = activeTasksSection.Rows.FirstOrDefault(p => taskRowID.Equals(p.Fields[KrConstants.KrActiveTasks.TaskID]));

            switch (activeTaskRow?.State)
            {
                case null:
                case CardRowState.Deleted:
                    return false;
                case CardRowState.Inserted:
                    activeTasksSection.Rows.Remove(activeTaskRow);
                    break;
                case CardRowState.Modified:
                case CardRowState.None:
                    activeTaskRow.State = CardRowState.Deleted;
                    break;
            }

            return true;
        }

        /// <inheritdoc/>
        public override ValueTask<IReadOnlyList<Guid>> GetActiveTasksAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(
                (IReadOnlyList<Guid>) this.ExternalContext.Context.CurrentNodeTasks.Select(x => x.TaskID).ToArray());
        }

        #endregion
    }
}
