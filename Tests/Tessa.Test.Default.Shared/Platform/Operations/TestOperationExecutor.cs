#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.ObjectLocking;
using Tessa.Platform.Operations;

namespace Tessa.Test.Default.Shared.Platform.Operations
{
    /// <inheritdoc cref="ITestOperationExecutor"/>
    public sealed class TestOperationExecutor :
        ITestOperationExecutor
    {
        #region Nested Types

        private enum ExecuteOperationState
        {
            Executed,
            Ignored,
            Failed,
        }

        #endregion

        #region Constants And Static Fields

        private const string ObjectPrefix = $"{nameof(TestOperationExecutor)}_{nameof(ExecuteOperationsAsync)}";

        #endregion

        #region Fields

        private readonly IDbScope dbScope;
        private readonly TestOperationServerRepository operationRepository;
        private readonly IObjectLockingStrategy objectLockingStrategy;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="operationRepository"><inheritdoc cref="IOperationRepository" path="/summary"/></param>
        /// <param name="objectLockingStrategy"><inheritdoc cref="IObjectLockingStrategy" path="/summary"/></param>
        public TestOperationExecutor(
            IDbScope dbScope,
            TestOperationServerRepository operationRepository,
            IObjectLockingStrategy objectLockingStrategy)
        {
            this.dbScope = NotNullOrThrow(dbScope);
            this.operationRepository = NotNullOrThrow(operationRepository);
            this.objectLockingStrategy = NotNullOrThrow(objectLockingStrategy);
        }

        #endregion

        #region ITestOperationExecutor Members

        /// <inheritdoc/>
        public async Task ExecuteOperationsAsync(
            Func<ITestOperationExecutorContext, ValueTask<bool>> actionAsync,
            TestOperationExecutorOptions options,
            Func<ITestOperationExecutorContext, ValueTask>? prepareActionAsync = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(actionAsync);
            ThrowIfNull(options);

            await using var _ = this.dbScope.CreateNew();
            bool hasExecutedOperations;

            do
            {
                // 1. Получение всех доступных операций.
                var preliminaryOperations = await this.operationRepository.GetAllAsync(options.OperationTypeID, loadEverything: false, cancellationToken);
                if (preliminaryOperations.Count == 0)
                {
                    break;
                }

                hasExecutedOperations = false;

                if (options.MaxParallelThreads == 1)
                {
                    // 2. Последовательных запуск всех доступных операций.
                    foreach (var preliminaryOperation in preliminaryOperations)
                    {
                        var result = await this.ExecuteOperationAsync(
                            preliminaryOperation,
                            actionAsync,
                            options,
                            prepareActionAsync,
                            cancellationToken);

                        switch (result)
                        {
                            case ExecuteOperationState.Executed:
                                hasExecutedOperations = true;
                                prepareActionAsync = null;
                                break;

                            case ExecuteOperationState.Failed:
                                return;
                        }
                    }
                }
                else
                {
                    // 2*. Параллельный запуск всех доступных операций.
                    var hasExecutedOperationsClosure = new[] { false };
                    var isPreparedClosure = new[] { false };
                    var isFailedClosure = new[] { false };

                    using var asyncLock = prepareActionAsync is null ? null : new AsyncLock();

                    var syncPrepareActionAsync = prepareActionAsync is null
                        ? null
                        : new Func<ITestOperationExecutorContext, ValueTask>(async context =>
                        {
                            if (isPreparedClosure[0])
                            {
                                return;
                            }

                            using var __ = await asyncLock!.EnterAsync(context.CancellationToken);

                            if (isPreparedClosure[0])
                            {
                                return;
                            }

                            await prepareActionAsync(context);

                            isPreparedClosure[0] = true;
                        });

                    await Parallel.ForEachAsync(
                        preliminaryOperations,
                        new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = options.MaxParallelThreads },
                        async (preliminaryOperation, ct) =>
                        {
                            if (isFailedClosure[0])
                            {
                                return;
                            }

                            await using var __ = this.dbScope.CreateNew();

                            var result = await this.ExecuteOperationAsync(
                                preliminaryOperation,
                                actionAsync,
                                options,
                                syncPrepareActionAsync,
                                ct);

                            switch (result)
                            {
                                case ExecuteOperationState.Executed:
                                    hasExecutedOperationsClosure[0] = true;
                                    break;

                                case ExecuteOperationState.Failed:
                                    isFailedClosure[0] = true;
                                    return;
                            }
                        });

                    if (isFailedClosure[0])
                    {
                        return;
                    }

                    hasExecutedOperations = hasExecutedOperationsClosure[0];
                }
            }
            // 3. После выполнения операции могли быть созданы новые. Повторяем выполнение.
            while (hasExecutedOperations && options.ExecuteNewOperations);
        }

        #endregion

        #region Private Methods

        private static bool CheckOperationCondition(
            IOperation operation) =>
            operation.State != OperationState.Completed;

        private async ValueTask<ExecuteOperationState> ExecuteOperationAsync(
            IOperation preliminaryOperation,
            Func<ITestOperationExecutorContext, ValueTask<bool>> actionAsync,
            TestOperationExecutorOptions options,
            Func<ITestOperationExecutorContext, ValueTask>? prepareActionAsync,
            CancellationToken cancellationToken)
        {
            // 1. Операция может быть выполнена?
            if (!CheckOperationCondition(preliminaryOperation))
            {
                return ExecuteOperationState.Ignored;
            }

            // 2. Взятие блокировки на операцию. Между получением и запуском операция может быть выполнена/изменена в параллельном тесте.
            var (attemptCount, retryTimeout) = TestHelper.GetRetryParameters(options.Timeout);

            var lockKey = new ObjectLockKey(preliminaryOperation.ID, ObjectPrefix);
            var result = await this.objectLockingStrategy.ObtainWriterLockAsync(
                lockKey,
                attemptCount,
                retryTimeout,
                cancellationToken);

            options.ValidationResult.Add(result);

            if (!result.IsSuccessful)
            {
                return ExecuteOperationState.Failed;
            }

            try
            {
                // 3. Получение операции. Между получением и запуском операция может быть выполнена/изменена в параллельном тесте.
                var operation = await this.operationRepository.TryGetAsync(
                    preliminaryOperation.ID,
                    true,
                    cancellationToken);

                if (operation is null)
                {
                    return ExecuteOperationState.Ignored;
                }

                // 4. Операция может быть выполнена?
                if (!CheckOperationCondition(operation))
                {
                    return ExecuteOperationState.Ignored;
                }

                var context = new TestOperationExecutorContext(
                    operation,
                    options.ValidationResult,
                    this.operationRepository,
                    cancellationToken);

                // 5. Подготовка общих данных.
                if (prepareActionAsync is not null)
                {
                    await prepareActionAsync(context);
                }

                // 6. Обработка операции.
                var hasExecutedOperation = false;

                try
                {
                    hasExecutedOperation = await actionAsync(context);
                }
                finally
                {
                    if (hasExecutedOperation
                        && options.DeleteOperation)
                    {
                        await this.operationRepository.DeleteAsync(
                            operation.ID,
                            operation.TypeID,
                            CancellationToken.None);
                    }
                }

                if (hasExecutedOperation)
                {
                    return ExecuteOperationState.Executed;
                }
            }
            finally
            {
                result = await this.objectLockingStrategy.ReleaseWriterLockAsync(lockKey);
                options.ValidationResult.Add(result);
            }

            return options.AllowErrors
                || options.ValidationResult.IsSuccessful()
                ? ExecuteOperationState.Ignored
                : ExecuteOperationState.Failed;
        }

        #endregion
    }
}
