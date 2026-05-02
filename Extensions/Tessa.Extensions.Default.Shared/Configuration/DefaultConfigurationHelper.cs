#nullable enable

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Configuration;
using Tessa.Platform.Redis;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Configuration
{
    /// <summary>
    /// Вспомогательный класс для преобразований файла конфигурации.
    /// </summary>
    public static class DefaultConfigurationHelper
    {
        #region Public Constants

        /// <summary>
        /// Ключ, по которому в контексте <see cref="IConfigurationBuilderContext"/>
        /// расположен набор подключений к Redis <see cref="RedisConnectionProvider"/> по их алиасам.
        /// </summary>
        public const string RedisConnectionsKey = nameof(RedisConnectionsKey);

        /// <summary>
        /// Ключ, по которому в контексте <see cref="IConfigurationBuilderContext"/>
        /// расположен объект для выполнения HTTP-запросов <see cref="HttpClientKey"/>.
        /// </summary>
        public const string HttpClientKey = nameof(HttpClientKey);

        #endregion

        #region Public Methods

        /// <summary>
        /// Получает строковое представление конфигурации из Redis по указанному ключу
        /// и возвращает настройки в виде словаря.
        /// </summary>
        /// <param name="configurationKey">Ключ, по которому хранится строковое представление конфигурации.</param>
        /// <param name="connectionString">Строка подключения к Redis.</param>
        /// <param name="context"><inheritdoc cref="IConfigurationBuilderContext" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Строковое представление конфигурации из Redis.</returns>
        public static async ValueTask<string?> GetStringFromRedisAsync(
            string configurationKey,
            string connectionString,
            IConfigurationBuilderContext context,
            CancellationToken cancellationToken = default)
        {
            var connections = context.Info.TryGet<Dictionary<string, RedisConnectionProvider>>(RedisConnectionsKey);
            if (connections is null)
            {
                connections = new();
                context.Info[RedisConnectionsKey] = connections;
            }
            
            // если в кэше для строки подключения нет объекта, то создаём его
            if (!connections.TryGetValue(connectionString, out var provider))
            {
                provider = new RedisConnectionProvider(
                    _ => new(connectionString),
                    context.ServiceProvider?.TryResolve<IRedisConnectionStringCleaner>());

                connections[connectionString] = provider;
            }
            
            // открываем подключение к Redis, если оно ещё не открыто для объекта provider
            var connection = await provider.GetOpenedConnectionAsync(false, cancellationToken)
                .ConfigureAwait(false);

            // получаем значение из Redis по ключу redisKey
            return await connection
                .GetDatabase()
                .StringGetAsync(configurationKey)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Получает строку подключения к Redis.
        /// </summary>
        /// <param name="connectionAlias">Алиас строки подключения к Redis, заданный в json-объекте верхнего уровня.</param>
        /// <param name="context"><inheritdoc cref="IConfigurationBuilderContext" path="/summary"/></param>
        /// <returns>Строка подключения к Redis.</returns>
        public static string? TryGetConnectionByAlias(string connectionAlias, IConfigurationBuilderContext context)
        {
            return TryGetConnectionFromStorage(connectionAlias, context.CurrentStorage) ??
                TryGetConnectionFromStorage(connectionAlias, context.Storage);

            static string? TryGetConnectionFromStorage(string alias, Dictionary<string, object?>? storage) =>
                storage?.TryGet<object>(".redis") is Dictionary<string, object?> connections
                && connections.TryGet<object>(alias) is string { Length: not 0 } connectionString
                    ? connectionString
                    : null;
        }
        
        /// <summary>
        /// Получает строку по HTTP-запросу на указанный адрес.
        /// </summary>
        /// <param name="method"><inheritdoc cref="HttpMethod" path="/summary"/></param>
        /// <param name="uri">Адрес, на который отправляется HTTP-запрос.</param>
        /// <param name="httpClient"><inheritdoc cref="HttpClientKey" path="/summary"/></param>
        /// <param name="requestHeaders">Заголовки запроса.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Ответ на HTTP-запрос в виде строки.</returns>
        public static async Task<string> GetStringFromHttpWithHeadersAsync(
            HttpMethod method,
            string uri,
            HttpClient httpClient,
            Dictionary<string, string>? requestHeaders = null,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage();
            request.RequestUri = new(uri);
            request.Method = method;

            if (requestHeaders is { Count: > 0 })
            {
                foreach (var header in requestHeaders)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
