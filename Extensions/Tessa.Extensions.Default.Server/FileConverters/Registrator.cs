#nullable enable

using Tessa.Cards;
using Tessa.Extensions.Default.Server.FileConverters.Workers;
using Tessa.Extensions.Default.Server.OnlyOffice;
using Tessa.Extensions.Default.Server.OnlyOffice.Token;
using Tessa.FileConverters;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Server.FileConverters
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity() => this.UnityContainer
            .RegisterSingleton<IFileConverterWorker, TiffToPdfFileConverterWorker>(FileConverterWorkerNames.TiffToPdf)
            .RegisterSingleton<IFileConverterWorker, HtmlToPdfFileConverterWorker>(FileConverterWorkerNames.HtmlToPdf)
            .RegisterSingleton<IFileConverterWorker, SvgToPngFileConverterWorker>(FileConverterWorkerNames.SvgToPng)
            .RegisterFactory<IFileConverterWorker>(
                    FileConverterWorkerNames.OnlyOfficeServiceToPdf,
                    c => new OnlyOfficeServiceWorker(
                        c.Resolve<IOnlyOfficeSettingsProvider>(),
                        c.Resolve<IOnlyOfficeService>(),
                        c.Resolve<ICardStreamServerRepository>(CardRepositoryNames.Extended),
                        c.Resolve<IOnlyOfficeR7TokenManager>()),
                    new ContainerControlledLifetimeManager())
            .RegisterFactory<IFileConverterWorker>(
                    FileConverterWorkerNames.OnlyOfficeDocumentBuilderToPdf,
                    c => c.Resolve<OnlyOfficeDocumentBuilderWorker>(),
                    new ContainerControlledLifetimeManager())
            .RegisterWorker<PdfFileConverterWorker>(FileConverterFormat.Pdf)
            .RegisterSingleton<IFileConverterOperationProcessor, FileConverterOperationProcessor>()
            ;
    }
}
