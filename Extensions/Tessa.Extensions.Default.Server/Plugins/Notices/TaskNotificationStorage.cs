#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    [StorageObjectGenerator]
    public abstract partial class TaskNotificationStorage : StorageObject, ITaskNotificationInfo
    {
        #region Constructors

        protected TaskNotificationStorage(Dictionary<string, object?> storage)
            : base(storage)
        {
            this.Init(nameof(this.CardID), GuidBoxes.Empty);
            this.Init(nameof(this.UserID), GuidBoxes.Empty);
            this.Init(nameof(this.UserName), null);
            this.Init(nameof(this.LinkText), null);
            this.Init(nameof(this.WebLink), null);
        }

        #endregion

        #region ITaskNotificationInfo Members

        /// <inheritdoc/>
        public Guid CardID
        {
            get => this.Get<Guid>(nameof(this.CardID));
            set => this.Set(nameof(this.CardID), value);
        }

        /// <inheritdoc/>
        public Guid UserID
        {
            get => this.Get<Guid>(nameof(this.UserID));
            set => this.Set(nameof(this.UserID), value);
        }

        /// <inheritdoc/>
        public string? UserName
        {
            get => this.Get<string>(nameof(this.UserName));
            set => this.Set(nameof(this.UserName), value);
        }

        /// <inheritdoc/>
        public string? LinkText
        {
            get => this.Get<string>(nameof(this.LinkText));
            set => this.Set(nameof(this.LinkText), value);
        }

        /// <inheritdoc/>
        public string? WebLink
        {
            get => this.Get<string>(nameof(this.WebLink));
            set => this.Set(nameof(this.WebLink), value);
        }

        #endregion
    }
}
