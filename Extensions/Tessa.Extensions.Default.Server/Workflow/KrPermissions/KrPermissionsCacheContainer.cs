using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Server.RefGroups;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions.Files;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.RefGroups;
using Tessa.Platform.RefGroups.Caching;
using Tessa.Platform.Validation;
using Tessa.Scheme;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions
{
    /// <inheritdoc cref="IKrPermissionsCacheContainer" />
    public sealed class KrPermissionsCacheContainer : IKrPermissionsCacheContainer, IDisposable
    {
        #region Nested Types

        private class KrPermissionsByType : Dictionary<Guid, KrPermissionsByState>
        {
        }

        private class KrPermissionsByState : Dictionary<int, KrPermissionsLists>
        {
        }

        private class KrPermissionsLists
        {
            public List<IKrPermissionRuleSettings> ExtendedRules { get; } = new();
            public List<IKrPermissionRuleSettings> RequiredRules { get; } = new();
            public List<IKrPermissionRuleSettings> AllRules { get; } = new();
        }

        private class ExtendedTaskRuleRow
        {
            public Guid RuleID { get; init; }
            public Guid RowID { get; init; }
            public Guid SectionID { get; init; }
            public int? AccessSettingID { get; init; }
            public bool IsHidden { get; init; }
            public List<Guid> Fields { get; } = new();
            public List<Guid> Types { get; } = new();
        }

        private sealed record CachedVersion(long Value);

        /// <inheritdoc cref="IKrPermissionsCache" />
        private class KrPermissionsCacheObject(long version) : IKrPermissionsCache
        {
            #region Properties

            public KrPermissionsByType PermissionsByType { get; set; }

            public IDictionary<Guid, IKrPermissionRuleSettings> PermissionsByID { get; set; }

            #endregion

            #region IKrPermissionsCache Implementation

            /// <inheritdoc />
            public long Version { get; } = version;

            /// <inheritdoc />
            public IDictionary<Guid, IKrPermissionRuleSettings> GetAll() => this.PermissionsByID;

            /// <inheritdoc />
            public IKrPermissionRuleSettings GetRuleByID(Guid ruleID)
            {
                return this.PermissionsByID.TryGetValue(ruleID, out var result)
                    ? result
                    : null;
            }

            /// <inheritdoc />
            public IEnumerable<IKrPermissionRuleSettings> GetExtendedRules(Guid typeID, KrState? state)
            {
                if (this.PermissionsByType.TryGetValue(typeID, out var byState)
                    && byState.TryGetValue(state?.ID ?? CreateFakeStateID, out var lists))
                {
                    return lists.ExtendedRules;
                }

                return Array.Empty<IKrPermissionRuleSettings>();
            }

            /// <inheritdoc />
            public IEnumerable<IKrPermissionRuleSettings> GetRequiredRules(Guid typeID, KrState? state)
            {
                if (this.PermissionsByType.TryGetValue(typeID, out var byState)
                    && byState.TryGetValue(state?.ID ?? CreateFakeStateID, out var lists))
                {
                    return lists.RequiredRules;
                }

                return Array.Empty<IKrPermissionRuleSettings>();
            }

            /// <inheritdoc />
            public IEnumerable<IKrPermissionRuleSettings> GetRulesByTypeAndState(Guid typeID, KrState? state)
            {
                if (this.PermissionsByType.TryGetValue(typeID, out var byState)
                    && byState.TryGetValue(state?.ID ?? CreateFakeStateID, out var lists))
                {
                    return lists.AllRules;
                }

                return Array.Empty<IKrPermissionRuleSettings>();
            }

            #endregion
        }

        #endregion

        #region Fields

        private const int CreateFakeStateID = int.MinValue;

        private readonly IDbScope dbScope;
        private readonly ICardCache cache;
        private readonly ICardCache localCache;
        private readonly ICardMetadata cardMetadata;
        private readonly ISchemeService schemeService;
        private readonly IKrPermissionsLockStrategy lockStrategy;
        private readonly ITransactionStrategy transactionStrategy;
        private readonly ISeparateDbConnectionWorker worker;
        private readonly IRefGroupsManager refGroupsManager;
        private readonly RefGroupsGlobalCache refGroupsGlobalCache;
        private readonly AsyncLock asyncLock = new();
        private readonly IKrFileOwnershipChecker krFileOwnershipChecker;

        private const string CacheKey = nameof(IKrPermissionsCacheContainer);
        private const string VersionKey = CacheKey + "Version";

        #endregion

        #region Constructors

        public KrPermissionsCacheContainer(
            IDbScope dbScope,
            ICardCache cache,
            [Dependency(CardCacheNames.Local)] ICardCache localCache,
            ICardMetadata cardMetadata,
            ISchemeService schemeService,
            IKrPermissionsLockStrategy lockStrategy,
            ICardTransactionStrategy transactionStrategy,
            ISeparateDbConnectionWorker worker,
            IRefGroupsManager refGroupsManager,
            RefGroupsGlobalCache refGroupsGlobalCache,
            IKrFileOwnershipChecker krFileOwnershipChecker,
            [OptionalDependency] IUnityDisposableContainer disposableContainer)
        {
            this.dbScope = NotNullOrThrow(dbScope);
            this.cache = NotNullOrThrow(cache);
            this.localCache = NotNullOrThrow(localCache);
            this.cardMetadata = NotNullOrThrow(cardMetadata);
            this.schemeService = NotNullOrThrow(schemeService);
            this.lockStrategy = NotNullOrThrow(lockStrategy);
            this.transactionStrategy = NotNullOrThrow(transactionStrategy);
            this.refGroupsManager = NotNullOrThrow(refGroupsManager);
            this.refGroupsGlobalCache = NotNullOrThrow(refGroupsGlobalCache);
            this.krFileOwnershipChecker = NotNullOrThrow(krFileOwnershipChecker);
            this.worker = NotNullOrThrow(worker);

            disposableContainer?.Register(this);

            // TODO async global cache invalidation
            var valueTask = refGroupsGlobalCache.AddInvalidatedHandlerAsync(this.InvalidateCacheIfRequiredAsync);
            if (!valueTask.IsCompletedSuccessfully)
            {
                valueTask.AsTask().GetAwaiter().GetResult();
            }
        }

        #endregion

        #region IKrPermissionsCacheContainer Implementation

        /// <inheritdoc />
        public async ValueTask<long> GetVersionAsync(CancellationToken cancellationToken = default)
        {
            var result = await this.cache.Settings.TryGetAlreadyCachedAsync<CachedVersion?>(VersionKey, cancellationToken);
            if (result is not null)
            {
                return result.Value;
            }

            using (await this.asyncLock.EnterAsync(cancellationToken))
            {
                result = await this.cache.Settings.TryGetAlreadyCachedAsync<CachedVersion?>(VersionKey, cancellationToken);
                if (result is not null)
                {
                    return result.Value;
                }

                var dbResult = new CachedVersion(await this.GetVersionFromDatabaseAsync(cancellationToken));
                return (await this.cache.Settings.GetAsync(VersionKey, x => dbResult, cancellationToken)).Value;
            }
        }

        /// <inheritdoc />
        public Task UpdateVersionAsync(CancellationToken cancellationToken = default) =>
            this.UpdateVersionCoreAsync(this.cache, cancellationToken);

        /// <inheritdoc />
        public async ValueTask<IKrPermissionsCache> TryGetCacheAsync(IValidationResultBuilder validationResult, CancellationToken cancellationToken = default)
        {
            var result = await this.cache.Settings.TryGetAlreadyCachedAsync<KrPermissionsCacheObject>(CacheKey, cancellationToken);
            if (result is not null)
            {
                return result;
            }

            using (await this.asyncLock.EnterAsync(cancellationToken))
            {
                result = await this.cache.Settings.TryGetAlreadyCachedAsync<KrPermissionsCacheObject>(CacheKey, cancellationToken);
                if (result is not null)
                {
                    return result;
                }

                result = await this.GetPermissionsFromDatabaseAsync(validationResult, cancellationToken);
                if (validationResult.IsSuccessful())
                {
                    await this.cache.Settings.GetAsync(VersionKey, x => new CachedVersion(result.Version), cancellationToken);
                    return await this.cache.Settings.GetAsync(CacheKey, x => result, cancellationToken);
                }

                return null;
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <inheritdoc/>
        public void Dispose()
        {
            this.refGroupsGlobalCache.RemoveInvalidatedHandler(this.InvalidateCacheIfRequiredAsync);
            this.asyncLock.Dispose();
        }

        #endregion

        #region Private Methods

        private async Task IncreaseVersionAsync(CancellationToken cancellationToken)
        {
            await using (this.dbScope.CreateNew())
            {
                var executor = this.dbScope.Executor;
                await executor.ExecuteNonQueryAsync(
                    this.dbScope.BuilderFactory
                        .Update(KrPermissionsHelper.SystemTable)
                        .C("Version").Assign().C("Version").Add(1)
                        .Build(),
                    cancellationToken);
            }
        }

        private async Task<long> GetVersionFromDatabaseAsync(CancellationToken cancellationToken)
        {
            await using (this.worker.CreateScope())
            {
                var db = this.dbScope.Db;

                return await db.SetCommand(this.dbScope.BuilderFactory
                        .Select().Top(1).C("Version")
                        .From(KrPermissionsHelper.SystemTable).NoLock()
                        .Limit(1)
                        .Build())
                    .LogCommand()
                    .ExecuteAsync<long>(cancellationToken);
            }
        }

        private async Task<KrPermissionsCacheObject> GetPermissionsFromDatabaseAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken)
        {
            await using (this.worker.CreateScope())
            {
                KrPermissionsCacheObject result = null;

                await this.transactionStrategy.ExecuteInTransactionAsync(
                    validationResult,
                    async tp =>
                    {
                        var lockResult = await this.lockStrategy.ObtainReaderLockAsync(tp.CancellationToken);
                        if (lockResult.HasErrors)
                        {
                            tp.ValidationResult.AddError(
                                this,
                                "$KrPermissions_UnableToLoadPermissionsMessage");
                            tp.ReportError = true;
                            return;
                        }

                        var permissionsByID = new Dictionary<Guid, IKrPermissionRuleSettings>();
                        result = new KrPermissionsCacheObject(await this.GetVersionFromDatabaseAsync(tp.CancellationToken));

                        var db = tp.DbScope.Db;
                        await this.FillPermissionMainAsync(db, permissionsByID, tp.CancellationToken);
                        await this.FillPermissionContextRolesAsync(db, permissionsByID, tp.CancellationToken);
                        await this.FillPermissionHashAsync<Guid>(db, "KrPermissionTypes", "TypeID", permissionsByID,
                            static (p, v) => p.Types.Add(v), tp.CancellationToken);
                        await this.FillPermissionHashAsync<short>(db, "KrPermissionStates", "StateID", permissionsByID,
                            static (p, v) => p.States.Add(v), tp.CancellationToken);
                        await this.FillPermissionCardExtendedSettingsAsync(db, permissionsByID, tp.CancellationToken);
                        await this.FillPermissionTaskExtendedSettingsAsync(db, permissionsByID, tp.CancellationToken);
                        await this.FillPermissionMandatoryExtendedSettingsAsync(db, permissionsByID, tp.CancellationToken);
                        await this.FillPermissionVisibilityExtendedSettingsAsync(db, permissionsByID, tp.CancellationToken);
                        await this.FillPermissionFileExtendedSettingsAsync(db, permissionsByID, tp.CancellationToken);

                        result.PermissionsByID = permissionsByID;
                        var byTypes = result.PermissionsByType = new KrPermissionsByType();

                        foreach (var permission in permissionsByID.Values)
                        {
                            foreach (var storedTypeID in permission.Types)
                            {
                                var typesGroup = await this.refGroupsManager
                                    .GetGroupAsync(storedTypeID, RefGroupsHelper.DocumentTypeRefGroupTypeID, cancellationToken);
                                if (typesGroup is null)
                                {
                                    await this.FillTypeAsync(byTypes, storedTypeID, permission, cancellationToken);
                                }
                                else if (typesGroup.CalculatedValues is { Count: > 0 } calculatedValues)
                                {
                                    foreach (var stateId in calculatedValues.Select(x => (Guid) x.ID))
                                    {
                                        await this.FillTypeAsync(byTypes, stateId, permission, cancellationToken);
                                    }
                                }
                            }
                        }
                    },
                    cancellationToken);

                return result;
            }
        }

        private async ValueTask FillTypeAsync(
            KrPermissionsByType byTypes,
            Guid typeID,
            IKrPermissionRuleSettings permission,
            CancellationToken cancellationToken = default
        )
        {
            if (!byTypes.TryGetValue(typeID, out var byState))
            {
                byState = new KrPermissionsByState();
                byTypes[typeID] = byState;
            }

            foreach (var storedStateID in permission.States)
            {
                if (await this.refGroupsManager
                    .GetGroupAsync(storedStateID, KrRefGroupsHelper.KrStateRefGroupTypeID, cancellationToken) is not { } stateGroup)
                {
                    FillDocState(byState, storedStateID, permission);
                }
                else if (stateGroup.CalculatedValues is { Count: > 0 } calculatedValues)
                {
                    foreach (var stateId in calculatedValues.Select(x => (int) x.ID))
                    {
                        FillDocState(byState, stateId, permission);
                    }
                }
            }

            // Создаём список со всеми правилами, где есть создание карточки данного типа.
            if (permission.Flags.Contains(KrPermissionFlagDescriptors.CreateCard))
            {
                if (!byState.TryGetValue(CreateFakeStateID, out var lists))
                {
                    lists = new KrPermissionsLists();
                    byState[CreateFakeStateID] = lists;
                }

                lists.AllRules.Add(permission);
                if (permission.IsExtended)
                {
                    lists.ExtendedRules.Add(permission);
                }

                if (permission.IsRequired)
                {
                    lists.RequiredRules.Add(permission);
                }
            }
        }

        private static void FillDocState(KrPermissionsByState byState, int stateId, IKrPermissionRuleSettings permission)
        {
            if (!byState.TryGetValue(stateId, out var lists))
            {
                lists = new KrPermissionsLists();
                byState[stateId] = lists;
            }

            lists.AllRules.Add(permission);
            if (permission.IsExtended)
            {
                lists.ExtendedRules.Add(permission);
            }

            if (permission.IsRequired)
            {
                lists.RequiredRules.Add(permission);
            }
        }

        private async Task FillPermissionFileExtendedSettingsAsync(
            DbManager db,
            Dictionary<Guid, IKrPermissionRuleSettings> permissionsByID,
            CancellationToken cancellationToken)
        {
            db.SetCommand(
                    this.dbScope.BuilderFactory
                        .Select()
                        .C("r", "ID", "RowID", "Extensions", "FileCheckRuleID", // 0 - 3
                            "ReadAccessSettingID", "EditAccessSettingID", "DeleteAccessSettingID", "SignAccessSettingID", "AddAccessSettingID", "CreateLinkAccessSettingID",
                            "FileSizeLimit", "MaxCount", "Mandatory") // 4 - 12
                        .C("c", "CategoryID") // 13
                        .From("KrPermissionExtendedFileRules", "r").NoLock()
                        .LeftJoin("KrPermissionExtendedFileRuleCategories", "c").NoLock()
                        .On().C("c", "RuleRowID").Equals().C("r", "RowID")
                        .OrderBy("r", "ID").By("r", "RowID")
                        .Build())
                .LogCommand();

            await using var reader = await db.ExecuteReaderAsync(cancellationToken);
            var currentRuleID = Guid.Empty;
            IKrPermissionRuleSettings currentRule = null;

            var currentFileRuleID = Guid.Empty;
            ICollection<Guid> currentFileRuleCategories = null;
            string extensions = null;
            int fileCheckRule = KrPermissionsHelper.FileCheckRules.FilesOfOtherUsers;
            bool mandatory = false;
            int? readAccessSetting = null,
                editAccessSetting = null,
                deleteAccessSetting = null,
                signAccessSetting = null,
                createLinkAccessSetting = null,
                addAccessSetting = null,
                fileSizeLimit = null,
                maxCount = null;

            while (await reader.ReadAsync(cancellationToken))
            {
                var ruleID = reader.GetValue<Guid>(0);
                if (ruleID != currentRuleID)
                {
                    if (currentFileRuleID != Guid.Empty)
                    {
                        var fileRule = new KrPermissionsFileRule(this.dbScope, this.krFileOwnershipChecker, extensions, currentFileRuleCategories)
                        {
                            Priority = currentRule.Priority,
                            FileCheckRule = fileCheckRule,
                            ReadAccessSetting = readAccessSetting,
                            EditAccessSetting = editAccessSetting,
                            DeleteAccessSetting = deleteAccessSetting,
                            SignAccessSetting = signAccessSetting,
                            CreateLinkAccessSetting = createLinkAccessSetting,
                            AddAccessSetting = addAccessSetting,
                            FileSizeLimit = fileSizeLimit,
                            MaxCount = maxCount,
                            Mandatory = mandatory
                        };

                        currentRule.FileRules.Add(fileRule);

                        currentFileRuleCategories?.Clear();
                    }

                    // При смене правила сбрасываем секцию.
                    currentRuleID = ruleID;
                    currentFileRuleID = Guid.Empty;
                    if (!permissionsByID.TryGetValue(ruleID, out currentRule))
                    {
                        continue;
                    }
                }

                // Правило, для которого актуальны данные настройки, отключено.
                if (currentRule is null)
                {
                    continue;
                }

                var fileRuleID = reader.GetGuid(1);
                var categoryID = reader.GetNullableGuid(13);

                if (fileRuleID != currentFileRuleID)
                {
                    if (currentFileRuleID != Guid.Empty)
                    {
                        var fileRule = new KrPermissionsFileRule(this.dbScope, this.krFileOwnershipChecker, extensions, currentFileRuleCategories)
                        {
                            Priority = currentRule.Priority,
                            FileCheckRule = fileCheckRule,
                            ReadAccessSetting = readAccessSetting,
                            EditAccessSetting = editAccessSetting,
                            DeleteAccessSetting = deleteAccessSetting,
                            SignAccessSetting = signAccessSetting,
                            CreateLinkAccessSetting = createLinkAccessSetting,
                            AddAccessSetting = addAccessSetting,
                            FileSizeLimit = fileSizeLimit,
                            MaxCount = maxCount,
                            Mandatory = mandatory
                        };

                        currentRule.FileRules.Add(fileRule);

                        currentFileRuleCategories?.Clear();
                    }

                    extensions = reader.GetNullableString(2);
                    fileCheckRule = reader.GetInt32(3);
                    readAccessSetting = reader.GetNullableInt32(4);
                    editAccessSetting = reader.GetNullableInt32(5);
                    deleteAccessSetting = reader.GetNullableInt32(6);
                    signAccessSetting = reader.GetNullableInt32(7);
                    addAccessSetting = reader.GetNullableInt32(8);
                    createLinkAccessSetting = reader.GetNullableInt32(9);
                    fileSizeLimit = reader.GetNullableInt32(10) is { } fileSize
                        ? fileSize * 1000
                        : null;

                    maxCount = reader.GetNullableInt32(11);
                    mandatory = reader.GetBoolean(12);

                    currentFileRuleID = fileRuleID;
                }

                if (categoryID.HasValue)
                {
                    currentFileRuleCategories ??= new HashSet<Guid>();
                    currentFileRuleCategories.Add(categoryID.Value);
                }
            }

            if (currentFileRuleID != Guid.Empty)
            {
                var fileRule = new KrPermissionsFileRule(this.dbScope, this.krFileOwnershipChecker, extensions, currentFileRuleCategories)
                {
                    Priority = currentRule.Priority,
                    FileCheckRule = fileCheckRule,
                    ReadAccessSetting = readAccessSetting,
                    EditAccessSetting = editAccessSetting,
                    DeleteAccessSetting = deleteAccessSetting,
                    SignAccessSetting = signAccessSetting,
                    CreateLinkAccessSetting = createLinkAccessSetting,
                    AddAccessSetting = addAccessSetting,
                    FileSizeLimit = fileSizeLimit,
                    MaxCount = maxCount,
                    Mandatory = mandatory
                };

                currentRule.FileRules.Add(fileRule);
            }
        }

        private async Task FillPermissionVisibilityExtendedSettingsAsync(
            DbManager db,
            Dictionary<Guid, IKrPermissionRuleSettings> permissionsByID,
            CancellationToken cancellationToken)
        {
            db.SetCommand(
                    this.dbScope.BuilderFactory
                        .Select()
                        .C("s", "ID", "Alias", "ControlTypeID", "IsHidden") // 0 - 3
                        .From("KrPermissionExtendedVisibilityRules", "s").NoLock()
                        .OrderBy("s", "ID")
                        .Build())
                .LogCommand();

            await using var reader = await db.ExecuteReaderAsync(cancellationToken);
            var prevRuleID = Guid.Empty;
            IKrPermissionRuleSettings prevRule = null;
            while (await reader.ReadAsync(cancellationToken))
            {
                var ruleID = reader.GetValue<Guid>(0);
                if (ruleID != prevRuleID)
                {
                    // При смене правила сбрасываем секцию
                    prevRuleID = ruleID;
                    if (!permissionsByID.TryGetValue(ruleID, out prevRule))
                    {
                        continue;
                    }
                }

                // Правило, для которого актуальны данные настройки, отключено
                if (prevRule is null)
                {
                    continue;
                }

                prevRule.VisibilitySettings.Add(
                    new KrPermissionVisibilitySettings(
                        reader.GetValue<string>(1),
                        reader.GetValue<int>(2),
                        reader.GetValue<bool>(3)));
            }
        }

        private async Task FillPermissionMandatoryExtendedSettingsAsync(
            DbManager db,
            Dictionary<Guid, IKrPermissionRuleSettings> permissionsByID,
            CancellationToken cancellationToken)
        {
            var allMandatoryRules = new Dictionary<Guid, KrPermissionMandatoryRule>();

            db.SetCommand(
                    this.dbScope.BuilderFactory
                        .Select()
                        .C("s", "ID", "RowID", "SectionID", "ValidationTypeID", "Text") // 0 - 4
                        .C("f", "FieldID") // 5
                        .From("KrPermissionExtendedMandatoryRules", "s").NoLock()
                        .LeftJoin("KrPermissionExtendedMandatoryRuleFields", "f").NoLock()
                        .On().C("f", "RuleRowID").Equals().C("s", "RowID")
                        .OrderBy("s", "ID").By("s", "SectionID")
                        .Build())
                .LogCommand();

            // Сперва грузим все настройки по строкам. При этом настройки для одной и той же секции в разных строках пишутся в несколько объектов.
            await using (var reader = await db.ExecuteReaderAsync(cancellationToken))
            {
                Guid? prevRuleID = null;
                IKrPermissionRuleSettings prevRule = null;

                Guid? prevRowID = null;
                KrPermissionMandatoryRule prevMandatory = null;

                while (await reader.ReadAsync(cancellationToken))
                {
                    var ruleID = reader.GetValue<Guid>(0);
                    if (ruleID != prevRuleID)
                    {
                        // При смене правила сбрасываем секцию.
                        prevRuleID = ruleID;
                        prevRowID = null;
                        if (!permissionsByID.TryGetValue(ruleID, out prevRule))
                        {
                            continue;
                        }
                    }

                    // Правило, для которого актуальны данные настройки, отключено.
                    if (prevRule is null)
                    {
                        continue;
                    }

                    var rowID = reader.GetValue<Guid>(1);
                    if (rowID != prevRowID)
                    {
                        var sectionID = reader.GetValue<Guid>(2);
                        var validationTypeID = reader.GetValue<int>(3);
                        var text = reader.GetValue<string>(4);

                        prevRowID = rowID;
                        prevMandatory = new KrPermissionMandatoryRule(
                            sectionID,
                            text,
                            validationTypeID);
                        allMandatoryRules[rowID] = prevMandatory;
                        prevRule.MandatoryRules.Add(prevMandatory);
                    }

                    var fieldID = reader.GetValue<Guid?>(5);

                    if (fieldID.HasValue)
                    {
                        prevMandatory.ColumnIDs.Add(fieldID.Value);
                    }
                }
            }

            // Затем грузим типы заданий по ID строкам и формируем результат по типам.
            db.SetCommand(
                    this.dbScope.BuilderFactory
                        .Select()
                        .C("t", "ID", "RuleRowID", "TaskTypeID") // 0 - 2
                        .From("KrPermissionExtendedMandatoryRuleTypes", "t").NoLock()
                        .OrderBy("t", "ID").By("t", "RuleRowID")
                        .Build())
                .LogCommand();

            await using (var reader = await db.ExecuteReaderAsync(cancellationToken))
            {
                Guid? prevRuleID = null;
                IKrPermissionRuleSettings prevRule = null;

                Guid? prevMandatoryID = null;
                KrPermissionMandatoryRule prevMandatory = null;

                while (await reader.ReadAsync(cancellationToken))
                {
                    var ruleID = reader.GetValue<Guid>(0);
                    if (ruleID != prevRuleID)
                    {
                        prevRuleID = ruleID;
                        prevMandatoryID = null;
                        prevMandatory = null;
                        if (!permissionsByID.TryGetValue(ruleID, out prevRule))
                        {
                            continue;
                        }
                    }

                    // Правило, для которого актуальны данные настройки, отключено.
                    if (prevRule is null)
                    {
                        continue;
                    }

                    var mandatoryID = reader.GetValue<Guid>(1);
                    var typeID = reader.GetValue<Guid>(2);
                    if (mandatoryID != prevMandatoryID)
                    {
                        prevMandatoryID = mandatoryID;
                        prevMandatory = allMandatoryRules[mandatoryID];
                    }

                    prevMandatory.TaskTypes.Add(typeID);
                }
            }

            // Затем грузим варианты завершения по ID строкам и формируем результат по типам.
            db.SetCommand(
                    this.dbScope.BuilderFactory
                        .Select()
                        .C("t", "ID", "RuleRowID", "OptionID") // 0 - 2
                        .From("KrPermissionExtendedMandatoryRuleOptions", "t").NoLock()
                        .OrderBy("t", "ID").By("t", "RuleRowID")
                        .Build())
                .LogCommand();

            await using (var reader = await db.ExecuteReaderAsync(cancellationToken))
            {
                Guid? prevRuleID = null;
                IKrPermissionRuleSettings prevRule = null;

                Guid? prevMandatoryID = null;
                KrPermissionMandatoryRule prevMandatory = null;

                while (await reader.ReadAsync(cancellationToken))
                {
                    var ruleID = reader.GetValue<Guid>(0);
                    if (ruleID != prevRuleID)
                    {
                        prevRuleID = ruleID;
                        prevMandatoryID = null;
                        prevMandatory = null;
                        if (!permissionsByID.TryGetValue(ruleID, out prevRule))
                        {
                            continue;
                        }
                    }

                    // Правило, для которого актуальны данные настройки, отключено
                    if (prevRule is null)
                    {
                        continue;
                    }

                    var mandatoryID = reader.GetValue<Guid>(1);
                    var optionID = reader.GetValue<Guid>(2);
                    if (mandatoryID != prevMandatoryID)
                    {
                        prevMandatoryID = mandatoryID;
                        prevMandatory = allMandatoryRules[mandatoryID];
                    }

                    prevMandatory.CompletionOptions.Add(optionID);
                }
            }

            var sections = (await this.schemeService.GetTablesAsync(cancellationToken)).ToDictionary(x => x.ID, x => x);
            // Заполняем обязательность полей в настройках правил доступа
            foreach (var permission in permissionsByID.Values)
            {
                foreach (var mandatoryRule in permission.MandatoryRules)
                {
                    // Такой род обязательности не отмечаем на карточке
                    if (mandatoryRule.ValidationType == KrPermissionsHelper.MandatoryValidationType.WhenOneFieldFilled)
                    {
                        continue;
                    }

                    var section = sections[mandatoryRule.SectionID];
                    if (section.InstanceType == SchemeTableInstanceType.Cards
                        && mandatoryRule.ValidationType == KrPermissionsHelper.MandatoryValidationType.Always)
                    {
                        if (!permission.CardSettings.TryGetItem(mandatoryRule.SectionID, out var sectionSettings))
                        {
                            sectionSettings = new KrPermissionSectionSettings
                            {
                                ID = mandatoryRule.SectionID,
                            };

                            permission.CardSettings.Add(sectionSettings);
                        }

                        if (mandatoryRule.HasColumns)
                        {
                            sectionSettings.MandatoryFields.AddRange(mandatoryRule.ColumnIDs);
                        }
                        else
                        {
                            sectionSettings.IsMandatory = true;
                        }
                    }
                    else if (section.InstanceType == SchemeTableInstanceType.Tasks
                             && mandatoryRule.ValidationType == KrPermissionsHelper.MandatoryValidationType.OnTaskCompletion
                             && mandatoryRule.HasTaskTypes)
                    {
                        foreach (var taskType in mandatoryRule.TaskTypes)
                        {
                            var taskMeta = await this.cardMetadata.GetMetadataForTypeAsync(taskType, cancellationToken);
                            if (!(await taskMeta.GetSectionsAsync(cancellationToken)).Contains(mandatoryRule.SectionID))
                            {
                                // Эта секция отсутствует в данном типе задания
                                continue;
                            }

                            if (!permission.TaskSettingsByTypes.TryGetValue(taskType, out var taskSettings))
                            {
                                taskSettings = new HashSet<Guid, KrPermissionSectionSettings>(x => x.ID);
                                permission.TaskSettingsByTypes.Add(taskType, taskSettings);
                            }

                            if (!taskSettings.TryGetItem(mandatoryRule.SectionID, out var sectionSettings))
                            {
                                sectionSettings = new KrPermissionSectionSettings
                                {
                                    ID = mandatoryRule.SectionID,
                                };

                                taskSettings.Add(sectionSettings);
                            }

                            if (mandatoryRule.HasColumns)
                            {
                                sectionSettings.MandatoryFields.AddRange(mandatoryRule.ColumnIDs);
                            }
                            else
                            {
                                sectionSettings.IsMandatory = true;
                            }
                        }
                    }
                }
            }
        }

        private async Task FillPermissionTaskExtendedSettingsAsync(
            DbManager db,
            Dictionary<Guid, IKrPermissionRuleSettings> permissionsByID,
            CancellationToken cancellationToken = default)
        {
            var taskGroups = await this.refGroupsManager
                .GetGroupsByTypeAsync(RefGroupsHelper.TaskTypeRefGroupTypeID, cancellationToken);
            var ruleRows = new HashSet<Guid, ExtendedTaskRuleRow>(x => x.RowID);

            db.SetCommand(
                    this.dbScope.BuilderFactory
                        .Select()
                        .C("r", "ID", "RowID", "SectionID", "AccessSettingID", "IsHidden")
                        .C("rf", "FieldID")
                        .From("KrPermissionExtendedTaskRules", "r").NoLock()
                        .LeftJoin("KrPermissionExtendedTaskRuleFields", "rf").NoLock()
                        .On().C("rf", "RuleRowID").Equals().C("r", "RowID")
                        .Build())
                .LogCommand();

            await using (var reader = await db.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var ruleID = reader.GetValue<Guid>(0);
                    var rowID = reader.GetValue<Guid>(1);
                    var sectionID = reader.GetValue<Guid>(2);
                    var accessSettings = reader.GetValue<int?>(3);
                    var isHidden = reader.GetValue<bool>(4);
                    var fieldID = reader.GetValue<Guid?>(5);
                    if (!ruleRows.TryGetItem(rowID, out var ruleRow))
                    {
                        ruleRow = new ExtendedTaskRuleRow()
                        {
                            RuleID = ruleID,
                            AccessSettingID = accessSettings,
                            IsHidden = isHidden,
                            RowID = rowID,
                            SectionID = sectionID,
                        };
                        ruleRows.Add(ruleRow);
                    }

                    if (fieldID.HasValue && !ruleRow.Fields.Contains(fieldID.Value))
                    {
                        ruleRow.Fields.Add(fieldID.Value);
                    }
                }
            }

            db.SetCommand(
                    this.dbScope.BuilderFactory
                        .Select()
                        .C("t", "RuleRowID", "TaskTypeID")
                        .From("KrPermissionExtendedTaskRuleTypes", "t").NoLock()
                        .Build())
                .LogCommand();

            await using (var reader = await db.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var rowID = reader.GetValue<Guid>(0);
                    var typeStored = reader.GetValue<Guid>(1);

                    if (!ruleRows.TryGetItem(rowID, out var ruleRow))
                    {
                        continue;
                    }

                    if (taskGroups.FirstOrDefault(x => x.ID == typeStored) is { } group)
                    {
                        if (group.CalculatedValues is null)
                        {
                            continue;
                        }

                        var typeIDs = group.CalculatedValues.Select(x => (Guid) x.ID);
                        foreach (var typeID in typeIDs)
                        {
                            if (!ruleRow.Types.Contains(typeID))
                            {
                                ruleRow.Types.Add(typeID);
                            }
                        }
                    }
                    else if (!ruleRow.Types.Contains(typeStored))
                    {
                        ruleRow.Types.Add(typeStored);
                    }
                }
            }

            foreach (var ruleRow in ruleRows)
            {
                var ruleId = ruleRow.RuleID;

                if (!permissionsByID.TryGetValue(ruleId, out var permissionRuleSettings))
                {
                    continue;
                }

                var settings = new KrPermissionSectionSettings()
                {
                    ID = ruleRow.SectionID,
                };
                var isHidden = ruleRow.IsHidden;
                var accessSettings = ruleRow.AccessSettingID;
                if (ruleRow.Fields is { Count: > 0 })
                {
                    foreach (var fieldID in ruleRow.Fields)
                    {
                        if (isHidden)
                        {
                            settings.HiddenFields.Add(fieldID);
                        }
                        else
                        {
                            settings.VisibleFields.Add(fieldID);
                        }

                        if (accessSettings == KrPermissionsHelper.AccessSettings.AllowEdit)
                        {
                            settings.AllowedFields.Add(fieldID);
                        }
                        else if (accessSettings == KrPermissionsHelper.AccessSettings.DisallowEdit)
                        {
                            settings.DisallowedFields.Add(fieldID);
                        }
                    }
                }
                else
                {
                    if (isHidden)
                    {
                        settings.IsHidden = true;
                    }
                    else
                    {
                        settings.IsVisible = true;
                    }

                    switch (accessSettings)
                    {
                        case KrPermissionsHelper.AccessSettings.AllowEdit:
                            settings.IsAllowed = true;
                            break;
                        case KrPermissionsHelper.AccessSettings.DisallowEdit:
                            settings.IsDisallowed = true;
                            settings.DisallowRowAdding = true;
                            settings.DisallowRowDeleting = true;
                            break;
                        case KrPermissionsHelper.AccessSettings.DisallowRowAdding:
                            settings.DisallowRowAdding = true;
                            break;
                        case KrPermissionsHelper.AccessSettings.DisallowRowDeleting:
                            settings.DisallowRowDeleting = true;
                            break;
                        case KrPermissionsHelper.AccessSettings.DisallowRowEdit:
                            settings.IsDisallowed = true;
                            break;
                    }
                }

                foreach (var typeID in ruleRow.Types)
                {
                    if (!permissionRuleSettings.TaskSettingsByTypes.TryGetValue(typeID, out var typeSettings))
                    {
                        typeSettings = new HashSet<Guid, KrPermissionSectionSettings>(x => x.ID);
                        permissionRuleSettings.TaskSettingsByTypes[typeID] = typeSettings;
                    }

                    if (!typeSettings.TryGetItem(ruleRow.SectionID, out var existingSettings))
                    {
                        typeSettings.Add(settings.Clone());
                    }
                    else
                    {
                        existingSettings.MergeWith(settings);
                    }
                }

                foreach (var typeSettings in permissionRuleSettings.TaskSettingsByTypes.Values)
                {
                    foreach (var sectionSettings in typeSettings)
                    {
                        sectionSettings.Clean();
                    }
                }
            }
        }

        private async Task FillPermissionCardExtendedSettingsAsync(
            DbManager db,
            Dictionary<Guid, IKrPermissionRuleSettings> permissionsByID,
            CancellationToken cancellationToken = default)
        {
            db.SetCommand(
                    this.dbScope.BuilderFactory
                        .Select()
                        .C("s", "ID", "SectionID", "IsHidden", "AccessSettingID", "Mask") // 0 - 4
                        .C("f", "FieldID") // 5
                        .From("KrPermissionExtendedCardRules", "s").NoLock()
                        .LeftJoin("KrPermissionExtendedCardRuleFields", "f").NoLock()
                        .On().C("f", "RuleRowID").Equals().C("s", "RowID")
                        .OrderBy("s", "ID").By("s", "SectionID")
                        .Build())
                .LogCommand();

            await using var reader = await db.ExecuteReaderAsync(cancellationToken);
            var prevRuleID = Guid.Empty;
            IKrPermissionRuleSettings prevRule = null;

            var prevSectionID = Guid.Empty;
            KrPermissionSectionSettings prevSection = null;

            while (await reader.ReadAsync(cancellationToken))
            {
                var ruleID = reader.GetValue<Guid>(0);
                if (ruleID != prevRuleID)
                {
                    // При смене правила сбрасываем секцию.
                    prevRuleID = ruleID;
                    prevSectionID = Guid.Empty;
                    if (!permissionsByID.TryGetValue(ruleID, out prevRule))
                    {
                        continue;
                    }
                }

                // Правило, для которого актуальны данные настройки, отключено.
                if (prevRule is null)
                {
                    continue;
                }

                var sectionID = reader.GetValue<Guid>(1);
                if (sectionID != prevSectionID)
                {
                    prevSection?.Clean();

                    prevSectionID = sectionID;
                    prevSection = new KrPermissionSectionSettings
                    {
                        ID = sectionID,
                    };
                    prevRule.CardSettings.Add(prevSection);
                }

                var isHidden = reader.GetValue<bool>(2);
                var accessSettings = reader.GetValue<int?>(3);
                var mask = reader.GetValue<string>(4);
                var fieldID = reader.GetValue<Guid?>(5);

                if (fieldID.HasValue)
                {
                    if (isHidden)
                    {
                        prevSection.HiddenFields.Add(fieldID.Value);
                    }
                    else
                    {
                        prevSection.VisibleFields.Add(fieldID.Value);
                    }

                    if (accessSettings == KrPermissionsHelper.AccessSettings.AllowEdit)
                    {
                        prevSection.AllowedFields.Add(fieldID.Value);
                    }
                    else if (accessSettings == KrPermissionsHelper.AccessSettings.DisallowEdit)
                    {
                        prevSection.DisallowedFields.Add(fieldID.Value);
                    }
                    else if (accessSettings == KrPermissionsHelper.AccessSettings.MaskData)
                    {
                        prevSection.DisallowedFields.Add(fieldID.Value);
                        prevSection.MaskedFields.Add(fieldID.Value);
                        prevSection.MaskedFieldsData[fieldID.Value] = mask;
                    }
                }
                else
                {
                    if (isHidden)
                    {
                        prevSection.IsHidden = true;
                    }
                    else
                    {
                        prevSection.IsVisible = true;
                    }

                    switch (accessSettings)
                    {
                        case KrPermissionsHelper.AccessSettings.AllowEdit:
                            prevSection.IsAllowed = true;
                            break;
                        case KrPermissionsHelper.AccessSettings.DisallowEdit:
                            prevSection.IsDisallowed = true;
                            prevSection.DisallowRowAdding = true;
                            prevSection.DisallowRowDeleting = true;
                            break;
                        case KrPermissionsHelper.AccessSettings.DisallowRowAdding:
                            prevSection.DisallowRowAdding = true;
                            break;
                        case KrPermissionsHelper.AccessSettings.DisallowRowDeleting:
                            prevSection.DisallowRowDeleting = true;
                            break;
                        case KrPermissionsHelper.AccessSettings.MaskData:
                            prevSection.IsMasked = true;
                            prevSection.IsDisallowed = true;
                            prevSection.DisallowRowAdding = true;
                            prevSection.DisallowRowDeleting = true;
                            prevSection.Mask = mask;
                            break;
                        case KrPermissionsHelper.AccessSettings.DisallowRowEdit:
                            prevSection.IsDisallowed = true;
                            break;
                    }
                }
            }

            prevSection?.Clean();
        }

        private async Task FillPermissionContextRolesAsync(
            DbManager db,
            Dictionary<Guid, IKrPermissionRuleSettings> permissionsByID,
            CancellationToken cancellationToken)
        {
            var builderFactory = this.dbScope.BuilderFactory;

            db.SetCommand(
                    builderFactory
                        .Select().C(null, "ID", "RoleID")
                        .From("KrPermissionRoles").NoLock()
                        .Where().C("IsContext").Equals().V(true)
                        .OrderBy("ID").By("RoleID") // сортировка не стоит ничего из-за индекса ndx_KrPermissionRoles_IDRoleIDIsContext
                        .Build())
                .LogCommand();

            await using var reader = await db.ExecuteReaderAsync(cancellationToken);
            var prevRuleID = Guid.Empty;
            IKrPermissionRuleSettings prevRule = null;

            while (await reader.ReadAsync(cancellationToken))
            {
                var ruleID = reader.GetValue<Guid>(0);
                if (ruleID != prevRuleID)
                {
                    prevRuleID = ruleID;
                    if (!permissionsByID.TryGetValue(ruleID, out prevRule))
                    {
                        continue;
                    }
                }

                prevRule?.ContextRoles.Add(reader.GetValue<Guid>(1));
            }
        }

        private async Task FillPermissionMainAsync(
            DbManager db,
            Dictionary<Guid, IKrPermissionRuleSettings> permissionsByID,
            CancellationToken cancellationToken = default)
        {
            var builder = this.dbScope.BuilderFactory
                .Select()
                .C("p", "ID", "Conditions", "IsExtended", "IsRequired", "Priority"); // 0 - 4

            foreach (var flag in KrPermissionFlagDescriptors.Full.IncludedPermissions)
            {
                if (flag.IsVirtual)
                {
                    continue;
                }

                builder.C(flag.SqlName); // 4 - ...
            }

            builder
                .From("KrPermissions", "p").NoLock()
                .Where().C("p", "IsDisabled").Equals().V(false);

            db.SetCommand(builder.Build())
                .LogCommand();

            await using var reader = await db.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetValue<Guid>(0);
                var permission = new KrPermissionRuleSettings(
                    id,
                    reader.GetValue<string>(1),
                    reader.GetValue<bool>(2),
                    reader.GetValue<bool>(3),
                    reader.GetValue<int>(4));

                var i = 5; // Список колонок флагов начинается с 5
                foreach (var flag in KrPermissionFlagDescriptors.Full.IncludedPermissions)
                {
                    if (flag.IsVirtual)
                    {
                        continue;
                    }

                    if (reader.GetValue<bool>(i++))
                    {
                        permission.Flags.Add(flag);
                    }
                }

                permissionsByID[id] = permission;
            }
        }

        private async Task FillPermissionHashAsync<T>(
            DbManager db,
            string section,
            string field,
            IDictionary<Guid, IKrPermissionRuleSettings> permissionsByID,
            Action<IKrPermissionRuleSettings, T> setAction,
            CancellationToken cancellationToken = default)
        {
            db.SetCommand(
                    this.dbScope.BuilderFactory
                        .Select()
                        .C(null, "ID", field)
                        .From(section).NoLock()
                        .OrderBy("ID")
                        .Build())
                .LogCommand();

            await using var reader = await db.ExecuteReaderAsync(cancellationToken);
            var prevPermissionID = Guid.Empty;
            IKrPermissionRuleSettings permission = null;
            while (await reader.ReadAsync(cancellationToken))
            {
                var permissionID = reader.GetValue<Guid>(0);
                if (permissionID != prevPermissionID)
                {
                    prevPermissionID = permissionID;
                    if (!permissionsByID.TryGetValue(permissionID, out permission))
                    {
                        continue;
                    }
                }

                if (permission is not null)
                {
                    var value = reader.GetValue<T>(1);
                    setAction(permission, value);
                }
            }
        }

        private async void InvalidateCacheIfRequiredAsync(object sender, DeferredEventArgs<RefGroupsGlobalCachePayload> e)
        {
            var deferral = e.Defer();
            try
            {
                var groupTypeIds = e.Value.Keys;

                // Если сброшен кэш типа групп "Состояние документа" или "Тип документа", то нужно сбросить кэш.
                if (groupTypeIds is null ||
                    groupTypeIds.Contains(RefGroupsHelper.DocumentTypeRefGroupTypeID) ||
                    groupTypeIds.Contains(KrRefGroupsHelper.KrStateRefGroupTypeID) ||
                    groupTypeIds.Contains(RefGroupsHelper.TaskTypeRefGroupTypeID))
                {
                    await this.UpdateVersionCoreAsync(this.localCache, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                deferral.SetException(ex);
            }
            finally
            {
                deferral.Dispose();
            }
        }

        private async Task UpdateVersionCoreAsync(ICardCache cache, CancellationToken cancellationToken = default)
        {
            using (await this.asyncLock.EnterAsync(cancellationToken))
            {
                await this.IncreaseVersionAsync(cancellationToken);
                await cache.Settings.InvalidateAsync(VersionKey);
                await cache.Settings.InvalidateAsync(CacheKey);
            }
        }

        #endregion
    }
}
