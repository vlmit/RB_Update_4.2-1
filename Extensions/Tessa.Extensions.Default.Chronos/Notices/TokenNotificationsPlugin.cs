using Chronos.Plugins;
using Chronos.Plugins.Base;
using NLog;
using Tessa.Extensions.Default.Server.Plugins;

namespace Tessa.Extensions.Default.Chronos.Notices
{
    /// <summary>
    /// Плагин, выполняющий добавление уведомлений о необходимости обновить токен для подписи.
    /// </summary>
    [Plugin(
        Name = "Token notifications plugin",
        Description = "Plugin send messages about token notifications.",
        Version = 1,
        JsonName = DefaultPluginNames.TokenNotificationsPlugin)]
    public sealed class TokenNotificationsPlugin :
        HandlerPluginBase
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override string PluginName => DefaultPluginNames.TokenNotificationsPlugin;

        /// <inheritdoc/>
        protected override ILogger Logger => logger;

        #endregion
    }
}
