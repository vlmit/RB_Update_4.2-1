#nullable enable

using System.Collections.Generic;
using Tessa.Cards;
using Tessa.Platform.Collections;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.KrPermissions
{
    /// <summary>
    /// Расширенные настройки доступа файлов.
    /// </summary>
    [StorageObjectGenerator]
    public sealed partial class KrPermissionsFilesSettings : CardStorageObject
    {
        #region Fields

        private static readonly IStorageValueFactory<string, KrPermissionsFileExtensionSettings> extensionSettingsFactory =
            new DictionaryStorageValueFactory<string, KrPermissionsFileExtensionSettings>((key, storage) => new KrPermissionsFileExtensionSettings(key, storage));

        #endregion

        #region Constructors

        /// <doc path='info[@type="StorageObject" and @item=".ctor:storage"]'/>
        public KrPermissionsFilesSettings(Dictionary<string, object?> storage)
            : base(storage)
        {
            this.Init(nameof(this.GlobalSettings), null);
            this.Init(nameof(this.ExtensionSettings), null);
        }

        #endregion

        #region Storage Properties

        /// <summary>
        /// Настройки доступа файлов для всех расширений.
        /// </summary>
        public KrPermissionsFileExtensionSettings GlobalSettings
        {
            get => this.GetDictionary(nameof(this.GlobalSettings), s => new KrPermissionsFileExtensionSettings(string.Empty, s));
            set => this.SetStorageValue(nameof(this.GlobalSettings), value);
        }

        /// <summary>
        /// Настройки доступа файлов для конкретных расширений.
        /// </summary>
        public StringDictionaryStorage<KrPermissionsFileExtensionSettings> ExtensionSettings
        {
            get => this.GetDictionary(
                nameof(this.ExtensionSettings),
                x => new StringDictionaryStorage<KrPermissionsFileExtensionSettings>(x, extensionSettingsFactory));
            set => this.SetStorageValue(nameof(this.ExtensionSettings), value);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Возвращает настройки доступа файлов для всех расширений или <c>null</c>, если настройки для всех расширений не заданы.
        /// </summary>
        /// <returns>
        /// Настройки доступа файлов для всех расширений или <c>null</c>, если настройки для всех расширений не заданы.
        /// </returns>
        public KrPermissionsFileExtensionSettings? TryGetGlobalSettings() =>
            this.TryGetDictionary(nameof(this.GlobalSettings), s => new KrPermissionsFileExtensionSettings(string.Empty, s));

        /// <summary>
        /// Возвращает настройки доступа файлов для конкретных расширений или <c>null</c>, если настройки для конкретных расширений не заданы.
        /// </summary>
        /// <returns>
        /// Настройки доступа файлов для конкретных расширений или <c>null</c>, если настройки для конкретных расширений не заданы.
        /// </returns>
        public StringDictionaryStorage<KrPermissionsFileExtensionSettings>? TryGetExtensionSettings() =>
            this.TryGetDictionary(
                nameof(this.ExtensionSettings),
                x => new StringDictionaryStorage<KrPermissionsFileExtensionSettings>(x, extensionSettingsFactory));

        /// <summary>
        /// Возвращает признак того, что объект не содержит значимых данных, т.е. является пустым.
        /// </summary>
        /// <returns><c>true</c>, если объект является пустым; <c>false</c> в противном случае.</returns>
        public bool IsEmpty() =>
            this.TryGetGlobalSettings()?.IsEmpty() is not false
            && this.TryGetExtensionSettings() is not { Count: not 0 };

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override void Clean()
        {
            base.Clean();

            if (this.TryGetGlobalSettings() is { } globalSettings)
            {
                globalSettings.Clean();
                if (globalSettings.IsEmpty())
                {
                    this.SetNull(nameof(this.GlobalSettings));
                }
            }

            this.TryGetExtensionSettings()?.ForEach(static x => x.Value.Clean());
            this.SetNullIfEmptyCollection(nameof(this.ExtensionSettings));
        }

        #endregion
    }
}
