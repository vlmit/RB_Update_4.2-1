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
        Name = "Tasks notifications plugin",
        Description = "Plugin starts at configured time, collects info about users tasks and send emails to users.",
        Version = 1,
        JsonName = DefaultPluginNames.TasksNotificationsPlugin)]
    public sealed class TasksNotificationsPlugin :
        HandlerPluginBase
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override string PluginName => DefaultPluginNames.TasksNotificationsPlugin;

        /// <inheritdoc/>
        protected override ILogger Logger => logger;

        #endregion
    }
}
