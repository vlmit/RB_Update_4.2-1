#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Объект, предоставляющий методы для создания и управления заданиями согласования и его дочерними заданиями.
    /// </summary>
    public interface IKrApprovalTaskManager :
        IKrMultiTaskManager<IKrApprovalTaskManagerDataProvider>
    {
    }
}
