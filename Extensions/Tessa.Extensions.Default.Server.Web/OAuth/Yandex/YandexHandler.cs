using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Claims;
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

namespace Tessa.Extensions.Default.Server.Web.OAuth.Yandex
{
    /// <summary>
    /// Обработчик Yandex-аутентификации посредством OAuth.
    /// </summary>
    public sealed class YandexHandler :
        OAuthDefaultHandler<YandexOptions>
    {
        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="YandexHandler"/>.
        /// </summary>
        /// <inheritdoc />
        public YandexHandler(
            IOptionsMonitor<YandexOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOAuthEventHandler eventHandler)
            : base(options, logger, encoder, eventHandler)
        {
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async Task InitializeEventsAsync()
        {
            await base.InitializeEventsAsync();

            this.Events.OnCreatingTicket = context =>
            {
                // Формируем утверждение для получения аватара пользователя.
                // Подробнее см. https://yandex.ru/dev/id/doc/ru/user-information#avatar-access
                if (context.Identity is { } identity
                    && !string.IsNullOrWhiteSpace(this.Options.UserAvatarEndpoint)
                    && identity.FindFirst(claim => claim.Type is "default_avatar_id" or "avatar_id")?.Value is { Length: > 0 } avatarId)
                {
                    if (!this.Options.UserAvatarEndpoint.EndsWith("/"))
                    {
                        this.Options.UserAvatarEndpoint += "/";
                    }

                    var baseUri = new Uri(this.Options.UserAvatarEndpoint, UriKind.Absolute);
                    var avatar = !string.IsNullOrWhiteSpace(this.Options.AvatarSize)
                        ? new Uri(baseUri, $"{avatarId}/{this.Options.AvatarSize}").ToString()
                        : new Uri(baseUri, avatarId).ToString();

                    identity.AddClaim(new Claim(OAuthHelper.ClaimsTypes.Picture, avatar, ClaimValueTypes.String, this.Options.ClaimsIssuer));
                }

                return Task.CompletedTask;
            };
        }

        /// <inheritdoc />
        protected override string BuildChallengeUrl(AuthenticationProperties properties, string redirectUri)
        {
            // Дополнительную информацию см. https://yandex.ru/dev/id/doc/ru/codes/code-url

            // Сначала выполним базовый метод, чтобы получить сформировать URL с основными параметрами
            var baseUrl = base.BuildChallengeUrl(properties, redirectUri);
            var parameters = QueryHelpers.ParseQuery(new Uri(baseUrl).Query);

            // Добавим дополнительные параметры в строку запроса
            this.SetQueryParameter(parameters, properties, OAuthChallengeProperties.ScopeKey, this.FormatScope, this.Options.Scope);
            this.SetQueryParameter(parameters, properties, "force_confirm", this.FormatBoolean, this.Options.ForceConfirm);
            this.SetQueryParameter(parameters, properties, "login_hint", this.Options.LoginHint);
            this.SetQueryParameter(parameters, properties, "device_id", this.Options.DeviceID);
            this.SetQueryParameter(parameters, properties, "device_name", this.Options.DeviceName);

            if (this.Options.OptionalScopes is { Count: > 0 } optionalScope)
            {
                this.SetQueryParameter(parameters, properties, "optional_scope", this.FormatScope, optionalScope);
            }

            // Некоторые свойства удаляются при настройке параметров запроса, поэтому состояние необходимо сбросить
            parameters["state"] = this.Options.StateDataFormat.Protect(properties);

            // Добавляем параметры к конечной точке авторизации
            return QueryHelpers.AddQueryString(this.Options.AuthorizationEndpoint, parameters);
        }

        /// <inheritdoc/>
        protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthCodeExchangeContext context)
        {
            var tokenRequestParameters = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = context.RedirectUri,
                ["code"] = context.Code,
            };

            // PKCE https://tools.ietf.org/html/rfc7636#section-4.5, см. BuildChallengeUrl
            if (context.Properties.Items.TryGetValue(OAuthConstants.CodeVerifierKey, out var codeVerifier))
            {
                tokenRequestParameters.Add(OAuthConstants.CodeVerifierKey, codeVerifier!);
                context.Properties.Items.Remove(OAuthConstants.CodeVerifierKey);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, this.Options.TokenEndpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
            request.Headers.Authorization = this.CreateAuthorizationHeader();
            request.Version = this.Backchannel.DefaultRequestVersion;
            request.Content = new FormUrlEncodedContent(tokenRequestParameters);

            using var response = await this.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, this.Context.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                this.Logger.LogError(
                    "An error occurred while retrieving an access token: the remote server returned a {0} response with the following payload: {1} {2}.",
                    response.StatusCode,
                    response.Headers.ToString(),
                    await response.Content.ReadAsStringAsync(this.Context.RequestAborted));

                return OAuthTokenResponse.Failed(new Exception("An error occurred while retrieving an access token."));
            }

            var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(this.Context.RequestAborted));

            return OAuthTokenResponse.Success(payload);
        }

        #endregion

        #region Private Methods

        private AuthenticationHeaderValue CreateAuthorizationHeader()
        {
            static string? EscapeDataString(string value) =>
                !string.IsNullOrEmpty(value)
                    ? Uri.EscapeDataString(value).Replace("%20", "+", StringComparison.Ordinal)
                    : null;

            var clientId = EscapeDataString(this.Options.ClientId);
            var clientSecret = EscapeDataString(this.Options.ClientSecret);
            var encoded = Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}");
            var credentials = Convert.ToBase64String(encoded);

            return new AuthenticationHeaderValue("Basic", credentials);
        }

        #endregion
    }
}
