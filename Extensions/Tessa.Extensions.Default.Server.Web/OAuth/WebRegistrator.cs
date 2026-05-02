using Tessa.Extensions.Default.Server.Web.OAuth.Facebook;
using Tessa.Extensions.Default.Server.Web.OAuth.Google;
using Tessa.Extensions.Default.Server.Web.OAuth.VKontakte;
using Tessa.Extensions.Default.Server.Web.OAuth.Yandex;
using Tessa.Web.Client.OAuth;
using Tessa.Web.Registrations;

namespace Tessa.Extensions.Default.Server.Web.OAuth
{
    /// <summary>
    /// Регистратор, используемый для конфигурации аутентификации посредством OAuth.
    /// </summary>
    [WebRegistrator]
    public sealed class WebRegistrator :
        WebRegistratorBase
    {
        /// <inheritdoc/>
        public override void InitializeRegistration()
        {
            RemoteAuthenticationRegistry.Instance.TryRegister("Google", provider => new GoogleConfigurator(provider));
            RemoteAuthenticationRegistry.Instance.TryRegister("Yandex", provider => new YandexConfigurator(provider));
            RemoteAuthenticationRegistry.Instance.TryRegister("Facebook", provider => new FacebookConfigurator(provider));
            RemoteAuthenticationRegistry.Instance.TryRegister("VKontakte", provider => new VKontakteConfigurator(provider));
        }
    }
}
