#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards.Workflow;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Events;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <summary>
    /// <see cref="IWorkflowWorker"/> для обработки процессов в подсистеме маршрутов.
    /// </summary>
    /// <param name="manager"><inheritdoc cref="KrProcessWorkflowManager" path="/summary"/></param>
    public sealed class KrProcessWorkflowWorker(
        KrProcessWorkflowManager manager) :
        WorkflowWorker<KrProcessWorkflowManager>(manager)
    {
        #region Properties

        /// <inheritdoc cref="KrProcessWorkflowContext"/>
        private KrProcessWorkflowContext WCtx => this.Manager.WorkflowContext;

        #endregion

        #region Private Methods

        private async Task StartRunnerAsync(
            IWorkflowProcessInfo info,
            CancellationToken cancellationToken = default)
        {
            // ValidationResult здесь и во всех вложенных выводах используется из context.ValidationResult
            // расширения на сохранение карточки. Таким образом все, что могло возникнуть здесь и глубже, будет корректно
            // выведено.

            var scope = this.WCtx.KrScope;
            if (!scope.MultirunEnabled(info.ProcessID)
                && !scope.FirstLaunchPerRequest(info.ProcessID))
            {
                return;
            }

            scope.AddToLaunchedLevels(info.ProcessID);

            var startingSecondaryProcess = this.WCtx.CardStoreContext.Request.GetStartingSecondaryProcess();
            StoreParentProcess(info, startingSecondaryProcess);

            var mainCardID = this.Manager.Request.Card.ID;
            var contextualSatellite = await this.WCtx.KrScope.GetKrSatelliteAsync(
                mainCardID,
                cancellationToken: cancellationToken);

            if (contextualSatellite is null)
            {
                return;
            }

            // Получаем холдер процесса в зависимости от типа процесса и его вложенности
            (var processHolderSatellite, var processHolderID, var processHolder) = await KrProcessHelper.GetProcessHolderAsync(
                scope,
                this.Manager.Request.Card.ID,
                info,
                startingSecondaryProcess,
                contextualSatellite,
                cancellationToken);

            // Ошибка будет в IKrScope.
            if (processHolderSatellite is null)
            {
                return;
            }

            var processHolderCreated = false;

            if (processHolder is null)
            {
                // ProcessHolder отсутствует, т.е. для текущего сателлита-холдер запускается самый верхний процесс.
                processHolder = new ProcessHolder()
                {
                    Persistent = true,
                    ProcessHolderID = processHolderID,

                    // Если запускается вложенный процесс, значит по нему было совершено какое-то действие, т.е. он уже был запущен ранее. Иначе запускается основной процесс.
                    MainProcessType = info.ProcessTypeName == KrConstants.KrNestedProcessName
                    ? KrProcessHelper.GetMainProcessType(info)
                    : info.ProcessTypeName,
                };
                this.WCtx.KrScope.AddProcessHolder(processHolder);

                processHolderCreated = true;
            }

            if (!processHolder.Persistent)
            {
                this.Manager.ValidationResult.AddError(
                    this,
                    "$KrProcess_Error_AsyncProcessWithoutPersistentHolder");
                return;
            }

            // Строим модель процесса на основе доступных сателлитов и холдеров.
            (var workflowProcess, var pci) = await this.WCtx.ObjectModelMapper.GetWorkflowProcessAsync(
                info,
                startingSecondaryProcess,
                contextualSatellite,
                processHolderSatellite,
                processHolder,
                cancellationToken);

            var mainCardKey = scope.LockCard(mainCardID);
            var contextualKey = scope.LockCard(contextualSatellite.ID);
            var processHolderKey = scope.LockCard(processHolderSatellite.ID);

            if (startingSecondaryProcess?.ProcessInfo is not null)
            {
                StorageHelper.Merge(
                    startingSecondaryProcess.ProcessInfo,
                    workflowProcess.InfoStorage);
            }

            var secondaryProcess = pci.SecondaryProcessID.HasValue
                ? await this.WCtx.ProcessCache.GetSecondaryProcessAsync(
                    pci.SecondaryProcessID.Value,
                    cancellationToken)
                : null;

            await using var cardLoadingStrategy = new KrScopeMainCardAccessStrategy(this.WCtx.CardID, this.WCtx.KrScope);
            var taskHistoryResolver = new KrTaskHistoryResolver(
                cardLoadingStrategy,
                this.WCtx,
                this.WCtx.ValidationResult,
                this.WCtx.TaskHistoryManager);

            var notMessageHasNoActiveStages = info.ProcessParameters.TryGet<bool>(KrConstants.Keys.NotMessageHasNoActiveStages)
                || secondaryProcess?.NotMessageHasNoActiveStages == true;

            var runnerContext = new KrProcessRunnerContext(
                workflowAPI: new WorkflowAPIBridge(this.Manager, info),
                taskHistoryResolver: taskHistoryResolver,
                mainCardAccessStrategy: cardLoadingStrategy,
                cardID: this.WCtx.CardID,
                cardType: this.WCtx.CardType,
                docTypeID: this.WCtx.DocTypeID,
                krComponents: this.WCtx.KrComponents,
                contextualSatellite: contextualSatellite,
                processHolderSatellite: processHolderSatellite,
                workflowProcess: workflowProcess,
                processHolder: processHolder,
                processInfo: info,
                validationResult: this.Manager.ValidationResult,
                cardContext: this.WCtx.CardStoreContext,
                defaultPreparingGroupStrategyFunc: this.DefaultPreparingStrategy,
                parentProcessTypeName: info.ProcessParameters.TryGet<string>(KrConstants.Keys.ParentProcessType),
                parentProcessID: info.ProcessParameters.TryGet<Guid?>(KrConstants.Keys.ParentProcessID),
                isProcessHolderCreated: processHolderCreated,
                updateCardFuncAsync: this.UpdateCardAsync,
                notMessageHasNoActiveStages: notMessageHasNoActiveStages,
                secondaryProcess: secondaryProcess,
                cancellationToken: cancellationToken);

            await this.WCtx.AsyncProcessRunner.RunAsync(runnerContext);

            if (runnerContext.WorkflowProcess.CurrentApprovalStageRowID is null)
            {
                await this.WCtx.EventManager.RaiseAsync(
                    DefaultEventTypes.AsyncProcessCompleted,
                    currentStage: null,
                    runnerMode: KrProcessRunnerMode.Async,
                    runnerContext: runnerContext,
                    cancellationToken: cancellationToken);

                this.Manager.ProcessesAwaitingRemoval.Add(info);

                this.WCtx.CardStoreContext.Info.SetProcessInfoAtEnd(workflowProcess.InfoStorage);

                if (runnerContext.InitiationCause == KrProcessRunnerInitiationCause.StartProcess)
                {
                    this.WCtx.CardStoreContext.Info.SetAsyncProcessCompletedSimultaniosly();
                }
            }

            await runnerContext.UpdateCardAsync();

            if (runnerContext.IsProcessHolderCreated)
            {
                this.WCtx.KrScope.RemoveProcessHolder(processHolder.ProcessHolderID);
            }

            if (mainCardKey.HasValue)
            {
                scope.ReleaseCard(mainCardID, mainCardKey.Value);
            }

            if (contextualKey.HasValue)
            {
                scope.ReleaseCard(contextualSatellite.ID, contextualKey.Value);
            }

            if (processHolderKey.HasValue)
            {
                scope.ReleaseCard(processHolderSatellite.ID, processHolderKey.Value);
            }
        }

        private static void StoreParentProcess(
            IWorkflowProcessInfo info,
            StartingSecondaryProcessInfo? startingSecondaryProcess)
        {
            if (startingSecondaryProcess is not null)
            {
                info.ProcessParameters[KrConstants.Keys.ParentProcessType] = startingSecondaryProcess.ParentProcessTypeName;
                info.ProcessParameters[KrConstants.Keys.ParentProcessID] = startingSecondaryProcess.ParentProcessID;
                info.PendingProcessParametersUpdate = true;
            }
        }

        private IPreparingGroupRecalcStrategy DefaultPreparingStrategy() =>
            new ForwardPreparingGroupRecalcStrategy(
                this.WCtx.DbScope, 
                this.WCtx.Session);

        private async ValueTask UpdateCardAsync(
            IKrProcessRunnerContext context)
        {
            // Только создающий процесс холдер может переводить его обратно.
            if (context.IsProcessHolderCreated)
            {
                var mainCard = await context.MainCardAccessStrategy.GetCardAsync(
                    cancellationToken: context.CancellationToken);

                if (mainCard is null)
                {
                    return;
                }

                await this.WCtx.ObjectModelMapper.ObjectModelToCardRowsAsync(
                    context.ProcessHolder,
                    context.ProcessHolderSatellite!,
                    context.ContextualSatellite!,
                    mainCard,
                    context.CancellationToken);
            }
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override Task StartProcessCoreAsync(
            IWorkflowProcessInfo processInfo,
            CancellationToken cancellationToken = default) =>
            this.StartRunnerAsync(processInfo, cancellationToken);

        /// <inheritdoc/>
        protected override async Task CompleteTaskCoreAsync(
            IWorkflowTaskInfo taskInfo,
            CancellationToken cancellationToken = default)
        {
            if (taskInfo.ProcessTypeName == KrConstants.KrSecondaryProcessName)
            {
                var card = await this.WCtx.KrScope.GetSecondaryKrSatelliteAsync(
                    taskInfo.ProcessID,
                    cancellationToken: cancellationToken);

                if (card is null)
                {
                    return;
                }

                this.Manager.SpecifySatelliteID(card.ID, false);
            }

            await this.StartRunnerAsync(taskInfo, cancellationToken);
        }

        /// <inheritdoc/>
        protected override async Task ReinstateTaskCoreAsync(
            IWorkflowTaskInfo taskInfo,
            CancellationToken cancellationToken = default)
        {
            if (taskInfo.ProcessTypeName == KrConstants.KrSecondaryProcessName)
            {
                var card = await this.WCtx.KrScope.GetSecondaryKrSatelliteAsync(
                    taskInfo.ProcessID,
                    cancellationToken: cancellationToken);

                if (card is null)
                {
                    return;
                }

                this.Manager.SpecifySatelliteID(card.ID);
            }

            await this.StartRunnerAsync(taskInfo, cancellationToken);
        }

        /// <inheritdoc/>
        protected override async Task<bool> ProcessSignalCoreAsync(
            IWorkflowSignalInfo signalInfo,
            CancellationToken cancellationToken = default)
        {
            if (signalInfo.ProcessTypeName == KrConstants.KrSecondaryProcessName)
            {
                var card = await this.WCtx.KrScope.GetSecondaryKrSatelliteAsync(
                    signalInfo.ProcessID,
                    cancellationToken: cancellationToken);

                if (card is null)
                {
                    return false;
                }

                this.Manager.SpecifySatelliteID(card.ID, false);
            }

            // Сразу проставляем признак Handled, что бы можно было из этапа, запущенного по сигналу, запустить новый процесс.
            var signalID = signalInfo.Signal.ID;
            var queueItem = this.Manager
                .Request
                .Card
                .TryGetWorkflowQueue()
                ?.Items
                .FirstOrDefault(i => NotNullOrThrow(i.TryGetSignal()).ID == signalID)
                ?? throw new InvalidOperationException($"Can't find an {nameof(WorkflowQueueItem)} that contains {nameof(WorkflowQueueSignal)} with identifier \"{signalID:B}\".");

            queueItem.Handled = true;

            await this.StartRunnerAsync(signalInfo, cancellationToken);
            return true;
        }

        #endregion
    }
}
