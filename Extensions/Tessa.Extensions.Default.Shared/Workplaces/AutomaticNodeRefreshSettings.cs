#nullable enable

using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workplaces
{
    /// <summary>
    /// Настройки автоматического обновления узлов рабочего места.
    /// </summary>
    public class AutomaticNodeRefreshSettings : StorageSerializable, IAutomaticNodeRefreshSettings
    {
        #region Constructor

        /// <inheritdoc />
        public AutomaticNodeRefreshSettings()
        {
            this.RefreshInterval = 300;
            this.WithContentDataRefreshing = true;
            this.AlwaysRefresh = false;
        }

        #endregion

        #region IAutomaticNodeRefreshSettings Implementation

        /// <inheritdoc />
        public int RefreshInterval { get; set; }

        /// <inheritdoc />
        public bool WithContentDataRefreshing { get; set; }

        /// <inheritdoc />
        public bool AlwaysRefresh { get; set; }

        #endregion

        #region IStorageSerializable Members

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.RefreshInterval)] = Int32Boxes.Box(this.RefreshInterval);
            storage[nameof(this.WithContentDataRefreshing)] = BooleanBoxes.Box(this.WithContentDataRefreshing);
            storage[nameof(this.AlwaysRefresh)] = BooleanBoxes.Box(this.AlwaysRefresh);
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.RefreshInterval = storage.TryGet<int>(nameof(this.RefreshInterval));
            this.WithContentDataRefreshing = storage.TryGet<bool>(nameof(this.WithContentDataRefreshing));
            this.AlwaysRefresh = storage.TryGet<bool>(nameof(this.AlwaysRefresh));
        }

        #endregion
    }
}
