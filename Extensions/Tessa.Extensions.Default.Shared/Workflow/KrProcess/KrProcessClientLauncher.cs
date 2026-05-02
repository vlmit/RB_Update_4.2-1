#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess
{
    /// <summary>
    /// Объект, запускающий процесс на клиенте, не имеющим доступа к зависимостям определённым в <b>Tessa.UI</b>.
    /// </summary>
    /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
    public sealed class KrProcessClientLauncher(
        ICardRepository cardRepository) :
        KrProcessClientLauncherBase
    {
        #region Nested Types

        /// <summary>
        /// Предоставляет параметры запуска процесса на клиенте, не имеющим доступа к зависимостям определённым в <b>Tessa.UI</b>.
        /// </summary>
        public sealed class SpecificParameters :
            KrProcessClientLauncherBaseSpecificParameters
        {

        }

        #endregion

        #region Fields

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override async Task<IKrProcessLaunchResult> LaunchCoreAsync(
            Dictionary<string, object?> requestInfo,
            ICardExtensionContext? cardContext = null,
            IKrProcessLauncherSpecificParameters? specificParameters = null,
            CancellationToken cancellationToken = default)
        {
            var request = new CardRequest
            {
                RequestType = KrConstants.LaunchProcessRequestType,
                Info = requestInfo,
            };

            var response = await this.cardRepository.RequestAsync(request, cancellationToken);
            return response.GetKrProcessLaunchFullResult();
        }

        #endregion
    }
}
