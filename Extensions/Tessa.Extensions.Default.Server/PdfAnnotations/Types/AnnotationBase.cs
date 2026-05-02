#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class AnnotationBase : StorageSerializable
    {
        #region Properties

        public AnnotationBaseType AnnClass { get; set; }

        public PDFRef ID { get; set; } = new PDFRef();

        public PDFRef? ParentID { get; set; }

        public AnnotationState State { get; set; }

        public string? Content { get; set; }

        public string? UserName { get; set; }

        public Guid? UserID { get; set; }

        public int Page { get; set; }

        public DateTime? Date { get; set; }

        public bool? IsImported { get; set; }

        public string? Alias { get; set; }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.AnnClass)] = Int32Boxes.Box((int)this.AnnClass);
            storage[nameof(this.ID)] = this.ID.ToSerializedDictionary();
            storage[nameof(this.ParentID)] = this.ParentID.ToSerializedDictionary();
            storage[nameof(this.State)] = Int32Boxes.Box((int)this.State);
            storage[nameof(this.Content)] = this.Content;
            storage[nameof(this.UserName)] = this.UserName;
            storage[nameof(this.UserID)] = this.UserID;
            storage[nameof(this.Page)] = Int32Boxes.Box(this.Page);
            storage[nameof(this.Date)] = this.Date;
            storage[nameof(this.IsImported)] = BooleanBoxes.Box(this.IsImported);
            storage[nameof(this.Alias)] = this.Alias;
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            var annClassValue = storage.TryConvertInt32(nameof(this.AnnClass))
                ?? throw new InvalidOperationException($"{nameof(this.AnnClass)} is required.");
            this.AnnClass = (AnnotationBaseType)annClassValue;
            this.ID = TryGetObject<PDFRef>(storage, nameof(this.ID)) ?? new PDFRef();
            this.ParentID = TryGetObject<PDFRef>(storage, nameof(this.ParentID));
            var stateValue = storage.TryConvertInt32(nameof(this.State))
                ?? throw new InvalidOperationException($"{nameof(this.State)} is required.");
            this.State = (AnnotationState)stateValue;
            this.Content = storage.TryGet<string>(nameof(this.Content));
            this.UserName = storage.TryGet<string>(nameof(this.UserName));
            this.UserID = storage.TryConvertGuid(nameof(this.UserID));
            this.Page = storage.TryConvertInt32(nameof(this.Page)) ?? 0;
            this.Date = storage.TryConvertDateTime(nameof(this.Date));
            this.IsImported = storage.TryConvertBoolean(nameof(this.IsImported));
            this.Alias = storage.TryGet<string>(nameof(this.Alias));
        }

        #endregion

    }
}
