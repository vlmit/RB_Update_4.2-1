#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Configuration;

namespace Tessa.Extensions.Default.Shared.Configuration
{
    /// <summary>
    /// Объект, соответствующий конфигурационному файлу, содержимое которого загружается из Redis.
    /// </summary>
    /// <param name="configurationKey">Ключ, по которому расположено строковое представление конфигурации в Redis.</param>
    /// <param name="connectionString">Строка подключения к Redis.</param>
    public class RedisConfigurationBuilderItem(
        string configurationKey,
        string connectionString)
        : ConfigurationBuilderItemBase
    {
        #region Private Properties
        
        /// <summary>
        /// Ключ, по которому расположено строковое представление конфигурации в Redis.
        /// </summary>
        private string ConfigurationKey { get; } = NotEmptyOrThrow(configurationKey);

        /// <summary>
        /// Строка подключения к Redis.
        /// </summary>
        private string ConnectionString { get; } = NotNullOrThrow(connectionString);

        #endregion

        #region Public Properties

        /// <inheritdoc/>
        public override string FilePathForError { get; } = nameof(RedisConfigurationBuilderItem);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask<Dictionary<string, object?>?> LoadStorageCoreAsync(
            IConfigurationBuilderContext context,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);
            
            if (context.JsonSerializer is not { } jsonSerializer)
            {
                return null;
            }

            var stringFromRedis = await DefaultConfigurationHelper.GetStringFromRedisAsync(
                this.ConfigurationKey,
                this.ConnectionString,
                context,
                cancellationToken).ConfigureAwait(false);

            return stringFromRedis is { Length: > 0 }
                ? await jsonSerializer
                    .DeserializeAsync(stringFromRedis, context, cancellationToken)
                    .ConfigureAwait(false)
                : null;
        }

        #endregion
    }
}
