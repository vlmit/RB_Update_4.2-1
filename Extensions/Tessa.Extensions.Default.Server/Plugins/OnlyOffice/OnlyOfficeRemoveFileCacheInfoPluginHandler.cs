#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using Tessa.Extensions.Default.Server.OnlyOffice;
using Tessa.Platform.Configuration;
using Tessa.Platform.Plugins;

namespace Tessa.Extensions.Default.Server.Plugins.OnlyOffice
{
    /// <summary>
    /// Обработчик плагина <see cref="DefaultPluginNames.OnlyOfficeRemoveFileCacheInfoPlugin"/>.
    /// </summary>
    public sealed class OnlyOfficeRemoveFileCacheInfoPluginHandler : IPluginHandler
    {
        #region Fields

        private readonly IOnlyOfficeFileCacheInfoStrategy cacheInfoStrategy;
        private readonly IConfigurationManager configurationManager;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="cacheInfoStrategy"><inheritdoc cref="IOnlyOfficeFileCacheInfoStrategy" path="/summary"/></param>
        /// <param name="configurationManager"><inheritdoc cref="IConfigurationManager" path="/summary"/></param>
        public OnlyOfficeRemoveFileCacheInfoPluginHandler(
            IOnlyOfficeFileCacheInfoStrategy cacheInfoStrategy,
            IConfigurationManager configurationManager)
        {
            this.cacheInfoStrategy = NotNullOrThrow(cacheInfoStrategy);
            this.configurationManager = NotNullOrThrow(configurationManager);
        }

        #endregion

        #region IPluginHandler Implementation

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(IPluginExecutingContext context)
        {
            ThrowIfNull(context);
            ThrowIfTypeIsNot<OnlyOfficeRemoveFileCacheInfoPluginSettings>(context.Settings);
            var settings = (OnlyOfficeRemoveFileCacheInfoPluginSettings) context.Settings;

            TimeSpan oldestPreviewFilePeriod = settings.OldestPreviewFilePeriod;

            if (oldestPreviewFilePeriod.Ticks > 0L)
            {
                logger.Info("Removing OnlyOffice file cache info older than {0}.", oldestPreviewFilePeriod);

                await this.cacheInfoStrategy
                    .CleanCacheInfoAsync(
                        DateTime.UtcNow.Subtract(oldestPreviewFilePeriod),
                        context.CancellationToken);
            }
            else
            {
                logger.Info("Removing OnlyOffice file cache info: disabled in configuration.");
            }
        }

        /// <inheritdoc/>
        public IPluginSettings ResolveSettings(Dictionary<string, object?>? info = null)
        {
            var settings = new OnlyOfficeRemoveFileCacheInfoPluginSettings(
                DefaultPluginNames.OnlyOfficeRemoveFileCacheInfoPlugin,
                this.configurationManager.Configuration.Settings);

            settings.Deserialize(info ?? []);

            return settings;
        }

        #endregion
    }
}
