#nullable enable

using DocumentFormat.OpenXml.Wordprocessing;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Правило для поиска автоматически определяемых табличных групп в параграфе.
    /// </summary>
    public sealed class WordDocumentAutoTableGroupParagraphRule : WordDocumentAutoTableGroupRuleBase<Paragraph>
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override bool FinishingRule => false;

        /// <inheritdoc/>
        protected override bool AutoTableCondition(Paragraph element, IWordDocumentParsingContext context)
        {
            return element.GetFirstChild<ParagraphProperties>() is { } prop
                && prop.GetFirstChild<NumberingProperties>() is not null;
        }

        #endregion
    }
}
