#nullable enable

using System.Threading.Tasks;
using Tessa.BusinessCalendar;
using Tessa.Cards;
using Tessa.Cards.Numbers;
using Tessa.Extensions.Default.Server.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
using Tessa.Workflow;
using Tessa.Workflow.Compilation;
using Tessa.Workflow.Helpful;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    /// <summary>
    /// Обработчик действия <see cref="KrDescriptors.DeregistrationDescriptor"/>.
    /// </summary>
    public sealed class KrDeregistrationAction : KrWorkflowActionBase
    {
        #region Fields

        private readonly INumberDirectorContainer numberDirectorContainer;
        private readonly IKrStageSerializer krStageSerializer;

        #endregion

        #region Constructors

        public KrDeregistrationAction(
            ICardRepository cardRepository,
            IWorkflowEngineCardRequestExtender requestExtender,
            INumberDirectorContainer numberDirectorContainer,
            IBusinessCalendarService calendarService,
            IKrStageSerializer krStageSerializer,
            IKrDocumentStateManager krDocumentStateManager,
            IKrHistoryStrategy historyStrategy,
            IKrWorkflowStateStrategy stateStrategy)
            : base(
                  KrDescriptors.DeregistrationDescriptor,
                  cardRepository,
                  requestExtender,
                  calendarService,
                  krDocumentStateManager,
                  historyStrategy,
                  stateStrategy)
        {
            this.numberDirectorContainer = NotNullOrThrow(numberDirectorContainer);
            this.krStageSerializer = NotNullOrThrow(krStageSerializer);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(IWorkflowEngineContext context, IWorkflowEngineCompiled? scriptObject)
        {
            await base.ExecuteAsync(context, scriptObject);

            var mainCard = await context.GetMainCardAsync(context.CancellationToken);
            if (mainCard is null)
            {
                return;
            }

            var cardType = (await context.CardMetadata.GetCardTypesAsync(context.CancellationToken))[mainCard.TypeID];

            // выделение номера при регистрации
            var numberProvider = this.numberDirectorContainer.GetProvider(cardType.ID);
            var numberDirector = numberProvider.GetDirector();
            var numberComposer = numberProvider.GetComposer();
            var numberContext = await numberDirector.CreateContextAsync(
                numberComposer,
                mainCard,
                cardType,
                CardServiceType.Default,
                transactionMode: NumberTransactionMode.SeparateTransaction,
                cancellationToken: context.CancellationToken);

            await numberDirector.NotifyOnDeregisteringCardAsync(numberContext, context.CancellationToken);
            context.ValidationResult.Add(numberContext.ValidationResult);

            if (context.ValidationResult.IsSuccessful())
            {
               var taskHistoryItem = await this.HistoryStrategy.CreateTaskHistoryAsync(
                    KrConstants.KrDeregistrationTypeID,
                    KrConstants.KrDeregistrationTypeName,
                    "$CardTypes_TypesNames_KrDeregistration",
                    DefaultCompletionOptions.DeregisterDocument,
                    "$ApprovalHistory_DocumentDeregistered",
                    context.ValidationResult,
                    groupRowID: context.ProcessInstance!.GetHistoryGroup(),
                    storeDateTime: context.StoreDateTime,
                    cancellationToken: context.CancellationToken);

                if (taskHistoryItem is null)
                {
                    return;
                }

                mainCard.TaskHistory.Add(taskHistoryItem);

                var sCard = await context.GetKrSatelliteAsync();

                if (sCard is null)
                {
                    return;
                }

                var krProcessInfo = ProcessInfoCacheHelper.Get(this.krStageSerializer, sCard);
                var prevStateID = krProcessInfo.TryGetValue(KrConstants.Keys.StateBeforeRegistration, out var stateIDObj)
                    ? (int?) stateIDObj ?? KrState.Draft.ID
                    : this.StateStrategy.TryGetPreviousState(context);

                await this.StateStrategy.SetStateIDAsync(
                    context,
                    (KrState) prevStateID,
                    context.ValidationResult,
                    context.CancellationToken);
            }
        }

        #endregion
    }
}
