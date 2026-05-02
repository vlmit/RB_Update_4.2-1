#nullable enable

using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class TextboxAnnotation : BaseXYAnnotation
    {
        public TextboxAnnotation()
            : base()
        {
            AnnType = AnnotationType.Textbox;
        }

        #region Properties

        public double? LineHeight { get; set; }

        public TextAlignment TextAlignment { get; set; }

        public string? FontWeight { get; set; }

        public string? FontStyle { get; set; }

        public string? PlaceholderContent { get; set; }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);
            storage[nameof(this.LineHeight)] = DoubleBoxes.Box(this.LineHeight);
            storage[nameof(this.TextAlignment)] = Int32Boxes.Box((int?)this.TextAlignment);
            storage[nameof(this.FontWeight)] = this.FontWeight;
            storage[nameof(this.FontStyle)] = this.FontStyle;
            storage[nameof(this.PlaceholderContent)] = this.PlaceholderContent;
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);
            this.LineHeight = storage.TryConvertDouble(nameof(this.LineHeight));
            this.TextAlignment = (TextAlignment?) storage.TryConvertInt32(nameof(this.TextAlignment)) ?? TextAlignment.Left;
            this.FontWeight = storage.TryGet<string>(nameof(this.FontWeight));
            this.FontStyle = storage.TryGet<string>(nameof(this.FontStyle));
            this.PlaceholderContent = storage.TryGet<string>(nameof(this.PlaceholderContent));
        }

        #endregion
    }
}
