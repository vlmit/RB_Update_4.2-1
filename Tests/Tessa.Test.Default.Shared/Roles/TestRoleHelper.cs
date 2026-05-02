#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tessa.Cards;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Roles;
using Tessa.Scheme;
using Tessa.Test.Default.Shared.Kr;

namespace Tessa.Test.Default.Shared.Roles
{
    /// <summary>
    /// Предоставляет статические вспомогательные методы для работы с ролями в тестах.
    /// </summary>
    public static class TestRoleHelper
    {
        #region CreateRole Methods

        /// <summary>
        /// Добавляет сотрудника с указанным идентификатором и именем.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="roleID">Идентификатор сотрудника.</param>
        /// <param name="roleName">Имя сотрудника.</param>
        /// <param name="modifyAction">Функция используемая для изменения создаваемого сотрудника.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Созданный пользователь.</returns>
        /// <remarks>
        /// Заполняет e-mail. Тип входа: <see cref="UserLoginTypes.Internal"/>. Логин и пароль соответствуют имени сотрудника.<para/>
        /// Для создания сотрудника с особыми параметрами используйте <see cref="PersonalRoleBuilder"/>.
        /// </remarks>
        public static ValueTask<PersonalRoleBuilder> CreateUserAsync(
            ICardLifecycleCompanionDependencies deps,
            Guid roleID,
            string roleName,
            Action<PersonalRoleBuilder>? modifyAction = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(deps);

            var clc = new PersonalRoleBuilder(roleID, deps)
                .Create()
                .SetName(roleName)
                .SetFullName(roleName)
                .SetFirstName(roleName)
                .SetEmail($"{roleName}@undef.undef")
                .SetLoginType(UserLoginTypes.Internal)
                .SetAccount(roleName)
                .SetPassword(roleName);

            modifyAction?.Invoke(clc);

            return clc
                .Save()
                .GoAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Добавляет сотрудника со случайным идентификатором и именем.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="modifyAction">Функция используемая для изменения создаваемого сотрудника.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Созданный пользователь.</returns>
        public static ValueTask<PersonalRoleBuilder> CreateUserAsync(
            ICardLifecycleCompanionDependencies deps,
            Action<PersonalRoleBuilder>? modifyAction = null,
            CancellationToken cancellationToken = default)
        {
            var roleID = Guid.NewGuid();
            var roleName = "User__" + TestHelper.GetPseudoRandomNumber().ToString();

            return CreateUserAsync(deps, roleID, roleName, modifyAction, cancellationToken);
        }

        /// <summary>
        /// Добавляет подразделение с указанным идентификатором и именем.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="roleID">Идентификатор подразделения.</param>
        /// <param name="roleName">Имя подразделения.</param>
        /// <param name="modifyAction">Функция используемая для изменения создаваемого подразделения.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Созданное подразделение.</returns>
        public static ValueTask<DepartmentRoleBuilder> CreateDepartmentAsync(
            ICardLifecycleCompanionDependencies deps,
            Guid roleID,
            string roleName,
            Action<DepartmentRoleBuilder>? modifyAction = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(deps);

            var clc = new DepartmentRoleBuilder(roleID, deps)
                .Create()
                .SetName(roleName);

            modifyAction?.Invoke(clc);

            return clc
                .Save()
                .GoAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Добавляет подразделение со случайным идентификатором и именем.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="modifyAction">Функция используемая для изменения создаваемого подразделения.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Созданное подразделение.</returns>
        public static ValueTask<DepartmentRoleBuilder> CreateDepartmentAsync(
            ICardLifecycleCompanionDependencies deps,
            Action<DepartmentRoleBuilder>? modifyAction = null,
            CancellationToken cancellationToken = default)
        {
            var roleID = Guid.NewGuid();
            var roleName = "Department__" + TestHelper.GetPseudoRandomNumber().ToString();

            return CreateDepartmentAsync(deps, roleID, roleName, modifyAction, cancellationToken);
        }

        /// <summary>
        /// Добавляет статическую роль с указанным идентификатором и именем.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="roleID">Идентификатор статической роли.</param>
        /// <param name="roleName">Имя статической роли.</param>
        /// <param name="modifyAction">Функция используемая для изменения создаваемой статической роли.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Созданная статическая роль.</returns>
        public static ValueTask<StaticRoleBuilder> CreateStaticRoleAsync(
            ICardLifecycleCompanionDependencies deps,
            Guid roleID,
            string roleName,
            Action<StaticRoleBuilder>? modifyAction = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(deps);

            var clc = new StaticRoleBuilder(roleID, deps)
                .Create()
                .SetName(roleName);

            modifyAction?.Invoke(clc);

            return clc
                .Save()
                .GoAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Добавляет статическую роль со случайным идентификатором и именем.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="modifyAction">Функция используемая для изменения создаваемой статической роли.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Созданная статическая роль.</returns>
        public static ValueTask<StaticRoleBuilder> CreateStaticRoleAsync(
            ICardLifecycleCompanionDependencies deps,
            Action<StaticRoleBuilder>? modifyAction = null,
            CancellationToken cancellationToken = default)
        {
            var roleID = Guid.NewGuid();
            var roleName = "StaticRole__" + TestHelper.GetPseudoRandomNumber().ToString();

            return CreateStaticRoleAsync(deps, roleID, roleName, modifyAction, cancellationToken);
        }

        /// <summary>
        /// Добавляет контекстную роль с указанным идентификатором и именем.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="roleID">Идентификатор контекстной роли.</param>
        /// <param name="roleName">Имя контекстной роли.</param>
        /// <param name="modifyAction">Функция используемая для изменения создаваемой контекстной роли.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Созданная контекстная роль.</returns>
        public static ValueTask<ContextRoleBuilder> CreateContextRoleAsync(
            ICardLifecycleCompanionDependencies deps,
            Guid roleID,
            string roleName,
            Action<ContextRoleBuilder>? modifyAction = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(deps);

            var clc = new ContextRoleBuilder(roleID, deps)
                .Create()
                .SetName(roleName);

            modifyAction?.Invoke(clc);

            return clc
                .Save()
                .GoAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Добавляет контекстную роль со случайным идентификатором и именем.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="modifyAction">Функция используемая для изменения создаваемой контекстной роли.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Созданная контекстная роль.</returns>
        public static ValueTask<ContextRoleBuilder> CreateContextRoleAsync(
            ICardLifecycleCompanionDependencies deps,
            Action<ContextRoleBuilder>? modifyAction = null,
            CancellationToken cancellationToken = default)
        {
            var roleID = Guid.NewGuid();
            var roleName = "ContextRole__" + TestHelper.GetPseudoRandomNumber().ToString();

            return CreateContextRoleAsync(deps, roleID, roleName, modifyAction, cancellationToken);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Добавляет в указанную роль пользователя, если она имеет пустой состав.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="role">Роль.</param>
        /// <param name="modifyAction">Функция используемая для изменения добавляемого пользователя.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public static Task TryAddUserIfEmptyAsync(
            ICardLifecycleCompanionDependencies deps,
            Role role,
            Action<PersonalRoleBuilder>? modifyAction = null,
            CancellationToken cancellationToken = default)
            => TryAddUserIfEmptyAsync(
                deps,
                role,
                async ct => await CreateUserAsync(deps, modifyAction, ct),
                cancellationToken);

        /// <summary>
        /// Добавляет в указанную роль пользователя, если она имеет пустой состав.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="role">Роль.</param>
        /// <param name="getUserAsync">Функция, возвращающая добавляемого пользователя.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public static async Task TryAddUserIfEmptyAsync(
            ICardLifecycleCompanionDependencies deps,
            Role role,
            Func<CancellationToken, ValueTask<PersonalRoleBuilder>> getUserAsync,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(deps);
            ThrowIfNull(role);
            ThrowIfNull(getUserAsync);

            var dbScope = deps.DbScope;
            await using (dbScope.Create())
            {
                var db = dbScope.Db;

                db
                    .SetCommand(
                        dbScope.BuilderFactory
                            .Select()
                            .C(null, Names.Table_ID, Names.Table_RowID, "UserID", "UserName", "IsDeputy", "TypeID")
                            .From(RoleStrings.RoleUsers).NoLock()
                            .Where().C(Names.Table_ID).Equals().P(Names.Table_ID)
                            .Build(),
                        db.Parameter(Names.Table_ID, role.ID))
                    .LogCommand();

                var result = new List<RoleUserRecord>();
                await using var reader = await db.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    result.Add(new()
                    {
                        ID = reader.GetGuid(0),
                        RowID = reader.GetGuid(1),
                        UserID = reader.GetGuid(2),
                        UserName = reader.GetString(3),
                        IsDeputy = reader.GetBoolean(4),
                        RoleType = (RoleType) reader.GetInt16(5)
                    });
                }

                role.Users = result;
            }

            if (role.Users.Count > 0)
            {
                return;
            }

            var roleUser = await getUserAsync(cancellationToken);
            var roleUserRecord = await AddUserAsync(
                deps,
                role,
                roleUser,
                cancellationToken);

            role.Users = [roleUserRecord];
        }

        /// <summary>
        /// Добавляет в указанную роль заданного пользователя.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="role">Роль.</param>
        /// <param name="roleUser">Добавляемый пользователь.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Добавленная запись о составе роли.</returns>
        public static async Task<RoleUserRecord> AddUserAsync(
            ICardLifecycleCompanionDependencies deps,
            Role role,
            PersonalRoleBuilder roleUser,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(deps);
            ThrowIfNull(role);
            ThrowIfNull(roleUser);

            var dbScope = deps.DbScope;
            await using var _ = dbScope.Create();

            var roleUserRecord = new RoleUserRecord
            {
                RowID = Guid.NewGuid(),
                ID = role.ID,
                IsDeputy = false,
                User = new RoleUser(roleUser.CardID, roleUser.GetName() ?? string.Empty),
                Role = role
            };
            roleUserRecord.UpdateFromAssociations();

            var db = dbScope.Db;
            await db
                .SetCommand(
                    dbScope.BuilderFactory
                        .InsertInto(RoleStrings.RoleUsers, Names.Table_RowID, Names.Table_ID, "TypeID", "IsDeputy", "UserID", "UserName")
                        .Values(b => b.P(Names.Table_RowID, Names.Table_ID, "TypeID", "IsDeputy", "UserID", "UserName"))
                        .Build(),
                    db.Parameter(Names.Table_RowID, roleUserRecord.RowID),
                    db.Parameter(Names.Table_ID, roleUserRecord.ID),
                    db.Parameter("TypeID", (int) roleUserRecord.RoleType),
                    db.Parameter("IsDeputy", roleUserRecord.IsDeputy),
                    db.Parameter("UserID", roleUserRecord.UserID),
                    db.Parameter("UserName", SqlHelper.LimitString(roleUserRecord.UserName, RoleHelper.RoleNameMaxLength)))
                .ExecuteNonQueryAsync(cancellationToken);

            return roleUserRecord;
        }

        /// <summary>
        /// Возвращает роль подразделения.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="departmentID">Идентификатор подразделения.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Роль подразделения.</returns>
        public static async Task<DepartmentRole> GetDepartmentRoleAsync(
            ICardLifecycleCompanionDependencies deps,
            Guid departmentID,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(deps);

            var cardRepository = deps.CardRepository;

            var getRequest = new CardGetRequest
            {
                CardID = departmentID,
                CardTypeID = RoleHelper.DepartmentRoleTypeID,
                CardTypeName = RoleHelper.DepartmentRoleTypeName
            };

            var getResponse = await cardRepository.GetAsync(getRequest, cancellationToken);
            ValidationAssert.IsSuccessful(getResponse.ValidationResult);

            var department = getResponse.TryGetCard();
            Assert.That(department, Is.Not.Null);

            var depsFields = department!.Sections["DepartmentRoles"].Fields;
            var rolesFields = department.Sections["Roles"].Fields;
            return new DepartmentRole
            {
                ID = departmentID,
                HeadUserID = depsFields.TryGet<Guid?>("HeadUserID"),
                HeadUserName = depsFields.TryGet<string>("HeadUserName"),
                Name = rolesFields.TryGet<string>("Name") ?? string.Empty,
                ParentID = rolesFields.TryGet<Guid?>("ParentID"),
                ParentName = rolesFields.TryGet<string>("ParentName"),
                RoleType = (RoleType) rolesFields.TryGet<int>("TypeID"),
                Hidden = rolesFields.TryGet<bool>("Hidden"),
                DisableDeputies = rolesFields.TryGet<bool>("DisableDeputies"),
                Modified = department.Modified ?? DateTime.MinValue,
                ModifiedByID = department.ModifiedByID
            };
        }

        /// <summary>
        /// Обновляет роль подразделения.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="departmentRole">Роль подразделения.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public static async Task UpdateDepartmentRoleAsync(
            ICardLifecycleCompanionDependencies deps,
            DepartmentRole departmentRole,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(deps);
            ThrowIfNull(departmentRole);

            var cardRepository = deps.CardRepository;

            var getRequest = new CardGetRequest
            {
                CardID = departmentRole.ID,
                CardTypeID = RoleHelper.DepartmentRoleTypeID,
                CardTypeName = RoleHelper.DepartmentRoleTypeName
            };

            var getResponse = await cardRepository.GetAsync(getRequest, cancellationToken);
            ValidationAssert.IsSuccessful(getResponse.ValidationResult);

            var department = getResponse.TryGetCard();
            Assert.That(department, Is.Not.Null);

            var depsFields = department!.Sections["DepartmentRoles"].Fields;
            depsFields["HeadUserID"] = departmentRole.HeadUserID;
            depsFields["HeadUserName"] = departmentRole.HeadUserName;

            var rolesFields = department.Sections["Roles"].Fields;
            rolesFields["Name"] = departmentRole.Name;
            rolesFields["ParentID"] = departmentRole.ParentID;
            rolesFields["ParentName"] = departmentRole.ParentName;
            rolesFields["TypeID"] = Int32Boxes.Box((int) departmentRole.RoleType);
            rolesFields["Hidden"] = BooleanBoxes.Box(departmentRole.Hidden);
            rolesFields["DisableDeputies"] = BooleanBoxes.Box(departmentRole.DisableDeputies);

            department.RemoveAllButChanged();

            var storeResponse = await cardRepository.StoreAsync(new CardStoreRequest { Card = department }, cancellationToken);
            ValidationAssert.IsSuccessful(storeResponse.ValidationResult);
        }

        /// <summary>
        /// Возвращает статическую роль.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="roleID">Идентификатор роли.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Статическая роль.</returns>
        public static async Task<StaticRole> GetStaticRoleAsync(
            ICardLifecycleCompanionDependencies deps,
            Guid roleID,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(deps);

            var cardRepository = deps.CardRepository;

            var getRequest = new CardGetRequest
            {
                CardID = roleID,
                CardTypeID = RoleHelper.StaticRoleTypeID,
                CardTypeName = RoleHelper.StaticRoleTypeName
            };

            var getResponse = await cardRepository.GetAsync(getRequest, cancellationToken);
            ValidationAssert.IsSuccessful(getResponse.ValidationResult);

            var department = getResponse.TryGetCard();
            Assert.That(department, Is.Not.Null);

            var rolesFields = department!.Sections["Roles"].Fields;
            return new StaticRole
            {
                ID = roleID,
                Name = rolesFields.TryGet<string>("Name") ?? string.Empty,
                ParentID = rolesFields.TryGet<Guid?>("ParentID"),
                ParentName = rolesFields.TryGet<string>("ParentName"),
                RoleType = (RoleType) rolesFields.TryGet<int>("TypeID"),
                Hidden = rolesFields.TryGet<bool>("Hidden"),
                DisableDeputies = rolesFields.TryGet<bool>("DisableDeputies"),
                Modified = department.Modified ?? DateTime.MinValue,
                ModifiedByID = department.ModifiedByID
            };
        }

        #endregion
    }
}
