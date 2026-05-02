#nullable enable

using System.Collections.Generic;
using Tessa.Content.Avatars;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Client.Views.Avatars
{
    /// <summary>
    /// Settings for <see cref="UserAvatarInRowViewExtension"/> used also in <see cref="UserAvatarInRowViewExtensionConfigurator"/>.
    /// </summary>
    public sealed class UserAvatarInRowViewExtensionSettings :
        StorageSerializable
    {
        #region Properties

        /// <summary>
        /// Add to the column with an alias.
        /// </summary>
        public string? DestinationColumn { get; set; }

        /// <summary>
        /// User ID column alias.
        /// </summary>
        public string? IDColumn { get; set; }

        /// <summary>
        /// Shape of the avatar.
        /// </summary>
        public AvatarShape AvatarShape { get; set; }

        /// <summary>
        /// Size of the avatar.
        /// </summary>
        public AvatarSize AvatarSize { get; set; }

        /// <summary>
        /// Kind of avatar content.
        /// </summary>
        public AvatarContentKind AvatarContentKind { get; set; }

        #endregion

        #region IStorageSerializable Members

        /// <inheritdoc/>
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.DestinationColumn = storage.TryGet<string>(nameof(this.DestinationColumn));
            this.IDColumn = storage.TryGet<string>(nameof(this.IDColumn));
            this.AvatarShape = storage.TryConvertEnum<AvatarShape>(nameof(this.AvatarShape)) ?? AvatarShape.Circle;
            this.AvatarSize = storage.TryConvertEnum<AvatarSize>(nameof(this.AvatarSize)) ?? AvatarSize.Medium;
            this.AvatarContentKind = storage.TryConvertEnum<AvatarContentKind>(nameof(this.AvatarContentKind)) ?? AvatarContentKind.Avatar;
        }

        /// <inheritdoc/>
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.DestinationColumn)] = this.DestinationColumn;
            storage[nameof(this.IDColumn)] = this.IDColumn;
            storage[nameof(this.AvatarShape)] = this.AvatarShape.ToString();
            storage[nameof(this.AvatarSize)] = this.AvatarSize.ToString();
            storage[nameof(this.AvatarContentKind)] = this.AvatarContentKind.ToString();
        }

        #endregion
    }
}
