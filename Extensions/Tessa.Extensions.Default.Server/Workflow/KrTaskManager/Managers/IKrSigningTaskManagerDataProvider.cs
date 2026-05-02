#nullable enable

using Tessa.Roles;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Объект, обеспечивающий передачу данных между внешней подсистемой и обработчиком <see cref="IKrSigningTaskManager"/>.
    /// </summary>
    public interface IKrSigningTaskManagerDataProvider :
        IKrTaskWithActionsManagerDataProvider<IRoleUser>,
        IKrTaskManagerNestedDataProviderProvider
    {
    }
}
