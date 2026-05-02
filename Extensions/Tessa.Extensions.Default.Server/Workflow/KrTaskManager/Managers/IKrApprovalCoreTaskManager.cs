#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Объект, предоставляющий методы для создания и управления заданиями согласования.
    /// </summary>
    public interface IKrApprovalCoreTaskManager :
        IKrSingleTaskManager<IKrApprovalCoreTaskManagerDataProvider>
    {
    }
}
