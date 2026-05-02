#nullable enable

using System;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Правило парсинга конца блока с таблицей, выделенного закладкой.
    /// </summary>
    public abstract class WordDocumentTableGroupBlockEndParsingRuleBase<T> : WordDocumentParsingRuleBase<T>
        where T : OpenXmlElement
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override bool TryParseElementCore(T element, IWordDocumentParsingContext context)
        {
            var elementID = this.TryGetID(element, context);
            if (string.IsNullOrEmpty(elementID))
            {
                return false;
            }

            var block = context.TryGetBlock<WordDocumentTableGroupBlock>(elementID);
            if (block is null)
            {
                return false;
            }

            block.EndPosition = context.GetCurrentPosition();
            block.EndIndex = GetIndex(element);

            // Блок целиком располагается в одном параграфе
            if (block.EndPosition.Count == block.StartPosition.Count
                && element.Parent == OpenXmlHelper.GetElementByPosition(context.WordDocument.MainDocumentPart, block.StartPosition).Parent)
            {
                if (block.StartIndex == 0
                    && block.EndIndex == element.Parent!.InnerText.Length)
                {
                    block.StartPosition.RemoveAt(block.EndPosition.Count - 1);
                }
                else
                {
                    block.InParagraph = true;
                    block.TableStartPosition.AddRange(block.StartPosition.Take(block.StartPosition.Count - 1));
                    block.TableEndPosition.AddRange(block.EndPosition.Take(block.EndPosition.Count - 1));

                    return true;
                }
            }

            // Далее рассчитываем позицию начала и конца таблицы, в которой копируются элементы
            var startPosition = block.StartPosition;
            var endPosition = block.EndPosition;
            var minCount = Math.Min(startPosition.Count, endPosition.Count) - 1;
            int depth = 0;
            // Берем глубину, как индекс первого различия в позициях начала и конца позиции
            while (depth < minCount
                && startPosition[depth] == endPosition[depth])
            {
                depth++;
            }

            block.TableStartPosition.AddRange(startPosition.Take(depth + 1));
            block.TableEndPosition.AddRange(endPosition.Take(depth + 1));

            var firstElement = OpenXmlHelper.GetElementByPosition(context.WordDocument.MainDocumentPart, block.TableStartPosition);
            var lastElement = OpenXmlHelper.GetElementByPosition(context.WordDocument.MainDocumentPart, block.TableEndPosition);

            // Word очень странно размещает конец метки внутри таблицы в ситуации, если при сохранении курсор был указан на абзац сразу после таблицы.
            // или в последнем абзаце есть текст. В первом случае метка будет внутри параграфа сразу за таблицей. Во втором случае будет как отдельный элемент за таблицей,
            // но перед параграфом с текстом.
            // Из-за этих особенностей, расчет глубины метки некорректен, т.к. общим родителем начала и конца всегда будет именно элемент Body и элементом строки будет считаться вся таблица.
            // Поэтому при такой ситуации мы указываем элемент строки таблицы в качестве элементов данной группы.
            if (firstElement is Table)
            {
                var tableRowOnly = true;
                var nextElement = firstElement.NextSibling();

                while (nextElement is not null
                    && nextElement != lastElement)
                {
                    if (nextElement.InnerText.Length > 0)
                    {
                        tableRowOnly = false;
                        break;
                    }
                    nextElement = nextElement.NextSibling();
                }

                if (tableRowOnly)
                {
                    block.TableEndPosition[^1] = block.TableStartPosition[^1];
                    block.TableStartPosition.Add(startPosition[depth + 1]);
                    block.TableEndPosition.Add(firstElement.Count() - 1);
                }
            }
            // Обрабатываем кейс, когда старт и конец комментария находятся в ячейках.
            // В таком случае считаем строкой не набор ячеек строки, а саму строку.
            else if (OpenXmlHelper.GetSelfOrParent<TableCell>(firstElement, out var depthStartDiff) is not null
                && OpenXmlHelper.GetSelfOrParent<TableCell>(lastElement, out var depthEndDiff) is { } lastParentCell)
            {
                lastElement = lastParentCell.Parent;
                block.TableStartPosition.RemoveRange(block.TableStartPosition.Count - 1 - depthStartDiff, 1 + depthStartDiff);
                block.TableEndPosition.RemoveRange(block.TableEndPosition.Count - 1 - depthEndDiff, 1 + depthEndDiff);
            }
            // Может быть ситуация, когда конец метки находится в самом начале следующего элемента (например при выделении всего параграфа).
            // В таком случае элемент, в котором располагается конец метки, не считаем частью строки.
            else if (block.EndIndex == 0)
            {
                block.TableEndPosition[^1] -= 1;
            }

            // Если тип блока - не таблица, а мы находимся в строке, то расширяем границы блока на все строки ниже
            if (block.GroupType != WordDocumentTableGroupType.Table
                && lastElement is TableRow)
            {
                var checkRow = lastElement;
                while (checkRow.NextSibling<TableRow>() is { } nextRow)
                {
                    block.TableEndPosition[^1]++;
                    checkRow = nextRow;
                }

                block.StartPosition.Clear();
                block.EndPosition.Clear();
                block.StartPosition.AddRange(block.TableStartPosition);
                block.EndPosition.AddRange(block.TableEndPosition);
                block.EndIndex = lastElement.Parent!.InnerText.Length;
            }

            return true;
        }

        #endregion

        #region Protected Abstract Methods

        /// <summary>
        /// Возвращает идентификатор блока из элемента.
        /// </summary>
        /// <param name="element">Элемент.</param>
        /// <param name="context">Контекст парсинга документа Word.</param>
        /// <returns>Идентификатор блока или <c>null</c>, если его не удалось определить.</returns>
        protected abstract string? TryGetID(T element, IWordDocumentParsingContext context);

        #endregion
    }
}
