#nullable enable

using Tessa.Cards.ComponentModel;
using Tessa.Platform.Operations;
using Tessa.Test.Default.Shared.Platform.Operations;
using Tessa.Workflow;
using Unity;

namespace Tessa.Test.Default.Shared.Workflow
{
    /// <summary>
    /// Зависимости, используемые в тестах WorkflowEngine.
    /// </summary>
    public interface IWeLifecycleCompanionDependencies
    {
        /// <inheritdoc cref="IWorkflowService" path="/summary"/>
        IWorkflowService WorkflowService { get; }

        /// <inheritdoc cref="ITestOperationExecutor" path="/summary"/>
        ITestOperationExecutor TestOperationExecutor { get; }

        /// <inheritdoc cref="ICardTransactionStrategy" path="/summary"/>
        ICardTransactionStrategy CardTransactionStrategy { get; }

        /// <inheritdoc cref="IWorkflowEngineProcessor" path="/summary"/>
        IWorkflowEngineProcessor WorkflowEngineProcessor { get; }

        /// <inheritdoc cref="IWorkflowEngineCache" path="/summary"/>
        IWorkflowEngineCache WorkflowEngineCache { get; }

        /// <summary>
        /// Обработчик асинхронный операций WorkflowEngine.
        /// </summary>
        IOperationHandler WorkflowOperationHandler { get; }

        /// <inheritdoc cref="IUnityContainer"/>
        IUnityContainer UnityContainer { get; }
    }
}
