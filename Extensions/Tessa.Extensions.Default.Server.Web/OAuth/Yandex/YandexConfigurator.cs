using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Tessa.Platform.Storage;
using Tessa.Web.Client.OAuth;

namespace Tessa.Extensions.Default.Server.Web.OAuth.Yandex
{
    /// <summary>
    /// Объект, посредством которого выполняется добавление и настройка
    /// внешней аутентификации по протоколу OAuth для провайдера Yandex.
    /// </summary>
    public sealed class YandexConfigurator :
        OAuthDefaultConfigurator<YandexOptions, YandexHandler>
    {
        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="YandexConfigurator"/>.
        /// </summary>
        /// <inheritdoc/>
        public YandexConfigurator(Dictionary<string, object?> provider) : base(provider) { }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void ConfigureOptions(YandexOptions options)
        {
            base.ConfigureOptions(options);

            options.UserAvatarEndpoint = this.Provider.TryGet<string?>(nameof(YandexOptions.UserAvatarEndpoint));
            options.ForceConfirm = this.Provider.TryGet<bool?>(nameof(YandexOptions.ForceConfirm));
            options.LoginHint = this.Provider.TryGet<string?>(nameof(YandexOptions.LoginHint));
            options.DeviceID = this.Provider.TryGet<string?>(nameof(YandexOptions.DeviceID));
            options.DeviceName = this.Provider.TryGet<string?>(nameof(YandexOptions.DeviceName));
            options.AvatarSize = this.Provider.TryGet<string?>(nameof(YandexOptions.AvatarSize));

            if (this.Provider.TryGet<List<object>?>(nameof(YandexOptions.OptionalScopes)) is { Count: > 0 } scopes)
            {
                foreach (string scope in scopes)
                {
                    options.OptionalScopes.Add(scope);
                }
            }

            options.ClaimActions.MapCustomJson(
                ClaimTypes.Email,
                user =>
                    user.TryGetProperty("emails", out var emails)
                    && emails.GetArrayLength() > 0
                        ? emails[0].GetString()
                        : null);
        }

        #endregion
    }
}
