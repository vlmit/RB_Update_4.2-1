#nullable enable

using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers;
using Tessa.Workflow;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    [Registrator]
    public sealed class Registrator :
        RegistratorBase
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterSingleton<IKrApprovalCoreTaskManager, KrApprovalCoreTaskManager>()
                .RegisterType<IKrApprovalCoreTaskManagerDataProvider<IStageTypeHandlerContext>, KrApprovalCoreTaskManagerRoutesDataProvider>(new PerResolveLifetimeManager())
                .RegisterType<IKrApprovalCoreTaskManagerDataProvider<IWorkflowEngineContext>, KrApprovalCoreTaskManagerWorkflowDataProvider>(new PerResolveLifetimeManager())

                .RegisterSingleton<IKrSigningCoreTaskManager, KrSigningCoreTaskManager>()
                .RegisterType<IKrSigningCoreTaskManagerDataProvider<IStageTypeHandlerContext>, KrSigningCoreTaskManagerRoutesDataProvider>(new PerResolveLifetimeManager())
                .RegisterType<IKrSigningCoreTaskManagerDataProvider<IWorkflowEngineContext>, KrSigningCoreTaskManagerWorkflowDataProvider>(new PerResolveLifetimeManager())

                .RegisterSingleton<IKrAdditionalApprovalTaskManager, KrAdditionalApprovalTaskManager>()
                .RegisterType<IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider<IStageTypeHandlerContext>, KrAdditionalApprovalTaskManagerWithParentApprovalTaskRoutesDataProvider>(new PerResolveLifetimeManager())
                .RegisterType<IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider<IWorkflowEngineContext>, KrAdditionalApprovalTaskManagerWithParentApprovalTaskWorkflowDataProvider>(new PerResolveLifetimeManager())
                .RegisterType<IKrAdditionalApprovalTaskManagerWithParentTaskDataProvider, KrAdditionalApprovalTaskManagerWithParentTaskDataProvider>(new PerResolveLifetimeManager())

                .RegisterSingleton<IKrEditInterjectTaskManager, KrEditInterjectTaskManager>()
                .RegisterType<IKrEditInterjectTaskManagerWithParentTaskDataProvider<IWorkflowEngineContext>, KrEditInterjectTaskManagerWithParentTaskWorkflowDataProvider>(new PerResolveLifetimeManager())

                .RegisterSingleton<IKrRequestCommentTaskManager, KrRequestCommentTaskManager>()
                .RegisterType<IKrRequestCommentTaskManagerWithParentTaskDataProvider, KrRequestCommentTaskManagerWithParentTaskDataProvider>(new PerResolveLifetimeManager())

                .RegisterSingleton<IKrApprovalTaskManager, KrApprovalTaskManager>()
                .RegisterType<IKrApprovalTaskManagerDataProvider<IStageTypeHandlerContext>, KrApprovalTaskManagerRoutesDataProvider>(new PerResolveLifetimeManager())
                .RegisterType<IKrApprovalTaskManagerDataProvider<IWorkflowEngineContext>, KrApprovalTaskManagerWorkflowDataProvider>(new PerResolveLifetimeManager())

                .RegisterSingleton<IKrSigningTaskManager, KrSigningTaskManager>()
                .RegisterType<IKrSigningTaskManagerDataProvider<IStageTypeHandlerContext>, KrSigningTaskManagerRoutesDataProvider>(new PerResolveLifetimeManager())
                .RegisterType<IKrSigningTaskManagerDataProvider<IWorkflowEngineContext>, KrSigningTaskManagerWorkflowDataProvider>(new PerResolveLifetimeManager())
                ;

        #endregion
    }
}
