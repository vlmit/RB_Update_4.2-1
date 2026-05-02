#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Tessa.Cards.Caching;
using Tessa.FileConverters;
using Tessa.Jinni;
using Tessa.Platform;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Server.FileConverters.Workers
{
    /// <summary>
    /// Объект, ответственный за преобразование файла в формат <see cref="FileConverterFormat.Pdf"/>
    /// посредством внешних программ, таких как OpenOffice или LibreOffice.
    /// </summary>
    /// <remarks>
    /// Наследники класса могут переопределять методы интерфейса, например, добавив к ним обработку файлов других форматов.
    /// Класс может также реализовывать <see cref="IAsyncDisposable"/> для очистки ресурсов, для этого в наследнике
    /// переопределяется метод <see cref="DisposeAsync"/> и вызывается сначала его базовая реализация.
    /// </remarks>
    public class PdfFileConverterWorker :
        JinniWorkerBase,
        IFileConverterWorker,
        IAsyncDisposable
    {
        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="cardCache"><inheritdoc cref="ICardCache" path="/summary"/></param>
        /// <param name="proxyFactory"><inheritdoc cref="IJinniBalancerProxyFactory" path="/summary"/></param>
        /// <param name="onlyOfficeServicePdfWorker"><inheritdoc cref="FileConverterWorkerNames.OnlyOfficeServiceToPdf" path="/summary"/></param>
        /// <param name="onlyOfficeDocumentBuilderPdfWorker"><inheritdoc cref="FileConverterWorkerNames.OnlyOfficeDocumentBuilderToPdf" path="/summary"/></param>
        /// <param name="tiffToPdfWorker"><inheritdoc cref="FileConverterWorkerNames.TiffToPdf" path="/summary"/></param>
        /// <param name="htmlToPdfWorker"><inheritdoc cref="FileConverterWorkerNames.HtmlToPdf" path="/summary"/></param>
        public PdfFileConverterWorker(
            ICardCache cardCache,
            IJinniBalancerProxyFactory proxyFactory,
            [OptionalDependency(FileConverterWorkerNames.OnlyOfficeServiceToPdf)]
            IFileConverterWorker? onlyOfficeServicePdfWorker = null,
            [OptionalDependency(FileConverterWorkerNames.OnlyOfficeDocumentBuilderToPdf)]
            IFileConverterWorker? onlyOfficeDocumentBuilderPdfWorker = null,
            [OptionalDependency(FileConverterWorkerNames.TiffToPdf)]
            IFileConverterWorker? tiffToPdfWorker = null,
            [OptionalDependency(FileConverterWorkerNames.HtmlToPdf)]
            IFileConverterWorker? htmlToPdfWorker = null)
            : base(proxyFactory)
        {
            this.cardCache = NotNullOrThrow(cardCache);
            this.onlyOfficeServicePdfWorker = onlyOfficeServicePdfWorker;
            this.onlyOfficeDocumentBuilderPdfWorker = onlyOfficeDocumentBuilderPdfWorker;
            this.tiffToPdfWorker = tiffToPdfWorker;
            this.htmlToPdfWorker = htmlToPdfWorker;
        }

        #endregion

        #region Fields

        private readonly ICardCache cardCache;

        private readonly IFileConverterWorker? onlyOfficeServicePdfWorker;

        private readonly IFileConverterWorker? onlyOfficeDocumentBuilderPdfWorker;

        private readonly IFileConverterWorker? tiffToPdfWorker;

        private readonly IFileConverterWorker? htmlToPdfWorker;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region IFileConverterWorker Members

        /// <inheritdoc/>
        public virtual async Task ConvertFileAsync(IFileConverterContext context, CancellationToken cancellationToken = default)
        {
            var converterType = await FileConverterOperationProcessor.GetConverterTypeAsync(this.cardCache, cancellationToken);
            switch (converterType)
            {
                case FileConverterType.None:
                    throw ArgumentOutOfRange(converterType, "File converter is not set.");

                case FileConverterType.LibreOffice:
                    {
                        switch (context.InputExtension)
                        {
                            case "pdf":
                                // файл PDF можно "конвертировать" в pdf, просто вернув исходный файл
                                var stream = await context.GetInputContentAsync(cancellationToken);
                                var length = stream.CanSeek ? stream.Length : -1L;
                                context.GetOutputContentAsync = _ => new((stream, length));
                                return;

                            case "tif":
                            case "tiff":
                                // файлы TIFF конвертируются отдельным worker-ом, если он задан
                                if (this.tiffToPdfWorker is not null)
                                {
                                    await this.tiffToPdfWorker.ConvertFileAsync(context, cancellationToken);
                                }
                                else
                                {
                                    context.ValidationResult.AddError(this, "Can't convert from TIFF without registered worker.");
                                }

                                return;

                            case "htm":
                            case "html":
                                // файлы HTML конвертируются отдельным worker-ом, если он задан
                                if (this.htmlToPdfWorker is not null)
                                {
                                    await this.htmlToPdfWorker.ConvertFileAsync(context, cancellationToken);
                                }
                                else
                                {
                                    context.ValidationResult.AddError(this, "Can't convert from HTML without registered worker.");
                                }

                                return;

                            default:
                                // все остальные файлы конвертируются средствами OpenOffice/LibreOffice с помощью веб-сервиса документов
                                await this.ConvertFileInternalAsync(context, cancellationToken);

                                // Запись ключа, через который вызывающая сторона поймёт, что конвертация была выполнена посредством unoconv
                                context.ResponseInfo["unoconv"] = BooleanBoxes.True;

                                return;
                        }
                    }
                case FileConverterType.OnlyOfficeService:
                    if (this.onlyOfficeServicePdfWorker is null)
                    {
                        throw new NotSupportedException($"Converter {nameof(FileConverterType.OnlyOfficeService)} isn't registered");
                    }

                    await this.onlyOfficeServicePdfWorker.ConvertFileAsync(context, cancellationToken);
                    break;

                case FileConverterType.OnlyOfficeDocumentBuilder:
                    if (this.onlyOfficeDocumentBuilderPdfWorker is null)
                    {
                        throw new NotSupportedException($"Converter {nameof(FileConverterType.OnlyOfficeDocumentBuilder)} isn't registered");
                    }

                    await this.onlyOfficeDocumentBuilderPdfWorker.ConvertFileAsync(context, cancellationToken);
                    break;
            }
        }


        /// <inheritdoc/>
        public virtual async Task PreprocessAsync(CancellationToken cancellationToken = default)
        {
            await PreprocessAsync(this.tiffToPdfWorker, cancellationToken);
            await PreprocessAsync(this.htmlToPdfWorker, cancellationToken);
            await PreprocessAsync(this.onlyOfficeServicePdfWorker, cancellationToken);
            await PreprocessAsync(this.onlyOfficeDocumentBuilderPdfWorker, cancellationToken);

            static Task PreprocessAsync(IFileConverterWorker? worker, CancellationToken cancellationToken) =>
                worker?.PreprocessAsync(cancellationToken) ?? Task.CompletedTask;
        }

        /// <inheritdoc/>
        public virtual async Task PerformMaintenanceAsync(CancellationToken cancellationToken = default)
        {
            await PerformMaintenanceAsync(this.tiffToPdfWorker, cancellationToken);
            await PerformMaintenanceAsync(this.htmlToPdfWorker, cancellationToken);
            await PerformMaintenanceAsync(this.onlyOfficeServicePdfWorker, cancellationToken);
            await PerformMaintenanceAsync(this.onlyOfficeDocumentBuilderPdfWorker, cancellationToken);

            static Task PerformMaintenanceAsync(IFileConverterWorker? worker, CancellationToken cancellationToken) =>
                worker?.PerformMaintenanceAsync(cancellationToken) ?? Task.CompletedTask;
        }

        #endregion

        #region IAsyncDisposable Members

        /// <inheritdoc/>
        public virtual async ValueTask DisposeAsync()
        {
            logger.Trace("Shutting down file converter. All child processes will be terminated.");

            await DisposeInstanceAsync(this.tiffToPdfWorker);
            await DisposeInstanceAsync(this.htmlToPdfWorker);
        }

        #endregion

        #region Private Methods

        private static ValueTask DisposeInstanceAsync(object? instance)
        {
            switch (instance)
            {
                case IAsyncDisposable asyncDisposable:
                    return asyncDisposable.DisposeAsync();
                case IDisposable disposable:
                    disposable.Dispose();
                    return ValueTask.CompletedTask;
                default:
                    return ValueTask.CompletedTask;
            }
        }

        #endregion
    }
}
