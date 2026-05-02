using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Discovery;
using Tessa.Discovery.Senders;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Json;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Platform.Web;
using Tessa.Properties.Resharper;
using Tessa.Webbi;
using Unity;

namespace Tessa.Extensions.Default.Console.PrintLocks
{
    public sealed class OperationClient :
        ConsoleOperation<OperationContext>
    {
        #region Private Records

        [UsedImplicitly]
        private record LocksRequest(string ServerCode, string[] LockGroups);

        #endregion
        
        #region Fields

        private readonly IWebProxyFactory webbiProxyFactory;
        private readonly IDiscoveryKeySerializer keySerializer;
        private readonly IWebbiConnectionSettings webbiConnectionSettings;
        private readonly ITessaServerSettings tessaServerSettings;

        #endregion

        #region Constructor

        public OperationClient(
            IConsoleLogger logger,
            IConsoleSessionManager sessionManager,
            IDiscoveryKeySerializer keySerializer,
            IWebbiConnectionSettings webbiConnectionSettings,
            ITessaServerSettings tessaServerSettings,
            [Dependency(nameof(WebbiWebProxyFactory))] IWebProxyFactory webbiProxyFactory)
            : base(logger, sessionManager, false)
        {
            this.webbiProxyFactory = NotNullOrThrow(webbiProxyFactory);
            this.keySerializer = NotNullOrThrow(keySerializer);
            this.webbiConnectionSettings = NotNullOrThrow(webbiConnectionSettings);
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
            ThrowIfNullOrWhiteSpace(context.KeyPath);
            ThrowIfNullOrWhiteSpace(context.KeyPassword);

            var signingKey = await DiscoverySenderHelper.LoadKeyAsync(
                this.keySerializer,
                context.KeyPath,
                context.KeyPassword,
                cancellationToken);
            ThrowIfNull(signingKey);

            var requestJson = StorageHelper.SerializeToJson(new LocksRequest(this.tessaServerSettings.ServerCode, context.LockGroups), TessaSerializer.Json);
            var requestJwt = DefaultConsoleHelper.CreateJwtToken(signingKey, requestJson, TimeSpan.FromMinutes(3));

            await this.Logger.InfoAsync("Connecting to Webbi ({0}/{1})...", this.webbiConnectionSettings.BaseAddress, this.webbiConnectionSettings.ManagementRoute);

            await using var proxy = await this.webbiProxyFactory.UseProxyAsync<WebbiWebProxy>(cancellationToken: cancellationToken);
            Dictionary<string, string[]>? results;
            try
            {
                results = await proxy.GetLocksAsync(requestJwt, cancellationToken);
            }
            catch (ValidationException e)
            {
                await this.Logger.LogExceptionAsync("Invalid get locks request.", e);
                return -7;
            }

            if (results.Keys.Count == 0)
            {
                return 0;
            }

            var lockGroups = context.LockGroups;
            if (lockGroups.Length == 0)
            {
                lockGroups = results.Keys.OrderBy(x => x).ToArray();
            }

            var printGroups = lockGroups.Length > 1;
            foreach (var lockGroup in lockGroups)
            {
                if (!results.TryGetValue(lockGroup, out var keys))
                {
                    continue;
                }

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
