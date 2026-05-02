using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Imaging.DocLoad;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Imaging.Cards
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterType<IDocLoadExtension, DefaultDocLoadExtension>(new ContainerControlledLifetimeManager())
                .RegisterType<DocLoadBarcodeTemplateNewExtension>(new ContainerControlledLifetimeManager())
                .RegisterType<DocLoadBarcodeStoreExtension>(new ContainerControlledLifetimeManager())
                ;
        }

        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                 .RegisterExtension<ICardStoreExtension, DocLoadBarcodeStoreExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 7)
                    .WithUnity(this.UnityContainer)
                    .WhenMethod(CardStoreMethod.Default))
                 .RegisterExtension<ICardNewExtension, DocLoadBarcodeTemplateNewExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 12)
                    .WithUnity(this.UnityContainer)
                    .WhenMethod(CardNewMethod.Template))
                ;
        }
    }
}
