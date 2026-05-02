#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Workflow;

using static Tessa.Extensions.Default.Shared.Workflow.KrProcess.KrConstants;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    /// <inheritdoc cref="IKrWorkflowStateStrategy"/>
    public sealed class KrWorkflowStateStrategy :
        IKrWorkflowStateStrategy
    {
        #region Constants

        /// <summary>
        /// Имя ключа, по которому в параметрах текущего действия содержится значение флага управляющего принудительным увеличением версии основной карточки,
        /// даже если других изменений в карточке не было. Если не найден в параметрах действия, то считается равным <c>true</c>. Тип значения: <see cref="bool"/>.
        /// </summary>
        private const string AffectMainCardVersionWhenStateChangedKey = "AffectMainCardVersionWhenStateChanged";

        /// <summary>
        /// Имя ключа, по которому в параметрах текущего процесса содержится идентификатор предыдущего состояния карточки. Тип значения: <see cref="int"/>.
        /// </summary>
        private const string PreviousStateKey = "KrPreviousState";

        #endregion

        #region Fields

        private readonly IKrDocumentStateManager krDocumentStateManager;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="krDocumentStateManager"><inheritdoc cref="IKrDocumentStateManager" path="/summary"/></param>
        public KrWorkflowStateStrategy(IKrDocumentStateManager krDocumentStateManager) =>
            this.krDocumentStateManager = NotNullOrThrow(krDocumentStateManager);

        #endregion

        #region IKrWorkflowStateStrategy Members

        /// <inheritdoc />
        public async ValueTask SetStateIDAsync(
            IWorkflowEngineContext context,
            KrState state,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);
            ThrowIfNull(validationResult);

            var mainCard = await context.GetMainCardAsync(cancellationToken);

            if (mainCard is null)
            {
                return;
            }

            var sCard = await context.GetKrSatelliteAsync(
                validationResult,
                cancellationToken);

            if (sCard is null)
            {
                return;
            }

            var (_, hasMainSatelliteChanges, oldState) = await this.krDocumentStateManager.SetStateAsync(
                mainCard,
                sCard,
                state,
                cancellationToken);

            if (hasMainSatelliteChanges)
            {
                this.StorePreviousState(context, oldState.HasValue ? oldState.Value.ID : KrState.Draft.ID);

                var approvalCommonInfoFields = sCard.GetApprovalInfoSection().Fields;
                approvalCommonInfoFields[KrApprovalCommonInfo.StateChangedDateTimeUTC] = DateTime.UtcNow;

                if (await context.GetAsync<bool?>(AffectMainCardVersionWhenStateChangedKey) ?? true)
                {
                    context.ModifyStoreRequest(static request => request.AffectVersion = true);
                }
            }
        }

        /// <inheritdoc />
        public void StorePreviousState(IWorkflowEngineContext context, int previousState) =>
            NotNullOrThrow(NotNullOrThrow(context).ProcessInstance).Hash[PreviousStateKey] = Int32Boxes.Box(previousState);

        /// <inheritdoc />
        /// <seealso cref="PreviousStateKey"/>
        public int TryGetPreviousState(IWorkflowEngineContext context) =>
            NotNullOrThrow(NotNullOrThrow(context).ProcessInstance).Hash.TryGet<int?>(PreviousStateKey) ?? KrState.Draft.ID;

        #endregion
    }
}
