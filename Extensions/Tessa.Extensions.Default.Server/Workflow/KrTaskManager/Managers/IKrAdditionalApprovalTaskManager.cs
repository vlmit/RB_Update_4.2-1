#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Объект, предоставляющий методы для создания и управления заданиями дополнительного согласования.
    /// </summary>
    public interface IKrAdditionalApprovalTaskManager :
        IKrSingleTaskManager<IKrAdditionalApprovalTaskManagerDataProvider>
    {
    }
}
