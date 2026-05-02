#nullable enable

using DocumentFormat.OpenXml.Wordprocessing;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Правило парсинга начала блока с таблицей, выделенного закладкой.
    /// </summary>
    public sealed class WordDocumentTableGroupBlockBookmarkStartParsingRule : WordDocumentTableGroupBlockStartParsingRuleBase<BookmarkStart>
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override string? TryGetID(BookmarkStart element, IWordDocumentParsingContext context)
        {
            return element.Id?.Value;
        }

        /// <inheritdoc/>
        protected override string? TryGetName(BookmarkStart element, IWordDocumentParsingContext context)
        {
            return element.Name?.Value;
        }

        /// <inheritdoc/>
        protected override WordDocumentTableGroupType? TryGetType(string blockName)
        {
            if (blockName is { Length : < 3 })
            {
                return null;
            }

            return GetTypeFromNamePrefix(blockName[0..2]);
        }

        #endregion
    }
}
