using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.ObjectLocking;
using Tessa.Platform.Redis;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions
{
    /// <inheritdoc cref="IKrPermissionsLockStrategy" />
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="KrPermissionsLockStrategy"/>.
    /// </summary>
    /// <param name="transactionLockingStrategy"><inheritdoc cref="IObjectTransactionLockingStrategy" path="/summary"/></param>
    /// <param name="lockingStrategy"><inheritdoc cref="IReadersWritersObjectLockingStrategy" path="/summary"/></param>
    public sealed class KrPermissionsLockStrategy(
        [Dependency(nameof(ReadersWritersObjectLockingStrategy))] IObjectTransactionLockingStrategy transactionLockingStrategy,
        IReadersWritersObjectLockingStrategy lockingStrategy) :
        IKrPermissionsLockStrategy
    {
        #region Constants And Static Fields

        private static readonly ObjectLockKey lockKey = new (Guid.Empty, RedisLockKeys.KrPermissionsObjectKey);

        #endregion

        #region Fields

        private readonly IObjectTransactionLockingStrategy transactionLockingStrategy = NotNullOrThrow(transactionLockingStrategy);
        private readonly IReadersWritersObjectLockingStrategy lockingStrategy = NotNullOrThrow(lockingStrategy);

        #endregion

        #region IKrPermissionsLockStrategy Implementation

        /// <inheritdoc />
        public Task<ValidationResult> ObtainReaderLockAsync(
            CancellationToken cancellationToken = default) =>
            this.transactionLockingStrategy.ObtainReaderLockAsync(lockKey, cancellationToken: cancellationToken);

        /// <inheritdoc />
        public Task<ValidationResult> ObtainWriterLockAsync(
            CancellationToken cancellationToken = default) =>
            this.transactionLockingStrategy.ObtainWriterLockAsync(lockKey, cancellationToken: cancellationToken);

        /// <inheritdoc />
        public Task ClearLocksAsync() =>
            this.lockingStrategy.ClearLocksAsync(lockKey);

        #endregion
    }
}
