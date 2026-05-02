#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Cards.Workflow;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers
{
    /// <inheritdoc cref="IStageTypeHandlerContext"/>
    public sealed class StageTypeHandlerContext :
        IStageTypeHandlerContext
    {
        #region Fields

        private Stage? stage;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="runnerContext"><inheritdoc cref="IKrProcessRunnerContext" path="/summary"/></param>
        /// <param name="stage"><inheritdoc cref="Stage" path="/summary"/></param>
        /// <param name="runnerMode"><inheritdoc cref="RunnerMode" path="/summary"/></param>
        /// <param name="directionAfterInterrupt"><inheritdoc cref="DirectionAfterInterrupt" path="/summary"/></param>
        public StageTypeHandlerContext(
            IKrProcessRunnerContext runnerContext,
            Stage stage,
            KrProcessRunnerMode runnerMode,
            DirectionAfterInterrupt? directionAfterInterrupt)
        {
            ThrowIfNull(runnerContext);
            ThrowIfNull(stage);

            this.MainCardAccessStrategy = runnerContext.MainCardAccessStrategy;
            this.MainCardID = runnerContext.CardID;
            this.MainCardType = runnerContext.CardType;
            this.MainCardDocTypeID = runnerContext.DocTypeID;
            this.KrComponents = runnerContext.KrComponents;
            this.SecondaryProcess = runnerContext.SecondaryProcess;
            this.ContextualSatellite = runnerContext.ContextualSatellite;
            this.ProcessHolderSatellite = runnerContext.ProcessHolderSatellite;
            this.ProcessHolder = runnerContext.ProcessHolder;
            this.CardExtensionContext = runnerContext.CardContext;
            this.ValidationResult = runnerContext.ValidationResult;
            this.WorkflowProcess = runnerContext.WorkflowProcess;
            this.ProcessInfo = runnerContext.ProcessInfo;
            this.TaskInfo = runnerContext.TaskInfo;
            this.SignalInfo = runnerContext.SignalInfo;
            this.InitiationCause = runnerContext.InitiationCause;
            this.Stage = stage;
            this.WorkflowAPI = runnerContext.WorkflowAPI;
            this.TaskHistoryResolver = runnerContext.TaskHistoryResolver;
            this.RunnerMode = runnerMode;
            this.DirectionAfterInterrupt = directionAfterInterrupt;
            this.ParentProcessTypeName = runnerContext.ParentProcessTypeName;
            this.ParentProcessID = runnerContext.ParentProcessID;
            this.NotMessageHasNoActiveStages = runnerContext.NotMessageHasNoActiveStages;
            this.IsProcessHolderCreated = runnerContext.IsProcessHolderCreated;
            this.CancellationToken = runnerContext.CancellationToken;
        }

        #endregion

        #region IStageTypeHandlerContext Members

        /// <inheritdoc />
        public IMainCardAccessStrategy MainCardAccessStrategy { get; }

        /// <inheritdoc />
        public Guid? MainCardID { get; }

        /// <inheritdoc />
        public CardType? MainCardType { get; }

        /// <inheritdoc />
        public Guid? MainCardDocTypeID { get; }

        /// <inheritdoc />
        public KrComponents? KrComponents { get; }

        /// <inheritdoc />
        public IKrSecondaryProcess? SecondaryProcess { get; }

        /// <inheritdoc />
        public Card? ContextualSatellite { get; }

        /// <inheritdoc />
        public Card? ProcessHolderSatellite { get; }

        /// <inheritdoc />
        public ProcessHolder ProcessHolder { get; }

        /// <inheritdoc />
        public ICardExtensionContext? CardExtensionContext { get; }

        /// <inheritdoc />
        public IValidationResultBuilder ValidationResult { get; }

        /// <inheritdoc />
        public Stage Stage
        {
            get => this.stage!;
            set => this.stage = NotNullOrThrow(value);
        }

        /// <inheritdoc />
        public WorkflowProcess WorkflowProcess { get; }

        /// <inheritdoc />
        public IWorkflowProcessInfo? ProcessInfo { get; }

        /// <inheritdoc />
        public IWorkflowTaskInfo? TaskInfo { get; }

        /// <inheritdoc />
        public IWorkflowSignalInfo? SignalInfo { get; }

        /// <inheritdoc />
        public IWorkflowAPIBridge? WorkflowAPI { get; }

        /// <inheritdoc />
        public IKrTaskHistoryResolver? TaskHistoryResolver { get; }

        /// <inheritdoc />
        public KrProcessRunnerMode RunnerMode { get; }

        /// <inheritdoc />
        public KrProcessRunnerInitiationCause InitiationCause { get; }

        /// <inheritdoc />
        public DirectionAfterInterrupt? DirectionAfterInterrupt { get; }

        /// <inheritdoc />
        public string? ParentProcessTypeName { get; }

        /// <inheritdoc />
        public Guid? ParentProcessID { get; }

        /// <inheritdoc/>
        public bool NotMessageHasNoActiveStages { get; }

        /// <inheritdoc/>
        public IDictionary<string, object?> Info { get; } = new Dictionary<string, object?>();

        /// <inheritdoc/>
        public bool IsProcessHolderCreated { get; }

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; }

        #endregion
    }
}
