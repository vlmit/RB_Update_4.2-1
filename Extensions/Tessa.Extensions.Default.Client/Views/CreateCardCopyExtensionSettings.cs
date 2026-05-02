#nullable enable
using System.Collections.Generic;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Client.Views
{
    /// <summary>
    /// Settings for <see cref="CreateCardCopyExtension"/> used also in <see cref="CreateCardCopyExtensionConfigurator"/>.
    /// </summary>
    public sealed class CreateCardCopyExtensionSettings :
        StorageSerializable
    {
        #region Properties

        /// <summary>
        /// View parameter for record selection by ID.
        /// </summary>
        public string? IDParam { get; set; }

        #endregion
        
        #region IStorageSerializable Members

        /// <inheritdoc/>
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.IDParam = storage.TryGet<string>(nameof(this.IDParam));
        }

        /// <inheritdoc/>
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.IDParam)] = this.IDParam;
        }

        #endregion
    }
}
