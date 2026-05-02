using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Imaging.Files
{
    [Registrator]
    public sealed class Registrator :
        RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterType<DocLoadBarcodeGetFileContentExtension>(new ContainerControlledLifetimeManager())
                ;
        }

        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<ICardGetFileContentExtension, DocLoadBarcodeGetFileContentExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithUnity(this.UnityContainer)
                    .WhenFileTypes(DefaultFileTypes.Barcode))
                ;
        }
    }
}
