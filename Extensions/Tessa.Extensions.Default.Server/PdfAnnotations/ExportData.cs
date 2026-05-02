#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations
{
    public class ExportData : StorageSerializable
    {
        #region Properties 

        public Guid StampsConstructorCardID { get; set; }

        public Guid StampsRowID { get; set; }

        public Guid CardID { get; set; }

        public bool Preview { get; set; }

        public ExportType ExportType { get; set; }

        #endregion

        #region Base Overrrides
        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.StampsConstructorCardID)] = this.StampsConstructorCardID;
            storage[nameof(this.StampsRowID)] = this.StampsRowID;
            storage[nameof(this.CardID)] = this.CardID;
            storage[nameof(this.Preview)] = BooleanBoxes.Box(this.Preview);
            storage[nameof(this.ExportType)] = Int32Boxes.Box((int)this.ExportType);
        }
        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.StampsConstructorCardID = storage.TryConvertGuid(nameof(this.StampsConstructorCardID)) ?? Guid.Empty;
            this.StampsRowID = storage.TryConvertGuid(nameof(this.StampsRowID)) ?? Guid.Empty;
            this.CardID = storage.TryConvertGuid(nameof(this.CardID)) ?? Guid.Empty;
            this.Preview = storage.TryConvertBoolean(nameof(this.Preview)) ?? false;
            this.ExportType = (ExportType)(storage.TryConvertInt32(nameof(this.ExportType)) ?? (int)ExportType.Png);
        }

        #endregion
    }
}
