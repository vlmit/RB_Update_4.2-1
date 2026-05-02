#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Operations;

namespace Tessa.Test.Default.Shared.Platform.Operations
{
    /// <summary>
    /// Объект, выполняющий операции в тестах.
    /// </summary>
    /// <remarks>Доступен только в серверном контейнере.</remarks>
    public interface ITestOperationExecutor
    {
        /// <summary>
        /// Выполняет операции.
        /// </summary>
        /// <param name="actionAsync">Метод, обрабатывающий операцию. Возвращаемое значение: <see langword="true"/>, если операция была выполнена, иначе - <see langword="false"/>.</param>
        /// <param name="options"><inheritdoc cref="TestOperationExecutorOptions" path="/summary"/></param>
        /// <param name="prepareActionAsync">
        /// Метод, выполняющийся перед началом обработки доступных операций.
        /// Может использоваться для подготовки данных.
        /// </param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        /// <remarks>
        /// Выполняются операции только в состояниях, отличных от <see cref="OperationState.Completed"/>.<para/>
        /// Операции автоматически в работу не берутся. Для взятия операции в работу необходимо выполнить метод <see cref="IOperationRepository.StartAsync(Guid, Guid?, CancellationToken)"/>.
        /// </remarks>
        Task ExecuteOperationsAsync(
            Func<ITestOperationExecutorContext, ValueTask<bool>> actionAsync,
            TestOperationExecutorOptions options,
            Func<ITestOperationExecutorContext, ValueTask>? prepareActionAsync = null,
            CancellationToken cancellationToken = default);
    }
}
