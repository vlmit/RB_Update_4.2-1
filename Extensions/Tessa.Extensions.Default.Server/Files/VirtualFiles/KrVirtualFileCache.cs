#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Server.RefGroups;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.FileConverters;
using Tessa.Files;
using Tessa.Platform;
using Tessa.Platform.Conditions;
using Tessa.Platform.Data;
using Tessa.Platform.IO;
using Tessa.Platform.RefGroups;
using Tessa.Platform.RefGroups.Caching;
using Tessa.Platform.Storage;
using Unity;

namespace Tessa.Extensions.Default.Server.Files.VirtualFiles
{
    /// <inheritdoc cref="IKrVirtualFileCache"/>
    public sealed class KrVirtualFileCache :
        IKrVirtualFileCache,
        IDisposable
    {
        #region Nested Types

        private class Cache
        {
            private readonly Dictionary<Guid, IKrVirtualFile> itemsByKey;

            public Cache(IKrVirtualFile[] items)
            {
                this.Items = items;
                this.itemsByKey = items.ToDictionary(static x => x.ID);
            }

            public IKrVirtualFile[] Items { get; }

            public bool TryGet(Guid id, [MaybeNullWhen(false)] out IKrVirtualFile result) =>
                this.itemsByKey.TryGetValue(id, out result);
        }

        #endregion

        #region Fields

        private readonly ICardCache cache;
        private readonly ICardCache localCache;
        private readonly IDbScope dbScope;
        private readonly ISeparateDbConnectionWorker worker;
        private readonly IRefGroupsManager refGroupsManager;
        private readonly RefGroupsGlobalCache refGroupsGlobalCache;
        private readonly AsyncLock asyncLock = new();

        private const string CacheKey = StorageHelper.SystemKeyPrefix + nameof(KrVirtualFileCache);
        private const string TypesCacheKey = CacheKey + "Types";

        #endregion

        #region Constructors

        public KrVirtualFileCache(
            ICardCache cache,
            [Dependency(CardCacheNames.Local)] ICardCache localCache,
            IDbScope dbScope,
            ISeparateDbConnectionWorker worker,
            IRefGroupsManager refGroupsManager,
            RefGroupsGlobalCache refGroupsGlobalCache,
            [OptionalDependency] IUnityDisposableContainer? container = null)
        {
            this.cache = NotNullOrThrow(cache);
            this.localCache = NotNullOrThrow(localCache);
            this.dbScope = NotNullOrThrow(dbScope);
            this.worker = NotNullOrThrow(worker);
            this.refGroupsManager = NotNullOrThrow(refGroupsManager);
            this.refGroupsGlobalCache = NotNullOrThrow(refGroupsGlobalCache);

            container?.Register(this);

            // TODO async global cache invalidation
            var valueTask = refGroupsGlobalCache.AddInvalidatedHandlerAsync(this.InvalidateCacheIfRequiredAsync);
            if (!valueTask.IsCompletedSuccessfully)
            {
                valueTask.AsTask().GetAwaiter().GetResult();
            }
        }

        #endregion

        #region IKrVirtualFileCache Implementation

        /// <inheritdoc />
        public async ValueTask<IKrVirtualFile?> TryGetAsync(Guid virtualFileID, CancellationToken cancellationToken = default)
        {
            (await this.cache.Settings.GetAsync(CacheKey, this.InitCacheAsync, cancellationToken))
                .TryGet(virtualFileID, out var result);
            return result;
        }

        /// <inheritdoc />
        public async ValueTask<IKrVirtualFile[]> GetAllAsync(CancellationToken cancellationToken = default) =>
            (await this.cache.Settings.GetAsync(CacheKey, this.InitCacheAsync, cancellationToken))
            .Items;

        /// <inheritdoc />
        public ValueTask<IList<Guid>> GetAllowedTypesAsync(CancellationToken cancellationToken = default) =>
            this.cache.Settings.GetAsync(TypesCacheKey, this.InitTypesAsync, cancellationToken);

        /// <inheritdoc />
        public Task InvalidateAsync() =>
            this.InvalidateCoreAsync(this.cache);

        #endregion

        #region Private Methods

        private async Task<IList<Guid>> InitTypesAsync(string arg, CancellationToken cancellationToken = default)
        {
            var cardTypesGroups = await this.refGroupsManager.GetGroupsByTypeAsync(RefGroupsHelper.CardTypeRefGroupTypeID, cancellationToken);
            var result = new HashSet<Guid>();

            using (await this.asyncLock.EnterAsync(cancellationToken))
            await using (this.worker.CreateScope())
            {
                var db = this.dbScope.Db;
                var builder = this.dbScope.BuilderFactory;

                var typeIds = await db.SetCommand(
                        builder
                            .SelectDistinct()
                            .C("vft", "TypeID")
                            .From("KrVirtualFileCardTypes", "vft").NoLock()
                            .Build())
                    .LogCommand()
                    .ExecuteListAsync<Guid>(cancellationToken);

                foreach (var typeId in typeIds)
                {
                    var group = cardTypesGroups.FirstOrDefault(x => x.ID == typeId);
                    if (group is null)
                    {
                        result.Add(typeId);
                    }
                    else
                    {
                        foreach (var groupItem in group.CalculatedValues!)
                        {
                            var type = (Guid) groupItem.ID;
                            result.Add(type);
                        }
                    }
                }
            }

            return result.ToList();
        }

        private async Task<Cache> InitCacheAsync(string arg, CancellationToken cancellationToken = default)
        {
            using (await this.asyncLock.EnterAsync(cancellationToken))
            {
                var documentStatesGroups = await this.refGroupsManager.GetGroupsByTypeAsync(KrRefGroupsHelper.KrStateRefGroupTypeID, cancellationToken);
                var cardTypesGroups = await this.refGroupsManager.GetGroupsByTypeAsync(RefGroupsHelper.CardTypeRefGroupTypeID, cancellationToken);
                await using (this.worker.CreateScope())
                {
                    var db = this.dbScope.Db;
                    var builder = this.dbScope.BuilderFactory;

                    db.SetCommand(
                            builder
                                .Select()
                                .C("vf", "ID", "FileVersionID", "FileTemplateID", "FileName", "FileCategoryID", "FileCategoryName", "Conditions", "InitializationScenario") // 0 - 7
                                .C("f", "Name") // 8
                                .C("vfv", "RowID", "FileTemplateID", "FileName") // 9 - 11
                                .C("ff", "Name") // 12
                                .C("ft", "ConvertToPDF") // 13
                                .From("KrVirtualFiles", "vf").NoLock()
                                .LeftJoin("KrVirtualFileVersions", "vfv").NoLock()
                                .On().C("vf", "ID").Equals().C("vfv", "ID")
                                .InnerJoin("Files", "f").NoLock()
                                .On().C("f", "ID").Equals().C("vf", "FileTemplateID")
                                .InnerJoin("FileTemplates", "ft").NoLock()
                                .On().C("ft", "ID").Equals().C("vf", "FileTemplateID")
                                .LeftJoin("Files", "ff").NoLock()
                                .On().C("ff", "ID").Equals().C("vfv", "FileTemplateID")
                                .OrderBy("vf", "ID").By("vfv", "Order", SortOrder.Ascending)
                                .Build())
                        .LogCommand();

                    var items = new List<IKrVirtualFile>();
                    await using (var reader = await db.ExecuteReaderAsync(cancellationToken))
                    {
                        var prevFileID = Guid.Empty;
                        IKrVirtualFile? prevFile = null;
                        IKrVirtualFileVersion? mainVersion = null;
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            var currentFileID = reader.GetGuid(0);
                            var currentVersionID = reader.GetGuid(1);
                            var fileTemplateID = reader.GetGuid(2);
                            var fileName = reader.GetNullableString(3);
                            var fileCategoryID = reader.GetNullableGuid(4);
                            var fileCategoryName = reader.GetNullableString(5);
                            var conditions = reader.GetNullableString(6);
                            var initializationScenario = reader.GetNullableString(7);
                            var templateName = reader.GetNullableString(8);
                            var additionalVersionID = reader.GetNullableGuid(9);
                            var additionalVersionTemplateID = reader.GetNullableGuid(10);
                            var additionalFileName = reader.GetNullableString(11);
                            var additionalTemplateName = reader.GetNullableString(12);
                            var convertToPdf = reader.GetBoolean(13);

                            if (currentFileID != prevFileID)
                            {
                                prevFile?.Versions.Add(mainVersion);
                                prevFileID = currentFileID;

                                if (string.IsNullOrEmpty(fileName))
                                {
                                    fileName = templateName;
                                }
                                else
                                {
                                    fileName += FileHelper.GetExtension(templateName);
                                }

                                var pdfConvertSupported = FileConverterFormat.IsSupportedConversion(
                                    FileConverterFormat.Pdf,
                                    FileHelper.GetExtension(templateName)?.TrimStart('.'));

                                if (pdfConvertSupported && convertToPdf)
                                {
                                    fileName = Path.ChangeExtension(fileName, ".pdf");
                                }

                                prevFile =
                                    new KrVirtualFile
                                    {
                                        ID = currentFileID,
                                        Name = fileName,
                                        FileCategory = fileCategoryID.HasValue
                                            ? new FileCategory(fileCategoryID, fileCategoryName!)
                                            : null,
                                        Conditions = string.IsNullOrEmpty(conditions)
                                            ? null
                                            : ConditionSettings.GetFromList(StorageHelper.DeserializeListFromTypedJson(conditions)),
                                        InitializationScenario = initializationScenario,
                                    };

                                mainVersion =
                                    new KrVirtualFileVersion
                                    {
                                        ID = currentVersionID,
                                        FileTemplateID = fileTemplateID,
                                        Name = fileName,
                                    };

                                items.Add(prevFile);
                            }

                            if (additionalVersionID.HasValue
                                && additionalVersionTemplateID.HasValue)
                            {
                                if (string.IsNullOrEmpty(additionalFileName))
                                {
                                    additionalFileName = additionalTemplateName;
                                }
                                else
                                {
                                    additionalFileName += FileHelper.GetExtension(additionalTemplateName);
                                }

                                prevFile?.Versions.Add(
                                    new KrVirtualFileVersion
                                    {
                                        ID = additionalVersionID.Value,
                                        FileTemplateID = additionalVersionTemplateID.Value,
                                        Name = additionalFileName,
                                    });
                            }
                        }

                        prevFile?.Versions.Add(mainVersion);
                    }

                    db.SetCommand(
                            builder
                                .Select().C(null, "ID", "TypeID")
                                .From("KrVirtualFileCardTypes").NoLock()
                                .Build())
                        .LogCommand();

                    await using (var reader = await db.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            var id = reader.GetGuid(0);
                            var virtualFile = items.FirstOrDefault(x => x.ID == id);

                            if (virtualFile is null)
                            {
                                continue;
                            }

                            var typeId = reader.GetGuid(1);
                            var group = cardTypesGroups.FirstOrDefault(x => x.ID == typeId);

                            if (group is null)
                            {
                                virtualFile.Types.Add(typeId);
                            }
                            else
                            {
                                foreach (var groupItem in group.CalculatedValues!)
                                {
                                    var type = (Guid) groupItem.ID;
                                    virtualFile.Types.Add(type);
                                }
                            }
                        }
                    }

                    db.SetCommand(
                            builder
                                .Select().C(null, "ID", "StateID")
                                .From("KrVirtualFileStates").NoLock()
                                .Build())
                        .LogCommand();

                    await using (var reader = await db.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            var id = reader.GetGuid(0);
                            var virtualFile = items.FirstOrDefault(x => x.ID == id);

                            if (virtualFile is null)
                            {
                                continue;
                            }

                            var stateId = reader.GetInt16(1);
                            var group = documentStatesGroups.FirstOrDefault(x => x.IntID == stateId);

                            if (group is null)
                            {
                                virtualFile.DocumentStates.Add((KrState) stateId);
                            }
                            else
                            {
                                foreach (var groupItem in group.CalculatedValues!)
                                {
                                    var krState = (KrState) (int) groupItem.ID;
                                    virtualFile.DocumentStates.Add(krState);
                                }
                            }
                        }
                    }

                    db.SetCommand(
                            builder
                                .Select().C(null, "ID", "RoleID")
                                .From("KrVirtualFileRoles").NoLock()
                                .Build())
                        .LogCommand();

                    await using (var reader = await db.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            var id = reader.GetGuid(0);
                            var virtualFile = items.FirstOrDefault(x => x.ID == id);

                            if (virtualFile is null)
                            {
                                continue;
                            }

                            var roleId = reader.GetGuid(1);
                            virtualFile.Roles.Add(roleId);
                        }
                    }

                    return new Cache(items.ToArray());
                }
            }
        }

        // ReSharper disable once AsyncVoidMethod
        private async void InvalidateCacheIfRequiredAsync(object? sender, DeferredEventArgs<RefGroupsGlobalCachePayload> e)
        {
            var deferral = e.Defer();
            try
            {
                var groupTypeIds = e.Value.Keys;

                // Если сброшен кэш типа групп "Состояние документа" или "Тип документа", то нужно сбросить кэш виртуальных файлов.
                if (groupTypeIds is null ||
                    groupTypeIds.Contains(RefGroupsHelper.CardTypeRefGroupTypeID) ||
                    groupTypeIds.Contains(KrRefGroupsHelper.KrStateRefGroupTypeID))
                {
                    await this.InvalidateCoreAsync(this.localCache);
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

        private async Task InvalidateCoreAsync(ICardCache cache)
        {
            using (await this.asyncLock.EnterAsync())
            {
                await cache.Settings.InvalidateAsync(CacheKey);
                await cache.Settings.InvalidateAsync(TypesCacheKey);
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            this.refGroupsGlobalCache.RemoveInvalidatedHandler(this.InvalidateCacheIfRequiredAsync);
            this.asyncLock.Dispose();
        }

        #endregion
    }
}
