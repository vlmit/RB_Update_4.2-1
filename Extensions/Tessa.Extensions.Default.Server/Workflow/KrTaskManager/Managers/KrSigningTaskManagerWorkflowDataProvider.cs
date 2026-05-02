#nullable enable

using Tessa.Workflow;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Реализация <see cref="IKrSigningTaskManagerDataProvider"/>, обеспечивающая взаимодействие с подсистемой  Workflow Engine.
    /// </summary>
    /// <param name="dataProviderFactory"><inheritdoc cref="IKrTaskManagerDataProviderFactory" path="/summary"/></param>
    public class KrSigningTaskManagerWorkflowDataProvider(
        IKrTaskManagerDataProviderFactory dataProviderFactory) :
        KrSigningTaskManagerDataProviderBase<IWorkflowEngineContext>(dataProviderFactory),
        IKrSigningTaskManagerDataProvider<IWorkflowEngineContext>
    {
    }
}
