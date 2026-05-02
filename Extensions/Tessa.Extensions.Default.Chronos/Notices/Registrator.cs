using Tessa.Platform.Plugins;

namespace Tessa.Extensions.Default.Chronos.Notices
{
    [Registrator(Tag = RegistratorTag.ServerPlugin)]
    public sealed class Registrator :
        RegistratorBase
    {
        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<IPluginExtension, MailSenderPlugin>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithSingleton()
                    .WithGroup(PluginGroups.Often))
                ;
        }
    }
}
