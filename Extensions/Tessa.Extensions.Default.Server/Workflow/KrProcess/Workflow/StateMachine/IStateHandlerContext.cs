#nullable enable

using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.StateMachine
{
    /// <summary>
    /// Контекст <see cref="IStateHandler"/>.
    /// </summary>
    public interface IStateHandlerContext
    {
        /// <inheritdoc cref="KrProcessState" path="/summary"/>
        KrProcessState State { get; }

        /// <summary>
        /// Текущий этап.
        /// </summary>
        Stage? Stage { get; set; }

        /// <inheritdoc cref="KrProcessRunnerMode" path="/summary"/>
        KrProcessRunnerMode RunnerMode { get; }

        /// <inheritdoc cref="IKrProcessRunnerContext" path="/summary"/>
        IKrProcessRunnerContext RunnerContext { get; }
    }
}
