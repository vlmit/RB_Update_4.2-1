using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tessa.Web.Client.OAuth;

namespace Tessa.Extensions.Default.Server.Web.OAuth.Facebook
{
    /// <summary>
    /// Обработчик Facebook-аутентификации посредством OAuth.
    /// </summary>
    public sealed class FacebookHandler :
        OAuthDefaultHandler<FacebookOptions>
    {
        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="FacebookHandler"/>.
        /// </summary>
        /// <inheritdoc />
        public FacebookHandler(
            IOptionsMonitor<FacebookOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOAuthEventHandler eventHandler)
            : base(options, logger, encoder, eventHandler)
        {
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override async Task<AuthenticationTicket> CreateTicketAsync(
            ClaimsIdentity identity,
            AuthenticationProperties properties,
            OAuthTokenResponse tokens)
        {
            var endpoint = QueryHelpers.AddQueryString(this.Options.UserInformationEndpoint, "access_token", tokens.AccessToken!);
            if (this.Options.SendAppSecretProof)
            {
                endpoint = QueryHelpers.AddQueryString(endpoint, "appsecret_proof", this.GenerateAppSecretProof(tokens.AccessToken!));
            }
            if (this.Options.Fields.Count > 0)
            {
                endpoint = QueryHelpers.AddQueryString(endpoint, "fields", string.Join(',', this.Options.Fields));
            }

            // Отправка запроса посредством Backchannel, чтобы пользователь не видел взаимодействия
            using var response = await this.Backchannel.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, this.Context.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                this.Logger.LogError(
                    "An error occurred while retrieving the user profile: the remote server returned a {0} response with the following payload: {1} {2}.",
                    response.StatusCode,
                    response.Headers.ToString(),
                    await response.Content.ReadAsStringAsync(this.Context.RequestAborted));

                throw new HttpRequestException("An error occurred while retrieving the user profile.");
            }

            var user = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: this.Context.RequestAborted);
            var principal = new ClaimsPrincipal(identity);
            var context = new OAuthCreatingTicketContext(
                principal,
                properties,
                this.Context,
                this.Scheme,
                this.Options,
                this.Backchannel,
                tokens,
                user);

            context.RunClaimActions();

            await this.Events.CreatingTicket(context);
            return new AuthenticationTicket(context.Principal!, context.Properties, this.Scheme.Name);
        }

        /// <inheritdoc />
        protected override string FormatScope(IEnumerable<string> scopes) =>
            // Здесь Facebook отклоняется от спецификации OAuth. Они требуют разделения запятыми, а не пробелами.
            // https://developers.facebook.com/docs/reference/dialogs/oauth
            // http://tools.ietf.org/html/rfc6749#section-3.3
            string.Join(',', scopes);

        #endregion

        #region Private Methods

        private string GenerateAppSecretProof(string accessToken)
        {
            var key = Encoding.ASCII.GetBytes(this.Options.ClientSecret);
            var tokenBytes = Encoding.ASCII.GetBytes(accessToken);
            var hash = HMACSHA256.HashData(key, tokenBytes);
            var builder = new StringBuilder();
            for (var i = 0; i < hash.Length; i++)
            {
                builder.Append(CultureInfo.InvariantCulture, $"{hash[i]:x2}");
            }
            return builder.ToString();
        }

        #endregion
    }
}
