using Chronos.Plugins;
using Chronos.Plugins.Base;
using NLog;
using Tessa.Extensions.Default.Server.Plugins;

namespace Tessa.Extensions.Default.Chronos.Notices
{
    /// <summary>
    /// Плагин, выполняющий добавление уведомлений о текущих заданиях пользователя.
    /// </summary>
    [Plugin(
        Name = "Password notifications plugin",
        Description = "Plugin starts at configured time and send emails to users for whom theirs passwords will expire soon.",
        Version = 1,
        JsonName = DefaultPluginNames.PasswordNotificationsPlugin)]
    public sealed class PasswordNotificationsPlugin :
        HandlerPluginBase
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override string PluginName => DefaultPluginNames.PasswordNotificationsPlugin;

        /// <inheritdoc/>
        protected override ILogger Logger => logger;

        #endregion
    }
}
