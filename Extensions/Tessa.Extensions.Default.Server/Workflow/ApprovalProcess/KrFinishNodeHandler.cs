#nullable enable

using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Workflow.ApprovalProcess;
using Tessa.Workflow.ApprovalProcess.Nodes;

namespace Tessa.Extensions.Default.Server.Workflow.ApprovalProcess
{
    /// <inheritdoc/>
    /// <param name="stateManager"><inheritdoc cref="IKrDocumentStateManager" path="/summary"/></param>
    public sealed class KrFinishNodeHandler(IKrDocumentStateManager stateManager) : FinishNodeHandler
    {
        #region Fields

        private readonly IKrDocumentStateManager stateManager = NotNullOrThrow(stateManager);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async ValueTask<NodeHandleResult> OnNodeStartedAsync(IApprovalProcessExecutionContext context)
        {
            var result = await base.OnNodeStartedAsync(context);
            if (result == NodeHandleResult.Faulted)
            {
                return result;
            }

            var instance = await context.GetProcessInstanceAsync();
            if (instance is null)
            {
                return NodeHandleResult.Faulted;
            }

            if (instance.Settings.ChangeStateOnEnd)
            {
                KrState? state = instance.State switch
                {
                    ApprovalProcessState.Approved => KrState.Approved,
                    ApprovalProcessState.Disapproved => KrState.Disapproved,
                    _ => null,
                };

                if (state is null)
                {
                    return result;
                }

                var mainCard = await context.GetCardAsync();
                if (mainCard is null)
                {
                    return NodeHandleResult.Faulted;
                }

                var krSatellite = await context.GetSatelliteAsync(DefaultCardTypes.KrSatelliteTypeID);
                if (krSatellite is null)
                {
                    return NodeHandleResult.Faulted;
                }

                var (_, hasSatelliteChanges, _) = await this.stateManager.SetStateAsync(
                    mainCard,
                    krSatellite,
                    state.Value,
                    context.CancellationToken);

                if (hasSatelliteChanges)
                {
                    context.ModifyStoreRequest(
                        mainCard.ID,
                        (request) => request.AffectVersion = true);
                }
            }

            return result;
        }

        #endregion
    }
}

