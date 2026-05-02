#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Platform.Plugins;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Plugins.OnlyOffice
{
    /// <summary>
    /// Настройки плагина <see cref="DefaultPluginNames.OnlyOfficeRemoveFileCacheInfoPlugin"/>.
    /// </summary>
    /// <inheritdoc cref="PluginSettings(string)"/>
    /// <param name="settings">Глобальные настройки конфигурации.</param>
    public sealed class OnlyOfficeRemoveFileCacheInfoPluginSettings(string name, Dictionary<string, object?> settings)
        : PluginSettings(name)
    {
        #region Fields

        private static readonly TimeSpan DefaultOldestPreviewFilePeriod = TimeSpan.FromDays(10);
        private readonly Dictionary<string, object?> settings = NotNullOrThrow(settings);

        #endregion

        #region Properties

        /// <summary>
        /// Период хранения файла с момента последнего обращения.
        /// </summary>
        public TimeSpan OldestPreviewFilePeriod { get; set; } = DefaultOldestPreviewFilePeriod;

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);

            storage[nameof(this.OldestPreviewFilePeriod)] = this.OldestPreviewFilePeriod.ToString();
        }

        /// <inheritdoc/>
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);

            this.OldestPreviewFilePeriod = storage.TryGet<string>(nameof(this.OldestPreviewFilePeriod)) is { Length: > 0 } localValue
                && TimeSpan.TryParse(localValue, out var oldestPreviewFilePeriod)
                ? oldestPreviewFilePeriod
                : this.settings.TryGet<string>("FileConverter.OldestPreviewFilePeriod") is { Length: > 0 } globalValue
                    && TimeSpan.TryParse(globalValue, out oldestPreviewFilePeriod)
                    ? oldestPreviewFilePeriod
                    : DefaultOldestPreviewFilePeriod;
        }

        #endregion
    }
}
