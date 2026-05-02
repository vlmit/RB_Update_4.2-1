#nullable enable

using System.Collections.Generic;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class BarAnnotation : ImageKindAnnotation
    {
        public BarAnnotation()
        {
            AnnType = AnnotationType.Bar;
        }

        #region Properties

        public string? BarcodeType { get; set; }

        public string? PlaceholderContent { get; set; }

        public bool ShowLabel { get; set; }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);
            storage[nameof(this.PlaceholderContent)] = this.PlaceholderContent;
            storage[nameof(this.BarcodeType)] = this.BarcodeType;
            storage[nameof(this.ShowLabel)] = this.ShowLabel;
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);
            this.PlaceholderContent = storage.TryGet<string>(nameof(this.PlaceholderContent));
            this.BarcodeType = storage.TryGet<string>(nameof(this.BarcodeType));
            this.ShowLabel = storage.TryConvertBoolean(nameof(this.ShowLabel)) ?? false;
        }

        #endregion

    }
}
