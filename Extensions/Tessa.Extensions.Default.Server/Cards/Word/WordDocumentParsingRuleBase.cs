#nullable enable

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;


namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Базовый класс для реализации правил парсинга документа Word.
    /// </summary>
    /// <typeparam name="TElement">Тип обрабатываемого элемента.</typeparam>
    public abstract class WordDocumentParsingRuleBase<TElement> : IWordDocumentParsingRule<TElement>
        where TElement : OpenXmlElement
    {

        #region IWordDocumentParsingRule<TElement> Implementation

        /// <inheritdoc/>
        public bool TryParseElement(TElement element, IWordDocumentParsingContext context)
        {
            ThrowIfNull(element);
            ThrowIfNull(context);

            return this.TryParseElementCore(element, context);
        }

        /// <inheritdoc/>
        public bool TryFinishParseElement(TElement element, IWordDocumentParsingContext context)
        {
            ThrowIfNull(element);
            ThrowIfNull(context);

            return this.TryFinishParseElementCore(element, context);
        }

        #endregion

        #region IWordDocumentParsingRule Implementation

        /// <inheritdoc/>
        bool IWordDocumentParsingRule.TryParseElement(OpenXmlElement element, IWordDocumentParsingContext context)
        {
            return element is TElement typedElement
                && this.TryParseElement(typedElement, context);
        }

        /// <inheritdoc/>
        bool IWordDocumentParsingRule.TryFinishParseElement(OpenXmlElement element, IWordDocumentParsingContext context)
        {
            return element is TElement typedElement
                && this.TryFinishParseElement(typedElement, context);
        }

        #endregion

        #region Virtual and Abstract Methods

        /// <inheritdoc cref="TryParseElement(TElement, IWordDocumentParsingContext)"/>
        protected abstract bool TryParseElementCore(TElement element, IWordDocumentParsingContext context);

        /// <inheritdoc cref="TryFinishParseElement(TElement, IWordDocumentParsingContext)"/>
        /// <remarks>Реализация по умолчанию ничего не делает и возвращает значение <c>false</c>.</remarks>
        protected virtual bool TryFinishParseElementCore(TElement element, IWordDocumentParsingContext context)
        {
            return false;
        }


        #endregion

        #region Static Methods

        /// <summary>
        /// Возвращает индекс элемента относительно его родительского элемента.
        /// </summary>
        /// <param name="element">Элемент.</param>
        /// <returns>Индекс элемента относительно его родительского элемента.</returns>
        protected static int GetIndex(OpenXmlElement element)
        {
            static int GetIndexCore(OpenXmlElement? element, bool allowParagraph)
            {
                int index = 0;
                while (element != null)
                {
                    if (element is Text textElement)
                    {
                        index += textElement.Text.Length;
                    }
                    else if (element.HasChildren
                        && element is not Hyperlink
                        && (element is not Paragraph || allowParagraph))
                    {
                        index += GetIndexCore(element.LastChild!, allowParagraph && element is not Paragraph);
                    }

                    element = element.PreviousSibling();
                }

                return index;
            }

            return GetIndexCore(element.PreviousSibling(), element.Parent is not Paragraph);
        }

        #endregion
    }
}
