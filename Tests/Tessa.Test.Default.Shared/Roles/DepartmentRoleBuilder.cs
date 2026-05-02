#nullable enable
using System;
using System.Threading;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Tessa.Test.Default.Shared.Kr;

namespace Tessa.Test.Default.Shared.Roles
{
    /// <summary>
    /// Объект-помощник для подразделения.
    /// </summary>
    public sealed class DepartmentRoleBuilder : RoleBuilderBase<DepartmentRoleBuilder>
    {
        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием значений его зависимостей.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        public DepartmentRoleBuilder(ICardLifecycleCompanionDependencies deps)
            : this(Guid.NewGuid(), deps)
        {
        }

        /// <summary>
        /// Создаёт экземпляр класса с указанием значений его идентификатора и зависимостей.
        /// </summary>
        /// <param name="cardID">Идентификатор роли.</param>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        public DepartmentRoleBuilder(Guid cardID, ICardLifecycleCompanionDependencies deps)
            : base(cardID, RoleHelper.DepartmentRoleTypeID, RoleHelper.DepartmentRoleTypeName, deps)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Добавляет строку в состав роли с информацией о пользователе.
        /// </summary>
        /// <param name="userID">Идентификатор пользователя.</param>
        /// <param name="userName">Имя пользователя.</param>
        /// <param name="isDeputy">Признак того, что добавляется запись о замещении.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public DepartmentRoleBuilder AddUser(Guid userID, string userName, bool isDeputy = false) =>
            this.AddUserCore(RoleType.Department, userID, userName, isDeputy);

        /// <summary>
        /// Добавляет строку в состав роли с информацией о пользователе.
        /// </summary>
        /// <param name="user">Добавляемый пользователь.</param>
        /// <param name="isDeputy">Признак того, что добавляется запись о замещении.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public DepartmentRoleBuilder AddUser(PersonalRoleBuilder user, bool isDeputy = false) =>
            this.AddUser(user.CardID, user.GetName() ?? string.Empty, isDeputy);

        #endregion
    }
}
