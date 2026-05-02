#nullable enable

using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope
{
    [Registrator]
    public sealed class Registrator :
        RegistratorBase
    {
        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterType<IKrScope, KrScope>(new ContainerControlledLifetimeManager())
                .RegisterType<KrLifecycleScopeStoreExtension>(new PerResolveLifetimeManager())
                .RegisterType<KrScopeStoreExtension>(new PerResolveLifetimeManager())
                .RegisterFactory<ICardTypePriorityComparer>(
                    static _ => new CardTypePriorityComparer(KrConstants.KrCardStorePriority),
                    new ContainerControlledLifetimeManager())
                ;

        public override void RegisterExtensions(IExtensionContainer extensionContainer) =>
            extensionContainer
                .RegisterExtension<ICardStoreExtension, KrLifecycleScopeStoreExtension>(x => x
                    .WithOrder(ExtensionStage.BeforePlatform, -100)
                    .WithUnity(this.UnityContainer))
                .RegisterExtension<ICardStoreExtension, KrScopeStoreExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 100)
                    .WithUnity(this.UnityContainer));
    }
}
