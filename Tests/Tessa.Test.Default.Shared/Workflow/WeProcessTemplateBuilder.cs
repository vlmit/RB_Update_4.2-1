#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Platform.Server.Cards;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Test.Default.Shared.Kr;
using Tessa.Workflow.Signals;
using Tessa.Workflow.Storage;

namespace Tessa.Test.Default.Shared.Workflow
{
    /// <summary>
    /// Предоставляет методы для создания и модификации карточки шаблона бизнес-процесса WorkflowEngine.
    /// </summary>
    public sealed class WeProcessTemplateBuilder :
        CardLifecycleCompanion<WeProcessTemplateBuilder>,
        INamedEntry
    {
        #region Constants And Static Fields

        private const string BusinessProcessInfo = nameof(BusinessProcessInfo);
        private const string BusinessProcessInfo_Name = "Name";
        private const string BusinessProcessInfo_Group = "Group";
        private const string BusinessProcessInfo_StartFromCard = "StartFromCard";
        private const string BusinessProcessInfo_Multiple = "Multiple";
        private const string BusinessProcessInfo_LockMessage = "LockMessage";
        private const string BusinessProcessInfo_ErrorMessage = "ErrorMessage";

        private const string BusinessProcessVersions = nameof(BusinessProcessVersions);
        private const string BusinessProcessVersionsVirtual = nameof(BusinessProcessVersionsVirtual);
        private const string BusinessProcessVersions_ProcessData = "ProcessData";
        private const string BusinessProcessVersions_ParentVersion = "ParentVersion";
        private const string BusinessProcessVersions_ActiveCount = "ActiveCount";
        private const string BusinessProcessVersions_IsDefault = "IsDefault";
        private const string BusinessProcessVersions_Version = "Version";
        private const string BusinessProcessVersions_LockedForEditing = "LockedForEditing";

        private const string BusinessProcessReadRoles = nameof(BusinessProcessReadRoles);
        private const string BusinessProcessReadRoles_RoleID = "RoleID";
        private const string BusinessProcessReadRoles_RoleName = "RoleName";

        private const string BusinessProcessEditRoles = nameof(BusinessProcessEditRoles);
        private const string BusinessProcessEditRoles_RoleID = "RoleID";
        private const string BusinessProcessEditRoles_RoleName = "RoleName";

        private const string BusinessProcessExtensions = nameof(BusinessProcessExtensions);
        private const string BusinessProcessExtensions_ExtensionID = "ExtensionID";
        private const string BusinessProcessExtensions_ExtensionName = "ExtensionName";

        private const string BusinessProcessCardTypes = nameof(BusinessProcessCardTypes);
        private const string BusinessProcessCardTypes_CardTypeID = "CardTypeID";
        private const string BusinessProcessCardTypes_CardTypeCaption = "CardTypeCaption";

        private const string BusinessProcessButtons = nameof(BusinessProcessButtons);
        private const string BusinessProcessButtonsVirtual = nameof(BusinessProcessButtonsVirtual);
        private const string BusinessProcessButtons_AllowedVersions = "AllowedVersions";
        private const string BusinessProcessButtons_Caption = "Caption";
        private const string BusinessProcessButtons_Alias = "Alias";
        private const string BusinessProcessButtons_StartProcess = "StartProcess";
        private const string BusinessProcessButtons_SignalName = "SignalName";

        private const string BusinessProcessButtonRoles = nameof(BusinessProcessButtonRoles);
        private const string BusinessProcessButtonRoles_RoleID = "RoleID";
        private const string BusinessProcessButtonRoles_RoleName = "RoleName";
        private const string BusinessProcessButtonRoles_ButtonRowID = "ButtonRowID";

        #endregion

        #region Fields

        private WorkflowProcessStorage? version;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="weDeps"><inheritdoc cref="IWeLifecycleCompanionDependencies" path="/summary"/></param>
        public WeProcessTemplateBuilder(
            ICardLifecycleCompanionDependencies deps,
            IWeLifecycleCompanionDependencies weDeps)
            : this(
                Guid.NewGuid(),
                deps,
                weDeps)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="weDeps"><inheritdoc cref="IWeLifecycleCompanionDependencies" path="/summary"/></param>
        public WeProcessTemplateBuilder(
            Guid cardID,
            ICardLifecycleCompanionDependencies deps,
            IWeLifecycleCompanionDependencies weDeps)
            : base(
                cardID,
                CardHelper.BusinessProcessTemplateTypeID,
                CardHelper.BusinessProcessTemplateTypeName,
                deps)
        {
            this.WeDependencies = NotNullOrThrow(weDeps);
        }

        #endregion

        #region Properties

        /// <inheritdoc cref="IWeLifecycleCompanionDependencies" path="/summary"/>
        public IWeLifecycleCompanionDependencies WeDependencies { get; }

        /// <summary>
        /// Загруженная версия шаблона бизнес-процесса.
        /// </summary>
        [AllowNull]
        public WorkflowProcessStorage Version
        {
            get
            {
                if (this.version is null)
                {
                    throw new InvalidOperationException("Version isn't specified.");
                }

                this.version.Initialize();
                return this.version;
            }
            set
            {
                if (value is not null
                    && value.TemplateCardID != this.CardID)
                {
                    throw new ArgumentException(
                        $"The template version (ID={value.ID:B}. TemplateID={value.TemplateCardID:B}) being set refers to a different template (ID={this.CardID:B}).",
                        nameof(value));
                }

                this.version = value;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Возвращает название процесса.
        /// </summary>
        /// <returns>Название процесса.</returns>
        public string? GetName() =>
            this.TryGetBpiField<string>(BusinessProcessInfo_Name);

        /// <summary>
        /// Устанавливает название процесса.
        /// </summary>
        /// <param name="value">Название процесса.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder SetName(string value) =>
            this.SetBpiField(BusinessProcessInfo_Name, value);

        /// <summary>
        /// Возвращает название группы.
        /// </summary>
        /// <returns>Название группы.</returns>
        public string? GetGroup() =>
            this.TryGetBpiField<string>(BusinessProcessInfo_Group);

        /// <summary>
        /// Устанавливает название группы.
        /// </summary>
        /// <param name="value">Название группы.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder SetGroup(string value) =>
            this.SetBpiField(BusinessProcessInfo_Group, value);

        /// <summary>
        /// Возвращает значение флага "Запуск из карточки".
        /// </summary>
        /// <returns>Значение <see langword="true"/>, если процесс можно запускать из карточки, иначе - <see langword="false"/>.</returns>
        public bool GetStartFromCard() =>
            this.TryGetBpiField<bool>(BusinessProcessInfo_StartFromCard);

        /// <summary>
        /// Устанавливает флаг "Запуск из карточки".
        /// </summary>
        /// <param name="value">Значение <see langword="true"/>, если процесс можно запускать из карточки, иначе - <see langword="false"/>.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder SetStartFromCard(bool value) =>
            this.SetBpiField(BusinessProcessInfo_StartFromCard, value);

        /// <summary>
        /// Возвращает значение флага "Можно запускать несколько экземпляров".
        /// </summary>
        /// <returns>Значение <see langword="true"/>, если процесс можно запускать из карточки, иначе - <see langword="false"/>.</returns>
        public bool GetMultiple() =>
            this.TryGetBpiField<bool>(BusinessProcessInfo_Multiple);

        /// <summary>
        /// Устанавливает флаг "Можно запускать несколько экземпляров".
        /// </summary>
        /// <param name="value">Значение <see langword="true"/>, если для процесса разрешено запускать несколько экземпляров, иначе - <see langword="false"/>.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder SetMultiple(bool value) =>
            this.SetBpiField(BusinessProcessInfo_Multiple, value);

        /// <summary>
        /// Возвращает сообщение при блокировке.
        /// </summary>
        /// <returns>Сообщение при блокировке.</returns>
        public string? GetLockMessage() =>
            this.TryGetBpiField<string>(BusinessProcessInfo_LockMessage);

        /// <summary>
        /// Устанавливает сообщение при блокировке.
        /// </summary>
        /// <param name="value">Сообщение при блокировке.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder SetLockMessage(string? value) =>
            this.SetBpiField(BusinessProcessInfo_LockMessage, value);

        /// <summary>
        /// Возвращает сообщение при ошибке.
        /// </summary>
        /// <returns>Сообщение при ошибке.</returns>
        public string? GetErrorMessage() =>
            this.TryGetBpiField<string>(BusinessProcessInfo_ErrorMessage);

        /// <summary>
        /// Устанавливает сообщение при ошибке.
        /// </summary>
        /// <param name="value">Сообщение при ошибке.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder SetErrorMessage(string? value) =>
            this.SetBpiField(BusinessProcessInfo_ErrorMessage, value);

        /// <summary>
        /// Добавляет новую версию шаблона бизнес-процесса.
        /// </summary>
        /// <param name="versionID">Идентификатор версии или значение <see langword="null"/>, если должен быть создан случайный идентификатор.</param>
        /// <param name="parentVersionID">Идентификатор родительской версии или значение <see langword="null"/>, если родительской версии нет.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder AddNewVersion(
            Guid? versionID = null,
            Guid? parentVersionID = null)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (_, _) =>
                    {
                        var newRow = this.CreateVersionRow(
                            versionID ?? Guid.NewGuid(),
                            parentVersionID);

                        newRow.ParentRowID = null;
                        newRow.State = CardRowState.Inserted;

                        return ValueTask.FromResult(ValidationResult.Empty);
                    }));

            return this;
        }

        /// <summary>
        /// Устанавливает версию шаблона бизнес-процесса по умолчанию.
        /// </summary>
        /// <param name="versionNumber">Номер версии.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder SetDefaultVersion(int versionNumber)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (_, _) =>
                    {
                        var virtualRows = this.GetVersionRows();
                        var rows = this.GetVersionRows(false);
                        var versionNumbers =
                            virtualRows.Select(x => x.Fields.TryGet<int?>(BusinessProcessVersions_Version))
                                .Where(y => y is not null)
                                .ToHashSet();

                        if (!versionNumbers.Contains(versionNumber))
                        {
                            throw new InvalidOperationException($"Can't set default version. Version number {versionNumber} doesn't exists.");
                        }

                        foreach (var virtualRow in virtualRows)
                        {
                            var virtualIsDefault = virtualRow.Fields.TryGet<bool>(BusinessProcessVersions_IsDefault);
                            var version = virtualRow.Fields.TryGet<int>(BusinessProcessVersions_Version);
                            var row = rows.FirstOrDefault(x => x.RowID == virtualRow.RowID);
                            
                            if (version == versionNumber)
                            {
                                if (!virtualIsDefault)
                                {
                                    virtualRow.Fields[BusinessProcessVersions_IsDefault] = BooleanBoxes.True;
                                }

                                if (row is null)
                                {
                                    row = this.CreateVersionRow(virtualRow.RowID, null);
                                    row.Fields[BusinessProcessVersions_IsDefault] = BooleanBoxes.True;
                                }
                                else
                                {
                                    var rowIsDefault = row.Fields.TryGet<bool?>(BusinessProcessVersions_IsDefault);
                                    if (rowIsDefault.HasValue && !rowIsDefault.Value)
                                    {
                                        row.Fields[BusinessProcessVersions_IsDefault] = BooleanBoxes.True;
                                    }
                                }
                            }
                            else
                            {
                                if (virtualIsDefault)
                                {
                                    virtualRow.Fields[BusinessProcessVersions_IsDefault] = BooleanBoxes.False;
                                }

                                if (row is null)
                                {
                                    row = this.CreateVersionRow(virtualRow.RowID, null);
                                    row.Fields[BusinessProcessVersions_IsDefault] = BooleanBoxes.False;
                                }
                                else
                                {
                                    var rowIsDefault = row.Fields.TryGet<bool?>(BusinessProcessVersions_IsDefault);
                                    if (rowIsDefault.HasValue && rowIsDefault.Value)
                                    {
                                        row.Fields[BusinessProcessVersions_IsDefault] = BooleanBoxes.False;
                                    }
                                }
                            }
                        }

                        return ValueTask.FromResult(ValidationResult.Empty);
                    }));

            return this;
        }

        /// <summary>
        /// Задаёт указанный тип карточки в качестве ограничения.
        /// </summary>
        /// <param name="id">Идентификатор типа карточки.</param>
        /// <param name="caption">Название типа карточки.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder ForType(
            Guid id,
            string caption)
        {
            return this.AddRow(
                BusinessProcessCardTypes,
                BusinessProcessCardTypes_CardTypeID,
                BusinessProcessCardTypes_CardTypeCaption,
                id,
                caption);
        }

        /// <summary>
        /// Удаляет все типы из ограничения.
        /// </summary>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder RemoveAllTypes() =>
            this.RemoveAllRows(BusinessProcessCardTypes);

        /// <summary>
        /// Удаляет тип карточки из ограничения.
        /// </summary>
        /// <param name="id">Идентификатор удаляемого типа.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder RemoveType(Guid id) =>
            this.RemoveRow(
                BusinessProcessCardTypes,
                row => row.Get<Guid>(BusinessProcessCardTypes_CardTypeID) == id);

        /// <summary>
        /// Добавляет указанный тип расширения для проверки доступа к тайлам.
        /// </summary>
        /// <param name="id">Идентификатор типа расширения.</param>
        /// <param name="name">Название типа расширения.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder AddExtension(
            Guid id,
            string name)
        {
            return this.AddRow(
                BusinessProcessExtensions,
                BusinessProcessExtensions_ExtensionID,
                BusinessProcessExtensions_ExtensionName,
                id,
                name);
        }

        /// <summary>
        /// Удаляет все типы расширений для проверки доступа к тайлам.
        /// </summary>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder RemoveAllExtensions() =>
            this.RemoveAllRows(BusinessProcessExtensions);

        /// <summary>
        /// Удаляет тип расширения для проверки доступа к тайлам.
        /// </summary>
        /// <param name="id">Идентификатор удаляемого типа расширения.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder RemoveExtension(Guid id) =>
            this.RemoveRow(
                BusinessProcessExtensions,
                row => row.Get<Guid>(BusinessProcessExtensions_ExtensionID) == id);

        /// <summary>
        /// Добавляет роль в список "Доступ на редактирование".
        /// </summary>
        /// <param name="id">Идентификатор роли.</param>
        /// <param name="name">Название роли.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder AddEditRole(
            Guid id,
            string name)
        {
            return this.AddRow(
                BusinessProcessEditRoles,
                BusinessProcessEditRoles_RoleID,
                BusinessProcessEditRoles_RoleName,
                id,
                name);
        }

        /// <summary>
        /// Удаляет все роли из списка "Доступ на редактирование".
        /// </summary>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder RemoveAllEditRoles() =>
            this.RemoveAllRows(BusinessProcessEditRoles);

        /// <summary>
        /// Удаляет роль из списка "Доступ на редактирование".
        /// </summary>
        /// <param name="id">Идентификатор удаляемой роли.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder RemoveEditRole(Guid id) =>
            this.RemoveRow(
                BusinessProcessEditRoles,
                row => row.Get<Guid>(BusinessProcessEditRoles_RoleID) == id);

        /// <summary>
        /// Добавляет роль в список "Доступ на чтение экземпляров".
        /// </summary>
        /// <param name="id">Идентификатор роли.</param>
        /// <param name="name">Название роли.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder AddReadRole(
            Guid id,
            string name)
        {
            return this.AddRow(
                BusinessProcessReadRoles,
                BusinessProcessReadRoles_RoleID,
                BusinessProcessReadRoles_RoleName,
                id,
                name);
        }

        /// <summary>
        /// Удаляет все роли из списка "Доступ на чтение экземпляров".
        /// </summary>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder RemoveAllReadRoles() =>
            this.RemoveAllRows(BusinessProcessReadRoles);

        /// <summary>
        /// Удаляет роль из списка "Доступ на чтение экземпляров".
        /// </summary>
        /// <param name="id">Идентификатор удаляемой роли.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder RemoveReadRole(Guid id) =>
            this.RemoveRow(
                BusinessProcessReadRoles,
                row => row.Get<Guid>(BusinessProcessReadRoles_RoleID) == id);

        /// <summary>
        /// Загружает заданную версию шаблона бизнес-процесса.
        /// </summary>
        /// <param name="versionID">Идентификатор загружаемой версии.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder LoadVersion(
            Guid versionID)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    async (_, ct) =>
                        await this.LoadAsync(versionID, ct)));

            return this;
        }

        /// <summary>
        /// Загружает заданную версию шаблона бизнес-процесса.
        /// </summary>
        /// <param name="version"><inheritdoc cref="TryGetVersionRow(int, bool)" path="/param[@name='version']"/></param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder LoadVersion(
            int version = int.MaxValue)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    async (_, ct) =>
                    {
                        var versionID = this.GetVersionRow(version, true).RowID;
                        return await this.LoadAsync(versionID, ct);
                    }));

            return this;
        }

        /// <summary>
        /// Удаляет версию шаблона бизнес-процесса.
        /// </summary>
        /// <param name="versionID">Идентификатор удаляемой версии.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder RemoveVersion(
            Guid versionID)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (_, _) =>
                    {
                        var virtualRow = this.GetVersionRow(versionID, true);

                        return ValueTask.FromResult(this.RemoveVersion(virtualRow));
                    }));

            return this;
        }

        /// <summary>
        /// Удаляет версию шаблона бизнес-процесса.
        /// </summary>
        /// <param name="version"><inheritdoc cref="TryGetVersionRow(int, bool)" path="/param[@name='version']"/></param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder RemoveVersion(
            int version = int.MaxValue)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (_, _) =>
                    {
                        var virtualRow = this.GetVersionRow(version, true);

                        return ValueTask.FromResult(this.RemoveVersion(virtualRow));
                    }));

            return this;
        }

        /// <summary>
        /// Создаёт копию версии с заданным идентификатором и устанавливает её в качестве текущей в <see cref="Version"/>.
        /// </summary>
        /// <param name="versionID">Идентификатор копируемой версии.</param>
        /// <param name="newVersionID">Идентификатор новой версии или значение <see langword="null"/>, если он должен быть создан случайным.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder CopyVersion(
            Guid versionID,
            Guid? newVersionID = null)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this, nameof(Guid)),
                    async (_, ct) =>
                    {
                        var virtualRow = this.GetVersionRow(versionID, true);

                        return await this.CopyVersionAsync(
                            virtualRow,
                            newVersionID,
                            ct);
                    }));

            return this;
        }

        /// <summary>
        /// Создаёт копию версии с заданным идентификатором и устанавливает её в качестве текущей в <see cref="Version"/>.
        /// </summary>
        /// <param name="version"><inheritdoc cref="TryGetVersionRow(int, bool)" path="/param[@name='version']"/></param>
        /// <param name="newVersionID">Идентификатор новой версии или значение <see langword="null"/>, если он должен быть создан случайным.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder CopyVersion(
            int version = int.MaxValue,
            Guid? newVersionID = null)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this, nameof(Int32)),
                    async (_, ct) =>
                    {
                        var virtualRow = this.GetVersionRow(version, true);

                        return await this.CopyVersionAsync(
                            virtualRow,
                            newVersionID,
                            ct);
                    }));

            return this;
        }

        /// <summary>
        /// Возвращает строки с информацией о версиях бизнес-процесса.
        /// </summary>
        /// <param name="isVirtual">Значение <see langword="true"/>, если возвращается виртуальная секция, иначе - физическая.</param>
        /// <returns>Строки с информацией о версиях бизнес-процесса.</returns>
        public ListStorage<CardRow> GetVersionRows(
            bool isVirtual = true)
        {
            var sections = this.GetCardOrThrow().Sections;

            if (isVirtual)
            {
                return sections
                    .GetOrAddTable(BusinessProcessVersionsVirtual)
                    .Rows;
            }

            return sections
                .GetOrAddTable(
                    BusinessProcessVersions,
                    CardTableType.Hierarchy)
                .Rows;
        }

        /// <summary>
        /// Возвращает строку с информацией о версии, имеющей заданный номер.
        /// </summary>
        /// <param name="version"><inheritdoc cref="TryGetVersionRow(int, bool)" path="/param[@name='version']"/></param>
        /// <param name="isVirtual"><inheritdoc cref="GetVersionRows(bool)" path="/param[@name='isVirtual']"/></param>
        /// <returns>Строка с информацией о версии.</returns>
        public CardRow GetVersionRow(
            int version,
            bool isVirtual = true)
        {
            return this.TryGetVersionRow(version, isVirtual)
                ?? throw new InvalidOperationException(
                    $"The business process template (ID={this.CardID:B}) does not contain version #{(version == int.MaxValue ? "last" : version.ToString())}.");
        }

        /// <summary>
        /// Возвращает строку с информацией о версии, имеющей заданный идентификатор.
        /// </summary>
        /// <param name="versionID">Идентификатор версии шаблона бизнес-процесса.</param>
        /// <param name="isVirtual"><inheritdoc cref="GetVersionRows(bool)" path="/param[@name='isVirtual']"/></param>
        /// <returns>Строка с информацией о версии.</returns>
        public CardRow GetVersionRow(
            Guid versionID,
            bool isVirtual = true)
        {
            return this.TryGetVersionRow(versionID, isVirtual)
                ?? throw new InvalidOperationException($"The business process template (ID={this.CardID:B}) does not contain version ID={versionID:B}.");
        }

        /// <summary>
        /// Возвращает строку с информацией о версии, имеющей заданный номер.
        /// </summary>
        /// <param name="version">Номер версии шаблона бизнес-процесса или значение <see cref="int.MaxValue"/>, если необходима последняя версия.</param>
        /// <param name="isVirtual"><inheritdoc cref="GetVersionRows(bool)" path="/param[@name='isVirtual']"/></param>
        /// <returns>Строка с информацией о версии или значение <see langword="null"/>, если она не найдена.</returns>
        public CardRow? TryGetVersionRow(
            int version,
            bool isVirtual = true)
        {
            return version == int.MaxValue
                ? this.GetVersionRows(true).MaxBy(static i => i.Get<int>(BusinessProcessVersions_Version))
                : this.TryGetVersionRow(
                    i => i.Get<int?>(BusinessProcessVersions_Version) == version,
                    isVirtual);
        }

        /// <summary>
        /// Возвращает строку с информацией о версии, имеющей заданный идентификатор.
        /// </summary>
        /// <param name="versionID">Идентификатор версии шаблона бизнес-процесса.</param>
        /// <param name="isVirtual"><inheritdoc cref="GetVersionRows(bool)" path="/param[@name='isVirtual']"/></param>
        /// <returns>Строка с информацией о версии или значение <see langword="null"/>, если она не найдена.</returns>
        public CardRow? TryGetVersionRow(
            Guid versionID,
            bool isVirtual = true)
        {
            return this.TryGetVersionRow(
                i => i.RowID == versionID,
                isVirtual);
        }

        /// <summary>
        /// Добавляет кнопку бизнес-процесса.
        /// </summary>
        /// <param name="buttonRowID">Идентификатор кнопки, или <c>null</c>, в этом случае он будет сгенерирован автоматически.</param>
        /// <param name="caption">Отображаемое наименование кнопки, или <c>null</c>, в этом случае оно будет сгенерировано автоматически.</param>
        /// <param name="alias">Алиас кнопки, или <c>null</c>, в этом случае он будет сгенерирован автоматически.</param>
        /// <param name="allowedVersions">Строка с фильтром разрешённых версий БП для кнопки или <c>null</c>.</param>
        /// <param name="startProcess">Определяет, запускает ли кнопка процесс, по умолчанию <c>true</c>.</param>
        /// <param name="signalName">Наименование сигнала, по умолчанию <see cref="WorkflowSignalTypes.Default"/>.</param>
        /// <param name="buttonRoles">Список ролей для которых доступна кнопка или <c>null</c>, в этом случае будет добавлена роль "System".</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public WeProcessTemplateBuilder AddButton(
            Guid? buttonRowID = null,
            string? caption = null,
            string? alias = null,
            string? allowedVersions = null,
            bool startProcess = true,
            string signalName = WorkflowSignalTypes.Default,
            IList<(Guid ID, string Name)>? buttonRoles = null)
        {
            buttonRowID ??= Guid.NewGuid();
            caption ??= $"Caption_{buttonRowID}";
            alias ??= caption;

            // Значения самой кнопки.
            var buttonValues = new List<KeyValuePair<string, object?>>
            {
                new("RowID", buttonRowID),
                new(BusinessProcessButtons_Caption, caption),
                new(BusinessProcessButtons_Alias, alias),
                new(BusinessProcessButtons_AllowedVersions, allowedVersions),
                new(BusinessProcessButtons_StartProcess, startProcess),
                new(BusinessProcessButtons_SignalName, signalName)
            };

            // Значения ролей
            if (buttonRoles is null || buttonRoles.Count == 0)
            {
                var roleValues = new List<KeyValuePair<string, object?>>
                {
                    new(BusinessProcessButtonRoles_ButtonRowID, buttonRowID),
                    new(BusinessProcessButtonRoles_RoleID, new Guid("11111111-1111-1111-1111-111111111111")),
                    new(BusinessProcessButtonRoles_RoleName, "System")
                };

                this.AddRow(BusinessProcessButtonRoles, rowValues: roleValues);
            }
            else
            {
                foreach (var role in buttonRoles)
                {
                    var roleValues = new List<KeyValuePair<string, object?>>
                    {
                        new(BusinessProcessButtonRoles_ButtonRowID, buttonRowID),
                        new(BusinessProcessButtonRoles_RoleID, role.ID),
                        new(BusinessProcessButtonRoles_RoleName, role.Name)
                    };

                    this.AddRow(BusinessProcessButtonRoles, rowValues: roleValues);
                }
            }

            return this.AddRow(BusinessProcessButtons, rowValues: buttonValues);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override WeProcessTemplateBuilder Save(Action<CardStoreRequest>? modifyRequestAction = null)
        {
            base.Save(modifyRequestAction);
            var lastAction = this.GetLastPendingAction();
            lastAction
                .AddPreparationAction(
                    new PendingAction(
                        $"{TestHelper.GetCallerMemberFullName(this)}_Preparation",
                        (_, _) =>
                        {
                            if (this.version is null)
                            {
                                return ValueTask.FromResult(ValidationResult.Empty);
                            }

                            var versionID = this.Version.ID;
                            var row = this.TryGetVersionRow(
                                versionID,
                                false);

                            row ??= this.CreateVersionRow(versionID, null);

                            var data = this.Version.GetDataForSave();

                            row.Fields[BusinessProcessVersions_ProcessData] = data;

                            return ValueTask.FromResult(ValidationResult.Empty);
                        }));

            lastAction
                .AddAfterAction(
                    new PendingAction(
                        $"{TestHelper.GetCallerMemberFullName(this)}_After",
                        (_, _) =>
                        {
                            if (this.version is null)
                            {
                                return ValueTask.FromResult(ValidationResult.Empty);
                            }

                            this.Version.ClearChangesGlobal();

                            return ValueTask.FromResult(ValidationResult.Empty);
                        }));

            return this;
        }

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

        #region Private Methods

        /// <summary>
        /// Задаёт значение указанного поля секции <see cref="BusinessProcessInfo"/>.
        /// </summary>
        /// <param name="field">Имя поля.</param>
        /// <param name="value">Значение.</param>
        /// <returns>Объект <see cref="WeProcessTemplateBuilder"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        private WeProcessTemplateBuilder SetBpiField(
            string field,
            object? value) =>
            this.SetValue(BusinessProcessInfo, field, value);

        /// <summary>
        /// Возвращает значение указанного поля секции <see cref="BusinessProcessInfo"/>.
        /// </summary>
        /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
        /// <param name="field">Имя поля.</param>
        /// <returns>Возвращаемое значение.</returns>
        private T? TryGetBpiField<T>(
            string field) =>
            this.TryGetValue<T>(BusinessProcessInfo, field);

        private CardRow? TryGetVersionRow(
            Func<CardRow, bool> predicate,
            bool isVirtual) =>
            this.GetVersionRows(isVirtual).FirstOrDefault(predicate);

        private CardRow CreateVersionRow(
            Guid versionID,
            Guid? parentVersionID)
        {
            var rows = this.GetVersionRows(false);

            var newRow = rows.Add();
            newRow.RowID = versionID;
            newRow.ParentRowID = parentVersionID;

            return newRow;
        }

        private async Task<ValidationResult> LoadAsync(
            Guid versionID,
            CancellationToken cancellationToken = default)
        {
            var (process, result) = await this.WeDependencies
                .WorkflowService
                .GetProcessVersionTemplateAsync(
                    versionID,
                    cancellationToken: cancellationToken);

            if (process is null
                && result.Items.Count == 0)
            {
                result = ValidationResult.FromText(
                    this,
                    await LocalizeFormatAsync(
                        "$WorkflowEngine_WorkflowCache_ProcessTemplateNotFound",
                        versionID),
                    ValidationResultType.Error);
            }

            this.Version = process;
            return result;
        }

        private ValidationResult RemoveVersion(
            CardRow virtualRow)
        {
            var versionID = virtualRow.RowID;

            if (virtualRow.TryGet<int?>(BusinessProcessVersions_Version).HasValue
                && !virtualRow.Get<bool>(BusinessProcessVersions_LockedForEditing))
            {
                return ValidationResult.FromText(
                    $"The version (ID={versionID:B}) is locked for editing.",
                    ValidationResultType.Error);
            }

            if (virtualRow.Get<bool>(BusinessProcessVersions_IsDefault))
            {
                return ValidationResult.FromText(
                    $"The version (ID={versionID:B}) is the default version.",
                    ValidationResultType.Error);
            }

            if (virtualRow.TryGet<int>(BusinessProcessVersions_ActiveCount) > 0)
            {
                return ValidationResult.FromText(
                    $"The version (ID={versionID:B}) has active process instances.",
                    ValidationResultType.Error);
            }

            var processRow = this.TryGetVersionRow(versionID, false);
            if (processRow is null)
            {
                var parentVersion = virtualRow.TryGet<int?>(BusinessProcessVersions_ParentVersion);
                var parentRowID = parentVersion.HasValue
                    ? this.TryGetVersionRow(parentVersion.Value, true)?.RowID
                    : null;
                processRow = this.CreateVersionRow(versionID, parentRowID);
            }

            if (processRow.State == CardRowState.Inserted)
            {
                this.GetVersionRows(false).Remove(processRow);
            }
            else
            {
                processRow.State = CardRowState.Deleted;
                processRow.SetChanged(nameof(processRow.ParentRowID));
            }

            virtualRow.State = CardRowState.Deleted;

            return ValidationResult.Empty;
        }

        private async Task<ValidationResult> CopyVersionAsync(
            CardRow virtualRow,
            Guid? newVersionID,
            CancellationToken cancellationToken = default)
        {
            var versionID = virtualRow.RowID;

            if (virtualRow.State is CardRowState.Inserted
                or CardRowState.Deleted)
            {
                return ValidationResult.FromText(
                    $"Cannot create a copy of an unsaved or deleted version with ID={versionID:B}).",
                    ValidationResultType.Error);
            }

            var versionRow = this.TryGetVersionRow(versionID, false);

            if (versionRow is null)
            {
                var result = await this.LoadAsync(
                    versionID,
                    cancellationToken: cancellationToken);

                if (!result.IsSuccessful)
                {
                    return result;
                }

                var processStorage = this.Version.GetStorage();
                var json = StorageHelper.SerializeToTypedJson(processStorage);

                versionRow = this.CreateVersionRow(versionID, null);
                versionRow[BusinessProcessVersions_ProcessData] = json;
                versionRow.State = CardRowState.None;
            }

            var newRow = this.GetVersionRows(false).Add(versionRow);

            newRow.RowID = newVersionID ?? Guid.NewGuid();
            newRow.ParentRowID = versionID;
            BusinessProcessCardHelper.FillNewVersionRow(
                this.Dependencies.Session,
                newRow,
                null,
                false,
                virtualRow.TryGet<int?>(BusinessProcessVersions_Version));

            BusinessProcessCardHelper.FillVirtualRow(
                newRow,
                this.GetVersionRows(true).Add());

            return ValidationResult.Empty;
        }

        #endregion
    }
}
