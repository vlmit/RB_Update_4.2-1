#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Настройки обновления позиции элементов в структуре документа Word.
    /// </summary>
    public sealed class WordDocumentMoveRule
    {
        /// <summary>
        /// Позиция, начиная с которой выполняется перемещение.
        /// </summary>
        public List<int> MoveFrom { get; init; } = new();

        /// <summary>
        /// Определяет на сколько выполняется перемещение.
        /// </summary>
        public int MoveBy { get; init; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"MoveFrom:{string.Join(';', this.MoveFrom)}, MoveBy: {this.MoveBy}";
        }
    }
}
