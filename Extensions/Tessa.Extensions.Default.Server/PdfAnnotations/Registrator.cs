#nullable enable

using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Server.PdfAnnotations
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterType<PdfAnnotationsRequestExtension>(new ContainerControlledLifetimeManager())
                .RegisterType<PdfAnnotationsStoreExtension>(new ContainerControlledLifetimeManager())
                .RegisterType<IPdfAnnotationsStrategy, PdfAnnotationsStrategy>(new ContainerControlledLifetimeManager())
                .RegisterType<PdfAnnotationsGetExtension>(new PerResolveLifetimeManager())
                ;
        }

        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<ICardRequestExtension, PdfAnnotationsRequestExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithUnity(this.UnityContainer)
                    .WhenRequestTypes(DefaultRequestTypes.PdfAnnotations))
                .RegisterExtension<ICardDeleteExtension, PdfAnnotationsDeleteExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithUnity(this.UnityContainer)
                    .WhenAnyDeleteMethod()
                    .WhenAnyCardType())
                .RegisterExtension<ICardStoreExtension, PdfAnnotationsStoreExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithUnity(this.UnityContainer)
                    .WhenAnyStoreMethod()
                    .WhenAnyCardType())
                .RegisterExtension<ICardGetExtension, PdfAnnotationsGetExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithUnity(this.UnityContainer))
                ;
        }
    }
}
