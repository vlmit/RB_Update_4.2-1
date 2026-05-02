#nullable enable

using System.Threading;
using Tessa.Platform.Operations;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared.Platform.Operations
{
    /// <inheritdoc cref="ITestOperationExecutorContext"/>
    public sealed class TestOperationExecutorContext :
        ITestOperationExecutorContext
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="operation"><inheritdoc cref="Operation" path="/summary"/></param>
        /// <param name="validationResult"><inheritdoc cref="ValidationResult" path="/summary"/></param>
        /// <param name="operationRepository"><inheritdoc cref="OperationRepository" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        public TestOperationExecutorContext(
            IOperation operation,
            IValidationResultBuilder validationResult,
            IOperationRepository operationRepository,
            CancellationToken cancellationToken = default)
        {
            this.Operation = operation;
            this.ValidationResult = validationResult;
            this.OperationRepository = operationRepository;
            this.CancellationToken = cancellationToken;
        }

        #endregion

        #region ITestOperationExecutorContext Members

        /// <inheritdoc/>
        public IOperation Operation { get; }

        /// <inheritdoc/>
        public IValidationResultBuilder ValidationResult { get; }

        /// <inheritdoc/>
        public IOperationRepository OperationRepository { get; }

        /// <inheritdoc/>
        public CancellationToken CancellationToken { get; set; }

        #endregion
    }
}
