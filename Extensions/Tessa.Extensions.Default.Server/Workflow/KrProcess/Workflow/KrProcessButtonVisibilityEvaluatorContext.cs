#nullable enable

using System;
using System.Threading;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <inheritdoc cref="IKrProcessButtonVisibilityEvaluatorContext"/>
    public sealed class KrProcessButtonVisibilityEvaluatorContext :
        IKrProcessButtonVisibilityEvaluatorContext
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="KrProcessButtonVisibilityEvaluatorContext"/>.
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="ValidationResult" path="/summary"/></param>
        /// <param name="mainCardAccessStrategy"><inheritdoc cref="MainCardAccessStrategy" path="/summary"/></param>
        /// <param name="card"><inheritdoc cref="Card" path="/summary"/></param>
        /// <param name="cardType"><inheritdoc cref="CardType" path="/summary"/></param>
        /// <param name="docTypeID"><inheritdoc cref="DocTypeID" path="/summary"/></param>
        /// <param name="krComponents"><inheritdoc cref="KrComponents" path="/summary"/></param>
        /// <param name="state"><inheritdoc cref="State" path="/summary"/></param>
        /// <param name="cardContext"><inheritdoc cref="CardContext" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        public KrProcessButtonVisibilityEvaluatorContext(
            IValidationResultBuilder validationResult,
            IMainCardAccessStrategy mainCardAccessStrategy,
            Card? card,
            CardType? cardType,
            Guid? docTypeID,
            KrComponents? krComponents,
            KrState? state,
            ICardExtensionContext? cardContext,
            CancellationToken cancellationToken = default)
        {
            this.ValidationResult = NotNullOrThrow(validationResult);
            this.MainCardAccessStrategy = NotNullOrThrow(mainCardAccessStrategy);
            this.Card = card;
            this.CardType = cardType;
            this.DocTypeID = docTypeID;
            this.KrComponents = krComponents;
            this.State = state;
            this.CardContext = cardContext;
            this.CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="KrProcessButtonVisibilityEvaluatorContext"/>. Конструктор используется для инициализации контекста глобальных тайлов.
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="ValidationResult" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        public KrProcessButtonVisibilityEvaluatorContext(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
            : this(
                  validationResult: validationResult,
                  mainCardAccessStrategy: NullMainCardAccessStrategy.Instance,
                  card: null,
                  cardType: null,
                  docTypeID: null,
                  krComponents: null,
                  state: null,
                  cardContext: null,
                  cancellationToken: cancellationToken)
        {
        }

        #endregion

        #region IKrProcessButtonVisibilityEvaluatorContext Members

        /// <inheritdoc />
        public IValidationResultBuilder ValidationResult { get; }

        /// <inheritdoc />
        public IMainCardAccessStrategy MainCardAccessStrategy { get; }

        /// <inheritdoc />
        public Card? Card { get; }

        /// <inheritdoc />
        public CardType? CardType { get; }

        /// <inheritdoc />
        public Guid? DocTypeID { get; }

        /// <inheritdoc />
        public KrComponents? KrComponents { get; }

        /// <inheritdoc />
        public KrState? State { get; }

        /// <inheritdoc />
        public ICardExtensionContext? CardContext { get; }

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; }

        #endregion
    }
}
