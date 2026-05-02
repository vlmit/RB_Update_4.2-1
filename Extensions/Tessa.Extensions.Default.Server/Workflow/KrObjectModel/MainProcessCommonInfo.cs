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
    public sealed partial class MainProcessCommonInfo :
        ProcessCommonInfo
    {
        #region Constructors

        /// <inheritdoc cref="ProcessCommonInfo(Guid?, IDictionary{string, object?}, Guid?, Guid?, string?, Guid?, string?)"/>
        /// <param name="authorComment"><inheritdoc cref="AuthorComment" path="/summary"/></param>
        /// <param name="state"><inheritdoc cref="State" path="/summary"/></param>
        public MainProcessCommonInfo(
            Guid? currentStageRowID,
            IDictionary<string, object?>? info,
            Guid? secondaryProcessID,
            Guid? authorID,
            string? authorName,
            string? authorComment,
            int state,
            Guid? processOwnerID,
            string? processOwnerName)
            : base(
                  currentStageRowID,
                  info,
                  secondaryProcessID,
                  authorID,
                  authorName,
                  processOwnerID,
                  processOwnerName)
        {
            this.Init(nameof(this.AuthorComment), authorComment);
            this.Init(nameof(this.AuthorCommentTimestamp), Int64Boxes.Zero);
            this.Init(nameof(this.State), Int32Boxes.Box(state));
            this.Init(nameof(this.StateTimestamp), Int64Boxes.Zero);
            this.Init(nameof(this.AffectMainCardVersionWhenStateChanged), BooleanBoxes.True);
            this.Init(nameof(this.AffectMainCardVersionWhenStateChangedTimestamp), Int64Boxes.Zero);
        }

        /// <inheritdoc cref="ProcessCommonInfo(Dictionary{string, object?})"/>
        public MainProcessCommonInfo(
            Dictionary<string, object?> storage)
            : base(storage)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Комментарий инициатора процесса.
        /// </summary>
        public string? AuthorComment
        {
            get => this.Get<string>(nameof(this.AuthorComment));
            set => this.Set(nameof(this.AuthorComment), value);
        }

        /// <summary>
        /// Штамп времени изменения комментария инициатора процесса.
        /// </summary>
        public long AuthorCommentTimestamp
        {
            get => this.Get<long>(nameof(this.AuthorCommentTimestamp));
            set => this.Set(nameof(this.AuthorCommentTimestamp), value);
        }

        /// <summary>
        /// Идентификатор состояние документа.
        /// </summary>
        public int State
        {
            get => this.Get<int>(nameof(this.State));
            set => this.Set(nameof(this.State), Int32Boxes.Box(value));
        }

        /// <summary>
        /// Штамп времени изменения состояние документа.
        /// </summary>
        public long StateTimestamp
        {
            get => this.Get<long>(nameof(this.StateTimestamp));
            set => this.Set(nameof(this.StateTimestamp), value);
        }

        /// <summary>
        /// Признак, показывающий, что версия основной карточки должна быть изменена, если состояние документа изменилось.
        /// </summary>
        public bool AffectMainCardVersionWhenStateChanged
        {
            get => this.Get<bool>(nameof(this.AffectMainCardVersionWhenStateChanged));
            set => this.Set(nameof(this.AffectMainCardVersionWhenStateChanged), BooleanBoxes.Box(value));
        }

        /// <summary>
        /// Штамп времени изменения флага версии основной карточки.
        /// </summary>
        public long AffectMainCardVersionWhenStateChangedTimestamp
        {
            get => this.Get<long>(nameof(this.AffectMainCardVersionWhenStateChangedTimestamp));
            set => this.Set(nameof(this.AffectMainCardVersionWhenStateChangedTimestamp), value);
        }

        #endregion
    }
}
