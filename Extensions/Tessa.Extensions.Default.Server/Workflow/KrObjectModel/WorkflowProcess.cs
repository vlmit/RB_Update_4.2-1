#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Workflow.KrObjectModel
{
    /// <summary>
    /// Объектная модель процесса.
    /// </summary>
    public sealed class WorkflowProcess :
        IEquatable<WorkflowProcess>,
        ISealable
    {
        #region Fields

        private string? authorComment;
        private KrState state;
        private SealableObjectList<Stage>? stages;
        private readonly bool isMainProcess;
        private IDictionary<string, object?>? mainProcessInfoStorage;
        private IDictionary<string, object?>? infoStorage;
        private bool affectMainCardVersionWhenStateChanged = true;
        private Guid? currentApprovalStageRowID;
        private Author? author;
        private long authorCommentTimestamp;
        private long authorTimestamp;
        private long stateTimestamp;
        private long affectMainCardVersionWhenStateChangedTimestamp;
        private Author? processOwner;
        private long processOwnerTimestamp;
        private Author? authorCurrentProcess;
        private long authorCurrentProcessTimestamp;
        private Author? processOwnerCurrentProcess;
        private long processOwnerCurrentProcessTimestamp;
        private volatile bool dynamicInfoIsInitialized;
        private dynamic? dynamicInfo;
        private volatile bool dynamicMainProcessInfoIsInitialized;
        private dynamic? dynamicMainProcessInfo;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="infoStorage"><inheritdoc cref="InfoStorage" path="/summary"/></param>
        /// <param name="mainProcessInfoStorage"><inheritdoc cref="MainProcessInfoStorage" path="/summary"/></param>
        /// <param name="stages"><inheritdoc cref="Stages" path="/summary"/></param>
        /// <param name="nestedProcessID"><inheritdoc cref="NestedProcessID" path="/summary"/></param>
        [JsonConstructor]
        public WorkflowProcess(
            IDictionary<string, object?> infoStorage,
            IDictionary<string, object?> mainProcessInfoStorage,
            SealableObjectList<Stage> stages,
            Guid? nestedProcessID,
            bool isMainProcess)
        {
            this.InfoStorage = NotNullOrThrow(infoStorage);
            this.MainProcessInfoStorage = NotNullOrThrow(mainProcessInfoStorage);
            this.Stages = NotNullOrThrow(stages);
            this.NestedProcessID = nestedProcessID;
            this.isMainProcess = isMainProcess;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="workflowProcess">Объект <see cref="WorkflowProcess"/>, на основе которого выполняется инициализация.</param>
        /// <param name="stageFilterPredicate">Условие в соответствии с которым будет выполнено копирование этапов в новый объект, если условие не задано, то копируются все этапы.</param>
        public WorkflowProcess(
            WorkflowProcess workflowProcess,
            Func<Stage, bool>? stageFilterPredicate = null) =>
            this.InternalUpdate(
                workflowProcess,
                stageFilterPredicate);

        #endregion

        #region Properties

        /// <summary>
        /// Идентификатор вложенного процесса.
        /// </summary>
        public Guid? NestedProcessID { get; private set; }

        /// <summary>
        /// Идентификатор текущего активного этапа процесса.
        /// </summary>
        public Guid? CurrentApprovalStageRowID
        {
            get => this.currentApprovalStageRowID;
            set
            {
                ThrowIfSealed(this);
                this.currentApprovalStageRowID = value;
            }
        }

        /// <summary>
        /// Дополнительная информация по процессу.
        /// </summary>
        public IDictionary<string, object?> InfoStorage
        {
            get => this.infoStorage!;
            set
            {
                this.infoStorage = NotNullOrThrow(value);
                this.dynamicInfoIsInitialized = false;
            }
        }

        /// <inheritdoc cref="InfoStorage"/>
        [JsonIgnore]
        public dynamic Info
        {
            get
            {
                if (this.dynamicInfoIsInitialized)
                {
                    return this.dynamicInfo!;
                }

                // если объект в кэше и к нему идут обращения из нескольких потоков, то сначала надо проставить
                // свойство с dynamic-ом, и уже потом - флажок на инициализацию
                var result = this.dynamicInfo = DynamicStorageAccessor.Create(this.InfoStorage);
                this.dynamicInfoIsInitialized = true;

                return result;
            }
        }

        /// <summary>
        /// Дополнительная информация по родительскому процессу. Актуально для вложенных,
        /// для родительского MainProcessInfo = Info.
        /// </summary>
        public IDictionary<string, object?> MainProcessInfoStorage
        {
            get => this.mainProcessInfoStorage!;
            set
            {
                this.mainProcessInfoStorage = NotNullOrThrow(value);
                this.dynamicMainProcessInfoIsInitialized = false;
            }
        }

        /// <inheritdoc cref="MainProcessInfoStorage"/>
        [JsonIgnore]
        public dynamic MainProcessInfo
        {
            get
            {
                if (this.dynamicMainProcessInfoIsInitialized)
                {
                    return this.dynamicMainProcessInfo!;
                }

                // если объект в кэше и к нему идут обращения из нескольких потоков, то сначала надо проставить
                // свойство с dynamic-ом, и уже потом - флажок на инициализацию
                var result = this.dynamicMainProcessInfo = DynamicStorageAccessor.Create(this.MainProcessInfoStorage);
                this.dynamicMainProcessInfoIsInitialized = true;

                return result;
            }
        }

        /// <summary>
        /// Автор (инициатор) основного процесса.
        /// </summary>
        /// <remarks>При задании значения изменяется штамп времени изменения <see cref="AuthorTimestamp"/>.</remarks>
        public Author? Author
        {
            get => this.author;
            set => this.SetAuthor(value);
        }

        /// <summary>
        /// Штамп времени изменения автора (инициатора) основного процесса.
        /// </summary>
        public long AuthorTimestamp => this.authorTimestamp;

        /// <summary>
        /// Комментарий автора (инициатора) основного процесса.
        /// </summary>
        /// <remarks>При задании значения изменяется штамп времени изменения <see cref="AuthorCommentTimestamp"/>.</remarks>
        public string? AuthorComment
        {
            get => this.authorComment;
            set => this.SetAuthorComment(value);
        }

        /// <summary>
        /// Штамп времени изменения комментария автора (инициатора) основного процесса.
        /// </summary>
        public long AuthorCommentTimestamp => this.authorCommentTimestamp;

        /// <summary>
        /// Состояние основного процесса.
        /// </summary>
        /// <remarks>При задании значения изменяется штамп времени изменения <see cref="StateTimestamp"/>.</remarks>
        public KrState State
        {
            get => this.state;
            set => this.SetState(value);
        }

        /// <summary>
        /// Штамп времени изменения состояния основного процесса.
        /// </summary>
        public long StateTimestamp => this.stateTimestamp;

        /// <summary>
        /// Значение, показывающее, что версию основной карточки должна быть изменена, если состояние документа изменилось.
        /// </summary>
        /// <remarks>При задании значения изменяется штамп времени изменения <see cref="AffectMainCardVersionWhenStateChangedTimestamp"/>.</remarks>
        public bool AffectMainCardVersionWhenStateChanged
        {
            get => this.affectMainCardVersionWhenStateChanged;
            set => this.SetAffectMainCardVersionWhenStateChanged(value);
        }

        /// <summary>
        /// Штамп времени изменения флага версии основной карточки.
        /// </summary>
        public long AffectMainCardVersionWhenStateChangedTimestamp => this.affectMainCardVersionWhenStateChangedTimestamp;

        /// <summary>
        /// Коллекция этапов процесса.
        /// </summary>
        public SealableObjectList<Stage> Stages
        {
            get => this.stages!;
            set
            {
                ThrowIfSealed(this);
                this.stages = value;
            }
        }

        /// <summary>
        /// Состояние процесса до выполнения обработчиков этапов.
        /// </summary>
        /// <remarks>Для инициализации значения необходимо использовать метод <see cref="UpdateInitialWorkflowProcess"/>.</remarks>
        public WorkflowProcess? InitialWorkflowProcess { get; private set; }

        /// <summary>
        /// Владелец основного процесса.
        /// </summary>
        /// <remarks>При задании значения изменяется штамп времени изменения <see cref="ProcessOwnerTimestamp"/>.</remarks>
        public Author? ProcessOwner
        {
            get => this.processOwner;
            set => this.SetProcessOwner(value);
        }

        /// <summary>
        /// Штамп времени изменения владельца основного процесса.
        /// </summary>
        public long ProcessOwnerTimestamp => this.processOwnerTimestamp;

        /// <summary>
        /// Владелец текущего процесса. Значение совпадает с <see cref="ProcessOwner"/>, если текущий процесс является основным.
        /// </summary>
        /// <remarks>При задании значения изменяется штамп времени изменения <see cref="ProcessOwnerCurrentProcessTimestamp"/>.</remarks>
        public Author? ProcessOwnerCurrentProcess
        {
            get => this.processOwnerCurrentProcess;
            set => this.SetProcessOwnerCurrentProcess(value);
        }

        /// <summary>
        /// Штамп времени изменения автора (инициатора) текущего процесса.
        /// </summary>
        public long ProcessOwnerCurrentProcessTimestamp => this.processOwnerCurrentProcessTimestamp;

        /// <summary>
        /// Автор (инициатор) текущего процесса. Значение совпадает с <see cref="Author"/>, если текущий процесс является основным.
        /// </summary>
        /// <remarks>При задании значения изменяется штамп времени изменения <see cref="AuthorCurrentProcessTimestamp"/>.</remarks>
        public Author? AuthorCurrentProcess
        {
            get => this.authorCurrentProcess;
            set => this.SetAuthorCurrentProcess(value);
        }

        /// <summary>
        /// Штамп времени изменения автора (инициатора) текущего процесса.
        /// </summary>
        public long AuthorCurrentProcessTimestamp => this.authorCurrentProcessTimestamp;

        #endregion

        #region ISealable Members

        /// <inheritdoc/>
        public bool IsSealed { get; private set; }

        /// <inheritdoc/>
        public void Seal()
        {
            this.IsSealed = true;
            this.Stages.Seal();
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj is WorkflowProcess other
                && this.Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

        #endregion

        #region Operators

        /// <doc path='info[@type="object" and @item="OperatorEquals"]'/>
        public static bool operator ==(WorkflowProcess? left, WorkflowProcess? right)
        {
            if (left is null
                && right is null)
            {
                return true;
            }

            return left?.Equals(right) == true;
        }

        /// <doc path='info[@type="object" and @item="OperatorNotEquals"]'/>
        public static bool operator !=(WorkflowProcess? left, WorkflowProcess? right)
        {
            if (left is null
                && right is null)
            {
                return false;
            }

            return left?.Equals(right) != true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Установить инициатора основного процесса. В общем случае необходимо использовать свойство <see cref="Author"/>.
        /// </summary>
        /// <param name="value">Новый инициатор процесса.</param>
        /// <param name="withTimestamp">Значение <see langword="true"/>, если необходимо проставить время изменения, иначе - <see langword="false"/>.</param>
        public void SetAuthor(
            Author? value,
            bool withTimestamp = true)
        {
            this.SetAuthorInternal(value, withTimestamp);

            if (this.isMainProcess)
            {
                this.SetAuthorCurrentProcessInternal(value, withTimestamp);
            }
        }

        /// <summary>
        /// Установить комментарий инициатора основного процесса.
        /// В общем случае необходимо использовать свойство <see cref="AuthorComment"/>.
        /// </summary>
        /// <param name="value">Новый комментарий.</param>
        /// <param name="withTimestamp">Значение <see langword="true"/>, если необходимо проставить время изменения, иначе - <see langword="false"/>.</param>
        public void SetAuthorComment(
            string? value,
            bool withTimestamp = true) =>
            this.SetWorkflowProcessValue(
                value,
                withTimestamp,
                out this.authorComment,
                ref this.authorCommentTimestamp);

        /// <summary>
        /// Устанавливает состояние основного процесса.
        /// В общем случае необходимо использовать свойство <see cref="State"/>.
        /// </summary>
        /// <param name="value">Новое состояние.</param>
        /// <param name="withTimestamp">Значение <see langword="true"/>, если необходимо проставить время изменения, иначе - <see langword="false"/>.</param>
        public void SetState(
            KrState value,
            bool withTimestamp = true) =>
            this.SetWorkflowProcessValue(
                value,
                withTimestamp,
                out this.state,
                ref this.stateTimestamp);

        /// <summary>
        /// Устанавливает флаг изменения версии документа при изменении состояния документа.
        /// В общем случае необходимо использовать свойство <see cref="AffectMainCardVersionWhenStateChanged"/>.
        /// </summary>
        /// <param name="value">Новое значение.</param>
        /// <param name="withTimestamp">Значение <see langword="true"/>, если необходимо проставить время изменения, иначе - <see langword="false"/>.</param>
        public void SetAffectMainCardVersionWhenStateChanged(
            bool value,
            bool withTimestamp = true) =>
            this.SetWorkflowProcessValue(
                value,
                withTimestamp,
                out this.affectMainCardVersionWhenStateChanged,
                ref this.affectMainCardVersionWhenStateChangedTimestamp);

        /// <summary>
        /// Устанавливает владельца основного процесса. В общем случае необходимо использовать свойство <see cref="ProcessOwner"/>.
        /// </summary>
        /// <param name="value">Новый владелец процесса.</param>
        /// <param name="withTimestamp">Значение <see langword="true"/>, если необходимо проставить время изменения, иначе - <see langword="false"/>.</param>
        public void SetProcessOwner(
            Author? value,
            bool withTimestamp = true)
        {
            this.SetProcessOwnerInternal(value, withTimestamp);

            if (this.isMainProcess)
            {
                this.SetProcessOwnerCurrentProcessInternal(value, withTimestamp);
            }
        }

        /// <summary>
        /// Установить инициатора текущего процесса. В общем случае необходимо использовать свойство <see cref="AuthorCurrentProcess"/>.
        /// </summary>
        /// <param name="value">Новый инициатор процесса.</param>
        /// <param name="withTimestamp">Значение <see langword="true"/>, если необходимо проставить время изменения, иначе - <see langword="false"/>.</param>
        public void SetAuthorCurrentProcess(
            Author? value,
            bool withTimestamp = true)
        {
            this.SetAuthorCurrentProcessInternal(value, withTimestamp);

            if (this.isMainProcess)
            {
                this.SetAuthorInternal(value, withTimestamp);
            }
        }

        /// <summary>
        /// Устанавливает владельца текущего процесса. В общем случае необходимо использовать свойство <see cref="ProcessOwnerCurrentProcess"/>.
        /// </summary>
        /// <param name="value">Новый владелец процесса.</param>
        /// <param name="withTimestamp">Значение <see langword="true"/>, если необходимо проставить время изменения, иначе - <see langword="false"/>.</param>
        public void SetProcessOwnerCurrentProcess(
            Author? value,
            bool withTimestamp = true)
        {
            this.SetProcessOwnerCurrentProcessInternal(value, withTimestamp);

            if (this.isMainProcess)
            {
                this.SetProcessOwnerInternal(value, withTimestamp);
            }
        }

        /// <inheritdoc />
        public bool Equals(WorkflowProcess? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            var currentAuthor = this.Author;
            var otherAuthor = other.Author;
            var authorsAreEqual = (currentAuthor is null && otherAuthor is null)
                || currentAuthor?.Equals(otherAuthor) == true;

            return authorsAreEqual
                && string.Equals(this.AuthorComment, other.AuthorComment, StringComparison.Ordinal)
                && this.State == other.State;
        }

        /// <summary>
        /// Сохраняет текущее состояние процесса в <see cref="InitialWorkflowProcess"/>.
        /// </summary>
        public void UpdateInitialWorkflowProcess()
        {
            ThrowIfSealed(this);

            this.InitialWorkflowProcess = new WorkflowProcess(this);
            this.InitialWorkflowProcess.Seal();
        }

        /// <summary>
        /// Обновляет состояние объекта.
        /// </summary>
        /// <param name="workflowProcess">Объект, данными из которого обновляется текущий объект.</param>
        /// <param name="isWithoutClone">Значение <see langword="true"/>, если словари, списки и т.п. объекты задаются соответствующим свойствам как есть, иначе перед присвоением создаются их копии.</param>
        public void Update(
            WorkflowProcess workflowProcess,
            bool isWithoutClone = false) =>
            this.InternalUpdate(
                workflowProcess,
                null,
                isWithoutClone);

        #endregion

        #region Private Methods

        private static SealableObjectList<Stage> CopyStages(
            WorkflowProcess workflowProcess,
            Func<Stage, bool>? actionPredicate,
            bool isWithoutClone)
        {
            if (actionPredicate is not null)
            {
                return workflowProcess
                    .Stages
                    .Where(actionPredicate)
                    .Select(static p => new Stage(p))
                    .ToSealableObjectList();
            }

            if (isWithoutClone)
            {
                return workflowProcess.Stages;
            }

            return workflowProcess
                .Stages
                .Select(static p => new Stage(p))
                .ToSealableObjectList();
        }

        private static long GetTimestamp() => DateTime.UtcNow.Ticks;

        private void SetWorkflowProcessValue<T>(
            T value,
            bool withTimestamp,
            out T field,
            ref long timestamp)
        {
            ThrowIfSealed(this);
            field = value;
            if (withTimestamp)
            {
                timestamp = GetTimestamp();
            }
        }

        /// <summary>
        /// Установить инициатора основного процесса. В общем случае необходимо использовать свойство <see cref="Author"/>.
        /// </summary>
        /// <param name="value">Новый инициатор процесса.</param>
        /// <param name="withTimestamp">Значение <see langword="true"/>, если необходимо проставить время изменения, иначе - <see langword="false"/>.</param>
        private void SetAuthorInternal(
            Author? value,
            bool withTimestamp = true) =>
            this.SetWorkflowProcessValue(
                value,
                withTimestamp,
                out this.author,
                ref this.authorTimestamp);

        /// <summary>
        /// Установить инициатора текущего процесса. В общем случае необходимо использовать свойство <see cref="AuthorCurrentProcess"/>.
        /// </summary>
        /// <param name="value">Новый инициатор процесса.</param>
        /// <param name="withTimestamp">Значение <see langword="true"/>, если необходимо проставить время изменения, иначе - <see langword="false"/>.</param>
        private void SetAuthorCurrentProcessInternal(
            Author? value,
            bool withTimestamp = true) =>
            this.SetWorkflowProcessValue(
                value,
                withTimestamp,
                out this.authorCurrentProcess,
                ref this.authorCurrentProcessTimestamp);

        /// <summary>
        /// Устанавливает владельца основного процесса. В общем случае необходимо использовать свойство <see cref="ProcessOwner"/>.
        /// </summary>
        /// <param name="value">Новый владелец процесса.</param>
        /// <param name="withTimestamp">Значение <see langword="true"/>, если необходимо проставить время изменения, иначе - <see langword="false"/>.</param>
        private void SetProcessOwnerInternal(
            Author? value,
            bool withTimestamp = true) =>
            this.SetWorkflowProcessValue(
                value,
                withTimestamp,
                out this.processOwner,
                ref this.processOwnerTimestamp);

        /// <summary>
        /// Устанавливает владельца текущего процесса. В общем случае необходимо использовать свойство <see cref="ProcessOwnerCurrentProcess"/>.
        /// </summary>
        /// <param name="value">Новый владелец процесса.</param>
        /// <param name="withTimestamp">Значение <see langword="true"/>, если необходимо проставить время изменения, иначе - <see langword="false"/>.</param>
        private void SetProcessOwnerCurrentProcessInternal(
            Author? value,
            bool withTimestamp = true) =>
            this.SetWorkflowProcessValue(
                value,
                withTimestamp,
                out this.processOwnerCurrentProcess,
                ref this.processOwnerCurrentProcessTimestamp);

        private void InternalUpdate(
            WorkflowProcess workflowProcess,
            Func<Stage, bool>? stageFilterPredicate = null,
            bool isWithoutClone = false)
        {
            ThrowIfNull(workflowProcess);

            this.SetAuthor(workflowProcess.Author, false);
            this.SetAuthorComment(workflowProcess.AuthorComment, false);
            this.SetState(workflowProcess.State, false);
            this.InfoStorage = isWithoutClone
                ? workflowProcess.InfoStorage
                : StorageHelper.Clone(workflowProcess.InfoStorage);
            this.MainProcessInfoStorage = isWithoutClone
                ? workflowProcess.MainProcessInfoStorage
                : StorageHelper.Clone(workflowProcess.MainProcessInfoStorage);
            this.NestedProcessID = workflowProcess.NestedProcessID;
            this.SetProcessOwner(workflowProcess.ProcessOwner, false);
            this.SetAuthorCurrentProcess(workflowProcess.AuthorCurrentProcess, false);
            this.SetProcessOwnerCurrentProcess(workflowProcess.ProcessOwnerCurrentProcess, false);
            this.stages = CopyStages(workflowProcess, stageFilterPredicate, isWithoutClone);
        }

        #endregion
    }
}
