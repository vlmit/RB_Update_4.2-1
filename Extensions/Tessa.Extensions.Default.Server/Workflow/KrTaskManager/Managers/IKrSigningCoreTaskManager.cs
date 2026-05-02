#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Объект, предоставляющий методы для создания и управления заданиями подписания.
    /// </summary>
    public interface IKrSigningCoreTaskManager :
        IKrSingleTaskManager<IKrSigningCoreTaskManagerDataProvider>
    {
    }
}
