#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.IO;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions.Files
{
    /// <summary>
    /// Правило для проверки доступа к файлу по расширенным настройкам карточки "Правило доступа".
    /// </summary>
    public sealed class KrPermissionsFileRule : IKrPermissionsFileRule
    {
        #region Fields

        private readonly IDbScope dbScope;
        private readonly IKrFileOwnershipChecker krFileOwnershipChecker;

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его свойств.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="krFileOwnershipChecker"><inheritdoc cref="IKrFileOwnershipChecker" path="/summary"/></param>
        /// <param name="extensions">
        /// <inheritdoc cref="Extensions" path="/summary"/>
        /// Если задан пустой список или <c>null</c>, то правило не выполняет фильтрацию по расширениям.
        /// </param>
        /// <param name="fileCategories">
        /// <inheritdoc cref="Categories" path="/summary"/>
        /// Если задан пустой список или <c>null</c>, то правило не выполняет фильтрацию по категории файла.
        /// </param>
        public KrPermissionsFileRule(
            IDbScope dbScope,
            IKrFileOwnershipChecker krFileOwnershipChecker,
            IEnumerable<string>? extensions,
            IEnumerable<Guid>? fileCategories)
        {
            this.dbScope = NotNullOrThrow(dbScope);
            this.krFileOwnershipChecker = NotNullOrThrow(krFileOwnershipChecker);

            this.Extensions = extensions is null
                ? ImmutableList<string>.Empty
                : extensions.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
            this.Categories = fileCategories is null
                ? ImmutableList<Guid>.Empty
                : fileCategories.ToImmutableHashSet();
        }

        /// <summary>
        /// Создаёт экземпляр класса с указанием его свойств.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="krFileOwnershipChecker"><inheritdoc cref="IKrFileOwnershipChecker" path="/summary"/></param>
        /// <param name="extensions">
        /// Строка, содержащая список расширений, разделённых пробелом.
        /// Если задана пустая строка или <c>null</c>, то правило не выполняет фильтрацию по расширениям.
        /// </param>
        /// <param name="fileCategories">
        /// <inheritdoc cref="Categories" path="/summary"/>
        /// Если задан пустой список или <c>null</c>, то правило не выполняет фильтрацию по категории файла.
        /// </param>
        public KrPermissionsFileRule(
            IDbScope dbScope,
            IKrFileOwnershipChecker krFileOwnershipChecker,
            string? extensions,
            IEnumerable<Guid>? fileCategories)
            : this(dbScope, krFileOwnershipChecker, ParseExtensions(extensions), fileCategories)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Список расширений файлов, для которых выполняется данное правило.
        /// </summary>
        public ICollection<string> Extensions { get; }

        /// <summary>
        /// Список категорий файлов, для которых выполняется данное правило.
        /// </summary>
        public ICollection<Guid> Categories { get; }

        /// <summary>
        /// Флаг определяет, что правило также должно выполняться для собственных файлов пользователя.
        /// </summary>
        public int FileCheckRule { get; init; }

        /// <summary>
        /// Ограничение на максимальное количество файлов.
        /// </summary>
        public int? MaxCount { get; init; }

        /// <summary>
        /// Флаг "Обязательный файл".
        /// </summary>
        public bool Mandatory { get; init; }

        /// <summary>
        /// Виртуальный идентификатор правила.
        /// </summary>
        public Guid VirtualID { get; init; } = Guid.NewGuid();

        #endregion

        #region IKrPermissionFileRule Properties

        /// <inheritdoc/>
        public int Priority { get; init; }

        /// <inheritdoc/>
        public int? AddAccessSetting { get; init; }

        /// <inheritdoc/>
        public int? ReadAccessSetting { get; init; }

        /// <inheritdoc/>
        public int? EditAccessSetting { get; init; }

        /// <inheritdoc/>
        public int? DeleteAccessSetting { get; init; }

        /// <inheritdoc/>
        public int? SignAccessSetting { get; init; }

        /// <summary>
        /// Настройка доступа на создание временной ссылки для файла или <c>null</c>, если правило не определяет данный доступ.
        /// </summary>
        public int? CreateLinkAccessSetting { get; init; }

        /// <inheritdoc/>
        public long? FileSizeLimit { get; init; }

        #endregion

        #region IKrPermissionFileRule Methods

        /// <inheritdoc/>
        public ValueTask<bool> CheckFileAsync(IKrPermissionsFilesManagerContext context)
        {
            ThrowIfNull(context);

            return context.File is not null
                ? this.CheckWithFileAsync(context)
                : this.CheckWithFileIDAsync(context);
        }

        #endregion

        #region Private Methods

        private async ValueTask<bool> CheckWithFileAsync(IKrPermissionsFilesManagerContext context)
        {
            ThrowIfNull(context.File);

            Guid? categoryID;
            string extension;
            Func<CancellationToken, ValueTask<bool>> isOwnFileAsync;

            // Если это не серверная операция, то не доверяем данным объекта файлов. 
            if (context.ServerOperation
                || context.File.State is CardFileState.Inserted)
            {
                isOwnFileAsync = (ct) => this.krFileOwnershipChecker.IsOwnAsync(
                    context.File.Card.CreatedByID,
                    context.Session.User.ID,
                    true,
                    card: context.Card,
                    cardId: context.CardId,
                    cardDocTypeId: context.DocTypeId,
                    cacheHolder: context.Info,
                    cancellationToken: ct);
                categoryID = GetCategoryID(context.File.CategoryID, context.File.CategoryCaption);
                extension = FileHelper.GetExtension(context.File.Name).TrimStart('.');
                context.FileInfo["FileExtension"] = extension;
            }
            else
            {
                (_, isOwnFileAsync, categoryID, extension) = await this.GetFileDataAsync(context);

                if (context.StoreFile is not null)
                {
                    if (context.StoreFile.Flags.Has(CardFileFlags.UpdateCategory))
                    {
                        categoryID = GetCategoryID(context.File.CategoryID, context.File.CategoryCaption);
                    }

                    if (context.StoreFile.Flags.Has(CardFileFlags.UpdateName))
                    {
                        extension = FileHelper.GetExtension(context.StoreFile.Name).TrimStart('.');
                    }
                }
            }

            // При проверке категории:
            // 1) Вариант категории "Без категории" рассчитываем как вариант с идентификатором Guid.Empty.
            // 2) Вариант категории, введённый вручную (идентификатор категории null) подходит под правила только при наличии всех категорий
            //    и в случае проверки на добавление с запретом добавления. Запрещено добавлять вручную введённые категории, если есть запрет добавления хотя бы на одну категорию.
            var result =
                await CheckFileCheckRuleAsync(this.FileCheckRule, isOwnFileAsync, context.CancellationToken)
                && (this.Extensions.Count == 0
                    || this.Extensions.Contains(extension))
                && (this.Categories.Count == 0
                    || (categoryID is null
                        && context.RequiredAccessFlags.Has(KrPermissionsFileAccessSettingFlag.Add)
                        && this.AddAccessSetting == KrPermissionsHelper.FileEditAccessSettings.Disallowed)
                    || (categoryID is not null && this.Categories.Contains(categoryID.Value)));

            return result;
        }

        private async ValueTask<bool> CheckWithFileIDAsync(IKrPermissionsFilesManagerContext context)
        {
            var (fileExists, isOwnFileAsync, categoryID, extension) = await this.GetFileDataAsync(context);

            return fileExists
                && await CheckFileCheckRuleAsync(this.FileCheckRule, isOwnFileAsync, context.CancellationToken)
                && (this.Extensions.Count == 0
                    || this.Extensions.Contains(extension))
                && (this.Categories.Count == 0
                    || (categoryID is null
                        && context.RequiredAccessFlags.Has(KrPermissionsFileAccessSettingFlag.Add)
                        && this.AddAccessSetting == KrPermissionsHelper.FileEditAccessSettings.Disallowed)
                    || (categoryID is not null && this.Categories.Contains(categoryID.Value)));
        }

        private async Task<(bool FileExists, Func<CancellationToken, ValueTask<bool>> IsOwnFileAsync, Guid? CategoryID, string Extension)> GetFileDataAsync(IKrPermissionsFilesManagerContext context)
        {
            var fileInfo = context.FileInfo;
            if (fileInfo.TryGetValue("FileExists", out var fileExistsObj)
                && fileExistsObj is bool fileExists)
            {
                return (fileExists, fileInfo.TryGet<Func<CancellationToken, ValueTask<bool>>>("IsOwnFile")!, fileInfo.TryGet<Guid?>("CategoryID"), fileInfo.TryGet<string>("FileExtension") ?? string.Empty);
            }

            string? name = null;
            Guid? createdById = null;
            Guid? categoryID = null;
            string? categoryCaption = null;

            await using (this.dbScope.Create())
            {
                var db = this.dbScope.Db;
                var builder =
                    this.dbScope.BuilderFactory
                        .Select().Top(1)
                        .C("f", "Name", "CreatedByID", "CategoryID", "CategoryCaption")
                        .From("Files", "f").NoLock()
                        .Where().C("f", "RowID").Equals().P("FileID");

                db.SetCommand(
                        builder.Limit(1).Build(),
                        db.Parameter("FileID", context.FileID, LinqToDB.DataType.Guid))
                    .LogCommand();

                await using var reader = await db.ExecuteReaderAsync(context.CancellationToken);
                if (await reader.ReadAsync(context.CancellationToken))
                {
                    name = reader.GetString(0);
                    createdById = reader.GetGuid(1);
                    categoryID = reader.GetNullableGuid(2);
                    categoryCaption = reader.GetNullableString(3);
                }
            }

            if (createdById.HasValue)
            {
                var extension = FileHelper.GetExtension(name!).TrimStart('.').ToLowerInvariant();
                Func<CancellationToken, ValueTask<bool>> isOwnFileAsync = (ct) => this.krFileOwnershipChecker.IsOwnAsync(
                    createdById.Value,
                    context.Session.User.ID,
                    checkDeputized: true,
                    card: context.Card,
                    cardId: context.CardId,
                    cardDocTypeId: context.DocTypeId,
                    cacheHolder: context.Info,
                    cancellationToken: ct);

                categoryID = GetCategoryID(categoryID, categoryCaption);

                fileInfo["FileExists"] = BooleanBoxes.True;
                fileInfo["IsOwnFile"] = isOwnFileAsync;
                fileInfo["CategoryID"] = categoryID;
                fileInfo["FileExtension"] = extension;

                return (true, isOwnFileAsync, categoryID, extension);
            }

            fileInfo["FileExists"] = BooleanBoxes.False;
            return default;
        }

        private static async ValueTask<bool> CheckFileCheckRuleAsync(
            int fileCheckRule,
            Func<CancellationToken, ValueTask<bool>> isOwnFileAsync,
            CancellationToken cancellationToken)
        {
            return fileCheckRule switch
            {
                KrPermissionsHelper.FileCheckRules.AllFiles => true,
                KrPermissionsHelper.FileCheckRules.FilesOfOtherUsers => !await isOwnFileAsync(cancellationToken),
                KrPermissionsHelper.FileCheckRules.OwnFiles => await isOwnFileAsync(cancellationToken),
                _ => false,
            };
        }

        private static IReadOnlyCollection<string>? ParseExtensions(string? extensions)
        {
            if (!string.IsNullOrWhiteSpace(extensions))
            {
                var extensionsHash = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var extension in extensions.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    extensionsHash.Add(extension);
                }

                return extensionsHash;
            }

            return null;
        }

        private static Guid? GetCategoryID(Guid? categoryID, string? categoryCaption)
        {
            return categoryCaption is null
                ? KrPermissionsHelper.NoCategoryFilesCategoryID
                : categoryID;
        }

        #endregion
    }
}
