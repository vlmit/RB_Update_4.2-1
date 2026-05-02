#nullable enable

using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.StateMachine;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <inheritdoc cref="IKrStageInterrupter"/>
    /// <param name="krScope"><inheritdoc cref="IKrScope" path="/summary"/></param>
    /// <param name="processContainer"><inheritdoc cref="IKrProcessContainer" path="/summary"/></param>
    public sealed class KrStageInterrupter(
        IKrScope krScope,
        IKrProcessContainer processContainer) :
        IKrStageInterrupter
    {
        #region Fields

        private readonly IKrScope krScope = NotNullOrThrow(krScope);

        private readonly IKrProcessContainer processContainer = NotNullOrThrow(processContainer);

        #endregion

        #region IKrStageInterrupter Members

        /// <inheritdoc />
        public async Task<bool> InterruptStageAsync(
            IKrStageInterrupterContext context)
        {
            if (!context.Stage.StageTypeID.HasValue)
            {
                return true;
            }

            var handler = this.processContainer.ResolveHandler(
                context.Stage.StageTypeID.Value);

            if (handler is null)
            {
                KrErrorHelper.WarnStageHandlerIsNull(
                    context.RunnerContext.ValidationResult,
                    context.Stage);

                return true;
            }

            var stageContext = new StageTypeHandlerContext(
                context.RunnerContext,
                context.Stage,
                context.RunnerMode,
                context.DirectionAfterInterrupt);

            var completelyInterrupted = await handler.HandleStageInterruptAsync(stageContext);
            context.Stage = stageContext.Stage;

            if (completelyInterrupted)
            {
                this.krScope.TryAddToTrace(
                    new KrProcessTraceItem(
                        context.Stage,
                        null,
                        context.RunnerContext.CardID,
                        context.RunnerContext.ProcessInfo?.ProcessID));

                if (context.SetupNextState is not null)
                {
                    this.krScope.SetRunnerState(
                        NotNullOrThrow(context.RunnerContext.ProcessInfo).ProcessID,
                        context.SetupNextState.Invoke(true));
                }

                return true;
            }

            this.krScope.SetRunnerState(
                NotNullOrThrow(context.RunnerContext.ProcessInfo).ProcessID,
                new KrProcessState(
                    KrConstants.InterruptionProcessState,
                    null,
                    context.SetupNextState?.Invoke(false)));

            return false;
        }

        #endregion
    }
}
