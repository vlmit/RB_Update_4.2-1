#nullable enable

using System;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Правило для поиска плейсхолдеров в элементе <see cref="FieldCode"/>.
    /// </summary>
    public sealed class WordDocumentPlaceholdersFieldCodeRule : WordDocumentPlaceholdersRuleBase<FieldCode>
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override bool TryParseElementCore(FieldCode element, IWordDocumentParsingContext context)
        {
            var decodedText = Uri.UnescapeDataString(element.Text);
            AddPlaceholdersFromText(context, decodedText);

            return false;
        }

        #endregion
    }
}
