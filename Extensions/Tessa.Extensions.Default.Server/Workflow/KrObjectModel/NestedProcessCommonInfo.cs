#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrObjectModel
{
    /// <summary>
    /// Предоставляет информацию о вложенном процессе.
    /// </summary>
    [StorageObjectGenerator(GenerateDefaultConstructor = false)]
    public sealed partial class NestedProcessCommonInfo :
        ProcessCommonInfo
    {
        #region Constructors

        /// <inheritdoc cref="ProcessCommonInfo(Guid?, IDictionary{string, object?}, Guid?, Guid?, string?, Guid?, string?)"/>
        /// <param name="nestedProcessID"><inheritdoc cref="NestedProcessID" path="/summary"/></param>
        /// <param name="parentStageRowID"><inheritdoc cref="ParentStageRowID" path="/summary"/></param>
        /// <param name="nestedOrder"><inheritdoc cref="NestedOrder" path="/summary"/></param>
        public NestedProcessCommonInfo(
            Guid? currentStageRowID,
            IDictionary<string, object?>? info,
            Guid? secondaryProcessID,
            Guid nestedProcessID,
            Guid parentStageRowID,
            int nestedOrder)
            : base(
                  currentStageRowID,
                  info,
                  secondaryProcessID,
                  null,
                  null,
                  null,
                  null)
        {
            this.Init(nameof(this.NestedProcessID), GuidBoxes.Box(nestedProcessID));
            this.Init(nameof(this.ParentStageRowID), GuidBoxes.Box(parentStageRowID));
            this.Init(nameof(this.NestedOrder), Int32Boxes.Box(nestedOrder));
        }

        /// <inheritdoc cref="ProcessCommonInfo(Dictionary{string, object?})"/>
        public NestedProcessCommonInfo(
            Dictionary<string, object?> storage)
            : base(storage)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Идентификатор дочернего процесса.
        /// </summary>
        public Guid NestedProcessID
        {
            get => this.Get<Guid>(nameof(this.NestedProcessID));
            set => this.Set(nameof(this.NestedProcessID), value);
        }

        /// <summary>
        /// Идентификатор родительского этапа.
        /// </summary>
        public Guid ParentStageRowID
        {
            get => this.Get<Guid>(nameof(this.ParentStageRowID));
            set => this.Set(nameof(this.ParentStageRowID), value);
        }

        /// <summary>
        /// Порядковый номер дочернего процесса.
        /// </summary>
        public int NestedOrder
        {
            get => this.Get<int>(nameof(this.NestedOrder));
            set => this.Set(nameof(this.NestedOrder), value);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override Guid? AuthorID
        {
            get => this.Info.TryGet<Guid?>(nameof(NestedProcessCommonInfo) + "." + nameof(this.AuthorID));
            set => this.Info[nameof(NestedProcessCommonInfo) + "." + nameof(this.AuthorID)] = value;
        }

        /// <inheritdoc/>
        public override string? AuthorName
        {
            get => this.Info.TryGet<string>(nameof(NestedProcessCommonInfo) + "." + nameof(this.AuthorName));
            set => this.Info[nameof(NestedProcessCommonInfo) + "." + nameof(this.AuthorName)] = value;
        }

        /// <inheritdoc/>
        public override long AuthorTimestamp
        {
            get => this.Info.TryGet<long>(nameof(NestedProcessCommonInfo) + "." + nameof(this.AuthorTimestamp));
            set => this.Info[nameof(NestedProcessCommonInfo) + "." + nameof(this.AuthorTimestamp)] = Int64Boxes.Box(value);
        }

        /// <inheritdoc/>
        public override Guid? ProcessOwnerID
        {
            get => this.Info.TryGet<Guid?>(nameof(NestedProcessCommonInfo) + "." + nameof(this.ProcessOwnerID));
            set => this.Info[nameof(NestedProcessCommonInfo) + "." + nameof(this.ProcessOwnerID)] = value;
        }

        /// <inheritdoc/>
        public override string? ProcessOwnerName
        {
            get => this.Info.TryGet<string>(nameof(NestedProcessCommonInfo) + "." + nameof(this.ProcessOwnerName));
            set => this.Info[nameof(NestedProcessCommonInfo) + "." + nameof(this.ProcessOwnerName)] = value;
        }

        /// <inheritdoc/>
        public override long ProcessOwnerTimestamp
        {
            get => this.Info.TryGet<long>(nameof(NestedProcessCommonInfo) + "." + nameof(this.ProcessOwnerTimestamp));
            set => this.Info[nameof(NestedProcessCommonInfo) + "." + nameof(this.ProcessOwnerTimestamp)] = Int64Boxes.Box(value);
        }

        #endregion
    }
}
