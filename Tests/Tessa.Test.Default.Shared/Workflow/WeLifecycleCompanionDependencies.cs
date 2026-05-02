#nullable enable

using System;
using Tessa.Cards.ComponentModel;
using Tessa.Platform.Operations;
using Tessa.Test.Default.Shared.Platform.Operations;
using Tessa.Workflow;
using Unity;

namespace Tessa.Test.Default.Shared.Workflow
{
    /// <inheritdoc cref="IWeLifecycleCompanionDependencies" path="/summary"/>
    public sealed class WeLifecycleCompanionDependencies :
        IWeLifecycleCompanionDependencies
    {
        #region Fields

        private readonly ICardTransactionStrategy? cardTransactionStrategy;
        private readonly IWorkflowEngineProcessor? workflowEngineProcessor;
        private readonly IWorkflowEngineCache? workflowEngineCache;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="workflowService"><inheritdoc cref="WorkflowService" path="/summary"/></param>
        /// <param name="testOperationExecutor"><inheritdoc cref="TestOperationExecutor" path="/summary"/></param>
        /// <param name="operationHandlerResolver"><inheritdoc cref="IOperationHandlerResolver" path="/summary"/></param>
        /// <param name="unityContainer"><inheritdoc cref="UnityContainer" path="/summary"/></param>
        /// <param name="cardTransactionStrategy"><inheritdoc cref="CardTransactionStrategy" path="/summary"/></param>
        /// <param name="workflowEngineProcessor"><inheritdoc cref="WorkflowEngineProcessor" path="/summary"/></param>
        /// <param name="workflowEngineCache"><inheritdoc cref="WorkflowEngineCache" path="/summary"/></param>
        public WeLifecycleCompanionDependencies(
            IWorkflowService workflowService,
            ITestOperationExecutor testOperationExecutor,
            IOperationHandlerResolver operationHandlerResolver,
            IUnityContainer unityContainer,
            [OptionalDependency] ICardTransactionStrategy? cardTransactionStrategy,
            [OptionalDependency] IWorkflowEngineProcessor? workflowEngineProcessor,
            [OptionalDependency] IWorkflowEngineCache? workflowEngineCache)
        {
            this.WorkflowService = NotNullOrThrow(workflowService);
            this.TestOperationExecutor = NotNullOrThrow(testOperationExecutor);
            this.WorkflowOperationHandler = NotNullOrThrow(operationHandlerResolver).Resolve(OperationTypes.WorkflowEngineAsync);
            this.UnityContainer = NotNullOrThrow(unityContainer);
            this.cardTransactionStrategy = cardTransactionStrategy;
            this.workflowEngineProcessor = workflowEngineProcessor;
            this.workflowEngineCache = workflowEngineCache;
        }

        #endregion

        #region Properties

        /// <inheritdoc/>
        public IWorkflowService WorkflowService { get; }

        /// <inheritdoc/>
        public ITestOperationExecutor TestOperationExecutor { get; }

        /// <inheritdoc/>
        public ICardTransactionStrategy CardTransactionStrategy => this.cardTransactionStrategy ?? throw new NotSupportedException();

        /// <inheritdoc/>
        public IWorkflowEngineProcessor WorkflowEngineProcessor => this.workflowEngineProcessor ?? throw new NotSupportedException();

        /// <inheritdoc/>
        public IWorkflowEngineCache WorkflowEngineCache => this.workflowEngineCache ?? throw new NotSupportedException();

        /// <inheritdoc/>
        public IOperationHandler WorkflowOperationHandler { get; }

        /// <inheritdoc/>
        public IUnityContainer UnityContainer { get; }

        #endregion
    }
}
