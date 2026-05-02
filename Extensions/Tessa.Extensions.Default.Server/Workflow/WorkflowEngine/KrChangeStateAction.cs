#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Tessa.BusinessCalendar;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Cards;
using Tessa.Extensions.Default.Server.Normalization;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
using Tessa.Platform.Validation;
using Tessa.Workflow;
using Tessa.Workflow.Compilation;
using Tessa.Workflow.Normalization;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    /// <summary>
    /// Обработчик действия <see cref="KrDescriptors.KrChangeStateDescriptor"/>.
    /// </summary>
    public sealed class KrChangeStateAction : KrWorkflowActionBase
    {
        #region Consts

        public const string MainActionSection = "KrChangeStateAction";

        #endregion

        #region Fields

        private readonly IKrTypesCache typesCache;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="KrChangeStateAction"/>.
        /// </summary>
        public KrChangeStateAction(
            ICardRepository cardRepository,
            IKrTypesCache typesCache,
            IWorkflowEngineCardRequestExtender requestExtender,
            IBusinessCalendarService calendarService,
            IKrDocumentStateManager krDocumentStateManager,
            IKrHistoryStrategy historyStrategy,
            IKrWorkflowStateStrategy stateStrategy)
            : base(
                  KrDescriptors.KrChangeStateDescriptor,
                  cardRepository,
                  requestExtender,
                  calendarService,
                  krDocumentStateManager,
                  historyStrategy,
                  stateStrategy)
        {
            this.typesCache = NotNullOrThrow(typesCache);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(
            IWorkflowEngineContext context,
            IWorkflowEngineCompiled? scriptObject)
        {
            await base.ExecuteAsync(context, scriptObject);

            var typeID =
                context.StoreCard?.ID == context.ProcessInstance!.CardID
                ? context.StoreCard.TypeID
                : (await context.GetMainCardAsync(context.CancellationToken))?.TypeID;

            if (!typeID.HasValue)
            {
                return;
            }

            var cardType = (await this.typesCache.GetCardTypesAsync(context.CancellationToken))
                .FirstOrDefault(x => x.ID == typeID);

            if (cardType is null)
            {
                var typeCaption = (await context.CardMetadata.GetCardTypesAsync(context.CancellationToken))[typeID.Value].Caption;
                context.ValidationResult.AddError(
                    this,
                    "$KrActions_ChangeState_TypeNotAllowed",
                    await LocalizeAsync(typeCaption));
                return;
            }

            var stateID = await context.GetAsync<int?>(MainActionSection, "State", "ID");
            if (stateID.HasValue)
            {
                await this.StateStrategy.SetStateIDAsync(
                    context,
                    (KrState) stateID.Value,
                    context.ValidationResult,
                    cancellationToken: context.CancellationToken);
            }
        }

        /// <inheritdoc/>
        protected override WorkflowNormalizationSettings GetNormalizationSettings()
        {
            return new WorkflowNormalizationSettings(
            [
                new WorkflowNormalizationSetting(
                    DefaultNormalizationSources.KrDocStates,
                    MainActionSection,
                    "State",
                    "Name")
            ]);
        }

        #endregion
    }
}
