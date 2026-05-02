#nullable enable

namespace Tessa.Extensions.Default.Server.FileConverters.Workers
{
    /// <summary>
    /// Имена стандартных конвертеров <see cref="Tessa.FileConverters.IFileConverterWorker"/>,
    /// которые используются в других конвертерах.
    /// </summary>
    public static class FileConverterWorkerNames
    {
        #region Constants

        /// <summary>
        /// Конвертер из TIFF в PDF, который используется в <see cref="PdfFileConverterWorker"/> или его наследниках.
        /// </summary>
        public const string TiffToPdf = "TiffToPdf";

        /// <summary>
        /// Конвертер из HTML в PDF, который используется в <see cref="PdfFileConverterWorker"/> или его наследниках.
        /// </summary>
        public const string HtmlToPdf = "HtmlToPdf";

        /// <summary>
        /// Конвертер в PDF, который используется в <see cref="PdfFileConverterWorker"/> или его наследниках.
        /// </summary>
        public const string OnlyOfficeServiceToPdf = "OnlyOfficeServiceToPdf";

        /// <summary>
        /// Конвертер в PDF, который используется в <see cref="PdfFileConverterWorker"/> или его наследниках.
        /// </summary>
        public const string OnlyOfficeDocumentBuilderToPdf = "OnlyOfficeDocumentBuilderToPdf";

        /// <summary>
        /// Конвертер в PNG.
        /// </summary>
        public const string SvgToPng = "SvgToPng";

        #endregion
    }
}
