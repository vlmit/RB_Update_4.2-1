#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Объект, предоставляющий методы для создания и управления заданиями доработки после согласования.
    /// </summary>
    public interface IKrEditInterjectTaskManager :
        IKrSingleTaskManager<IKrEditInterjectTaskManagerDataProvider>
    {
    }
}
