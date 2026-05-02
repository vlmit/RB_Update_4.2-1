using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.ObjectLocking;
using Tessa.Platform.Redis;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <inheritdoc cref="IKrStageTemplateLockStrategy" />
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="KrStageTemplatesLockStrategy"/>.
    /// </summary>
    /// <param name="transactionLockingStrategy"><inheritdoc cref="IObjectTransactionLockingStrategy" path="/summary"/></param>
    public sealed class KrStageTemplatesLockStrategy(
        [Dependency(ObjectTransactionLockingStrategyNames.WithMultipleWrites)] IObjectTransactionLockingStrategy transactionLockingStrategy) :
        IKrStageTemplateLockStrategy
    {
        #region Constants And Static Fields

        private static readonly ObjectLockKey lockKey = new(Guid.Empty, RedisLockKeys.KrStageTemplatesObjectKey);
        private const int attemptCount = 100;

        #endregion

        #region Fields

        private readonly IObjectTransactionLockingStrategy transactionLockingStrategy = NotNullOrThrow(transactionLockingStrategy);

        #endregion

        #region IKrStageTemplateLockStrategy Implementation

        /// <inheritdoc />
        public Task<ValidationResult> ObtainReaderLockAsync(
            CancellationToken cancellationToken = default) =>
            this.transactionLockingStrategy.ObtainReaderLockAsync(lockKey, attemptCount: attemptCount, cancellationToken: cancellationToken);

        /// <inheritdoc />
        public Task<ValidationResult> ObtainWriterLockAsync(
            CancellationToken cancellationToken = default) =>
            this.transactionLockingStrategy.ObtainWriterLockAsync(lockKey, attemptCount: attemptCount, cancellationToken: cancellationToken);

        #endregion
    }
}
