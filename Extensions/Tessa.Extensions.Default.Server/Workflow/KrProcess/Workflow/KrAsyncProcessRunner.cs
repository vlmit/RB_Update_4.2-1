#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.SqlProcessing;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.StateMachine;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <summary>
    /// Раннер, используемый для выполнения асинхронных процессов маршрутов документов.
    /// </summary>
    /// <param name="processContainer"><inheritdoc cref="IKrProcessContainer"/></param>
    /// <param name="stageGroupCompilationCache"><inheritdoc cref="IKrStageGroupCompilationCache"/></param>
    /// <param name="stageTemplateCompilationCache"><inheritdoc cref="IKrStageTemplateCompilationCache"/></param>
    /// <param name="executor"><inheritdoc cref="IKrExecutor"/></param>
    /// <param name="scope"><inheritdoc cref="IKrScope"/></param>
    /// <param name="dbScope"><inheritdoc cref="IDbScope"/></param>
    /// <param name="processCache"><inheritdoc cref="IKrProcessCache"/></param>
    /// <param name="unityContainer"><inheritdoc cref="IUnityContainer"/></param>
    /// <param name="session"><inheritdoc cref="ISession"/></param>
    /// <param name="runnerProvider"><inheritdoc cref="IKrProcessRunnerProvider"/></param>
    /// <param name="typesCache"><inheritdoc cref="IKrTypesCache"/></param>
    /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata"/></param>
    /// <param name="stateMachine"><inheritdoc cref="IKrProcessStateMachine"/></param>
    /// <param name="sqlExecutor"><inheritdoc cref="IKrSqlExecutor"/></param>
    /// <param name="cardCache"><inheritdoc cref="ICardCache"/></param>
    /// <param name="stageSerializer"><inheritdoc cref="IKrStageSerializer"/></param>
    /// <param name="objectModelMapper"><inheritdoc cref="IObjectModelMapper"/></param>
    /// <param name="tokenProvider"><inheritdoc cref="IKrTokenProvider"/></param>
    /// <param name="cardRepository"><inheritdoc cref="ICardRepository"/></param>
    /// <param name="cardRepositoryEwt">Репозиторий для управления карточками, не выполняющий действия в транзакции и взятия блокировки на карточку.</param>
    /// <param name="cardStreamServerRepository"><inheritdoc cref="ICardStreamServerRepository"/></param>
    /// <param name="cardStreamServerRepositoryEwt">Репозиторий для потокового управления карточками на сервере, не выполняющий действия в транзакции и взятия блокировки на карточку.</param>
    /// <param name="cardTransactionStrategy"><inheritdoc cref="ICardTransactionStrategy"/></param>
    /// <param name="transactionScope"><inheritdoc cref="ITransactionScope"/></param>
    /// <param name="krStageTemplateLockStrategy"><inheritdoc cref="IKrStageTemplateLockStrategy"/></param>
    public sealed class KrAsyncProcessRunner(
        IKrProcessContainer processContainer,
        IKrStageGroupCompilationCache stageGroupCompilationCache,
        IKrStageTemplateCompilationCache stageTemplateCompilationCache,
        IKrExecutor executor,
        IKrScope scope,
        IDbScope dbScope,
        IKrProcessCache processCache,
        IUnityContainer unityContainer,
        ISession session,
        IKrProcessRunnerProvider runnerProvider,
        IKrTypesCache typesCache,
        ICardMetadata cardMetadata,
        IKrProcessStateMachine stateMachine,
        IKrSqlExecutor sqlExecutor,
        ICardCache cardCache,
        IKrStageSerializer stageSerializer,
        IObjectModelMapper objectModelMapper,
        IKrTokenProvider tokenProvider,
        ICardRepository cardRepository,
        [Dependency(CardRepositoryNames.ExtendedWithoutTransactionAndLocking)] ICardRepository cardRepositoryEwt,
        ICardStreamServerRepository cardStreamServerRepository,
        [Dependency(CardRepositoryNames.ExtendedWithoutTransactionAndLocking)] ICardStreamServerRepository cardStreamServerRepositoryEwt,
        ICardTransactionStrategy cardTransactionStrategy,
        ITransactionScope transactionScope,
        IKrStageTemplateLockStrategy krStageTemplateLockStrategy) :
        KrProcessRunnerBase(
              processContainer,
              stageGroupCompilationCache,
              stageTemplateCompilationCache,
              executor,
              scope,
              dbScope,
              processCache,
              unityContainer,
              session,
              runnerProvider,
              typesCache,
              cardMetadata,
              stateMachine,
              sqlExecutor,
              cardCache,
              stageSerializer,
              objectModelMapper,
              tokenProvider,
              cardRepository,
              cardRepositoryEwt,
              cardStreamServerRepository,
              cardStreamServerRepositoryEwt,
              cardTransactionStrategy,
              transactionScope,
              krStageTemplateLockStrategy)
    {
        #region Base Overrides

        /// <inheritdoc />
        protected override KrProcessRunnerMode RunnerMode { get; } = KrProcessRunnerMode.Async;

        /// <inheritdoc />
        protected override async Task<bool> PrepareAsync(IKrProcessRunnerContext context)
        {
            if (this.Scope.HasLaunchedRunner(context.ProcessInfo!.ProcessID))
            {
                context.ValidationResult.AddError(this, "$KrProcess_ErrorMessage_NestedProcessRunner");
                return false;
            }

            this.Scope.AddLaunchedRunner(context.ProcessInfo.ProcessID);

            if (context.InitiationCause != KrProcessRunnerInitiationCause.StartProcess)
            {
                return true;
            }

            if (context.WorkflowProcess.CurrentApprovalStageRowID.HasValue)
            {
                context.ValidationResult.AddError(this, "$KrStages_ProcessAlreadyStarted");
                return false;
            }

            await this.SetAuthorAsync(context);

            await this.InitialRecalcAsync(context);

            if (!context.ValidationResult.IsSuccessful())
            {
                return false;
            }

            if (context.WorkflowProcess.Stages.Count == 0)
            {
                if (!context.NotMessageHasNoActiveStages)
                {
                    context.ValidationResult.AddError(this, KrErrorHelper.FormatEmptyRoute(context.SecondaryProcess));
                }

                return false;
            }

            foreach (var stage in context.WorkflowProcess.Stages)
            {
                stage.State = KrStageState.Inactive;
            }

            return true;
        }

        /// <inheritdoc />
        protected override ValueTask FinalizeAsync(
            IKrProcessRunnerContext context,
            Exception? exception = null)
        {
            if (!context.NotMessageHasNoActiveStages
                && context.WorkflowProcess.Stages.Count > 0
                && context.InitiationCause == KrProcessRunnerInitiationCause.StartProcess
                && context.WorkflowProcess.Stages.All(p => p.State == KrStageState.Skipped || p.Hidden && context.SecondaryProcess is null))
            {
                context.ValidationResult.AddError(this, KrErrorHelper.FormatEmptyRoute(context.SecondaryProcess));
            }

            this.Scope.RemoveLaunchedRunner(context.ProcessInfo!.ProcessID);

            return new ValueTask();
        }

        /// <inheritdoc/>
        protected override async Task<NextAction> ProcessStageHandlerResultAsync(
            StageHandlerResult result,
            IKrProcessRunnerContext context)
        {
            // InProgress и None не делают ничего.
            if (result.Action is not StageHandlerAction.InProgress
                and not StageHandlerAction.None)
            {
                return await base.ProcessStageHandlerResultAsync(
                    result,
                    context);
            }

            return new NextAction();
        }

        #endregion

        #region Private Methods

        private async Task InitialRecalcAsync(
            IKrProcessRunnerContext context)
        {
            if (!context.CardID.HasValue)
            {
                return;
            }

            HashSet<Guid>? executionUnitIDs;
            if (context.SecondaryProcess is { } secondaryProcess)
            {
                var stageGroups = await this.ProcessCache.GetStageGroupsForSecondaryProcessAsync(
                    secondaryProcess.ID,
                    context.CancellationToken);

                executionUnitIDs = new HashSet<Guid>(stageGroups.Select(static p => p.ID));
            }
            else
            {
                executionUnitIDs = null;
            }

            await using var cardLoadingStrategy = new KrScopeMainCardAccessStrategy(
                context.CardID.Value,
                this.Scope);

            var ctx = new KrExecutionContext(
                cardContext: context.CardContext,
                mainCardAccessStrategy: cardLoadingStrategy,
                cardID: context.CardID,
                cardType: context.CardType,
                docTypeID: context.DocTypeID,
                krComponents: context.KrComponents,
                workflowProcess: context.WorkflowProcess,
                validationResult: context.ValidationResult,
                executionUnitIDs: executionUnitIDs, // или null, тогда выполнится все что возможно
                secondaryProcess: context.SecondaryProcess, // или null
                cancellationToken: context.CancellationToken
            );

            await this.Executor.ExecuteAsync(ctx);
        }

        #endregion
    }
}
