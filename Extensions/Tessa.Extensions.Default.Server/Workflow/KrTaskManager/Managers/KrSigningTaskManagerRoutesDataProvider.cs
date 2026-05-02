#nullable enable

using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Реализация <see cref="IKrSigningTaskManagerDataProvider"/>, обеспечивающая взаимодействие с подсистемой маршрутов.
    /// </summary>
    /// <param name="dataProviderFactory"><inheritdoc cref="IKrTaskManagerDataProviderFactory" path="/summary"/></param>
    public class KrSigningTaskManagerRoutesDataProvider(
        IKrTaskManagerDataProviderFactory dataProviderFactory) :
        KrSigningTaskManagerDataProviderBase<IStageTypeHandlerContext>(dataProviderFactory),
        IKrSigningTaskManagerDataProvider<IStageTypeHandlerContext>
    {
    }
}
