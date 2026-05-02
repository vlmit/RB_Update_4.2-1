#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Tessa.Platform;
using Tessa.Platform.ObjectLocking;
using Tessa.Platform.Redis;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.OnlyOffice
{
    public sealed class OnlyOfficeLockingStrategy(IObjectLockingStrategy objectLockingStrategy) : IOnlyOfficeLockingStrategy
    {
        private readonly IObjectLockingStrategy objectLockingStrategy = NotNullOrThrow(objectLockingStrategy);

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private sealed class Releaser(ObjectLockKey key, bool readerLock, IObjectLockingStrategy objectLockingStrategy) : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                var result = readerLock
                    ? await objectLockingStrategy.ReleaseReaderLockAsync(key)
                    : await objectLockingStrategy.ReleaseWriterLockAsync(key);

                if (!result.IsSuccessful)
                {
                    logger.LogResult(result, ValidationLevel.Detailed,
                        $"Failed to release an OnlyOffice {(readerLock ? "reader" : "writer")} lock: {0}");
                    // do not throw
                }
            }
        }

        private static ObjectLockKey GetLockKey(Guid id) => new(id, RedisLockKeys.OnlyOfficeKey);

        public async Task<IAsyncDisposable> AcquireLockAsync(Guid id, bool readerLock, CancellationToken cancellationToken = default)
        {
            var key = GetLockKey(id);

            var result = readerLock
                ? await this.objectLockingStrategy.ObtainReaderLockAsync(key, cancellationToken: cancellationToken)
                : await this.objectLockingStrategy.ObtainWriterLockAsync(key, cancellationToken: cancellationToken);

            if (!result.IsSuccessful)
            {
                throw new ValidationException(ValidationResult.Aggregate(
                    ValidationResult.FromText($"Failed to acquire an OnlyOffice {(readerLock ? "reader" : "writer")} lock.", ValidationResultType.Error),
                    result));
            }

            return new Releaser(key, readerLock, this.objectLockingStrategy);
        }
    }
}

