using Tessa.Discovery;
using Tessa.Extensions.Default.Server.SaaS;

namespace Tessa.Extensions.Default.Chronos.SaaS
{
    public sealed class SaasDiscoveryKeyProvider : IDiscoveryKeyProvider
    {
        private readonly DiscoveryKey key;

        public SaasDiscoveryKeyProvider(DiscoveryKey key) =>
            this.key = NotNullOrThrow(key);

        /// <inheritdoc/>
        public DiscoveryKey GetKey() => this.key;
    }
}
