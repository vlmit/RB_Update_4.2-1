using System.Collections.Generic;
using Tessa.Platform.Collections;
using Tessa.SaaS;

namespace Tessa.Extensions.Default.Server.SaaS
{
    /// <inheritdoc cref="ISaasMailFromAddressProvider"/>
    public class SaasMailFromAddressProvider : ISaasMailFromAddressProvider
    {
        private readonly HashSet<string, SaasInstanceInfo> instances;

        public SaasMailFromAddressProvider(IEnumerable<SaasInstanceInfo> instances)
        {
            this.instances = new HashSet<string, SaasInstanceInfo>(x => x.Instance, instances);
        }

        /// <inheritdoc/>
        public string GetFromAddress(string instance) => this.instances[instance].Email;
    }
}
