#nullable enable

using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.Extensions;
using Unity;
using Unity.Injection;

namespace Tessa.Extensions.Default.Client.Workflow.KrProcess
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterSingleton<IKrGlobalTileContainer, KrGlobalTileContainer>()
                .RegisterSingleton<IKrTileInflater, KrTileInflater>()
                .RegisterSingleton<IKrSecondaryProcessTileUIHandlerResolver, KrSecondaryProcessTileUIHandlerResolver>()
                .RegisterSingleton<IKrTileCommand, KrGlobalTileCommand>(KrTileCommandNames.Global)
                .RegisterSingleton<IKrTileCommand, KrLocalTileCommand>(KrTileCommandNames.Local)

                .RegisterSingleton<KrCardMetadataExtension>(
                    new InjectionConstructor(typeof(ICardMetadata), typeof(ICardCache)))
                ;

        public override void RegisterExtensions(IExtensionContainer extensionContainer) =>
            extensionContainer
                .RegisterExtension<ICardMetadataExtension, KrCardMetadataExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithUnity(this.UnityContainer))
                ;
    }
}
