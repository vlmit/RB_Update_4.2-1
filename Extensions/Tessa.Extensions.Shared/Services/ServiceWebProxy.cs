using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Tessa.Cards;
using Tessa.Platform.Runtime;
using Tessa.Platform.Web;

namespace Tessa.Extensions.Shared.Services
{
    /// <summary>
    /// Прокси-класс для обращения к методам контроллера Tessa.Extensions.Server.Web/Controllers/ServiceController.
    /// Все методы в нём асинхронные, но аналогичны методам сервиса <see cref="IService"/>.
    /// </summary>
    public sealed class ServiceWebProxy :
        WebProxy
    {
        #region Constructors

        /*
         * Базовому конструктору передаётся путь к контроллеру, задаваемый в атрибуте [Route("...")] на классе контроллера.
         */
        /// <doc path='info[@type="class" and @item=".ctor"]'/>
        public ServiceWebProxy()
            : base(
                controllerRoute: "service",
                serviceName: RuntimeHelper.DefaultServiceName,
                new[]
                {
                    WebRequestFlags.AddAcceptLanguageHeader
                })
        {
        }

        #endregion

        #region Methods

        /*
         * Флаг RequestFlags.IgnoreSession запрещает передавать токен сессии в HTTP-заголовке "Tessa-Session".
         * Это актуально в методах логина, при передаче токена в параметре или при вызове методов, которым не требуется логин.
         */
        public async Task<string> LoginAsync(IntegrationLoginParameters parameters, CancellationToken cancellationToken = default) =>
            NotNullOrThrow(await this.SendAsync<string>(
                HttpMethod.Post,
                "login",
                WebRequestFlags.PerRequest() + WebRequestFlags.JsonRequest,
                parameters,
                cancellationToken: cancellationToken).ConfigureAwait(false));

        public async Task LogoutAsync(string token, CancellationToken cancellationToken = default) =>
            await this.SendAsync<Void>(
                HttpMethod.Post,
                $"logout?token={HttpUtility.UrlEncode(token)}",
                cancellationToken: cancellationToken).ConfigureAwait(false);

        public async Task<string> GetDataAsync(string parameter, CancellationToken cancellationToken = default) =>
            await this.SendAsync<string>(
                HttpMethod.Get,
                $"data?p={HttpUtility.UrlEncode(parameter)}",
                WebRequestFlags.PerRequest() + WebRequestFlags.AddSessionHeader,
                cancellationToken: cancellationToken).ConfigureAwait(false) ?? string.Empty;

        public async Task<string> GetDataWhenTokenInParameterAsync(string token, string parameter, CancellationToken cancellationToken = default) =>
            await this.SendAsync<string>(
                HttpMethod.Get,
                $"data?p={HttpUtility.UrlEncode(parameter)}&token={HttpUtility.UrlEncode(token)}",
                cancellationToken: cancellationToken).ConfigureAwait(false) ?? string.Empty;

        public async Task<string> GetDataWithoutCheckingTokenAsync(string parameter, CancellationToken cancellationToken = default) =>
            await this.SendAsync<string>(
                HttpMethod.Get,
                $"data-without-login?p={HttpUtility.UrlEncode(parameter)}",
                cancellationToken: cancellationToken).ConfigureAwait(false) ?? string.Empty;

        /*
         * Для передачи потоковых данных в метод используйте единственный параметр типа Stream.
         * Для получения потоковых данных от сервера возвращайте значение Task<Stream>.
         */
        public async Task<CardGetResponse> GetCardAsync(CardGetRequest request, CancellationToken cancellationToken = default) =>
            NotNullOrThrow(await this.SendAsync<CardGetResponse>(
                HttpMethod.Post,
                "cards/get",
                WebRequestFlags.PerRequest() + WebRequestFlags.AddSessionHeader + WebRequestFlags.TypedJsonRequest + WebRequestFlags.TypedJsonResponse,
                request,
                cancellationToken: cancellationToken).ConfigureAwait(false));

        /*
         * Пример передачи параметров через адресную строку.
         */
        public async Task<CardGetResponse> GetCardByIDAsync(Guid cardID, string? cardTypeName = null, CancellationToken cancellationToken = default) =>
            NotNullOrThrow(await this.SendAsync<CardGetResponse>(
                HttpMethod.Get,
                $"cards/{cardID}" + (string.IsNullOrEmpty(cardTypeName) ? null : $"?type={HttpUtility.UrlEncode(cardTypeName)}"),
                WebRequestFlags.PerRequest() + WebRequestFlags.AddSessionHeader + WebRequestFlags.TypedJsonResponse,
                cancellationToken: cancellationToken).ConfigureAwait(false));

        /// <summary>
        /// Возвращает <c>ValidationResult</c> с <c>ValidationKeys.OperationIsUnavailable</c>
        /// и кодом <c>HttpStatusCode.Forbidden</c>  - 403.
        /// </summary>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Данные, возвращаемые методом.</returns>
        public async Task GetValidationResultErrorAsync(CancellationToken cancellationToken = default) =>
            await this.SendAsync<Void>(
                HttpMethod.Get,
                "validation-result-error",
                cancellationToken: cancellationToken).ConfigureAwait(false);

        #endregion
    }
}
