using NLog;
using Tessa.Extensions.Default.Server.Plugins;
using Tessa.Platform.Plugins;

namespace Tessa.Extensions.Default.Chronos.Workflow
{
    /// <summary>
    /// Плагин для обработки мобильного согласования.
    /// </summary>
    public sealed class MobileApprovalPlugin : HandlerBasePluginExtension
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override ILogger Logger => logger;

        /// <inheritdoc/>
        protected override string PluginName => DefaultPluginNames.MobileApprovalPlugin;

        #endregion
    }
}
