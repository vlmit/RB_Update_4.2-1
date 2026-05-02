#nullable enable

using System;
using System.Threading;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers.SqlProcessing
{
    /// <inheritdoc cref="IKrSqlExecutorContext"/>
    public sealed class KrSqlExecutorContext :
        IKrSqlExecutorContext
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="query"><inheritdoc cref="Query" path="/summary"/></param>
        /// <param name="validationResult"><inheritdoc cref="ValidationResult" path="/summary"/></param>
        /// <param name="getErrorTextFunc"><inheritdoc cref="GetErrorTextFunc" path="/summary"/></param>
        /// <param name="secondaryProcess"><inheritdoc cref="SecondaryProcess" path="/summary"/></param>
        /// <param name="stageGroupID"><inheritdoc cref="StageGroupID" path="/summary"/></param>
        /// <param name="stageTypeID"><inheritdoc cref="StageTypeID" path="/summary"/></param>
        /// <param name="stageTemplateID"><inheritdoc cref="StageTemplateID" path="/summary"/></param>
        /// <param name="stageRowID"><inheritdoc cref="StageRowID" path="/summary"/></param>
        /// <param name="userID"><inheritdoc cref="UserID" path="/summary"/></param>
        /// <param name="userName"><inheritdoc cref="UserName" path="/summary"/></param>
        /// <param name="cardID"><inheritdoc cref="CardID" path="/summary"/></param>
        /// <param name="cardTypeID"><inheritdoc cref="CardTypeID" path="/summary"/></param>
        /// <param name="docTypeID"><inheritdoc cref="DocTypeID" path="/summary"/></param>
        /// <param name="state"><inheritdoc cref="State" path="/summary"/></param>
        /// <param name="stageName"><inheritdoc cref="StageName" path="/summary"/></param>
        /// <param name="templateName"><inheritdoc cref="TemplateName" path="/summary"/></param>
        /// <param name="groupName"><inheritdoc cref="GroupName" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        public KrSqlExecutorContext(
            string? query,
            IValidationResultBuilder validationResult,
            Func<IKrSqlExecutorContext, string, object[], string> getErrorTextFunc,
            IKrSecondaryProcess? secondaryProcess,
            Guid stageGroupID,
            Guid stageTypeID,
            Guid stageTemplateID,
            Guid stageRowID,
            Guid? userID,
            string? userName,
            Guid? cardID,
            Guid? cardTypeID,
            Guid? docTypeID,
            KrState? state,
            string? stageName,
            string? templateName,
            string? groupName,
            CancellationToken cancellationToken)
        {
            this.Query = query;
            this.ValidationResult = NotNullOrThrow(validationResult);
            this.GetErrorTextFunc = NotNullOrThrow(getErrorTextFunc);
            this.SecondaryProcess = secondaryProcess;
            this.StageGroupID = stageGroupID;
            this.StageTypeID = stageTypeID;
            this.StageTemplateID = stageTemplateID;
            this.StageRowID = stageRowID;
            this.UserID = userID;
            this.UserName = userName;
            this.CardID = cardID;
            this.CardTypeID = cardTypeID;
            this.DocTypeID = docTypeID;
            this.State = state;
            this.StageName = stageName;
            this.TemplateName = templateName;
            this.GroupName = groupName;
            this.CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса для вычисления условий этапов, шаблонов, групп.
        /// </summary>
        /// <param name="query"><inheritdoc cref="Query" path="/summary"/></param>
        /// <param name="validationResult"><inheritdoc cref="ValidationResult" path="/summary"/></param>
        /// <param name="getErrorTextFunc"><inheritdoc cref="GetErrorTextFunc" path="/summary"/></param>
        /// <param name="unit"><inheritdoc cref="IKrExecutionUnit" path="/summary"/></param>
        /// <param name="secondaryProcess"><inheritdoc cref="SecondaryProcess" path="/summary"/></param>
        /// <param name="cardID"><inheritdoc cref="CardID" path="/summary"/></param>
        /// <param name="cardTypeID"><inheritdoc cref="CardTypeID" path="/summary"/></param>
        /// <param name="docTypeID"><inheritdoc cref="DocTypeID" path="/summary"/></param>
        /// <param name="state"><inheritdoc cref="State" path="/summary"/></param>
        /// <param name="userID"><inheritdoc cref="UserID" path="/summary"/></param>
        /// <param name="userName"><inheritdoc cref="UserName" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        public KrSqlExecutorContext(
            string? query,
            IValidationResultBuilder validationResult,
            Func<IKrSqlExecutorContext, string, object[], string> getErrorTextFunc,
            IKrExecutionUnit unit,
            IKrSecondaryProcess? secondaryProcess,
            Guid? cardID,
            Guid? cardTypeID,
            Guid? docTypeID,
            KrState state,
            Guid? userID = null,
            string? userName = null,
            CancellationToken cancellationToken = default)
            : this(
                  query: query,
                  validationResult: validationResult,
                  getErrorTextFunc: getErrorTextFunc,
                  secondaryProcess: secondaryProcess,
                  stageGroupID: NotNullOrThrow(unit).StageGroup?.ID ?? unit.StageTemplate?.StageGroupID ?? Guid.Empty,
                  stageTypeID: Guid.Empty,
                  stageTemplateID: unit.StageTemplate?.ID ?? Guid.Empty,
                  stageRowID: Guid.Empty,
                  userID: userID,
                  userName: userName,
                  cardID: cardID,
                  cardTypeID: cardTypeID,
                  docTypeID: docTypeID,
                  state: state,
                  stageName: unit.RuntimeStage?.StageName,
                  templateName: unit.StageTemplate?.Name,
                  groupName: unit.StageGroup?.Name ?? unit.StageTemplate?.StageGroupName,
                  cancellationToken: cancellationToken)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса для пересчета SQL-исполнителей в этапе.
        /// </summary>
        /// <param name="stage"><inheritdoc cref="Stage" path="/summary"/></param>
        /// <param name="validationResult"><inheritdoc cref="ValidationResult" path="/summary"/></param>
        /// <param name="getErrorTextFunc"><inheritdoc cref="GetErrorTextFunc" path="/summary"/></param>
        /// <param name="secondaryProcess"><inheritdoc cref="SecondaryProcess" path="/summary"/></param>
        /// <param name="cardID"><inheritdoc cref="CardID" path="/summary"/></param>
        /// <param name="cardTypeID"><inheritdoc cref="CardTypeID" path="/summary"/></param>
        /// <param name="docTypeID"><inheritdoc cref="DocTypeID" path="/summary"/></param>
        /// <param name="state"><inheritdoc cref="State" path="/summary"/></param>
        /// <param name="userID"><inheritdoc cref="UserID" path="/summary"/></param>
        /// <param name="userName"><inheritdoc cref="UserName" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        public KrSqlExecutorContext(
            Stage stage,
            IValidationResultBuilder validationResult,
            Func<IKrSqlExecutorContext, string, object[], string> getErrorTextFunc,
            IKrSecondaryProcess? secondaryProcess,
            Guid? cardID,
            Guid? cardTypeID,
            Guid? docTypeID,
            KrState state,
            Guid? userID = null,
            string? userName = null,
            CancellationToken cancellationToken = default)
            : this(
                  query: NotNullOrThrow(stage).SqlPerformers,
                  validationResult: validationResult,
                  getErrorTextFunc: getErrorTextFunc,
                  secondaryProcess: secondaryProcess,
                  stageGroupID: stage.StageGroupID,
                  stageTypeID: stage.StageTypeID ?? Guid.Empty,
                  stageTemplateID: stage.TemplateID ?? Guid.Empty,
                  stageRowID: stage.RowID,
                  userID: userID,
                  userName: userName,
                  cardID: cardID,
                  cardTypeID: cardTypeID,
                  docTypeID: docTypeID,
                  state: state,
                  stageName: stage.Name,
                  templateName: stage.TemplateName,
                  groupName: stage.StageGroupName,
                  cancellationToken: cancellationToken)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="query"><inheritdoc cref="Query" path="/summary"/></param>
        /// <param name="validationResult"><inheritdoc cref="ValidationResult" path="/summary"/></param>
        /// <param name="getErrorTextFunc"><inheritdoc cref="GetErrorTextFunc" path="/summary"/></param>
        /// <param name="secondaryProcess"><inheritdoc cref="SecondaryProcess" path="/summary"/></param>
        /// <param name="cardID"><inheritdoc cref="CardID" path="/summary"/></param>
        /// <param name="cardTypeID"><inheritdoc cref="CardTypeID" path="/summary"/></param>
        /// <param name="docTypeID"><inheritdoc cref="DocTypeID" path="/summary"/></param>
        /// <param name="state"><inheritdoc cref="State" path="/summary"/></param>
        /// <param name="userID"><inheritdoc cref="UserID" path="/summary"/></param>
        /// <param name="userName"><inheritdoc cref="UserName" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        public KrSqlExecutorContext(
            string? query,
            IValidationResultBuilder validationResult,
            Func<IKrSqlExecutorContext, string, object[], string> getErrorTextFunc,
            IKrSecondaryProcess? secondaryProcess,
            Guid? cardID,
            Guid? cardTypeID,
            Guid? docTypeID,
            KrState? state,
            Guid? userID = null,
            string? userName = null,
            CancellationToken cancellationToken = default)
            : this(
                  query: query,
                  validationResult: validationResult,
                  getErrorTextFunc: getErrorTextFunc,
                  secondaryProcess: secondaryProcess,
                  stageGroupID: Guid.Empty,
                  stageTypeID: Guid.Empty,
                  stageTemplateID: Guid.Empty,
                  stageRowID: Guid.Empty,
                  userID: userID,
                  userName: userName,
                  cardID: cardID,
                  cardTypeID: cardTypeID,
                  docTypeID: docTypeID,
                  state: state,
                  stageName: null,
                  templateName: null,
                  groupName: null,
                  cancellationToken: cancellationToken)
        {
        }

        #endregion

        #region IKrSqlExecutorContext Members

        /// <inheritdoc />
        public string? Query { get; }

        /// <inheritdoc />
        public IValidationResultBuilder ValidationResult { get; }

        /// <inheritdoc />
        public Func<IKrSqlExecutorContext, string, object[], string> GetErrorTextFunc { get; }

        /// <inheritdoc />
        public IKrSecondaryProcess? SecondaryProcess { get; }

        /// <inheritdoc />
        public Guid StageGroupID { get; }

        /// <inheritdoc />
        public Guid StageTypeID { get; }

        /// <inheritdoc />
        public Guid StageTemplateID { get; }

        /// <inheritdoc />
        public Guid StageRowID { get; }

        /// <inheritdoc />
        public Guid? UserID { get; }

        /// <inheritdoc />
        public string? UserName { get; }

        /// <inheritdoc />
        public Guid? CardID { get; }

        /// <inheritdoc />
        public Guid? CardTypeID { get; }

        /// <inheritdoc />
        public Guid? DocTypeID { get; }

        /// <inheritdoc />
        public Guid? TypeID => this.DocTypeID ?? this.CardTypeID;

        /// <inheritdoc />
        public KrState? State { get; }

        /// <inheritdoc />
        public string? StageName { get; }

        /// <inheritdoc />
        public string? TemplateName { get; }

        /// <inheritdoc />
        public string? GroupName { get; }

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; set; }

        #endregion
    }
}
