#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Реализация <see cref="IKrTaskManagerContext"/>, предназначенная для совместной работы с подсистемой маршрутов.
    /// </summary>
    /// <param name="krScope"><inheritdoc cref="IKrScope" path="/summary"/></param>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
    /// <param name="unityContainer"><inheritdoc cref="UnityContainer" path="/summary"/></param>
    /// <param name="placeholderManager"><inheritdoc cref="IPlaceholderManager" path="/summary"/></param>
    public sealed class KrRoutesTaskManagerContext(
        IKrScope krScope,
        IDbScope dbScope,
        ISession session,
        ICardMetadata cardMetadata,
        IUnityContainer unityContainer,
        IPlaceholderManager placeholderManager)
        : KrTaskManagerContextBase<IStageTypeHandlerContext>
    {
        #region Fields

        private readonly IKrScope krScope = NotNullOrThrow(krScope);

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
            NotNullOrThrow(this.ExternalContext.MainCardID);

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
            this.krScope.CardIsLoaded(this.MainCardID);

        /// <inheritdoc/>
        public override DateTime StoreDateTime =>
            (this.ExternalContext.CardExtensionContext as ICardStoreExtensionContext)?.StoreDateTime ?? DateTime.UtcNow;

        /// <inheritdoc/>
        public override Card? StoreCard =>
            (this.ExternalContext.CardExtensionContext as ICardStoreExtensionContext)
                ?.Request
                .TryGetCard();

        /// <inheritdoc/>
        public override async ValueTask<Card?> GetCardAsync(
            Guid cardID,
            IValidationResultBuilder validationResult,
            bool forceLoadTasks = false,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(validationResult);

            var isMainCard = cardID == this.MainCardID;
            var card = isMainCard
                ? await this.ExternalContext
                    .MainCardAccessStrategy
                    .GetCardAsync(
                        validationResult,
                        cancellationToken: cancellationToken)
                : await this.krScope.GetMainCardAsync(
                    cardID,
                    validationResult: validationResult,
                    cancellationToken: cancellationToken);

            if (forceLoadTasks
                && card is not null)
            {
                if (isMainCard)
                {
                    await this.ExternalContext.MainCardAccessStrategy.EnsureTasksLoadedAsync(
                        validationResult,
                        cancellationToken);
                }
                else
                {
                    await this.krScope.EnsureTasksLoadedAsync(
                        cardID,
                        validationResult,
                        cancellationToken);
                }
            }

            return card;
        }

        /// <inheritdoc/>
        public override ValueTask<ICardFileContainer?> GetCardFileContainerAsync(
            Guid cardID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(validationResult);

            if (cardID == this.MainCardID)
            {
                return this.ExternalContext
                    .MainCardAccessStrategy
                    .GetFileContainerAsync(
                        validationResult: validationResult,
                        cancellationToken: cancellationToken);
            }

            return new(this.krScope
                .GetMainCardFileContainerAsync(
                    cardID,
                    validationResult: validationResult,
                    cancellationToken: cancellationToken));
        }

        /// <inheritdoc/>
        public override ValueTask<Card?> GetCardSatelliteAsync(
            Guid cardID,
            Guid satelliteTypeID,
            IValidationResultBuilder validationResult,
            Guid? taskID = null,
            CancellationToken cancellationToken = default) =>
            this.krScope.GetSatelliteAsync(
                cardID,
                taskID,
                satelliteTypeID,
                validationResult: validationResult,
                cancellationToken: cancellationToken);

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
            ValueTask ModifyTaskFuncAsync(CardTask task, CancellationToken _)
            {
                task.Planned = planned;
                task.PlannedQuants = plannedQuants;
                task.PlannedWorkingDays = plannedWorkingDays;

                modifyTaskAction?.Invoke(task);

                return ValueTask.CompletedTask;
            }

            return (await this.ExternalContext
                .WorkflowAPI
                !.SendTaskAsync(
                    taskTypeID,
                    digest,
                    roleID,
                    roleName,
                    validationResult,
                    taskParameters: null,
                    taskRowID: null,
                    modifyTaskAction: ModifyTaskFuncAsync,
                    cancellationToken: cancellationToken))?.Task;
        }

        /// <inheritdoc/>
        public override ValueTask<KrState> GetStateAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.WorkflowProcess.State);

        /// <inheritdoc/>
        public override ValueTask SetStateAsync(
            KrState state,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            this.ExternalContext.WorkflowProcess.State = state;

            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override ValueTask<Guid?> GetTaskHistoryGroupIDAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            HandlerHelper.GetTaskHistoryGroupAsync(
                this.ExternalContext,
                this.krScope,
                validationResult,
                cancellationToken);

        /// <inheritdoc/>
        public override ValueTask AddToHistoryAsync(
            Guid taskRowID,
            int cycle,
            IValidationResultBuilder validationResult,
            bool isAdvisory = false,
            CancellationToken cancellationToken = default)
        {
            this.ExternalContext.ContextualSatellite.AddToHistory(
                taskRowID,
                cycle,
                isAdvisory);

            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override ValueTask<int> GetProcessCycleAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext
                .WorkflowProcess
                .InfoStorage
                .TryGet(KrConstants.Keys.Cycle, 1));

        /// <inheritdoc/>
        public override ValueTask<string?> GetAuthorCommentAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(this.ExternalContext.WorkflowProcess.AuthorComment?.Trim());

        /// <inheritdoc/>
        public override ValueTask SetAuthorCommentAsync(
            string? comment,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            this.ExternalContext.WorkflowProcess.AuthorComment = comment?.Trim();
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override ValueTask AddActiveTaskAsync(
            Guid taskRowID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.WorkflowAPI!.AddActiveTaskAsync(
                taskRowID,
                validationResult,
                cancellationToken);

        /// <inheritdoc/>
        public override ValueTask<bool> TryRemoveActiveTaskAsync(
            Guid taskRowID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.WorkflowAPI!.TryRemoveActiveTaskAsync(
                taskRowID,
                validationResult,
                cancellationToken);

        /// <inheritdoc/>
        public override ValueTask<IReadOnlyList<Guid>> GetActiveTasksAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            this.ExternalContext.WorkflowAPI!.GetActiveTasksAsync(
                validationResult,
                cancellationToken);

        /// <inheritdoc/>
        public override async ValueTask<string?> CreateDigestAsync(
            string? baseDigest,
            CardTask? oldTask,
            string? additionalComment,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var sb = StringBuilderHelper.Acquire()
                .Append(LocalizationManager.EscapeIfLocalizationString("{$KrMessages_Stage}"))
                .Append(": ")
                .Append(LocalizationManager.EscapeIfLocalizationString(this.ExternalContext.Stage.Name));

            if (!string.IsNullOrEmpty(baseDigest))
            {
                sb
                    .Append(". ")
                    .Append(LocalizationManager.EscapeIfLocalizationString(baseDigest));
            }

            return await base.CreateDigestAsync(
                sb.ToStringAndRelease(),
                oldTask,
                additionalComment,
                validationResult,
                cancellationToken);
        }

        #endregion
    }
}
