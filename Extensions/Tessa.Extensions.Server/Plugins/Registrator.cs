using Tessa.Platform;
using Tessa.Platform.Plugins;
using Unity;

namespace Tessa.Extensions.Server.Plugins
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<ExamplePluginHandler>()
                ;
        }

        /// <inheritdoc/>
        public override void FinalizeRegistration()
        {
            this.UnityContainer
                .TryResolve<IPluginHandlerResolver>()?
                .Register<ExamplePluginHandler>(ExamplePluginHandler.PluginName)
                ;
        }

        #endregion
    }
}
