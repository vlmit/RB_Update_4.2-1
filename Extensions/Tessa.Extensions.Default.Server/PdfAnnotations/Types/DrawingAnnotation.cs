#nullable enable

using System.Collections.Generic;
using System.Linq;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class DrawingAnnotation : Annotation
    {
        public DrawingAnnotation()
            : base()
        {
            AnnType = AnnotationType.Drawing;
        }

        #region Properties

        public int Width { get; set; }

        public IReadOnlyList<IReadOnlyList<double>> Lines { get; set; } = Enumerable.Empty<List<double>>().ToList();

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);
            storage[nameof(this.Width)] = Int32Boxes.Box(this.Width);
            storage[nameof(this.Lines)] = this.Lines;
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);
            this.Width = storage.TryConvertInt32(nameof(this.Width)) ?? 0;
            this.Lines = storage.GetListInList<double>(nameof(this.Lines))?.ToList() ?? Enumerable.Empty<List<double>>().ToList();
        }

        #endregion
    }
}
