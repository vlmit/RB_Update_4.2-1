#nullable enable

using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class QRAnnotation : ImageKindAnnotation
    {
        #region Properties

        public string? QRCodeType { get; set; }

        public string? PlaceholderContent { get; set; }

        public string? ECC { get; set; }

        public bool? UTF8 { get; set; }

        public bool? BOM { get; set; }

        public int? PX { get; set; }

        #endregion

        #region Constructor

        public QRAnnotation()
        {
            AnnType = AnnotationType.QR;
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);
            storage[nameof(this.QRCodeType)] = this.QRCodeType;
            storage[nameof(this.PlaceholderContent)] = this.PlaceholderContent;
            storage[nameof(this.ECC)] = this.ECC;
            storage[nameof(this.UTF8)] = BooleanBoxes.Box(this.UTF8);
            storage[nameof(this.BOM)] = BooleanBoxes.Box(this.BOM);
            storage[nameof(this.PX)] = Int32Boxes.Box(this.PX);
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);
            this.QRCodeType = storage.TryGet<string>(nameof(this.QRCodeType));
            this.PlaceholderContent = storage.TryGet<string>(nameof(this.PlaceholderContent));
            this.ECC = storage.TryGet<string>(nameof(this.ECC));
            this.UTF8 = storage.TryConvertBoolean(nameof(this.UTF8)) ?? false;
            this.BOM = storage.TryConvertBoolean(nameof(this.BOM)) ?? false;
            this.PX = storage.TryConvertInt32(nameof(this.PX)) ?? 2;
        }

        #endregion

    }
}
