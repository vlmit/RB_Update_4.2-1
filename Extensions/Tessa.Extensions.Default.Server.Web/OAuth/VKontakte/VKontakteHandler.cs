using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tessa.Web.Client.OAuth;

namespace Tessa.Extensions.Default.Server.Web.OAuth.VKontakte
{
    /// <summary>
    /// Обработчик VKontakte-аутентификации посредством OAuth.
    /// </summary>
    public sealed class VKontakteHandler :
        OAuthDefaultHandler<VKontakteOptions>
    {
        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="VKontakteHandler"/>.
        /// </summary>
        /// <inheritdoc />
        public VKontakteHandler(
            IOptionsMonitor<VKontakteOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOAuthEventHandler eventHandler)
            : base(options, logger, encoder, eventHandler)
        {
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            // Дополнительную информацию см. https://dev.vk.com/ru/api/access-token/authcode-flow-user

            // Сначала выполним базовый метод, чтобы получить сформировать URL с основными параметрами
            var baseUrl = base.BuildChallengeUrl(properties, redirectUri);
            var parameters = QueryHelpers.ParseQuery(new Uri(baseUrl).Query);

            // Добавим дополнительные параметры в строку запроса
            this.SetQueryParameter(parameters, properties, OAuthChallengeProperties.ScopeKey, this.FormatScope, this.Options.Scope);
            this.SetQueryParameter(parameters, properties, "display", this.Options.Display);

            // Некоторые свойства удаляются при настройке параметров запроса, поэтому состояние необходимо сбросить
            parameters["state"] = this.Options.StateDataFormat.Protect(properties);

            // Добавляем параметры к конечной точке авторизации
            return QueryHelpers.AddQueryString(this.Options.AuthorizationEndpoint, parameters);
        }

        /// <inheritdoc/>
        protected override async Task<AuthenticationTicket> CreateTicketAsync(
            ClaimsIdentity identity,
            AuthenticationProperties properties,
            OAuthTokenResponse tokens)
        {
            var parameters = new Dictionary<string, string?>
            {
                ["access_token"] = tokens.AccessToken,
                ["v"] = this.Options.ApiVersion,
            };

            var endpoint = QueryHelpers.AddQueryString(this.Options.UserInformationEndpoint, parameters);

            if (this.Options.Fields.Count != 0)
            {
                endpoint = QueryHelpers.AddQueryString(endpoint, "fields", string.Join(',', this.Options.Fields));
            }

            using var response = await this.Backchannel.GetAsync(endpoint, this.Context.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                this.Logger.LogError(
                    "An error occurred while retrieving the user profile: the remote server returned a {0} response with the following payload: {1} {2}.",
                    response.StatusCode,
                    response.Headers.ToString(),
                    await response.Content.ReadAsStringAsync(this.Context.RequestAborted));

                throw new HttpRequestException("An error occurred while retrieving the user profile.");
            }

            var root = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: this.Context.RequestAborted);
            var user = root.GetProperty("response")[0];

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

            // Выполнение повторной обработки утверждений для получения утверждения "email" из ответа с токенами
            context.RunClaimActions(tokens.Response!.RootElement);

            await this.Events.CreatingTicket(context);
            return new AuthenticationTicket(context.Principal!, context.Properties, this.Scheme.Name);
        }

        #endregion
    }
}
