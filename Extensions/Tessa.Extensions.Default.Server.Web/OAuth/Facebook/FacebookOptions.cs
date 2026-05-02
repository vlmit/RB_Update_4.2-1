using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace Tessa.Extensions.Default.Server.Web.OAuth.Facebook
{
    /// <summary>
    /// Набор опций для конфигурирования Facebook-аутентификации посредством OAuth, используемые в <see cref="FacebookHandler"/>.
    /// </summary>
    public sealed class FacebookOptions :
        OAuthOptions
    {
        /// <summary>
        /// Свойство, отражающее значение ключа <c>appsecret_proof</c>, который должен быть сгенерирован и и отправлен при вызове Facebook API.
        /// </summary>
        /// <remarks>
        /// Подробнее см. <see href="https://developers.facebook.com/docs/graph-api/security#appsecret_proof"/>
        /// </remarks>
        /// <value>Значение по умолчанию <see langword="true"/>.</value>
        public bool SendAppSecretProof { get; set; }

        /// <summary>
        /// Список полей, которые необходимо получить из <see cref="OAuthOptions.UserInformationEndpoint"/>.
        /// </summary>
        /// <remarks>
        /// Подробнее см. <see href="https://developers.facebook.com/docs/graph-api/reference/user"/>.
        /// </remarks>
        public ISet<string> Fields { get; } = new HashSet<string>();
    }
}
