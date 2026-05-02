#nullable enable

using Tessa.Cards;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Реализация <see cref="IKrAdditionalApprovalTaskManagerDataProvider"/>, обеспечивающая получение данных из родительского задания дополнительного согласования.
    /// </summary>
    public interface IKrAdditionalApprovalTaskManagerWithParentTaskDataProvider :
        IKrAdditionalApprovalTaskManagerDataProvider
    {
        /// <summary>
        /// Родительское задание.
        /// </summary>
        CardTask ParentTask { get; set; }
    }
}
