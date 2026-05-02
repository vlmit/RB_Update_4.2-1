#nullable enable

using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.GlobalSignals
{
    /// <inheritdoc cref="IGlobalSignalHandlerContext"/>
    public sealed class GlobalSignalHandlerContext :
        IGlobalSignalHandlerContext
    {
        #region Fields

        private Stage? stage;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="stage"><inheritdoc cref="Stage" path="/summary"/></param>
        /// <param name="runnerContext"><inheritdoc cref="RunnerContext" path="/summary"/></param>
        /// <param name="runnerMode"><inheritdoc cref="RunnerMode" path="/summary"/></param>
        public GlobalSignalHandlerContext(
            Stage stage,
            IKrProcessRunnerContext runnerContext,
            KrProcessRunnerMode runnerMode)
        {
            this.Stage = stage;
            this.RunnerContext = NotNullOrThrow(runnerContext);
            this.RunnerMode = runnerMode;
        }

        #endregion

        #region IGlobalSignalHandlerContext Members

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

        #endregion
    }
}
