#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Cards.Metadata;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions.Files;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions
{
    /// <summary>
    /// Расширение на создание и получение карточки, которое рассчитывает доступ к карточке.
    /// </summary>
    public sealed class KrPermissionsNewGetExtension(
        IKrTokenProvider krTokenProvider,
        IKrPermissionsManager permissionsManager,
        ICardMetadata cardMetadata,
        IKrTypesCache typesCache,
        IKrScope krScope,
        IKrFileOwnershipChecker krFileOwnershipChecker) : CardNewGetExtension
    {
        #region Constants And Static Fields

        private const string DocTypeIDKey = StorageHelper.SystemKeyPrefix + nameof(KrPermissionsNewGetExtension) + "DocTypeID";

        #endregion

        #region Fields

        private readonly IKrTokenProvider krTokenProvider = NotNullOrThrow(krTokenProvider);
        private readonly IKrPermissionsManager permissionsManager = NotNullOrThrow(permissionsManager);
        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);
        private readonly IKrTypesCache typesCache = NotNullOrThrow(typesCache);
        private readonly IKrScope krScope = NotNullOrThrow(krScope);
        private readonly IKrFileOwnershipChecker krFileOwnershipChecker = NotNullOrThrow(krFileOwnershipChecker);

        #endregion

        #region Private Methods

        /// <summary>
        /// Устанавливает на секции и строки карточки разрешения в зависимости от полученных разрешений.
        /// </summary>
        /// <param name="userID">Идентификатор пользователя.</param>
        /// <param name="card">Карточка.</param>
        /// <param name="permissionsResult">Результат расчета прав доступа.</param>
        /// <param name="fileSettings">Хеш настроек доступа к файлам.</param>
        /// <param name="filesSettings">Настройки доступа файлов пользователя.</param>
        /// <param name="info">Дополнительная информация, связанная с контекстом расширений.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        private async Task SetCardPermissionsAsync(
            Guid userID,
            Card card,
            IKrPermissionsManagerResult permissionsResult,
            HashSet<Guid, KrPermissionsFileSettings>? fileSettings,
            KrPermissionsFilesSettings? filesSettings,
            Dictionary<string, object?> info,
            CancellationToken cancellationToken = default)
        {
            var permissions = card.Permissions;
            var krPermissions = permissionsResult.Permissions;

            if (permissionsResult.WithExtendedSettings)
            {
                await this.SetCardExtendedPermissionsAsync(card, permissionsResult.ExtendedCardSettings, cancellationToken);
            }
            foreach (var task in card.Tasks)
            {
                if (permissionsResult.ExtendedTasksSettings.TryGetValue(task.TypeID, out var extendedTaskSettings))
                {
                    await this.SetCardExtendedPermissionsAsync(task.Card, extendedTaskSettings, cancellationToken);
                }
            }

            //Права на редактирование карточки
            permissions.SetCardPermissions(
                krPermissions.Contains(KrPermissionFlagDescriptors.EditCard)
                    ? CardPermissionFlags.AllowModify
                    | CardPermissionFlags.AllowDeleteRow
                    | CardPermissionFlags.AllowInsertRow
                    : CardPermissionFlags.ProhibitModify
                    | CardPermissionFlags.ProhibitDeleteRow
                    | CardPermissionFlags.ProhibitInsertRow);

            // Права на подписание файлов
            permissions.SetCardPermissions(
                krPermissions.Contains(KrPermissionFlagDescriptors.SignFiles)
                    ? CardPermissionFlags.AllowSignFile
                    : CardPermissionFlags.ProhibitSignFile);

            // Право на редактирование (и удаление) файлов.
            for (var i = card.Files.Count - 1; i >= 0; i--)
            {
                var file = card.Files[i];
                var createdBy = file.Card.CreatedByID;

                KrPermissionsFileSettings? settings = null;
                if (fileSettings is not null && fileSettings.TryGetItem(file.RowID, out settings))
                {
                    switch (settings.ReadAccessSetting)
                    {
                        case KrPermissionsHelper.FileReadAccessSettings.FileNotAvailable:
                            card.Files.RemoveAt(i);
                            permissions.FilePermissions.Remove(file.RowID);
                            continue;

                        case KrPermissionsHelper.FileReadAccessSettings.ContentNotAvailable:
                        case KrPermissionsHelper.FileReadAccessSettings.OnlyLastVersion:
                            file.VersionsLoaded = true;
                            break;
                    }
                }

                //Пользователь может редактировать файлы, которые он добавил, если они не виртуальные
                if (file.IsVirtual)
                {
                    permissions.SetFilePermissions(file.RowID, CardPermissionFlagValues.ProhibitAllFile, overwrite: true);
                }
                else
                {
                    var canEdit = settings?.EditAccessSetting is null
                        ? // есть разрешение на редактирование файлов
                            krPermissions.Contains(KrPermissionFlagDescriptors.EditFiles)
                            // или есть разрешение на редактирование своих файлов и файл его
                            || krPermissions.Contains(KrPermissionFlagDescriptors.EditOwnFiles) &&
                                await this.krFileOwnershipChecker.IsOwnAsync(
                                    createdBy,
                                    userID,
                                    true,
                                    card: card,
                                    cacheHolder: info,
                                    cancellationToken: cancellationToken)
                        : settings.EditAccessSetting == KrPermissionsHelper.FileEditAccessSettings.Allowed;

                    var canDelete = settings?.DeleteAccessSetting is null
                        ? // есть разрешение на редактирование файлов
                            krPermissions.Contains(KrPermissionFlagDescriptors.DeleteFiles)
                            // или есть разрешение на редактирование своих файлов и файл его
                            || krPermissions.Contains(KrPermissionFlagDescriptors.DeleteOwnFiles) &&
                                await this.krFileOwnershipChecker.IsOwnAsync(
                                    createdBy,
                                    userID,
                                    true,
                                    card: card,
                                    cacheHolder: info,
                                    cancellationToken: cancellationToken)
                        : settings.DeleteAccessSetting == KrPermissionsHelper.FileEditAccessSettings.Allowed;

                    var canSign = settings?.SignAccessSetting is null
                            ? krPermissions.Contains(KrPermissionFlagDescriptors.SignFiles)
                            : settings?.SignAccessSetting == KrPermissionsHelper.FileEditAccessSettings.Allowed;

                    var canCreateLink = settings?.CreateLinkAccessSetting is null
                        ? krPermissions.Contains(KrPermissionFlagDescriptors.CreateFileLink)
                        : settings?.CreateLinkAccessSetting == KrPermissionsHelper.FileEditAccessSettings.Allowed;

                    permissions.SetFilePermissions(
                        file.RowID,
                        (canEdit
                            ? CardPermissionFlags.AllowModify | CardPermissionFlags.AllowReplaceFile
                            : CardPermissionFlags.ProhibitModify | CardPermissionFlags.ProhibitReplaceFile)
                        |
                        // право на удаление файлов
                        (canDelete
                            ? CardPermissionFlags.AllowDeleteFile
                            : CardPermissionFlags.ProhibitDeleteFile)
                        |
                        // право на подписание файлов
                        (canSign
                            ? CardPermissionFlags.AllowSignFile
                            : CardPermissionFlags.ProhibitSignFile)
                        |
                        // право на создание ссылки на файл
                        (canCreateLink
                            ? CardPermissionFlags.AllowCreateFileLink
                            : CardPermissionFlags.ProhibitCreateFileLink),
                        overwrite: true);
                }
            }

            // Добавление файлов в карточку.
            permissions.SetCardPermissions(
                krPermissions.Contains(KrPermissionFlagDescriptors.AddFiles)
                || filesSettings?.TryGetGlobalSettings() is { AddAllowed: true } or { AllowedCategories.Count: > 0 }
                || filesSettings?.TryGetExtensionSettings()?.Values.Any(x => x.AddAllowed || x.AllowedCategories is { Count: > 0 }) == true
                    ? CardPermissionFlags.AllowInsertFile
                    : CardPermissionFlags.ProhibitInsertFile);

            // Управление номерами в карточке.
            permissions.SetCardPermissions(
                krPermissions.Contains(KrPermissionFlagDescriptors.EditNumber)
                    ? CardPermissionFlags.AllowEditNumber
                    : CardPermissionFlags.ProhibitEditNumber);

            // Редактирование маршрута согласования.

            // Редактирование функциональных ролей заданий.
            if (krPermissions.Contains(KrPermissionFlagDescriptors.ModifyAllTaskAssignedRoles))
            {
                foreach (var task in card.Tasks)
                {
                    task.Flags |= CardTaskFlags.CanModifyTaskAssignedRoles;
                }
            }

            if (krPermissions.Contains(KrPermissionFlagDescriptors.ModifyOwnTaskAssignedRoles))
            {
                foreach (var task in card.Tasks)
                {
                    if (task.TaskSessionRoles.Count > 0)
                    {
                        task.Flags |= CardTaskFlags.CanModifyTaskAssignedRoles;
                    }
                }
            }

            //Редактирование этапов
            var stagesSection =
                permissions.Sections.GetOrAdd(KrConstants.KrStages.Virtual);
            stagesSection.Type = CardSectionType.Table;

            //И согласующих
            var approversSection =
                permissions.Sections.GetOrAdd(KrConstants.KrPerformersVirtual.Synthetic);
            approversSection.Type = CardSectionType.Table;

            if (krPermissions.Contains(KrPermissionFlagDescriptors.EditRoute))
            {
                //Если можно редактировать маршрут - позволяем изменять / добавлять / удалять этапы и согласантов
                stagesSection.SetSectionPermissions(
                    CardPermissionFlags.AllowModify
                    | CardPermissionFlags.AllowInsertRow
                    | CardPermissionFlags.AllowDeleteRow,
                    overwrite: true);

                approversSection.SetSectionPermissions(
                    CardPermissionFlags.AllowModify
                    | CardPermissionFlags.AllowInsertRow
                    | CardPermissionFlags.AllowDeleteRow,
                    overwrite: true);
            }
            else
            {
                stagesSection.SetSectionPermissions(
                    CardPermissionFlags.ProhibitModify
                    | CardPermissionFlags.ProhibitInsertRow
                    | CardPermissionFlags.ProhibitDeleteRow,
                    overwrite: true);

                approversSection.SetSectionPermissions(
                    CardPermissionFlags.ProhibitModify
                    | CardPermissionFlags.ProhibitInsertRow
                    | CardPermissionFlags.ProhibitDeleteRow,
                    overwrite: true);
            }

            var stagesRows = stagesSection.Rows;
            var approverRows = approversSection.Rows;
            if (card.Sections.TryGetValue(KrConstants.KrStages.Virtual, out var stagesDataSection))
            {
                foreach (var stage in stagesDataSection.Rows)
                {
                    var isInactiveStage = stage.Get<int>(KrConstants.KrStages.StateID) == KrStageState.Inactive.ID;
                    var canEditStage = krPermissions.Contains(KrPermissionFlagDescriptors.EditRoute)
                        && isInactiveStage;
                    //Если нет прав или этап активен или завершен - редактирование запрещено
                    stagesRows
                        .GetOrAdd(stage.RowID)
                        .SetRowPermissions(canEditStage
                                ? CardPermissionFlagValues.AllowAllRow
                                : CardPermissionFlagValues.ProhibitAllRow,
                            overwrite: true);

                    foreach (var approver in card.Sections[KrConstants.KrPerformersVirtual.Synthetic].Rows)
                    {
                        if (approver.Fields.Get<Guid>(KrConstants.KrPerformersVirtual.StageRowID) != stage.RowID)
                        {
                            continue;
                        }

                        approverRows
                            .GetOrAdd(approver.RowID)
                            .SetRowPermissions(canEditStage
                                    ? CardPermissionFlagValues.AllowAllRow
                                    : CardPermissionFlagValues.ProhibitAllRow,
                                overwrite: true);
                    }

                    if (isInactiveStage
                        && KrProcessSharedHelper.CanBeSkipped(stage))
                    {
                        CardPermissionFlags flags;
                        if (krPermissions.Contains(KrPermissionFlagDescriptors.CanSkipStages))
                        {
                            flags = stage.TryGet<bool>(KrConstants.KrStages.Skip)
                                ? CardPermissionFlags.ProhibitDeleteRow
                                : CardPermissionFlags.AllowDeleteRow;
                        }
                        else
                        {
                            flags = CardPermissionFlags.ProhibitDeleteRow;
                        }

                        stagesRows
                            .GetOrAdd(stage.RowID)
                            .SetRowPermissions(flags);
                    }
                }
            }

            //если сателлит еще не создан - состояние = драфт
            var state = (KrState?) card
                .TryGetSections()
                ?.TryGet(KrConstants.KrApprovalCommonInfo.Virtual)
                ?.TryGetRawFields()
                ?.TryGet<int?>(KrConstants.KrApprovalCommonInfo.StateID) ?? KrState.Draft;

            approversSection.SetSectionPermissions(
                krPermissions.Contains(KrPermissionFlagDescriptors.EditRoute)
                && state != KrState.Approved
                    ? CardPermissionFlags.AllowInsertRow
                    : CardPermissionFlags.ProhibitInsertRow);

            // Запуск процесса согласования проверяется непосредственно при попытке запуска процесса
            // Отзыв, возврат, отмена процесса согласования проверяется непосредственно при попытке

            //Даем право на удаление чтобы не скрывался тайл
            permissions.SetCardPermissions(CardPermissionFlags.AllowDeleteCard);
        }

        /// <summary>
        /// Устанавливает расширенные настройки прав доступа на карточку.
        /// </summary>
        /// <param name="card">Карточка.</param>
        /// <param name="extendedCardSettings">Расширенные настройки прав доступа.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        private async Task SetCardExtendedPermissionsAsync(
            Card card,
            HashSet<Guid, IKrPermissionSectionSettings> extendedCardSettings,
            CancellationToken cancellationToken = default)
        {
            var permissions = card.Permissions;
            var cardTypeMeta = await this.cardMetadata.GetMetadataForTypeAsync(card.TypeID, cancellationToken);
            var cardTypeMetaSections = await cardTypeMeta.GetSectionsAsync(cancellationToken);
            foreach (var sectionSettings in extendedCardSettings)
            {
                SetSectionPermission(card, permissions, cardTypeMetaSections, sectionSettings);
            }
        }

        /// <summary>
        /// Устанавливает права на секцию.
        /// </summary>
        /// <param name="card">Карточка.</param>
        /// <param name="permissions">Права на карточку.</param>
        /// <param name="cardMetadataSections">Метаданные секций.</param>
        /// <param name="sectionSettings">Расширенные настройки секции.</param>
        private static void SetSectionPermission(
            Card card,
            CardPermissionInfo permissions,
            CardMetadataSectionCollection cardMetadataSections,
            IKrPermissionSectionSettings sectionSettings)
        {
            if (cardMetadataSections.TryGetValue(sectionSettings.ID, out var sectionMeta)
                && card.TryGetSections() is { Count: > 0 } sections
                && sections.TryGetValue(sectionMeta.Name, out var section))
            {
                var sectionPermissions = permissions.Sections.GetOrAdd(sectionMeta.Name);
                sectionPermissions.Type = sectionMeta.SectionType;
                var isTable = sectionMeta.SectionType == CardSectionType.Table;

                // Запрет редактирования всей секции выше всего, остальное можно игнорировать сразу же
                if (sectionSettings.IsDisallowed)
                {
                    if (isTable)
                    {
                        foreach (var row in section.Rows)
                        {
                            var rowPermissions = sectionPermissions.Rows.GetOrAdd(row.RowID);
                            rowPermissions.SetRowPermissions(CardPermissionFlags.ProhibitModify);
                        }
                        SetChildSectionsDisallowed(
                            card,
                            permissions,
                            sectionMeta,
                            cardMetadataSections);
                    }
                    else
                    {
                        sectionPermissions.SetSectionPermissions(CardPermissionFlags.ProhibitModify);
                    }
                }
                else if (sectionSettings.IsAllowed)
                {
                    sectionPermissions.SetSectionPermissions(CardPermissionFlags.AllowModify);
                }

                if (isTable)
                {
                    if (sectionSettings.IsAllowed)
                    {
                        sectionPermissions.SetSectionPermissions(CardPermissionFlags.AllowInsertRow | CardPermissionFlags.AllowDeleteRow);
                    }
                    if (sectionSettings.DisallowRowAdding)
                    {
                        // Запрет на добавление строк также расширяет запрет на редактирование строк
                        sectionPermissions.SetSectionPermissions(CardPermissionFlags.ProhibitInsertRow);
                        if (sectionSettings.IsDisallowed)
                        {
                            sectionPermissions.SetSectionPermissions(CardPermissionFlags.ProhibitModify);
                        }
                    }
                    if (sectionSettings.DisallowRowDeleting)
                    {
                        sectionPermissions.SetSectionPermissions(CardPermissionFlags.ProhibitDeleteRow);
                    }
                }

                foreach (var field in sectionSettings.AllowedFields)
                {
                    SetFieldPermission(sectionMeta, sectionPermissions, field, CardPermissionFlags.AllowModify);
                }
                foreach (var field in sectionSettings.DisallowedFields)
                {
                    SetFieldPermission(sectionMeta, sectionPermissions, field, CardPermissionFlags.ProhibitModify);
                }
            }
        }

        /// <summary>
        /// Устанавливает запрет на редактирование дочерних секций.
        /// </summary>
        /// <param name="card">Карточка.</param>
        /// <param name="permissions">Права на карточку.</param>
        /// <param name="sectionMeta">Метаданные родительской секции.</param>
        /// <param name="cardMetadataSections">Метаданные секций.</param>
        private static void SetChildSectionsDisallowed(
            Card card,
            CardPermissionInfo permissions,
            CardMetadataSection sectionMeta,
            CardMetadataSectionCollection cardMetadataSections)
        {
            var childSections = new HashSet<string>();

            // Сначала собираем все дочерние секции, после чего каждую из них мы будем запрещать редактировать
            FindChildSections(
                card,
                permissions,
                sectionMeta,
                cardMetadataSections,
                childSections);

            foreach (var childSection in childSections)
            {
                var sectionPermissions = permissions.Sections.GetOrAdd(childSection);
                sectionPermissions.Type = CardSectionType.Table;
                if (card.Sections.TryGetValue(childSection, out var section))
                {
                    foreach (var row in section.Rows)
                    {
                        var rowPermission = sectionPermissions.Rows.GetOrAdd(row.RowID);
                        rowPermission.SetRowPermissions(CardPermissionFlags.ProhibitModify);
                    }
                }
            }

            static void FindChildSections(
                Card card,
                CardPermissionInfo permissions,
                CardMetadataSection sectionMeta,
                CardMetadataSectionCollection cardMetadataSections,
                HashSet<string> childSections)
            {
                foreach (var possibleChildSectionMeta in cardMetadataSections)
                {
                    // Предотвращаем зацикливание в случае если 2 секции ссылаются друг на друга за счёт проверки на то, проверяли ли мы эту секцию
                    if (!childSections.Contains(possibleChildSectionMeta.Name)
                        && possibleChildSectionMeta.SectionType == CardSectionType.Table
                        && possibleChildSectionMeta.Columns.Any(x => x.ColumnType == CardMetadataColumnType.Complex && x.ParentRowSection?.ID == sectionMeta.ID))
                    {
                        childSections.Add(possibleChildSectionMeta.Name);

                        FindChildSections(
                            card,
                            permissions,
                            possibleChildSectionMeta,
                            cardMetadataSections,
                            childSections);
                    }
                }
            }
        }

        /// <summary>
        /// Устанавливает права на поле секции с учетом комплексных полей.
        /// </summary>
        /// <param name="sectionMeta">Метаданные секции.</param>
        /// <param name="sectionPermissions">Права секции.</param>
        /// <param name="field">Идентификатор поля.</param>
        /// <param name="permissionFlags">Устанавливаемые права доступа.</param>
        private static void SetFieldPermission(
            CardMetadataSection sectionMeta,
            CardSectionPermissionInfo sectionPermissions,
            Guid field,
            CardPermissionFlags permissionFlags)
        {
            if (sectionMeta.Columns.TryGetValue(field, out var columnMeta))
            {
                if (columnMeta.ColumnType == CardMetadataColumnType.Complex)
                {
                    foreach (var refColumnMeta in sectionMeta.Columns
                        .Where(x => x.ColumnType == CardMetadataColumnType.Physical
                            && x.ComplexColumnIndex == columnMeta.ComplexColumnIndex))
                    {
                        sectionPermissions.SetFieldPermissions(refColumnMeta.Name, permissionFlags);
                    }
                }
                else
                {
                    sectionPermissions.SetFieldPermissions(columnMeta.Name, permissionFlags);
                }
            }
        }

        /// <summary>
        /// Сохраняет оригинальную карточку в токен.
        /// </summary>
        /// <param name="token">Токен прав доступа.</param>
        /// <param name="card">Карточка.</param>
        private void StoreOriginalSource(KrToken token, Card card)
        {
            var cardSource = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var section in card.Sections.Values)
            {
                if (this.permissionsManager.IgnoreSections.Contains(section.Name))
                {
                    continue;
                }

                if (section.Type == CardSectionType.Entry)
                {
                    Dictionary<string, object>? sectionSource = null;
                    foreach (var field in section.RawFields)
                    {
                        if (field.Value is not null)
                        {
                            if (sectionSource is null)
                            {
                                cardSource[section.Name] = sectionSource = new Dictionary<string, object>(StringComparer.Ordinal);
                            }

                            sectionSource[field.Key] = field.Value;
                        }
                    }
                }
                else
                {
                    List<object>? sectionSource = null;
                    foreach (var row in section.Rows)
                    {
                        Dictionary<string, object>? rowSource = null;
                        foreach (var field in row)
                        {
                            if (field.Value is not null)
                            {
                                rowSource ??= new Dictionary<string, object>(StringComparer.Ordinal);

                                rowSource[field.Key] = field.Value;
                            }
                        }
                        if (rowSource is not null)
                        {
                            if (sectionSource is null)
                            {
                                cardSource[section.Name] = sectionSource = new List<object>();
                            }
                            sectionSource.Add(rowSource);
                        }
                    }
                }
            }

            var fileSources = new List<object>();
            cardSource["Files"] = fileSources;
            foreach (var file in card.Files)
            {
                fileSources.Add(new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["ExternalSource"] = file.ExternalSource?.GetStorage(),
                    ["RowID"] = file.RowID,
                });
            }

            token.Info![KrPermissionsHelper.NewCardSourceKey] = cardSource;
        }

        #endregion

        #region Base Overrides New

        /// <inheritdoc/>
        public override async Task BeforeRequest(ICardNewExtensionContext context)
        {
            if (context.CardType is not { } cardType
                || cardType.InstanceType != CardInstanceType.Card
                || cardType.Flags.Has(CardTypeFlags.Singleton)
                || !context.ValidationResult.IsSuccessful()
                || context.Request is not { } request)
            {
                return;
            }

            Guid? docTypeID;

            if (context.Method == CardNewMethod.Template && request.TryGetTemplateCard() is { } templateCard)
            {
                KrProcessSharedHelper.TryGetDocTypeID(templateCard, out docTypeID);
            }
            else
            {
                // Если для типа карточки используются типы документов, тип документа должен быть указан.
                docTypeID = request.Info.TryGet<Guid?>(KrConstants.Keys.DocTypeID);
            }

            var permContextResult = await this.permissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    CardTypeID = cardType.ID,
                    DocTypeID = docTypeID,
                    WithExtendedPermissions = true,
                    ValidationResult = context.ValidationResult,
                    AdditionalInfo = context.Info,
                    PrevToken = KrToken.TryGet(request.Info),
                    ExtensionContext = context,
                    ServerToken = context.Info.TryGetServerToken(),
                    ServiceType = request.ServiceType,
                },
                cancellationToken: context.CancellationToken);

            if (permContextResult.Status == KrPermissionsCreateContextStatus.Success &&
                permContextResult.Context is { } permContext)
            {
                var result = await this.permissionsManager.GetEffectivePermissionsAsync(
                    permContext,
                    KrPermissionFlagDescriptors.CreateCard,
                    KrPermissionFlagDescriptors.FullCardPermissionsGroup);

                // Проверяем возможность создания карточки
                // если возможность создания дана, то даем все права на карточку, т.е. ничего не закрываем
                // после сохранения при обновлении карточка уже будет получаться в соответствии с указанными
                // правами
                if (!result.Permissions.Contains(KrPermissionFlagDescriptors.CreateCard)
                    && context.ValidationResult.IsSuccessful())
                {
                    await permContext.AddErrorAsync(this, "$KrMessages_HaveNoPermissionsToCreateCard", context.CancellationToken);
                    return;
                }

                context.Info[nameof(KrPermissionsNewGetExtension)] = result;

                if (permContext.DocTypeID.HasValue)
                {
                    context.Info[DocTypeIDKey] = permContext.DocTypeID;
                }
            }
        }

        ///<inheritdoc/>
        public override async Task AfterRequest(ICardNewExtensionContext context)
        {
            if (context.CardType is not { } cardType
                || cardType.InstanceType != CardInstanceType.Card
                || cardType.Flags.Has(CardTypeFlags.Singleton)
                || context.DbScope is not { } dbScope
                || !context.RequestIsSuccessful
                || context.Response is not { } response
                || !response.ValidationResult.IsSuccessful())
            {
                return;
            }

            var userId = context.Session.User.ID;
            var cancellationToken = context.CancellationToken;

            var card = response.Card;
            if (context.Info.TryGetValue(nameof(KrPermissionsNewGetExtension), out var obj)
                && obj is IKrPermissionsManagerResult result)
            {
                var docTypeID = context.Info.TryGet<Guid?>(DocTypeIDKey);
                await this.CalcPermissionCanFullRecalcRouteAsync(
                    card,
                    docTypeID,
                    result,
                    (CardTaskDialogStoreMode?) context.Request.TryGetInfo()?.TryGet<int?>(CardTaskDialogHelper.StoreMode),
                    true,
                    false,
                    context.ValidationResult,
                    cancellationToken);

                var extendedCardSettings = await result.CreateExtendedCardSettingsAsync(userId, card, cancellationToken);
                var fileSettings = GetFileSettings(extendedCardSettings);

                await this.SetCardPermissionsAsync(
                    userId,
                    card,
                    result,
                    fileSettings,
                    extendedCardSettings?.TryGetOwnFilesSettings(),
                    context.Info,
                    cancellationToken);

                // Создаем токен
                var token = this.krTokenProvider.CreateToken(
                    card,
                    result.Version,
                    result.Permissions,
                    extendedCardSettings,
                    (t) =>
                    {
                        this.StoreOriginalSource(t, card);
                        t.SetDocTypeID(docTypeID);
                        t.SetCardTypeID(card.TypeID);

                        if (result.SubmittedRules.Count > 0)
                        {
                            t.SubmittedRules.AddRange(result.SubmittedRules);
                        }

                        if (result.RejectedRules.Count > 0)
                        {
                            t.RejectedRules.AddRange(result.RejectedRules);
                        }
                    });

                //кладем токен в карточку
                token.Set(card.Info);
            }
        }

        private static HashSet<Guid, KrPermissionsFileSettings>? GetFileSettings(KrPermissionExtendedCardSettingsStorage extendedCardSettings)
        {
            if (extendedCardSettings is null)
            {
                return null;
            }

            var fileSettings = extendedCardSettings.TryGetFileSettings();

            return fileSettings is { Count: > 0 } ?
                new HashSet<Guid, KrPermissionsFileSettings>(x => x.FileID, fileSettings)
                : null;
        }

        /// <summary>
        /// Рассчитывает для указанной карточки возможность полного пересчёта процесса.
        /// </summary>
        /// <param name="card">Карточка для которой выполняется расчёт <see cref="KrPermissionFlagDescriptors.CanFullRecalcRoute"/>.</param>
        /// <param name="docTypeID">Идентификатор типа документа.</param>
        /// <param name="permissionsResult">Результат выполнения проверки прав доступа в <see cref="IKrPermissionsManager"/>.</param>
        /// <param name="storeMode">Режим сохранения карточки.</param>
        /// <param name="isNewRequest">Значение <see langword="true"/>, если расчёт прав выполняется в расширении на получение карточки, иначе - <see langword="false"/>.</param>
        /// <param name="isStoreKrSatelliteInKrScope">Значение <see langword="true"/>, если при загрузке карточки не надо сохранять информацию по основному сателлиту (<see cref="DefaultCardTypes.KrSatelliteTypeID"/>) карточки в <see cref="IKrScope"/>, иначе - <see langword="false"/>.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        private async ValueTask CalcPermissionCanFullRecalcRouteAsync(
            Card card,
            Guid? docTypeID,
            IKrPermissionsManagerResult permissionsResult,
            CardTaskDialogStoreMode? storeMode,
            bool isNewRequest,
            bool isStoreKrSatelliteInKrScope,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            if (permissionsResult.Has(KrPermissionFlagDescriptors.CanFullRecalcRoute)
                && (storeMode.HasValue && (isNewRequest || storeMode != CardTaskDialogStoreMode.Card)
                    || (await KrComponentsHelper.GetKrComponentsAsync(
                            card.TypeID,
                            docTypeID,
                            this.typesCache,
                            cancellationToken)).HasNot(KrComponents.Routes)
                    || !isNewRequest
                    && (await this.krScope.TryGetKrSatelliteAsync(
                            card.ID,
                            isStoreKrSatelliteInKrScope,
                            validationResult,
                            cancellationToken: cancellationToken))
                        ?.GetStagesSection()
                        .Rows
                        .All(static p =>
                            (p.TryGet<int?>(KrConstants.KrStages.StateID) ?? KrStageState.Inactive.ID) == KrStageState.Inactive) == false))
            {
                permissionsResult.Permissions.Remove(KrPermissionFlagDescriptors.CanFullRecalcRoute);
            }
        }

        #endregion

        #region Base Overrides Get

        /// <inheritdoc/>
        public override async Task AfterRequest(ICardGetExtensionContext context)
        {
            if (!context.RequestIsSuccessful
                || !context.ValidationResult.IsSuccessful()
                || context.CardType is null
                || context.Response is not { } response
                || response.TryGetCard() is not { } card
                || context.DbScope is not { } dbScope
                || context.Request is not { } request
                || !await KrComponentsHelper.HasBaseAsync(card.TypeID, this.typesCache, context.CancellationToken))
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var userId = context.Session.User.ID;

            if (context.Method == CardGetMethod.Default)
            {
                var needMessage = false;
                var requiredPermissions = new List<KrPermissionFlagDescriptor>();

                var calculateFullPermissions = request.Info.TryGet<bool>(KrPermissionsHelper.CalculatePermissionsMark);

                if (calculateFullPermissions)
                {
                    //Нужно рассчитать как бы все права, но на самом деле многие права (удаление/отмена и т.д.)
                    //нужны непосредственно при попытке совершения действия - поэтому будем проверять не все права
                    requiredPermissions.Add(KrPermissionFlagDescriptors.FullCardPermissionsGroup);

                    if (!request.Info.TryGet<bool>(KrPermissionsHelper.PermissionsCalculatedMark))
                    {
                        //Если пришел признак что права уже были рассчитаны по плитке "редактировать"
                        //- не будем отображать сообщение
                        needMessage = true;
                    }
                }
                else
                {
                    requiredPermissions.Add(KrPermissionFlagDescriptors.ReadCard);
                    if (request.Info.TryGet<bool>(KrPermissionsHelper.CalculateElevatedPermissions))
                    {
                        requiredPermissions.Add(KrPermissionFlagDescriptors.SuperModeratorMode);
                        requiredPermissions.Add(KrPermissionFlagDescriptors.EditAllMessages);
                    }

                    if (request.Info.TryGet<bool>(KrPermissionsHelper.CalculateEditMyMessagesPermissions))
                    {
                        requiredPermissions.Add(KrPermissionFlagDescriptors.EditMyMessages);
                    }

                    if (request.Info.TryGet<bool>(KrPermissionsHelper.CalculateAddTopicPermissions))
                    {
                        requiredPermissions.Add(KrPermissionFlagDescriptors.AddTopics);
                    }

                    if (request.Info.TryGet<bool>(KrPermissionsHelper.CalculateResolutionPermissionsMark))
                    {
                        //Нужно рассчитать как бы все права, но на самом деле многие права (удаление/отмена и т.д.)
                        //нужны непосредственно при попытке совершения действия - поэтому будем проверять не все права
                        requiredPermissions.Add(KrPermissionFlagDescriptors.CreateResolutions);
                    }

                    if (request.Info.TryGet<bool>(KrPermissionsHelper.CalculateTaskAssignedRolesPermissionsMark))
                    {
                        requiredPermissions.Add(KrPermissionFlagDescriptors.ModifyAllTaskAssignedRoles);
                        requiredPermissions.Add(KrPermissionFlagDescriptors.ModifyOwnTaskAssignedRoles);
                    }
                }

                var previousToken = KrToken.TryGet(request.Info);
                // Получаем расширенные настройки всегда, если запрос пришёл с клиента или если мы получили не серверный токен
                var withExtendedPermissions = request.ServiceType is CardServiceType.Client || previousToken is not { ServerOnly: true };
                var permContextResult = await this.permissionsManager.TryCreateContextAsync(
                    new KrPermissionsCreateContextParams
                    {
                        Card = card,
                        // Расчет обязательных прав и расширенных настроек на сервере не имеет смысла
                        WithRequiredPermissions = withExtendedPermissions,
                        WithExtendedPermissions = withExtendedPermissions,
                        ValidationResult = context.ValidationResult,
                        AdditionalInfo = context.Info,
                        PrevToken = previousToken,
                        ExtensionContext = context,
                        ServerToken = context.Info.TryGetServerToken(),
                        ServiceType = request.ServiceType,
                    },
                    cancellationToken: cancellationToken);

                // Или была ошибка, и тогда она записалась в context.ValidationResult
                // или карточка не относится к типовому решению
                if (permContextResult.Status != KrPermissionsCreateContextStatus.Success)
                {
                    return;
                }

                var permContext = permContextResult.Context!;
                IKrPermissionsManagerResult result;
                await using (dbScope.Create())
                {
                    result = await this.permissionsManager.GetEffectivePermissionsAsync(
                        permContext,
                        requiredPermissions.ToArray());

                    context.Info[nameof(KrPermissionsNewGetExtension)] = result;
                }

                //Если был запрос на полный расчет прав - отметим это в инфо карточки, чтобы не отображать
                //плитку "редактировать"
                if (calculateFullPermissions
                    //Или если все права были получены
                    || result.Has(KrPermissionFlagDescriptors.FullCardPermissionsGroup))
                {
                    card.Info[KrPermissionsHelper.PermissionsCalculatedMark] = true;
                }

                if (request.Info.TryGet<bool>(KrPermissionsHelper.CalculateElevatedPermissions)
                    || result.Has(KrPermissionFlagDescriptors.SuperModeratorMode)
                    && result.Has(KrPermissionFlagDescriptors.EditAllMessages))
                {
                    card.Info.Add(KrPermissionsHelper.ElevatedPermissionsCalculated, true);
                }

                if (request.Info.TryGet<bool>(KrPermissionsHelper.CalculateEditMyMessagesPermissions))
                {
                    card.Info.Add(KrPermissionsHelper.EditMyMessagesPermissionsCalculated, true);
                }

                if (request.Info.TryGet<bool>(KrPermissionsHelper.CalculateAddTopicPermissions)
                    || result.Has(KrPermissionFlagDescriptors.SuperModeratorMode)) // TODO тут по логике должно быть AddTopic
                {
                    card.Info.Add(KrPermissionsHelper.AddTopicPermissionsCalculated, true);
                }

                //Право на создание проверяется в соотв. расширении

                //Права на чтение карточки
                // Не пишем ошибку об отсутствии доступа на чтение, если есть другие ошибки
                if (!result.Permissions.Contains(KrPermissionFlagDescriptors.ReadCard)
                    && context.ValidationResult.IsSuccessful())
                {
                    await permContext.AddErrorAsync(this, "$KrMessages_HaveNoPermissionsToReadCard", cancellationToken);
                    return;
                }

                await this.CalcPermissionCanFullRecalcRouteAsync(
                    card,
                    permContext.DocTypeID,
                    result,
                    (CardTaskDialogStoreMode?)request.TryGetInfo()?.TryGet<int?>(CardTaskDialogHelper.StoreMode),
                    false,
                    !request.IsIgnoreStoreKrSatelliteInKrScope(),
                    context.ValidationResult,
                    cancellationToken);

                var extendedCardSettings = await result.CreateExtendedCardSettingsAsync(userId, card, cancellationToken);
                var fileSettings = GetFileSettings(extendedCardSettings);

                await this.SetCardPermissionsAsync(
                    userId,
                    card,
                    result,
                    fileSettings,
                    extendedCardSettings?.TryGetOwnFilesSettings(),
                    context.Info,
                    cancellationToken);

                //Если был запрос на полный расчет прав (по плитке "Редактировать") и не было дано
                //право на редактирование карточки, то, чтобы пользователь понял что расчет был -
                //отобразим информационное сообщение какие права были получены
                if (needMessage && !result.Permissions.Contains(KrPermissionFlagDescriptors.EditCard))
                {
                    var message = KrPermissionsHelper.GetGrantedPermissionsMessage(result.Permissions.ToArray());
                    context.ValidationResult.AddInfo(this, message);
                }

                // Создаем токен
                var token = this.krTokenProvider.CreateToken(
                    card,
                    result.Version,
                    result.Permissions,
                    extendedCardSettings,
                    t =>
                    {
                        t.SetDocTypeID(permContext.DocTypeID);
                        t.SetCardTypeID(permContext.CardType.ID);

                        if (result.SubmittedRules.Count > 0)
                        {
                            t.SubmittedRules.AddRange(result.SubmittedRules);
                        }

                        if (result.RejectedRules.Count > 0)
                        {
                            t.RejectedRules.AddRange(result.RejectedRules);
                        }
                    });

                //кладем токен в карточку
                token.Set(card.Info);
            }
            // Проверка экспорта не затирание данных при нем не выполняется для админов
            else if (context.Method == CardGetMethod.Export
                && !context.Session.User.IsAdministrator())
            {
                var permContextResult = await this.permissionsManager.TryCreateContextAsync(
                    new KrPermissionsCreateContextParams
                    {
                        Card = card,
                        // Состояние грузится напрямую через базу, т.к. в карточке при экспорте его нет
                        KrState = await KrProcessSharedHelper.GetKrStateAsync(card.ID, dbScope, cancellationToken),
                        WithExtendedPermissions = true,
                        ValidationResult = context.ValidationResult,
                        AdditionalInfo = context.Info,
                        ExtensionContext = context,
                        ServerToken = context.Info.TryGetServerToken(),
                        ServiceType = request.ServiceType,
                    },
                    cancellationToken: cancellationToken);

                // Или была ошибка, и тогда она записалась в context.ValidationResult
                // или карточка не относится к типовому решению
                if (permContextResult.Status != KrPermissionsCreateContextStatus.Success)
                {
                    return;
                }

                var permContext = permContextResult.Context!;
                IKrPermissionsManagerResult result;
                await using (dbScope.Create())
                {
                    result = await this.permissionsManager.GetEffectivePermissionsAsync(
                        permContext,
                        KrPermissionFlagDescriptors.ReadCard, KrPermissionFlagDescriptors.CreateTemplateAndCopy);
                    context.Info[nameof(KrPermissionsNewGetExtension)] = result;
                }

                // Если при расчете прав нет права на создание шаблона и копирование, то пишем ошибку
                if (result.Has(KrPermissionFlagDescriptors.CreateTemplateAndCopy))
                {
                    var extendedCardSettings = await result.CreateExtendedCardSettingsAsync(userId, card, cancellationToken);
                    var fileSettings = GetFileSettings(extendedCardSettings);

                    if (fileSettings is not null
                        && fileSettings.Count > 0)
                    {
                        var cardFiles = card.Files;
                        for (var i = cardFiles.Count - 1; i >= 0; i--)
                        {
                            var file = cardFiles[i];
                            if (fileSettings.TryGetItem(file.RowID, out var settings)
                                && settings.ReadAccessSetting <= KrPermissionsHelper.FileReadAccessSettings.ContentNotAvailable)
                            {
                                card.Files.RemoveAt(i);
                            }
                        }
                    }
                }
                else if (context.ValidationResult.IsSuccessful()) // Не пишем ошибку об отсутствии доступа, если есть другие ошибки
                {
                    await permContext.AddErrorAsync(
                        this,
                        KrPermissionsHelper.GetNotEnoughPermissionsErrorMessage(
                            KrPermissionFlagDescriptors.CreateTemplateAndCopy),
                        cancellationToken);
                }
            }
        }

        #endregion
    }
}
