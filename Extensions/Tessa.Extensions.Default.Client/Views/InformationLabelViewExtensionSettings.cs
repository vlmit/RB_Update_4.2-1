#nullable enable
using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Client.Views
{
    /// <summary>
    /// Settings for information label.
    /// </summary>
    public sealed class InformationLabelViewExtensionSettings : StorageSerializable
    {
        #region Properties

        /// <summary>
        /// Text to show user if no required params given.
        /// </summary>
        public string? LabelText { get; set; }
        
        /// <summary>
        /// Required params list separated by space, comma or semicolon.
        /// </summary>
        public string? RequiredParams { get; set; }
        
        /// <summary>
        /// Flag, that all view params are required.
        /// </summary>
        public bool AllParamsRequired { get; set; }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.LabelText)] = this.LabelText;
            storage[nameof(this.RequiredParams)] = this.RequiredParams;
            storage[nameof(this.AllParamsRequired)] = BooleanBoxes.Box(this.AllParamsRequired);
        }

        /// <inheritdoc/>
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.LabelText = storage.TryGet<string>(nameof(this.LabelText));
            this.RequiredParams = storage.TryGet<string>(nameof(this.RequiredParams));
            this.AllParamsRequired = storage.TryGet<bool>(nameof(this.AllParamsRequired));
        }

        #endregion
    }
}
