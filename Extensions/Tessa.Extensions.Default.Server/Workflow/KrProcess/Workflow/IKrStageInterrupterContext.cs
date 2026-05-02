#nullable enable

using System;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.StateMachine;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <summary>
    /// Контекст <see cref="IKrStageInterrupter"/>.
    /// </summary>
    public interface IKrStageInterrupterContext
    {
        /// <inheritdoc cref="DirectionAfterInterrupt" path="/summary"/>
        DirectionAfterInterrupt DirectionAfterInterrupt { get; }

        /// <summary>
        /// Прерываемый этап.
        /// </summary>
        Stage Stage { get; set; }

        /// <inheritdoc cref="IKrProcessRunnerContext" path="/summary"/>
        IKrProcessRunnerContext RunnerContext { get; }

        /// <inheritdoc cref="KrProcessRunnerMode" path="/summary"/>
        KrProcessRunnerMode RunnerMode { get; }

        /// <summary>
        /// Метод, создающий состояние, следующий за состоянием прерывания.
        /// В качестве параметра передается признак того, было ли прерывание выполнено до конца.
        /// </summary>
        Func<bool, KrProcessState>? SetupNextState { get; }
    }
}
