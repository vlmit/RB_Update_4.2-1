#nullable enable

using System.Collections.Generic;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Обработчик смещения позиций объектов в документе Word.
    /// </summary>
    public interface IWordDocumentMoveProcessor
    {
        /// <summary>
        /// Выполняет обновление позиции блоков и их дочерних блоков по указанному списку правил.
        /// </summary>
        /// <param name="blocks">Блоки документа Word.</param>
        /// <param name="rules">Список правил смещения.</param>
        void MoveBlocks(
            IEnumerable<IWordDocumentBlock> blocks,
            IEnumerable<WordDocumentMoveRule> rules);

        /// <summary>
        /// Выполняет обновление позиции плейсхолдеров по указанному списку правил.
        /// </summary>
        /// <param name="placeholders">Объект с информацией о плейсхолдере документа Word.</param>
        /// <param name="rules">Список правил смещения.</param>
        void MovePlaceholder(
            IEnumerable<WordDocumentPlaceholderInfo> placeholders,
            IEnumerable<WordDocumentMoveRule> rules);
    }
}
