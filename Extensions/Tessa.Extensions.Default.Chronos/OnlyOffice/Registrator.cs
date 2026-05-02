using Tessa.Extensions.Default.Chronos.FileConverters;
using Tessa.Platform.Configuration;
using Tessa.Platform.Plugins;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Chronos.OnlyOffice
{
    [Registrator(Tag = RegistratorTag.ServerPlugin)]
    public sealed class Registrator :
        RegistratorBase
    {
        public override void RegisterUnity()
        {
            if (!this.UnityContainer.IsRegistered<IFileConverterSettings>())
            {
                this.UnityContainer
                    .RegisterFactory<IFileConverterSettings>(
                        static c => new FileConverterSettings().SetFromConfig(c.Resolve<IConfigurationManager>()),
                        new ContainerControlledLifetimeManager());
            }

            this.UnityContainer
                .RegisterSingleton<OnlyOfficeRemoveFileCacheInfoPlugin>()
                ;
        }


        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<IPluginExtension, OnlyOfficeRemoveFileCacheInfoPlugin>(x => x
                    .WithOrder(ExtensionStage.Platform, 1)
                    .WithUnity(this.UnityContainer)
                    .WithGroup(PluginGroups.Daily))
                ;
        }
    }
}
