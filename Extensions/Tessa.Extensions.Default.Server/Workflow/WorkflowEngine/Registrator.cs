#nullable enable

using Tessa.Extensions.Default.Server.Workflow.WorkflowEngine.Upgrade;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform;
using Tessa.Workflow;
using Tessa.Workflow.Actions;
using Tessa.Workflow.Upgrade;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterSingleton<IKrWorkflowStateStrategy, KrWorkflowStateStrategy>()
                .RegisterSingleton<IWorkflowEngineCardRequestExtender, KrWorkflowEngineCardRequestExtender>()
                .RegisterSingleton<IWorkflowAction, KrChangeStateAction>(nameof(KrChangeStateAction))
                .RegisterSingleton<IWorkflowAction, WorkflowCreateCardAction>(nameof(WorkflowCreateCardAction))
                .RegisterSingleton<IWorkflowAction, KrAcquaintanceAction>(nameof(KrAcquaintanceAction))
                .RegisterSingleton<IWorkflowAction, KrRegistrationAction>(nameof(KrRegistrationAction))
                .RegisterSingleton<IWorkflowAction, KrDeregistrationAction>(nameof(KrDeregistrationAction))
                .RegisterSingleton<IWorkflowAction, WorkflowDialogAction>(nameof(WorkflowDialogAction))
                .RegisterSingleton<IWorkflowAction, KrTaskRegistrationAction>(nameof(KrTaskRegistrationAction))
                .RegisterSingleton<IWorkflowAction, KrApprovalAction>(nameof(KrApprovalAction))
                .RegisterSingleton<IWorkflowAction, KrSigningAction>(nameof(KrSigningAction))
                .RegisterSingleton<IWorkflowAction, KrAmendingAction>(nameof(KrAmendingAction))
                .RegisterSingleton<IWorkflowAction, KrUniversalTaskAction>(nameof(KrUniversalTaskAction))
                .RegisterSingleton<IWorkflowAction, KrResolutionAction>(nameof(KrResolutionAction))
                .RegisterSingleton<IWorkflowAction, KrRouteInitializationAction>(nameof(KrRouteInitializationAction))
                .RegisterSingleton<IWorkflowEngineTileManagerExtension, KrCheckStateTileManagerExtension>(nameof(KrCheckStateTileManagerExtension));

        public override void FinalizeRegistration() =>
            this.UnityContainer
                .TryResolve<IWorkflowEngineActionUpgradeHandlersRegistry>()?
                .Register<KrUniversalTaskWithTaskRolesActionUpgradeHandler>(DefaultCardTypes.KrUniversalTaskActionTypeID, 2)
                .Register<KrApprovalAndSigningPerformersFormatActionUpgradeHandler>(DefaultCardTypes.KrApprovalActionTypeID,
                    KrApprovalAndSigningPerformersFormatActionUpgradeHandler.UpgradeToVersion)
                .Register<KrApprovalAndSigningPerformersFormatActionUpgradeHandler>(DefaultCardTypes.KrSigningActionTypeID,
                    KrApprovalAndSigningPerformersFormatActionUpgradeHandler.UpgradeToVersion);
    }
}
