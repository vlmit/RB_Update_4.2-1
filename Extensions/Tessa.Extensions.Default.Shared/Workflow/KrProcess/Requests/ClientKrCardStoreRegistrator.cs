#nullable enable

using Tessa.Cards.Extensions;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess.Requests
{
    [Registrator(Tag = RegistratorTag.ClientOther)]
    public sealed class ClientKrCardStoreRegistrator :
        RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterType<KrCardStoreExtension>(
                    new ContainerControlledLifetimeManager())
                ;
        }

        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<ICardStoreExtension, KrCardStoreExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithUnity(this.UnityContainer))
                ;
        }
    }
}
