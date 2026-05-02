#nullable enable

using Tessa.Roles;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Объект, обеспечивающий передачу данных между внешней подсистемой и обработчиком <see cref="IKrRequestCommentTaskManager"/>.
    /// </summary>
    public interface IKrRequestCommentTaskManagerDataProvider :
        IKrTaskWithParametersManagerDataProvider<IRoleUser>
    {
    }
}
