#nullable enable

using System.Collections.Generic;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Tessa.Platform;
using Tessa.Platform.Placeholders;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Базовое правило для поиска плейсхолдеров в документе Word.
    /// </summary>
    /// <typeparam name="T">Тип проверяемого элемента.</typeparam>
    public abstract class WordDocumentPlaceholdersRuleBase<T> : WordDocumentParsingRuleBase<T>
        where T : OpenXmlElement
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override bool TryParseElementCore(T element, IWordDocumentParsingContext context)
        {
            var allText = StringBuilderHelper.Acquire();

            foreach (var textElement in this.GetTextElements(element))
            {
                allText.Append(textElement.Text);
            }

            var allTextString = allText.ToStringAndRelease();
            AddPlaceholdersFromText(context, allTextString);

            return false;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Возвращает список текстовых элементов, которые относятся к текущему элементу.
        /// </summary>
        /// <param name="element">Элемент документов Word.</param>
        /// <returns>Список текстовых элементов.</returns>
        protected virtual IEnumerable<OpenXmlLeafTextElement> GetTextElements(OpenXmlElement element)
        {
            foreach (var childElement in element)
            {
                if (childElement is Text textElement)
                {
                    yield return textElement;
                }
                else if (childElement.HasChildren
                    && childElement is not Paragraph and not Hyperlink)
                {
                    foreach (var childTextElement in this.GetTextElements(childElement))
                    {
                        yield return childTextElement;
                    }
                }
            }
        }

        /// <summary>
        /// Добавляет плейсхолдеры из текста.
        /// </summary>
        /// <param name="context">Контекст парсинга документа Word.</param>
        /// <param name="allText">Текст, из которого производится поиск плейсхолдеров.</param>
        protected static void AddPlaceholdersFromText(
            IWordDocumentParsingContext context,
            string allText)
        {
            foreach (Match match in PlaceholderHelper.UnescapedPlaceholdersRegex.Matches(allText))
            {
                string text = match.Value;
                string? value = PlaceholderHelper.TryGetValue(text);

                if (value is not null)
                {
                    var newPlaceholder = new WordDocumentPlaceholderInfo
                    {
                        Position = context.GetCurrentPosition(),
                        Index = match.Index,
                        Text = text,
                        Value = value,
                    };

                    context.AddPlaceholder(newPlaceholder);
                }
            }
        }

        #endregion
    }
}
