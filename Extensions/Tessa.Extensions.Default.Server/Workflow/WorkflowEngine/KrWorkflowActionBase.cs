#nullable enable

using System.Threading.Tasks;
using Tessa.BusinessCalendar;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Cards;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
using Tessa.Workflow;
using Tessa.Workflow.Actions;
using Tessa.Workflow.Actions.Descriptors;
using Tessa.Workflow.Compilation;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    /// <summary>
    /// Базовый класс обработчиков действий для типового решения.
    /// </summary>
    public abstract class KrWorkflowActionBase : WorkflowActionBase
    {
        #region Properties

        /// <inheritdoc cref="ICardRepository"/>
        protected ICardRepository CardRepository { get; }

        /// <inheritdoc cref="IKrHistoryStrategy"/>
        protected IKrHistoryStrategy HistoryStrategy { get; }

        /// <inheritdoc cref="IKrWorkflowStateStrategy"/>
        protected IKrWorkflowStateStrategy StateStrategy { get; }

        /// <inheritdoc cref="IWorkflowEngineCardRequestExtender"/>
        protected IWorkflowEngineCardRequestExtender RequestExtender { get; }

        /// <inheritdoc cref="IBusinessCalendarService"/>
        protected IBusinessCalendarService CalendarService { get; }

        /// <inheritdoc cref="IKrDocumentStateManager"/>
        protected IKrDocumentStateManager KrDocumentStateManager { get; }

        #endregion

        #region Constructors

        protected KrWorkflowActionBase(
            WorkflowActionDescriptor actionDescriptor,
            ICardRepository cardRepository,
            IWorkflowEngineCardRequestExtender requestExtender,
            IBusinessCalendarService calendarService,
            IKrDocumentStateManager krDocumentStateManager,
            IKrHistoryStrategy historyStrategy,
            IKrWorkflowStateStrategy stateStrategy)
            : base(actionDescriptor)
        {
            this.CardRepository = NotNullOrThrow(cardRepository);
            this.RequestExtender = NotNullOrThrow(requestExtender);
            this.CalendarService = NotNullOrThrow(calendarService);
            this.KrDocumentStateManager = NotNullOrThrow(krDocumentStateManager);
            this.HistoryStrategy = NotNullOrThrow(historyStrategy);
            this.StateStrategy = NotNullOrThrow(stateStrategy);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override Task ExecuteAsync(
            IWorkflowEngineContext context,
            IWorkflowEngineCompiled? scriptObject)
        {
            context.CardsScope.CardStorePriorityComparer = WorkflowConstants.KrCardStorePriorityComparerDefault;

            return Task.CompletedTask;
        }

        #endregion
    }
}
