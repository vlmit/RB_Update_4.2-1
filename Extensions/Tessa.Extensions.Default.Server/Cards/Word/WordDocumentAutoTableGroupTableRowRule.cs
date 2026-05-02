#nullable enable

using System.Collections.Generic;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Правило для поиска автоматически определяемых табличных групп в строке таблицы.
    /// </summary>
    public sealed class WordDocumentAutoTableGroupTableRowRule : WordDocumentAutoTableGroupRuleBase<TableRow>
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override List<int> GetStartPosition(TableRow element, IWordDocumentParsingContext context)
        {
            // В качестве начальной позиции берём первый элемент первого параграфа первой ячейки строки таблицы
            var currentPosition = context.GetCurrentPosition();

            int cellPosition = 0;
            var cellElement = element.FirstChild;
            while (cellElement is not null and not TableCell)
            {
                cellPosition++;
                cellElement = cellElement.NextSibling();
            }

            if (cellElement is null)
            {
                return base.GetStartPosition(element, context);
            }

            currentPosition.Add(cellPosition);

            var paragraphPosition = 0;
            var paragraphElement = cellElement.FirstChild;
            while (paragraphElement is not null and not Paragraph)
            {
                paragraphPosition++;
                paragraphElement = paragraphElement.NextSibling();
            }

            if (paragraphElement is null)
            {
                return base.GetStartPosition(element, context);
            }

            currentPosition.Add(paragraphPosition);
            currentPosition.Add(0);

            return currentPosition;
        }

        #endregion
    }
}
