#nullable enable

using DocumentFormat.OpenXml.Wordprocessing;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Правило парсинга конца блока с таблицей, выделенного закладкой.
    /// </summary>
    public sealed class WordDocumentTableGroupBlockBookmarkEndParsingRule : WordDocumentTableGroupBlockEndParsingRuleBase<BookmarkEnd>
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override string? TryGetID(BookmarkEnd element, IWordDocumentParsingContext context)
        {
            return element.Id?.Value;
        }

        #endregion
    }
}
