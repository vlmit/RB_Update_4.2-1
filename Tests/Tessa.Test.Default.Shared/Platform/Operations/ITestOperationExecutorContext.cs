#nullable enable

using Tessa.Extensions;
using Tessa.Platform.Operations;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared.Platform.Operations
{
    /// <summary>
    /// Контекст <see cref="ITestOperationExecutor"/>.
    /// </summary>
    public interface ITestOperationExecutorContext :
        IExtensionContext
    {
        /// <summary>
        /// Выполняемая операция.
        /// </summary>
        IOperation Operation { get; }

        /// <inheritdoc cref="IValidationResultBuilder" path="/summary"/>
        IValidationResultBuilder ValidationResult { get; }

        /// <inheritdoc cref="IOperationRepository" path="/summary"/>
        IOperationRepository OperationRepository { get; }
    }
}
