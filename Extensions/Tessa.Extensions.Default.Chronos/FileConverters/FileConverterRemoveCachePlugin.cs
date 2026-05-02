using Chronos.Plugins;
using Chronos.Plugins.Base;
using NLog;
using Tessa.Extensions.Default.Server.Plugins;

namespace Tessa.Extensions.Default.Chronos.FileConverters
{
    /// <summary>
    /// Плагин для очистки старых файлов в карточке кэша конвертации.
    /// </summary>
    [Plugin(
        Name = "File converter remove old files cache plugin",
        Description = "Removes old files from the conversion cache card",
        Version = 1,
        JsonName = DefaultPluginNames.FileConverterRemoveCachePlugin)]
    public sealed class FileConverterRemoveCachePlugin :
        HandlerPluginBase
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override ILogger Logger => logger;

        /// <inheritdoc/>
        protected override string PluginName => DefaultPluginNames.FileConverterRemoveCachePlugin;

        #endregion
    }
}
