#nullable enable

using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Контейнер со всеми правилами парсинга плейсхолдеров.
    /// </summary>
    public interface IWordDocumentParsingRuleContainer
    {
        /// <summary>
        /// Возвращает список зарегистрированных правил парсинга для указанного типа элемента.
        /// </summary>
        /// <typeparam name="TElement">Тип элемента документа Word.</typeparam>
        /// <returns>Список правил парсинга для указанного типа элемента. Возвращает пустой список, если для заданного типа элемента не указаны правила парсинга.</returns>
        IReadOnlyList<IWordDocumentParsingRule<TElement>> GetRulesForType<TElement>() where TElement : OpenXmlElement;

        /// <summary>
        /// Возвращает список зарегистрированных правил парсинга для указанного типа элемента.
        /// </summary>
        /// <paramref name="type">Тип элемента документа Word.</paramref>
        /// <returns>Список правил парсинга для указанного типа элемента. Возвращает пустой список, если для заданного типа элемента не указаны правила парсинга.</returns>
        IReadOnlyList<IWordDocumentParsingRule> GetRulesForType(Type type);
    }
}
