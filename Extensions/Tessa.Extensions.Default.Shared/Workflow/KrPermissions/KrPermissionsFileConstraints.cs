#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Cards;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.KrPermissions
{
    /// <summary>
    /// Настройки ограничений файлов.
    /// </summary>
    [StorageObjectGenerator]
    public sealed partial class KrPermissionsFileConstraints : CardStorageObject
    {
        #region Constructors

        /// <summary>
        /// Создаёт экземпляр настроек ограничений для файлов.
        /// </summary>
        /// <param name="storage">Хранилище с данными настроек.</param>
        public KrPermissionsFileConstraints(Dictionary<string, object?> storage)
            : base(storage)
        {
            this.Init(nameof(this.Limit), null);
            this.Init(nameof(this.MaxCount), null);
            this.Init(nameof(this.Mandatory), false);
            this.Init(nameof(this.FileCheckRule), Int32Boxes.Zero);
            this.Init(nameof(this.Priority), Int32Boxes.Zero);
        }

        #endregion

        #region Storage Properties

        /// <summary>
        /// Ограничение на размер файла в байтах.
        /// </summary>
        public long? Limit
        {
            get => this.Get<long?>(nameof(this.Limit));
            set => this.Set(nameof(this.Limit), Int64Boxes.Box(value));
        }

        /// <summary>
        /// Ограничение на максимальное количество файлов.
        /// </summary>
        public int? MaxCount
        {
            get => this.Get<int?>(nameof(this.MaxCount));
            set => this.Set(nameof(this.MaxCount), Int32Boxes.Box(value));
        }

        /// <summary>
        /// Флаг "Обязательный файл".
        /// </summary>
        public bool Mandatory
        {
            get => this.Get<bool>(nameof(this.Mandatory));
            set => this.Set(nameof(this.Mandatory), BooleanBoxes.Box(value));
        }

        /// <summary>
        /// Список категорий, к которым относится настройка, или <c>null</c>, если она относится ко всем категориям.
        /// </summary>
        public ICollection<Guid>? Categories
        {
            get => this.TryGet<ICollection<Guid>>(nameof(this.Categories));
            set => this.Set(nameof(this.Categories), value);
        }

        /// <summary>
        /// Список расширений, к которым относится настройка, или <c>null</c>, если она относится ко всем расширениям файлов.
        /// </summary>
        public ICollection<string>? Extensions
        {
            get => this.TryGet<ICollection<string>>(nameof(this.Extensions));
            set => this.Set(nameof(this.Extensions), value);
        }

        /// <summary>
        /// Правило проверки файла по принадлежности файла пользователю.
        /// </summary>
        public int FileCheckRule
        {
            get => this.Get<int>(nameof(this.FileCheckRule));
            set => this.Set(nameof(this.FileCheckRule), Int32Boxes.Box(value));
        }

        /// <summary>
        /// Приоритет ограничения.
        /// </summary>
        public int Priority
        {
            get => this.Get<int>(nameof(this.Priority));
            set => this.Set(nameof(this.Priority), Int32Boxes.Box(value));
        }

        #endregion
    }
}
