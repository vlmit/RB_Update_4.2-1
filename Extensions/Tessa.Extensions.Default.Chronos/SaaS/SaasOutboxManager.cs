using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Plugins.Notices;
using Tessa.Platform.Data;
using Tessa.SaaS;

namespace Tessa.Extensions.Default.Chronos.SaaS
{
    /// <summary>
    /// Specialized for SaaS outbox manager.
    /// </summary>
    public class SaasOutboxManager : OutboxManager
    {
        #region Fields

        private readonly IClusterInformationService saasInfoService;

        #endregion
        
        #region Constructor

        public SaasOutboxManager(IClusterInformationService saasInfoService, IDbScope dbScope) :
            base(dbScope)
        {
            this.saasInfoService = NotNullOrThrow(saasInfoService);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override bool InSaas => true;

        /// <inheritdoc/>
        protected override async ValueTask<IReadOnlyCollection<string>> GetSaasInstanceNamesAsync(CancellationToken cancellationToken = default)
        {
            var infos = await this.saasInfoService.GetInstancesInfoAsync(cancellationToken);
            return infos.Select(x => x.Instance).ToArray();
        }

        #endregion
    }
}
