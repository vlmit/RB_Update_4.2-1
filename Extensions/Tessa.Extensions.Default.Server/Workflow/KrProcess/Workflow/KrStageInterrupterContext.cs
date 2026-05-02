#nullable enable

using System;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.StateMachine;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <inheritdoc cref="IKrStageInterrupterContext"/>
    public sealed class KrStageInterrupterContext :
        IKrStageInterrupterContext
    {
        #region Fields

        private Stage? stage;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="directionAfterInterrupt"><inheritdoc cref="DirectionAfterInterrupt" path="/summary"/></param>
        /// <param name="stage"><inheritdoc cref="Stage" path="/summary"/></param>
        /// <param name="runnerContext"><inheritdoc cref="RunnerContext" path="/summary"/></param>
        /// <param name="runnerMode"><inheritdoc cref="RunnerMode" path="/summary"/></param>
        /// <param name="setupNextState"><inheritdoc cref="SetupNextState" path="/summary"/></param>
        public KrStageInterrupterContext(
            DirectionAfterInterrupt directionAfterInterrupt,
            Stage stage,
            IKrProcessRunnerContext runnerContext,
            KrProcessRunnerMode runnerMode,
            Func<bool, KrProcessState>? setupNextState)
        {
            this.DirectionAfterInterrupt = directionAfterInterrupt;
            this.Stage = stage;
            this.RunnerContext = NotNullOrThrow(runnerContext);
            this.RunnerMode = runnerMode;
            this.SetupNextState = setupNextState;
        }

        #endregion

        #region IKrStageInterrupterContext Members

        /// <inheritdoc />
        public DirectionAfterInterrupt DirectionAfterInterrupt { get; }

        /// <inheritdoc />
        public Stage Stage
        {
            get => this.stage!;
            set => this.stage = NotNullOrThrow(value);
        }

        /// <inheritdoc />
        public IKrProcessRunnerContext RunnerContext { get; }

        /// <inheritdoc />
        public KrProcessRunnerMode RunnerMode { get; }

        /// <inheritdoc />
        public Func<bool, KrProcessState>? SetupNextState { get; }

        #endregion
    }
}
