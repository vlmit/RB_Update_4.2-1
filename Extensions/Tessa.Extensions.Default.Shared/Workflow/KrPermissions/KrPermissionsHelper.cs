#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Files;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.IO;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Shared.Workflow.KrPermissions
{
    /// <summary>
    /// Вспомогательные классы, свойства и методы для системы правил доступа.
    /// </summary>
    public static class KrPermissionsHelper
    {
        #region Nested Types

        /// <summary>
        /// Список настроек доступа к полям. Берется из таблицы KrPermissionRuleAccessSettings в схеме.
        /// </summary>
        public static class AccessSettings
        {
            public const int AllowEdit = 0;
            public const int DisallowEdit = 1;
            public const int DisallowRowAdding = 2;
            public const int DisallowRowDeleting = 3;
            public const int MaskData = 4;
            public const int DisallowRowEdit = 5;
        }

        /// <summary>
        /// Список типов проверки обязательности полей и секций.
        /// </summary>
        public static class MandatoryValidationType
        {
            public const int Always = 0;
            public const int OnTaskCompletion = 1;
            public const int WhenOneFieldFilled = 2;
        }

        /// <summary>
        /// Список типов элементов управления для настроек видимости.
        /// </summary>
        public static class ControlType
        {
            public const int Tab = 0;
            public const int Block = 1;
            public const int Control = 2;
        }

        /// <summary>
        /// Список настроек доступа на чтение файлов карточки.
        /// </summary>
        public static class FileReadAccessSettings
        {
            public const int FileNotAvailable = 0;
            public const int ContentNotAvailable = 1;
            public const int OnlyLastVersion = 2;
            public const int OnlyLastAndOwnVersions = 3;
            public const int AllVersions = 4;

            public const string InfoKey = StorageHelper.SystemKeyPrefix + nameof(FileReadAccessSettings);
        }

        /// <summary>
        /// Список настроек доступа на редактирование, удаление и подписание файлов карточки.
        /// </summary>
        public static class FileEditAccessSettings
        {
            public const int Disallowed = 0;
            public const int Allowed = 1;
        }

        /// <summary>
        /// Список правил проверки файлов в расширенных настроек доступа к файлам.
        /// </summary>
        public static class FileCheckRules
        {
            public const int AllFiles = 0;
            public const int FilesOfOtherUsers = 1;
            public const int OwnFiles = 2;
        }

        /// <summary>
        /// Тип действия, производимого над файлом, которое привело к ошибке.
        /// </summary>
        public enum KrPermissionsErrorAction
        {
            /// <summary>
            /// Добавление нового файла.
            /// </summary>
            AddFile = 0,

            /// <summary>
            /// Изменение файла.
            /// </summary>
            EditFile = 1,

            /// <summary>
            /// Замена файла.
            /// </summary>
            ReplaceFile = 2,

            /// <summary>
            /// Изменение категории файла.
            /// </summary>
            ChangeCategory = 3,
        }

        /// <summary>
        /// Тип ошибки, возникшей при проверке файла.
        /// </summary>
        public enum KrPermissionsErrorType
        {
            /// <summary>
            /// Действие запрещено.
            /// </summary>
            NotAllowed = 0,

            /// <summary>
            /// Ошибка размера файла.
            /// </summary>
            FileTooBig = 1,

            /// <summary>
            /// Превышено допустимое количество файлов.
            /// </summary>
            MaxFilesCountExceeded = 2,

            /// <summary>
            /// Отсутствует обязательный файл.
            /// </summary>
            MandatoryFileMissed = 3,
        }

        #endregion

        #region Consts

        public const string CalculateElevatedPermissions = "kr_calculate_elevated_permissions";

        public const string CalculateAddTopicPermissions = "kr_calculate_addtopic_permissions";

        public const string ElevatedPermissionsCalculated = "kr_elevated_permissions_calculated";

        public const string CalculateEditMyMessagesPermissions = "kr_calculate_editmymessages_permissions";

        public const string EditMyMessagesPermissionsCalculated = "kr_editmymessages_permissions_calculated";

        public const string AddTopicPermissionsCalculated = "kr_addtopic_permissions_calculated";

        public const string SaveWithPermissionsCalcFlag = StorageHelper.SystemKeyPrefix + "SaveWithPermissionsCalc";

        public const string CalculatePermissionsMark = "kr_calculate_permissions";

        public const string CalculateTaskAssignedRolesPermissionsMark = "kr_calculate_task_assigned_roles_permissions";

        public const string CalculateResolutionPermissionsMark = "kr_calculate_resolution_permissions";

        public const string PermissionsCalculatedMark = "kr_permissions_calculated";

        public const string UnavailableTypesKey = ".unavailableTypes";

        public const string NewCardSourceKey = StorageHelper.SystemKeyPrefix + "CardSource";

        public const string ServerTokenKey = StorageHelper.SystemKeyPrefix + "KrServerToken";

        public const string SystemTable = "KrPermissionsSystem";

        public const string DropPermissionsCacheMark = StorageHelper.SystemKeyPrefix + "DropPermissionsCache";

        public const string FailedMandatoryRulesKey = StorageHelper.SystemKeyPrefix + "FailedMandatoryRules";

        /// <summary>
        /// Ключ, по которому в <see cref="KrToken.Info"/> содержится идентификатор типа карточки. Тип значения: <c>T?</c>, где T - <see cref="Guid"/>.
        /// </summary>
        public const string CardTypeIDKey = StorageHelper.SystemKeyPrefix + "CardTypeID";

        /// <summary>
        /// Ключ, по которому в <see cref="KrToken.Info"/> содержится идентификатор типа документа. Тип значения: <see cref="Nullable{T}"/>, где T - <see cref="Guid"/>.
        /// </summary>
        public const string DocTypeIDKey = StorageHelper.SystemKeyPrefix + "DocTypeID";

        /// <summary>
        /// Идентификатор категории файла, используемый при проверке доступа к файлу в ситуации, когда у файла не задана категория.
        /// </summary>
        public static readonly Guid NoCategoryFilesCategoryID = Guid.Empty;

        /// <summary>
        /// Ключ, по которому хранится признак того, что в ответе на сохранение карточки нужно приложить токен для рефреша карточки.
        /// </summary>
        public const string AddRefreshTokenKey = StorageHelper.SystemKeyPrefix + " AddRefreshToken";

        /// <summary>
        /// Суффикс "Без категории".
        /// </summary>
        public const string WithoutCategorySuffix = "WithoutCategory";

        /// <summary>
        /// Суффикс "С категорией".
        /// </summary>
        public const string WithCategorySuffix = "WithCategory";

        #endregion

        #region Static Methods

        /// <summary>
        /// Возвращает список недоступных имен для создания эффективных (типы карточек, не использующие типы документов и типы документов) типов.
        /// </summary>
        /// <param name="cardRepository">Репозиторий карточек</param>
        /// <param name="krTypesCache">Кеш типов</param>
        /// <param name="includeHiddenTypes">Включать ли скрытые типы</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Возвращает список недоступных имен для создания эффективных типов.</returns>
        public static async Task<(IReadOnlyList<Guid>, ValidationResult)> GetUnavailableTypesAsync(
            ICardRepository cardRepository,
            IKrTypesCache krTypesCache,
            bool includeHiddenTypes = false,
            CancellationToken cancellationToken = default)
        {
            var request = new CardRequest { RequestType = DefaultRequestTypes.GetUnavailableTypes };
            var response = await cardRepository.RequestAsync(request, cancellationToken);

            if (!response.Info.TryGetValue(UnavailableTypesKey, out var unavailableTypesObj)
                || unavailableTypesObj is not IList unavailableTypes)
            {
                return (ImmutableList<Guid>.Empty, response.ValidationResult.Build());
            }

            var result = unavailableTypes.Cast<Guid>().ToList();

            if (!includeHiddenTypes)
            {
                foreach (var krType in await krTypesCache.GetTypesAsync(cancellationToken))
                {
                    if (krType.HideCreationButton && !result.Contains(krType.ID))
                    {
                        result.Add(krType.ID);
                    }
                }
            }

            return (result.AsReadOnly(), response.ValidationResult.Build());
        }

        /// <summary>
        /// Формирует сообщение об ошибке недостаточности прав.
        /// </summary>
        /// <param name="stillRequired">Список прав, которых не хватает</param>
        /// <returns>Сообщение об ошибке</returns>
        public static string GetNotEnoughPermissionsErrorMessage(params KrPermissionFlagDescriptor[] stillRequired)
        {
            return LocalizeName("KrMessages_NoPermissionsTo")
                + GetPermissionsSplittedByNewLineStartsWithNewLine(stillRequired);
        }

        /// <summary>
        /// Формирует сообщение о выданных правах.
        /// </summary>
        /// <param name="granted">Список выданных прав</param>
        /// <returns>Сообщение о выданных правах</returns>
        public static string GetGrantedPermissionsMessage(params KrPermissionFlagDescriptor[] granted)
        {
            return LocalizeName("KrMessages_PermissionsGranted")
                + GetPermissionsSplittedByNewLineStartsWithNewLine(granted);
        }

        /// <summary>
        /// Возвращает локализованные разрешения разделенные переводом на новую строку. Начинает новой строкой.
        /// </summary>
        /// <param name="permissions">Разрешения, которые нужно локализовать и перечислить.</param>
        /// <returns>Локализованные разрешения разделенные переводом на новую строку. Начинает новой строкой.</returns>
        public static string GetPermissionsSplittedByNewLineStartsWithNewLine(
            params KrPermissionFlagDescriptor[] permissions)
        {
            if (permissions.Length == 0)
            {
                return string.Empty;
            }
            else if (permissions.Length == 1)
            {
                return Environment.NewLine + Localize(permissions[0].Description);
            }

            var result = StringBuilderHelper.Acquire();

            var split = Environment.NewLine;

            foreach (var permission in permissions.OrderBy(x => x.Order))
            {
                result.Append(split).Append(Localize(permission.Description));
            }

            return result.ToStringAndRelease();
        }

        /// <summary>
        /// Задаёт информацию по идентификатору типа карточки в токене безопасности.
        /// </summary>
        /// <param name="krToken">Токен безопасности.</param>
        /// <param name="cardTypeID">Идентификатор типа карточки.</param>
        /// <exception cref="ArgumentNullException">Параметр <paramref name="krToken"/> имеет значение <see langword="null"/>.</exception>
        public static void SetCardTypeID(
            this KrToken krToken,
            Guid cardTypeID)
        {
            ThrowIfNull(krToken);

            krToken.Info ??= new Dictionary<string, object?>();
            krToken.Info[CardTypeIDKey] = cardTypeID;
        }

        /// <summary>
        /// Возвращает идентификатор типа карточки из токена безопасности.
        /// </summary>
        /// <param name="krToken">Токен безопасности.</param>
        /// <param name="cardTypeID">Идентификатор типа карточки или значение <see langword="null"/>, если токен его не содержит.</param>
        /// <returns>
        /// Значение <see langword="true"/>, если токен безопасности содержит идентификатор типа карточки, иначе - <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Параметр <paramref name="krToken"/> имеет значение <see langword="null"/>.</exception>
        public static bool TryGetCardTypeID(
            this KrToken krToken,
            out Guid? cardTypeID)
        {
            ThrowIfNull(krToken);

            if (krToken.Info is not null && krToken.Info.TryGetValue(CardTypeIDKey, out var cardTypeIDObj))
            {
                cardTypeID = (Guid?) cardTypeIDObj;
                return true;
            }

            cardTypeID = null;
            return false;
        }

        /// <summary>
        /// Задаёт информацию по идентификатору типа документа в токене безопасности.
        /// </summary>
        /// <param name="krToken">Токен безопасности.</param>
        /// <param name="docTypeID">Идентификатор типа документа или <c>null</c>, если известно, что для типа карточки нет типа документа.</param>
        /// <exception cref="ArgumentNullException">Параметр <paramref name="krToken"/> имеет значение <see langword="null"/>.</exception>
        public static void SetDocTypeID(
            this KrToken krToken,
            Guid? docTypeID)
        {
            ThrowIfNull(krToken);

            krToken.Info ??= new Dictionary<string, object?>();
            krToken.Info[DocTypeIDKey] = docTypeID;
        }

        /// <summary>
        /// Возвращает идентификатор типа документа из токена безопасности.
        /// </summary>
        /// <param name="krToken">Токен безопасности.</param>
        /// <param name="docTypeID">Идентификатор типа документа или значение <see langword="null"/>, если для карточки не используется тип документа или токен его не содержит.</param>
        /// <returns>
        /// Значение <see langword="true"/>, если токен безопасности содержит идентификатор типа документа, иначе - <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">Параметр <paramref name="krToken"/> имеет значение <see langword="null"/>.</exception>
        public static bool TryGetDocTypeID(
            this KrToken krToken,
            out Guid? docTypeID)
        {
            ThrowIfNull(krToken);

            if (krToken.Info is not null && krToken.Info.TryGetValue(DocTypeIDKey, out var docTypeIDObj))
            {
                docTypeID = (Guid?) docTypeIDObj;
                return true;
            }

            docTypeID = null;
            return false;
        }

        /// <summary>
        /// Метод для добавления ошибки доступа к файлу.
        /// </summary>
        /// <param name="validationResultBuilder">Билдер результата валидации, куда записывается ошибка.</param>
        /// <param name="validationObject">Объект валидации, указываемый в ошибке.</param>
        /// <param name="errorAction">Тип действия работы с файлом, который привёл к ошибке.</param>
        /// <param name="errorType">Тип ошибки.</param>
        /// <param name="fileName">Имя файла.</param>
        /// <param name="fileExtension">Расширение файла или <c>null</c>, если требуется определить расширение файла по его имени.</param>
        /// <param name="replacedFileName">Имя заменяемого файла. Используется, если <paramref name="errorAction"/> имеет значение <see cref="KrPermissionsErrorAction.ReplaceFile"/>.</param>
        /// <param name="categoryCaption">Текст категории файла или <c>null</c>, если файл без категории.</param>
        /// <param name="sizeLimit">Ограничение на размер файла в байтах. Используется, если <paramref name="errorType"/> имеет значение <see cref="KrPermissionsErrorType.FileTooBig"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Выбрасывает исключение данного типа, если значение параметра <paramref name="errorAction"/> или <paramref name="errorType"/> выходят за рамки перечисления.
        /// </exception>
        public static void AddFileValidationError(
            IValidationResultBuilder validationResultBuilder,
            object validationObject,
            KrPermissionsErrorAction errorAction,
            KrPermissionsErrorType errorType,
            string fileName,
            string? fileExtension = null,
            string? replacedFileName = null,
            string? categoryCaption = null,
            long? sizeLimit = null)
        {
            ThrowIfNullOrWhiteSpace(fileName);

            string messageSuffix = categoryCaption is null
                ? WithoutCategorySuffix
                : WithCategorySuffix;
            fileExtension ??= FileHelper.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
            var (formattedSizeLimit, sizeUnit) = sizeLimit is null
                ? (null, null)
                : (FormatSize(sizeLimit.Value, SizeUnit.Kilobytes), FormatUnit(SizeUnit.Kilobytes));

            switch (errorAction)
            {
                case KrPermissionsErrorAction.AddFile:
                    validationResultBuilder.AddError(
                        validationObject,
                        $"$KrPermissions_Messages_AddFile_{errorType}_{messageSuffix}",
                        fileName,
                        fileExtension,
                        categoryCaption,
                        formattedSizeLimit,
                        sizeUnit);
                    break;

                case KrPermissionsErrorAction.EditFile:
                    validationResultBuilder.AddError(
                        validationObject,
                        $"$KrPermissions_Messages_EditFile_{errorType}_{messageSuffix}",
                        fileName,
                        fileExtension,
                        categoryCaption,
                        formattedSizeLimit,
                        sizeUnit);
                    break;

                case KrPermissionsErrorAction.ReplaceFile:
                    validationResultBuilder.AddError(
                        validationObject,
                        $"$KrPermissions_Messages_ReplaceFile_{errorType}_{messageSuffix}",
                        replacedFileName,
                        fileName,
                        fileExtension,
                        categoryCaption,
                        formattedSizeLimit,
                        sizeUnit);
                    break;

                case KrPermissionsErrorAction.ChangeCategory:
                    validationResultBuilder.AddError(
                        validationObject,
                        $"$KrPermissions_Messages_ChangeCategory_{errorType}_{messageSuffix}",
                        fileName,
                        fileExtension,
                        categoryCaption,
                        formattedSizeLimit,
                        sizeUnit);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(errorAction));
            }
        }

        /// <summary>
        /// Метод для добавления ошибки доступа к файлу при проверке допустимого количества файлов или отсутствия обязательных файлов.
        /// </summary>
        /// <param name="validationResultBuilder"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="validationObject">Объект валидации, указываемый в ошибке.</param>
        /// <param name="errorType">Тип ошибки.</param>
        /// <param name="extensions">Расширения файлов, которые затрагивает правило.</param>
        /// <param name="fileCheckRule">Правило проверки файлов (собственный файл, файл других сотрудников или все).</param>
        /// <param name="categoryIDs">Идентификаторы категорий файлов, которые затрагивает правило или <c>null</c>, если задан параметр <paramref name="categoryNames"/>.</param>
        /// <param name="categoryNames">Наименования категорий файлов, которые затрагивает правило или <c>null</c>.</param>
        /// <param name="maxCount">Допустимое количество файлов, для правила или null, если ограничение не задано.</param>
        /// <param name="dbScope">
        /// Объект <see cref="IDbScope"/> или <c>null</c>. Если этот параметр передан, но не передан <paramref name="categoryNames"/>,
        /// то будет произведена попытка получить наименования категорий по их идентификаторам.
        /// </param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        public static async ValueTask AddMaxFileCountExceededOrMandatoryErrorAsync(
            IValidationResultBuilder validationResultBuilder,
            object validationObject,
            KrPermissionsErrorType errorType,
            ICollection<string>? extensions,
            int fileCheckRule,
            ICollection<Guid>? categoryIDs,
            ICollection<string>? categoryNames,
            int? maxCount,
            IDbScope? dbScope,
            CancellationToken cancellationToken = default)
        {
            // Основной текст.
            var sb = new StringBuilder(await LocalizeAsync($"$KrPermissions_Messages_{errorType}"));

            string? categoriesStr = null;
            bool withoutCategoryOnly = false;

            // Если напрямую переданы имена категорий, то используем их.
            if (categoryNames is not null)
            {
                categoriesStr = string.Join(", ", categoryNames.Select(x => $"\"{Localize(x)}\""));
            }
            else if (categoryIDs is not null && categoryIDs.Count > 0)
            {
                // Если задана только одна категория "( Без категории )".
                if (categoryIDs.Count == 1 && categoryIDs.First() == Guid.Empty)
                {
                    categoriesStr = await LocalizeAsync("$KrPermissions_Messages_WithoutCategory");
                    withoutCategoryOnly = true;
                }
                else
                {
                    // При наличии dbScope, используем его для получения названий категорий.
                    if (dbScope is not null)
                    {
                        var categories = await TryGetFileCategoriesAsync(categoryIDs, dbScope, cancellationToken);

                        categoriesStr = string.Join(", ", categoryIDs.Select(x =>
                        {
                            var category = categories.FirstOrDefault(c => c.ID == x);
                            return category is not null
                                ? $"\"{Localize(category.Caption)}\""
                                : x == Guid.Empty
                                    ? Localize("$UI_Cards_FileNoCategory")
                                    : $"\"{x}\"";
                        }));
                    }
                    else
                    {
                        categoriesStr = string.Join(", ", categoryIDs.Select(x => $"\"{x}\""));
                    }
                }
            }

            // c расширениями: {0}
            if (extensions is { Count: > 0 })
            {
                sb.Append(' ');
                sb.Append(await LocalizeFormatAsync("$KrPermissions_Messages_ForExtensions", string.Join(", ", extensions.Select(x => $"\"{x}\""))));
            }
                
            // в категориях: {0}
            if (!string.IsNullOrEmpty(categoriesStr))
            {
                sb.Append(extensions is not { Count: > 0 } || withoutCategoryOnly ? " " : ", ");
                if (withoutCategoryOnly)
                {
                    sb.Append(categoriesStr);
                }
                else
                {
                    sb.Append(await LocalizeFormatAsync("$KrPermissions_Messages_InCategories", categoriesStr));
                }
            }

            // Точка после основного текста.
            sb.Append('.');
            
            // Ограничение количества файлов.
            if (errorType == KrPermissionsErrorType.MaxFilesCountExceeded && maxCount.HasValue)
            {
                sb.Append(' ');
                sb.Append(await LocalizeFormatAsync("$KrPermissions_Messages_AllowedNumberOfFiles", maxCount));
            }

            // Правило проверки файлов.
            var fileCheckRuleStr = fileCheckRule switch
            {
                FileCheckRules.AllFiles => null,
                FileCheckRules.FilesOfOtherUsers => await LocalizeAsync("$KrPermissions_FileCheckRules_FilesOfOtherUsers"),
                FileCheckRules.OwnFiles => await LocalizeAsync("$KrPermissions_FileCheckRules_OwnFiles"),
                _ => throw new ArgumentOutOfRangeException(nameof(fileCheckRule), fileCheckRule, null),
            };

            if (!string.IsNullOrEmpty(fileCheckRuleStr))
            {
                sb.Append(' ');
                sb.Append(await LocalizeFormatAsync("$KrPermissions_Messages_FileCheckRule", fileCheckRuleStr));
            }

            validationResultBuilder.AddError(validationObject, sb.ToString());
        }

        #endregion

        #region Private Methods

        private static async Task<IList<IFileCategory>> TryGetFileCategoriesAsync(ICollection<Guid> categoryIDs, IDbScope dbScope, CancellationToken cancellationToken)
        {
            var result = new List<IFileCategory>(categoryIDs.Count);

            await using var instance = dbScope.Create();
            var db = dbScope.Db;
            var builderFactory = dbScope.BuilderFactory;
            var command =
                db.SetCommand(builderFactory
                        .Select().C(null, "ID", "Name")
                        .From("FileCategories").NoLock()
                        .Where().C("ID").In(categoryIDs.ToArray())
                        .Build())
                    .LogCommand();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new FileCategory(
                    reader.GetGuid(0),
                    reader.GetString(1)));
            }

            return result;
        }

        #endregion
    }
}
