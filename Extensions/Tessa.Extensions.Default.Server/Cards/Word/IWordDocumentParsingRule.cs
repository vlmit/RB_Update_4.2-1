#nullable enable

using DocumentFormat.OpenXml;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Правило парсинга элемента документа Word.
    /// </summary>
    public interface IWordDocumentParsingRule
    {
        /// <summary>
        /// Выполняет парсинг элемента Word по заданному правилу. Вызывается до парсинга дочерних элементов.
        /// </summary>
        /// <param name="element">Элемент документа Word.</param>
        /// <param name="context">Контекста парсинга документа.</param>
        /// <returns>Значение <c>true</c>, если элемент был распарсен по заданному правилу и обработка другими правилами не требуется, иначе <c>false</c>.</returns>
        bool TryParseElement(
            OpenXmlElement element,
            IWordDocumentParsingContext context);

        /// <summary>
        /// Выполняет завершение парсинга элемента Word по заданному правилу. Вызывается после парсинга дочерних элементов.
        /// </summary>
        /// <param name="element">Элемент документа Word.</param>
        /// <param name="context">Контекста парсинга документа.</param>
        /// <returns>Значение <c>true</c>, если элемент был распарсен по заданному правилу и обработка другими правилами не требуется, иначе <c>false</c>.</returns>
        bool TryFinishParseElement(
            OpenXmlElement element,
            IWordDocumentParsingContext context);
    }
}
