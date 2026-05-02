#nullable enable

using System;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess
{
    /// <summary>
    /// Дескриптор обработчика тайлов вторичных процессов.
    /// </summary>
    public sealed class KrSecondaryProcessTileHandlerDescriptor :
        IEquatable<KrSecondaryProcessTileHandlerDescriptor>
    {
        #region Properties

        /// <summary>
        /// Идентификатор обработчика.
        /// </summary>
        public required Guid ID { get; init; }

        /// <summary>
        /// Название обработчика.
        /// </summary>
        public required string Name { get; init; }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            obj is KrSecondaryProcessTileHandlerDescriptor descriptor
            && this.Equals(descriptor);

        /// <inheritdoc/>
        public override int GetHashCode() => this.ID.GetHashCode();

        #endregion

        #region IEquatable<KrSecondaryProcessTileHandlerDescriptor> Members

        /// <inheritdoc/>
        public bool Equals(KrSecondaryProcessTileHandlerDescriptor? other) =>
            other is not null && other.ID == this.ID;

        #endregion

        #region Operators

        /// <summary>
        /// Преобразует идентификатор обработчика в дескриптор обработчика тайла неявным образом.
        /// </summary>
        /// <param name="value"><inheritdoc cref="ID" path="/summary"/></param>
        public static implicit operator KrSecondaryProcessTileHandlerDescriptor(Guid value) =>
            new()
            {
                ID = value,
                Name = string.Empty,
            };

        #endregion
    }
}
