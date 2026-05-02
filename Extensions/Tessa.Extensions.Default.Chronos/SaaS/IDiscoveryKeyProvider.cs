using Tessa.Discovery;

namespace Tessa.Extensions.Default.Chronos.SaaS
{
    /// <summary>
    /// Provides secret key.
    /// </summary>
    public interface IDiscoveryKeyProvider
    {
        /// <summary>
        /// Get the secret key.
        /// </summary>
        /// <returns>Secret key.</returns>
        DiscoveryKey GetKey();
    }
}
