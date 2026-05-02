#nullable enable
using System;
using System.Collections.Generic;
using Tessa.Cards;
using Tessa.Platform.Collections;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.KrPermissions
{
    /// <summary>
    /// Объект с расширенными настройками доступа к карточке, записывающий настройки в хранилище <c>Dictionary&lt;string, object&gt;</c>.
    /// </summary>
    [StorageObjectGenerator]
    public sealed partial class KrPermissionExtendedCardSettingsStorage : CardStorageObject
    {
        #region Fields

        private static readonly IStorageValueFactory<int, KrPermissionSectionSettingsStorage> cardSettingsFactory =
            new DictionaryStorageValueFactory<int, KrPermissionSectionSettingsStorage>((index, storage) => new KrPermissionSectionSettingsStorage(storage));

        private static readonly IStorageValueFactory<int, ListStorage<KrPermissionSectionSettingsStorage>> taskSettingsFactory =
            new ListStorageValueFactory<int, ListStorage<KrPermissionSectionSettingsStorage>>((index, storage) =>
                new ListStorage<KrPermissionSectionSettingsStorage>(storage, cardSettingsFactory));

        private static readonly IStorageValueFactory<int, KrPermissionsFileSettings> fileSettingsFactory =
            new DictionaryStorageValueFactory<int, KrPermissionsFileSettings>((index, storage) => new KrPermissionsFileSettings(storage));

        private static readonly IStorageValueFactory<int, KrPermissionVisibilitySettings> visibilitySettingsFactory =
            new DictionaryStorageValueFactory<int, KrPermissionVisibilitySettings>((index, storage) => new KrPermissionVisibilitySettings(storage));

        private readonly IStorageValueFactory<int, KrPermissionsFileConstraints> fileConstraintsValueFactory =
            new DictionaryStorageValueFactory<int, KrPermissionsFileConstraints>((key, storage) => new KrPermissionsFileConstraints(storage));

        #endregion

        #region Constructors

        public KrPermissionExtendedCardSettingsStorage(Dictionary<string, object?> storage)
            : base(storage)
        {
            this.Init(nameof(this.SectionSettings), null);
            this.Init(nameof(this.TaskSettings), null);
            this.Init(nameof(this.TaskSettingsTypes), null);
            this.Init(nameof(this.VisibilitySettings), null);
            this.Init(nameof(this.FileSettings), null);
            this.Init(nameof(this.OwnFilesSettings), null);
            this.Init(nameof(this.OtherFilesSettings), null);
            this.Init(nameof(this.FileConstraints), null);
        }

        #endregion

        #region Storage Properties

        /// <summary>
        /// Расширенные настройки доступа к секциям карточки.
        /// </summary>
        public ListStorage<KrPermissionSectionSettingsStorage> SectionSettings
        {
            get
            {
                return this.GetList(
                    nameof(this.SectionSettings),
                    x => new ListStorage<KrPermissionSectionSettingsStorage>(x, cardSettingsFactory));
            }
            set => this.SetStorageValue(nameof(this.SectionSettings), value);
        }

        /// <summary>
        /// Расширенные настройки доступа к секциям заданий.
        /// </summary>
        public ListStorage<ListStorage<KrPermissionSectionSettingsStorage>> TaskSettings
        {
            get
            {
                return this.GetList(
                    nameof(this.TaskSettings),
                    x => new ListStorage<ListStorage<KrPermissionSectionSettingsStorage>>(x, taskSettingsFactory));
            }
            set => this.SetStorageValue(nameof(this.TaskSettings), value);
        }

        /// <summary>
        /// Типы заданий, для которых передаются расширенные настройки заданий.
        /// </summary>
        public ListStorage<Guid> TaskSettingsTypes
        {
            get => this.GetList(nameof(this.TaskSettingsTypes), x => new ListStorage<Guid>(x));
            set => this.Set(nameof(this.TaskSettingsTypes), value);
        }

        /// <summary>
        /// Настройки видимости контролов, блоков и вкладок.
        /// </summary>
        public ListStorage<KrPermissionVisibilitySettings> VisibilitySettings
        {
            get => this.GetList(
                nameof(this.VisibilitySettings),
                x => new ListStorage<KrPermissionVisibilitySettings>(x, visibilitySettingsFactory));
            set => this.Set(nameof(this.VisibilitySettings), value);
        }

        /// <summary>
        /// Настройки доступа к файлам.
        /// </summary>
        public ListStorage<KrPermissionsFileSettings> FileSettings
        {
            get
            {
                return this.GetList(
                    nameof(this.FileSettings),
                    x => new ListStorage<KrPermissionsFileSettings>(x, fileSettingsFactory));
            }
            set => this.SetStorageValue(nameof(this.FileSettings), value);
        }

        /// <summary>
        /// Настройки доступа создания и замены файлов текущего пользователя.
        /// </summary>
        public KrPermissionsFilesSettings OwnFilesSettings
        {
            get { return this.GetDictionary(nameof(this.OwnFilesSettings), static storage => new KrPermissionsFilesSettings(storage)); }
            set => this.SetStorageValue(nameof(this.OwnFilesSettings), value);
        }

        /// <summary>
        /// Настройки доступа замены файлов других пользователей.
        /// </summary>
        public KrPermissionsFilesSettings OtherFilesSettings
        {
            get { return this.GetDictionary(nameof(this.OtherFilesSettings), static (storage) => new KrPermissionsFilesSettings(storage)); }
            set => this.SetStorageValue(nameof(this.OtherFilesSettings), value);
        }


        /// <summary>
        /// Список настроек с ограничениями файлов. Создаёт пустой список, если он не задан.
        /// </summary>
        public ListStorage<KrPermissionsFileConstraints> FileConstraints
        {
            get => this.GetList(nameof(this.FileConstraints), x => new ListStorage<KrPermissionsFileConstraints>(x, this.fileConstraintsValueFactory));
            set => this.SetStorageValue(nameof(this.FileConstraints), value);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Метод для получения расширенных настроек для секций заданий по типам заданий.
        /// </summary>
        /// <returns>Списки расширенных настроек для секций заданий по типам заданий или <c>null</c>, если настройки не заданы.</returns>
        public Dictionary<Guid, IReadOnlyCollection<IKrPermissionSectionSettings>>? TryGetTasksSettings()
        {
            if (this.TryGetTaskSettings() is { Count: > 0 } taskSettings
                && this.TryGetTaskSettingsTypes() is { Count: > 0 } taskTypes
                && taskSettings.Count == taskTypes.Count)
            {
                var result = new Dictionary<Guid, IReadOnlyCollection<IKrPermissionSectionSettings>>();
                for (var i = 0; i < taskSettings.Count; i++)
                {
                    var settingsTypeID = taskTypes[i];
                    result[settingsTypeID] = taskSettings[i];
                }

                return result;
            }

            return null;
        }

        /// <summary>
        /// Возвращает расширенные настройки доступа к секциям карточки или <c>null</c>, если настройки не были заданы.
        /// </summary>
        /// <returns>Расширенные настройки доступа к секциям карточки или <c>null</c>, если настройки не были заданы.</returns>
        public ListStorage<KrPermissionSectionSettingsStorage>? TryGetSectionSettings() =>
            this.TryGetList(nameof(this.SectionSettings), x => new ListStorage<KrPermissionSectionSettingsStorage>(x, cardSettingsFactory));

        /// <summary>
        /// Возвращает расширенные настройки доступа к секциям заданий или <c>null</c>, если настройки не были заданы.
        /// </summary>
        /// <returns>Расширенные настройки доступа к секциям заданий или <c>null</c>, если настройки не были заданы.</returns>
        public ListStorage<ListStorage<KrPermissionSectionSettingsStorage>>? TryGetTaskSettings() =>
            this.TryGetList(nameof(this.TaskSettings), x => new ListStorage<ListStorage<KrPermissionSectionSettingsStorage>>(x, taskSettingsFactory));

        /// <summary>
        /// Возвращает список типов заданий, для которых передаются расширенные настройки заданий, или <c>null</c>, если типы заданий не были заданы.
        /// </summary>
        /// <returns>Список типов заданий, для которых передаются расширенные настройки заданий, или <c>null</c>, если типы заданий не были заданы.</returns>
        public ListStorage<Guid>? TryGetTaskSettingsTypes() =>
            this.TryGetList(nameof(this.TaskSettingsTypes), x => new ListStorage<Guid>(x));

        /// <summary>
        /// Возвращает список настроек видимости или <c>null</c>, если настройки видимости не были заданы.
        /// </summary>
        /// <returns>Список настроек видимости или <c>null</c>, если настройки видимости не были заданы.</returns>
        public ListStorage<KrPermissionVisibilitySettings>? TryGetVisibilitySettings() =>
            this.TryGetList(nameof(this.VisibilitySettings), x => new ListStorage<KrPermissionVisibilitySettings>(x, visibilitySettingsFactory));

        /// <summary>
        /// Возвращает список настроек доступа к файлам карточки или <c>null</c>, если настройки доступа к файлам не были заданы.
        /// </summary>
        /// <returns>Список настроек доступа к файлам карточки или <c>null</c>, если настройки доступа к файлам не были заданы.</returns>
        public ListStorage<KrPermissionsFileSettings>? TryGetFileSettings() =>
            this.TryGetList(nameof(this.FileSettings), x => new ListStorage<KrPermissionsFileSettings>(x, fileSettingsFactory));

        /// <summary>
        /// Возвращает настройки доступа к собственным файлам карточки или <c>null</c>, если настройки не были заданы.
        /// </summary>
        /// <returns>Настройки доступа к собственным файлам карточки или <c>null</c>, если настройки не были заданы.</returns>
        public KrPermissionsFilesSettings? TryGetOwnFilesSettings() =>
            this.TryGetDictionary(nameof(this.OwnFilesSettings), static storage => new KrPermissionsFilesSettings(storage));

        /// <summary>
        /// Возвращает настройки доступа к файлам других сотрудников или <c>null</c>, если настройки не были заданы.
        /// </summary>
        /// <returns>Настройки доступа к файлам других сотрудников или <c>null</c>, если настройки не были заданы.</returns>
        public KrPermissionsFilesSettings? TryGetOtherFilesSettings() =>
            this.TryGetDictionary(nameof(this.OtherFilesSettings), static storage => new KrPermissionsFilesSettings(storage));

        /// <summary>
        /// Метод для получения списка настроек с ограничениями файлов.
        /// </summary>
        /// <returns>Список настроек с ограничениями файлов или <c>null</c>, если список не задан.</returns>
        public ListStorage<KrPermissionsFileConstraints>? TryGetFileConstraints()
            => this.TryGetList(nameof(this.FileConstraints), x => new ListStorage<KrPermissionsFileConstraints>(x, this.fileConstraintsValueFactory));

        /// <summary>
        /// Возвращает признак того, что объект не содержит значимых данных, т.е. является пустым.
        /// </summary>
        /// <returns><c>true</c>, если объект является пустым; <c>false</c> в противном случае.</returns>
        public bool IsEmpty() =>
            this.TryGetSectionSettings() is not { Count: not 0 }
            && this.TryGetTaskSettings() is not { Count: not 0 }
            && this.TryGetTaskSettingsTypes() is not { Count: not 0 }
            && this.TryGetVisibilitySettings() is not { Count: not 0 }
            && this.TryGetFileSettings() is not { Count: not 0 }
            && this.TryGetOwnFilesSettings()?.IsEmpty() is not false
            && this.TryGetOtherFilesSettings()?.IsEmpty() is not false
            && this.TryGetFileConstraints() is not { Count: not 0 };

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override void Clean()
        {
            base.Clean();

            this.TryGetSectionSettings()?.ForEach(static x => x.Clean());
            this.SetNullIfEmptyCollection(nameof(this.SectionSettings));

            this.TryGetTaskSettings()?.ForEach(static x => x.ForEach(static y => y.Clean()));
            this.SetNullIfEmptyCollection(nameof(this.TaskSettings));

            this.SetNullIfEmptyCollection(nameof(this.TaskSettingsTypes));

            this.TryGetVisibilitySettings()?.ForEach(static x => x.Clean());
            this.SetNullIfEmptyCollection(nameof(this.VisibilitySettings));

            this.TryGetFileSettings()?.ForEach(static x => x.Clean());
            this.SetNullIfEmptyCollection(nameof(this.FileSettings));

            if (this.TryGetOwnFilesSettings() is { } ownFilesSettings)
            {
                ownFilesSettings.Clean();
                if (ownFilesSettings.IsEmpty())
                {
                    this.SetNull(nameof(this.OwnFilesSettings));
                }
            }

            if (this.TryGetOtherFilesSettings() is { } otherFilesSettings)
            {
                otherFilesSettings.Clean();
                if (otherFilesSettings.IsEmpty())
                {
                    this.SetNull(nameof(this.OtherFilesSettings));
                }
            }

            this.TryGetFileConstraints()?.ForEach(static x => x.Clean());
            this.SetNullIfEmptyCollection(nameof(this.FileConstraints));
        }

        #endregion
    }
}
