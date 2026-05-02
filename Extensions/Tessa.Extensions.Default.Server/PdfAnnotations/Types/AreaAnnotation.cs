#nullable enable

using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class AreaAnnotation : BaseXYAnnotation
    {
        #region Properties

        public double? Rx { get; set; }

        public double? Ry { get; set; }

        public string? BackgroundColor { get; set; }

        #endregion
        public AreaAnnotation() : base()
        {
            AnnType = AnnotationType.Area;
        }

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);
            storage[nameof(this.Rx)] = DoubleBoxes.Box(this.Rx);
            storage[nameof(this.Ry)] = DoubleBoxes.Box(this.Ry);
            storage[nameof(this.BackgroundColor)] = this.BackgroundColor;
            storage[nameof(this.IsImported)] = BooleanBoxes.Box(this.IsImported);
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);
            this.Rx = storage.TryConvertDouble(nameof(this.Rx));
            this.Ry = storage.TryConvertDouble(nameof(this.Ry));
            this.BackgroundColor = storage.TryGet<string>(nameof(this.BackgroundColor));
            this.IsImported = storage.TryConvertBoolean(nameof(this.IsImported));
        }

        #endregion

    }
}
