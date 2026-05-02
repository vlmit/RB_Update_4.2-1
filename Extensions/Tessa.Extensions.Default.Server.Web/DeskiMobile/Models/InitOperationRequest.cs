using System.Linq;
using System.Collections.Generic;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Web.DeskiMobile.Models
{
    /// <summary>
    /// Модель, которая ожидается в теле запроса на cоздание операции для работы с TESSA Assistant.
    /// </summary>
    public sealed class InitOperationRequest : StorageSerializable
    {
        #region Properties

        /// <summary>
        /// Список файлов.
        /// </summary>
        public List<DeskiMobileFile> Files { get; set; } = new List<DeskiMobileFile>();

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.Files)] = ToObjectList(this.Files);
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.Files = GetObjectList<DeskiMobileFile>(storage, nameof(this.Files));
        }

        #endregion
    }
}
