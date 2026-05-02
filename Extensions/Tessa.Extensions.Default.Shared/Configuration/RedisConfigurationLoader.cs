#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Configuration;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Configuration
{
    /// <summary>
    /// Загрузчик конфигурации из Redis.
    /// </summary>
    [Order(1)]
    public class RedisConfigurationLoader : IConfigurationItemSourceLoader
    {
        #region Public Constants

        /// <summary>
        /// Константа для регистраций в DI.
        /// </summary>
        public const string Key = "redis";

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public ValueTask<IConfigurationBuilderItemSource?> GetItemSourceAsync(
            Dictionary<string, object?> parameters,
            IConfigurationBuilderContext context,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(parameters);
            ThrowIfNull(context);

            return new(
                parameters.TryGet<object>("connection") is string { Length: > 0 } connectionAlias
                && parameters.TryGet<object>("key") is string { Length: > 0 } configurationKey
                && DefaultConfigurationHelper.TryGetConnectionByAlias(connectionAlias, context) is { Length: > 0 } connectionString
                    ? new SingleConfigurationBuilderItemSource(new RedisConfigurationBuilderItem(configurationKey, connectionString))
                    : null);
        }

        #endregion
    }
}
