using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace Tessa.Extensions.Default.Server.Web.OAuth.VKontakte
{
    /// <summary>
    /// Набор опций для конфигурирования VKontakte-аутентификации посредством OAuth, используемые в <see cref="VKontakteHandler"/>.
    /// </summary>
    public sealed class VKontakteOptions :
        OAuthOptions
    {
        /// <summary>
        /// Указывает тип отображения страницы авторизации.
        /// <para>
        /// Поддерживаются следующие варианты:
        ///   <list type="bullet">
        /// <item>
        ///   <term>page</term>
        ///   <description>
        ///     Форма авторизации в отдельном окне.
        ///   </description>
        /// </item>
        /// <item>
        ///   <term>page</term>
        ///   <description>
        ///     Всплывающее окно.
        ///   </description>
        /// </item>
        /// <item>
        ///   <term>mobile</term>
        ///   <description>
        ///     Авторизация для мобильных устройств (без использования JS).
        ///     Если пользователь авторизуется с мобильного устройства, будет использован тип <c>mobile</c>.
        ///   </description>
        /// </item>
        ///   </list>
        /// </para>
        /// </summary>
        public string? Display { get; set; }

        /// <summary>
        /// Получает список полей, которые необходимо получить из <see cref="OAuthOptions.UserInformationEndpoint"/>.
        /// </summary>
        /// <remarks>
        /// Подробнее см. <see href="https://vk.com/dev/fields"/>.
        /// </remarks>
        public ISet<string> Fields { get; } = new HashSet<string>();

        /// <summary>
        /// Версия VKontakte API.
        /// </summary>
        /// <remarks>
        /// Подробнее см. <see href="https://vk.com/dev/versions"/>.
        /// </remarks>
        public string ApiVersion { get; set; } = string.Empty;
    }
}
