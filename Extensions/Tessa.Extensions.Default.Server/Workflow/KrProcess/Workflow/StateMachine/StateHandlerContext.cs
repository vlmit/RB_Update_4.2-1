#nullable enable

using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.StateMachine
{
    /// <inheritdoc cref="IStateHandlerContext"/>
    public sealed class StateHandlerContext :
        IStateHandlerContext
    {
        #region Constrictors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="state"><inheritdoc cref="State" path="/summary"/></param>
        /// <param name="stage"><inheritdoc cref="Stage" path="/summary"/></param>
        /// <param name="runnerMode"><inheritdoc cref="RunnerMode" path="/summary"/></param>
        /// <param name="runnerContext"><inheritdoc cref="RunnerContext" path="/summary"/></param>
        public StateHandlerContext(
            KrProcessState state,
            Stage? stage,
            KrProcessRunnerMode runnerMode,
            IKrProcessRunnerContext runnerContext)
        {
            this.State = NotNullOrThrow(state);
            this.Stage = stage;
            this.RunnerMode = runnerMode;
            this.RunnerContext = NotNullOrThrow(runnerContext);
        }

        #endregion

        #region IStateHandlerContext Members

        /// <inheritdoc />
        public KrProcessState State { get; }

        /// <inheritdoc />
        public Stage? Stage { get; set; }

        /// <inheritdoc />
        public KrProcessRunnerMode RunnerMode { get; }

        /// <inheritdoc />
        public IKrProcessRunnerContext RunnerContext { get; }

        #endregion
    }
}
