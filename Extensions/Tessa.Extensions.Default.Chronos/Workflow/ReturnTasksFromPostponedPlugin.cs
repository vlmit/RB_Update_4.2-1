using NLog;
using Tessa.Extensions.Default.Server.Plugins;
using Tessa.Platform.Plugins;

namespace Tessa.Extensions.Default.Chronos.Workflow
{
    /// <summary>
    /// Плагин, возвращающий из отложенного задания, для которых срок откладывания завершился.
    /// </summary>
    public sealed class ReturnTasksFromPostponedPlugin :
        HandlerBasePluginExtension
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override ILogger Logger => logger;

        /// <inheritdoc/>
        protected override string PluginName => DefaultPluginNames.ReturnTasksFromPostponedPlugin;

        #endregion
    }
}
