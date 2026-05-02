#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class RectanglesAnnotation : Annotation
    {
        #region Properties

        public IReadOnlyList<AnnotationRectangle> Rectangles { get; set; } 
                = Enumerable.Empty<AnnotationRectangle>().ToList();

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);
            storage[nameof(this.Rectangles)] = ToObjectList(this.Rectangles);
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);
            this.Rectangles = GetObjectList<AnnotationRectangle>(storage, nameof(this.Rectangles));
        }

        #endregion

    }
}
