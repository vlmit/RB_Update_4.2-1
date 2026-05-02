#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using PdfSharp.Pdf.IO;
using Tessa.Extensions.Platform.Client.Scanning;
using Unity;

namespace Tessa.Extensions.Default.Client.Pdf
{
    /// <summary>
    /// Объект, выполняющий разбор файла PDF на страницы с изображениями PNG.
    /// Выбор подходящей библиотеки (Pdfium или PdfSharp) определяется автоматически.
    /// </summary>
    public class DefaultPdfPageExtractor :
        IPdfPageExtractor
    {
        // любые зависимости Unity можно получить через конструктор

        #region Constructors

        public DefaultPdfPageExtractor(
            [Dependency(nameof(PdfiumPageExtractor))]
            Func<IPdfPageExtractor> getPdfiumFunc,
            [Dependency(nameof(PdfSharpPageExtractor))]
            Func<IPdfPageExtractor> getPdfSharpFunc)
        {
            this.getPdfiumFunc = NotNullOrThrow(getPdfiumFunc);
            this.getPdfSharpFunc = NotNullOrThrow(getPdfSharpFunc);
        }

        #endregion

        #region Fields

        private readonly Func<IPdfPageExtractor> getPdfiumFunc;

        private readonly Func<IPdfPageExtractor> getPdfSharpFunc;

        #endregion

        #region IPdfPageExtractor Members

        /// <summary>
        /// Выполняет извлечение страниц PDF с изображениями PNG.
        /// </summary>
        /// <param name="context">Контекст операции по разбору файла PDF на страницы.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public virtual Task ExtractPagesAsync(IPdfPageExtractorContext context, CancellationToken cancellationToken = default)
        {
            // we are not using PDFium to read document props,
            // so that legacy Windows client will work for at least TESSA generated documents
            string pdfAuthor;
            using (var document = PdfReader.Open(context.PdfFilePath, PdfDocumentOpenMode.Import))
            {
                pdfAuthor = document.Info.Author;
            }

            // TESSA generated documents are rendered using PdfSharp, otherwise for third-party docs - using Pdfium
            var extractor = pdfAuthor == PdfHelper.TessaGeneratedPdfAuthor
                ? this.getPdfSharpFunc.Invoke()
                : this.getPdfiumFunc.Invoke();

            return extractor.ExtractPagesAsync(context, cancellationToken);
        }

        #endregion
    }
}
