#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.ComponentModel;
using Tessa.Cards.Extensions;
using Tessa.Cards.Numbers;
using Tessa.Cards.Workflow;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.SourceBuilders;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.SqlProcessing;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.UserAPI;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.GlobalSignals;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.StateMachine;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrCompilers;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Formatting;
using Tessa.Platform.ObjectLocking;
using Tessa.Platform.Redis;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <summary>
    /// Абстрактный базовый класс раннера используемого для выполнения процессов маршрутов документов.
    /// </summary>
    public abstract class KrProcessRunnerBase :
        IKrProcessRunner
    {
        #region Nested Types

        /// <summary>
        /// Результат обработки результата выполнения обработчика этапа.
        /// </summary>
        [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
        protected struct NextAction
        {
            #region Properties

            /// <summary>
            /// Возвращает или задаёт значение, показывающее, должен ли быть выполнен переход к следующему этапу или нет.
            /// </summary>
            public bool ContinueToNextStage { get; set; }

            /// <summary>
            /// Возвращает или задаёт значение, показывающее, что был выполнен переход на этап или группу.
            /// </summary>
            public bool AfterTransition { get; set; }

            /// <summary>
            /// Возвращает или задаёт значение, показывающее, что был выполнен переход на группу.
            /// </summary>
            public bool AfterGroupTransition { get; set; }

            #endregion

            #region Private Methods

            private readonly string GetDebuggerDisplay() =>
                $"{DebugHelper.GetTypeName(this)}: " +
                $"{nameof(this.ContinueToNextStage)} = {this.ContinueToNextStage}, " +
                $"{nameof(this.AfterTransition)} = {this.AfterTransition}, " +
                $"{nameof(this.AfterGroupTransition)} = {this.AfterGroupTransition}";

            #endregion
        }

        #endregion

        #region Fields

        private readonly IKrTokenProvider tokenProvider;

        private readonly ICardRepository cardRepository;

        private readonly ICardRepository cardRepositoryEwt;

        private readonly ICardStreamServerRepository cardStreamServerRepository;

        private readonly ICardStreamServerRepository cardStreamServerRepositoryEwt;

        private readonly ICardTransactionStrategy cardTransactionStrategy;

        private readonly ITransactionScope transactionScope;

        private readonly IKrStageTemplateLockStrategy krStageTemplateLockStrategy;

        #endregion

        #region Constructors

        protected KrProcessRunnerBase(
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
            ICardRepository cardRepositoryEwt,
            ICardStreamServerRepository cardStreamServerRepository,
            ICardStreamServerRepository cardStreamServerRepositoryEwt,
            ICardTransactionStrategy cardTransactionStrategy,
            ITransactionScope transactionScope,
            IKrStageTemplateLockStrategy krStageTemplateLockStrategy)
        {
            this.ProcessContainer = NotNullOrThrow(processContainer);
            this.StageGroupCompilationCache = NotNullOrThrow(stageGroupCompilationCache);
            this.StageTemplateCompilationCache = NotNullOrThrow(stageTemplateCompilationCache);
            this.Executor = NotNullOrThrow(executor);
            this.Scope = NotNullOrThrow(scope);
            this.DbScope = NotNullOrThrow(dbScope);
            this.ProcessCache = NotNullOrThrow(processCache);
            this.UnityContainer = NotNullOrThrow(unityContainer);
            this.Session = NotNullOrThrow(session);
            this.RunnerProvider = NotNullOrThrow(runnerProvider);
            this.TypesCache = NotNullOrThrow(typesCache);
            this.CardMetadata = NotNullOrThrow(cardMetadata);
            this.StateMachine = NotNullOrThrow(stateMachine);
            this.SqlExecutor = NotNullOrThrow(sqlExecutor);
            this.CardCache = NotNullOrThrow(cardCache);
            this.StageSerializer = NotNullOrThrow(stageSerializer);
            this.ObjectModelMapper = NotNullOrThrow(objectModelMapper);
            this.tokenProvider = NotNullOrThrow(tokenProvider);
            this.cardRepository = NotNullOrThrow(cardRepository);
            this.cardRepositoryEwt = NotNullOrThrow(cardRepositoryEwt);
            this.cardStreamServerRepository = NotNullOrThrow(cardStreamServerRepository);
            this.cardStreamServerRepositoryEwt = NotNullOrThrow(cardStreamServerRepositoryEwt);
            this.cardTransactionStrategy = NotNullOrThrow(cardTransactionStrategy);
            this.transactionScope = NotNullOrThrow(transactionScope);
            this.krStageTemplateLockStrategy = NotNullOrThrow(krStageTemplateLockStrategy);
        }

        #endregion

        #region IKrProcessRunner Members

        /// <inheritdoc />
        public async Task RunAsync(
            IKrProcessRunnerContext context)
        {
            ThrowIfNull(context);


            this.AssertKrScope();

            await using var transactionScope = TransactionScopeContext.HasCurrent ? null : this.transactionScope.Create();
            Exception? exception = null;
            try
            {
                if (await this.krStageTemplateLockStrategy.ObtainReaderLockAsync(context.CancellationToken) is not { IsSuccessful: true })
                {
                    throw new ValidationException(ValidationResult.FromText(this, "$KrMessages_StageTemplatesReadErrorMessage", ValidationResultType.Error));
                }

                await using (this.DbScope.Create())
                {
                    if (await this.PrepareAsync(context))
                    {
                        await this.RunInternalAsync(context);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ProcessRunnerInterruptedException e)
            when (string.IsNullOrWhiteSpace(e.Message) && context.ValidationResult.IsSuccessful())
            {
                exception = e;

                ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .ErrorException(e, LocalizeName("KrProcessRunner_Interrupted"))
                    .End();
            }
            catch (Exception e)
            {
                exception = e;

                context.ValidationResult.Add(this.Scope.ValidationResult);
                this.Scope.ValidationResult.Clear();
                ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .ErrorException(e)
                    .End();
            }
            finally
            {
                context.ValidationResult.Add(this.Scope.ValidationResult);
                this.Scope.ValidationResult.Clear();

                await this.FinalizeAsync(context, exception);

                if (transactionScope is not null)
                {
                    await this.transactionScope.PerformHandlersAsync(
                        context.ValidationResult.IsSuccessful(),
                        context.ValidationResult,
                        context.CancellationToken);
                }
            }
        }

        #endregion

        #region Properties

        /// <inheritdoc cref="IKrProcessContainer" path="/summary"/>
        protected IKrProcessContainer ProcessContainer { get; }

        /// <inheritdoc cref="IKrStageGroupCompilationCache" path="/summary"/>
        protected IKrStageGroupCompilationCache StageGroupCompilationCache { get; }

        /// <inheritdoc cref="IKrStageTemplateCompilationCache" path="/summary"/>
        protected IKrStageTemplateCompilationCache StageTemplateCompilationCache { get; }

        /// <inheritdoc cref="IKrExecutor" path="/summary"/>
        protected IKrExecutor Executor { get; }

        /// <inheritdoc cref="IKrScope" path="/summary"/>
        protected IKrScope Scope { get; }

        /// <inheritdoc cref="IDbScope" path="/summary"/>
        protected IDbScope DbScope { get; }

        /// <inheritdoc cref="IKrProcessCache" path="/summary"/>
        protected IKrProcessCache ProcessCache { get; }

        /// <inheritdoc cref="IUnityContainer" path="/summary"/>
        protected IUnityContainer UnityContainer { get; }

        /// <inheritdoc cref="ISession" path="/summary"/>
        protected ISession Session { get; }

        /// <inheritdoc cref="IKrProcessRunnerProvider" path="/summary"/>
        protected IKrProcessRunnerProvider RunnerProvider { get; }

        /// <inheritdoc cref="IKrTypesCache" path="/summary"/>
        protected IKrTypesCache TypesCache { get; }

        /// <inheritdoc cref="ICardMetadata" path="/summary"/>
        protected ICardMetadata CardMetadata { get; }

        /// <inheritdoc cref="IKrProcessStateMachine" path="/summary"/>
        protected IKrProcessStateMachine StateMachine { get; }

        /// <inheritdoc cref="IKrSqlExecutor" path="/summary"/>
        protected IKrSqlExecutor SqlExecutor { get; }

        /// <inheritdoc cref="ICardCache" path="/summary"/>
        protected ICardCache CardCache { get; }

        /// <inheritdoc cref="IKrStageSerializer" path="/summary"/>
        protected IKrStageSerializer StageSerializer { get; }

        /// <inheritdoc cref="IObjectModelMapper" path="/summary"/>
        protected IObjectModelMapper ObjectModelMapper { get; }

        /// <inheritdoc cref="KrProcessRunnerMode" path="/summary"/>
        protected abstract KrProcessRunnerMode RunnerMode { get; }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Выполняет действия перед выполнением процесса.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если процесс может быть запущен, иначе - <see langword="false"/>.</returns>
        protected virtual Task<bool> PrepareAsync(
            IKrProcessRunnerContext context) => TaskBoxes.True;

        /// <summary>
        /// Выполняет действия после завершения обработки процесса.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <param name="exception">Исключение, произошедшее при обработке процесса, или значение <see langword="null"/>, если выполнение завершилось без исключений.</param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        protected virtual ValueTask FinalizeAsync(
            IKrProcessRunnerContext context,
            Exception? exception = null) => ValueTask.CompletedTask;

        /// <summary>
        /// Обрабатывает результат выполнения обработчика этапа <see cref="IKrProcessRunnerContext.CurrentStage"/>.
        /// </summary>
        /// <param name="result"><inheritdoc cref="StageHandlerResult" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <returns><inheritdoc cref="NextAction" path="/summary"/></returns>
        protected virtual async Task<NextAction> ProcessStageHandlerResultAsync(
            StageHandlerResult result,
            IKrProcessRunnerContext context)
        {
            ThrowIfNull(context);
            ThrowIfNull(context.CurrentStage);

            result = await this.CoerceStageHandlerResultAsync(
                result,
                context);

            switch (result.Action)
            {
                case StageHandlerAction.Complete:
                case StageHandlerAction.Skip:
                    // Ставится Complete или Skipped
                    SetStageFinalState(
                        context.CurrentStage,
                        result);

                    context.CurrentStage = await this.RunAfterStageAsync(
                        context.CurrentStage,
                        context);

                    return new NextAction
                    {
                        ContinueToNextStage = true,
                    };
                case StageHandlerAction.Transition:
                    {
                        if (result.TransitionID is null)
                        {
                            throw new InvalidOperationException(
                                $"Result with action = {StageHandlerAction.Transition} " +
                                "lacks transitionID (group into current stage group)");
                        }

                        context.CurrentStage = await this.RunAfterStageAsync(
                            context.CurrentStage,
                            context);

                        var transit = TransitToStage(
                            context.CurrentStage,
                            result,
                            context);

                        context.PreparingGroupStrategy = transit
                            ? new DisableRecalcPreparingGroupRecalcStrategy()
                            : null;

                        return new NextAction
                        {
                            ContinueToNextStage = true,
                            AfterTransition = transit,
                        };
                    }
                case StageHandlerAction.GroupTransition:
                    {
                        if (result.TransitionID is null)
                        {
                            throw new InvalidOperationException(
                                $"Result with action = {StageHandlerAction.GroupTransition} " +
                                "lacks transitionID (stage group)");
                        }

                        context.CurrentStage = await this.RunAfterStageAsync(
                            context.CurrentStage,
                            context);

                        var transit = await this.TransitToStageGroupAsync(
                            context.CurrentStage,
                            result,
                            context);

                        return new NextAction
                        {
                            ContinueToNextStage = true,
                            AfterGroupTransition = transit,
                        };
                    }
                case StageHandlerAction.NextGroupTransition:
                    {
                        var nextGroupIndex = TransitionHelper.TransitToNextGroup(
                            context.WorkflowProcess.Stages,
                            context.CurrentStage.StageGroupID);

                        if (nextGroupIndex == TransitionHelper.NotFound)
                        {
                            return this.StopEntireProcess(
                                result.Action,
                                context.CurrentStage,
                                context);
                        }

                        context.CurrentStage = await this.RunAfterStageAsync(
                            context.CurrentStage,
                            context);

                        var transit = TransitByIndex(
                            context.CurrentStage,
                            nextGroupIndex,
                            result.KeepStageStates ?? false,
                            context);

                        context.PreparingGroupStrategy = new ForwardPreparingGroupRecalcStrategy(
                            this.DbScope,
                            this.Session);

                        return new NextAction
                        {
                            ContinueToNextStage = true,
                            AfterGroupTransition = transit,
                        };
                    }
                case StageHandlerAction.PreviousGroupTransition:
                    {
                        context.CurrentStage = await this.RunAfterStageAsync(
                            context.CurrentStage,
                            context);

                        var prevGroupIndex = TransitionHelper.TransitToPreviousGroup(
                            context.WorkflowProcess.Stages,
                            context.CurrentStage.StageGroupID);

                        if (prevGroupIndex == TransitionHelper.NotFound)
                        {
                            return TransitToCurrentGroup(
                                context.CurrentStage,
                                result,
                                context);
                        }

                        var transit = TransitByIndex(
                            context.CurrentStage,
                            prevGroupIndex,
                            result.KeepStageStates ?? false,
                            context);

                        context.PreparingGroupStrategy = new BackwardPreparingGroupRecalcStrategy(
                            this.DbScope,
                            this.Session);

                        return new NextAction
                        {
                            ContinueToNextStage = true,
                            AfterGroupTransition = transit,
                        };
                    }
                case StageHandlerAction.CurrentGroupTransition:
                    context.CurrentStage = await this.RunAfterStageAsync(
                        context.CurrentStage,
                        context);

                    return TransitToCurrentGroup(
                        context.CurrentStage,
                        result,
                        context);
                case StageHandlerAction.SkipProcess:
                case StageHandlerAction.CancelProcess:
                    return this.StopEntireProcess(
                        result.Action,
                        context.CurrentStage,
                        context);
                case StageHandlerAction.None when !context.ValidationResult.IsSuccessful():
                    return new NextAction();
                default:
                    throw new InvalidOperationException($"Stage {context.CurrentStage.Name} ({context.CurrentStage.RowID:B}), template {context.CurrentStage.TemplateName} ({context.CurrentStage.TemplateID:B}) returns prohibited result {result.Action:G}.");
            }

            static NextAction TransitToCurrentGroup(
                Stage stage,
                StageHandlerResult result,
                IKrProcessRunnerContext context)
            {
                var currGroupIndex = TransitionHelper.TransitToStageGroup(
                    context.WorkflowProcess.Stages,
                    stage.StageGroupID);

                if (currGroupIndex == TransitionHelper.NotFound)
                {
                    stage.State = KrStageState.Skipped;
                    return new NextAction
                    {
                        ContinueToNextStage = true,
                    };
                }

                var transit = TransitByIndex(
                    stage,
                    currGroupIndex,
                    result.KeepStageStates ?? false,
                    context);

                context.PreparingGroupStrategy = new CurrentPreparingGroupRecalcStrategy();

                return new NextAction
                {
                    ContinueToNextStage = true,
                    AfterGroupTransition = transit,
                };
            }
        }

        /// <summary>
        /// Корректирует при необходимости результат выполнения обработчика этапа.
        /// </summary>
        /// <param name="result"><inheritdoc cref="StageHandlerResult" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <returns><inheritdoc cref="StageHandlerResult" path="/summary"/></returns>
        protected virtual async ValueTask<StageHandlerResult> CoerceStageHandlerResultAsync(
            StageHandlerResult result,
            IKrProcessRunnerContext context)
        {
            if (context.ProcessInfo?.ProcessTypeName != KrConstants.KrProcessName
                || !context.CardID.HasValue
                || !this.Scope.CardIsLoaded(context.CardID.Value))
            {
                return result;
            }

            var mainCard = await this.Scope.GetMainCardAsync(
                context.CardID.Value,
                cancellationToken: context.CancellationToken);

            if (mainCard is null)
            {
                return StageHandlerResult.EmptyResult;
            }

            var workflowQueueItems = mainCard
                .TryGetWorkflowQueue()
                ?.TryGetItems();

            if (workflowQueueItems?.Count > 0)
            {
                foreach (var workflowQueueItem in workflowQueueItems)
                {
                    if (workflowQueueItem.Handled
                        || workflowQueueItem.TryGetSignal() is not { ProcessTypeName: KrConstants.KrProcessName } signal)
                    {
                        continue;
                    }

                    switch (signal.Name)
                    {
                        case KrConstants.KrCancelProcessGlobalSignal:
                            return StageHandlerResult.CancelProcessResult;
                        case KrConstants.KrSkipProcessGlobalSignal:
                            return StageHandlerResult.SkipProcessResult;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Обрабатывает процесс, информация о котором задана в контексте.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <returns><inheritdoc cref="Task" path="/summary"/></returns>
        protected async Task RunInternalAsync(IKrProcessRunnerContext context)
        {
            ThrowIfNull(context);

            context.PreviousStage = null;
            context.CurrentStage = GetCurrentStage(context);

            if (context.CurrentStage is null)
            {
                var stages = context.WorkflowProcess.Stages;
                var stagesRowIDStr = stages.Count > 0
                    ? string.Join(", ", stages.Select(i => i.RowID.ToString("B")))
                    : FormattingHelper.EmptyText;

                context.ValidationResult.AddError(this, $"The first stage of the route is not defined. {nameof(WorkflowProcess)}.{nameof(WorkflowProcess.CurrentApprovalStageRowID)} = {context.WorkflowProcess.CurrentApprovalStageRowID:B}, {nameof(WorkflowProcess)}.{nameof(WorkflowProcess.Stages)} ({nameof(Stage)}.{nameof(Stage.RowID)}): {stagesRowIDStr}.");
                return;
            }

            var stateHandlerChangedStage = false;
            var globalSignalHandled = false;

            if (context.WorkflowProcess.CurrentApprovalStageRowID.HasValue)
            {
                // Обрабатываем состояния Runner-а
                var stateResult = await this.ProcessStateAsync(context);
                ThrowIfNull(context.PreviousStage);

                // Важный нюанс:
                // При наличии состояния, отличного дефолтного, устанавливается режим один запуск за реквест (Scope.DisableMultirunPerRequest)
                // Это позволяет обрабатывать состояния процесса как машину состояний.
                // Однако, для это может препятствовать обработки нескольких сигналов за один запрос,
                // Поэтому не рекомендуется за один раз раннеру отправлять больше одного сигнала (старт процесса + сигнал допустимо)
                if (stateResult.Handled
                    && !stateResult.ContinueCurrentRun)
                {
                    return;
                }

                stateHandlerChangedStage = stateResult.ForceStartGroup
                    || context.CurrentStage?.RowID != context.PreviousStage.RowID;

                if (context.CurrentStage?.StageGroupID != context.PreviousStage.StageGroupID
                    || stateResult.ForceStartGroup)
                {
                    var tempStage = await this.TryStartStageGroupAsync(
                        context.CurrentStage,
                        context.PreviousStage,
                        context);

                    context.PreviousStage = context.CurrentStage;
                    context.CurrentStage = tempStage;
                }

                if (context.CurrentStage is null)
                {
                    return;
                }

                // Сначала слово дается глобальным сигналам, которые могут перевернуть весь процесс.
                var signalResult = await this.ProcessGlobalSignalsAsync(
                    context);
                ThrowIfNull(context.PreviousStage);

                if (signalResult.Handled
                    && !signalResult.ContinueCurrentRun)
                {
                    // Глобальный сигнал перехвачен и обработан.
                    // При этом этап не изменился и нам нечего сказать этапу
                    // Или текущий процесс завершен вообще.
                    return;
                }

                globalSignalHandled = signalResult.Handled;

                if (context.CurrentStage?.StageGroupID != context.PreviousStage.StageGroupID
                    || signalResult.ForceStartGroup)
                {
                    var tempStage = await this.TryStartStageGroupAsync(
                        context.CurrentStage,
                        context.PreviousStage,
                        context);

                    context.PreviousStage = context.CurrentStage;
                    context.CurrentStage = tempStage;
                }

                if (context.CurrentStage is null)
                {
                    return;
                }
            }
            else
            {
                context.WorkflowProcess.CurrentApprovalStageRowID = context.CurrentStage.RowID;

                var tempStage = await this.TryStartStageGroupAsync(
                    context.CurrentStage,
                    context.PreviousStage,
                    context);

                context.PreviousStage = context.CurrentStage;
                context.CurrentStage = tempStage;

                if (context.CurrentStage is null)
                {
                    // Группа этапов так сильно запустилась, что этапы кончились
                    return;
                }

                if (context.CardID.HasValue
                    && context.CardContext is ICardStoreExtensionContext storeContext)
                {
                    // Для процесса с сигналами при старте проверяем, есть ли запланированные сигналы.
                    // Проверять достаточно только для основного процесса, т.к. только он поддерживает получение сигналов.
                    var hasUnhandledSignals = storeContext
                        .Request
                        .Card
                        .TryGetWorkflowQueue()
                        ?.Items
                        ?.Any(static p => !p.Handled
                            && p.Signal.ProcessTypeName == KrConstants.KrProcessName);

                    if (hasUnhandledSignals == true)
                    {
                        // Процессу еще перед запуском дали сигналы.
                        // Такие сигналы должны быть выполнены вместо старта этапа.
                        return;
                    }
                }
            }

            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            var result = await this.ProcessCurrentStageAsync(
                context,
                globalSignalHandled
                || stateHandlerChangedStage);

            this.AddTrace(
                context,
                context.CurrentStage,
                result);

            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            var nextAction = await this.ProcessStageHandlerResultAsync(
                result,
                context);

            while (nextAction.ContinueToNextStage)
            {
                if (!context.ValidationResult.IsSuccessful())
                {
                    return;
                }

                var afterTransition = nextAction.AfterTransition
                    || nextAction.AfterGroupTransition;

                var tempStage0 = afterTransition
                    ? GetCurrentStage(context)
                    : GetNextStage(context);

                context.PreviousStage = context.CurrentStage;
                context.CurrentStage = tempStage0;

                if (context.CurrentStage is not null)
                {
                    await this.IntermediateApplyChangesAsync(context);

                    if (!context.ValidationResult.IsSuccessful())
                    {
                        return;
                    }

                    if (context.CurrentStage.StageGroupID != context.PreviousStage!.StageGroupID
                        || nextAction.AfterGroupTransition)
                    {
                        await this.RunAfterStageGroupAsync(
                            context.PreviousStage,
                            context);

                        var tempStage = await this.TryStartStageGroupAsync(
                            context.CurrentStage,
                            context.PreviousStage,
                            context);

                        context.PreviousStage = context.CurrentStage;
                        context.CurrentStage = tempStage;

                        if (context.CurrentStage is not null)
                        {
                            nextAction = await this.RunNextStageAsync(context);
                        }
                    }
                    else
                    {
                        nextAction = await this.RunNextStageAsync(context);
                    }
                }

                // За верхний if (currentStage is not null) сам currentStage мог изменится
                if (context.CurrentStage is null)
                {
                    if (!context.ValidationResult.IsSuccessful())
                    {
                        return;
                    }

                    await this.RunAfterStageGroupAsync(context.PreviousStage!, context);

                    // Попытка пересчитать конец маршрута. Он может содержать группы, которые ранее не удовлетворяли условиям.
                    var tempStage = await this.TryStartStageGroupAsync(
                        context.CurrentStage,
                        context.PreviousStage,
                        context);

                    context.PreviousStage = context.CurrentStage;
                    context.CurrentStage = tempStage;

                    if (context.CurrentStage is null)
                    {
                        // Больше групп нет. Завершаем процесс.
                        context.WorkflowProcess.CurrentApprovalStageRowID = null;
                        nextAction = new NextAction();
                    }
                    else
                    {
                        await this.IntermediateApplyChangesAsync(context);

                        if (!context.ValidationResult.IsSuccessful())
                        {
                            return;
                        }

                        nextAction = await this.RunNextStageAsync(context);
                    }
                }
            }
        }

        protected async Task<NextAction> RunNextStageAsync(
            IKrProcessRunnerContext context)
        {
            ThrowIfNull(context);
            ThrowIfNull(context.CurrentStage);

            context.WorkflowProcess.CurrentApprovalStageRowID = context.CurrentStage.RowID;

            var result = await this.ProcessCurrentStageAsync(
                context,
                true);

            this.AddTrace(
                context,
                context.CurrentStage,
                result);

            return await this.ProcessStageHandlerResultAsync(
                result,
                context);
        }

        protected async Task<IStateHandlerResult> ProcessStateAsync(
            IKrProcessRunnerContext context)
        {
            ThrowIfNull(context);
            ThrowIfNull(context.CurrentStage);

            if (this.RunnerMode == KrProcessRunnerMode.Sync)
            {
                // Т.к. текущим этапом становится context.CurrentStage, а он и был изначально, то:
                context.PreviousStage = context.CurrentStage;
                return StateHandlerResult.EmptyResult;
            }

            var stateContext = new StateHandlerContext(
                this.Scope.GetRunnerState(context.ProcessInfo!.ProcessID),
                context.CurrentStage,
                this.RunnerMode,
                context);

            if (stateContext.State != KrProcessState.Default)
            {
                this.Scope.DisableMultirunForRequest(context.ProcessInfo.ProcessID);
            }

            this.Scope.SetDefaultState(context.ProcessInfo.ProcessID);

            var stateResult = await this.StateMachine.HandleStateAsync(stateContext);
            context.CurrentStage = NotNullOrThrow(stateContext.Stage);

            var lastHandledResult = stateResult;

            var newStage = GetCurrentStage(context, isGetFirstStage: false);

            var isForceStartGroup = false;

            // Выполняем состояния, пока они обрабатываются и не требуют вложенного сохранения
            while (stateResult.Handled
                && stateResult.ContinueCurrentRun)
            {
                lastHandledResult = stateResult;

                stateContext = new StateHandlerContext(
                    this.Scope.GetRunnerState(context.ProcessInfo.ProcessID),
                    newStage,
                    this.RunnerMode,
                    context);

                this.Scope.SetDefaultState(context.ProcessInfo.ProcessID);
                stateResult = await this.StateMachine.HandleStateAsync(stateContext);

                isForceStartGroup |= stateResult.ForceStartGroup;

                newStage = GetCurrentStage(context, isGetFirstStage: false);
            }

            context.PreviousStage = context.CurrentStage;
            context.CurrentStage = newStage;

            // Обработчик состояния сменил этап
            // или был выполнен переход в начало текущей группы этапов без смены активного этапа?
            if (context.PreviousStage!.RowID != context.CurrentStage?.RowID
                || isForceStartGroup)
            {
                context.PreviousStage = await this.RunAfterStageAsync(context.PreviousStage, context);

                // Нет этапов для выполнения
                // или была изменена группа
                // или был выполнен переход в начало текущей группы этапов без смены активного этапа?
                if (context.CurrentStage is null
                    || context.CurrentStage.StageGroupID != context.PreviousStage.StageGroupID
                    || isForceStartGroup)
                {
                    await this.RunAfterStageGroupAsync(context.PreviousStage, context);
                }
            }

            return lastHandledResult;
        }

        protected async Task<IGlobalSignalHandlerResult> ProcessGlobalSignalsAsync(
            IKrProcessRunnerContext context)
        {
            ThrowIfNull(context);
            ThrowIfNull(context.CurrentStage);

            var signalInfo = context.SignalInfo;

            if (signalInfo is null)
            {
                // Т.к. текущим этапом становится context.CurrentStage, а он и был изначально, то:
                context.PreviousStage = context.CurrentStage;
                return GlobalSignalHandlerResult.EmptyHandlerResult;
            }

            var aggregateHandlerResult = GlobalSignalHandlerResult.EmptyHandlerResult;
            var handlers = this.ProcessContainer.ResolveSignal(signalInfo.Signal.Name);

            if (handlers?.Count > 0)
            {
                var ctx = new GlobalSignalHandlerContext(
                    context.CurrentStage,
                    context,
                    this.RunnerMode);

                var anyHandled = false;
                var allContinue = true;
                var forceStartGroup = false;

                foreach (var handler in handlers)
                {
                    var res = await handler.Handle(ctx);
                    context.CurrentStage = ctx.Stage;

                    anyHandled |= res.Handled;
                    allContinue &= res.ContinueCurrentRun;
                    forceStartGroup |= res.ForceStartGroup;
                }

                aggregateHandlerResult = new GlobalSignalHandlerResult(
                    anyHandled,
                    allContinue,
                    forceStartGroup);
            }

            if (context.CurrentStage.RowID == context.WorkflowProcess.CurrentApprovalStageRowID
                && !aggregateHandlerResult.ForceStartGroup)
            {
                // Т.к. текущим этапом становится context.CurrentStage, а он и был изначально, то:
                context.PreviousStage = context.CurrentStage;
                return aggregateHandlerResult;
            }

            context.CurrentStage = await this.RunAfterStageAsync(context.CurrentStage, context);

            var newStage = GetCurrentStage(
                context,
                isGetFirstStage: false);

            context.PreviousStage = context.CurrentStage;
            context.CurrentStage = newStage;

            if (context.CurrentStage?.StageGroupID != context.PreviousStage.StageGroupID
                || aggregateHandlerResult.ForceStartGroup)
            {
                await this.RunAfterStageGroupAsync(context.PreviousStage, context);
            }

            return aggregateHandlerResult;
        }

        /// <summary>
        /// Возвращает текущий активный этап процесса.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <param name="isGetFirstStage">Значение <see langword="true"/>, если следует вернуть первый доступный этап, если не удалось получить текущий активный этап по <see cref="WorkflowProcess.CurrentApprovalStageRowID"/>, иначе <see langword="false"/>.</param>
        /// <returns>Текущий активный этап процесса или <see langword="null"/>, если его не удалось получить.</returns>
        protected static Stage? GetCurrentStage(
            IKrProcessRunnerContext context,
            bool isGetFirstStage = true)
        {
            ThrowIfNull(context);

            var currentApprovalStageRowID = context.WorkflowProcess.CurrentApprovalStageRowID;

            if (currentApprovalStageRowID.HasValue)
            {
                return context.WorkflowProcess.Stages.FirstOrDefault(p => p.RowID == currentApprovalStageRowID);
            }

            if (isGetFirstStage && context.WorkflowProcess.Stages.Count > 0)
            {
                return context.WorkflowProcess.Stages[0];
            }

            return null;
        }

        protected async Task<StageHandlerResult> ProcessCurrentStageAsync(
            IKrProcessRunnerContext context,
            bool forceStartProcess)
        {
            ThrowIfNull(context);

            if (context.CurrentStage is null)
            {
                return StageHandlerResult.SkipResult;
            }

            if (!context.CurrentStage.StageTypeID.HasValue)
            {
                KrErrorHelper.WarnStageTypeIsNull(
                    context.ValidationResult,
                    context.CurrentStage);

                return StageHandlerResult.SkipResult;
            }

            var handler = this.ProcessContainer.ResolveHandler(
                context.CurrentStage.StageTypeID.Value);

            if (handler is null)
            {
                KrErrorHelper.WarnStageHandlerIsNull(
                    context.ValidationResult,
                    context.CurrentStage);

                return StageHandlerResult.SkipResult;
            }

            this.AssertRunnerMode(context.CurrentStage.StageTypeID.Value);

            var stageContext = new StageTypeHandlerContext(
                context,
                context.CurrentStage,
                this.RunnerMode,
                null);

            StageHandlerResult result;

            if (forceStartProcess)
            {
                result = await this.TryStartStageAsync(
                    handler,
                    stageContext,
                    context);
            }
            else
            {
                result = await (context.InitiationCause switch
                {
                    KrProcessRunnerInitiationCause.StartProcess => this.TryStartStageAsync(
                        handler,
                        stageContext,
                        context),
                    KrProcessRunnerInitiationCause.CompleteTask => handler.HandleTaskCompletionAsync(stageContext),
                    KrProcessRunnerInitiationCause.ReinstateTask => handler.HandleTaskReinstateAsync(stageContext),
                    KrProcessRunnerInitiationCause.Signal => handler.HandleSignalAsync(stageContext),
                    KrProcessRunnerInitiationCause.InMemoryLaunching => this.TryStartStageAsync(
                        handler,
                        stageContext,
                        context),
                    KrProcessRunnerInitiationCause.Resurrection => TryResurrectStage(
                        handler,
                        stageContext),
                    _ => throw ArgumentOutOfRange(context.InitiationCause),
                });
            }

            context.CurrentStage = stageContext.Stage;

            return result;
        }

        protected static Stage? GetNextStage(IKrProcessRunnerContext context)
        {
            var currentApprovalStageRowID = context.WorkflowProcess.CurrentApprovalStageRowID;
            if (currentApprovalStageRowID is null)
            {
                // Если нет текущего этапа, то и следующего за ним быть не может
                return null;
            }

            var currentStageIndex = context.WorkflowProcess.Stages.IndexOf(p => p.RowID == currentApprovalStageRowID);
            if (currentStageIndex == -1)
            {
                // Если нет текущего этапа, то и следующего за ним быть не может
                return null;
            }

            return currentStageIndex + 1 < context.WorkflowProcess.Stages.Count
                ? context.WorkflowProcess.Stages[currentStageIndex + 1]
                : null;
        }

        protected async ValueTask<IKrExecutionUnit?> CreateRuntimeStageInstanceAsync(
            Stage currentStage,
            IKrProcessRunnerContext context)
        {
            var cachedExecutionUnits = context.ExecutionUnitCache;

            IKrExecutionUnit unit;
            IKrScript? instance;

            if (cachedExecutionUnits.TryGetValue(currentStage.RowID, out var cached))
            {
                // Взятие объекта из кэша с проинициализированными общими зависимостями
                unit = cached;
                instance = unit.Instance;
            }
            else
            {
                if (!currentStage.TemplateID.HasValue)
                {
                    return null;
                }

                // Создание нового объекта с инициализацией общих зависимостей
                if (!(await this.ProcessCache.GetAllRuntimeStagesAsync(context.CancellationToken)).TryGetValue(currentStage.ID, out var runtimeStage))
                {
                    return null;
                }

                var compilationObject = await this.StageTemplateCompilationCache.GetAsync(
                    currentStage.TemplateID.Value,
                    cancellationToken: context.CancellationToken);

                instance = compilationObject.TryCreateKrScriptInstance(
                    KrCompilersHelper.FormatClassName(
                        SourceIdentifiers.KrRuntimeClass,
                        SourceIdentifiers.StageAlias,
                        currentStage.ID),
                    context.ValidationResult);

                if (!context.ValidationResult.IsSuccessful())
                {
                    return null;
                }

                // Создание KrScript необходимо для передачи данных через KrExecutionUnit.Instance.
                instance ??= new KrScript();
                unit = new KrExecutionUnit(runtimeStage, instance);
                cachedExecutionUnits[currentStage.RowID] = unit;

                instance.MainCardAccessStrategy = context.MainCardAccessStrategy;
                instance.CardID = context.CardID ?? Guid.Empty;
                instance.CardType = context.CardType;
                instance.DocTypeID = context.DocTypeID ?? Guid.Empty;
                if (context.KrComponents.HasValue)
                {
                    instance.KrComponents = context.KrComponents.Value;
                }

                instance.WorkflowProcessInfo = context.ProcessInfo;
                instance.ProcessID = context.ProcessInfo?.ProcessID;
                instance.ProcessTypeName = context.ProcessInfo?.ProcessTypeName;
                instance.InitiationCause = context.InitiationCause;
                instance.SetContextualSatellite(context.ContextualSatellite);
                instance.ProcessHolderSatellite = context.ProcessHolderSatellite;
                instance.SecondaryProcess = context.SecondaryProcess;
                instance.CardContext = context.CardContext;
                instance.ValidationResult = context.ValidationResult;
                instance.TaskHistoryResolver = context.TaskHistoryResolver;
                instance.Session = this.Session;
                instance.DbScope = this.DbScope;
                instance.UnityContainer = this.UnityContainer;
                instance.CardMetadata = this.CardMetadata;
                instance.KrScope = this.Scope;
                instance.CardCache = this.CardCache;
                instance.KrTypesCache = this.TypesCache;
                instance.StageSerializer = this.StageSerializer;
                instance.CancellationToken = context.CancellationToken;
            }

            if (!currentStage.TemplateID.HasValue
                || !(await this.ProcessCache.GetAllStageTemplatesAsync(context.CancellationToken)).TryGetValue(currentStage.TemplateID.Value, out var stageTemplate))
            {
                return null;
            }

            // Информация о конкретных этапах и шаблонах не является общей,
            // поэтому инициализируется всегда
            instance.StageGroupID = currentStage.StageGroupID;
            instance.StageGroupName = currentStage.StageGroupName;
            instance.StageGroupOrder = currentStage.StageGroupOrder;
            instance.TemplateID = currentStage.TemplateID.Value;
            instance.TemplateName = currentStage.TemplateName;
            instance.Order = stageTemplate?.Order ?? -1;
            instance.Position = stageTemplate?.Position ?? GroupPosition.Unspecified;
            instance.CanChangeOrder = stageTemplate?.CanChangeOrder ?? true;
            instance.IsStagesReadonly = stageTemplate?.IsStagesReadonly ?? true;

            // На данном этапе нет контейнера, способного пересчитывать положения этапов.
            instance.StagesContainer = null;
            instance.WorkflowProcess = context.WorkflowProcess;
            instance.Stage = currentStage;

            // Необходимо сбросить информацию о переключении контекста
            instance.DifferentContextCardID = null;
            instance.DifferentContextWholeCurrentGroup = false;
            instance.DifferentContextProcessInfo = null;
            instance.DifferentContextSetupScriptType = null;

            instance.CancellationToken = context.CancellationToken;

            return unit;
        }

        protected async ValueTask<IKrExecutionUnit?> CreateStageGroupInstanceAsync(
            Stage stage,
            IKrProcessRunnerContext context)
        {
            ThrowIfNull(stage);
            ThrowIfNull(context);

            var groups = await this.ProcessCache.GetAllStageGroupsAsync(context.CancellationToken);

            if (!groups.TryGetValue(stage.StageGroupID, out var stageGroup))
            {
                return null;
            }

            var cachedExecutionUnits = context.ExecutionUnitCache;

            IKrExecutionUnit unit;
            IKrScript? instance;

            if (cachedExecutionUnits.TryGetValue(stageGroup.ID, out var cached))
            {
                // Взятие объекта из кэша с проинициализированными общими зависимостями
                unit = cached;
                instance = unit.Instance;
            }
            else
            {
                var compilationObject = await this.StageGroupCompilationCache.GetAsync(
                    stageGroup.ID,
                    cancellationToken: context.CancellationToken);

                instance = compilationObject.TryCreateKrScriptInstance(
                    KrCompilersHelper.FormatClassName(
                        SourceIdentifiers.KrRuntimeClass,
                        SourceIdentifiers.GroupAlias,
                        stageGroup.ID),
                    context.ValidationResult);

                if (!context.ValidationResult.IsSuccessful())
                {
                    return null;
                }

                // Создание KrScript необходимо для передачи данных через KrExecutionUnit.Instance.
                instance ??= new KrScript();
                unit = new KrExecutionUnit(stageGroup, instance);
                cachedExecutionUnits[stageGroup.ID] = unit;

                instance.MainCardAccessStrategy = context.MainCardAccessStrategy;
                instance.CardID = context.CardID ?? Guid.Empty;
                instance.CardType = context.CardType;
                instance.DocTypeID = context.DocTypeID ?? Guid.Empty;

                if (context.KrComponents.HasValue)
                {
                    instance.KrComponents = context.KrComponents.Value;
                }

                instance.WorkflowProcessInfo = context.ProcessInfo;

                instance.ProcessID = context.ProcessInfo?.ProcessID;
                instance.ProcessTypeName = context.ProcessInfo?.ProcessTypeName;
                instance.InitiationCause = context.InitiationCause;
                instance.SetContextualSatellite(context.ContextualSatellite);
                instance.ProcessHolderSatellite = context.ProcessHolderSatellite;
                instance.SecondaryProcess = context.SecondaryProcess;
                instance.CardContext = context.CardContext;
                instance.ValidationResult = context.ValidationResult;
                instance.TaskHistoryResolver = context.TaskHistoryResolver;
                instance.Session = this.Session;
                instance.DbScope = this.DbScope;
                instance.UnityContainer = this.UnityContainer;
                instance.CardMetadata = this.CardMetadata;
                instance.KrScope = this.Scope;
            }

            instance.StageGroupID = stageGroup.ID;
            instance.StageGroupName = stageGroup.Name;
            instance.StageGroupOrder = stageGroup.Order;

            // На данном этапе нет контейнера, способного пересчитывать положения этапов.
            instance.StagesContainer = null;
            instance.WorkflowProcess = context.WorkflowProcess;

            instance.DifferentContextCardID = null;
            instance.DifferentContextWholeCurrentGroup = false;
            instance.DifferentContextProcessInfo = null;
            instance.DifferentContextSetupScriptType = null;

            instance.CancellationToken = context.CancellationToken;

            return unit;
        }

        protected async Task<StageHandlerResult> TryStartStageAsync(
            IStageTypeHandler handler,
            IStageTypeHandlerContext handlerContext,
            IKrProcessRunnerContext context)
        {
            if (handlerContext.Stage.StageTypeID is null)
            {
                throw new ProcessRunnerInterruptedException();
            }

            var descriptor = this.ProcessContainer.GetHandlerDescriptor(
                handlerContext.Stage.StageTypeID.Value);

            // На старте удаляем старые переопределения
            HandlerHelper.RemoveTaskHistoryGroupOverride(handlerContext.Stage);
            context.PreparingGroupStrategy = null;

            await handler.BeforeInitializationAsync(handlerContext);

            if (!context.ValidationResult.IsSuccessful())
            {
                return StageHandlerResult.EmptyResult;
            }

            handlerContext.Stage.State = KrStageState.Active;

            if (!handlerContext.Stage.BasedOnTemplateStage)
            {
                if (handlerContext.Stage.Skip)
                {
                    context.SkippedStagesByCondition.Add(handlerContext.Stage.ID);
                    return StageHandlerResult.SkipResult;
                }

                CheckTime(handlerContext.Stage, descriptor);
                CheckPerformers(handlerContext.Stage, descriptor);
                return await handler.HandleStageStartAsync(handlerContext);
            }

            var unit = await this.CreateRuntimeStageInstanceAsync(
                handlerContext.Stage,
                context);

            if (unit is null)
            {
                if (handlerContext.Stage.Skip)
                {
                    context.SkippedStagesByCondition.Add(handlerContext.Stage.ID);
                    return StageHandlerResult.SkipResult;
                }

                // Не получилось создать элемент выполнения этапа.
                // Скорее всего, какой-то связанный элемент удален, поэтому просто выполним хендлер.
                CheckTime(handlerContext.Stage, descriptor);
                CheckPerformers(handlerContext.Stage, descriptor);
                return await handler.HandleStageStartAsync(handlerContext);
            }

            // Переносим переключение контекста из After предыдущего этапа.
            var changeContext = false;

            if (handlerContext.Stage.ChangeContextToCardID.HasValue)
            {
                unit.Instance.DifferentContextCardID = handlerContext.Stage.ChangeContextToCardID.Value;
                unit.Instance.DifferentContextWholeCurrentGroup = handlerContext.Stage.ChangeContextWholeGroupToDifferentCard;
                unit.Instance.DifferentContextProcessInfo = handlerContext.Stage.ChangeContextProcessInfo;
                unit.Instance.DifferentContextSetupScriptType = KrScriptType.Before;

                // При этом необходима очистка этапа.
                handlerContext.Stage.ChangeContextToCardID = null;
                handlerContext.Stage.ChangeContextWholeGroupToDifferentCard = false;
                handlerContext.Stage.ChangeContextProcessInfo = null;
                changeContext = true;
            }

            if (!changeContext)
            {
                var condition = !handlerContext.Stage.Skip
                    && await this.SafeRunAsync(
                        context,
                        async u => await this.RunConditionsAsync(u, context),
                        unit);

                if (!condition)
                {
                    context.SkippedStagesByCondition.Add(handlerContext.Stage.ID);
                    return StageHandlerResult.SkipResult;
                }

                await this.RecalcSqlRolesAsync(handlerContext.Stage, context);
                await this.SafeRunAsync(context, RunBeforeAsync, unit);

                changeContext = unit.Instance.DifferentContextCardID.HasValue
                    && unit.Instance.DifferentContextCardID != context.CardID;
            }

            // Место подмены контекста.
            if (changeContext)
            {
                var nextGroupID = Guid.Empty;
                var stageSubset = unit.Instance.DifferentContextWholeCurrentGroup
                    ? GetSubsequentStages(context, handlerContext.Stage, out nextGroupID)
                    : [new Stage(handlerContext.Stage)];

                await this.RunInDifferentCardContextAsync(
                    context,
                    unit.Instance.DifferentContextCardID!.Value,
                    stageSubset,
                    unit.Instance.DifferentContextProcessInfo);

                if (stageSubset.Count <= 1)
                {
                    return StageHandlerResult.SkipResult;
                }

                if (nextGroupID != Guid.Empty)
                {
                    return StageHandlerResult.GroupTransition(nextGroupID);
                }

                return StageHandlerResult.SkipProcessResult;
            }

            CheckTime(handlerContext.Stage, descriptor);
            CheckPerformers(handlerContext.Stage, descriptor);

            if (!context.ValidationResult.IsSuccessful())
            {
                return StageHandlerResult.EmptyResult;
            }

            return await handler.HandleStageStartAsync(handlerContext);
        }

        protected static Task<StageHandlerResult> TryResurrectStage(
            IStageTypeHandler handler,
            IStageTypeHandlerContext handlerContext) =>
            handler.HandleResurrectionAsync(handlerContext);

        protected async Task<Stage> RunAfterStageAsync(
            Stage stage,
            IKrProcessRunnerContext context)
        {
            ThrowIfNull(stage);
            ThrowIfNull(context);

            if (context.SkippedStagesByCondition.Contains(stage.ID))
            {
                return stage;
            }

            if (stage.BasedOnTemplateStage)
            {
                var unit = await this.CreateRuntimeStageInstanceAsync(stage, context);

                if (unit is null)
                {
                    // Не получилось создать элемент выполнения этапа.
                    // Пропускаем весь этап.
                    return stage;
                }

                await this.SafeRunAsync(context, RunAfterAsync, unit);

                if (unit.Instance.DifferentContextCardID.HasValue
                    && unit.Instance.DifferentContextSetupScriptType == KrScriptType.After)
                {
                    var nextStage = GetNextStage(context);
                    if (nextStage is not null)
                    {
                        nextStage.ChangeContextToCardID = unit.Instance.DifferentContextCardID;
                        nextStage.ChangeContextWholeGroupToDifferentCard = unit.Instance.DifferentContextWholeCurrentGroup;
                        nextStage.ChangeContextProcessInfo = unit.Instance.DifferentContextProcessInfo;
                    }
                }
            }

            if (stage.StageTypeID.HasValue)
            {
                var handler = this.ProcessContainer.ResolveHandler(stage.StageTypeID.Value);
                if (handler is not null)
                {
                    var stageContext = new StageTypeHandlerContext(
                        context,
                        stage,
                        this.RunnerMode,
                        null);
                    await handler.AfterPostprocessingAsync(stageContext);
                    stage = stageContext.Stage;
                }
            }

            return stage;
        }

        /// <summary>
        /// Возвращает следующий этап.
        /// </summary>
        /// <param name="stage">Текущий этап. Может быть не задан.</param>
        /// <param name="previousStage">Предыдущий этап. Может быть не задан.</param>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <returns>Следующий этап или <see langword="null"/>, если он не найден.</returns>
        protected async Task<Stage?> TryStartStageGroupAsync(
            Stage? stage,
            Stage? previousStage,
            IKrProcessRunnerContext context)
        {
            ThrowIfNull(context);

            if (context.IgnoreGroupScripts)
            {
                return stage;
            }

            var preparingStrategy = context.PreparingGroupStrategy
                ?? context.DefaultPreparingGroupStrategyFunc?.Invoke()
                ?? throw new InvalidOperationException(nameof(IKrProcessRunnerContext) + "." + nameof(IKrProcessRunnerContext.DefaultPreparingGroupStrategyFunc) + " isn't set.");

            context.PreparingGroupStrategy = null;
            await preparingStrategy.ApplyAsync(context, stage, previousStage);

            if (preparingStrategy.ExecutionUnits.Count > 0)
            {
                await this.PartialGroupRecalcAsync(
                    new HashSet<Guid>(preparingStrategy.ExecutionUnits),
                    context);
            }

            var nextStage = preparingStrategy.GetSuitableStage(context.WorkflowProcess.Stages);
            if (nextStage is null)
            {
                context.WorkflowProcess.CurrentApprovalStageRowID = null;
                return null;
            }

            var unit = await this.CreateStageGroupInstanceAsync(nextStage, context);

            context.WorkflowProcess.CurrentApprovalStageRowID = nextStage.RowID;

            if (unit is null && !nextStage.BasedOnTemplate)
            {
                return nextStage;
            }

            if (await this.SafeRunAsync(context, async u => await this.RunConditionsAsync(u, context), unit))
            {
                await this.SafeRunAsync(context, RunBeforeAsync, unit);
                return nextStage;
            }

            // Пропускаем все этапы внутри
            context.SkippedGroupsByCondition.Add(nextStage.StageGroupID);
            var stages = context.WorkflowProcess.Stages;
            var currentIdx = stages.IndexOf(p => p.RowID == nextStage.RowID);
            var count = stages.Count;

            while (currentIdx < count && stages[currentIdx].StageGroupID == nextStage.StageGroupID)
            {
                stages[currentIdx].State = KrStageState.Skipped;
                currentIdx++;
            }

            // Пометим следующий этап как текущий, чтобы с него потом стартануть
            if (currentIdx < count)
            {
                nextStage = stages[currentIdx];
                context.WorkflowProcess.CurrentApprovalStageRowID = nextStage.RowID;
                return nextStage;
            }

            context.WorkflowProcess.CurrentApprovalStageRowID = null;
            return null;
        }

        protected async Task PartialGroupRecalcAsync(
            ISet<Guid> executionUnitIDs,
            IKrProcessRunnerContext context)
        {
            ThrowIfNull(executionUnitIDs);
            ThrowIfNull(context);

            if (context.ProcessHolderSatellite is not null)
            {
                NestedStagesCleaner.ClearGroup(
                    context.ProcessHolderSatellite,
                    context.WorkflowProcess.NestedProcessID,
                    executionUnitIDs);
            }

            var ctx = new KrExecutionContext(
                cardContext: context.CardContext,
                mainCardAccessStrategy: context.MainCardAccessStrategy,
                cardID: context.CardID,
                cardType: context.CardType,
                docTypeID: context.DocTypeID,
                krComponents: context.KrComponents,
                workflowProcess: context.WorkflowProcess,
                validationResult: context.ValidationResult,
                executionUnitIDs: executionUnitIDs,
                secondaryProcess: context.SecondaryProcess,
                cancellationToken: context.CancellationToken);

            await this.Executor.ExecuteAsync(ctx);
        }

        protected async Task RunAfterStageGroupAsync(
            Stage stage,
            IKrProcessRunnerContext context)
        {
            ThrowIfNull(context);

            if (context.IgnoreGroupScripts
                || context.SkippedGroupsByCondition.Contains(stage.StageGroupID))
            {
                return;
            }

            var unit = await this.CreateStageGroupInstanceAsync(stage, context);
            await this.SafeRunAsync(context, RunAfterAsync, unit);
        }

        /// <summary>
        /// Выполняет переход к этапу. Идентификатор этапа к которой должен быть выполнен переход содержится в <see cref="StageHandlerResult.TransitionID"/>.
        /// </summary>
        /// <param name="currentStage">Текущий этап.</param>
        /// <param name="stageHandlerResult"><inheritdoc cref="StageHandlerResult" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если переход успешно выполнен, иначе - <see langword="false"/>.</returns>
        protected static bool TransitToStage(
            Stage currentStage,
            StageHandlerResult stageHandlerResult,
            IKrProcessRunnerContext context)
        {
            ThrowIfNull(currentStage);
            ThrowIfNull(context);

            var transitTo = stageHandlerResult.TransitionID;
            var keepStageStates = stageHandlerResult.KeepStageStates ?? false;
            return Transit(currentStage, keepStageStates, s => s.ID == transitTo, context);
        }

        /// <summary>
        /// Выполняет переход к группе этапов. Идентификатор группы этапов к которой должен быть выполнен переход содержится в <see cref="StageHandlerResult.TransitionID"/>.
        /// </summary>
        /// <param name="currentStage">Текущий этап.</param>
        /// <param name="stageHandlerResult"><inheritdoc cref="StageHandlerResult" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если переход успешно выполнен, иначе - <see langword="false"/>.</returns>
        protected async Task<bool> TransitToStageGroupAsync(
            Stage currentStage,
            StageHandlerResult stageHandlerResult,
            IKrProcessRunnerContext context)
        {
            var transitToStageGroupID = NotNullOrThrow(stageHandlerResult.TransitionID);
            var keepStageStates = stageHandlerResult.KeepStageStates ?? false;
            var stages = context.WorkflowProcess.Stages;
            var isPartialGroupRecalc = false;

            var stage = TransitToStage(stages, transitToStageGroupID);

            if (stage is null)
            {
                // Попытка выполнения пересчёта группы отсутствующей в маршруте.
                isPartialGroupRecalc = true;
                await this.PartialGroupRecalcAsync(
                    new HashSet<Guid>(1)
                    {
                        transitToStageGroupID,
                    },
                    context);

                stage = TransitToStage(stages, transitToStageGroupID);
            }

            var isTransit = Transit(currentStage, keepStageStates, stage, context);

            context.PreparingGroupStrategy =
                isPartialGroupRecalc
                ? new DisableRecalcPreparingGroupRecalcStrategy()
                : isTransit
                ? new ExplicitlySelectedPreparingGroupRecalcStrategy()
                : null;

            return isTransit;

            static Stage? TransitToStage(IEnumerable<Stage> stages, Guid transitToStageGroupID)
            {
                return stages.FirstOrDefault(s => s.StageGroupID == transitToStageGroupID);
            }
        }

        /// <summary>
        /// Выполняет переход к этапу с заданным порядковым индексом.
        /// </summary>
        /// <param name="currentStage">Текущий этап.</param>
        /// <param name="index">Порядковый индекс этапа к которому должен быть выполнен переход.</param>
        /// <param name="keepStageStates">Значение <see langword="true"/>, если необходимо сохранить состояние этапов, иначе - <see langword="false"/>.</param>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если переход успешно выполнен, иначе - <see langword="false"/>.</returns>
        protected static bool TransitByIndex(
            Stage currentStage,
            int index,
            bool keepStageStates,
            IKrProcessRunnerContext context)
        {
            if (!(0 <= index && index < context.WorkflowProcess.Stages.Count))
            {
                return false;
            }

            var transitTo = context.WorkflowProcess.Stages[index];
            return Transit(currentStage, keepStageStates, transitTo, context);
        }

        /// <summary>
        /// Выполняет переход к этапу удовлетворяющему заданное условие.
        /// </summary>
        /// <param name="currentStage">Текущий этап.</param>
        /// <param name="keepStageStates">Значение <see langword="true"/>, если необходимо сохранить состояние этапов, иначе - <see langword="false"/>.</param>
        /// <param name="transitionPredicate">Условие по которому определяется этап к которому выполняется переход.</param>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если переход успешно выполнен, иначе - <see langword="false"/>.</returns>
        protected static bool Transit(
            Stage currentStage,
            bool keepStageStates,
            Func<Stage, bool> transitionPredicate,
            IKrProcessRunnerContext context)
        {
            ThrowIfNull(currentStage);
            ThrowIfNull(transitionPredicate);
            ThrowIfNull(context);

            return Transit(
                currentStage,
                keepStageStates,
                context.WorkflowProcess.Stages.FirstOrDefault(transitionPredicate),
                context);
        }

        /// <summary>
        /// Выполняет переход на заданный этап.
        /// </summary>
        /// <param name="currentStage">Текущий этап.</param>
        /// <param name="keepStageStates">Значение <see langword="true"/>, если необходимо сохранить состояние этапов, иначе - <see langword="false"/>.</param>
        /// <param name="stage">Этап на который выполняется переход или <see langword="null"/>, если он не выполняется.</param>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если переход успешно выполнен, иначе - <see langword="false"/>.</returns>
        protected static bool Transit(
            Stage currentStage,
            bool keepStageStates,
            Stage? stage,
            IKrProcessRunnerContext context)
        {
            ThrowIfNull(currentStage);
            ThrowIfNull(context);

            if (stage is null)
            {
                currentStage.State = KrStageState.Skipped;
                return false;
            }

            currentStage.State = KrStageState.Completed;
            context.WorkflowProcess.CurrentApprovalStageRowID = stage.RowID;

            if (!keepStageStates)
            {
                TransitionHelper.ChangeStatesTransition(
                    context.WorkflowProcess.Stages,
                    currentStage.RowID,
                    // ReSharper disable once PossibleInvalidOperationException
                    context.WorkflowProcess.CurrentApprovalStageRowID.Value,
                    context.ProcessHolderSatellite);
            }

            return true;
        }

        protected NextAction StopEntireProcess(
            StageHandlerAction action,
            Stage currentStage,
            IKrProcessRunnerContext context)
        {
            var skip = action is StageHandlerAction.SkipProcess
                or StageHandlerAction.NextGroupTransition;

            context.WorkflowProcess.CurrentApprovalStageRowID = null;

            // В случае перехода на следующую группу дадим возможность еще досчитать следующую группу.
            context.PreparingGroupStrategy = action == StageHandlerAction.NextGroupTransition
                ? new ForwardPreparingGroupRecalcStrategy(
                    this.DbScope, 
                    this.Session)
                : new DisableRecalcPreparingGroupRecalcStrategy();

            if (skip)
            {
                TransitionHelper.SetSkipStateToSubsequentStages(
                    currentStage,
                    context.WorkflowProcess.Stages,
                    context.ProcessHolderSatellite);
            }
            else
            {
                TransitionHelper.SetInactiveStateToAllStages(
                    context.WorkflowProcess.Stages,
                    context.ProcessHolderSatellite);
            }

            // Переход к следующему этапу даст понять раннеру, что следующий этап == null и пора выходить
            return new NextAction { ContinueToNextStage = true };
        }

        /// <summary>
        /// Выполняет сценарий инициализации для единицы выполнения.
        /// </summary>
        /// <param name="unit"><inheritdoc cref="IKrExecutionUnit" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        protected static async Task RunBeforeAsync(
            IKrExecutionUnit unit)
        {
            try
            {
                await unit.Instance.RunBeforeAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                var text = KrErrorHelper.RuntimeError(
                    unit,
                    e.Message);

                throw new ScriptExecutionException(
                    text,
                    unit.RuntimeSources?.RuntimeSourceBefore,
                    e);
            }
        }

        /// <summary>
        /// Выполняет условия времени выполнения маршрута для единицы выполнения.
        /// </summary>
        /// <param name="unit"><inheritdoc cref="IKrExecutionUnit" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если условия выполняются, иначе - <see langword="false"/>.</returns>
        protected async Task<bool> RunConditionsAsync(
            IKrExecutionUnit unit,
            IKrProcessRunnerContext context) =>
            await ExecuteScriptConditionAsync(unit) && await this.ExecuteSQLConditionAsync(unit, context);

        /// <summary>
        /// Выполняет C#-условие выполнения маршрута для единицы выполнения.
        /// </summary>
        /// <param name="unit"><inheritdoc cref="IKrExecutionUnit" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если условие выполняется, иначе - <see langword="false"/>.</returns>
        protected static async ValueTask<bool> ExecuteScriptConditionAsync(
            IKrExecutionUnit unit)
        {
            try
            {
                return await unit.Instance.RunConditionAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                var text = KrErrorHelper.RuntimeError(
                    unit,
                    e.Message);

                throw new ScriptExecutionException(
                    text,
                    unit.RuntimeSources?.RuntimeSourceCondition,
                    e);
            }
        }

        /// <summary>
        /// Выполняет SQL-условие времени построения маршрута для единицы выполнения.
        /// </summary>
        /// <param name="unit"><inheritdoc cref="IKrExecutionUnit" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если условие выполняется, иначе - <see langword="false"/>.</returns>
        protected Task<bool> ExecuteSQLConditionAsync(
            IKrExecutionUnit unit,
            IKrProcessRunnerContext context)
        {
            var sqlExecutionContext = new KrSqlExecutorContext(
                unit.RuntimeSources?.RuntimeSqlCondition,
                context.ValidationResult,
                (_, errorText, args) =>
                    KrErrorHelper.SqlRuntimeError(
                        unit,
                        errorText,
                        args),
                unit,
                context.SecondaryProcess,
                context.CardID,
                context.CardType?.ID,
                context.DocTypeID,
                context.WorkflowProcess.State,
                cancellationToken: context.CancellationToken);

            return this.SqlExecutor.ExecuteConditionAsync(sqlExecutionContext);
        }

        /// <summary>
        /// Выполняет сценарий постобработки для единицы выполнения.
        /// </summary>
        /// <param name="unit"><inheritdoc cref="IKrExecutionUnit" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        protected static async Task RunAfterAsync(
            IKrExecutionUnit unit)
        {
            try
            {
                await unit.Instance.RunAfterAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                var text = KrErrorHelper.RuntimeError(
                    unit,
                    e.Message);

                throw new ScriptExecutionException(
                    text,
                    unit.RuntimeSources?.RuntimeSourceAfter,
                    e);
            }
        }

        /// <summary>
        /// Выполняет заданное действие для единицы выполнения.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <param name="funcAsync">Выполняемое действие.</param>
        /// <param name="unit"><inheritdoc cref="IKrExecutionUnit" path="/summary"/></param>
        /// <returns>Результат выполнения <paramref name="funcAsync"/>.</returns>
        protected async Task<bool> SafeRunAsync(
            IKrProcessRunnerContext context,
            Func<IKrExecutionUnit, Task<bool>> funcAsync,
            IKrExecutionUnit? unit)
        {
            if (unit is null)
            {
                return false;
            }

            try
            {
                return await funcAsync(unit);
            }
            catch (ExecutionExceptionBase e)
            {
                var validator = ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .ErrorDetails(e.ErrorMessageText, e.SourceText);

                if (e.InnerException is not null)
                {
                    validator.ErrorException(e.InnerException);
                }

                validator.End();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                var text = KrErrorHelper.UnexpectedError(unit);

                ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .ErrorText(text)
                    .ErrorException(e)
                    .End();
            }

            return false;
        }

        /// <summary>
        /// Выполняет заданное действие для единицы выполнения.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <param name="funcAsync">Выполняемое действие.</param>
        /// <param name="unit"><inheritdoc cref="IKrExecutionUnit" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        protected Task SafeRunAsync(
            IKrProcessRunnerContext context,
            Func<IKrExecutionUnit, Task> funcAsync,
            IKrExecutionUnit? unit) =>
            this.SafeRunAsync(
                context,
                async unit =>
                {
                    await funcAsync(unit);
                    return true;
                },
                unit);

        protected static void SetStageFinalState(
            Stage stage,
            StageHandlerResult result)
        {
            switch (result.Action)
            {
                case StageHandlerAction.None:
                case StageHandlerAction.Skip:
                    if (stage.State == KrStageState.Active)
                    {
                        stage.State = KrStageState.Skipped;
                    }

                    break;
                case StageHandlerAction.Complete:
                    if (stage.State == KrStageState.Active)
                    {
                        stage.State = KrStageState.Completed;
                    }

                    break;
                case StageHandlerAction.Transition:
                case StageHandlerAction.GroupTransition:
                case StageHandlerAction.InProgress:
                    break;
                default:
                    throw ArgumentOutOfRange(result.Action);
            }
        }

        protected static SealableObjectList<Stage> GetSubsequentStages(
            IKrProcessRunnerContext context,
            Stage currentStage,
            out Guid nextGroupID)
        {
            nextGroupID = Guid.Empty;
            var allStages = context.WorkflowProcess.Stages;
            var currentStageIndex = allStages.IndexOf(p => p.RowID == currentStage.RowID);
            if (currentStageIndex == -1)
            {
                return [new Stage(currentStage)];
            }

            var subset = new SealableObjectList<Stage>(allStages.Count);
            Stage stage;
            var i = currentStageIndex;
            for (;
                i < allStages.Count && (stage = allStages[i]).StageGroupID == currentStage.StageGroupID;
                i++)
            {
                subset.Add(new Stage(stage));
            }

            if (i != allStages.Count)
            {
                nextGroupID = allStages[i].StageGroupID;
            }

            return subset;
        }

        protected async Task RunInDifferentCardContextAsync(
            IKrProcessRunnerContext context,
            Guid differentContextCardID,
            SealableObjectList<Stage> stageSubset,
            IDictionary<string, object?>? processInfo = null)
        {
            var db = this.DbScope.Db;
            db.SetCommand(this.DbScope.BuilderFactory
                    .Select()
                        .C("dci", "CardTypeID", "DocTypeID")
                    .From("DocumentCommonInfo", "dci").NoLock()
                    .Where().C("dci", "ID").Equals().P("ID")
                    .Build(),
                    db.Parameter("ID", differentContextCardID))
                .LogCommand();

            Guid typeID;
            Guid? docTypeID;

            await using (var reader = await db.ExecuteReaderAsync(context.CancellationToken))
            {
                if (!await reader.ReadAsync(context.CancellationToken))
                {
                    context.ValidationResult.AddError(
                        this,
                        string.Format(
                            Localize("$KrProcessRunner_DifferentContextCardDoesnotExist"),
                            differentContextCardID));
                    return;
                }

                typeID = reader.GetGuid(0);
                docTypeID = reader.GetNullableGuid(1);
            }

            var level = this.Scope.EnterNewLevel();

            try
            {
                var satellite = await this.Scope.GetKrSatelliteAsync(
                    differentContextCardID,
                    cancellationToken: context.CancellationToken);

                if (satellite is null)
                {
                    return;
                }

                processInfo ??= new Dictionary<string, object?>(StringComparer.Ordinal);

                var processHolder = new ProcessHolder
                {
                    Persistent = false,
                    MainProcessType = KrConstants.KrSecondaryProcessName,
                    ProcessHolderID = Guid.NewGuid(),
                    PrimaryProcessCommonInfo = NotNullOrThrow(this.ObjectModelMapper.GetMainProcessCommonInfo(
                        satellite,
                        false)),
                    MainProcessCommonInfo = new MainProcessCommonInfo(
                        null,
                        processInfo,
                        null,
                        null,
                        null,
                        null,
                        KrState.Draft.ID,
                        null,
                        null),
                };

                var workflowProcess = new WorkflowProcess(
                    processInfo,
                    processInfo,
                    stageSubset,
                    nestedProcessID: null,
                    isMainProcess: false); // При смене контекста выполнения создаётся вторичный процесс.
                workflowProcess.UpdateInitialWorkflowProcess();

                processHolder.MainWorkflowProcess = workflowProcess;

                this.ObjectModelMapper.FillWorkflowProcessFromPci(
                    workflowProcess,
                    processHolder.MainProcessCommonInfo,
                    processHolder.PrimaryProcessCommonInfo);

                await using var mainCardAccessStrategy = new KrScopeMainCardAccessStrategy(differentContextCardID, this.Scope);
                var historyManager =
                    context.TaskHistoryResolver?.TaskHistoryManager ?? this.UnityContainer.Resolve<ICardTaskHistoryManager>();

                var taskHistoryResolver = new KrTaskHistoryResolver(
                    mainCardAccessStrategy,
                    context.CardContext,
                    context.ValidationResult,
                    historyManager);

                var components = await KrComponentsHelper.GetKrComponentsAsync(
                    typeID,
                    docTypeID,
                    this.TypesCache,
                    context.CancellationToken);

                var runnerContext = new KrProcessRunnerContext(
                    workflowAPI: null,
                    taskHistoryResolver: taskHistoryResolver,
                    mainCardAccessStrategy: mainCardAccessStrategy,
                    cardID: differentContextCardID,
                    cardType: await CardComponentHelper.TryGetCardTypeAsync(typeID, this.CardMetadata, context.CancellationToken),
                    docTypeID: docTypeID,
                    krComponents: components,
                    contextualSatellite: satellite,    // Контекстуальный сателлит есть, т.к. процесс в рамках карточки работает
                    processHolderSatellite: null,      // Процессного сателлита нет, процесс синхронный весь в памяти
                    workflowProcess: workflowProcess,
                    processHolder: processHolder,
                    processInfo: null,
                    validationResult: context.ValidationResult,
                    cardContext: context.CardContext,
                    defaultPreparingGroupStrategyFunc: () => new DisableRecalcPreparingGroupRecalcStrategy(),
                    parentProcessTypeName: null,
                    parentProcessID: null,
                    isProcessHolderCreated: true,
                    updateCardFuncAsync: this.UpdateCardAsync,
                    notMessageHasNoActiveStages: context.NotMessageHasNoActiveStages,
                    secondaryProcess: null,
                    ignoreGroupScripts: true,
                    cancellationToken: context.CancellationToken);

                await this.RunnerProvider.GetRunner(KrProcessRunnerNames.Sync).RunAsync(runnerContext);

                if (!context.ValidationResult.IsSuccessful())
                {
                    return;
                }

                await runnerContext.UpdateCardAsync();

                await level.ApplyChangesAsync(
                    differentContextCardID,
                    true,
                    context.ValidationResult,
                    cancellationToken: context.CancellationToken);
            }
            finally
            {
                await level.ExitAsync(context.ValidationResult);
            }

            // Заполнять ValidationResult и ClientCommands не нужно, т.к. это не верхний уровень,
            // Раннер может быть запущен только в скопе.
        }

        /// <summary>
        /// Добавляет новую запись в список истории выполнения процесса.
        /// </summary>
        /// <param name="context">Контекст объекта управляющего обработкой процесса.</param>
        /// <param name="stage">Этап.</param>
        /// <param name="result">Результат выполнения этапа.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void AddTrace(IKrProcessRunnerContext context, Stage stage, StageHandlerResult result) =>
            this.Scope.TryAddToTrace(
                new KrProcessTraceItem(
                    stage,
                    result,
                    context.CardID,
                    context.ProcessInfo?.ProcessID));

        protected void AssertRunnerMode(
            Guid stageTypeID)
        {
            var descriptor = this.ProcessContainer.GetHandlerDescriptor(stageTypeID);
            if (!descriptor.SupportedModes.Contains(this.RunnerMode))
            {
                // Тип этапа "имя" не поддерживает синхронный/асинхронный режим. Поддерживаемые режимы: ...
                var text = LocalizeFormat(
                    "$KrProcess_StageTypeDoesNotSupportMode",
                    descriptor.Caption,
                    this.RunnerMode.GetCaption(),
                    string.Join(", ", descriptor.SupportedModes.Select(p => p.GetCaption())));
                throw new ProcessRunnerInterruptedException(text);
            }
        }

        protected async Task RecalcSqlRolesAsync(
            Stage stage,
            IKrProcessRunnerContext context)
        {
            if (stage.RowChanged)
            {
                return;
            }

            try
            {
                var oldRowIDs = new List<Guid>(stage.Performers.Count);
                stage.Performers.RemoveAll(p =>
                {
                    if (!p.IsSql)
                    {
                        return false;
                    }

                    oldRowIDs.Add(p.RowID);
                    return true;
                });

                if (!string.IsNullOrWhiteSpace(stage.SqlPerformers))
                {
                    await this.RecalcRoleAsync(stage, oldRowIDs, context);
                }
            }
            catch (ExecutionExceptionBase e)
            {
                var validator = ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .ErrorDetails(e.ErrorMessageText, e.SourceText);

                if (e.InnerException is not null)
                {
                    validator.ErrorException(e.InnerException);
                }

                validator.End();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                var text = KrErrorHelper.UnexpectedError(stage);

                ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .ErrorText(text)
                    .ErrorException(e)
                    .End();
            }
        }

        protected async Task RecalcRoleAsync(
            Stage stage,
            List<Guid> oldRowIDs,
            IKrProcessRunnerContext context)
        {
            StageTypeDescriptor descriptor;
            if (!stage.StageTypeID.HasValue
                || (descriptor = this.ProcessContainer.GetHandlerDescriptor(stage.StageTypeID.Value)) is null)
            {
                return;
            }

            var sqlPreprocessorContext = new KrSqlExecutorContext(
                stage,
                context.ValidationResult,
                static (ctx, errorText, args) =>
                    KrErrorHelper.SqlPerformersError(
                        ctx.StageName,
                        ctx.TemplateName,
                        ctx.GroupName,
                        ctx.SecondaryProcess?.Name,
                        errorText,
                        args),
                context.SecondaryProcess,
                context.CardID,
                context.CardType?.ID,
                context.DocTypeID,
                context.WorkflowProcess.State,
                cancellationToken: context.CancellationToken);

            var newPerformers = await this.SqlExecutor.ExecutePerformersAsync(sqlPreprocessorContext);

            switch (descriptor.PerformerUsageMode)
            {
                case PerformerUsageMode.Single:
                    UpdateSingleSqlPerformer(
                        stage,
                        newPerformers);
                    break;
                case PerformerUsageMode.Multiple:
                    UpdateSqlPerformers(
                        stage,
                        oldRowIDs,
                        newPerformers);
                    break;
            }
        }

        protected static void UpdateSingleSqlPerformer(
            Stage stage,
            IList<Performer> newPerformers)
        {
            if (newPerformers.Count > 0)
            {
                var newPerformer = newPerformers[0];

                stage.Performer = new Performer(
                    newPerformer.PerformerID,
                    newPerformer.PerformerName);
            }
        }

        protected static void UpdateSqlPerformers(
            Stage stage,
            IEnumerable<Guid> oldRowIDs,
            IEnumerable<Performer> newPerformers)
        {
            var insertionIndex = stage.SqlPerformersIndex is null
                || stage.SqlPerformersIndex > stage.Performers.Count
                ? stage.Performers.Count
                : stage.SqlPerformersIndex.Value;

            var idsEnum = oldRowIDs.GetEnumerator();
            try
            {
                foreach (var newPerformer in newPerformers)
                {
                    var id = idsEnum.MoveNext()
                        ? idsEnum.Current
                        : Guid.NewGuid();
                    // Инкапсуляция разбивается о скалы суровой реальности
                    newPerformer.GetStorage()[nameof(Performer.RowID)] = id;
                    stage.Performers.Insert(insertionIndex++, newPerformer);
                }
            }
            finally
            {
                idsEnum.Dispose();
            }
        }

        /// <summary>
        /// Задаёт автора (инициатора) текущего процесса.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        protected async Task SetAuthorAsync(
            IKrProcessRunnerContext context)
        {
            var user = this.Session.User;
            var userID = user.ID;
            var userName = user.Name;

            switch (context.ProcessInfo?.ProcessTypeName)
            {
                case KrConstants.KrProcessName:
                    await this.SetAuthorProcessAsync(context, userID, userName);
                    break;
                case KrConstants.KrSecondaryProcessName:
                    await this.SetAuthorSecondaryProcessAsync(context, userID, userName);
                    break;
                case KrConstants.KrNestedProcessName:
                    {
                        var process = context.WorkflowProcess;

                        if (process.AuthorCurrentProcess is null)
                        {
                            var author = new Author(userID, userName);

                            process.AuthorCurrentProcess = author;
                        }

                        var mainProcessOwner = process.ProcessOwner ?? process.Author;

                        if (process.ProcessOwnerCurrentProcess is null
                            && mainProcessOwner is not null)
                        {
                            var processOwner = new Author(mainProcessOwner.AuthorID, mainProcessOwner.AuthorName);

                            process.ProcessOwnerCurrentProcess = processOwner;
                        }
                    }

                    break;
            }
        }

        #endregion

        #region Private Methods

        private static void CheckTime(
            Stage stage,
            StageTypeDescriptor descriptor)
        {
            if (descriptor.UseTimeLimit
                && descriptor.UsePlanned)
            {
                if (stage.TimeLimit is null
                    && stage.Planned is null)
                {
                    KrErrorHelper.PlannedNotSpecified(stage.Name);
                }
            }
            else if (descriptor.UsePlanned
                && stage.Planned is null)
            {
                KrErrorHelper.PlannedNotSpecified(stage.Name);
            }
            else if (descriptor.UseTimeLimit
                && stage.TimeLimit is null)
            {
                KrErrorHelper.TimeLimitNotSpecified(stage.Name);
            }
        }

        private static void CheckPerformers(
            Stage stage,
            StageTypeDescriptor descriptor)
        {
            if (descriptor.PerformerIsRequired)
            {
                switch (descriptor.PerformerUsageMode)
                {
                    case PerformerUsageMode.Single:
                        if (stage.Performer is null)
                        {
                            KrErrorHelper.PerformerNotSpecified(stage.Name);
                        }

                        break;
                    case PerformerUsageMode.Multiple:
                        if (stage.Performers?.Count > 0 != true)
                        {
                            KrErrorHelper.PerformerNotSpecified(stage.Name);
                        }

                        break;
                }
            }
        }

        private void AssertKrScope()
        {
            if (!this.Scope.Exists)
            {
                throw new InvalidOperationException($"{this.GetType().FullName} can't run without KrScope.");
            }
        }

        private async ValueTask UpdateCardAsync(
            IKrProcessRunnerContext context)
        {
            this.ObjectModelMapper.ObjectModelToPci(
                context.ProcessHolder.MainWorkflowProcess!,
                context.ProcessHolder.MainProcessCommonInfo!,
                context.ProcessHolder.MainProcessCommonInfo!,
                context.ProcessHolder.PrimaryProcessCommonInfo);

            var mainCard = await context.MainCardAccessStrategy.GetCardAsync(
                cancellationToken: context.CancellationToken);

            if (mainCard is null)
            {
                return;
            }

            await this.ObjectModelMapper.SetMainProcessCommonInfoAsync(
                mainCard,
                context.ContextualSatellite!,
                context.ProcessHolder.PrimaryProcessCommonInfo!,
                context.CancellationToken);
        }

        private async Task IntermediateApplyChangesAsync(
            IKrProcessRunnerContext context)
        {
            if (!context.CardID.HasValue)
            {
                return;
            }

            var mainCardID = context.CardID.Value;

            if (!this.Scope.CardIsLoaded(mainCardID))
            {
                return;
            }

            var mainCard = await this.Scope.GetMainCardAsync(
                mainCardID,
                cancellationToken: context.CancellationToken);

            if (mainCard is null
                || !mainCard.HasChanges()
                && !mainCard.HasNumberQueueToProcess())
            {
                return;
            }

            var processID = context.ProcessInfo?.ProcessID;
            if (processID.HasValue)
            {
                this.Scope.RemoveLaunchedRunner(processID.Value);
            }

            await context.UpdateCardAsync();

            if (this.Scope.TryGetLoadedSatellite(mainCardID, null, DefaultCardTypes.KrSatelliteTypeID) is { } satellite)
            {
                ProcessInfoCacheHelper.Update(this.StageSerializer, satellite);

                if (satellite.StoreMode == CardStoreMode.Insert
                    || satellite.HasChanges())
                {
                    await this.cardTransactionStrategy.ExecuteInWriterLockAsync(
                        mainCardID,
                        CardComponentHelper.DoNotCheckVersion,
                        context.ValidationResult,
                        async p =>
                        {
                            await this.StoreCardAsync(
                                this.cardRepository,
                                this.cardStreamServerRepository,
                                satellite,
                                null,
                                p.ValidationResult,
                                cancellationToken: p.CancellationToken);

                            if (!p.ValidationResult.IsSuccessful())
                            {
                                p.ReportError = true;
                            }
                        },
                        cancellationToken: context.CancellationToken);
                }
            }

            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            ICardRepository suitableCardRepository;
            ICardStreamServerRepository suitableCardStreamServerRepository;

            if (TransactionScopeContext.HasCurrent
                && TransactionScopeContext.Current.Locks.TryGetValue(new (mainCard.ID, RedisLockKeys.CardsObjectKey), out var objectLockInfo)
                && objectLockInfo.LockType == ObjectLockTypes.WriteLock)
            {
                suitableCardRepository = this.cardRepositoryEwt;
                suitableCardStreamServerRepository = this.cardStreamServerRepositoryEwt;
            }
            else
            {
                suitableCardRepository = this.cardRepository;
                suitableCardStreamServerRepository = this.cardStreamServerRepository;
            }

            var container = this.Scope.TryGetLoadedCardFileContainer(mainCardID);

            await this.StoreCardAsync(
                suitableCardRepository,
                suitableCardStreamServerRepository,
                mainCard,
                container,
                context.ValidationResult,
                cancellationToken: context.CancellationToken);

            // Освобождение ресурсов файлового контейнера будет выполнено в методе KrScopeLevel.Exit() после завершения сохранения карточки.

            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            foreach (var card in this.Scope.GetLoadedCards())
            {
                if (card.TypeID != DefaultCardTypes.KrSecondarySatelliteTypeID)
                {
                    continue;
                }

                var secondaryProcessMainCardID = card
                    .GetApprovalInfoSection()
                    .RawFields
                    .Get<Guid?>(KrConstants.KrProcessCommonInfo.MainCardID);

                if (secondaryProcessMainCardID != mainCardID)
                {
                    continue;
                }

                ProcessInfoCacheHelper.Update(this.StageSerializer, card);

                if (card.StoreMode == CardStoreMode.Insert
                    || card.HasChanges())
                {
                    await this.StoreCardAsync(
                        this.cardRepository,
                        this.cardStreamServerRepository,
                        card,
                        null,
                        context.ValidationResult,
                        cancellationToken: context.CancellationToken);

                    if (!context.ValidationResult.IsSuccessful())
                    {
                        return;
                    }
                }
            }

            // Текущий процесс не является дочерним к основному (дочерние запускаются через ветвление)
            // Обновлять состояние процесса может только основной процесс
            // См. #665
            if (context.IsProcessHolderCreated)
            {
                context.WorkflowProcess.UpdateInitialWorkflowProcess();
            }
        }

        private async Task StoreCardAsync(
            ICardRepository cardRepository,
            ICardStreamServerRepository cardStreamServerRepository,
            Card card,
            ICardFileContainer? fileContainer,
            IValidationResultBuilder result,
            CancellationToken cancellationToken = default)
        {
            var copy = card.Clone();
            card.RemoveChanges(CardRemoveChangesDeletedHandling.Remove);
            card.RemoveNumberQueue();

            copy.RemoveWorkflowQueue();

            var storeMode = copy.StoreMode;
            if (storeMode == CardStoreMode.Update)
            {
                copy.UpdateStates();
            }

            copy.RemoveAllButChanged(storeMode);
            var request = new CardStoreRequest
            {
                Card = copy,
            };
            request.SetIntermediateApply();

            this.Scope.ModifyStoreRequest(request);

            if (await KrComponentsHelper.HasBaseAsync(card.TypeID, this.TypesCache, cancellationToken))
            {
                this.tokenProvider.CreateFullToken(copy).Set(copy.Info);
            }

            var digest = await cardRepository.GetDigestAsync(
                card,
                CardDigestEventNames.ActionHistoryStoreRouteProcess,
                cancellationToken);

            if (digest is not null)
            {
                request.SetDigest(digest);
            }

            var response = await CardHelper.StoreAsync(
                request,
                fileContainer?.FileContainer,
                cardRepository,
                cardStreamServerRepository,
                cancellationToken);

            if (response is not null)
            {
                result.Add(response.ValidationResult);

                card.Version = response.CardVersion;
            }
        }

        /// <summary>
        /// Задаёт автора (инициатора) процесса в сателлите текущего основного процесса.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <param name="userID">Идентификатор автора (инициатора) процесса.</param>
        /// <param name="userName">Имя автора (инициатора) процесса.</param>
        /// <returns>Асинхронная задача.</returns>
        private async Task SetAuthorProcessAsync(
            IKrProcessRunnerContext context,
            Guid userID,
            string userName)
        {
            var contextualSatellite = context.ContextualSatellite;

            if (context.WorkflowProcess.Author is not null
                || contextualSatellite is null)
            {
                return;
            }

            // Для дальнейшей доступности автора (инициатора) процесса в объектной модели.
            var process = context.WorkflowProcess;
            ThrowIfNull(context.ProcessHolder.PrimaryProcessCommonInfo);

            process.Author = process.AuthorCurrentProcess = new Author(userID, userName);

            // Для дальнейшей доступности в холдере.
            context.ProcessHolder.PrimaryProcessCommonInfo.AuthorID = userID;
            context.ProcessHolder.PrimaryProcessCommonInfo.AuthorName = userName;

            // Для дальнейшей доступности автора в сателлите.
            var aciFields = contextualSatellite.GetApprovalInfoSection().RawFields;
            aciFields[KrConstants.KrProcessCommonInfo.AuthorID] = userID;
            aciFields[KrConstants.KrProcessCommonInfo.AuthorName] = userName;

            if (this.RunnerMode == KrProcessRunnerMode.Async)
            {
                await this.SetAuthorInDbAsync(
                    KrConstants.KrApprovalCommonInfo.Name,
                    userID,
                    userName,
                    contextualSatellite.ID,
                    context.CancellationToken);
            }
        }

        /// <summary>
        /// Задаёт автора (инициатора) процесса в сателлите текущего вторичного процесса.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <param name="userID">Идентификатор автора (инициатора) процесса.</param>
        /// <param name="userName">Имя автора (инициатора) процесса.</param>
        /// <returns>Асинхронная задача.</returns>
        private async Task SetAuthorSecondaryProcessAsync(
            IKrProcessRunnerContext context,
            Guid userID,
            string userName)
        {
            ThrowIfNull(context.ProcessHolder.MainProcessCommonInfo);

            var process = context.WorkflowProcess;

            if (process.AuthorCurrentProcess is not null)
            {
                return;
            }

            // Для дальнейшей доступности автора (инициатора) процесса в объектной модели.
            process.AuthorCurrentProcess = new Author(userID, userName);

            // Для дальнейшей доступности в холдере.
            context.ProcessHolder.MainProcessCommonInfo.AuthorID = userID;
            context.ProcessHolder.MainProcessCommonInfo.AuthorName = userName;

            // Для дальнейшей доступности автора в сателлите.
            var satellite = context.ProcessHolderSatellite;

            if (satellite is null)
            {
                return;
            }

            var fields = satellite.GetApprovalInfoSection().RawFields;
            fields[KrConstants.KrProcessCommonInfo.AuthorID] = userID;
            fields[KrConstants.KrProcessCommonInfo.AuthorName] = userName;

            if (this.RunnerMode == KrProcessRunnerMode.Async)
            {
                await this.SetAuthorInDbAsync(
                    KrConstants.KrSecondaryProcessCommonInfo.Name,
                    userID,
                    userName,
                    satellite.ID,
                    context.CancellationToken);
            }
        }

        /// <summary>
        /// Обновляет автора (инициатора) процесса в базе данных.
        /// </summary>
        /// <param name="sectionName">Имя обновляемой секции.</param>
        /// <param name="authorID">Идентификатор автора (инициатора) процесса.</param>
        /// <param name="authorName">Имя автора (инициатора) процесса.</param>
        /// <param name="satelliteID">Идентификатор сателлита.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        private Task SetAuthorInDbAsync(
            string sectionName,
            Guid authorID,
            string authorName,
            Guid satelliteID,
            CancellationToken cancellationToken = default)
        {
            var db = this.DbScope.Db;
            return db.SetCommand(
                    this.DbScope.BuilderFactory
                        .Update(sectionName)
                        .C(KrConstants.KrProcessCommonInfo.AuthorID).Assign().P(KrConstants.KrProcessCommonInfo.AuthorID)
                        .C(KrConstants.KrProcessCommonInfo.AuthorName).Assign().P(KrConstants.KrProcessCommonInfo.AuthorName)
                        .Where().C(KrConstants.KrProcessCommonInfo.ID).Equals().P(KrConstants.KrProcessCommonInfo.ID)
                        .Build(),
                    db.Parameter(KrConstants.KrProcessCommonInfo.AuthorID, authorID),
                    db.Parameter(KrConstants.KrProcessCommonInfo.AuthorName, authorName),
                    db.Parameter(KrConstants.KrProcessCommonInfo.ID, satelliteID))
                .LogCommand()
                .ExecuteNonQueryAsync(cancellationToken);
        }

        #endregion
    }
}
