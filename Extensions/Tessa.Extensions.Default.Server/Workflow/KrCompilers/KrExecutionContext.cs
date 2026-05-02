#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <inheritdoc cref="IKrExecutionContext"/>
    public sealed class KrExecutionContext :
        IKrExecutionContext
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="KrExecutionContext"/>.
        /// </summary>
        /// <param name="cardContext"><inheritdoc cref="CardContext" path="/summary"/></param>
        /// <param name="mainCardAccessStrategy"><inheritdoc cref="MainCardAccessStrategy" path="/summary"/></param>
        /// <param name="cardID"><inheritdoc cref="CardID" path="/summary"/></param>
        /// <param name="cardType"><inheritdoc cref="CardType" path="/summary"/></param>
        /// <param name="docTypeID"><inheritdoc cref="DocTypeID" path="/summary"/></param>
        /// <param name="krComponents"><inheritdoc cref="KrComponents" path="/summary"/></param>
        /// <param name="workflowProcess"><inheritdoc cref="WorkflowProcess" path="/summary"/></param>
        /// <param name="validationResult"><inheritdoc cref="ValidationResult" path="/summary"/></param>
        /// <param name="secondaryProcess"><inheritdoc cref="SecondaryProcess" path="/summary"/></param>
        /// <param name="executionUnitIDs"><inheritdoc cref="ExecutionUnitIDs" path="/summary"/></param>
        /// <param name="groupID"><inheritdoc cref="GroupID" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        public KrExecutionContext(
            ICardExtensionContext? cardContext,
            IMainCardAccessStrategy mainCardAccessStrategy,
            Guid? cardID,
            CardType? cardType,
            Guid? docTypeID,
            KrComponents? krComponents,
            WorkflowProcess workflowProcess,
            IValidationResultBuilder validationResult,
            IKrSecondaryProcess? secondaryProcess = null,
            ISet<Guid>? executionUnitIDs = null,
            Guid? groupID = null,
            CancellationToken cancellationToken = default)
        {
            this.CardContext = cardContext;
            this.MainCardAccessStrategy = NotNullOrThrow(mainCardAccessStrategy);
            this.CardID = cardID;
            this.CardType = cardType;
            this.DocTypeID = docTypeID;
            this.KrComponents = krComponents;
            this.TypeID = docTypeID ?? cardType?.ID;
            this.WorkflowProcess = NotNullOrThrow(workflowProcess);
            this.ValidationResult = NotNullOrThrow(validationResult);
            this.GroupID = groupID;
            this.SecondaryProcess = secondaryProcess;
            this.ExecutionUnitIDs = executionUnitIDs;
            this.CancellationToken = cancellationToken;
        }

        #endregion

        #region IKrExecutionContext Members

        /// <inheritdoc />
        public ISet<Guid>? ExecutionUnitIDs { get; }

        /// <inheritdoc />
        public IMainCardAccessStrategy MainCardAccessStrategy { get; }

        /// <inheritdoc />
        public Guid? CardID { get; }

        /// <inheritdoc />
        public CardType? CardType { get; }

        /// <inheritdoc />
        public Guid? DocTypeID { get; }

        /// <inheritdoc />
        public Guid? TypeID { get; }

        /// <inheritdoc />
        public KrComponents? KrComponents { get; }

        /// <inheritdoc />
        public WorkflowProcess WorkflowProcess { get; }

        /// <inheritdoc />
        public ICardExtensionContext? CardContext { get; }

        /// <inheritdoc />
        public IKrSecondaryProcess? SecondaryProcess { get; }

        /// <inheritdoc />
        public Guid? GroupID { get; }

        /// <inheritdoc />
        public IValidationResultBuilder ValidationResult { get; }

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; set; }

        /// <inheritdoc />
        public IKrExecutionContext Copy(
            ISet<Guid>? executionUnitIDs = null) =>
            new KrExecutionContext(
                this.CardContext,
                this.MainCardAccessStrategy,
                this.CardID,
                this.CardType,
                this.DocTypeID,
                this.KrComponents,
                this.WorkflowProcess,
                this.ValidationResult,
                this.SecondaryProcess,
                executionUnitIDs,
                this.GroupID,
                this.CancellationToken);

        /// <inheritdoc />
        public IKrExecutionContext Copy(
            Guid? groupID,
            ISet<Guid>? executionUnitIDs = null) =>
            new KrExecutionContext(
                this.CardContext,
                this.MainCardAccessStrategy,
                this.CardID,
                this.CardType,
                this.DocTypeID,
                this.KrComponents,
                this.WorkflowProcess,
                this.ValidationResult,
                this.SecondaryProcess,
                executionUnitIDs,
                groupID,
                this.CancellationToken);

        #endregion
    }
}
