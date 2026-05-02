namespace Tessa.Extensions.Default.Server.SaaS
{
    /// <summary>
    /// Provider for From address in e-mails.
    /// </summary>
    public interface ISaasMailFromAddressProvider
    {
        /// <summary>
        /// Get from e-mail address for given instance.
        /// </summary>
        /// <param name="instance">Instance name.</param>
        /// <returns>E-mail from address.</returns>
        string GetFromAddress(string instance);
    }
}
