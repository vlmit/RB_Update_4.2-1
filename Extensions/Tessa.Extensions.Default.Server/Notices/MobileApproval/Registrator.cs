#nullable enable

using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.Wf;
using Tessa.Notices.Extensions;
using Unity;

namespace Tessa.Extensions.Default.Server.Notices.MobileApproval
{
    [Registrator]
    public sealed class Registrator :
        RegistratorBase
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterSingleton<ApprovalMobileApprovalNotificationSendExtension>()
                .RegisterSingleton<SigningMobileApprovalNotificationSendExtension>()
                .RegisterSingleton<ResolutionMobileApprovalNotificationSendExtension>()
            ;

        /// <inheritdoc/>
        public override void RegisterExtensions(IExtensionContainer extensionContainer) =>
            extensionContainer

                // AfterPlatform
                .RegisterExtension<INotificationSendExtension, ApprovalMobileApprovalNotificationSendExtension>(x => x
                    .WithUnity(this.UnityContainer)
                    .WithOrder(ExtensionStage.AfterPlatform, 0)
                    .WhenAnyNotificationTypes()
                    .WhenTaskTypes(
                        DefaultTaskTypes.KrApproveTypeID,
                        DefaultTaskTypes.KrAdditionalApprovalTypeID))
                .RegisterExtension<INotificationSendExtension, SigningMobileApprovalNotificationSendExtension>(x => x
                    .WithUnity(this.UnityContainer)
                    .WithOrder(ExtensionStage.AfterPlatform, 0)
                    .WhenAnyNotificationTypes()
                    .WhenTaskTypes(DefaultTaskTypes.KrSigningTypeID))
                .RegisterExtension<INotificationSendExtension, ResolutionMobileApprovalNotificationSendExtension>(x => x
                    .WithUnity(this.UnityContainer)
                    .WithOrder(ExtensionStage.AfterPlatform, 0)
                    .WhenAnyNotificationTypes()
                    .WhenFunc<INotificationSendExtensionContext>(static context =>
                        context.TaskTypeID.HasValue && WfHelper.TaskTypeIsResolution(context.TaskTypeID.Value)))

                // Finalize
                .RegisterExtension<INotificationSendExtension, FinalizeMobileApprovalNotificationSendExtension>(x => x
                    .WithSingleton()
                    .WithOrder(ExtensionStage.Finalize, 0)
                    .WhenAnyNotificationTypes()
                    .WhenAnyTaskType())
            ;

        #endregion
    }
}
