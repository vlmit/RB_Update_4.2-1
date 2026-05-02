using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Redis;

namespace Tessa.Extensions.Default.Console.DeleteLocks
{
    public sealed class Operation :
        ConsoleOperation<OperationContext>
    {
        #region Private Fields

        private readonly IRedisConnectionProvider redisConnectionProvider;

        private readonly IRedisConnectionStringCleaner redisConnectionStringCleaner;

        private readonly ITessaServerSettings tessaServerSettings;

        #endregion

        #region Constructor

        public Operation(
            IConsoleLogger logger,
            IConsoleSessionManager sessionManager,
            IRedisConnectionProvider redisConnectionProvider,
            IRedisConnectionStringCleaner redisConnectionStringCleaner,
            ITessaServerSettings tessaServerSettings)
            : base(logger, sessionManager)
        {
            this.redisConnectionProvider = NotNullOrThrow(redisConnectionProvider);
            this.redisConnectionStringCleaner = NotNullOrThrow(redisConnectionStringCleaner);
            this.tessaServerSettings = NotNullOrThrow(tessaServerSettings);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task<int> ExecuteAsync(
            OperationContext context,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);
            ThrowIfNull(context.LockGroups);
            ThrowIfNull(context.LockIDs);

            var serverCode = this.tessaServerSettings.ServerCode;
            var redisConnectionString = this.tessaServerSettings.RedisConnectionString;

            await this.Logger.InfoAsync("Server code: {0}", serverCode);
            await this.Logger.InfoAsync(
                "Redis connection: {0}",
                await this.redisConnectionStringCleaner.CleanConnectionSafeAsync(redisConnectionString, cancellationToken));

            await this.Logger.InfoAsync("Connecting to Redis...");

            var connection = await this.redisConnectionProvider.GetOpenedConnectionAsync(cancellationToken);
            var db = connection.GetDatabase();

            await this.Logger.InfoAsync("Connected to Redis");

            if (context.LockGroups.Length == 1 && context.LockIDs.Length > 0)
            {
                await this.Logger.InfoAsync("Deleting locks in group \"{0}\"...", context.LockGroups[0]);
                var locksKey = RedisHelper.GetObjectLockingKey(serverCode, context.LockGroups[0]);
                foreach (var lockID in context.LockIDs)
                {
                    await this.Logger.InfoAsync("{0}", lockID);
                    await db.HashDeleteAsync(locksKey, lockID);
                }
            }
            else if (context.LockGroups.Length > 0)
            {
                foreach (var lockGroup in context.LockGroups)
                {
                    await this.Logger.InfoAsync("Deleting all locks in group \"{0}\"...", lockGroup);
                    await db.KeyDeleteAsync(RedisHelper.GetObjectLockingKey(serverCode, lockGroup));
                }
            }
            else
            {
                await this.Logger.InfoAsync("Deleting all locks in all groups...");
                var success = await RedisHelper.SafeRemoveKeysAsync(
                    db,
                    new List<string>()
                    {
                        RedisHelper.GetObjectLockingMatchingExpression(serverCode)
                    });

                if (!success)
                {
                    await this.Logger.ErrorAsync("Couldn't clean locks keys.");
                    return -1;
                }
            }

            return 0;
        }

        #endregion
    }
}
