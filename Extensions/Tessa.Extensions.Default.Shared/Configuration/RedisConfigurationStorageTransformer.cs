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
    /// Преобразователь конфигурации значениями, полученными из Redis.
    /// </summary>
    [Order(1)]
    public class RedisConfigurationStorageTransformer : ConfigurationStorageTransformerBase
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask<object?> TransformCoreAsync(
            Dictionary<string, object?> obj,
            IConfigurationBuilderContext context,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(obj);
            ThrowIfNull(context);

            return obj.TryGet<object>(".redis") is string { Length: > 0 } connectionAlias
                && obj.TryGet<object>("key") is string { Length: > 0 } configKey
                && DefaultConfigurationHelper.TryGetConnectionByAlias(connectionAlias, context) is { Length: > 0 } connectionString
                    ? await DefaultConfigurationHelper.GetStringFromRedisAsync(
                        configKey,
                        connectionString,
                        context,
                        cancellationToken).ConfigureAwait(false)
                    : obj;
        }

        #endregion
    }
}
