using System.Collections.Generic;
using Tessa.Platform.Storage;
using Tessa.Web.Client.OAuth;

namespace Tessa.Extensions.Default.Server.Web.OAuth.Facebook
{
    /// <summary>
    /// Объект, посредством которого выполняется добавление и настройка
    /// внешней аутентификации по протоколу OAuth для провайдера Facebook.
    /// </summary>
    public sealed class FacebookConfigurator :
        OAuthDefaultConfigurator<FacebookOptions, FacebookHandler>
    {
        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="FacebookConfigurator"/>.
        /// </summary>
        /// <inheritdoc/>
        public FacebookConfigurator(Dictionary<string, object?> provider) : base(provider) { }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void ConfigureOptions(FacebookOptions options)
        {
            base.ConfigureOptions(options);

            options.SendAppSecretProof = this.Provider.TryGet<bool>(nameof(FacebookOptions.SendAppSecretProof));

            if (this.Provider.TryGet<List<object>?>(nameof(FacebookOptions.Fields)) is { Count: > 0 } fields)
            {
                foreach (string field in fields)
                {
                    options.Fields.Add(field);
                }
            }
        }

        #endregion
    }
}
