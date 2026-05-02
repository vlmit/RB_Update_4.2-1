#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workplaces
{
    public sealed class TreeItemFilteringSettings :
        StorageSerializable,
        ITreeItemFilteringSettings
    {
        #region Fields

        private List<string> parameters = [];
        private List<string> refSections = [];

        #endregion

        #region Public properties

        /// <inheritdoc />
        [AllowNull]
        public List<string> Parameters
        {
            get => this.parameters;
            set => this.parameters = value ?? [];
        }

        /// <inheritdoc />
        [AllowNull]
        public List<string> RefSections
        {
            get => this.refSections;
            set => this.refSections = value ?? [];
        }

        #endregion

        #region IStorageSerializable Members

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.Parameters)] = this.Parameters is { Count: > 0 } ? this.Parameters : null;
            storage[nameof(this.RefSections)] = this.RefSections is { Count: > 0 } ? this.RefSections : null;
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.Parameters = storage.TryGet<List<string>>(nameof(this.Parameters)) ?? [];
            this.RefSections = storage.TryGet<List<string>>(nameof(this.RefSections), []);
        }

        #endregion
    }
}
