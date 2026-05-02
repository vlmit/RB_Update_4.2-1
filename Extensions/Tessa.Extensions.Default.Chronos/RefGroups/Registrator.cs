using Tessa.Platform.Plugins;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Chronos.RefGroups
{
    [Registrator(Tag = RegistratorTag.ServerPlugin)]
    public sealed class Registrator :
        RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterType<RefGroupsRecalculatePlugin>(new ContainerControlledLifetimeManager())
                ;
        }
        
        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<IPluginExtension, RefGroupsRecalculatePlugin>(x => x
                    .WithOrder(ExtensionStage.Platform, 1)
                    .WithUnity(this.UnityContainer)
                    .WithGroup(PluginGroups.Normal))
                ;
        }
    }
}
