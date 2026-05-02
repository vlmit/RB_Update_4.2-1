#nullable enable

using LinqToDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Roles;
using Tessa.Roles.Deputies;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions.Files
{
    /// <inheritdoc/>
    public sealed class KrFileOwnershipChecker(
        IDbScope dbScope,
        IDeputiesManagementSettingsProvider deputiesProvider) : IKrFileOwnershipChecker
    {
        #region Constants

        // Ключ, по которому хранится кэш заместителей текущего пользователя по файлам для текущей карточки.
        private const string CardDeputiesCacheKey = StorageHelper.SystemKeyPrefix + "cardDeputiesCache";

        // Ключ, по которому в кэше заместителей хранится словарь заместителей.
        private const string DeputiesCacheKey = "deputies";

        // Ключ, по которому в кэше заместителей хранится идентификатор типа документа карточки.
        // Это нужно, когда в качестве параметра в метод передается только идентификатор карточки,
        // и, если тип документа не сохранять в кэше, то при следующем обращении снова придется
        // извлекать его из базы.
        private const string DocTypeIdCacheKey = "docType";

        #endregion

        #region Fields

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);
        private readonly IDeputiesManagementSettingsProvider deputiesProvider = NotNullOrThrow(deputiesProvider);

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public async ValueTask<bool> IsOwnAsync(
            Guid fileOrVersionCreatedByID,
            Guid userId,
            bool checkDeputized,
            Card? card = null,
            Guid? cardId = null,
            Guid? cardDocTypeId = null,
            Dictionary<string, object?>? cacheHolder = null,
            CancellationToken cancellationToken = default)
        {
            if (fileOrVersionCreatedByID == userId)
            {
                return true;
            }

            if (!checkDeputized)
            {
                return false;
            }

            var deputiesSettings = await this.deputiesProvider.GetSettingsAsync(cancellationToken);
            if (deputiesSettings.UseDeputyRoleSeparation)
            {
                return false;
            }

            Dictionary<string, object?>? cache = null;
            Dictionary<Guid, bool>? deputies = null;
            var docTypeId = cardDocTypeId;

            if (cacheHolder is not null)
            {
                cache = cacheHolder.TryGet<Dictionary<string, object?>>(CardDeputiesCacheKey);
                if (cache is null)
                {
                    cache = new Dictionary<string, object?>();
                    deputies = new Dictionary<Guid, bool>();
                    cache[DeputiesCacheKey] = deputies;
                    docTypeId ??= await this.GetDocTypeIdAsync(card, cardId, cancellationToken);
                    cache[DocTypeIdCacheKey] = docTypeId;
                    cacheHolder[CardDeputiesCacheKey] = cache;
                }
                else
                {
                    deputies = (Dictionary<Guid, bool>)cache[DeputiesCacheKey]!;
                    if (deputies?.TryGetValue(fileOrVersionCreatedByID, out var isDeputizedFile) == true)
                    {
                        return isDeputizedFile;
                    }
                    docTypeId ??= (Guid?)cache[DocTypeIdCacheKey];
                }
            }
            else
            {
                docTypeId ??= await this.GetDocTypeIdAsync(card, cardId, cancellationToken);
            }

            // Контекстом может быть тип документа, если для типа карточки используются типы документа,
            // или тип карточки, если для типа карточки не используется тип документа, но карточка в типовом решении.
            docTypeId ??= card?.TypeID;

            var isDeputized = await IsDeputizedAsync(
                    userId,
                    fileOrVersionCreatedByID,
                    docTypeId, 
                    cancellationToken);

            if (deputies is not null)
            {
                deputies[fileOrVersionCreatedByID] = isDeputized;
            }

            return isDeputized;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Получает идентификатор типа документа по карточке / идентификатору карточки.
        /// </summary>
        /// <param name="card">Карточка или <see langword="null"/>.</param>
        /// <param name="cardId">Идентификатор карточки или <see langword="null"/>.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="summary"/></param>
        /// <returns>Идентификатор типа документа или  <see langword="null"/>, если его не удалось определить.</returns>
        private async Task<Guid?> GetDocTypeIdAsync(
            Card? card,
            Guid? cardId,
            CancellationToken cancellationToken = default)
        {
            Guid? docTypeId = null;
            if (card is not null)
            {
                KrProcessSharedHelper.TryGetDocTypeID(card, out docTypeId);
            }
            if (docTypeId.HasValue)
            {
                return docTypeId;
            }
            return cardId.HasValue
                ? await KrProcessSharedHelper.GetDocTypeIDAsync(cardId.Value, this.dbScope, cancellationToken)
                : null;
        }

        /// <summary>
        /// Определяет, замещает ли пользователь указанного сотрудника с учетом типа документа.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="deputizedId">Идентификатор потенциального замещаемого.</param>
        /// <param name="docTypeId">Идентификатор типа документа карточки или <see langword="null"/>, если значение не задано.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="summary"/></param>
        /// <returns><see langword="true"/>, если пользователь замещает указанного сотрудника.</returns>
        private async ValueTask<bool> IsDeputizedAsync(
            Guid userId,
            Guid deputizedId,
            Guid? docTypeId,
            CancellationToken cancellationToken = default)
        {
            await using var _ = this.dbScope.Create();
            var db = this.dbScope.Db;
            return await db
                .SetCommand(
                    this.dbScope.BuilderFactory
                        .SelectExists(x => x
                            .StartLogicalSubQuery("")
                            .Select().Top(1).V(null)
                                .From(RoleStrings.RoleUsers, "ru").NoLock()
                                .Where().C("ru", "IsDeputy").Equals().V(true)
                                    .And().C("ru", "UserID").Equals().P("UserID")
                                    .And().C("ru", "ID").Equals().P("DeputizedID")
                                .Limit(1)
                            .EndLogicalSubQuery()
                            .UnionAll()
                            .StartLogicalSubQuery("")
                            .Select().Top(1).V(null)
                                .From(RoleStrings.RoleUsers, "ru").NoLock()
                                .InnerJoin(RoleStrings.NestedRoles, "nr").NoLock()
                                    .On().C("nr", "ID").Equals().C("ru", "ID")
                                    .And().C("nr", "ContextID").Equals().P("ContextID")
                                    .And().C("nr", "ParentID").Equals().P("DeputizedID")
                                .Where().C("ru", "UserID").Equals().P("UserID")
                                .Limit(1)
                            .EndLogicalSubQuery())
                        .Build(),
                    db.Parameter("UserID", userId, DataType.Guid),
                    db.Parameter("DeputizedID", deputizedId, DataType.Guid),
                    db.Parameter("ContextID", docTypeId, DataType.Guid))
                .LogCommand()
                .ExecuteAsync<bool>(cancellationToken);
        }

        #endregion
    }
}
