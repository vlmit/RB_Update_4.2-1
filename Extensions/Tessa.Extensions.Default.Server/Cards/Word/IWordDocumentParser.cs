#nullable enable

using DocumentFormat.OpenXml.Packaging;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Парсер документа Word.
    /// </summary>
    public interface IWordDocumentParser
    {
        /// <summary>
        /// Выполняет парсинг документа Word для обработки документа в качестве шаблона файлов.
        /// </summary>
        /// <param name="wordDocument">Документ Word.</param>
        /// <returns>Результат парсинга.</returns>
        IWordDocumentParsingResult ParseDocument(WordprocessingDocument wordDocument);
    }
}
