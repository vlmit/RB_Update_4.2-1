#nullable enable

using System.Linq;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager;
using Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers;

namespace Tessa.Extensions.Default.Server.Workflow.ApprovalProcess
{
    /// <summary>
    /// Реализация <see cref="IKrApprovalTaskManagerDataProvider"/>, обеспечивающая взаимодействие с подсистемой процессов согласования.
    /// </summary>
    /// <param name="dataProviderFactory"><inheritdoc cref="IKrTaskManagerDataProviderFactory" path="/summary"/></param>
    public class KrApprovalProcessTaskManagerDataProvider(
        IKrTaskManagerDataProviderFactory dataProviderFactory) :
        KrApprovalTaskManagerDataProviderBase<KrApprovalProcessExternalContext>(dataProviderFactory),
        IKrApprovalTaskManagerDataProvider<KrApprovalProcessExternalContext>
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override void ConfigureNested<TDataProvider>(TDataProvider dataProvider)
        {
            if (dataProvider is IKrApprovalCoreTaskManagerDataProvider approvalCoreTaskManagerDataProvider)
            {
                approvalCoreTaskManagerDataProvider.IsNegativeActionResult =
                    this.ExternalContext.NodeData.Approvers.Any(x => x.TaskInfo?.CompletionInfo?.CompletionState == Tessa.Workflow.ApprovalProcess.Nodes.ApproverNodeCompletionState.Disapproved);
                approvalCoreTaskManagerDataProvider.CurrentPerformerIndex =
                    this.ExternalContext.NodeData.Approvers.Count(x => !x.SkipOnCurrentCycle && x.TaskInfo?.CompletionInfo is not null);
            }
        }

        #endregion
    }
}
