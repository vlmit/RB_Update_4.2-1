#nullable enable

using Tessa.Extensions.Default.Shared.Workflow;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Объект, обеспечивающий передачу данных между внешней подсистемой и обработчиком <see cref="IKrAdditionalApprovalTaskManager"/>.
    /// </summary>
    public interface IKrAdditionalApprovalTaskManagerDataProvider :
        IKrTaskWithParametersManagerDataProvider<AdditionalRoleEntryStorage>
    {
    }
}
