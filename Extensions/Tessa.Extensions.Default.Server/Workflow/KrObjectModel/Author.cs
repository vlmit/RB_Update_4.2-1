#nullable enable

using System;
using System.Runtime.CompilerServices;
using Tessa.Platform;

namespace Tessa.Extensions.Default.Server.Workflow.KrObjectModel
{
    /// <summary>
    /// Объект, представляющий автора.
    /// </summary>
    public class Author :
        IEquatable<Author>,
        ISealable
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса. Используется только для сериализации.
        /// </summary>
        protected Author()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="roleID"><inheritdoc cref="AuthorID" path="/summary"/></param>
        /// <param name="roleName"><inheritdoc cref="AuthorName" path="/summary"/></param>
        public Author(
            Guid roleID,
            string? roleName)
        {
            this.AuthorID = roleID;
            this.AuthorName = roleName;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Идентификатор роли автора.
        /// </summary>
        public virtual Guid AuthorID { get; }

        /// <summary>
        /// Название роли автора.
        /// </summary>
        public virtual string? AuthorName { get; }

        #endregion

        #region Operators

        /// <doc path='info[@type="object" and @item="OperatorEquals"]'/>
        public static bool operator ==(Author? left, Author? right) => Equals(left, right);

        /// <doc path='info[@type="object" and @item="OperatorNotEquals"]'/>
        public static bool operator !=(Author? left, Author? right) => !Equals(left, right);

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override string? ToString() =>
            $"Author: ID = {this.AuthorID:B}, Name = {this.AuthorName}";

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is Author author && this.Equals(author);
        }

        /// <inheritdoc />
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

        #endregion

        #region IEquatable<Author> Members

        /// <inheritdoc />
        public bool Equals(Author? other) => other is not null && this.AuthorID == other.AuthorID;

        #endregion

        #region ISealable Members

        /// <inheritdoc />
        public bool IsSealed { get; } = true;

        /// <inheritdoc />
        public void Seal()
        {
        }

        #endregion

    }
}
