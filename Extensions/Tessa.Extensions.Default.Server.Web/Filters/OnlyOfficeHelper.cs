using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Tessa.Extensions.Default.Server.OnlyOffice.Token;
using Tessa.Web;

namespace Tessa.Extensions.Default.Server.Web.Filters
{
    public static class OnlyOfficeHelper
    {
        #region Constants

        public const string ContextSystemKeyPrefix = ".onlyoffice.";

        #endregion

        #region Methods

        /// <summary>
        /// Возвращает значение заголовка <see cref="HeaderNames.Authorization"/> соответствующее используемой схеме проверки подлинности по умолчанию.
        /// </summary>
        /// <param name="headers">Заголовки передаваемые в <see cref="HttpRequest"/> или <see cref="HttpResponse"/>.</param>
        /// <returns>Значение заголовка <see cref="HeaderNames.Authorization"/> соответствующее используемой схеме проверки подлинности по умолчанию или значение по умолчанию для типа, если заголовок <see cref="HeaderNames.Authorization"/> не найден или он не соответствует схеме проверки подлинности по умолчанию <see cref="OnlyOfficeTokenHelper.AuthenticationScheme"/>.</returns>
        public static string? TryGetAuthorizationHeaderValue(this IHeaderDictionary? headers) =>
            headers?.TryGetBearerAuthToken(OnlyOfficeTokenHelper.AuthenticationScheme);

        public static void SetJwtToken(this HttpContext httpContext, OnlyOfficeJwtTokenInfo? token) =>
            httpContext.Items[ContextSystemKeyPrefix + "Token"] = token;

        public static OnlyOfficeJwtTokenInfo? TryGetJwtToken(this HttpContext httpContext) =>
            httpContext.Items.TryGetValue(ContextSystemKeyPrefix + "Token", out object? value) ? (OnlyOfficeJwtTokenInfo?) value : null;

        #endregion
    }
}
