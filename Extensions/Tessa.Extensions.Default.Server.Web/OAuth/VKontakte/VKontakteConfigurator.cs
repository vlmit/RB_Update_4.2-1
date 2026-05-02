using System.Collections.Generic;
using Tessa.Platform.Storage;
using Tessa.Web.Client.OAuth;

namespace Tessa.Extensions.Default.Server.Web.OAuth.VKontakte
{
    /// <summary>
    /// Объект, посредством которого выполняется добавление и настройка
    /// внешней аутентификации по протоколу OAuth для провайдера VKontakte.
    /// </summary>
    public sealed class VKontakteConfigurator :
        OAuthDefaultConfigurator<VKontakteOptions, VKontakteHandler>
    {
        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="VKontakteConfigurator"/>.
        /// </summary>
        /// <inheritdoc/>
        public VKontakteConfigurator(Dictionary<string, object?> provider) : base(provider) { }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void ConfigureOptions(VKontakteOptions options)
        {
            base.ConfigureOptions(options);

            options.ApiVersion = NotWhiteSpaceOrThrow(this.Provider.Get<string>(nameof(VKontakteOptions.ApiVersion)));
            options.Display = this.Provider.TryGet<string?>(nameof(VKontakteOptions.Display));

            if (this.Provider.TryGet<List<object>?>(nameof(VKontakteOptions.Fields)) is { Count: > 0 } fields)
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
