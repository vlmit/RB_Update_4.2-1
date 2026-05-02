using NLog;
using Tessa.Extensions.Default.Server.Plugins;
using Tessa.Platform.Plugins;

namespace Tessa.Extensions.Default.Chronos.Notices
{
    /// <summary>
    /// Плагин, выполняющий рассылку уведомлений.
    /// </summary>
    public sealed class MailSenderPlugin :
        HandlerBasePluginExtension
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region IPlugin Members

        /// <inheritdoc/>
        protected override ILogger Logger => logger;

        /// <inheritdoc/>
        protected override string PluginName => DefaultPluginNames.MailSenderPlugin;

        #endregion
    }
}
