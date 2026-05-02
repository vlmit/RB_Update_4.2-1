#nullable enable

using Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers;
using Tessa.Workflow.ApprovalProcess;
using Tessa.Workflow.ApprovalProcess.Nodes;

namespace Tessa.Extensions.Default.Server.Workflow.ApprovalProcess
{
    /// <summary>
    /// Внешний контекст с данными процесса согласования для обработки заданий согласования через <see cref="IKrApprovalTaskManager"/>.
    /// </summary>
    public sealed class KrApprovalProcessExternalContext
    {
        #region Properties

        /// <inheritdoc cref="IApprovalProcessExecutionContext"/>
        public required IApprovalProcessExecutionContext Context { get; init; }

        /// <inheritdoc cref="ApproverNodeData"/>
        public required ApproverNodeData NodeData { get; init; }

        #endregion
    }
}
