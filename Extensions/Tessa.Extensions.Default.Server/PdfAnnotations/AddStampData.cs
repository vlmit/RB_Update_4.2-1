#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations
{
    public class AddStampData : StorageSerializable
    {
        #region Properties 

        public Guid StampsConstructorCardID { get; set; }

        public Guid StampsRowID { get; set; }

        public Guid CardID { get; set; }

        public Guid FileID { get; set; }

        public bool StoreToCard { get; set; }

        #endregion

        #region Base Overrrides
        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.StampsConstructorCardID)] = this.StampsConstructorCardID;
            storage[nameof(this.StampsRowID)] = this.StampsRowID;
            storage[nameof(this.CardID)] = this.CardID;
            storage[nameof(this.FileID)] = this.FileID;
            storage[nameof(this.StoreToCard)] = BooleanBoxes.Box(this.StoreToCard);
        }
        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.StampsConstructorCardID = storage.TryConvertGuid(nameof(this.StampsConstructorCardID)) ?? Guid.Empty;
            this.StampsRowID = storage.TryConvertGuid(nameof(this.StampsRowID)) ?? Guid.Empty;
            this.CardID = storage.TryConvertGuid(nameof(this.CardID)) ?? Guid.Empty;
            this.FileID = storage.TryConvertGuid(nameof(this.FileID)) ?? Guid.Empty;
            this.StoreToCard = storage.TryConvertBoolean(nameof(this.StoreToCard)) ?? false;
        }

        #endregion
    }
}
