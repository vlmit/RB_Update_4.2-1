#nullable enable

using DocumentFormat.OpenXml;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Объект, возвращающий из контейнера зависимостей список зарегистрированных в нём правил обработки плейсхолдеров.
    /// </summary>
    public interface IWordDocumentParsingRuleResolver
    {
        /// <summary>
        /// Регистрирует указанное правило обработки для указанного типа элемента.
        /// </summary>
        /// <typeparam name="TElement">Тип элемента документа Word.</typeparam>
        /// <typeparam name="TRule">Правило обработки для указанного элемента.</typeparam>
        /// <returns>Объект <see cref="IWordDocumentParsingRuleResolver"/> для создания цепочки вызовов.</returns>
        IWordDocumentParsingRuleResolver RegisterRule<TElement, TRule>()
            where TElement : OpenXmlElement
            where TRule : IWordDocumentParsingRule<TElement>;

        /// <summary>
        /// Возвращает контейнер со всеми правилами парсинга, зарегистрированным в текущем объекте.
        /// </summary>
        /// <returns>Контейнер со всеми зарегистрированными правилами парсинга.</returns>
        IWordDocumentParsingRuleContainer ResolveAll();
    }
}
