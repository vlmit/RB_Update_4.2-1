#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations.Types
{
    public class ImageKindAnnotation : BaseXYAnnotation
    {
        #region Properties

        public string FileName { get; set; } = string.Empty;

        public double Opacity { get; set; }

        public string? DataUrl { get; set; }

        public Guid? ReferenceID { get; set; }

        public double? SelfRotationAngle { get; set; }

        public string? XObjectKey { get; set; }


        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);
            storage[nameof(this.FileName)] = FileName;
            storage[nameof(this.Opacity)] = DoubleBoxes.Box(this.Opacity);
            storage[nameof(this.DataUrl)] = DataUrl;
            storage[nameof(this.ReferenceID)] = ReferenceID;
            storage[nameof(this.SelfRotationAngle)] = DoubleBoxes.Box(this.SelfRotationAngle);
            storage[nameof(this.XObjectKey)] = XObjectKey;
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);
            this.FileName = storage.TryGet<string>(nameof(this.FileName)) ?? string.Empty;
            this.Opacity = storage.TryConvertDouble(nameof(this.Opacity)) ?? 0;
            this.DataUrl = storage.TryGet<string>(nameof(this.DataUrl));
            this.ReferenceID = storage.TryConvertGuid(nameof(this.ReferenceID));
            this.SelfRotationAngle = storage.TryConvertDouble(nameof(this.SelfRotationAngle)) ?? 0;
            this.XObjectKey = storage.TryGet<string>(nameof(this.XObjectKey));
        }

        #endregion

    }
}
