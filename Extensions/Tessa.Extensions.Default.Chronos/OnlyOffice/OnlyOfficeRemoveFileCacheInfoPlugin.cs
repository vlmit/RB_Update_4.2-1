using NLog;
using Tessa.Extensions.Default.Server.Plugins;
using Tessa.Platform.Plugins;

namespace Tessa.Extensions.Default.Chronos.OnlyOffice
{
    /// <summary>
    /// Плагин очистки файлового кэша конвертации.
    /// </summary>
    public sealed class OnlyOfficeRemoveFileCacheInfoPlugin :
        HandlerBasePluginExtension
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override ILogger Logger => logger;

        /// <inheritdoc/>
        protected override string PluginName => DefaultPluginNames.OnlyOfficeRemoveFileCacheInfoPlugin;

        #endregion
    }
}
