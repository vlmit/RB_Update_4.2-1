#nullable enable

using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Реализация <see cref="IKrAdditionalApprovalTaskManagerDataProvider"/>, обеспечивающая взаимодействие с родительским заданием.
    /// </summary>
    public interface IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider :
        IKrAdditionalApprovalTaskManagerDataProvider
    {
        /// <summary>
        /// Родительское задание.
        /// </summary>
        CardTask ParentTask { get; set; }

        /// <summary>
        /// Исполнитель родительского задания.
        /// </summary>
        RoleEntryStorage ParentTaskPerformer { get; set; }
    }
}
