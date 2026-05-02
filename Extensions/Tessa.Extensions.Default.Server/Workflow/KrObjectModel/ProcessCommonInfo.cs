#nullable enable
using System;
using System.Collections.Generic;
using Tessa.Platform;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrObjectModel
{
    /// <summary>
    /// Предоставляет информацию о процессе.
    /// </summary>
    [StorageObjectGenerator(GenerateDefaultConstructor = false)]
    public abstract partial class ProcessCommonInfo :
        StorageObject
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="currentStageRowID"><inheritdoc cref="CurrentStageRowID" path="/summary"/></param>
        /// <param name="info"><inheritdoc cref="Info" path="/summary"/></param>
        /// <param name="secondaryProcessID"><inheritdoc cref="SecondaryProcessID" path="/summary"/></param>
        /// <param name="authorID"><inheritdoc cref="AuthorID" path="/summary"/></param>
        /// <param name="authorName"><inheritdoc cref="AuthorName" path="/summary"/></param>
        /// <param name="processOwnerID"><inheritdoc cref="ProcessOwnerID" path="/summary"/></param>
        /// <param name="processOwnerName"><inheritdoc cref="ProcessOwnerName" path="/summary"/></param>
        protected ProcessCommonInfo(
            Guid? currentStageRowID,
            IDictionary<string, object?>? info,
            Guid? secondaryProcessID,
            Guid? authorID,
            string? authorName,
            Guid? processOwnerID,
            string? processOwnerName)
            : base(new Dictionary<string, object?>(DefaultCapacity, StringComparer.Ordinal))
        {
            this.Init(nameof(this.CurrentStageRowID), currentStageRowID);
            this.Init(nameof(this.Info), info);
            this.Init(nameof(this.SecondaryProcessID), secondaryProcessID);
            this.Init(nameof(this.AuthorID), authorID);
            this.Init(nameof(this.AuthorName), authorName);
            this.Init(nameof(this.AuthorTimestamp), Int64Boxes.Zero);
            this.Init(nameof(this.ProcessOwnerID), processOwnerID);
            this.Init(nameof(this.ProcessOwnerName), processOwnerName);
            this.Init(nameof(this.ProcessOwnerTimestamp), Int64Boxes.Zero);
        }

        /// <inheritdoc />
        protected ProcessCommonInfo(
            Dictionary<string, object?> storage)
            : base(storage)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Идентификатор текущего этапа.
        /// </summary>
        public Guid? CurrentStageRowID
        {
            get => this.Get<Guid?>(nameof(this.CurrentStageRowID));
            set => this.Set(nameof(this.CurrentStageRowID), value);
        }

        /// <summary>
        /// Дополнительная информация по процессу.
        /// </summary>
        public IDictionary<string, object?> Info
        {
            get => this.Get<IDictionary<string, object?>>(nameof(this.Info), static () => new Dictionary<string, object?>(StringComparer.Ordinal));
            set => this.Set(nameof(this.Info), value);
        }

        /// <summary>
        /// Идентификатор вторичного процесса.
        /// </summary>
        public Guid? SecondaryProcessID
        {
            get => this.TryGet<Guid?>(nameof(this.SecondaryProcessID));
            set => this.Set(nameof(this.SecondaryProcessID), value);
        }

        /// <summary>
        /// Идентификатор инициатора процесса.
        /// </summary>
        public virtual Guid? AuthorID
        {
            get => this.Get<Guid?>(nameof(this.AuthorID));
            set => this.Set(nameof(this.AuthorID), value);
        }

        /// <summary>
        /// Имя инициатора процесса.
        /// </summary>
        public virtual string? AuthorName
        {
            get => this.Get<string>(nameof(this.AuthorName));
            set => this.Set(nameof(this.AuthorName), value);
        }

        /// <summary>
        /// Штамп времени изменения инициатора процесса.
        /// </summary>
        public virtual long AuthorTimestamp
        {
            get => this.Get<long>(nameof(this.AuthorTimestamp));
            set => this.Set(nameof(this.AuthorTimestamp), value);
        }

        /// <summary>
        /// Идентификатор владельца процесса.
        /// </summary>
        public virtual Guid? ProcessOwnerID
        {
            get => this.Get<Guid?>(nameof(this.ProcessOwnerID));
            set => this.Set(nameof(this.ProcessOwnerID), value);
        }

        /// <summary>
        /// Имя владельца процесса.
        /// </summary>
        public virtual string? ProcessOwnerName
        {
            get => this.Get<string>(nameof(this.ProcessOwnerName));
            set => this.Set(nameof(this.ProcessOwnerName), value);
        }

        /// <summary>
        /// Штамп времени изменения владельца процесса.
        /// </summary>
        public virtual long ProcessOwnerTimestamp
        {
            get => this.Get<long>(nameof(this.ProcessOwnerTimestamp));
            set => this.Set(nameof(this.ProcessOwnerTimestamp), value);
        }

        #endregion
    }
}
