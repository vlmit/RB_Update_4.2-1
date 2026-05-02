#nullable enable

using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Правило парсинга начала условного блока.
    /// </summary>
    public sealed class WordDocumentConditionalBlockStartParsingRule : WordDocumentParsingRuleBase<CommentRangeStart>
    {
        #region Fields

        private const string ExpressionGroupName = "expression";

        private static readonly Regex ExpressionRegex = new($"^\\s*#if\\s+(?<{ExpressionGroupName}>.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex QuotesRegex = new("[“”»«]", RegexOptions.Compiled);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override bool TryParseElementCore(CommentRangeStart element, IWordDocumentParsingContext context)
        {
            var commentID = element.Id?.Value;

            if (!string.IsNullOrEmpty(commentID)
                && TryGetCommentText(commentID, context) is { } comment
                && TryParseExpression(comment, context) is { } expression)
            {
                var newBlock = new WordDocumentConditionalBlock
                {
                    Name = comment,
                    Expression = expression,
                    ID = commentID,
                    StartIndex = GetIndex(element),
                    StartPosition = context.GetCurrentPosition(),
                };
                newBlock.StartOfElement = newBlock.StartIndex == 0;
                context.AddBlock(newBlock);

                return true;
            }

            return false;
        }

        #endregion

        #region Private Methods

        private static string? TryGetCommentText(string commentID, IWordDocumentParsingContext context)
        {
            if (context.WordDocument.MainDocumentPart!.Parts.FirstOrDefault(p => p.OpenXmlPart is WordprocessingCommentsPart).OpenXmlPart is not WordprocessingCommentsPart commentsPart)
            {
                return null;
            }

            var commentElement = commentsPart.Comments?.Descendants<Comment>().FirstOrDefault(x => x.Id?.Value == commentID);

            return commentElement?.InnerText;
        }

        private static string? TryParseExpression(string comment, IWordDocumentParsingContext context)
        {
            var expressionMatch = ExpressionRegex.Match(comment);
            if (!expressionMatch.Success)
            {
                return null;
            }

            var expression = expressionMatch.Groups[ExpressionGroupName].Value;

            return QuotesRegex.Replace(expression, "\"");
        }

        #endregion
    }
}
