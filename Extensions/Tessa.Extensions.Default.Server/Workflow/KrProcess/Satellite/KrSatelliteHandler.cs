#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Platform.Server.Cards.Satellites.Handlers;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Satellite
{
    /// <summary>
    /// Обработчик сателлита основного процесса маршрутов.
    /// </summary>
    public sealed class KrSatelliteHandler : SatelliteHandlerBase
    {
        #region Fields

        private readonly IKrTypesCache krTypesCache;

        #endregion

        #region Constructors

        public KrSatelliteHandler(IKrTypesCache krTypesCache)
        {
            this.krTypesCache = NotNullOrThrow(krTypesCache);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override ValueTask<bool> IsMainCardTypeAsync(
            CardType mainCardType,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(mainCardType);

            return KrComponentsHelper.HasBaseAsync(mainCardType.ID, this.krTypesCache, cancellationToken);
        }

        /// <inheritdoc/>
        public override async ValueTask PrepareSatelliteForCreateAsync(
            ICardGetExtensionContext context,
            ISatelliteHandlerContext satelliteContext)
        {
            ThrowIfNull(context);
            ThrowIfNull(satelliteContext);

            var satellite = await satelliteContext.GetSatelliteAsync(context.ValidationResult, context.CancellationToken);
            if (satellite is null)
            {
                return;
            }

            var krApprovalSection = satellite.Sections.GetOrAdd(KrConstants.KrApprovalCommonInfo.Name);
            krApprovalSection.Fields[KrConstants.KrProcessCommonInfo.MainCardID] = satelliteContext.MainCardID;
        }

        #endregion
    }
}
