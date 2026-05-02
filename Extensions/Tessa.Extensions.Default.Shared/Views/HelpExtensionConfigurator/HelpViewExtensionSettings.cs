#nullable enable

using System.Collections.Generic;
using Tessa.Cards;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Views.HelpExtensionConfigurator
{
    /// <summary>
    /// Настройки расширения представления для работы со справкой.
    /// </summary>
    public sealed class HelpViewExtensionSettings :
        StorageSerializable
    {
        #region Properties

        /// <summary>
        /// Способ отображения справочной информации.
        /// </summary>
        public CardHelpMode HelpMode { get; set; } = CardHelpMode.Url;

        /// <summary>
        /// Значение, используемое для отображения справочной информации.
        /// </summary>
        public string? Value { get; set; }

        #endregion

        #region IStorageSerializable Members

        /// <inheritdoc />
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            storage[nameof(this.HelpMode)] = this.HelpMode.ToString();
            storage[nameof(this.Value)] = this.Value;
        }

        /// <inheritdoc />
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            this.HelpMode = storage.TryConvertEnum<CardHelpMode>(nameof(this.HelpMode)) ?? CardHelpMode.Url;
            this.Value = storage.TryGet<string>(nameof(this.Value));
        }

        #endregion
    }
}
