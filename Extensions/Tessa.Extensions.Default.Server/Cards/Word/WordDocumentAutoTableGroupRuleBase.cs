#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Правило для поиска автоматически определяемых табличных групп.
    /// </summary>
    public abstract class WordDocumentAutoTableGroupRuleBase<TElement> : WordDocumentParsingRuleBase<TElement>
        where TElement : OpenXmlElement
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override bool TryParseElementCore(TElement element, IWordDocumentParsingContext context)
        {
            if (this.AutoTableCondition(element, context)
                && context.OrderedBlocks.All(x => x is not WordDocumentTableGroupBlock { GroupType: not WordDocumentTableGroupType.Table } || x.EndPosition.Count > 0))
            {
                context.Info[OpenXmlHelper.TextPosition(context.CurrentPosition)] = context.OrderedBlocks.Count;

                return this.FinishingRule;
            }

            return false;
        }

        /// <inheritdoc/>
        protected override bool TryFinishParseElementCore(TElement element, IWordDocumentParsingContext context)
        {
            if (this.AutoTableCondition(element, context)
                && context.OrderedBlocks.All(x => x is WordDocumentTableGroupBlock { GroupType: WordDocumentTableGroupType.Table } || x.EndPosition.Count > 0)
                && context.Info.TryGetValue(OpenXmlHelper.TextPosition(context.CurrentPosition), out var valueObj)
                && valueObj is int newBlocksFrom)
            {
                for (int i = newBlocksFrom; i < context.OrderedBlocks.Count; i++)
                {
                    var block = context.OrderedBlocks[i];

                    if (block is WordDocumentTableGroupBlock { GroupType: not WordDocumentTableGroupType.Table, IsOptional: false })
                    {
                        return false;
                    }
                }

                context.AddBlock(
                    new WordDocumentTableGroupBlock()
                    {
                        ID = Guid.NewGuid().ToString(),
                        Name = element.InnerText,
                        IsOptional = true,
                        GroupType = WordDocumentTableGroupType.Row,
                        TableElement = element.Parent,
                        StartPosition = this.GetStartPosition(element, context),
                        EndPosition = this.GetEndPosition(element, context),
                        TableStartPosition = context.GetCurrentPosition(),
                        TableEndPosition = context.GetCurrentPosition(),
                        StartIndex = 0,
                        EndIndex = element.InnerText.Length,
                    });

                return this.FinishingRule;
            }

            return false;
        }

        #endregion

        #region Virtual Properties and Methods

        /// <summary>
        /// Определяет, является ли правило завершающим обработку данного элемента.
        /// </summary>
        protected virtual bool FinishingRule => true;

        /// <summary>
        /// Условие, при котором элемент определяется как автоматически генерируемый табличный блок.
        /// </summary>
        /// <param name="element">Элемент документа Word.</param>
        /// <param name="context">Контекст парсинга документа.</param>
        /// <returns>Значение <c>true</c>, если условие добавления автоматически генерируемого табличного блока таблицы, иначе <c>false</c>.</returns>
        protected virtual bool AutoTableCondition(TElement element, IWordDocumentParsingContext context)
        {
            return true;
        }

        /// <summary>
        /// Возвращает позицию начала таблицы.
        /// </summary>
        /// <param name="element">Элемент документа Word.</param>
        /// <param name="context">Контекст парсинга плейсхолдера.</param>
        /// <returns>Позиция начала таблицы.</returns>
        protected virtual List<int> GetStartPosition(TElement element, IWordDocumentParsingContext context)
        {
            var startPosition = context.GetCurrentPosition();
            startPosition.Add(0);
            return startPosition;
        }

        /// <summary>
        /// Возвращает позицию конца таблицы.
        /// </summary>
        /// <param name="element">Элемент документа Word.</param>
        /// <param name="context">Контекст парсинга плейсхолдера.</param>
        /// <returns>Позиция конца таблицы.</returns>
        protected virtual List<int> GetEndPosition(TElement element, IWordDocumentParsingContext context)
        {
            var endPosition = context.GetCurrentPosition();
            endPosition.Add(element.ChildElements.Count - 1);
            return endPosition;
        }

        #endregion
    }
}
