#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <summary>
    /// Информация, необходимая для построения уведомления по заданию.
    /// </summary>
    [StorageObjectGenerator]
    public partial class TaskNotificationInfo : TaskNotificationStorage
    {
        #region Constructors

        public TaskNotificationInfo(Dictionary<string, object?> storage)
            :base(storage)
        {
            this.Init(nameof(this.CardNumber), null);
            this.Init(nameof(this.CardSubject), null);
            this.Init(nameof(this.InProgress), Int32Boxes.Zero);
            this.Init(nameof(this.TaskID), GuidBoxes.Empty);
            this.Init(nameof(this.Created), null);
            this.Init(nameof(this.Planned), null);
            this.Init(nameof(this.TaskInfo), null);
            this.Init(nameof(this.AuthorRole), null);
            this.Init(nameof(this.TypeCaption), null);
            this.Init(nameof(this.AutoApproveString), null);
            this.Init(nameof(this.AutoApproveDate), null);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Номер карточки.
        /// </summary>
        public string? CardNumber
        {
            get => this.Get<string>(nameof(this.CardNumber));
            set => this.Set(nameof(this.CardNumber), value);
        }

        /// <summary>
        /// Тема карточки.
        /// </summary>
        public string? CardSubject
        {
            get => this.Get<string>(nameof(this.CardSubject));
            set => this.Set(nameof(this.CardSubject), value);
        }

        /// <summary>
        /// Признак, что задание взято в работу.
        /// </summary>
        public int InProgress
        {
            get => this.Get<int>(nameof(this.InProgress));
            set => this.Set(nameof(this.InProgress), value);
        }

        /// <summary>
        /// ID задания.
        /// </summary>
        public Guid TaskID
        {
            get => this.Get<Guid>(nameof(this.TaskID));
            set => this.Set(nameof(this.TaskID), value);
        }

        /// <summary>
        /// Дата создания.
        /// </summary>
        public DateTime? Created
        {
            get => this.Get<DateTime?>(nameof(this.Created));
            set => this.Set(nameof(this.Created), value);
        }

        /// <summary>
        /// Запланированная дата выполнения.
        /// </summary>
        public DateTime? Planned
        {
            get => this.Get<DateTime?>(nameof(this.Planned));
            set => this.Set(nameof(this.Planned), value);
        }

        /// <summary>
        /// Информация о задании.
        /// </summary>
        public string? TaskInfo
        {
            get => this.Get<string>(nameof(this.TaskInfo));
            set => this.Set(nameof(this.TaskInfo), value);
        }

        /// <summary>
        /// Автор задания.
        /// </summary>
        public string? AuthorRole
        {
            get => this.Get<string>(nameof(this.AuthorRole));
            set => this.Set(nameof(this.AuthorRole), value);
        }

        /// <summary>
        /// Тип задания.
        /// </summary>
        public string? TypeCaption
        {
            get => this.Get<string>(nameof(this.TypeCaption));
            set => this.Set(nameof(this.TypeCaption), value);
        }

        /// <summary>
        /// Строка, предупреждающая об автозавершении.
        /// </summary>
        public string? AutoApproveString
        {
            get => this.Get<string>(nameof(this.AutoApproveString));
            set => this.Set(nameof(this.AutoApproveString), value);
        }

        /// <summary>
        /// Планируемое время автозавершения.
        /// </summary>
        public DateTime? AutoApproveDate
        {
            get => this.Get<DateTime?>(nameof(this.AutoApproveDate));
            set => this.Set(nameof(this.AutoApproveDate), value);
        }

        #endregion
    }
}
