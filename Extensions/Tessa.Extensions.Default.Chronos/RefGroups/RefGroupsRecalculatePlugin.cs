using NLog;
using Tessa.Extensions.Default.Server.Plugins;
using Tessa.Platform.Plugins;

namespace Tessa.Extensions.Default.Chronos.RefGroups
{
    /// <summary>
    /// Плагин для перерасчёта групп ссылок.
    /// </summary>
    public sealed class RefGroupsRecalculatePlugin :
        HandlerBasePluginExtension
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override ILogger Logger => logger;

        /// <inheritdoc/>
        protected override string PluginName => DefaultPluginNames.RefGroupsRecalculatePlugin;

        #endregion
    }
}
