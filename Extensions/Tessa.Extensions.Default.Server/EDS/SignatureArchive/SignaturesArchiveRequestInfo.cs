using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.EDS.SignatureArchive
{
    /// <summary>
    /// Данные для запроса типа <see cref="Shared.DefaultRequestTypes.SignaturesArchiveRequest"/>.
    /// </summary>
    public class SignaturesArchiveRequestInfo
        : StorageSerializable
    {
        #region Properties

        /// <summary>
        /// Идентификатор карточки.
        /// </summary>
        public Guid CardID { get; set; }

        /// <summary>
        /// Список идентификаторов файлов.
        /// </summary>
        public IList<Guid> FileIDs { get; set; } = [];

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void DeserializeCore(Dictionary<string, object> storage)
        {
            this.CardID = storage.TryGet<Guid>(nameof(this.CardID));
            this.FileIDs = [.. storage.TryGet<IList>(nameof(this.FileIDs)).Cast<Guid>()];
        }

        /// <inheritdoc/>
        protected override void SerializeCore(Dictionary<string, object> storage)
        {
            storage[nameof(this.CardID)] = this.CardID;
            storage[nameof(this.FileIDs)] = this.FileIDs.Cast<object>().ToList();
        }

        #endregion
    }
}
