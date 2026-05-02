#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class Annotation : AnnotationBase
    {
        public Annotation()
        {
            AnnClass = AnnotationBaseType.Annotation;
        }

        #region Properties

        public AnnotationType AnnType { get; set; }

        public string? Color { get; set; }

        public int? Size { get; set; }

        public int? StrokeWidth { get; set; }

        public IReadOnlyList<CommentAnnotation> Comments { get; set; } = Enumerable.Empty<CommentAnnotation>().ToList();

        public IReadOnlyList<StatusComment> Statuses { get; set; } = Enumerable.Empty<StatusComment>().ToList();

        public double? Angle { get; set; }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);
            storage[nameof(this.AnnType)] = Int32Boxes.Box((int)this.AnnType);
            storage[nameof(this.Color)] = this.Color;
            storage[nameof(this.Size)] = Int32Boxes.Box(this.Size);
            storage[nameof(this.StrokeWidth)] = Int32Boxes.Box(this.StrokeWidth);
            storage[nameof(this.Comments)] = ToObjectList(this.Comments);
            storage[nameof(this.Statuses)] = ToObjectList(this.Statuses);
            storage[nameof(this.Angle)] = DoubleBoxes.Box(this.Angle);
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);

            var annTypeValue = storage.TryConvertInt32(nameof(this.AnnType))
                ?? throw new InvalidOperationException($"{nameof(this.AnnType)} is required.");
            this.AnnType = (AnnotationType)annTypeValue;
            this.Color = storage.TryGet<string>(nameof(this.Color));
            this.Size = storage.TryConvertInt32(nameof(this.Size));
            this.StrokeWidth = storage.TryConvertInt32(nameof(this.StrokeWidth));
            this.Comments = GetObjectList<CommentAnnotation>(storage, nameof(this.Comments));
            this.Statuses = GetObjectList<StatusComment>(storage, nameof(this.Statuses));
            this.Angle = storage.TryConvertDouble(nameof(this.Angle)) ?? 0;
        }

        #endregion

    }
}
