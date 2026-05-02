#nullable enable
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;
using Tessa.Platform;
using Tessa.Platform.Web;
using IHttpClientFactory = Tessa.Platform.Web.IHttpClientFactory;

namespace Tessa.Test.Default.Shared.Web
{
    /// <summary>
    /// Фабрика объектов <see cref="HttpClient"/>, настроенных для подключения к <see cref="TestServer"/>.
    /// </summary>
    public class TestServerHttpClientFactory :
        Tessa.Platform.Web.IHttpClientFactory
    {
        #region Fields

        private readonly IWebApplicationFactory webApplicationFactory;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="TestServerHttpClientFactory"/>.
        /// </summary>
        /// <param name="webApplicationFactory">
        /// Класс, предоставляющий методы для создания тестового сервера, предназначенного для тестирования web-приложений.
        /// </param>
        public TestServerHttpClientFactory(IWebApplicationFactory webApplicationFactory)
        {
            this.webApplicationFactory = NotNullOrThrow(webApplicationFactory);
        }

        #endregion

        #region IHttpClientFactory Members

        /// <inheritdoc/>
        public HttpClient CreateHttpClient(IHttpClientCreationOptions? options = null, bool windowsAuth = false)
        {
            var client = this.webApplicationFactory.CreateClient();

            if (options?.Timeout is { } timeout)
            {
                client.Timeout = timeout;
            }

            return client;
        }

        #endregion
    }
}
