#nullable enable

using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class BaseXYAnnotation : Annotation
    {
        #region Properties

        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);
            storage[nameof(this.X)] = DoubleBoxes.Box(this.X);
            storage[nameof(this.Y)] = DoubleBoxes.Box(this.Y);
            storage[nameof(this.Width)] = DoubleBoxes.Box(this.Width);
            storage[nameof(this.Height)] = DoubleBoxes.Box(this.Height);
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);
            this.X = storage.TryConvertDouble(nameof(this.X)) ?? 0d;
            this.Y = storage.TryConvertDouble(nameof(this.Y)) ?? 0d;
            this.Width = storage.TryConvertDouble(nameof(this.Width)) ?? 0d;
            this.Height = storage.TryConvertDouble(nameof(this.Height)) ?? 0d;
        }

        #endregion
    }
}
