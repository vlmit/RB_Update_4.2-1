#nullable enable

using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Правило парсинга начала блока с таблицей, выделенного комментарием.
    /// </summary>
    public sealed class WordDocumentTableGroupBlockCommentStartParsingRule : WordDocumentTableGroupBlockStartParsingRuleBase<CommentRangeStart>
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override string? TryGetID(CommentRangeStart element, IWordDocumentParsingContext context)
        {
            return element.Id?.Value;
        }

        /// <inheritdoc/>
        protected override string? TryGetName(CommentRangeStart element, IWordDocumentParsingContext context)
        {
            if (context.WordDocument.MainDocumentPart!.Parts.FirstOrDefault(p => p.OpenXmlPart is WordprocessingCommentsPart).OpenXmlPart is not WordprocessingCommentsPart commentsPart)
            {
                return null;
            }

            var commentID = element.Id?.Value;
            var commentElement = commentsPart.Comments?.Descendants<Comment>().FirstOrDefault(x => x.Id?.Value == commentID);

            return commentElement?.InnerText;
        }

        /// <inheritdoc/>
        protected override WordDocumentTableGroupType? TryGetType(string blockName)
        {
            if (blockName is { Length : < 4 }
                || blockName[0] != '#')
            {
                return null;
            }

            return this.GetTypeFromNamePrefix(blockName[1..3]);
        }
        #endregion
    }
}
