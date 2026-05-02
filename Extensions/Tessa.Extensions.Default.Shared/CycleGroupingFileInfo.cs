#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Cards;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared
{
    /// <summary>
    /// Информация о файле и цикле согласования.
    /// </summary>
    public sealed class CycleGroupingFileInfo :
        StorageSerializable
    {
        #region Properties

        /// <summary>
        /// Номер цикла согласования.
        /// </summary>
        public int Cycle { get; set; }

        /// <summary>
        /// Идентификатор файла.
        /// </summary>
        public Guid FileID { get; set; }

        /// <summary>
        /// Идентификатор версии файла.
        /// </summary>
        public Guid VersionID { get; set; }

        /// <summary>
        /// Номер версии файла.
        /// </summary>
        public int VersionNumber { get; set; }

        /// <summary>
        /// Размер версии файла.
        /// </summary>
        public long VersionSize { get; set; }

        /// <inheritdoc cref="CardFileSourceType" path="/summary"/>
        public CardFileSourceType VersionSource { get; set; }

        /// <summary>
        /// Дата и время создания версии файла.
        /// </summary>
        public DateTime VersionCreated { get; set; }

        /// <summary>
        /// Идентификатор пользователя, создавшего версию (изменившего файл).
        /// </summary>
        public Guid VersionCreatedByID { get; set; }

        /// <summary>
        /// Имя пользователя, создавшего версию (изменившего файл).
        /// </summary>
        public string? VersionCreatedByName { get; set; }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.Cycle)] = Int32Boxes.Box(this.Cycle);
            storage[nameof(this.FileID)] = this.FileID;
            storage[nameof(this.VersionID)] = this.VersionID;
            storage[nameof(this.VersionNumber)] = Int32Boxes.Box(this.VersionNumber);
            storage[nameof(this.VersionSize)] = Int64Boxes.Box(this.VersionSize);
            storage[nameof(this.VersionSource)] = Int32Boxes.Box((int) this.VersionSource);
            storage[nameof(this.VersionCreated)] = this.VersionCreated;
            storage[nameof(this.VersionCreatedByID)] = this.VersionCreatedByID;
            storage[nameof(this.VersionCreatedByName)] = this.VersionCreatedByName;
        }

        /// <inheritdoc/>
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.Cycle = storage.TryConvertInt32(nameof(this.Cycle)) ?? 0;
            this.FileID = storage.TryConvertGuid(nameof(this.FileID)) ?? Guid.Empty;
            this.VersionID = storage.TryConvertGuid(nameof(this.VersionID))?? Guid.Empty;
            this.VersionNumber = storage.TryConvertInt32(nameof(this.VersionNumber)) ?? 0;
            this.VersionSize = storage.TryConvertInt64(nameof(this.VersionSize)) ?? 0L;
            this.VersionSource = (CardFileSourceType) (storage.TryConvertInt32(nameof(this.VersionSource)) ?? 0);
            this.VersionCreated = storage.TryConvertDateTime(nameof(this.VersionCreated)) ?? DateTime.MinValue;
            this.VersionCreatedByID = storage.TryConvertGuid(nameof(this.VersionCreatedByID)) ?? Guid.Empty;
            this.VersionCreatedByName = storage.Get<string>(nameof(this.VersionCreatedByName));
        }

        #endregion
    }
}
