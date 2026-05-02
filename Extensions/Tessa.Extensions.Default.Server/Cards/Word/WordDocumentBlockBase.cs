#nullable enable

using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Базовый класс для блока документа Word.
    /// </summary>
    public abstract class WordDocumentBlockBase : StorageSerializable, IWordDocumentBlock
    {
        #region Fields

        private List<IWordDocumentBlock>? childBlocks;
        private List<int>? placeholders;
        private List<int>? startPosition;
        private List<int>? endPosition;

        #endregion

        #region IWordDocumentBlock Implementation

        /// <inheritdoc/>
        public string ID { get; set; } = string.Empty;

        /// <inheritdoc/>
        public string Name { get; set; } = string.Empty;

        /// <inheritdoc/>
        public List<int> StartPosition
        {
            get => this.startPosition ??= new();
            set => this.startPosition = NotNullOrThrow(value, nameof(this.StartPosition));
        }

        /// <inheritdoc/>
        public List<int> EndPosition
        {
            get => this.endPosition ??= new();
            set => this.endPosition = NotNullOrThrow(value, nameof(this.EndPosition));
        }

        /// <inheritdoc/>
        public int StartIndex { get; set; }

        /// <inheritdoc/>
        public int EndIndex { get; set; }

        /// <inheritdoc />
        string? ITypedSerializable.TypeName { get; set; }

        /// <inheritdoc/>
        public List<IWordDocumentBlock> ChildBlocks => this.childBlocks ??= new();

        /// <inheritdoc/>
        public List<int> Placeholders => this.placeholders ??= new();

        /// <inheritdoc/>
        public bool Contains(WordDocumentPlaceholderInfo placeholderInfo)
        {
            return OpenXmlHelper.HasSamePart(this.StartPosition, placeholderInfo.Position)
                && OpenXmlHelper.IsLessOrEquals(this.StartPosition, placeholderInfo.Position, this.StartIndex <= placeholderInfo.Index)
                && OpenXmlHelper.Compare(
                    this.EndPosition,
                    placeholderInfo.Position,
                    this.EndIndex.CompareTo(placeholderInfo.Index + placeholderInfo.Text.Length),
                    this.EndIndex.CompareTo(placeholderInfo.Index + placeholderInfo.Text.Length)) >= 0;
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.ID)] = this.ID;
            storage[nameof(this.Name)] = this.Name;
            storage[nameof(this.StartPosition)] = this.startPosition;
            storage[nameof(this.EndPosition)] = this.endPosition;
            storage[nameof(this.StartIndex)] = Int32Boxes.Box(this.StartIndex);
            storage[nameof(this.EndIndex)] = Int32Boxes.Box(this.EndIndex);
            storage[nameof(this.ChildBlocks)] = this.childBlocks?.ToTypedList();
            storage[nameof(this.Placeholders)] = this.placeholders;
        }

        /// <inheritdoc/>
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.ID = storage.TryGet<string>(nameof(this.ID)) ?? string.Empty;
            this.Name = storage.TryGet<string>(nameof(this.Name)) ?? string.Empty;
            this.startPosition = storage.TryGet<List<int>>(nameof(this.StartPosition));
            this.endPosition = storage.TryGet<List<int>>(nameof(this.EndPosition));
            this.StartIndex = storage.TryGet<int>(nameof(this.StartIndex));
            this.EndIndex = storage.TryGet<int>(nameof(this.EndIndex));
            this.childBlocks = storage.GetTypedList<IWordDocumentBlock>(nameof(this.ChildBlocks));
            this.placeholders = storage.TryGet<List<int>>(nameof(this.Placeholders));
        }

        #endregion

    }
}
