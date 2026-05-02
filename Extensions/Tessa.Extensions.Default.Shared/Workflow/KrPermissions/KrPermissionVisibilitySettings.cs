#nullable enable

using System.Collections.Generic;
using Tessa.Cards;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.KrPermissions
{
    /// <summary>
    /// Настройки видимости элементов управления.
    /// </summary>
    [StorageObjectGenerator]
    public sealed partial class KrPermissionVisibilitySettings : CardStorageObject
    {
        #region Constructors

        /// <inheritdoc cref="CardStorageObject(Dictionary{string, object?})"/>
        public KrPermissionVisibilitySettings(Dictionary<string, object?> storage)
            : base(storage)
        {
            this.Init(nameof(this.Alias), null);
            this.Init(nameof(this.ControlType), Int32Boxes.Zero);
            this.Init(nameof(this.IsHidden), BooleanBoxes.False);
        }

        /// <summary>
        /// Создаёт экземпляр объекта с указанием его свойств.
        /// </summary>
        /// <param name="alias"><inheritdoc cref="Alias" path="/summary"/></param>
        /// <param name="controlType"><inheritdoc cref="ControlType" path="/summary"/></param>
        /// <param name="isHidden"><inheritdoc cref="IsHidden" path="/summary"/></param>
        public KrPermissionVisibilitySettings(
            string alias,
            int controlType,
            bool isHidden)
            : base([])
        {
            this.Alias = alias;
            this.ControlType = controlType;
            this.IsHidden = isHidden;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Алиас элементов управления, к которым применяется данная настройка.
        /// </summary>
        public string? Alias
        {
            get => this.Get<string>(nameof(this.Alias));
            set => this.Set(nameof(this.Alias), value);
        }

        /// <summary>
        /// Тип элемента управления.
        /// </summary>
        public int ControlType
        {
            get => this.Get<int>(nameof(this.ControlType));
            set => this.Set(nameof(this.ControlType), Int32Boxes.Box(value));
        }

        /// <summary>
        /// Определяет, должны ли элементы управления быть скрыты.
        /// </summary>
        public bool IsHidden
        {
            get => this.Get<bool>(nameof(this.IsHidden));
            set => this.Set(nameof(this.IsHidden), BooleanBoxes.Box(value));
        }

        #endregion
    }
}
