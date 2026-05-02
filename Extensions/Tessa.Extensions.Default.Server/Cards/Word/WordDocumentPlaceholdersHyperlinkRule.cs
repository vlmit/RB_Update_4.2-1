#nullable enable

using System.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Правило для поиска плейсхолдеров в гиперссылке.
    /// </summary>
    public sealed class WordDocumentPlaceholdersHyperlinkRule : WordDocumentPlaceholdersRuleBase<Hyperlink>
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override bool TryParseElementCore(Hyperlink element, IWordDocumentParsingContext context)
        {
            var hyperlinkID = element.Id?.Value;
            if (!string.IsNullOrEmpty(hyperlinkID)
                && context.HasPlaceholders)
            {
                foreach (var hyperLinkPlaceholder in context.Placeholders.Where(x => x.HyperlinkID == hyperlinkID))
                {
                    hyperLinkPlaceholder.Position = context.GetCurrentPosition();
                }

            }

            return base.TryParseElementCore(element, context);
        }

        #endregion
    }
}
