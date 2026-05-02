#nullable enable
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Tessa.Test.Default.Shared.Kr;

namespace Tessa.Test.Default.Shared.Roles
{
    /// <summary>
    /// Объект-помощник для персональной роли (сотрудника).
    /// </summary>
    public sealed class PersonalRoleBuilder : RoleBuilderBase<PersonalRoleBuilder>
    {
        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием значений его зависимостей.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        public PersonalRoleBuilder(ICardLifecycleCompanionDependencies deps)
            : this(Guid.NewGuid(), deps)
        {
        }

        /// <summary>
        /// Создаёт экземпляр класса с указанием значений его идентификатора и зависимостей.
        /// </summary>
        /// <param name="cardID">Идентификатор роли.</param>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        public PersonalRoleBuilder(Guid cardID, ICardLifecycleCompanionDependencies deps)
            : base(cardID, RoleHelper.PersonalRoleTypeID, RoleHelper.PersonalRoleTypeName, deps)
        {
        }

        #endregion

        #region Private Methods

        private async ValueTask<string?> GetEnumItemNameAsync<TID>(
            string enumName,
            TID id,
            string columnID = "ID",
            string columnName = "Name",
            CancellationToken cancellationToken = default)
            where TID : IEquatable<TID> =>
            (await this.Dependencies.CardMetadata.GetEnumerationsAsync(cancellationToken)).TryGetValue(enumName, out var @enum)
                ? (string?) @enum.Records.FirstOrDefault(i => ((TID) NotNullOrThrow(i[columnID])).Equals(id))?[columnName]
                : null;

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override PersonalRoleBuilder SetName(string value) =>
            this.SetValue(RoleStrings.PersonalRoles, "Name", value);

        /// <inheritdoc/>
        public override string? GetName() =>
            this.TryGetValue<string>(RoleStrings.PersonalRoles, "Name");

        #endregion

        #region Methods

        /// <summary>
        /// Устанавливает полное имя.
        /// </summary>
        /// <param name="value">Полное имя.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public PersonalRoleBuilder SetFullName(string? value) =>
            this.SetValue(RoleStrings.PersonalRoles, "FullName", value);

        /// <summary>
        /// Возвращает полное имя.
        /// </summary>
        /// <returns>Полное имя.</returns>
        public string? GetFullName() =>
            this.TryGetValue<string>(RoleStrings.PersonalRoles, "FullName");

        /// <summary>
        /// Устанавливает имя.
        /// </summary>
        /// <param name="value">Имя.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public PersonalRoleBuilder SetFirstName(string? value) =>
            this.SetValue(RoleStrings.PersonalRoles, "FirstName", value);

        /// <summary>
        /// Возвращает имя.
        /// </summary>
        /// <returns>Имя.</returns>
        public string? GetFirstName() =>
            this.TryGetValue<string>(RoleStrings.PersonalRoles, "FirstName");

        /// <summary>
        /// Устанавливает фамилию.
        /// </summary>
        /// <param name="value">Фамилия.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public PersonalRoleBuilder SetLastName(string? value) =>
            this.SetValue(RoleStrings.PersonalRoles, "LastName", value);

        /// <summary>
        /// Возвращает фамилию.
        /// </summary>
        /// <returns>Фамилия.</returns>
        public string? GetLastName() =>
            this.TryGetValue<string>(RoleStrings.PersonalRoles, "LastName");

        /// <summary>
        /// Устанавливает отчество.
        /// </summary>
        /// <param name="value">Отчество.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public PersonalRoleBuilder SetMiddleName(string? value) =>
            this.SetValue(RoleStrings.PersonalRoles, "MiddleName", value);

        /// <summary>
        /// Возвращает отчество.
        /// </summary>
        /// <returns>Отчество.</returns>
        public string? GetMiddleName() =>
            this.TryGetValue<string>(RoleStrings.PersonalRoles, "MiddleName");

        /// <summary>
        /// Устанавливает тип входа.
        /// </summary>
        /// <param name="value">Тип входа.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public PersonalRoleBuilder SetLoginType(UserLoginType value)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this, value.ToString()),
                    async (action, ct) =>
                    {
                        var fields = this.GetCardOrThrow().Sections[RoleStrings.PersonalRoles].Fields;
                        var valueInt = (int) value;

                        fields["LoginTypeID"] = Int32Boxes.Box(valueInt);
                        fields["LoginTypeName"] = await this.GetEnumItemNameAsync(
                            "LoginTypes",
                            valueInt,
                            cancellationToken: ct) ?? value.ToString();

                        return ValidationResult.Empty;
                    }));

            return this;
        }

        /// <summary>
        /// Возвращает тип входа.
        /// </summary>
        /// <returns>Тип входа.</returns>
        public UserLoginType GetLoginType() =>
            UserLoginTypes.TryGet(this.TryGetValue<int>(RoleStrings.PersonalRoles, "LoginTypeID")) ?? UserLoginTypes.Forbidden;

        /// <summary>
        /// Устанавливает уровень доступа.
        /// </summary>
        /// <param name="value">Уровень доступа.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public PersonalRoleBuilder SetAccessLevel(UserAccessLevel value)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this, value.ToString()),
                    async (_, ct) =>
                    {
                        var fields = this.GetCardOrThrow().Sections[RoleStrings.PersonalRoles].Fields;
                        var valueInt = (int) value;

                        fields["AccessLevelID"] = Int32Boxes.Box(valueInt);
                        fields["AccessLevelName"] = await this.GetEnumItemNameAsync(
                            "AccessLevels",
                            valueInt,
                            cancellationToken: ct) ?? value.ToString();

                        return ValidationResult.Empty;
                    }));

            return this;
        }

        /// <summary>
        /// Возвращает уровень доступа.
        /// </summary>
        /// <returns>Уровень доступа.</returns>
        public UserAccessLevel GetAccessLevel() =>
            (UserAccessLevel) this.TryGetValue<int>(RoleStrings.PersonalRoles, "AccessLevelID");

        /// <summary>
        /// Устанавливает аккаунт.
        /// </summary>
        /// <param name="value">Аккаунт.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public PersonalRoleBuilder SetAccount(string? value) =>
            this.SetValue(RoleStrings.PersonalRoles, "Login", value);

        /// <summary>
        /// Возвращает аккаунт.
        /// </summary>
        /// <returns>Аккаунт.</returns>
        public string? GetAccount() =>
            this.TryGetValue<string>(RoleStrings.PersonalRoles, "Login");

        /// <summary>
        /// Устанавливает пароль.
        /// </summary>
        /// <param name="value">Пароль.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public PersonalRoleBuilder SetPassword(string? value) =>
            this
                .SetValue("PersonalRolesVirtual", "Password", value is null ? null : Convert.ToBase64String(Encoding.UTF8.GetBytes(value)));

        /// <summary>
        /// Возвращает пароль.
        /// </summary>
        /// <returns>Пароль.</returns>
        /// <remarks>Значение доступно только до сохранения карточки.</remarks>
        public string? GetPassword() =>
            this.TryGetValue<string>("PersonalRolesVirtual", "Password");

        /// <summary>
        /// Устанавливает e-mail.
        /// </summary>
        /// <param name="value">E-mail.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public PersonalRoleBuilder SetEmail(string? value) =>
            this.SetValue(RoleStrings.PersonalRoles, "Email", value);

        /// <summary>
        /// Возвращает e-mail.
        /// </summary>
        /// <returns>E-mail.</returns>
        public string? GetEmail() =>
            this.TryGetValue<string>(RoleStrings.PersonalRoles, "Email");

        /// <summary>
        /// Устанавливает язык.
        /// </summary>
        /// <param name="language"><inheritdoc cref="LocalizationLanguage" path="/summary"/></param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public PersonalRoleBuilder SetLanguage(LocalizationLanguage? language) =>
            this
                .SetValue("PersonalRolesVirtual", "LanguageID", language?.ID)
                .SetValue("PersonalRolesVirtual", "LanguageCaption", language?.Caption)
                .SetValue("PersonalRolesVirtual", "LanguageCode", language?.Code);

        /// <summary>
        /// Возвращает язык.
        /// </summary>
        /// <returns>Информация о языке.</returns>
        public LocalizationLanguage GetLanguage() =>
            new(this.TryGetValue<int?>("PersonalRolesVirtual", "LanguageID"),
                this.TryGetValue<string>("PersonalRolesVirtual", "LanguageCaption"),
                this.TryGetValue<string>("PersonalRolesVirtual", "LanguageCode"));

        /// <summary>
        /// Устанавливает настройки форматирования.
        /// </summary>
        /// <param name="id">Идентификатор карточки с настройками форматирования.</param>
        /// <param name="name">Имя культуры для настроек форматирования.</param>
        /// <param name="caption">Отображаемое имя настроек форматирования.</param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения.
        /// Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult},CancellationToken)"/>.
        /// </remarks>
        public PersonalRoleBuilder SetFormatSettings(Guid? id, string? name, string? caption) =>
            this
                .SetValue("PersonalRolesVirtual", "FormatID", id)
                .SetValue("PersonalRolesVirtual", "FormatName", name)
                .SetValue("PersonalRolesVirtual", "FormatCaption", caption);

        /// <summary>
        /// Возвращает настройки форматирования.
        /// </summary>
        /// <returns>Информация о настройках форматирования.</returns>
        public (Guid? ID, string? Name, string? Caption) GetFormatSettings() =>
        (
            this.TryGetValue<Guid?>("PersonalRolesVirtual", "FormatID"),
            this.TryGetValue<string>("PersonalRolesVirtual", "FormatName"),
            this.TryGetValue<string>("PersonalRolesVirtual", "FormatCaption")
        );

        #endregion
    }
}
