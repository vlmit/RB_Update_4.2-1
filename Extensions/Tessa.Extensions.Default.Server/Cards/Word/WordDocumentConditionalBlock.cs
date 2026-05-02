#nullable enable

using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Часть документа Word, которая может быть удалена по условию.
    /// </summary>
    public sealed class WordDocumentConditionalBlock : WordDocumentBlockBase, IWordDocumentBlock
    {
        #region Properties

        /// <summary>
        /// Определяет, что начало удаляемой части находится в самом начале объекта.
        /// Это указывает на то, что также нужно удалить данный объект, если он остаётся пустым после удаления текущей части.
        /// </summary>
        public bool StartOfElement { get; set; }

        /// <summary>
        /// Определяет, что конец удаляемой части находится в самом конце объекта.
        /// Это указывает на то, что также удалить данный объект, если он остаётся пустым после удаления текущей части.
        /// </summary>
        public bool EndOfElement { get; set; }

        /// <summary>
        /// Условное выражение.
        /// </summary>
        public string Expression { get; set; } = string.Empty;

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);
            storage[nameof(this.Expression)] = this.Expression;
            storage[nameof(this.StartOfElement)] = BooleanBoxes.Box(this.StartOfElement);
            storage[nameof(this.EndOfElement)] = BooleanBoxes.Box(this.EndOfElement);
        }

        /// <inheritdoc/>
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);
            this.Expression = storage.TryGet<string>(nameof(this.Expression)) ?? string.Empty;
            this.StartOfElement = storage.TryGet<bool>(nameof(this.StartOfElement));
            this.EndOfElement = storage.TryGet<bool>(nameof(this.EndOfElement));
        }

        #endregion
    }
}
