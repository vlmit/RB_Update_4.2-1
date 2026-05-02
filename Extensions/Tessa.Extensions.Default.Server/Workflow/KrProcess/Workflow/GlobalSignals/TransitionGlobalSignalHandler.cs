#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Cards.Workflow;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.StateMachine;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.GlobalSignals
{
    /// <summary>
    /// Обработчик сигнала типа <see cref="KrConstants.KrTransitionGlobalSignal"/>.
    /// </summary>
    /// <param name="interrupter"><inheritdoc cref="IKrStageInterrupter" path="/summary"/></param>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    public sealed class TransitionGlobalSignalHandler(
        IKrStageInterrupter interrupter,
        IDbScope dbScope,
        ISession session) :
        GlobalSignalHandlerBase
    {
        #region Constants And Static Fields

        private const int DoNothing = -1;
        private const int ToEndOfProcess = -2;

        #endregion

        #region Nested Types

        private readonly struct TransitionInfo()
        {
            public int TransitionIndex { get; init; }
            public IPreparingGroupRecalcStrategy? PreparingGroupRecalcStrategy { get; init; }
            public bool ForceStartGroup { get; init; }
            public DirectionAfterInterrupt DirectionAfterInterrupt { get; init; }
        }

        #endregion

        #region Fields

        private readonly IKrStageInterrupter interrupter = NotNullOrThrow(interrupter);

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);

        private readonly ISession session = NotNullOrThrow(session);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task<IGlobalSignalHandlerResult> Handle(
            IGlobalSignalHandlerContext context)
        {
            var runnerContext = context.RunnerContext;
            var signalInfo = runnerContext.SignalInfo;

            if (signalInfo is null)
            {
                return GlobalSignalHandlerResult.EmptyHandlerResult;
            }

            var keepStageStates = signalInfo.Signal.Parameters.TryGet(KrConstants.KrTransitionKeepStates, false);
            var transitionInfo = this.GetTransitionInfo(signalInfo, context);
            var transitionIndex = transitionInfo.TransitionIndex;
            var preparingGroupRecalcStrategy = transitionInfo.PreparingGroupRecalcStrategy;

            if (transitionIndex == DoNothing)
            {
                return GlobalSignalHandlerResult.ContinueCurrentRunResult;
            }

            if (!await this.InterruptStageAsync(
                context,
                keepStageStates,
                transitionInfo))
            {
                // За раз прервать не удалось, допрерывается потом
                return GlobalSignalHandlerResult.WithoutContinuationProcessResult;
            }

            var transitTo = transitionIndex != ToEndOfProcess
                ? (Guid?) context.RunnerContext.WorkflowProcess.Stages[transitionIndex].RowID
                : null;
            context.RunnerContext.WorkflowProcess.CurrentApprovalStageRowID = transitTo;
            context.RunnerContext.PreparingGroupStrategy = preparingGroupRecalcStrategy;
            var result = transitionInfo.ForceStartGroup
                ? GlobalSignalHandlerResult.ContinueCurrentRunWithStartingNewGroupResult
                : GlobalSignalHandlerResult.ContinueCurrentRunResult;

            if (keepStageStates)
            {
                return result;
            }

            if (transitTo.HasValue)
            {
                TransitionHelper.ChangeStatesTransition(
                    context.RunnerContext.WorkflowProcess.Stages,
                    context.Stage.RowID,
                    transitTo.Value,
                    context.RunnerContext.ProcessHolderSatellite);
            }
            else
            {
                TransitionHelper.SetSkipStateToSubsequentStages(
                    context.Stage,
                    context.RunnerContext.WorkflowProcess.Stages,
                    context.RunnerContext.ProcessHolderSatellite);
            }

            return result;
        }

        #endregion

        #region Private Methods

        private TransitionInfo GetTransitionInfo(
            IWorkflowSignalInfo signalInfo,
            IGlobalSignalHandlerContext context)
        {
            var runnerContext = context.RunnerContext;
            var stages = runnerContext.WorkflowProcess.Stages;
            var transitionIndex = 0;
            var forceStartGroup = false;

            var originalStageIndex = stages.IndexOf(context.Stage, new ReferenceEqualityComparer<Stage>());
            var originalStageGroupID = context.Stage.StageGroupID;
            var stageGroupID = signalInfo.Signal.Parameters.TryGet<Guid?>(KrConstants.StageGroupID);
            var stageRowID = signalInfo.Signal.Parameters.TryGet<Guid?>(KrConstants.StageRowID);
            var transitToPreviousGroup = signalInfo.Signal.Parameters.TryGet<bool?>(KrConstants.KrTransitionPrevGroup) ?? false;
            var transitToCurrentGroup = signalInfo.Signal.Parameters.TryGet<bool?>(KrConstants.KrTransitionCurrentGroup) ?? false;
            var transitToNextGroup = signalInfo.Signal.Parameters.TryGet<bool?>(KrConstants.KrTransitionNextGroup) ?? false;
            IPreparingGroupRecalcStrategy? preparingGroupRecalcStrategy = null;
            var directionAfterInterrupt = default(DirectionAfterInterrupt);

            if (stageRowID.HasValue)
            {
                transitionIndex = TransitionHelper.TransitToStage(stages, stageRowID.Value);
                preparingGroupRecalcStrategy = new ExplicitlySelectedPreparingGroupRecalcStrategy();
                directionAfterInterrupt = transitionIndex <= originalStageIndex
                    ? DirectionAfterInterrupt.Backward
                    : DirectionAfterInterrupt.Forward;
            }

            if (stageGroupID.HasValue)
            {
                forceStartGroup = true; // Переход может выполняться на текущую группу.
                transitionIndex = TransitionHelper.TransitToStageGroup(stages, stageGroupID.Value);
                preparingGroupRecalcStrategy = new ExplicitlySelectedPreparingGroupRecalcStrategy();
                directionAfterInterrupt = transitionIndex <= originalStageIndex
                    ? DirectionAfterInterrupt.Backward
                    : DirectionAfterInterrupt.Forward;
            }

            if (transitToNextGroup)
            {
                transitionIndex = TransitionHelper.TransitToNextGroup(stages, originalStageGroupID);
                preparingGroupRecalcStrategy = new ForwardPreparingGroupRecalcStrategy(
                    this.dbScope, 
                    this.session);
                if (transitionIndex == TransitionHelper.NotFound)
                {
                    // Если следующую не нашли, то идем просто до конца
                    transitionIndex = ToEndOfProcess;
                }

                directionAfterInterrupt = DirectionAfterInterrupt.Forward;
            }

            if (transitToPreviousGroup)
            {
                transitionIndex = TransitionHelper.TransitToPreviousGroup(stages, originalStageGroupID);
                preparingGroupRecalcStrategy = new BackwardPreparingGroupRecalcStrategy(
                    this.dbScope, 
                    this.session);
                if (transitionIndex == TransitionHelper.NotFound)
                {
                    // Если переход назад не удался, то просто перейдем на текущую
                    transitToCurrentGroup = true;
                }

                directionAfterInterrupt = DirectionAfterInterrupt.Backward;
            }

            if (transitToCurrentGroup)
            {
                forceStartGroup = true;
                transitionIndex = TransitionHelper.TransitToStageGroup(stages, originalStageGroupID);
                preparingGroupRecalcStrategy = new CurrentPreparingGroupRecalcStrategy();
                directionAfterInterrupt = DirectionAfterInterrupt.Backward;
            }

            if (transitionIndex == TransitionHelper.NotFound)
            {
                return new TransitionInfo { TransitionIndex = DoNothing, };
            }

            return new TransitionInfo
            {
                TransitionIndex = transitionIndex,
                PreparingGroupRecalcStrategy = preparingGroupRecalcStrategy,
                ForceStartGroup = forceStartGroup,
                DirectionAfterInterrupt = directionAfterInterrupt,
            };
        }

        private async Task<bool> InterruptStageAsync(
            IGlobalSignalHandlerContext context,
            bool keepStageStates,
            TransitionInfo transitionInfo)
        {
            if (!context.Stage.StageTypeID.HasValue)
            {
                return true;
            }

            var stageInterrupterContext = new KrStageInterrupterContext(
                transitionInfo.DirectionAfterInterrupt,
                context.Stage,
                context.RunnerContext,
                context.RunnerMode,
                ci =>
                {
                    if (ci)
                    {
                        return KrProcessState.Default;
                    }

                    if (transitionInfo.TransitionIndex == ToEndOfProcess)
                    {
                        return new KrProcessState(
                            KrConstants.TransitionProcessState,
                            new Dictionary<string, object?>
                            {
                                [KrConstants.Keys.KeepStageStatesParam] =
                                    BooleanBoxes.Box(keepStageStates),
                                [KrConstants.Keys.FinalStageRowIDParam] = null,
                                [KrConstants.Keys.PreparingGroupRecalcStrategyParam] =
                                    transitionInfo.PreparingGroupRecalcStrategy,
                                [KrConstants.Keys.DirectionAfterInterruptParam] =
                                    Int32Boxes.Box((int) transitionInfo.DirectionAfterInterrupt),
                            });
                    }

                    var transitTo = context.RunnerContext
                        .WorkflowProcess
                        .Stages[transitionInfo.TransitionIndex]
                        .RowID;

                    return new KrProcessState(
                        KrConstants.TransitionProcessState,
                        new Dictionary<string, object?>
                        {
                            [KrConstants.Keys.KeepStageStatesParam] =
                                BooleanBoxes.Box(keepStageStates),
                            [KrConstants.Keys.FinalStageRowIDParam] = transitTo,
                            [KrConstants.Keys.PreparingGroupRecalcStrategyParam] =
                                transitionInfo.PreparingGroupRecalcStrategy,
                            [KrConstants.Keys.ForceStartGroupParam] =
                                BooleanBoxes.Box(transitionInfo.ForceStartGroup),
                            [KrConstants.Keys.DirectionAfterInterruptParam] =
                                Int32Boxes.Box((int) transitionInfo.DirectionAfterInterrupt),
                        });
                });

            var completelyInterrupted = await this.interrupter.InterruptStageAsync(stageInterrupterContext);
            context.Stage = stageInterrupterContext.Stage;

            return completelyInterrupted;
        }

        #endregion
    }
}
