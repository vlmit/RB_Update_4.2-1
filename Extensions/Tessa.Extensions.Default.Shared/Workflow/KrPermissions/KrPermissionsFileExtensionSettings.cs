#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Cards;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.KrPermissions
{
    /// <summary>
    /// Настройки доступа новых файлов для конкретного расширения.
    /// </summary>
    [StorageObjectGenerator(GenerateDefaultConstructor = false)]
    public sealed partial class KrPermissionsFileExtensionSettings : CardStorageObject
    {
        #region Constructors

        /// <summary>
        /// Создаёт экземпляр настроек доступа на добавление файлов для конкретного расширения.
        /// </summary>
        /// <param name="extension">Расширение файлов.</param>
        public KrPermissionsFileExtensionSettings(string extension)
            : this(extension, new Dictionary<string, object?>(StringComparer.Ordinal))
        {
        }

        /// <summary>
        /// Создаёт экземпляр настроек доступа на добавление файлов для конкретного расширения.
        /// </summary>
        /// <param name="extension">Расширение файлов.</param>
        /// <param name="storage">Хранилище с данными настроек доступа к файлам.</param>
        public KrPermissionsFileExtensionSettings(string extension, Dictionary<string, object?> storage)
            : base(storage)
        {
            this.Extension = NotNullOrThrow(extension);

            this.Init(nameof(this.Flag), Int32Boxes.Zero);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Расширение, для которого применяется настройка.
        /// </summary>
        public string Extension { get; }

        /// <summary>
        /// Определяет, что добавление файлов для данного расширения файлов разрешено для всех категорий, кроме <see cref="DisallowedCategories"/>. 
        /// </summary>
        public bool AddAllowed => this.Flag.Has(KrPermissionsFilesAccessFlag.AddAllowed);

        /// <summary>
        /// Определяет, что добавление файлов для данного расширения файлов запрещено для всех категорий, кроме <see cref="AllowedCategories"/>. 
        /// </summary>
        public bool AddDisallowed => this.Flag.Has(KrPermissionsFilesAccessFlag.AddProhibited);

        /// <summary>
        /// Определяет, что подписание файлов для данного расширения файлов разрешено для всех категорий, кроме <see cref="SignDisallowedCategories"/>. 
        /// </summary>
        public bool SignAllowed => this.Flag.Has(KrPermissionsFilesAccessFlag.SignAllowed);

        /// <summary>
        /// Определяет, что подписание файлов для данного расширения файлов запрещено для всех категорий, кроме <see cref="SignAllowedCategories"/>. 
        /// </summary>
        public bool SignDisallowed => this.Flag.Has(KrPermissionsFilesAccessFlag.SignProhibited);

        #endregion

        #region Storage Properties

        /// <summary>
        /// Флаг доступа новых файлов.
        /// </summary>
        public KrPermissionsFilesAccessFlag Flag
        {
            get => (KrPermissionsFilesAccessFlag) this.Get<int>(nameof(this.Flag));
            set => this.Set(nameof(this.Flag), Int32Boxes.Box((int) value));
        }

        /// <summary>
        /// Список категорий, доступных для использования при добавлении файлов. Может иметь значение <c>null</c>, тогда доступны все категории,
        /// если <see cref="AddAllowed"/> имеет значение <c>true</c>.
        /// </summary>
        public IList<Guid>? AllowedCategories
        {
            get => this.TryGet<IList<Guid>>(nameof(this.AllowedCategories));
            set => this.Set(nameof(this.AllowedCategories), value);
        }

        /// <summary>
        /// Список категорий, недоступных для использования при добавлении файлов. Может иметь значение <c>null</c>, тогда недоступны все категории,
        /// если <see cref="AddDisallowed"/> имеет значение <c>true</c>.
        /// </summary>
        public IList<Guid>? DisallowedCategories
        {
            get => this.TryGet<IList<Guid>>(nameof(this.DisallowedCategories));
            set => this.Set(nameof(this.DisallowedCategories), value);
        }

        /// <summary>
        /// Список категорий, доступных для использования при подписании файлов. Может иметь значение <c>null</c>, тогда доступны все категории,
        /// если <see cref="SignAllowed"/> имеет значение <c>true</c>.
        /// </summary>
        public IList<Guid>? SignAllowedCategories
        {
            get => this.TryGet<IList<Guid>>(nameof(this.SignAllowedCategories));
            set => this.Set(nameof(this.SignAllowedCategories), value);
        }

        /// <summary>
        /// Список категорий, недоступных для использования при подписании файлов. Может иметь значение <c>null</c>, тогда недоступны все категории,
        /// если <see cref="SignDisallowed"/> имеет значение <c>true</c>.
        /// </summary>
        public IList<Guid>? SignDisallowedCategories
        {
            get => this.TryGet<IList<Guid>>(nameof(this.SignDisallowedCategories));
            set => this.Set(nameof(this.SignDisallowedCategories), value);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Возвращает признак того, что объект не содержит значимых данных, т.е. является пустым.
        /// </summary>
        /// <returns><c>true</c>, если объект является пустым; <c>false</c> в противном случае.</returns>
        public bool IsEmpty() =>
            this.Flag == KrPermissionsFilesAccessFlag.None
            && this.AllowedCategories is not { Count: not 0 }
            && this.DisallowedCategories is not { Count: not 0 }
            && this.SignAllowedCategories is not { Count: not 0 }
            && this.SignDisallowedCategories is not { Count: not 0 };

        #endregion
    }
}
