#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Platform.Data;
using Tessa.Platform.IO;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Tessa.Roles.Deputies;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions.Files
{
    /// <inheritdoc cref="IKrPermissionsFilesManager"/>
    public sealed class KrPermissionsFilesManager : IKrPermissionsFilesManager
    {
        #region Fields

        private readonly IDbScope dbScope;
        private readonly IDeputiesManagementSettingsProvider deputiesProvider;

        #endregion

        #region Constructors

        public KrPermissionsFilesManager(
            IDbScope dbScope,
            IDeputiesManagementSettingsProvider deputiesProvider)
        {
            this.dbScope = NotNullOrThrow(dbScope);
            this.deputiesProvider = NotNullOrThrow(deputiesProvider);
        }

        #endregion

        #region IKrPermissionsFilesManager Implementation

        /// <inheritdoc/>
        public async ValueTask<IKrPermissionsFilesManagerResult> CheckPermissionsAsync(IKrPermissionsFilesManagerContext context)
        {
            var result = new KrPermissionsFilesManagerResult();

            var stillRequired = ParseAccessFlags(context.RequiredAccessFlags);

            if (stillRequired.Count == 0
                || context.OrderedRules.Count == 0)
            {
                return result;
            }

            var withReplace = await this.HasCheckAttributeChangesAsync(context);
            if (withReplace)
            {
                stillRequired.Remove(KrPermissionsFileAccessSettingFlag.Sign);
            }

            await CheckRulesAsync(
                context,
                result,
                stillRequired);

            if (withReplace
                && context.StoreFile is not null
                && context.RequiredAccessFlags.HasAny(KrPermissionsFileAccessSettingFlag.Edit | KrPermissionsFileAccessSettingFlag.Sign)
                && result.AccessSettings.All(x => x.Value != KrPermissionsHelper.FileEditAccessSettings.Disallowed))
            {
                var newRequiredFlag = context.RequiredAccessFlags;
                if (context.RequiredAccessFlags.Has(KrPermissionsFileAccessSettingFlag.Edit))
                {
                    newRequiredFlag |= KrPermissionsFileAccessSettingFlag.Add;
                    newRequiredFlag &= ~KrPermissionsFileAccessSettingFlag.Edit;
                }

                context.SetFile(context.StoreFile, context.VersionID, context.StoreFile, newRequiredFlag);
                result.FileSizeLimit = null;

                await CheckRulesAsync(
                    context,
                    result,
                    ParseAccessFlags(newRequiredFlag));
            }

            if (context.WriteValidationResult)
            {
                result.ValidationResult = await this.GetValidationResultAsync(context, result, withReplace);
            }

            return result;
        }

        /// <inheritdoc/>
        public async ValueTask<ValidationResult> GetValidationResultAsync(
            IKrPermissionsFilesManagerContext context,
            IKrPermissionsFilesManagerResult result,
            bool isReplace = false,
            bool isCopy = false)
        {
            ValidationResultBuilder? validationResult = null;
            string? fileName = null;

            foreach (var (accessFlag, accessSetting) in result.AccessSettings)
            {
                switch (accessFlag, accessSetting)
                {
                    case (KrPermissionsFileAccessSettingFlag.Add, KrPermissionsHelper.FileEditAccessSettings.Disallowed):
                        (validationResult ??= []).AddError(
                            this,
                            isReplace ? "$KrPermissions_Messages_FailToEditFile" : "$KrPermissions_Messages_FailToAddFile",
                            fileName ??= await this.GetFileNameAsync(context));
                        break;

                    case (KrPermissionsFileAccessSettingFlag.CreateLink, KrPermissionsHelper.FileEditAccessSettings.Disallowed):
                        (validationResult ??= []).AddError(
                            this,
                            "$KrPermissions_Messages_FailToCreateFileLink",
                            fileName ??= await this.GetFileNameAsync(context));
                        break;

                    case (KrPermissionsFileAccessSettingFlag.Edit, KrPermissionsHelper.FileEditAccessSettings.Disallowed):
                        (validationResult ??= []).AddError(
                            this,
                            "$KrPermissions_Messages_FailToEditFile",
                            fileName ??= await this.GetFileNameAsync(context));
                        break;

                    case (KrPermissionsFileAccessSettingFlag.Delete, KrPermissionsHelper.FileEditAccessSettings.Disallowed):
                        (validationResult ??= []).AddError(
                            this,
                            "$KrPermissions_Messages_FailToDeleteFile",
                            fileName ??= await this.GetFileNameAsync(context));
                        break;

                    case (KrPermissionsFileAccessSettingFlag.Sign, KrPermissionsHelper.FileEditAccessSettings.Disallowed):
                        (validationResult ??= []).AddError(
                            this,
                            "$KrPermissions_Messages_FailToSignFile",
                            fileName ??= await this.GetFileNameAsync(context));
                        break;

                    case (KrPermissionsFileAccessSettingFlag.Read, _):
                        var errorMessage = isCopy
                            ? "$KrPermissions_Messages_FailToCopyFile"
                            : "$KrPermissions_Messages_FailToLoadFile";

                        switch (accessSetting)
                        {
                            case KrPermissionsHelper.FileReadAccessSettings.FileNotAvailable:
                                (validationResult ??= []).AddError(
                                    this,
                                    errorMessage,
                                    context.FileID.ToString());
                                break;

                            case KrPermissionsHelper.FileReadAccessSettings.ContentNotAvailable:
                                (validationResult ??= []).AddError(
                                    this,
                                    errorMessage,
                                    fileName ??= await this.GetFileNameAsync(context));
                                break;

                            case KrPermissionsHelper.FileReadAccessSettings.OnlyLastVersion:
                            case KrPermissionsHelper.FileReadAccessSettings.OnlyLastAndOwnVersions:
                                if (context.VersionID.HasValue
                                    && !await this.CheckVersionOwnerAsync(
                                        context.FileID,
                                        context.VersionID.Value,
                                        context.Session.User.ID,
                                        context.Card,
                                        accessSetting == KrPermissionsHelper.FileReadAccessSettings.OnlyLastAndOwnVersions,
                                        context.CancellationToken))
                                {
                                    (validationResult ??= []).AddError(
                                        this,
                                        errorMessage,
                                        fileName ??= await this.GetFileNameAsync(context));
                                }

                                break;
                        }

                        break;
                }
            }

            if (validationResult is null
                && result.FileSizeLimit.HasValue
                && context.StoreFile is not null)
            {
                // Доаверяем размеру файла, если:
                // 1) Файл новый или изменён контент - мы не можем взять размер из базы, поэтому работаем с тем, что есть.
                // 2) Стоит флаг ServerOperation и размер файла задан.
                var fileSize =
                    (context.ServerOperation && context.ServerOperation && context.StoreFile.Size >= 0)
                    || context.StoreFile.State is CardFileState.Inserted or CardFileState.Replaced or CardFileState.ModifiedAndReplaced
                    ? context.StoreFile.Size
                    : await this.GetFileSizeAsync(context.StoreFile.RowID, context.CancellationToken);

                if (fileSize > result.FileSizeLimit)
                {
                    KrPermissionsHelper.AddFileValidationError(
                        validationResult ??= [],
                        this,
                        isReplace
                            ? KrPermissionsHelper.KrPermissionsErrorAction.ReplaceFile
                            : context.StoreFile.State == CardFileState.Inserted
                                ? KrPermissionsHelper.KrPermissionsErrorAction.AddFile
                                : KrPermissionsHelper.KrPermissionsErrorAction.EditFile,
                        KrPermissionsHelper.KrPermissionsErrorType.FileTooBig,
                        context.StoreFile.Name,
                        replacedFileName: isReplace ? await this.GetFileNameFromDbAsync(context) : null,
                        categoryCaption: context.StoreFile.CategoryCaption,
                        sizeLimit: result.FileSizeLimit);
                }
            }

            return validationResult?.Build() ?? ValidationResult.Empty;
        }

        #endregion

        #region Private Methods

        private static async ValueTask CheckRulesAsync(
            IKrPermissionsFilesManagerContext context,
            KrPermissionsFilesManagerResult result,
            IList<KrPermissionsFileAccessSettingFlag> stillRequired)
        {
            var currentPriority = context.OrderedRules[0].Priority;

            // Определяем лимит файла только, если было запрошено добавление или изменение файла.
            var sizeChecked = !context.RequiredAccessFlags.HasAny(KrPermissionsFileAccessSettingFlag.Add | KrPermissionsFileAccessSettingFlag.Edit);
            foreach (var fileRule in context.OrderedRules)
            {
                if (currentPriority != fileRule.Priority)
                {
                    currentPriority = fileRule.Priority;
                    for (var i = stillRequired.Count - 1; i >= 0; i--)
                    {
                        if (result.AccessSettings.ContainsKey(stillRequired[i]))
                        {
                            stillRequired.RemoveAt(i);
                        }
                    }

                    sizeChecked |= result.FileSizeLimit.HasValue;

                    // Если все настройки уже определены, то нет смысла проверять правила с более низким приоритетом.
                    if (stillRequired.Count == 0
                        && sizeChecked)
                    {
                        break;
                    }
                }

                if (!await fileRule.CheckFileAsync(context))
                {
                    continue;
                }

                foreach (var requiredFlag in stillRequired)
                {
                    var ruleSettings = requiredFlag switch
                    {
                        KrPermissionsFileAccessSettingFlag.Add => fileRule.AddAccessSetting,
                        KrPermissionsFileAccessSettingFlag.Read => fileRule.ReadAccessSetting,
                        KrPermissionsFileAccessSettingFlag.Edit => fileRule.EditAccessSetting,
                        KrPermissionsFileAccessSettingFlag.Delete => fileRule.DeleteAccessSetting,
                        KrPermissionsFileAccessSettingFlag.Sign => fileRule.SignAccessSetting,
                        KrPermissionsFileAccessSettingFlag.CreateLink => fileRule.CreateLinkAccessSetting,
                        _ => null,
                    };

                    if (ruleSettings is not null
                        && (!result.AccessSettings.TryGetValue(requiredFlag, out var currentSettings)
                            || currentSettings is null
                            || ruleSettings <= currentSettings.Value))
                    {
                        result.AccessSettings[requiredFlag] = ruleSettings.Value;
                    }
                }


                if (!sizeChecked
                    && fileRule.FileSizeLimit.HasValue
                    && (result.FileSizeLimit is null
                        || fileRule.FileSizeLimit.Value < result.FileSizeLimit.Value))
                {
                    result.FileSizeLimit = fileRule.FileSizeLimit;
                }
            }

            for (var i = 0; i < stillRequired.Count; i++)
            {
                result.AccessSettings.TryAdd(stillRequired[i], null);
            }
        }

        private static IList<KrPermissionsFileAccessSettingFlag> ParseAccessFlags(KrPermissionsFileAccessSettingFlag requiredAccessFlags)
        {
            if (requiredAccessFlags == KrPermissionsFileAccessSettingFlag.None)
            {
                return Array.Empty<KrPermissionsFileAccessSettingFlag>();
            }

            var result = new List<KrPermissionsFileAccessSettingFlag>();

            var requiredAccessFlagsInt = Math.Min((int) requiredAccessFlags, (int) KrPermissionsFileAccessSettingFlag.All);

            var currentFlag = 1;
            while (requiredAccessFlagsInt > 0)
            {
                if (requiredAccessFlagsInt % 2 == 1)
                {
                    result.Add((KrPermissionsFileAccessSettingFlag) currentFlag);
                }

                requiredAccessFlagsInt >>= 1;
                currentFlag *= 2;
            }

            return result;
        }

        private ValueTask<string?> GetFileNameAsync(IKrPermissionsFilesManagerContext context)
        {
            if (context.VersionID is not null)
            {
                return this.GetFileVersionNameAsync(context, context.VersionID.Value);
            }

            var fileName = context.StoreFile?.Name ?? context.File?.Name;
            if (!string.IsNullOrEmpty(fileName))
            {
                return new(fileName);
            }

            return this.GetFileNameFromDbAsync(context);
        }

        private async ValueTask<string?> GetFileNameFromDbAsync(IKrPermissionsFilesManagerContext context)
        {
            await using var _ = this.dbScope.Create();

            var db = this.dbScope.Db;
            var builder = this.dbScope.BuilderFactory;

            return await db
                .SetCommand(
                    builder
                        .Select().C("Name").From("Files").NoLock().Where().C("RowID").Equals().P("FileID")
                        .Build(),
                    db.Parameter("FileID", context.FileID))
                .LogCommand()
                .ExecuteAsync<string>(context.CancellationToken);
        }

        private async ValueTask<string?> GetFileVersionNameAsync(IKrPermissionsFilesManagerContext context, Guid versionID)
        {
            var fileName = context.StoreFile?.Versions.FirstOrDefault(x => x.RowID == versionID)?.Name
                ?? context.StoreFile?.Name
                ?? context.File?.Versions.FirstOrDefault(x => x.RowID == versionID)?.Name;

            if (!string.IsNullOrEmpty(fileName))
            {
                return fileName;
            }

            await using var _ = this.dbScope.Create();

            var db = this.dbScope.Db;
            var builder = this.dbScope.BuilderFactory;

            return await db
                .SetCommand(
                    builder
                        .Select().C("Name").From("FileVersions").NoLock().Where().C("RowID").Equals().P("VersionID")
                        .Build(),
                    db.Parameter("VersionID", versionID))
                .LogCommand()
                .ExecuteAsync<string>(context.CancellationToken);
        }

        private async Task<bool> CheckVersionOwnerAsync(
            Guid fileID,
            Guid fileVersionID,
            Guid userID,
            Card? card,
            bool checkOwnVersions,
            CancellationToken cancellationToken)
        {
            await using var _ = this.dbScope.Create();
            var db = this.dbScope.Db;
            if (!checkOwnVersions)
            {
                return await db
                    .SetCommand(
                        this.dbScope.BuilderFactory
                            .SelectExists(x => x
                                .Select().V(null)
                                    .From("Files", "f").NoLock()
                                    .Where().C("f", "RowID").Equals().P("FileID")
                                        .And().C("f", "VersionRowID").Equals().P("FileVersionID"))
                            .Build(),
                        db.Parameter("FileID", fileID, LinqToDB.DataType.Guid),
                        db.Parameter("FileVersionID", fileVersionID, LinqToDB.DataType.Guid))
                    .LogCommand()
                    .ExecuteAsync<bool>(cancellationToken);
            }

            var deputiesSettings = await this.deputiesProvider.GetSettingsAsync(cancellationToken);
            var useDeputyRoleSeparation = deputiesSettings.UseDeputyRoleSeparation;

            if (deputiesSettings.UseDeputyRoleSeparation)
            {
                return await db
                    .SetCommand(
                        this.dbScope.BuilderFactory
                            .SelectExists(x => x
                                .Select().V(null)
                                    .From("Files", "f").NoLock()
                                    .InnerJoin("FileVersions", "fv").NoLock()
                                        .On().C("f", "RowID").Equals().C("fv", "ID")
                                        .And().C("fv", "RowID").Equals().P("FileVersionID")
                                    .Where().C("f", "RowID").Equals().P("FileID")
                                        .And().E(b => b
                                            .C("f", "VersionRowID").Equals().P("FileVersionID")
                                            .Or().C("fv", "CreatedByID").Equals().P("UserID")))
                            .Build(),
                        db.Parameter("UserID", userID, LinqToDB.DataType.Guid),
                        db.Parameter("FileID", fileID, LinqToDB.DataType.Guid),
                        db.Parameter("FileVersionID", fileVersionID, LinqToDB.DataType.Guid))
                    .LogCommand()
                    .ExecuteAsync<bool>(cancellationToken);
            }

            return await db
                .SetCommand(
                    this.dbScope.BuilderFactory
                        .SelectExists(x => x
                            .StartLogicalSubQuery("")
                                .Select().Top(1).V(null)
                                    .From("Files", "f").NoLock()
                                    .InnerJoin("FileVersions", "fv").NoLock()
                                        .On().C("f", "RowID").Equals().C("fv", "ID")
                                        .And().C("fv", "RowID").Equals().P("FileVersionID")
                                    .Where().C("f", "RowID").Equals().P("FileID")
                                        .And().E(b => b
                                            .C("f", "VersionRowID").Equals().P("FileVersionID")
                                            .Or().C("fv", "CreatedByID").Equals().P("UserID"))
                                    .Limit(1)
                            .EndLogicalSubQuery()
                            .UnionAll()
                            .StartLogicalSubQuery("")
                                .Select().Top(1).V(null)
                                    .From("Files", "f").NoLock()
                                    .InnerJoin("FileVersions", "fv").NoLock()
                                        .On().C("f", "RowID").Equals().C("fv", "ID")
                                        .And().C("fv", "RowID").Equals().P("FileVersionID")
                                    .InnerJoin(RoleStrings.RoleUsers, "ru").NoLock()
                                        .On().C("ru", "ID").Equals().C("fv", "CreatedByID")
                                    .Where().C("f", "RowID").Equals().P("FileID")
                                        .And().C("ru", "UserID").Equals().P("UserID")
                                        .And().C("ru", "IsDeputy").Equals().V(true)
                                        .And().E(b => b
                                            .C("f", "VersionRowID").Equals().P("FileVersionID")
                                            .Or().C("fv", "CreatedByID").Equals().C("ru", "ID"))
                                    .Limit(1)
                            .EndLogicalSubQuery()
                            .UnionAll()
                            .StartLogicalSubQuery("")
                                .Select().Top(1).V(null)
                                    .From("Files", "f").NoLock()
                                    .InnerJoin("FileVersions", "fv").NoLock()
                                        .On().C("f", "RowID").Equals().C("fv", "ID")
                                        .And().C("fv", "RowID").Equals().P("FileVersionID")
                                    .InnerJoin(RoleStrings.NestedRoles, "nr").NoLock()
                                        .On().C("nr", "ParentID").Equals().C("fv", "CreatedByID")
                                    .InnerJoin(RoleStrings.RoleUsers, "ru").NoLock()
                                        .On().C("nr", "ID").Equals().C("ru", "ID")
                                    .Where().C("f", "RowID").Equals().P("FileID")
                                        .And().C("ru", "UserID").Equals().P("UserID")
                                        .And().C("ru", "IsDeputy").Equals().V(true)
                                        .And().E(b => b
                                            .C("f", "VersionRowID").Equals().P("FileVersionID")
                                            .Or().C("fv", "CreatedByID").Equals().C("nr", "ParentID"))
                                    .Limit(1)
                            .EndLogicalSubQuery())
                        .Build(),
                    db.Parameter("UserID", userID, LinqToDB.DataType.Guid),
                    db.Parameter("FileID", fileID, LinqToDB.DataType.Guid),
                    db.Parameter("FileVersionID", fileVersionID, LinqToDB.DataType.Guid))
                .LogCommand()
                .ExecuteAsync<bool>(cancellationToken);
        }

        private async ValueTask<bool> HasCheckAttributeChangesAsync(IKrPermissionsFilesManagerContext context)
        {
            var storeFile = context.StoreFile;
            if (storeFile is null)
            {
                return false;
            }

            if (storeFile.Flags.Has(CardFileFlags.UpdateCategory))
            {
                return true;
            }
            else if (storeFile.Flags.Has(CardFileFlags.UpdateName))
            {
                var newExtension = FileHelper.GetExtension(storeFile.Name).TrimStart('.').ToLowerInvariant();
                var oldExtension = context.File is null
                    ? context.FileInfo.TryGet<string>("FileExtension")
                    ?? await this.GetFileExtensionAsync(context.FileID, context.CancellationToken)
                    : System.IO.Path.GetExtension(context.File.Name);

                if (!newExtension.Equals(oldExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private async ValueTask<string> GetFileExtensionAsync(Guid fileID, CancellationToken cancellationToken)
        {
            await using var _ = this.dbScope.Create();

            var db = this.dbScope.Db;
            var builder = this.dbScope.BuilderFactory;

            var fileName = await db.SetCommand(
                    builder
                        .Select().C("Name")
                        .From("Files").NoLock()
                        .Where().C("RowID").Equals().P("FileID")
                        .Build(),
                    db.Parameter("FileID", fileID, LinqToDB.DataType.Guid))
                .LogCommand()
                .ExecuteAsync<string>(cancellationToken);

            return string.IsNullOrEmpty(fileName) ? string.Empty : System.IO.Path.GetExtension(fileName);
        }

        private async Task<long> GetFileSizeAsync(Guid fileID, CancellationToken cancellationToken)
        {
            await using var _ = this.dbScope.Create();

            var db = this.dbScope.Db;
            var builder = this.dbScope.BuilderFactory;

            return await db.SetCommand(
                    builder
                        .Select().C("fv", "Size")
                        .From("Files", "f").NoLock()
                        .InnerJoin("FileVersions", "fv").NoLock()
                            .On().C("f", "VersionRowID").Equals().C("fv", "RowID")
                        .Where().C("f", "RowID").Equals().P("FileID")
                        .Build(),
                    db.Parameter("FileID", fileID, LinqToDB.DataType.Guid))
                .LogCommand()
                .ExecuteAsync<long>(cancellationToken);
        }

        #endregion
    }
}
