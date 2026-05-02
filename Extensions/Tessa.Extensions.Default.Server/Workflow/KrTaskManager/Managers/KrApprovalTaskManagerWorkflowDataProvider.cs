#nullable enable

using Tessa.Workflow;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Реализация <see cref="IKrApprovalTaskManagerDataProvider"/>, обеспечивающая взаимодействие с подсистемой Workflow Engine.
    /// </summary>
    /// <param name="dataProviderFactory"><inheritdoc cref="IKrTaskManagerDataProviderFactory" path="/summary"/></param>
    public class KrApprovalTaskManagerWorkflowDataProvider(
        IKrTaskManagerDataProviderFactory dataProviderFactory) :
        KrApprovalTaskManagerDataProviderBase<IWorkflowEngineContext>(dataProviderFactory),
        IKrApprovalTaskManagerDataProvider<IWorkflowEngineContext>
    {
    }
}
