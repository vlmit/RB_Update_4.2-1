#nullable enable

using System;
using System.Threading;
using Tessa.Cards;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.RefGroups;
using Tessa.Platform.Validation;
using Tessa.Test.Default.Shared.Kr;
using Tessa.Test.Default.Shared.Kr.Routes;

namespace Tessa.Test.Default.Shared.RefGroups
{
    /// <summary>
    /// Предоставляет методы для создания и модификации карточки типа группы ссылок.
    /// </summary>
    public sealed class RefGroupTypeBuilder :
        CardLifecycleCompanion<RefGroupTypeBuilder>,
        INamedEntry
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="RefGroupTypeBuilder"/>.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/>.</param>
        public RefGroupTypeBuilder(
            ICardLifecycleCompanionDependencies deps)
            : this(Guid.NewGuid(), deps)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="RefGroupTypeBuilder"/>.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки типа группы ссылок.</param>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/>.</param>
        public RefGroupTypeBuilder(
            Guid cardID,
            ICardLifecycleCompanionDependencies deps)
            : base(
                  cardID,
                  CardHelper.RefGroupTypeTypeID,
                  CardHelper.RefGroupTypeTypeName,
                  deps)
        {
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Устанавливает название типа группы ссылок.
        /// </summary>
        /// <param name="value">Название типа группы ссылок.</param>
        /// <returns>Объект <see cref="RefGroupTypeBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupTypeBuilder SetName(string value) =>
            this.SetField(RefGroupsHelper.RefGroupTypesName, value);

        /// <summary>
        /// Возвращает название типа группы ссылок.
        /// </summary>
        /// <returns>Название типа группы ссылок.</returns>
        public string? GetName() =>
            this.TryGetField<string>(RefGroupsHelper.RefGroupTypesName);

        /// <summary>
        /// Устанавливает RefSection значений группы.
        /// </summary>
        /// <param name="value">RefSection значений группы.</param>
        /// <returns>Объект <see cref="RefGroupTypeBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupTypeBuilder SetRefSection(string value) =>
            this.SetField(RefGroupsHelper.RefGroupTypesRefSection, value);

        /// <summary>
        /// Возвращает RefSection значений группы.
        /// </summary>
        /// <returns>RefSection значений группы.</returns>
        public string? GetRefSection() =>
            this.TryGetField<string>(RefGroupsHelper.RefGroupTypesRefSection);

        /// <summary>
        /// Устанавливает RefSection группы.
        /// </summary>
        /// <param name="value">RefSection группы.</param>
        /// <returns>Объект <see cref="RefGroupTypeBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupTypeBuilder SetGroupRefSection(string value) =>
            this.SetField(RefGroupsHelper.RefGroupTypesGroupRefSection, value);

        /// <summary>
        /// Возвращает RefSection группы.
        /// </summary>
        /// <returns>RefSection группы.</returns>
        public string? GetGroupRefSection() =>
            this.TryGetField<string>(RefGroupsHelper.RefGroupTypesGroupRefSection);

        /// <summary>
        /// Устанавливает тип ключа.
        /// </summary>
        /// <param name="value"><inheritdoc cref="RefGroupKeyType" path="/summary"/></param>
        /// <returns>Объект <see cref="RefGroupTypeBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupTypeBuilder SetKeyType(RefGroupKeyType value)
        {
            return this
                .SetField(RefGroupsHelper.RefGroupTypesKeyTypeID, Int32Boxes.Box((int) value))
                .SetField(RefGroupsHelper.RefGroupTypesKeyTypeName, value.ToString());
        }

        /// <summary>
        /// Возвращает тип ключа.
        /// </summary>
        /// <returns><inheritdoc cref="RefGroupKeyType" path="/summary"/></returns>
        public RefGroupKeyType GetKeyType() =>
            (RefGroupKeyType) this.TryGetField<int>(RefGroupsHelper.RefGroupTypesKeyTypeID);

        /// <summary>
        /// Устанавливает название колонки с именем значения.
        /// </summary>
        /// <param name="value">Название колонки с именем значения.</param>
        /// <returns>Объект <see cref="RefGroupTypeBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupTypeBuilder SetValueNameColumn(string value) =>
            this.SetField(RefGroupsHelper.RefGroupTypesValueNameColumn, value);

        /// <summary>
        /// Возвращает название колонки с именем значения.
        /// </summary>
        /// <returns>Название колонки с именем значения.</returns>
        public string? GetValueNameColumn() =>
            this.TryGetField<string>(RefGroupsHelper.RefGroupTypesValueNameColumn);

        /// <summary>
        /// Устанавливает SQL-скрипт для расчёта значений.
        /// </summary>
        /// <param name="value">SQL-скрипт для расчёта значений.</param>
        /// <returns>Объект <see cref="RefGroupTypeBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupTypeBuilder SetSqlScript(string? value) =>
            this.SetField(RefGroupsHelper.RefGroupTypesSQL, value);

        /// <summary>
        /// Возвращает SQL-скрипт для расчёта значений.
        /// </summary>
        /// <returns>SQL-скрипт для расчёта значений.</returns>
        public string? GetSqlScript() =>
            this.TryGetField<string>(RefGroupsHelper.RefGroupTypesSQL);

        /// <summary>
        /// Устанавливает C#-скрипт для расчёта значений.
        /// </summary>
        /// <param name="value">C#-скрипт для расчёта значений.</param>
        /// <returns>Объект <see cref="RefGroupTypeBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public RefGroupTypeBuilder SetCSharpScript(string? value) =>
            this.SetField(RefGroupsHelper.RefGroupTypesScript, value);

        /// <summary>
        /// Возвращает C#-скрипт для расчёта значений.
        /// </summary>
        /// <returns>C#-скрипт для расчёта значений.</returns>
        public string? GetCSharpScript() =>
            this.TryGetField<string>(RefGroupsHelper.RefGroupTypesScript);

        #endregion

        #region INamedEntry Members

        /// <summary>
        /// Возвращает идентификатор объекта.
        /// </summary>
        /// <exception cref="NotSupportedException">Установка значения не поддерживается.</exception>
        Guid INamedEntry.ID
        {
            get => this.CardID;
            set => throw new NotSupportedException("Setting the value is not supported.");
        }

        /// <summary>
        /// Возвращает название объекта.
        /// </summary>
        /// <exception cref="NotSupportedException">Установка значения не поддерживается.</exception>
        string? INamedEntry.Name
        {
            get => this.GetName();
            set => throw new NotSupportedException("Setting the value is not supported.");
        }

        #endregion

        #region INamedItem Members

        /// <inheritdoc/>
        string INamedItem.Name => this.GetName() ?? string.Empty;

        #endregion

        #region Private methods

        /// <summary>
        /// Задаёт значение указанного поля секции <see cref="RefGroupsHelper.RefGroupTypesTable"/>.
        /// </summary>
        /// <param name="field">Имя поля.</param>
        /// <param name="value">Значение.</param>
        /// <returns>Объект <see cref="KrSecondaryProcessBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        private RefGroupTypeBuilder SetField(
            string field,
            object? value) => this.SetValue(RefGroupsHelper.RefGroupTypesTable, field, value);

        /// <summary>
        /// Возвращает значение указанного поля секции <see cref="RefGroupsHelper.RefGroupTypesTable"/>.
        /// </summary>
        /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
        /// <param name="field">Имя поля.</param>
        /// <returns>Возвращаемое значение.</returns>
        private T? TryGetField<T>(
            string field) => this.TryGetValue<T>(RefGroupsHelper.RefGroupTypesTable, field);

        #endregion
    }
}
