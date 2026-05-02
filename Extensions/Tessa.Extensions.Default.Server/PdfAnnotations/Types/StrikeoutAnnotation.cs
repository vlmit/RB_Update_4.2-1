#nullable enable

using System.Collections.Generic;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class StrikeoutAnnotation : RectanglesAnnotation
    {
        public StrikeoutAnnotation() : base()
        {
            AnnType = AnnotationType.Strikeout;
        }

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);
        }

        #endregion

    }
}
