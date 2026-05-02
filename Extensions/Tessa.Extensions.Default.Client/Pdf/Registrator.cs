using Tessa.Extensions.Platform.Client.Scanning;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Client.Pdf
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void InitializeExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterPdfStampExtensionTypes()
                ;
        }


        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<IPdfStampExtension, DefaultPdfStampExtension>(x => x
                    .WithOrder(ExtensionStage.BeforePlatform, int.MaxValue)
                    .WithUnity(this.UnityContainer))
                ;
        }


        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterType<DefaultPdfStampExtension>(new ContainerControlledLifetimeManager())
                .RegisterType<IPdfPageExtractor, PdfSharpPageExtractor>(nameof(PdfSharpPageExtractor), new ContainerControlledLifetimeManager())
                .RegisterType<IPdfPageExtractor, PdfiumPageExtractor>(nameof(PdfiumPageExtractor), new PerResolveLifetimeManager())
                .RegisterType<IPdfPageExtractor, DefaultPdfPageExtractor>(new ContainerControlledLifetimeManager())
                .RegisterType<IPdfGenerator, DefaultPdfGenerator>(new ContainerControlledLifetimeManager())
                ;
        }
    }
}
