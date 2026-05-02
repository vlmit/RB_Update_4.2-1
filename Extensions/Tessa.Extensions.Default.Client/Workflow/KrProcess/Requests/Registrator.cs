using Tessa.Cards;
using Tessa.Cards.Extensions;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Client.Workflow.KrProcess.Requests
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterType<KrClientCommandStoreExtension>(new ContainerControlledLifetimeManager())
                .RegisterType<KrClientCommandCustomExtension>(new ContainerControlledLifetimeManager())
                ;
        }


        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer

                .RegisterExtension<ICardStoreExtension, KrClientCommandStoreExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform)
                    .WithUnity(this.UnityContainer))
                .RegisterExtension<ICardRequestExtension, KrClientCommandCustomExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform)
                    .WithUnity(this.UnityContainer)
                    .WhenAnyRequestType())
                ;
        }
    }
}
