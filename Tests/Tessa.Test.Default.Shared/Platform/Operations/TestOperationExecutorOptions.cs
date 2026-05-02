#nullable enable

using System;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared.Platform.Operations
{
    /// <summary>
    /// Параметры обработки операций в <see cref="ITestOperationExecutor"/>.
    /// </summary>
    public sealed class TestOperationExecutorOptions
    {
        #region Constants And Static Fields

        /// <summary>
        /// Таймаут на взятие блокировки на выполнение операции по умолчанию.
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Максимальное число одновременно выполняемых операций по умолчанию.
        /// </summary>
        public const int DefaultMaxParallelThreads = 1;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="operationTypeID"><inheritdoc cref="OperationTypeID" path="/summary"/></param>
        /// <param name="validationResult"><inheritdoc cref="ValidationResult" path="/summary"/></param>
        public TestOperationExecutorOptions(
            Guid operationTypeID,
            IValidationResultBuilder validationResult)
        {
            this.OperationTypeID = operationTypeID;
            this.ValidationResult = NotNullOrThrow(validationResult);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Идентификатор типа выполняемых операций.
        /// </summary>
        public Guid OperationTypeID { get; }

        /// <inheritdoc cref="IValidationResultBuilder" path="/summary"/>
        public IValidationResultBuilder ValidationResult { get; }

        /// <summary>
        /// Значение <see langword="true"/>, если необходимо выполнить операции созданные после выполнения других операций, иначе - <see langword="false"/>, если необходимо выполнить только текущие операции.
        /// </summary>
        public bool ExecuteNewOperations { get; init; }

        /// <summary>
        /// Значение <see langword="true"/>, если необходимо удалить выполненную операцию, иначе - <see langword="false"/>.
        /// </summary>
        /// <remarks>Операция удаляется вне зависимости от наличия ошибок в <see cref="ValidationResult"/>.</remarks>
        public bool DeleteOperation { get; init; }

        /// <summary>
        /// Определяет, разрешено ли получение ошибок при обработке асинхронных операций.
        /// Если получение ошибок запрещено, то обработка операций будет прервана после получения первой ошибки.
        /// </summary>
        public bool AllowErrors { get; init; }

        /// <summary>
        /// Таймаут на взятие блокировки на выполнение операции. Значение по умолчанию: <see cref="DefaultTimeout"/>.
        /// </summary>
        public TimeSpan Timeout { get; init; } = DefaultTimeout;

        /// <summary>
        /// Максимальное число операций, запускаемым параллельно. Значение по умолчанию: <see cref="DefaultMaxParallelThreads"/>.
        /// </summary>
        public int MaxParallelThreads { get; init; } = DefaultMaxParallelThreads;

        #endregion
    }
}
