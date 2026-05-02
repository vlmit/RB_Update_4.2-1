#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations
{
    public class StampsBarData : StorageSerializable
    {
        #region Properties 

        public Guid CardID { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public string? PlaceholderContent { get; set; }

        public string? BarcodeType { get; set; }

        public bool ShowLabel { get; set; }

        #endregion

        #region Base Overrrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.CardID)] = this.CardID;
            storage[nameof(this.Width)] = DoubleBoxes.Box(this.Width);
            storage[nameof(this.Height)] = DoubleBoxes.Box(this.Height);
            storage[nameof(this.PlaceholderContent)] = this.PlaceholderContent;
            storage[nameof(this.BarcodeType)] = this.BarcodeType;
            storage[nameof(this.ShowLabel)] = BooleanBoxes.Box(this.ShowLabel);
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.CardID = storage.TryConvertGuid(nameof(this.CardID)) ?? Guid.Empty;
            this.Width = storage.TryConvertDouble(nameof(this.Width)) ?? 0d;
            this.Height = storage.TryConvertDouble(nameof(this.Height)) ?? 0d;
            this.PlaceholderContent = storage.TryGet<string>(nameof(this.PlaceholderContent));
            this.BarcodeType = storage.TryGet<string>(nameof(this.BarcodeType));
            this.ShowLabel = storage.TryConvertBoolean(nameof(this.ShowLabel)) ?? false;
        }

        #endregion
    }
}
