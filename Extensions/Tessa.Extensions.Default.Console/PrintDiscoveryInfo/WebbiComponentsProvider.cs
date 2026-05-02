using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Discovery;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Json;
using Tessa.Platform.Storage;
using Tessa.Platform.Web;
using Tessa.Properties.Resharper;
using Tessa.Webbi;
using Unity;

namespace Tessa.Extensions.Default.Console.PrintDiscoveryInfo
{
    public sealed class WebbiComponentsProvider :
        IComponentsProvider
    {
        #region Private Record

        private record WebbiGetComponentsRequest([UsedImplicitly] bool LoadPlugins);

        #endregion

        #region Private Fields

        private readonly IWebProxyFactory webbiProxyFactory;
        private readonly IWebbiConnectionSettings webbiConnectionSettings;
        private readonly IConsoleLogger consoleLogger;
        private readonly DiscoveryKey signingKey;

        #endregion

        #region Constructor

        public WebbiComponentsProvider(
            IWebbiConnectionSettings webbiConnectionSettings,
            [Dependency(nameof(WebbiWebProxyFactory))] IWebProxyFactory webbiProxyFactory,
            IConsoleLogger consoleLogger,
            DiscoveryKey signingKey)
        {
            this.webbiProxyFactory = NotNullOrThrow(webbiProxyFactory);
            this.webbiConnectionSettings = NotNullOrThrow(webbiConnectionSettings);
            this.consoleLogger = NotNullOrThrow(consoleLogger);
            this.signingKey = NotNullOrThrow(signingKey);
        }

        #endregion

        #region IComponentsProvider Implementation

        public async Task<DiscoveryComponentsInfo> GetComponentsAsync(
            bool loadPlugins,
            CancellationToken cancellationToken = default)
        {
            if (this.signingKey.Scopes?.Contains(DiscoveryScopes.DiscoveryInfo, StringComparer.Ordinal) is not true)
            {
                throw new Exception($"Key doesn't have scope \"{DiscoveryScopes.DiscoveryInfo}\"");
            }

            var requestJson = StorageHelper.SerializeToJson(new WebbiGetComponentsRequest(loadPlugins), TessaSerializer.Json);
            var requestJwt = DefaultConsoleHelper.CreateJwtToken(this.signingKey, requestJson, TimeSpan.FromMinutes(3));

            await this.consoleLogger.InfoAsync("Connecting to Webbi ({0}/{1})...", this.webbiConnectionSettings.BaseAddress, this.webbiConnectionSettings.ManagementRoute);

            WebbiWebProxy.GetComponentsResponse response;
            await using (var proxy = await this.webbiProxyFactory.UseProxyAsync<WebbiWebProxy>(cancellationToken: cancellationToken))
            {
                response = await proxy.GetComponentsAsync(requestJwt, cancellationToken);
            }

            var chronosPlugins = new Dictionary<string, PluginState?>();
            if (response.Plugins is not null)
            {
                foreach (var entry in response.Plugins)
                {
                    if (entry.Value is not string entryValue)
                    {
                        continue;
                    }

                    var extendedPluginInfo = StorageHelper.DeserializeFromJson<PluginState>(entryValue, TessaSerializer.Json);
                    chronosPlugins.Add(entry.Key, extendedPluginInfo);
                }
            }

            return new DiscoveryComponentsInfo
            {
                Components = response.Components ?? new List<DiscoveryComponent>(0),
                Plugins = chronosPlugins,
                RedisTime = DateTime.SpecifyKind(response.RedisTime, DateTimeKind.Utc)
            };
        }

        #endregion
    }
}
