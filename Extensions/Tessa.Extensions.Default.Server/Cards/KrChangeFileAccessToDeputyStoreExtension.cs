#nullable enable

using LinqToDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Tessa.Roles;
using Tessa.Roles.Deputies;

namespace Tessa.Extensions.Default.Server.Cards
{
    /// <summary>
    /// Расширение для изменения автора создаваемого заместителем файла при наличии активных заданий типа "Согласование".
    /// Используется в новой системе замещений.
    /// </summary>
    /// <param name="deputiesProvider"><inheritdoc cref="IDeputiesManagementSettingsProvider" path="/summary"/></param>
    /// <param name="cardGetStrategy"><inheritdoc cref="ICardGetStrategy" path="/summary"/></param>
    public class KrChangeFileAccessToDeputyStoreExtension(
        IDeputiesManagementSettingsProvider deputiesProvider,
        ICardGetStrategy cardGetStrategy) : CardStoreExtension
    {
        #region Private Fields

        /// <summary>
        /// Идентификаторы типов заданий.
        /// </summary>
        private static readonly IEnumerable<Guid> TaskTypeIDs = [DefaultTaskTypes.KrApproveTypeID];

        /// <summary>
        /// Идентификатор ФРЗ.
        /// </summary>
        private static readonly Guid FunctionRoleID = CardFunctionRoles.PerformerID;

        /// <inheritdoc cref="IDeputiesManagementSettingsProvider" path="/summary"/>
        private readonly IDeputiesManagementSettingsProvider deputiesProvider = NotNullOrThrow(deputiesProvider);

        /// <inheritdoc cref="ICardGetStrategy" path="/summary"/>
        private readonly ICardGetStrategy cardGetStrategy = NotNullOrThrow(cardGetStrategy);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeCommitTransaction(ICardStoreExtensionContext context)
        {
            if (context.DbScope is not { } dbScope)
            {
                return;
            }

            // Получаем карточку.
            var card = context.Request.TryGetCard();
            if (card is null)
            {
                return;
            }

            // Получаем новые сохраняемые файлы.
            var storedFileIDs = card.Files
                .Where(x => x.State is CardFileState.Inserted)
                .Select(x => x.RowID)
                .ToArray();

            // Выходим, если нет добавленных файлов.
            if (storedFileIDs is not { Length: > 0 })
            {
                return;
            }

            // Получаем настройки системы замещений.
            var deputiesSettings = await this.deputiesProvider.GetSettingsAsync(context.CancellationToken);

            // В новой системе замещений флаг IDeputiesManagementSettings.UseDeputyRoleSeparation равен false.
            // Иное означает, что используется старая система замещений.
            if (deputiesSettings.UseDeputyRoleSeparation)
            {
                return;
            }

            // Подгружаем задания текущей карточки в новую пустую карточку.
            var cardWithTasks = new Card();
            var taskContexts = await this.cardGetStrategy.TryLoadTaskInstancesAsync(
                card.ID,
                cardWithTasks,
                dbScope.Db,
                context.CardMetadata,
                context.ValidationResult,
                context.Session,
                taskTypeIDList: TaskTypeIDs,
                cancellationToken: context.CancellationToken);

            if (taskContexts is not { Count: > 0 } || !context.ValidationResult.IsSuccessful())
            {
                return;
            }

            foreach (var taskContext in taskContexts)
            {
                await this.cardGetStrategy.LoadSectionsAsync(taskContext, context.CancellationToken);
            }

            // Пытаемся получить идентификатор замещаемого сотрудника для текущих задач с типом TaskTypeIDs.
            var deputizedUserId = await TryGetDeputizedUserAsync(
                card,
                context.Session.User.ID,
                cardWithTasks.Tasks,
                dbScope,
                context.CancellationToken);

            // Выходим, если не удалось получить идентификатор замещаемого.
            if (!deputizedUserId.HasValue)
            {
                return;
            }

            // Обновляем информацию о создателе в файлах.
            await UpdateFileCreatorAsync(
                deputizedUserId.Value,
                storedFileIDs,
                context.DbScope!,
                context.CancellationToken);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Пытается получить идентификатор замещаемого сотрудника по идентификатору текущего пользователя
        /// для заданий карточки.
        /// </summary>
        /// <param name="card">Карточка.</param>
        /// <param name="currentUserID">Идентификатор текущего пользователя.</param>
        /// <param name="tasks">Задания карточки.</param>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Идентификатор замещаемого сотрудника.</returns>
        private async Task<Guid?> TryGetDeputizedUserAsync(
            Card card,
            Guid currentUserID,
            IEnumerable<CardTask> tasks,
            IDbScope dbScope,
            CancellationToken cancellationToken = default)
        {
            Guid? deputizedId = null;

            foreach (var task in tasks)
            {
                // Получаем список ролей текущей сессии для нашей ФРЗ.
                var sessionRoles = task.TryGetTaskSessionRoles()?.Where(x => x.FunctionRoleID == FunctionRoleID).ToList();

                // Если в списке нет роли для нашей ФРЗ, переходим к следующему заданию.
                if (sessionRoles is not { Count: > 0 })
                {
                    continue;
                }

                // Если хотя бы в одной задаче текущий сотрудник является основным исполнителем,
                // возвращаем null в качестве идентификатора замещаемого.
                if (sessionRoles.Where(x => !x.IsDeputy).Any())
                {
                    return null;
                }

                // Получаем запись роли заместителя.
                var deputy = sessionRoles.Where(x => x.IsDeputy).FirstOrDefault();

                // Если запись не найдена, переходим к следующему заданию.
                if (deputy is null)
                {
                    continue;
                }

                // Получаем ФРЗ для задания. 
                var assignedRoles = task.TryGetTaskAssignedRoles();

                // Если ФРЗ не удалось получить, переходим к следующем заданию.
                if (assignedRoles is null)
                {
                    continue;
                }

                // Получаем замещаемую роль из задания.
                var role = assignedRoles.First(x => x.RowID == deputy.TaskRoleRowID);

                // Если это персональная роль, устанавливаем ее идентификатор в качестве идентификатора замещаемого сотрудника.
                // В ином случае получаем список пользователей роли, из которого берем первое значение.
                if (role.RoleType == RoleType.Personal)
                {
                    deputizedId = role.RoleID;
                }
                else
                {
                    var docTypeID = await KrProcessSharedHelper.GetDocTypeIDAsync(card, dbScope, cancellationToken);
                    deputizedId = await GetDeputizedFromRole(dbScope, currentUserID, role.RoleID, docTypeID, cancellationToken);
                }

                // Если замещаемый сотрудник определен, возвращаем идентификатор.
                if (deputizedId.HasValue)
                {
                    return deputizedId;
                }
            }

            // Замещаемого сотрудника не удалось определить, возвращаем null.
            return null;
        }

        /// <summary>
        /// Обновляет идентификатор создателя файла для файлов.
        /// </summary>
        /// <param name="authorId">Идентификатор автора.</param>
        /// <param name="fileIDs">Идентификаторы прикрепляемых к карточке файлов.</param>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        private static async Task UpdateFileCreatorAsync(
            Guid authorId,
            IEnumerable<Guid> fileIDs,
            IDbScope dbScope,
            CancellationToken cancellationToken = default)
        {
            await using var _ = dbScope.Create();
            var db = dbScope.Db;

            var factory = await dbScope.GetBuilderFactoryAsync(cancellationToken);
            var query = factory.Cached(nameof(KrChangeFileAccessToDeputyStoreExtension), "UpdateFileCreator", b => b
                .Update("Files")
                .C("CreatedByID").Assign().P(nameof(authorId))
                .Where()
                .C("RowID").Equals().P("fileID")
                .Build());

            var idParam = db.Parameter(nameof(authorId), authorId, DataType.Guid);
            var fileIDParam = db.Parameter("fileID", DataType.Guid);

            foreach (var fileID in fileIDs)
            {
                fileIDParam.Value = fileID;
                await db
                    .SetCommand(query, idParam, fileIDParam)
                    .LogCommand()
                    .ExecuteNonQueryAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Получает идентификатор первого замещаемого пользователя для заместителя с учетом роли и типа документа.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="summary"/></param>
        /// <param name="deputyId">Идентификатор заместителя.</param>
        /// <param name="deputizedRoleID">Идентификатор роли.</param>
        /// <param name="docTypeId">Идентификатор типа документа или <see langword="null"/>, если тип документа не задан.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="summary"/></param>
        /// <returns>Идентификатор замещаемого пользователя или <see langword="null"/>, если пользователь не задан.</returns>
        private static async ValueTask<Guid?> GetDeputizedFromRole(
            IDbScope dbScope,
            Guid deputyId,
            Guid deputizedRoleID,
            Guid? docTypeId,
            CancellationToken cancellationToken)
        {
            // Пытаемся получить замещаемого без указания типа документов.
            await using var _ = dbScope.Create();
            var db = dbScope.Db;
            var builderFactory = dbScope.BuilderFactory;
            db
                .SetCommand(
                    builderFactory
                        .Select().Top(1).C("t", "ID").From()
                        .StartLogicalSubQuery("")
                            .StartLogicalSubQuery("")
                                .Select().Top(1).C("ru", "ID").As("ID")
                                .From(RoleStrings.RoleUsers, "ru").NoLock()
                                .InnerJoin(RoleStrings.RoleUsers, "ru2").NoLock()
                                    .On().C("ru2", "UserID").Equals().C("ru", "ID")
                                    .And().C("ru2", "ID").Equals().P("DeputizedRoleID")
                                .Where().C("ru", "IsDeputy").Equals().V(true)
                                    .And().C("ru", "UserID").Equals().P("UserID")
                                .Limit(1)
                            .EndLogicalSubQuery()
                            .UnionAll()
                            .StartLogicalSubQuery("")
                                .Select().Top(1).C("nr", "ParentID").As("ID")
                                .From(RoleStrings.RoleUsers, "ru").NoLock()
                                .InnerJoin(RoleStrings.NestedRoles, "nr").NoLock()
                                    .On().C("nr", "ID").Equals().C("ru", "ID")
                                    .And().C("nr", "ContextID").Equals().P("ContextID")
                                .InnerJoin(RoleStrings.RoleUsers, "ru2").NoLock()
                                    .On().C("nr", "ParentID").Equals().C("ru2", "UserID")
                                    .And().C("ru2", "ID").Equals().P("DeputizedRoleID")
                                .Where().C("ru", "UserID").Equals().P("UserID")
                                .Limit(1)
                            .EndLogicalSubQuery()
                        .EndLogicalSubQuery().As("t")
                        .Limit(1)
                .Build(),
                    db.Parameter("UserID", deputyId, DataType.Guid),
                    db.Parameter("DeputizedRoleID", deputizedRoleID, DataType.Guid),
                    db.Parameter("ContextID", docTypeId, DataType.Guid))
                .LogCommand();
                
                
                return await db.ExecuteAsync<Guid?>(cancellationToken);
        }

        #endregion
    }
}
