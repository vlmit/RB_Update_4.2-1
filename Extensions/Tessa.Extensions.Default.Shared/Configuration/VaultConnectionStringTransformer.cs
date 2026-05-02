#nullable enable

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Configuration;
using Tessa.Platform.Storage;
using Unity;
using IHttpClientFactory = Tessa.Platform.Web.IHttpClientFactory;

namespace Tessa.Extensions.Default.Shared.Configuration
{
    /// <summary>
    /// Преобразователь строки подключения с помощью Hashicorp Vault.
    /// </summary>
    [Order(2)]
    public class VaultConnectionStringTransformer(
        [OptionalDependency] IHttpClientFactory? httpClientFactory = null)
        : ConfigurationStorageTransformerBase
    {
        #region Protected Properties

        /// <inheritdoc cref="IHttpClientFactory" path="/summary"/>
        protected IHttpClientFactory? HttpClientFactory { get; } = httpClientFactory;

        #endregion
        
        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask<object?> TransformCoreAsync(
            Dictionary<string, object?> obj,
            IConfigurationBuilderContext context,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(obj);
            ThrowIfNull(context);

            return obj.TryGet<object>(".vault_token") is string { Length: > 0 } token
                && obj.TryGet<object>("secret") is string { Length: > 0 } secret
                && obj.TryGet<object>("template") is string { Length: > 0 } template
                && (this.HttpClientFactory ?? context.ServiceProvider?.TryResolve<IHttpClientFactory>())
                is { } httpClientFactory
                && context.JsonSerializer is { } jsonSerializer
                    ? await this.GetConnectionStringFromVaultAsync(
                        token,
                        secret,
                        template,
                        httpClientFactory,
                        jsonSerializer,
                        context,
                        cancellationToken).ConfigureAwait(false)
                    : obj;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Получает сформированную строку подключения из шаблона и учетной записи роли, полученной из Vault.
        /// </summary>
        /// <param name="token">Токен для подключения к Vault.</param>
        /// <param name="secret">Секрет для получения данных учетной записи роли из Vault.</param>
        /// <param name="template">Шаблон строки подключения.</param>
        /// <param name="httpClientFactory"><inheritdoc cref="IHttpClientFactory" path="/summary"/></param>
        /// <param name="jsonSerializer"><inheritdoc cref="IConfigurationJsonSerializer" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="IConfigurationBuilderContext" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Сформированная строка подключения.</returns>
        protected virtual async ValueTask<string?> GetConnectionStringFromVaultAsync(
            string token,
            string secret,
            string template,
            IHttpClientFactory httpClientFactory,
            IConfigurationJsonSerializer jsonSerializer,
            IConfigurationBuilderContext context,
            CancellationToken cancellationToken = default)
        {
            // Получаем токен из переменной среды, если он так сохранен
            var regex = new Regex("%[\\S].*%$");
            if (regex.IsMatch(token))
            {
                var variableName = token.Replace("%", string.Empty, StringComparison.Ordinal);
                
                // Ищем как в пременных пользователя, так и в переменных системы.
                var tokenFromEnvironmentVariable = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User)
                    ?? Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Machine);

                if (tokenFromEnvironmentVariable is not { Length: > 0 })
                {
                    return null;
                }

                token = tokenFromEnvironmentVariable;
            }
            
            // Получаем HttpClient из контекста или создаем новый, если еще не создан.
            var httpClient = context.Info.TryGet<HttpClient>(DefaultConfigurationHelper.HttpClientKey);
            if (httpClient is null)
            {
                httpClient = httpClientFactory.CreateHttpClient();
                context.Info[DefaultConfigurationHelper.HttpClientKey] = httpClient;
            }
            
            string secretDataString = await DefaultConfigurationHelper.GetStringFromHttpWithHeadersAsync(
                HttpMethod.Get,
                secret,
                httpClient,
                new()
                {
                    { "X-Vault-Token", token }
                },
                cancellationToken);

            if (secretDataString is not { Length: > 0 })
            {
                return null;
            }

            // Парсим полученный секрет
            var secretData = await jsonSerializer
                .DeserializeAsync(secretDataString, context, cancellationToken)
                .ConfigureAwait(false);

            var creds = secretData?.TryGet<Dictionary<string, object?>>("data");
            if (creds is null)
            {
                return null;
            }

            // Достаем логин/пароль для подстановки в шаблон строки подключения
            var userName = creds.TryGet<string>("username");
            var password = creds.TryGet<string>("password");

            return userName is { Length: > 0 } && password is { Length: > 0 }
                ? GetConnectionStringFromTemplate(template, userName, password)
                : null;
        }

        /// <summary>
        /// Формирует строку подключения с помощью логина, пароля, а также указанноно шаблона.
        /// </summary>
        /// <param name="template">Шаблон строки подключения.</param>
        /// <param name="userName">Логин.</param>
        /// <param name="password">Пароль.</param>
        /// <returns>Сформированная строка подключения.</returns>
        protected static string GetConnectionStringFromTemplate(
            string template,
            string userName,
            string password) => template
                .Replace("{username}", userName, StringComparison.Ordinal)
                .Replace("{password}", password, StringComparison.Ordinal);

        #endregion
    }
}
