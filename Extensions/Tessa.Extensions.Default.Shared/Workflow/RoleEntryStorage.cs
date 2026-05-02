#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Roles;

namespace Tessa.Extensions.Default.Shared.Workflow
{
    /// <summary>
    /// Предоставляет информацию по роли.
    /// </summary>
    [StorageObjectGenerator(GenerateDefaultConstructor = false)]
    public sealed partial class RoleEntryStorage :
        StorageObject,
        IRoleUser
    {
        #region Storage Properties

        /// <inheritdoc/>
        public Guid ID
        {
            get => this.TryGet<Guid>(nameof(this.ID));
            set => this.Set(nameof(this.ID), value);
        }

        /// <inheritdoc/>
        [AllowNull]
        public string Name
        {
            get => this.TryGet<string>(nameof(this.Name)) ?? string.Empty;
            set => this.Set(nameof(this.Name), value);
        }

        /// <summary>
        /// Идентификатор строки.
        /// </summary>
        public Guid? RowID
        {
            get => this.Get<Guid?>(nameof(this.RowID));
            set => this.Set(nameof(this.RowID), value);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="RoleEntryStorage"/>.
        /// </summary>
        /// <param name="id"><inheritdoc cref="ID" path="/summary"/></param>
        /// <param name="name"><inheritdoc cref="Name" path="/summary"/></param>
        public RoleEntryStorage(Guid id, string? name)
            : this(null, id, name)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="RoleEntryStorage"/>.
        /// </summary>
        /// <param name="rowID"><inheritdoc cref="RowID" path="/summary"/></param>
        /// <param name="id"><inheritdoc cref="ID" path="/summary"/></param>
        /// <param name="name"><inheritdoc cref="Name" path="/summary"/></param>
        public RoleEntryStorage(Guid? rowID, Guid id, string? name)
            : base(new(DefaultCapacity, StringComparer.Ordinal))
        {
            this.ID = id;
            this.RowID = rowID;
            this.Name = name;
        }

        /// <doc path='info[@type="StorageObject" and @item=".ctor:storage"]'/>
        public RoleEntryStorage(
            Dictionary<string, object?> storage)
            : base(storage)
        {
            this.Init(nameof(this.ID), GuidBoxes.Empty);
            this.Init(nameof(this.RowID), null);
            this.Init(nameof(this.Name), null);
        }

        #endregion
    }
}
