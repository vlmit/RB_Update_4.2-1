using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tessa.Web.Client.OAuth;

namespace Tessa.Extensions.Default.Server.Web.OAuth.Google
{
    /// <summary>
    /// Обработчик Google-аутентификации посредством OAuth.
    /// </summary>
    public sealed class GoogleHandler :
        OAuthDefaultHandler<GoogleOptions>
    {
        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="GoogleHandler"/>.
        /// </summary>
        /// <inheritdoc />
        public GoogleHandler(
            IOptionsMonitor<GoogleOptions> options,
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
            // Дополнительную информацию см. https://developers.google.com/identity/protocols/OAuth2WebServer

            // Сначала выполним базовый метод, чтобы получить сформировать URL с основными параметрами
            var baseUrl = base.BuildChallengeUrl(properties, redirectUri);
            var parameters = QueryHelpers.ParseQuery(new Uri(baseUrl).Query);

            // Добавим дополнительные параметры в строку запроса
            this.SetQueryParameter(parameters, properties, OAuthChallengeProperties.ScopeKey, this.FormatScope, this.Options.Scope);
            this.SetQueryParameter(parameters, properties, "include_granted_scopes", this.FormatBoolean, this.Options.IncludeGrantedScopes);
            this.SetQueryParameter(parameters, properties, "access_type", this.Options.AccessType);
            this.SetQueryParameter(parameters, properties, "login_hint", this.Options.LoginHint);
            this.SetQueryParameter(parameters, properties, "prompt", this.Options.Prompt);

            // Некоторые свойства удаляются при настройке параметров запроса, поэтому состояние необходимо сбросить
            parameters["state"] = this.Options.StateDataFormat.Protect(properties);

            // Добавляем параметры к конечной точке авторизации
            return QueryHelpers.AddQueryString(this.Options.AuthorizationEndpoint, parameters);
        }

        #endregion
    }
}
