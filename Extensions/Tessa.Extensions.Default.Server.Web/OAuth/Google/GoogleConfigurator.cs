using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Tessa.Platform.Storage;
using Tessa.Web.Client.OAuth;
using static Tessa.Web.Client.OAuth.OAuthHelper;

namespace Tessa.Extensions.Default.Server.Web.OAuth.Google
{
    /// <summary>
    /// Объект, посредством которого выполняется добавление и настройка
    /// внешней аутентификации по протоколам OAuth и OpenID Connect для провайдера Google.
    /// </summary>
    public sealed class GoogleConfigurator :
        RemoteAuthenticationConfigurator
    {
        #region Fields

        private readonly RemoteAuthenticationConfigurator configurator;

        #endregion

        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="GoogleConfigurator"/>.
        /// </summary>
        /// <inheritdoc/>
        public GoogleConfigurator(Dictionary<string, object?> provider) : base(provider) =>
            this.configurator = this.Provider.TryGet<bool>(ProviderOptions.UseOpenIdConnect)
                ? new OpenIdDefaultConfigurator<OpenIdDefaultHandler>(provider)
                : new GoogleOAuthConfigurator(provider);

        #endregion

        #region Base Overrides

        public override void AddScheme(AuthenticationBuilder builder) =>
            this.configurator.AddScheme(builder);

        #endregion

        #region Nested Types

        /// <summary>
        /// Объект, посредством которого выполняется добавление и настройка
        /// внешней аутентификации по протоколу OAuth для провайдера Google.
        /// </summary>
        private sealed class GoogleOAuthConfigurator :
            OAuthDefaultConfigurator<GoogleOptions, GoogleHandler>
        {
            #region Constructors

            /// <summary>
            /// Создает экземпляр класса <see cref="GoogleOAuthConfigurator"/>.
            /// </summary>
            /// <inheritdoc/>
            public GoogleOAuthConfigurator(Dictionary<string, object?> provider) : base(provider) { }

            #endregion

            #region Base Overrides

            /// <inheritdoc/>
            protected override void ConfigureOptions(GoogleOptions options)
            {
                base.ConfigureOptions(options);

                options.IncludeGrantedScopes = this.Provider.TryGet<bool?>(nameof(GoogleOptions.IncludeGrantedScopes));
                options.AccessType = this.Provider.TryGet<string?>(nameof(GoogleOptions.AccessType));
                options.LoginHint = this.Provider.TryGet<string?>(nameof(GoogleOptions.LoginHint));
                options.Prompt = this.Provider.TryGet<string?>(nameof(GoogleOptions.Prompt));
            }

            #endregion
        }

        #endregion
    }
}
