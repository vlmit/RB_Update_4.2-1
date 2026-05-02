#nullable enable
using System;
using System.Threading;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Tessa.Test.Default.Shared.Kr;

namespace Tessa.Test.Default.Shared.Roles
{
    /// <summary>
    /// Объект-помощник для контекстной роли.
    /// </summary>
    public sealed class ContextRoleBuilder : RoleBuilderBase<ContextRoleBuilder>
    {
        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием значений его зависимостей.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        public ContextRoleBuilder(ICardLifecycleCompanionDependencies deps)
            : this(Guid.NewGuid(), deps)
        {
        }

        /// <summary>
        /// Создаёт экземпляр класса с указанием значений его идентификатора и зависимостей.
        /// </summary>
        /// <param name="cardID">Идентификатор роли.</param>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        public ContextRoleBuilder(Guid cardID, ICardLifecycleCompanionDependencies deps)
            : base(cardID, RoleHelper.ContextRoleTypeID, RoleHelper.ContextRoleTypeName, deps)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Устанавливает SQL-текст контекстной роли (с плейсхолдерами контекстных ролей).
        /// </summary>
        /// <param name="value">SQL-текст.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public ContextRoleBuilder SetSqlText(string? value) =>
            this.SetValue(RoleStrings.ContextRoles, "SqlText", value);

        /// <summary>
        /// Возвращает SQL-текст контекстной роли (с плейсхолдерами контекстных ролей).
        /// </summary>
        /// <returns>SQL-текст.</returns>
        public string? GetSqlText() =>
            this.TryGetValue<string>(RoleStrings.ContextRoles, "SqlText");

        #endregion
    }
}
