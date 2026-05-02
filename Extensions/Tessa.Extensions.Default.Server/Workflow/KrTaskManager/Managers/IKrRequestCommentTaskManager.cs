#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Объект, предоставляющий методы для создания и управления заданиями запроса комментария.
    /// </summary>
    public interface IKrRequestCommentTaskManager :
        IKrSingleTaskManager<IKrRequestCommentTaskManagerDataProvider>
    {
    }
}
