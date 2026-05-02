#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    [StorageObjectGenerator]
    public partial class AutoAprovedTaskNotificationInfo : TaskNotificationStorage
    {
        #region Constructors

        public AutoAprovedTaskNotificationInfo(Dictionary<string, object?> storage)
            : base(storage)
        {
            this.Init(nameof(this.ID), GuidBoxes.Empty);
            this.Init(nameof(this.Date), null);
            this.Init(nameof(this.Comment), null);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Идентификатор записи
        /// </summary>
        public Guid ID
        {
            get => this.Get<Guid>(nameof(this.ID));
            set => this.Set(nameof(this.ID), value);
        }

        /// <summary>
        /// Дата автоматического завершения
        /// </summary>
        public DateTime? Date
        {
            get => this.Get<DateTime?>(nameof(this.Date));
            set => this.Set(nameof(this.Date), value);
        }

        /// <summary>
        /// Комментарий, с которым было завершено задание
        /// </summary>
        public string? Comment
        {
            get => this.Get<string>(nameof(this.Comment));
            set => this.Set(nameof(this.Comment), value);
        }

        #endregion
    }
}
