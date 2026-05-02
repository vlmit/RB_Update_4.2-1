#nullable enable

using System;
using System.Threading;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared.Kr.Routes
{
    /// <summary>
    /// Базовый абстрактный класс для объектов маршрутов.
    /// </summary>
    /// <typeparam name="T"><inheritdoc cref="CardLifecycleCompanion{T}" path="/typeparam[@name='T']"/></typeparam>
    public abstract class KrRouteObjectBuilder<T> :
        CardLifecycleCompanion<T>
        where T : KrRouteObjectBuilder<T>
    {
        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <param name="cardTypeID">Идентификатор типа карточки.</param>
        /// <param name="cardTypeName">Название типа карточки.</param>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        protected KrRouteObjectBuilder(
            Guid cardID,
            Guid? cardTypeID,
            string cardTypeName,
            ICardLifecycleCompanionDependencies deps)
            : base(cardID, cardTypeID, cardTypeName, deps)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="card">Карточка, жизненным циклом которой необходимо управлять.</param>
        /// <param name="deps">Зависимости, используемые при взаимодействии с карточкой.</param>
        protected KrRouteObjectBuilder(
            Card card,
            ICardLifecycleCompanionDependencies deps)
            : base(card, deps)
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Задаёт указанный тип документа или карточки в качестве ограничения при пересчёте.
        /// </summary>
        /// <param name="typeID">Идентификатор типа документа или карточки.</param>
        /// <param name="typeName">Название типа документа или карточки.</param>
        /// <param name="isDocType">Значение <see langword="true"/>, если указанный тип является типом документа, иначе - <see langword="false"/>, типом карточки.</param>
        /// <returns>Объект <see cref="KrStageTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public T ForType(
            Guid typeID,
            string typeName,
            bool isDocType)
        {
            return ((T) this).AddRow(
                KrConstants.KrStageTypes.Name,
                KrConstants.KrStageTypes.TypeID,
                KrConstants.KrStageTypes.TypeCaption,
                KrConstants.KrStageTypes.TypeIsDocType,
                typeID,
                typeName,
                BooleanBoxes.Box(isDocType));
        }

        /// <summary>
        /// Удаляет все типы из ограничения при пересчёте.
        /// </summary>
        /// <returns>Объект <see cref="KrStageTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public T RemoveAllTypes() =>
            ((T) this).RemoveAllRows(KrConstants.KrStageTypes.Name);

        /// <summary>
        /// Удаляет тип карточки или тип документа из ограничения при пересчёте.
        /// </summary>
        /// <param name="id">Идентификатор удаляемого типа.</param>
        /// <returns>Объект <see cref="KrStageTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public T RemoveType(Guid id) =>
            ((T) this).RemoveRow(
                KrConstants.KrStageTypes.Name,
                row => row.Get<Guid>(KrConstants.KrStageTypes.TypeID) == id);

        /// <summary>
        /// Задаёт указанный тип карточки в качестве ограничения при пересчёте.
        /// </summary>
        /// <param name="typeID">Идентификатор типа карточки.</param>
        /// <param name="typeName">Название типа карточки.</param>
        /// <returns>Объект <see cref="KrStageTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public T ForCardType(
            Guid typeID,
            string typeName) =>
            this.ForType(typeID, typeName, false);

        /// <summary>
        /// Задаёт указанный тип документа в качестве ограничения при пересчёте.
        /// </summary>
        /// <param name="typeID">Идентификатор типа документа.</param>
        /// <param name="typeName">Название типа документа.</param>
        ///<returns>Объект <see cref="KrStageTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public T ForDocType(
            Guid typeID,
            string typeName) =>
            this.ForType(typeID, typeName, true);

        /// <summary>
        /// Задаёт указанную роль в качестве ограничения при пересчёте.
        /// </summary>
        /// <param name="roleID">Идентификатор роли.</param>
        /// <param name="roleName">Название роли.</param>
        /// <returns>Объект <see cref="KrStageTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public T ForRole(
            Guid roleID,
            string roleName)
        {
            return ((T) this).AddRow(
                KrConstants.KrStageRoles.Name,
                KrConstants.KrStageRoles.RoleID,
                KrConstants.KrStageRoles.RoleName,
                roleID,
                roleName);
        }

        /// <summary>
        /// Удаляет все роли из ограничения при пересчёте.
        /// </summary>
        /// <returns>Объект <see cref="KrStageTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public T RemoveAllRoles() =>
            ((T) this).RemoveAllRows(KrConstants.KrStageRoles.Name);

        /// <summary>
        /// Удаляет роль из ограничения при пересчёте.
        /// </summary>
        /// <param name="id">Идентификатор удаляемой роли.</param>
        /// <returns>Объект <see cref="KrStageTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public T RemoveRole(Guid id) =>
            ((T) this).RemoveRow(
                KrConstants.KrStageRoles.Name,
                row => row.Get<Guid>(KrConstants.KrStageRoles.RoleID) == id);

        #endregion
    }
}
