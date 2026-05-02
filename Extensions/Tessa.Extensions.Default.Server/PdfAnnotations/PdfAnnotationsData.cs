#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tessa.Extensions.Default.Server.PdfAnnotations.Types;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations
{
    public class PdfAnnotationsData : StorageSerializable
    {
        #region Properties 

        public Guid? ID { get; set; }

        public Guid CardID { get; set; }

        public Guid FileID { get; set; }

        public Guid FileVersionRowID { get; set; }

        public IList<Annotation>? Annotations { get; set; }

        public int Version { get; set; }

        public Guid? ModifiedByID { get; set; }

        public DateTime? Modified { get; set; }

        #endregion

        #region Base Overrrides
        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.ID)] = this.ID;
            storage[nameof(this.CardID)] = this.CardID;
            storage[nameof(this.FileID)] = this.FileID;
            storage[nameof(this.FileVersionRowID)] = this.FileVersionRowID;
            storage[nameof(this.Annotations)] = ToObjectList(this.Annotations);
            storage[nameof(this.Version)] = Int32Boxes.Box(this.Version);
            storage[nameof(this.ModifiedByID)] = this.ModifiedByID;
            storage[nameof(this.Modified)] = this.Modified;
        }
        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.ID = storage.TryConvertGuid(nameof(this.ID));
            this.CardID = storage.TryConvertGuid(nameof(this.CardID)) ?? Guid.Empty;
            this.FileID = storage.TryConvertGuid(nameof(this.FileID)) ?? Guid.Empty;
            this.FileVersionRowID = storage.TryConvertGuid(nameof(this.FileVersionRowID)) ?? Guid.Empty;
            this.Annotations = GetAnnotations(storage.TryGet<object?>(nameof(this.Annotations)) as IList);
            this.Version = storage.TryConvertInt32(nameof(this.Version)) ?? 0;
            this.ModifiedByID = storage.TryConvertGuid(nameof(this.ModifiedByID));
            this.Modified = storage.TryConvertDateTime(nameof(this.Modified));
        }

        #endregion

        #region Public Static

        public static IList<Annotation>? GetAnnotations(IList? input) => ((input)?.OfType<Dictionary<string, object?>>()
                            ?? [])
                        .Select(static itemStorage =>
                        {
                            var a = new Annotation();
                            a.Deserialize(itemStorage);
                            IStorageSerializable? serializable = a.AnnType switch
                            {
                                AnnotationType.Circle => new CircleAnnotation(),
                                AnnotationType.Area => new AreaAnnotation(),
                                AnnotationType.Canvas => new CanvasAnnotation(),
                                AnnotationType.Drawing => new DrawingAnnotation(),
                                AnnotationType.Highlight => new HighlightAnnotation(),
                                AnnotationType.Strikeout => new StrikeoutAnnotation(),
                                AnnotationType.Textbox => new TextboxAnnotation(),
                                AnnotationType.Image => new ImageAnnotation(),
                                AnnotationType.Bar => new BarAnnotation(),
                                AnnotationType.QR => new QRAnnotation(),
                                _ => null
                            };

                            return serializable is not null ?
                                (Annotation) serializable.Deserialize(itemStorage) : null;
                        }).Where(static x => x is not null).ToList() as IList<Annotation>;

        #endregion
    }
}
