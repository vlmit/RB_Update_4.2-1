#nullable enable

using System.Collections.Generic;
using System.Linq;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class StatusComment : StorageSerializable
    {
        #region Properties

        public string? UserID { get; set; }

        public string? UserName { get; set; }

        public IReadOnlyList<CommentAnnotation> Statuses { get; set; } = Enumerable.Empty<CommentAnnotation>().ToList();

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.UserID)] = this.UserID;
            storage[nameof(this.UserName)] = this.UserName;
            storage[nameof(this.Statuses)] = ToObjectList(this.Statuses);
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.UserID = storage.TryGet<string>(nameof(this.UserID));
            this.UserName = storage.TryGet<string>(nameof(this.UserName));
            this.Statuses = GetObjectList<CommentAnnotation>(storage, nameof(this.Statuses));
        }

        #endregion

    }
}
