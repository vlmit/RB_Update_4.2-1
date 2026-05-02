#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Объект, предоставляющий методы для создания и управления заданиями подписания и его дочерними заданиями.
    /// </summary>
    public interface IKrSigningTaskManager :
        IKrMultiTaskManager<IKrSigningTaskManagerDataProvider>
    {
    }
}
