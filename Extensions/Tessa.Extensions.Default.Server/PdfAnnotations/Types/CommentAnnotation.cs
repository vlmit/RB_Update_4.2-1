#nullable enable

using System.Collections.Generic;
using System.Linq;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class CommentAnnotation : AnnotationBase
    {
        public CommentAnnotation()
        {
            AnnClass = AnnotationBaseType.Comment;
        }

        #region Properties

        public AnnotationStatus Status { get; set; }

        /// <summary>
        /// Статус, выставленный родительской аннотации, при добавлении комментария.
        /// </summary>
        /// <value></value>
        public AnnotationStatus AnnotationStatus { get; set; }

        public IReadOnlyList<StatusComment> StatusesComments { get; set; } = Enumerable.Empty<StatusComment>().ToList();

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);
            storage[nameof(this.Status)] = Int32Boxes.Box((int) this.Status);
            storage[nameof(this.AnnotationStatus)] = Int32Boxes.Box((int) this.AnnotationStatus);
            storage[nameof(this.StatusesComments)] = ToObjectList(this.StatusesComments);
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);
            this.Status = (AnnotationStatus)(storage.TryConvertInt32(nameof(this.Status)) ?? default);
            this.AnnotationStatus = (AnnotationStatus)(storage.TryConvertInt32(nameof(this.AnnotationStatus)) ?? default);
            this.StatusesComments = GetObjectList<StatusComment>(storage, nameof(this.StatusesComments));
        }

        #endregion

    }
}
