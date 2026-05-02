#nullable enable

using DocumentFormat.OpenXml.Wordprocessing;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Правило парсинга конца условного блока.
    /// </summary>
    public sealed class WordDocumentConditionalBlockEndParsingRule : WordDocumentParsingRuleBase<CommentRangeEnd>
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override bool TryParseElementCore(CommentRangeEnd element, IWordDocumentParsingContext context)
        {
            var commentID = element.Id?.Value;

            if (!string.IsNullOrEmpty(commentID)
                && context.TryGetBlock<WordDocumentConditionalBlock>(commentID) is { } conditionalPart)
            {
                conditionalPart.EndPosition = context.GetCurrentPosition();
                conditionalPart.EndIndex = GetIndex(element);
                conditionalPart.EndOfElement = conditionalPart.EndIndex == element.Parent!.InnerText.Length;
            }

            return false;
        }

        #endregion
    }
}
