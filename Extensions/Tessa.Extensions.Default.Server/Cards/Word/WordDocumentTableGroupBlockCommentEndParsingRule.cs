#nullable enable

using DocumentFormat.OpenXml.Wordprocessing;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Правило парсинга конца блока с таблицей, выделенного комментарием.
    /// </summary>
    public sealed class WordDocumentTableGroupBlockCommentEndParsingRule : WordDocumentTableGroupBlockEndParsingRuleBase<CommentRangeEnd>
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override string? TryGetID(CommentRangeEnd element, IWordDocumentParsingContext context)
        {
            return element.Id?.Value;
        }

        #endregion
    }
}
