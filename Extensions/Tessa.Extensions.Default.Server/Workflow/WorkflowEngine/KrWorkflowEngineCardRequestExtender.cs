#nullable enable

using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow;
using Tessa.Workflow;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    /// <summary>
    /// Реализация <see cref="IWorkflowEngineCardRequestExtender"/>, обеспечивающая работу совместно с подсистемой маршрутов.
    /// </summary>
    /// <param name="permissionsProvider"><inheritdoc cref="ICardServerPermissionsProvider" path="/summary"/></param>
    public class KrWorkflowEngineCardRequestExtender(ICardServerPermissionsProvider permissionsProvider) :
        WorkflowEngineCardRequestExtender(permissionsProvider)
    {
        #region Base Overrides

        /// <inheritdoc />
        public override void ExtendGetRequest(CardGetRequest request)
        {
            base.ExtendGetRequest(request);

            request.SetIgnoreStoreKrSatelliteInKrScope();
        }

        #endregion
    }
}
