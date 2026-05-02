#nullable enable

using System.Threading.Tasks;
using OtpNet;
using Tessa.Cards.Extensions;

namespace Tessa.Extensions.Default.Server.Login
{
    /// <summary>
    /// Запрос на генерацию нового TOTP-кода для двухфакторной аутентификации.
    /// </summary>
    public class TwoFactorAuthTotpCodeRequest :
        CardRequestExtension
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override Task AfterRequest(ICardRequestExtensionContext context)
        {
            if (context.RequestIsSuccessful
                && context.ValidationResult.IsSuccessful()
                && context.Request.CardID.HasValue
                && context.Response is not null
                && context.Response.Info is { } info)
            {
                var key = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey());
                info["Uri"] = new OtpUri(OtpType.Totp, key, context.Request.CardID.ToString()).ToString();
                info["Key"] = key;
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
