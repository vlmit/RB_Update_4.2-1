#nullable enable

using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.PdfAnnotations
{
    public class StampsQRInfo : StorageSerializable
    {
        #region Properties 

        public PdfAnnotationsAction? Action { get; set; }

        public StampsQRData? Data { get; set; }

        #endregion

        #region Base Overrrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.Action)] = this.Action is null ? null : Int32Boxes.Box((int)this.Action);
            storage[nameof(this.Data)] = this.Data.ToSerializedDictionary();
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.Action = (PdfAnnotationsAction?)storage.TryConvertInt32(nameof(this.Action));
            this.Data = TryGetObject<StampsQRData>(storage, nameof(this.Data));
        }

        #endregion
    }
}
