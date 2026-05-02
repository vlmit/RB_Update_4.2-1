#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;

namespace Tessa.Extensions.Default.Shared.Workflow.KrCompilers
{
    /// <summary>
    /// Положение относительно этапов, добавленных вручную.
    /// </summary>
    public readonly struct GroupPosition :
        IEquatable<GroupPosition>,
        IComparable<GroupPosition>
    {
        #region Properties

        /// <summary>
        /// Информация о положении не определена.
        /// </summary>
        public static GroupPosition Unspecified { get; } = new GroupPosition(null);

        /// <summary>
        /// В начале группы.
        /// </summary>
        /// <remarks>
        /// Этапы этого шаблона в маршруте документа будут всегда в начале своей группы. Добавленные пользователем вручную этапы будут всегда идти после них.
        /// </remarks>
        public static GroupPosition AtFirst { get; } = new GroupPosition(0);

        /// <summary>
        /// В конце группы.
        /// </summary>
        /// <remarks>
        /// Этапы из этого шаблона в маршруте документа будут всегда в конце своей группы и, даже если пользователь после пересчета вручную будет добавлять новые этапы, они добавятся не в конец списка, а будут расположены в списке перед этапами из шаблона.
        /// </remarks>
        public static GroupPosition AtLast { get; } = new GroupPosition(1);

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GroupPosition"/>.
        /// </summary>
        /// <param name="id"><inheritdoc cref="ID" path="/summary"/></param>
        public GroupPosition(int? id) =>
            this.ID = id;

        #endregion

        #region Properties

        /// <summary>
        /// Идентификатор положения относительно этапов, добавленных вручную.
        /// </summary>
        public int? ID { get; }

        #endregion

        #region Static Methods

        /// <inheritdoc cref="GetByID(int?)"/>
        public static GroupPosition GetByID(object? id)
        {
            return id is int idInt
                ? GetByID(idInt)
                : Unspecified;
        }

        /// <summary>
        /// Возвращает объект <see cref="GroupPosition"/> в соответствии с заданным идентификатором положения относительно этапов, добавленных вручную.
        /// </summary>
        /// <param name="id">Идентификатор положения относительно этапов, добавленных вручную.</param>
        /// <returns>Объект <see cref="GroupPosition"/>, соответствующий заданному идентификатору положения относительно этапов, добавленных вручную, или значение <see cref="Unspecified"/>, если заданное значение не соответствует ни одному из известных положений этапов.</returns>
        public static GroupPosition GetByID(int? id)
        {
            if (id == AtFirst.ID)
            {
                return AtFirst;
            }

            if (id == AtLast.ID)
            {
                return AtLast;
            }

            return Unspecified;
        }

        #endregion

        #region Operators

        /// <doc path='info[@type="object" and @item="OperatorEquals"]'/>
        public static bool operator ==(GroupPosition first, GroupPosition second) =>
            first.Equals(second);

        /// <doc path='info[@type="object" and @item="OperatorNotEquals"]'/>
        public static bool operator !=(GroupPosition first, GroupPosition second) =>
            !first.Equals(second);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is GroupPosition gp && this.Equals(gp);

        /// <inheritdoc/>
        public override int GetHashCode() => this.ID.GetHashCode();

        /// <inheritdoc/>
        public override string? ToString() => FormatNullable(this.ID);

        #endregion

        #region IEquatable<T> Members

        /// <inheritdoc/>
        public bool Equals(GroupPosition other) =>
            other.ID == this.ID;

        #endregion

        #region IComparable<T> Member

        /// <inheritdoc/>
        public int CompareTo(GroupPosition other) =>
            (this.ID ?? 0.5).CompareTo(other.ID ?? 0.5);

        #endregion
    }
}
