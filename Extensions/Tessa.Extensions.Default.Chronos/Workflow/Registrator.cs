using Tessa.Extensions.Default.Server.Notices;
using Tessa.Extensions.Default.Server.Workflow;
using Tessa.Platform.Plugins;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Chronos.Workflow
{
    [Registrator(Tag = RegistratorTag.ServerPlugin)]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterNoticesMessageProcessor()
                ;
        }


        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<IPluginExtension, KrAutoApprovePlugin>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithSingleton()
                    .WithGroup(PluginGroups.Normal))

                .RegisterExtension<IPluginExtension, ReturnTasksFromPostponedPlugin>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 2)
                    .WithSingleton()
                    .WithGroup(PluginGroups.Normal))

                // "Often, AfterPlatform, 1" - это Tessa.Extensions.Default.Chronos.Notices.MailSenderPlugin

                .RegisterExtension<IPluginExtension, MobileApprovalPlugin>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 2)
                    .WithSingleton()
                    .WithGroup(PluginGroups.Often))
                ;
        }
    }
}
