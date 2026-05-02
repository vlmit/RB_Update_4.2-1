#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Tessa.Platform;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Информация о плейсхолдере в документе Word.
    /// </summary>
    public sealed class WordDocumentPlaceholderInfo : StorageSerializable
    {
        #region Fields

        private List<int>? position;

        private const string PlaceholderInfoKey = "PlaceholderInfo";

        #endregion

        #region Properties

        /// <summary>
        /// Идентификатор плейсхолдера.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Значение плейсхолдера.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Текст плейсхолдера.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Позиция элемента документа Word, в котором располагается плейсхолдер.
        /// </summary>
        public List<int> Position
        {
            get => this.position ??= new();
            set => this.position = NotNullOrThrow(value, nameof(this.Position));
        }

        /// <summary>
        /// Индекс начала плейсхолдера в тексте элемента, располагаемого по позиции <see cref="Position"/> документа.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Идентификатор гиперссылки, в которой располагается плейсхолдер, или <c>null</c>, если плейсхолдер располагается не в гиперссылке.
        /// </summary>
        public string? HyperlinkID { get; set; }

        /// <summary>
        /// Показывает, располагается ли плейсхолдер в гиперссылке.
        /// </summary>
        [MemberNotNullWhen(true, nameof(HyperlinkID))]
        public bool IsHyperlinkPlaceholder => this.HyperlinkID is not null;

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.ID)] = Int32Boxes.Box(this.ID);
            storage[nameof(this.Value)] = this.Value;
            storage[nameof(this.Text)] = this.Text;
            storage[nameof(this.Position)] = this.position;
            storage[nameof(this.Index)] = Int32Boxes.Box(this.Index);
            storage[nameof(this.HyperlinkID)] = this.HyperlinkID;
        }

        /// <inheritdoc/>
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.ID = storage.TryGet<int>(nameof(this.ID));
            this.Value = storage.TryGet<string>(nameof(this.Value)) ?? string.Empty;
            this.Text = storage.TryGet<string>(nameof(this.Text)) ?? string.Empty;
            this.position = storage.TryGet<List<int>>(nameof(this.Position));
            this.Index = storage.TryGet<int>(nameof(this.Index));
            this.HyperlinkID = storage.TryGet<string>(nameof(this.HyperlinkID));
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{Text}, {(this.position is null ? "<null position>" : string.Join(";", this.position.Select(x => x.ToString())))}, {this.Index}";
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Возвращает информацию о плейсхолдере в формате, используемом системой плейсхолдеров.
        /// </summary>
        /// <returns>Информация о плейсхолдере.</returns>
        public IPlaceholderText ToPlaceholderText()
        {
            return new PlaceholderText(
                this.Text,
                this.Value,
                info: new()
                {
                    [PlaceholderInfoKey] = Int32Boxes.Box(this.ID),
                });
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Возвращает идентификатор плейсхолдера в документе Word из текущей информации о плейсхолдере. 
        /// </summary>
        /// <param name="placeholderText">Информация о плейсхолдере.</param>
        /// <returns>Идентификатор плейсхолдера в документе Word или <c>null</c>, если эта информация отсутствует в плейсхолдере.</returns>
        public static int? TryGetWordInfoID(IPlaceholderText placeholderText)
        {
            return NotNullOrThrow(placeholderText)
                .Info
                .TryGet<int?>(PlaceholderInfoKey);
        }

        #endregion
    }
}
