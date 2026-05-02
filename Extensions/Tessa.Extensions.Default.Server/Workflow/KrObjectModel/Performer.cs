#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrObjectModel
{
    /// <summary>
    /// Базовый класс для прокси объектной модели на настройки этапа.
    /// Для сериализации не используется, StorageObject нужен исключительно для декорирования настроек.
    /// </summary>
    [StorageObjectGenerator]
    public partial class Performer :
        StorageObject,
        IEquatable<Performer>,
        ISealable
    {
        #region fields

        private static readonly Dictionary<string, object?> emptyDict = new(DefaultCapacity, StringComparer.Ordinal);

        #endregion

        #region constructors

        /// <doc path='info[@type="StorageObject" and @item=".ctor:storage"]'/>
        protected Performer(
            Dictionary<string, object?> storage)
            : base(storage)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="performerID"><inheritdoc cref="PerformerID" path="/summary"/></param>
        /// <param name="performerName"><inheritdoc cref="PerformerName" path="/summary"/></param>
        public Performer(
            Guid performerID,
            string? performerName)
            : base(emptyDict)
        {
            this.PerformerID = performerID;
            this.PerformerName = performerName;
        }

        #endregion

        #region properties

        /// <summary>
        /// Идентификатор строки исполнителя. Используется только для представления в виртуальных секциях
        /// </summary>
        public virtual Guid RowID => Guid.Empty;

        /// <summary>
        /// Значение, показывающее, что исполнитель является SQL исполнителем.
        /// </summary>
        public virtual bool IsSql => false;

        /// <summary>
        /// Идентификатор роли исполнителя.
        /// </summary>
        public virtual Guid PerformerID { get; }

        /// <summary>
        /// Название роли исполнителя.
        /// </summary>
        public virtual string? PerformerName { get; }

        /// <summary>
        /// Идентификатор этапа.
        /// </summary>
        public virtual Guid StageRowID => Guid.Empty;

        #endregion

        #region Operators

        /// <doc path='info[@type="object" and @item="OperatorEquals"]'/>
        public static bool operator ==(Performer? left, Performer? right) => Equals(left, right);

        /// <doc path='info[@type="object" and @item="OperatorNotEquals"]'/>
        public static bool operator !=(Performer? left, Performer? right) => !Equals(left, right);

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override string ToString() =>
            $"{DebugHelper.GetTypeName(this)}"
            + $": RowID = {this.RowID:B}"
            + $", ID = {this.PerformerID:B}"
            + $", Name = {this.PerformerName}";

        /// <inheritdoc />
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is Performer approver && this.Equals(approver);
        }

        /// <inheritdoc />
        public override int GetHashCode() =>
            RuntimeHelpers.GetHashCode(this);

        #endregion

        #region IEquatable

        /// <inheritdoc/>
        /// <remarks>Сравнение выполняется по: <see cref="RowID"/>, <see cref="PerformerID"/> и <see cref="PerformerName"/>.</remarks>
        public bool Equals(Performer? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.RowID.Equals(other.RowID)
                && this.PerformerID.Equals(other.PerformerID)
                && string.Equals(this.PerformerName, other.PerformerName, StringComparison.Ordinal);
        }

        #endregion

        #region ISealable Members

        /// <inheritdoc/>
        public bool IsSealed { get; } = true;

        /// <inheritdoc/>
        public void Seal()
        {
        }

        #endregion

    }
}
