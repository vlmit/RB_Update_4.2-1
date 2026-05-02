using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Redis;

namespace Tessa.Extensions.Default.Console.PrintLocks
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

            var lockGroups = context.LockGroups;

            var printGroups = lockGroups.Length != 1;
            if (lockGroups.Length == 0)
            {
                await this.Logger.InfoAsync("Resolving lock groups list from Redis...");
                var lockGroupsHashSet = new HashSet<string>();

                var pattern = RedisHelper.GetObjectLockingMatchingExpression(serverCode);
                foreach (var endPoint in connection.GetEndPoints(configuredOnly: true))
                {
                    var server = connection.GetServer(endPoint);
                    await foreach (var key in server.KeysAsync(pattern: pattern).WithCancellation(cancellationToken))
                    {
                        lockGroupsHashSet.Add(key.ToString()[(pattern.Length - 1)..]);
                    }
                }

                lockGroups = lockGroupsHashSet.OrderBy(x => x).ToArray();
            }

            foreach (var lockGroup in lockGroups)
            {
                var locksKey = RedisHelper.GetObjectLockingKey(serverCode, lockGroup);
                var entries = await db.HashGetAllAsync(locksKey);
                if (entries.Length == 0)
                {
                    continue;
                }

                var keys = entries
                    .Select(x => x.Name)
                    .Where(x => !string.IsNullOrEmpty(x));

                foreach (var key in keys)
                {
                    if (printGroups)
                    {
                        await this.Logger.WriteLineAsync("{0}\t{1}", lockGroup, key);
                    }
                    else
                    {
                        await this.Logger.WriteLineAsync(key);
                    }
                }
            }

            return 0;
        }

        #endregion
    }
}
