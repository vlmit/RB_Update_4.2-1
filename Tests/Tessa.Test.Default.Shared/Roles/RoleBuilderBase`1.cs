#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Tessa.Test.Default.Shared.Kr;

namespace Tessa.Test.Default.Shared.Roles
{
    /// <summary>
    /// Базовый объект-помощник для ролей.
    /// </summary>
    /// <typeparam name="T">Тип текущего объекта.</typeparam>
    /// <param name="cardID">Идентификатор роли.</param>
    /// <param name="cardTypeID">Идентификатор типа карточки роли.</param>
    /// <param name="cardTypeName">Имя типа карточки роли.</param>
    /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
    public abstract class RoleBuilderBase<T>(
        Guid cardID,
        Guid cardTypeID,
        string cardTypeName,
        ICardLifecycleCompanionDependencies deps)
        : CardLifecycleCompanion<T>(cardID, cardTypeID, cardTypeName, deps), INamedEntry
        where T : RoleBuilderBase<T>
    {
        #region Protected Methods

        /// <summary>
        /// Добавляет строку в состав роли с информацией о пользователе.
        /// </summary>
        /// <param name="roleType">Тип роли.</param>
        /// <param name="userID">Идентификатор пользователя.</param>
        /// <param name="userName">Имя пользователя.</param>
        /// <param name="isDeputy">Признак того, что добавляется запись о замещении.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        protected T AddUserCore(RoleType roleType, Guid userID, string userName, bool isDeputy) =>
            ((T) this).AddRow(RoleStrings.RoleUsers, rowValues:
            [
                new("TypeID", Int32Boxes.Box((int) roleType)),
                new("UserID", userID),
                new("UserName", userName),
                new("IsDeputy", BooleanBoxes.Box(isDeputy))
            ]);

        #endregion

        #region Methods

        /// <summary>
        /// Устанавливает имя роли (краткое имя для сотрудника).
        /// </summary>
        /// <param name="value">Имя роли.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public virtual T SetName(string value) =>
            ((T) this).SetValue(RoleStrings.Roles, "Name", value);

        /// <summary>
        /// Возвращает имя роли (краткое имя для сотрудника).
        /// </summary>
        /// <returns>Имя роли.</returns>
        public virtual string? GetName() =>
            this.TryGetValue<string>(RoleStrings.Roles, "Name");

        /// <summary>
        /// Устанавливает временную зону.
        /// </summary>
        /// <param name="id">Идентификатор временной зоны.</param>
        /// <param name="shortName">Короткое имя временной зоны.</param>
        /// <param name="utcOffsetMinutes">Смещение относительно UTC в минутах.</param>
        /// <param name="codeName">Код временной зоны.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public T SetTimeZone(int? id, string? shortName, int? utcOffsetMinutes, string? codeName) =>
            ((T) this)
            .SetValue(RoleStrings.Roles, "TimeZoneID", id)
            .SetValue(RoleStrings.Roles, "TimeZoneShortName", shortName)
            .SetValue(RoleStrings.Roles, "TimeZoneUtcOffsetMinutes", Int32Boxes.Box(utcOffsetMinutes))
            .SetValue(RoleStrings.Roles, "TimeZoneCodeName", codeName);

        /// <summary>
        /// Возвращает информацию по временной зоне.
        /// </summary>
        /// <returns>Информацию по временной зоне.</returns>
        public (int? ID, string? ShortName, int? UtcOffsetMinutes, string? CodeName) GetTimeZone() =>
        (
            this.TryGetValue<int?>(RoleStrings.Roles, "TimeZoneID"),
            this.TryGetValue<string>(RoleStrings.Roles, "TimeZoneShortName"),
            this.TryGetValue<int?>(RoleStrings.Roles, "TimeZoneUtcOffsetMinutes"),
            this.TryGetValue<string>(RoleStrings.Roles, "TimeZoneCodeName")
        );

        /// <summary>
        /// Устанавливает значение, показывающее, требуется ли наследовать временную зону или нет.
        /// </summary>
        /// <param name="value">Значение <see langword="true"/>, если временная зона должна наследоваться, иначе - <see langword="false"/>.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public T SetInheritTimeZone(bool value) =>
            ((T) this).SetValue(RoleStrings.Roles, "InheritTimeZone", BooleanBoxes.Box(value));

        /// <summary>
        /// Возвращает значение, показывающее, требуется ли наследовать временную зону или нет.
        /// </summary>
        /// <returns>Значение <see langword="true"/>, если временная зона наследуется, иначе - <see langword="false"/>.</returns>
        public bool GetInheritTimeZone() =>
            this.TryGetValue<bool>(RoleStrings.Roles, "InheritTimeZone");

        /// <summary>
        /// Устанавливает календарь.
        /// </summary>
        /// <param name="calendarID">Идентификатор календаря.</param>
        /// <param name="calendarName">Имя календаря.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public T SetCalendar(Guid? calendarID, string? calendarName) =>
            ((T) this)
            .SetValue(RoleStrings.Roles, "CalendarID", calendarID)
            .SetValue(RoleStrings.Roles, "CalendarName", calendarName);

        /// <summary>
        /// Возвращает информацию о календаре.
        /// </summary>
        /// <returns>Информацию о календаре.</returns>
        public (Guid? CalendarID, string? CalendarName) GetCalendar() =>
        (
            this.TryGetValue<Guid?>(RoleStrings.Roles, "CalendarID"),
            this.TryGetValue<string?>(RoleStrings.Roles, "CalendarName")
        );

        /// <summary>
        /// Устанавливает значение, показывающее, требуется ли календарь или нет.
        /// </summary>
        /// <param name="value">Значение <see langword="true"/>, если календарь должен наследоваться, иначе - <see langword="false"/>.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public T SetInheritCalendar(bool value) =>
            ((T) this).SetValue(RoleStrings.Roles, "InheritCalendar", BooleanBoxes.Box(value));

        /// <summary>
        /// Возвращает значение, показывающее, требуется ли наследовать календарь или нет.
        /// </summary>
        /// <returns>Значение <see langword="true"/>, если календарь наследуется, иначе - <see langword="false"/>.</returns>
        public bool GetInheritCalendar() =>
            this.TryGetValue<bool>(RoleStrings.Roles, "InheritCalendar");

        #endregion

        #region INamedEntry Members

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException">Установка значения не поддерживается.</exception>
        Guid INamedEntry.ID
        {
            get => this.CardID;
            set => throw new NotSupportedException("Setting the value is not supported.");
        }

        /// <inheritdoc/>
        [DisallowNull]
        string? INamedEntry.Name
        {
            get => this.GetName();
            set => this.SetName(value);
        }

        #endregion

        #region INamedItem Members

        /// <inheritdoc/>
        string INamedItem.Name => this.GetName() ?? string.Empty;

        #endregion
    }
}
