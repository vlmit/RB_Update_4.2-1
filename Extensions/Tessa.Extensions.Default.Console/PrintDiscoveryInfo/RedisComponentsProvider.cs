using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Discovery;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Json;
using Tessa.Platform.Redis;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Console.PrintDiscoveryInfo
{
    public sealed class RedisComponentsProvider :
        IComponentsProvider
    {
        #region Private Fields

        private readonly IRedisConnectionProvider redisConnectionProvider;
        private readonly IRedisConnectionStringCleaner redisConnectionStringCleaner;
        private readonly IDiscoveryComponentStrategy discoveryComponentStrategy;
        private readonly IConsoleLogger consoleLogger;
        private readonly ITessaServerSettings tessaServerSettings;

        #endregion

        #region Constructor

        public RedisComponentsProvider(
            IRedisConnectionProvider redisConnectionProvider,
            IRedisConnectionStringCleaner redisConnectionStringCleaner,
            IDiscoveryComponentStrategy discoveryComponentStrategy,
            IConsoleLogger consoleLogger,
            ITessaServerSettings tessaServerSettings)
        {
            this.redisConnectionProvider = NotNullOrThrow(redisConnectionProvider);
            this.redisConnectionStringCleaner = NotNullOrThrow(redisConnectionStringCleaner);
            this.discoveryComponentStrategy = NotNullOrThrow(discoveryComponentStrategy);
            this.consoleLogger = NotNullOrThrow(consoleLogger);
            this.tessaServerSettings = NotNullOrThrow(tessaServerSettings);
        }

        #endregion

        #region IComponentsProvider Implementation

        public async Task<DiscoveryComponentsInfo> GetComponentsAsync(
            bool loadPlugins,
            CancellationToken cancellationToken = default)
        {
            var serverCode = this.tessaServerSettings.ServerCode;
            var redisConnectionString = this.tessaServerSettings.RedisConnectionString;

            await this.consoleLogger.InfoAsync("Server code: {0}", serverCode);
            await this.consoleLogger.InfoAsync(
                "Redis connection: {0}",
                await this.redisConnectionStringCleaner.CleanConnectionSafeAsync(redisConnectionString, cancellationToken));

            await this.consoleLogger.InfoAsync("Connecting to Redis...");

            var connection = await this.redisConnectionProvider.GetOpenedConnectionAsync(cancellationToken);
            var db = connection.GetDatabase();

            await this.consoleLogger.InfoAsync("Connected to Redis");
            
            var redisTime = await connection.GetRedisTimeAsync();
            var components = await this.discoveryComponentStrategy.GetComponentsAsync(returnOffline: true, cancellationToken: cancellationToken);

            var chronosPlugins = new Dictionary<string, PluginState?>();
            if (loadPlugins)
            {
                var chronosDiscovery = await db.HashGetAllAsync(RedisHelper.GetDiscoveryChronosKey());
                foreach (var entry in chronosDiscovery)
                {
                    var extendedPluginInfo = StorageHelper.DeserializeFromJson<PluginState>(entry.Value!, TessaSerializer.Json);
                    chronosPlugins.Add(entry.Name!, extendedPluginInfo);
                }
            }

            return new DiscoveryComponentsInfo
            {
                Components = components,
                Plugins = chronosPlugins,
                RedisTime = redisTime
            };
        }

        #endregion
    }
}
