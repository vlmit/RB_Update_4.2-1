#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions.Files;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.IO;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using static Tessa.Extensions.Default.Shared.Workflow.KrProcess.KrConstants;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions
{
    /// <summary>
    /// Расширение, проверяющее права доступа к карточке при сохранении.
    /// </summary>
    public sealed class KrPermissionsStoreExtension(
        IKrTypesCache cache,
        IKrTokenProvider krTokenProvider,
        IKrPermissionsManager permissionsManager,
        IKrScope krScope,
        IKrFileOwnershipChecker krFileOwnershipChecker) : CardStoreExtension
    {
        #region Fields

        private readonly IKrTypesCache cache = NotNullOrThrow(cache);
        private readonly IKrTokenProvider krTokenProvider = NotNullOrThrow(krTokenProvider);
        private readonly IKrPermissionsManager permissionsManager = NotNullOrThrow(permissionsManager);
        private readonly IKrScope krScope = NotNullOrThrow(krScope);
        private readonly IKrFileOwnershipChecker krFileOwnershipChecker = NotNullOrThrow(krFileOwnershipChecker);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeRequestWhenTypeResolved(ICardStoreExtensionContext context)
        {
            if (context.CardType is null
                || !await KrComponentsHelper.HasBaseAsync(context.CardType.ID, this.cache, context.CancellationToken))
            {
                return;
            }

            // Для карточек типового решения всегда ставим ForceTransaction
            context.Request.ForceTransaction = true;
        }

        /// <inheritdoc/>
        public override async Task AfterBeginTransaction(ICardStoreExtensionContext context)
        {
            // права на измененную карточку считаем в AfterBeginTransaction, чтобы считать доступ к карточке по данным,
            // по которым эти права были рассчитаны изначально.

            if (context.CardType is null
                || context.Request.TryGetCard() is not { } card
                || card.StoreMode != CardStoreMode.Update
                || !await KrComponentsHelper.HasBaseAsync(context.CardType.ID, this.cache, context.CancellationToken))
            {
                return;
            }

            await this.CheckPermissionsOnUpdatingAsync(context, card);
        }

        /// <inheritdoc/>
        public override async Task BeforeCommitTransaction(ICardStoreExtensionContext context)
        {
            // считаем права на возможность изменить только что созданную карточку внутри блокировки на запись карточки;
            // BeforeCommit нужен, чтобы карточка уже была в базе для выполнения контекстных ролей.

            if (context.CardType is null
                || context.Request.TryGetCard() is not { } card
                || card.StoreMode != CardStoreMode.Insert
                || !await KrComponentsHelper.HasBaseAsync(context.CardType.ID, this.cache, context.CancellationToken))
            {
                return;
            }

            await this.CheckPermissionsOnCreatingAsync(context, card);
        }

        /// <inheritdoc/>
        public override async Task AfterRequest(ICardStoreExtensionContext context)
        {
            if (context.CardType is not { } cardType ||
                context.Response is not { } response)
            {
                return;
            }

            if (!context.RequestIsSuccessful)
            {
                PrepareMandatoryFailedData(context);
                return;
            }
            else if (!await KrComponentsHelper.HasBaseAsync(cardType.ID, this.cache, context.CancellationToken))
            {
                return;
            }

            if (context.Info.TryGet<bool>(KrPermissionsHelper.AddRefreshTokenKey))
            {
                var token = this.krTokenProvider
                    .CreateToken(
                        response.CardID,
                        permissions: [KrPermissionFlagDescriptors.ReadCard],
                        modifyTokenAction: (t) => t.ExpiryDate = DateTime.UtcNow.AddMinutes(1));

                token.Set(response.Info);
            }
        }

        #endregion

        #region Private Methods

        private static bool FileHasChangesExceptSignatures(CardFile file)
        {
            // подписи могут изменяться без прав на редактирование файлов
            var state = file.State;
            if (state is CardFileState.Replaced or CardFileState.ModifiedAndReplaced)
            {
                return true;
            }

            if (state != CardFileState.Modified)
            {
                return false;
            }

            // изменялись системные свойства
            if (file.Flags != CardFileFlags.None)
            {
                return true;
            }

            if (file.TryGetCard() is { } fileCard
                && fileCard.TryGetSections() is { } fileSections)
            {
                // нет изменённых секций
                int count = fileSections.Count;
                if (count == 0)
                {
                    return false;
                }

                // изменялись другие секции, кроме как секции с подписями
                return count > 1 || fileSections.First().Key != CardSignatureHelper.SectionName;
            }

            // нет изменённых секций
            return false;
        }

        private async Task<KrPermissionFlagDescriptor[]> GetRequiredPermissionsAsync(
            Guid userID,
            Card card,
            IValidationResultBuilder validationResults,
            IDbScope dbScope,
            Dictionary<string, object?> info,
            bool serverStore,
            CancellationToken cancellationToken = default)
        {
            //Сначала посчитаем какие проверки нужны
            var required = new HashSet<KrPermissionFlagDescriptor>();

            if (card.TryGetSections()?.Any(x => x.Key != KrStages.Virtual
                                       && x.Key != KrActiveTasks.Virtual
                                       && x.Key != KrPerformersVirtual.Synthetic
                                       && (x.Value.TryGetRawFields()?.Count > 0 || x.Value.TryGetRows()?.Count > 0)) == true
                || card.TryGetTasks()?.Any(x =>
                        x.State != CardRowState.None
                        && x.TryGetCard() is { } taskCard
                        && taskCard.TryGetSections()?.Any(y =>
                            y.Value.TryGetRawFields()?.Count > 0 || y.Value.TryGetRows()?.Count > 0) == true) == true)
            {
                required.Add(KrPermissionFlagDescriptors.EditCard);
            }

            if (card.TryGetFiles() is { Count: > 0 } files)
            {
                foreach (var file in files)
                {
                    Guid fileCreatedByID = serverStore
                        ? file.Card.CreatedByID
                        : file.State == CardFileState.Inserted
                            ? userID
                            : await this.GetFileOwnerIDAsync(dbScope, file.RowID, cancellationToken);
                    if (FileHasChangesExceptSignatures(file))
                    {
                        var isOwnOrDeputizedFile = await this.krFileOwnershipChecker.IsOwnAsync(
                            fileCreatedByID,
                            userID,
                            true,
                            card: card,
                            cacheHolder: info,
                            cancellationToken: cancellationToken);
                        if (isOwnOrDeputizedFile)
                        {
                            // изменение собственных файлов - право на это изменение определяется отдельно согласно
                            // заданиям карточки, поэтому пользователь может его получить, не обладая правом на
                            // редактирование файлов в целом
                            required.Add(KrPermissionFlagDescriptors.EditOwnFiles);
                        }
                        else
                        {
                            // изменение файлов
                            required.Add(KrPermissionFlagDescriptors.EditFiles);
                        }

                        // Изменение категории и расширения файла теперь требуют прав на добавление файла
                        if (file.Flags.Has(CardFileFlags.UpdateCategory)
                            || (file.Flags.Has(CardFileFlags.UpdateName)
                                && await CheckExtensionChangedAsync(dbScope, file, cancellationToken)))
                        {
                            required.Add(KrPermissionFlagDescriptors.AddFiles);
                        }
                    }

                    if (file.State == CardFileState.Inserted)
                    {
                        // добавление файлов
                        required.Add(KrPermissionFlagDescriptors.AddFiles);

                        if (file.DeletionMode is CardFileDeletionMode.Restore && fileCreatedByID != userID)
                        {
                            // восстановление всех удалённых файлов
                            required.Add(KrPermissionFlagDescriptors.RestoreAllDeletedFiles);
                        }
                    }
                    else if (file.State == CardFileState.Deleted)
                    {
                        var isOwnOrDeputizedFile = await this.krFileOwnershipChecker.IsOwnAsync(
                            fileCreatedByID,
                            userID,
                            true,
                            card: card,
                            cacheHolder: info,
                            cancellationToken: cancellationToken);
                        if (isOwnOrDeputizedFile)
                        {
                            // удаление собственных файлов.
                            required.Add(KrPermissionFlagDescriptors.DeleteOwnFiles);
                        }
                        else
                        {
                            required.Add(KrPermissionFlagDescriptors.DeleteFiles);
                        }
                    }
                }
            }

            if (card.StoreMode == CardStoreMode.Update)
            {
                var containsKrStagesVirtual = card.TryGetStagesSection(out var krStagesVirtual);
                var hasSkipStages = false;
                var hasModifiedStages = false;
                if (containsKrStagesVirtual)
                {
                    Card? satellite = null;
                    foreach (var stage in krStagesVirtual.Rows.Where(i => i.State == CardRowState.Deleted))
                    {
                        if (satellite is null)
                        {
                            satellite = await this.krScope.GetKrSatelliteAsync(
                                card.ID,
                                validationResult: validationResults,
                                cancellationToken: cancellationToken);

                            if (satellite is null)
                            {
                                break;
                            }
                        }

                        if (satellite.TryGetStagesSection(out var satelliteKrStages)
                            && satelliteKrStages.Rows.SingleOrDefault(j => j.RowID == stage.RowID) is { } satelliteKrStage
                            && KrProcessSharedHelper.CanBeSkipped(satelliteKrStage))
                        {
                            hasSkipStages = true;
                            break;
                        }
                    }

                    hasModifiedStages = krStagesVirtual.Rows.Any(i => i.State is CardRowState.Modified or CardRowState.Inserted);
                }

                // Пропуск этапа.
                if (hasSkipStages)
                {
                    required.Add(KrPermissionFlagDescriptors.CanSkipStages);
                }

                // Изменение маршрута.
                if (hasModifiedStages
                    || card.Sections.TryGetValue(KrPerformersVirtual.Synthetic, out var performersSection)
                    && performersSection.TryGetRows()?.Any(x => x.State != CardRowState.None) == true)
                {
                    required.Add(KrPermissionFlagDescriptors.EditRoute);
                }
            }

            // Подписание файлов
            if (CardSignatureHelper.AnySignatureRow(card,
                (file, signatureRow) => signatureRow.State != CardRowState.Deleted))
            {
                required.Add(KrPermissionFlagDescriptors.SignFiles);
            }

            return required.ToArray();
        }

        private async Task<Guid> GetFileOwnerIDAsync(IDbScope dbScope, Guid fileID, CancellationToken cancellationToken)
        {
            await using var _ = dbScope.Create();
            var db = dbScope.Db;
            var query =
                dbScope.BuilderFactory.Cached(
                    $"{nameof(KrPermissionsStoreExtension)}.{nameof(GetFileOwnerIDAsync)}",
                    bf => bf
                        .Select()
                        .C("CreatedByID")
                        .From("Files").NoLock()
                        .Where().C("RowID").Equals().P("FileID")
                        .Build());

            return await db.SetCommand(
                query,
                db.Parameter("FileID", fileID, DataType.Guid))
                .LogCommand()
                .ExecuteAsync<Guid>(cancellationToken);
        }

        private async Task CheckPermissionsOnCreatingAsync(ICardStoreExtensionContext context, Card storeCard)
        {
            var krToken = KrToken.TryGet(storeCard.Info);
            var serverToken = context.Info.TryGetServerToken();

            var permContextResult = await this.permissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    Card = storeCard,
                    IsStore = true,
                    WithExtendedPermissions = true,
                    ValidationResult = context.ValidationResult,
                    AdditionalInfo = context.Info,
                    PrevToken = krToken,
                    ServerToken = serverToken,
                    ExtensionContext = context,
                    CustomValidationAction = (token, validationResult) =>
                    {
                        if (token.TryGetCardTypeID(out var cardTypeID)
                            && cardTypeID != storeCard.TypeID)
                        {
                            validationResult.AddError(this, "$KrMessages_TokenForOtherCardType", cardTypeID, storeCard.TypeID);
                        }

                        if (token.TryGetDocTypeID(out var docTypeID)
                            && (!KrProcessSharedHelper.TryGetDocTypeID(storeCard, out var storeDocTypeID) || docTypeID != storeDocTypeID))
                        {
                            validationResult.AddError(this, "$KrMessages_TokenForOtherDocType", docTypeID, storeDocTypeID);
                        }
                    },
                    ServiceType = context.Request.ServiceType,
                },
                cancellationToken: context.CancellationToken);

            if (permContextResult.Status == KrPermissionsCreateContextStatus.Success)
            {
                context.Info[nameof(KrPermissionsStoreExtension)] = await this.permissionsManager.CheckRequiredPermissionsAsync(
                    permContextResult.Context,
                    await this.GetRequiredPermissionsAsync(
                        context.Session.User.ID,
                        storeCard,
                        context.ValidationResult,
                        context.DbScope!,
                        context.Info,
                        context.Request.ServiceType == CardServiceType.Default,
                        context.CancellationToken));

                context.Info[KrPermissionsHelper.AddRefreshTokenKey] = BooleanBoxes.True;
            }
        }

        private async Task CheckPermissionsOnUpdatingAsync(ICardStoreExtensionContext context, Card storeCard)
        {
            if (context.Request.ServiceType != CardServiceType.Default
                && storeCard.TryGetSections()?.TryGetValue(DocumentCommonInfo.Name, out var section) == true
                && section.RawFields.TryGetValue(DocumentCommonInfo.DocTypeID, out var docTypeIDObj)
                && await KrProcessSharedHelper.GetDocTypeIDAsync(storeCard.ID, context.DbScope, context.CancellationToken) != docTypeIDObj as Guid?)
            {
                context.ValidationResult.AddError(
                    this,
                    "$KrMessages_DocumentTypeChangingProhibited");
                return;
            }

            var krToken = KrToken.TryGet(storeCard.Info);
            var serverToken = context.Info.TryGetServerToken();
            if (krToken is null)
            {
                //Если запрос отправлен не из плагина Chronos или из неизвестного плагина (как правило это обычный запрос из клиента)
                if (!context.Request.GetIgnorePermissionsWarning()
                    && !context.Request.TryGetPluginType().HasValue)
                {
                    //Предупредим пользователя, что что-то пошло не так и токен не был найден
                    context.ValidationResult.AddWarning(this, "$KrMessages_CardHasNoTokenWhenSaving");
                }
            }

            var permContextResult = await this.permissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    Card = storeCard,
                    IsStore = true,
                    WithExtendedPermissions = true,
                    ValidationResult = context.ValidationResult,
                    AdditionalInfo = context.Info,
                    PrevToken = krToken,
                    ServerToken = serverToken,
                    ExtensionContext = context,
                    ServiceType = context.Request.ServiceType,
                },
                cancellationToken: context.CancellationToken);

            if (permContextResult.Status == KrPermissionsCreateContextStatus.Success)
            {
                var requiredPermissions = await this.GetRequiredPermissionsAsync(
                    context.Session.User.ID,
                    storeCard,
                    context.ValidationResult,
                    context.DbScope!,
                    context.Info,
                    context.Request.ServiceType == CardServiceType.Default,
                    context.CancellationToken);
                context.Info[nameof(KrPermissionsStoreExtension)] = await this.permissionsManager.CheckRequiredPermissionsAsync(
                    permContextResult.Context,
                    requiredPermissions);

                if (requiredPermissions.Contains(KrPermissionFlagDescriptors.EditCard)
                    || krToken?.CardVersion == storeCard.Version)
                {
                    context.Info[KrPermissionsHelper.AddRefreshTokenKey] = BooleanBoxes.True;
                }
            }
        }

        private static void PrepareMandatoryFailedData(ICardStoreExtensionContext context)
        {
            if (context.Info.TryGetValue(nameof(KrPermissionsStoreExtension), out var resultObject)
                && resultObject is KrPermissionsManagerCheckResult result
                && result.Info.TryGetValue(KrPermissionsHelper.FailedMandatoryRulesKey, out var rulesObj)
                && rulesObj is IList rules
                && context.Response is { } response)
            {
                var rulesForSend = new List<KrPermissionMandatoryRuleStorage>();
                foreach (KrPermissionMandatoryRule rule in rules)
                {
                    rulesForSend.Add(
                        new KrPermissionMandatoryRuleStorage(
                            rule.SectionID,
                            rule.HasColumns ? rule.ColumnIDs : null));
                }

                response.Info[KrPermissionsHelper.FailedMandatoryRulesKey]
                    = rulesForSend.Select(x => (object) x.GetStorage());
            }
        }

        private static async ValueTask<bool> CheckExtensionChangedAsync(IDbScope dbScope, CardFile file, CancellationToken cancellationToken = default)
        {
            var oldName = await GetFileNameFromDbAsync(dbScope, file.RowID, cancellationToken);
            var oldExtension = FileHelper.GetExtension(oldName)?.ToLowerInvariant().TrimStart('.');
            var newExtension = FileHelper.GetExtension(file.Name).ToLowerInvariant().TrimStart('.');

            return !String.Equals(oldExtension, newExtension, StringComparison.Ordinal);
        }

        private static async ValueTask<string?> GetFileNameFromDbAsync(IDbScope dbScope, Guid fileID, CancellationToken cancellationToken = default)
        {
            await using var _ = dbScope.Create();

            var db = dbScope.Db;
            var builder = dbScope.BuilderFactory;

            return await db
                .SetCommand(
                    builder
                        .Select().C("Name").From("Files").NoLock().Where().C("RowID").Equals().P("FileID")
                        .Build(),
                    db.Parameter("FileID", fileID))
                .LogCommand()
                .ExecuteAsync<string>(cancellationToken);
        }

        #endregion
    }
}
