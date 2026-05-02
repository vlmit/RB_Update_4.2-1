using System;
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

namespace Tessa.Extensions.Default.Console.DeleteLocks
{
    public sealed class OperationClient :
        ConsoleOperation<OperationContext>
    {
        #region Private Records

        [UsedImplicitly]
        private record DeleteRequest(string ServerCode, string[] LockIDs, string[] LockGroups);

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
            ThrowIfNull(context.LockIDs);
            ThrowIfNullOrWhiteSpace(context.KeyPath);
            ThrowIfNullOrWhiteSpace(context.KeyPassword);

            var signingKey = await DiscoverySenderHelper.LoadKeyAsync(
                this.keySerializer,
                context.KeyPath,
                context.KeyPassword,
                cancellationToken);
            ThrowIfNull(signingKey);

            var requestJson = StorageHelper.SerializeToJson(
                new DeleteRequest(this.tessaServerSettings.ServerCode, context.LockIDs, context.LockGroups),
                TessaSerializer.Json);
            var requestJwt = DefaultConsoleHelper.CreateJwtToken(signingKey, requestJson, TimeSpan.FromMinutes(3));

            await this.Logger.InfoAsync("Connecting to Webbi ({0}/{1})...", this.webbiConnectionSettings.BaseAddress, this.webbiConnectionSettings.ManagementRoute);

            await using var proxy = await this.webbiProxyFactory.UseProxyAsync<WebbiWebProxy>(cancellationToken: cancellationToken);
            try
            {
                await proxy.DeleteLocksAsync(requestJwt, cancellationToken);
            }
            catch (ValidationException e)
            {
                await this.Logger.LogExceptionAsync("Invalid delete request.", e);
                return -6;
            }

            if (context.LockGroups.Length == 1 && context.LockIDs.Length > 0)
            {
                await this.Logger.InfoAsync("Locks in group \"{0}\" deleted:", context.LockGroups[0]);
                foreach (var lockID in context.LockIDs)
                {
                    await this.Logger.InfoAsync("{0}", lockID);
                }
            }
            else if (context.LockGroups.Length > 0)
            {
                foreach (var lockGroup in context.LockGroups)
                {
                    await this.Logger.InfoAsync("All locks in group \"{0}\" deleted.", lockGroup);
                }
            }
            else
            {
                await this.Logger.InfoAsync("All locks in all groups deleted.");
            }

            return 0;
        }

        #endregion
    }
}
