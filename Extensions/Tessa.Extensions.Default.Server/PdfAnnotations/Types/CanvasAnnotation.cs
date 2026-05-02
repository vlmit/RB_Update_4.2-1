#nullable enable

using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class CanvasAnnotation : BaseXYAnnotation
    {
        #region Properties

        public double? Rx { get; set; }

        public double? Ry { get; set; }

        public string? BackgroundColor { get; set; }

        public StampPlacement StampPlacement { get; set; }

        public bool? NoSelection { get; set; }

        #endregion
        public CanvasAnnotation() : base()
        {
            AnnType = AnnotationType.Canvas;
        }

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);
            storage[nameof(this.Rx)] = DoubleBoxes.Box(this.Rx);
            storage[nameof(this.Ry)] = DoubleBoxes.Box(this.Ry);
            storage[nameof(this.BackgroundColor)] = this.BackgroundColor;
            storage[nameof(this.StampPlacement)] = Int32Boxes.Box((int) this.StampPlacement);
            storage[nameof(this.NoSelection)] = BooleanBoxes.Box(this.NoSelection);
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);
            this.Rx = storage.TryConvertDouble(nameof(this.Rx));
            this.Ry = storage.TryConvertDouble(nameof(this.Ry));
            this.BackgroundColor = storage.TryGet<string>(nameof(this.BackgroundColor));
            this.StampPlacement = (StampPlacement?) storage.TryConvertInt32(nameof(this.StampPlacement)) ?? Types.StampPlacement.All;
            this.NoSelection = storage.TryConvertBoolean(nameof(this.NoSelection));
        }

        #endregion

    }
}
