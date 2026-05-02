#nullable enable

using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.GlobalSignals
{
    /// <summary>
    /// Контекст <see cref="IGlobalSignalHandler"/>.
    /// </summary>
    public interface IGlobalSignalHandlerContext
    {
        /// <summary>
        /// Текущий этап на момент запуска обработки сигналов.
        /// После обработки сигналов может быть неактуальным.
        /// </summary>
        Stage Stage { get; set; }

        /// <inheritdoc cref="IKrProcessRunnerContext" path="/summary"/>
        IKrProcessRunnerContext RunnerContext { get; }

        /// <inheritdoc cref="KrProcessRunnerMode" path="/summary"/>
        KrProcessRunnerMode RunnerMode { get; }
    }
}
