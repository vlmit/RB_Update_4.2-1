#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using Tessa.FileConverters;
using Tessa.Platform;
using Tessa.Platform.Configuration;
using Tessa.Platform.Plugins;

namespace Tessa.Extensions.Default.Server.Plugins.FileConverters
{
    /// <summary>
    /// Обработчик плагина <see cref="DefaultPluginNames.FileConverterRemoveCachePlugin"/>.
    /// </summary>
    public sealed class FileConverterRemoveCachePluginHandler : IPluginHandler
    {
        #region Fields

        private readonly IFileConverterCache fileConverterCache;
        private readonly IConfigurationManager configurationManager;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="fileConverterCache"><inheritdoc cref="IFileConverterCache" path="/summary"/></param>
        /// <param name="configurationManager"><inheritdoc cref="IConfigurationManager" path="/summary"/></param>
        public FileConverterRemoveCachePluginHandler(
            IFileConverterCache fileConverterCache,
            IConfigurationManager configurationManager)
        {
            this.fileConverterCache = NotNullOrThrow(fileConverterCache);
            this.configurationManager = NotNullOrThrow(configurationManager);
        }

        #endregion

        #region IPluginHandler Implementation

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(IPluginExecutingContext context)
        {
            ThrowIfNull(context);
            ThrowIfTypeIsNot<FileConverterRemoveCachePluginSettings>(context.Settings);
            var settings = (FileConverterRemoveCachePluginSettings) context.Settings;

            TimeSpan oldestPreviewFilePeriod = settings.OldestPreviewFilePeriod;

            if (oldestPreviewFilePeriod.Ticks > 0L)
            {
                try
                {
                    logger.Info("Removing file conversion cache.");

                    var cleanResult = await this.fileConverterCache.CleanCacheAsync(
                        DateTime.UtcNow.Subtract(oldestPreviewFilePeriod),
                        context.CancellationToken);

                    logger.LogResult(cleanResult);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogException(ex, LogLevel.Warn);
                }
                finally
                {
                    logger.Info("File conversion cache removed.");
                }
            }
            else
            {
                logger.Info("Removing file conversion cache disabled in configuration.");
            }
        }

        /// <inheritdoc/>
        public IPluginSettings ResolveSettings(Dictionary<string, object?>? info = null)
        {
            var settings = new FileConverterRemoveCachePluginSettings(
                DefaultPluginNames.FileConverterRemoveCachePlugin,
                this.configurationManager.Configuration.Settings);

            settings.Deserialize(info ?? []);

            return settings;
        }

        #endregion
    }
}
