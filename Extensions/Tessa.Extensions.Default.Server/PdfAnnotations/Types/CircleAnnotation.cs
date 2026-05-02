#nullable enable

using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class CircleAnnotation : BaseXYAnnotation
    {
        #region Properties

        public string? BackgroundColor { get; set; }

        public int? FontSize { get; set; }

        public int? StartAngle { get; set; }

        public bool Clockwise { get; set; }

        public bool RoundText { get; set; }


        #endregion
        public CircleAnnotation() : base()
        {
            AnnType = AnnotationType.Circle;
        }

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);
            storage[nameof(this.BackgroundColor)] = this.BackgroundColor;
            storage[nameof(this.FontSize)] = Int32Boxes.Box(this.FontSize);
            storage[nameof(this.StartAngle)] = Int32Boxes.Box(this.StartAngle);
            storage[nameof(this.Clockwise)] = BooleanBoxes.Box(this.Clockwise);
            storage[nameof(this.RoundText)] = BooleanBoxes.Box(this.RoundText);
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);
            this.BackgroundColor = storage.TryGet<string>(nameof(this.BackgroundColor));
            this.FontSize = storage.TryConvertInt32(nameof(this.FontSize));
            this.StartAngle = storage.TryConvertInt32(nameof(this.StartAngle));
            this.Clockwise = storage.TryConvertBoolean(nameof(this.Clockwise)) ?? true;
            this.RoundText = storage.TryConvertBoolean(nameof(this.RoundText)) ?? false;
        }

        #endregion

    }
}
