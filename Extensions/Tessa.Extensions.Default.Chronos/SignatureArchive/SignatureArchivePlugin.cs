using Chronos.Plugins;
using Chronos.Plugins.Base;
using NLog;
using Tessa.Extensions.Default.Server.Plugins;

namespace Tessa.Extensions.Default.Chronos.SignatureArchive
{
    /// <summary>
    /// Плагин, обогащающий подписи файлов до профиля <see cref="Tessa.Platform.EDS.SignatureProfile.A"/>.
    /// </summary>
    [Plugin(
        Name = "Signature archive plugin",
        Description = "Plugin starts at configured time and extend signatures matching conditions within settings to archive profile.",
        Version = 1,
        JsonName = DefaultPluginNames.SignatureArchivePlugin)]
    public sealed class SignatureArchivePlugin :
        HandlerPluginBase
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override string PluginName => DefaultPluginNames.SignatureArchivePlugin;

        /// <inheritdoc/>
        protected override ILogger Logger => logger;

        #endregion
    }
}
