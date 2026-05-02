#nullable enable

using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Plugins;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <summary>
    /// Настройки плагина <see cref="DefaultPluginNames.TasksNotificationsPlugin"/>.
    /// </summary>
    public sealed class TasksNotificationsPluginSettings : PluginSettings
    {
        #region Fields

        private const long DefaultMaxTasksPerUserNotification = 20;

        #endregion

        #region Constructors

        /// <inheritdoc cref="PluginSettings(string)"/>
        public TasksNotificationsPluginSettings(
            string pluginName)
            : base(pluginName)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Ограничивает максимальное количество заданий, приходящихся на одного сотрудника, которые попадают в уведомление (по умолчанию 20 заданий на сотрудника).
        /// Из базы отбирается ограниченное количество заданий, начиная с самых новых, которые потом передаются в уведомление.
        /// </summary>
        public long MaxTasksPerUserNotification { get; set; } = DefaultMaxTasksPerUserNotification;

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);

            storage[nameof(this.MaxTasksPerUserNotification)] = Int64Boxes.Box(this.MaxTasksPerUserNotification);
        }

        /// <inheritdoc/>
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);

            this.MaxTasksPerUserNotification = storage.TryGet(nameof(this.MaxTasksPerUserNotification), DefaultMaxTasksPerUserNotification);
        }

        #endregion
    }
}
