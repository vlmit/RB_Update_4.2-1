#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <inheritdoc cref="IWordDocumentMoveProcessor"/>
    public sealed class WordDocumentMoveProcessor : IWordDocumentMoveProcessor
    {
        #region IWordDocumentMoveProcessor Implementation

        /// <inheritdoc/>
        public void MoveBlocks(IEnumerable<IWordDocumentBlock> blocks, IEnumerable<WordDocumentMoveRule> rules)
        {
            foreach (var moveRule in rules.OrderByDescending(x => x.MoveFrom, OpenXmlHelper.PositionComparer))
            {
                ProcessMoveBlocks(moveRule, blocks);
            }
        }

        /// <inheritdoc/>
        public void MovePlaceholder(IEnumerable<WordDocumentPlaceholderInfo> placeholders, IEnumerable<WordDocumentMoveRule> rules)
        {
            foreach (var moveRule in rules)
            {
                ProcessMovePlaceholders(moveRule, placeholders);
            }
        }

        #endregion

        #region Private Methods

        private static void ProcessMoveBlocks(WordDocumentMoveRule moveRule, IEnumerable<IWordDocumentBlock> blocks)
        {
            foreach (var block in blocks)
            {
                bool blockMoved = false;
                if (block.StartPosition.Count >= moveRule.MoveFrom.Count
                    && OpenXmlHelper.HasSameSubtree(moveRule.MoveFrom, block.StartPosition, moveRule.MoveFrom.Count - 1)
                    && OpenXmlHelper.IsLessOrEquals(moveRule.MoveFrom, block.StartPosition))
                {
                    block.StartPosition[moveRule.MoveFrom.Count - 1] -= moveRule.MoveBy;
                    blockMoved = true;
                }

                if (block.EndPosition.Count >= moveRule.MoveFrom.Count
                    && OpenXmlHelper.HasSameSubtree(moveRule.MoveFrom, block.EndPosition, moveRule.MoveFrom.Count - 1)
                    && OpenXmlHelper.IsLessOrEquals(moveRule.MoveFrom, block.EndPosition))
                {
                    block.EndPosition[moveRule.MoveFrom.Count - 1] -= moveRule.MoveBy;
                    blockMoved = true;
                }

                if (block is WordDocumentTableGroupBlock tableGroupBlock)
                {
                    if (tableGroupBlock.TableStartPosition.Count >= moveRule.MoveFrom.Count
                    && OpenXmlHelper.HasSameSubtree(moveRule.MoveFrom, tableGroupBlock.TableStartPosition, moveRule.MoveFrom.Count - 1)
                        && OpenXmlHelper.IsLessOrEquals(moveRule.MoveFrom, tableGroupBlock.TableStartPosition))
                    {
                        tableGroupBlock.TableStartPosition[moveRule.MoveFrom.Count - 1] -= moveRule.MoveBy;
                    }

                    if (tableGroupBlock.TableEndPosition.Count >= moveRule.MoveFrom.Count
                    && OpenXmlHelper.HasSameSubtree(moveRule.MoveFrom, tableGroupBlock.TableEndPosition, moveRule.MoveFrom.Count - 1)
                        && OpenXmlHelper.IsLessOrEquals(moveRule.MoveFrom, tableGroupBlock.TableEndPosition))
                    {
                        tableGroupBlock.TableEndPosition[moveRule.MoveFrom.Count - 1] -= moveRule.MoveBy;
                    }
                }

                if (blockMoved)
                {
                    if (block.ChildBlocks.Count > 0)
                    {
                        ProcessMoveBlocks(moveRule, block.ChildBlocks);
                    }
                }
            }
        }

        private static void ProcessMovePlaceholders(WordDocumentMoveRule moveRule, IEnumerable<WordDocumentPlaceholderInfo> placeholderInfos)
        {
            foreach (var placeholderInfo in placeholderInfos)
            {
                if (placeholderInfo.Position.Count >= moveRule.MoveFrom.Count
                    && OpenXmlHelper.HasSameSubtree(moveRule.MoveFrom, placeholderInfo.Position, moveRule.MoveFrom.Count - 1)
                    && OpenXmlHelper.IsLessOrEquals(moveRule.MoveFrom, placeholderInfo.Position))
                {
                    placeholderInfo.Position[moveRule.MoveFrom.Count - 1] -= moveRule.MoveBy;
                }
            }
        }

        #endregion
    }
}
