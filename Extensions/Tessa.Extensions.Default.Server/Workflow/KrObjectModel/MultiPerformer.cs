#nullable enable
using System;
using System.Collections.Generic;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrObjectModel
{
    /// <summary>
    /// Реализация объекта исполнителя для этапа с множественными исполнителями.
    /// Работает поверх хранилища <see cref="KrConstants.KrPerformersVirtual"/>.
    /// </summary>
    [StorageObjectGenerator]
    public sealed partial class MultiPerformer :
        Performer
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="MultiPerformer"/>.
        /// Запечатанность не переносится.
        /// </summary>
        /// <param name="performer">Копируемый объект.</param>
        public MultiPerformer(
            MultiPerformer performer)
            : this(
                  NotNullOrThrow(performer).RowID,
                  performer.PerformerID,
                  performer.PerformerName,
                  performer.StageRowID,
                  performer.IsSql)
        {
        }

        /// <doc path='info[@type="StorageObject" and @item=".ctor:storage"]'/>
        public MultiPerformer(
            Dictionary<string, object?> storage)
            : base(storage)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="MultiPerformer"/>.
        /// </summary>
        /// <param name="rowID"><inheritdoc cref="RowID" path="/summary"/></param>
        /// <param name="performerID"><inheritdoc cref="PerformerID" path="/summary"/></param>
        /// <param name="performerName"><inheritdoc cref="PerformerName" path="/summary"/></param>
        /// <param name="stageRowID"><inheritdoc cref="StageRowID" path="/summary"/></param>
        /// <param name="isSql"><inheritdoc cref="IsSql" path="/summary"/></param>
        public MultiPerformer(
            Guid rowID,
            Guid performerID,
            string? performerName,
            Guid stageRowID,
            bool isSql = false)
            : base(new Dictionary<string, object?>(6, StringComparer.Ordinal))
        {
            this.Init(KrConstants.KrPerformersVirtual.RowID, rowID);
            this.Init(KrConstants.KrPerformersVirtual.PerformerID, performerID);
            this.Init(KrConstants.KrPerformersVirtual.PerformerName, performerName);
            this.Init(KrConstants.KrPerformersVirtual.StageRowID, stageRowID);
            this.Init(KrConstants.KrPerformersVirtual.SQLApprover, BooleanBoxes.Box(isSql));
            this.Init(KrConstants.KrPerformersVirtual.Order, Int32Boxes.Zero);
        }

        /// <summary>
        /// ДИнициализирует новый экземпляр класса <see cref="MultiPerformer"/>.
        /// </summary>
        /// <param name="id"><inheritdoc cref="PerformerID" path="/summary"/></param>
        /// <param name="name"><inheritdoc cref="PerformerName" path="/summary"/></param>
        /// <param name="stageRowID"><inheritdoc cref="StageRowID" path="/summary"/></param>
        /// <param name="isSql"><inheritdoc cref="IsSql" path="/summary"/></param>
        public MultiPerformer(
            Guid id,
            string? name,
            Guid stageRowID,
            bool isSql = false)
            : this(Guid.NewGuid(), id, name, stageRowID, isSql)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="MultiPerformer"/>.
        /// </summary>
        /// <param name="id"><inheritdoc cref="PerformerID" path="/summary"/></param>
        /// <param name="name"><inheritdoc cref="PerformerName" path="/summary"/></param>
        /// <param name="stageRowID"><inheritdoc cref="StageRowID" path="/summary"/></param>
        /// <param name="isSql"><inheritdoc cref="IsSql" path="/summary"/></param>
        public MultiPerformer(
            string id,
            string name,
            Guid stageRowID,
            bool isSql = false)
            : this(Guid.Parse(id), name, stageRowID, isSql)
        {
        }

        #endregion

        #region Properties

        /// <inheritdoc/>
        public override Guid RowID => this.Get<Guid>(KrConstants.KrPerformersVirtual.RowID);

        /// <inheritdoc/>
        public override bool IsSql => this.Get<bool>(KrConstants.KrPerformersVirtual.SQLApprover);

        /// <inheritdoc/>
        public override Guid PerformerID => this.Get<Guid>(KrConstants.KrPerformersVirtual.PerformerID);

        /// <inheritdoc/>
        public override string? PerformerName => this.Get<string>(KrConstants.KrPerformersVirtual.PerformerName);

        /// <inheritdoc/>
        public override Guid StageRowID => this.Get<Guid>(KrConstants.KrPerformersVirtual.StageRowID);

        #endregion
    }
}
