#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.StateMachine;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.GlobalSignals
{
    /// <summary>
    /// Обработчик сигнала типа <see cref="KrConstants.KrCancelProcessGlobalSignal"/>.
    /// </summary>
    /// <param name="interrupter"><inheritdoc cref="IKrStageInterrupter" path="/summary"/></param>
    public sealed class CancelProcessSignalHandler(
        IKrStageInterrupter interrupter) :
        GlobalSignalHandlerBase
    {
        #region Fields

        private readonly IKrStageInterrupter interrupter = NotNullOrThrow(interrupter);

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task<IGlobalSignalHandlerResult> Handle(
            IGlobalSignalHandlerContext context)
        {
            if (!context.Stage.StageTypeID.HasValue)
            {
                context.RunnerContext.WorkflowProcess.CurrentApprovalStageRowID = null;
                context.RunnerContext.PreparingGroupStrategy = new DisableRecalcPreparingGroupRecalcStrategy();
                TransitionHelper.SetInactiveStateToAllStages(
                    context.RunnerContext.WorkflowProcess.Stages,
                    context.RunnerContext.ProcessHolderSatellite);
                return GlobalSignalHandlerResult.WithoutContinuationProcessResult;
            }

            var stageInterrupterContext = new KrStageInterrupterContext(
                DirectionAfterInterrupt.Backward,
                context.Stage,
                context.RunnerContext,
                context.RunnerMode,
                ci => ci
                    ? KrProcessState.Default
                    : new KrProcessState(
                        KrConstants.CancelellationProcessState,
                        new Dictionary<string, object>
                        {
                            [KrConstants.Keys.DirectionAfterInterruptParam] = DirectionAfterInterrupt.Backward,
                        }));

            var completelyInterrupted = await this.interrupter.InterruptStageAsync(stageInterrupterContext);
            context.Stage = stageInterrupterContext.Stage;

            if (completelyInterrupted)
            {
                context.RunnerContext.WorkflowProcess.CurrentApprovalStageRowID = null;
                context.RunnerContext.PreparingGroupStrategy = new DisableRecalcPreparingGroupRecalcStrategy();

                TransitionHelper.SetInactiveStateToAllStages(
                    context.RunnerContext.WorkflowProcess.Stages,
                    context.RunnerContext.ProcessHolderSatellite);
            }

            return GlobalSignalHandlerResult.WithoutContinuationProcessResult;
        }

        #endregion
    }
}
