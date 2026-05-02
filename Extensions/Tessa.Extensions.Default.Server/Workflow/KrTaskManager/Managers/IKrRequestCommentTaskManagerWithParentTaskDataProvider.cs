#nullable enable

using Tessa.Cards;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Реализация <see cref="IKrAdditionalApprovalTaskManagerDataProvider"/>, обеспечивающая получение данных из родительского задания.
    /// </summary>
    public interface IKrRequestCommentTaskManagerWithParentTaskDataProvider :
        IKrRequestCommentTaskManagerDataProvider
    {
        /// <summary>
        /// Родительское задание.
        /// </summary>
        CardTask ParentTask { get; set; }
    }
}
