#nullable enable

using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Workflow.ApprovalProcess;
using Tessa.Workflow.ApprovalProcess.Nodes;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Server.Workflow.ApprovalProcess
{
    [Registrator]
    public sealed class Registrator :
        RegistratorBase
    {
        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterSingleton<IApprovalProcessAccessManager, KrApprovalProcessAccessManager>()
                .RegisterSingleton<KrApproverNodeHandler>()
                .RegisterSingleton<KrFinishNodeHandler>()
                .RegisterType<ITaskPermissionsExtension, ApprovalSchemeTaskPermissionsExtension>(new ContainerControlledLifetimeManager())
                .RegisterType<IKrTaskManagerContext<KrApprovalProcessExternalContext>, KrApprovalProcessTaskManagerContext>(new PerResolveLifetimeManager())
                .RegisterType<IKrTaskManagerContext<KrApprovalProcessExternalContext>, KrApprovalProcessTaskManagerContext>(new PerResolveLifetimeManager())
                .RegisterType<IKrApprovalTaskManagerDataProvider<KrApprovalProcessExternalContext>, KrApprovalProcessTaskManagerDataProvider>(new PerResolveLifetimeManager())
                .RegisterType<IKrApprovalCoreTaskManagerDataProvider<KrApprovalProcessExternalContext>, KrApprovalProcessCoreTaskManagerDataProvider>(new PerResolveLifetimeManager())
                .RegisterType<IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider<KrApprovalProcessExternalContext>, KrApprovalProcessAdditionalApprovalTaskManagerDataProvider>(new PerResolveLifetimeManager())
                .RegisterSingleton<KrStartApprovalProcessRoutesTileHandler>()
                .RegisterSingleton<IApprovalProcessRunner, KrRoutesApprovalProcessRunner>(KrConstants.KrProcessName)
                .RegisterSingleton<IApprovalProcessRunner, KrRoutesApprovalProcessRunner>(KrConstants.KrSecondaryProcessName)
                .RegisterSingleton<IApprovalProcessRunner, KrRoutesApprovalProcessRunner>(KrConstants.KrNestedProcessName)
                ;

        public override void RegisterExtensions(IExtensionContainer extensionContainer) =>
            extensionContainer
                .RegisterExtension<ITaskPermissionsExtension, ApprovalSchemeTaskPermissionsExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithUnity(this.UnityContainer))
                ;

        public override void FinalizeRegistration()
        {
            this.UnityContainer
                .TryResolve<IApprovalProcessNodeResolver>()
                ?.Register<KrApproverNodeHandler>(NodeTypes.Approval)
                .Register<KrFinishNodeHandler>(NodeTypes.Finish)
                ;

            this.UnityContainer
                .TryResolve<IKrSecondaryProcessTileHandlerResolver>()
                ?.Register<KrStartApprovalProcessRoutesTileHandler>(KrStartApprovalProcessRoutesTileHandler.Descriptor)
                ;
        }
    }
}
