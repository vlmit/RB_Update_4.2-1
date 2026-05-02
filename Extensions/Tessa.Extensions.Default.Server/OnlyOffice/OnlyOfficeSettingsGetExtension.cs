#nullable enable

using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.OnlyOffice
{
    /// <summary>
    /// Obfuscate JWT secret
    /// </summary>
    public sealed class OnlyOfficeSettingsGetExtension :
        CardGetExtension
    {
        #region Base Overrides

        public override async Task AfterRequest(ICardGetExtensionContext context)
        {
            if (!context.RequestIsSuccessful
                || context.Request.ServiceType != CardServiceType.Client
                || context.Session.Token?.ApplicationID != ApplicationIdentifiers.WebClient
                || context.Response?.Card.TryGetSections()?.TryGet("OnlyOfficeSettings") is not { } section
                || section.Fields?.TryGet<string>("JWTSecret") is not { } jwtSecret)
            {
                return;
            }

            section.RawFields["JWTSecret"] = ObfuscateSecret(jwtSecret);
        }

        #endregion

        #region Private Methods

        private string ObfuscateSecret(string jwtSecret)
        {
            if (string.IsNullOrEmpty(jwtSecret))
            {
                return jwtSecret;
            }

            if (jwtSecret.Length < 3)
            {
                jwtSecret = $"{jwtSecret}{jwtSecret}{jwtSecret}";
            }

            return $"{jwtSecret[..3]}xxx{jwtSecret[^3..]}";
        }

        #endregion
    }
}
