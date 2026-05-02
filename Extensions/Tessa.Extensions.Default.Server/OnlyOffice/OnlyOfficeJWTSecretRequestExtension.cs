#nullable enable
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.Extensions;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.OnlyOffice
{
    /// <summary>
    /// Fill files info with current coedit status.
    /// </summary>
    public sealed class OnlyOfficeJWTSecretRequestExtension(ICardCache cardCache) :
        CardRequestExtension
    {
        #region Fields

        private ICardCache cardCache = NotNullOrThrow(cardCache);

        #endregion

        #region Base Overrides

        public override async Task AfterRequest(ICardRequestExtensionContext context)
        {
            if (!context.RequestIsSuccessful)
            {
                return;
            }

            var onlyOfficeSettings = await this.cardCache.Cards.GetAsync(CardHelper.OnlyOfficeSettingsTypeName, context.CancellationToken);
            var jwtSecret = onlyOfficeSettings.GetValue().Sections["OnlyOfficeSettings"].RawFields.Get<string>("JWTSecret");

            context.Response!.Info["jwtSecret"] = jwtSecret;
        }

        #endregion

    }
}
