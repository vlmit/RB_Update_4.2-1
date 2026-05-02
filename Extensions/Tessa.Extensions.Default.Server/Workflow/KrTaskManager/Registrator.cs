#nullable enable

using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers;
using Tessa.Workflow;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    [Registrator]
    public sealed class Registrator :
        RegistratorBase
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterType<IKrTaskManagerContext<IStageTypeHandlerContext>, KrRoutesTaskManagerContext>(new PerResolveLifetimeManager())
                .RegisterType<IKrTaskManagerContext<IWorkflowEngineContext>, KrWorkflowTaskManagerContext>(new PerResolveLifetimeManager())
                .RegisterSingleton<IKrTaskManagerDataProviderFactory, KrTaskManagerDataProviderFactory>()
                .RegisterSingleton<IKrTaskManagerContextFactory, KrTaskManagerContextFactory>()
                ;

        #endregion
    }
}
