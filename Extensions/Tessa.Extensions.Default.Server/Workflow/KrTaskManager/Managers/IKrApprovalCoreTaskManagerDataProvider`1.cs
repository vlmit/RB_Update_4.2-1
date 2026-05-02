#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Объект, обеспечивающий передачу данных между внешней подсистемой и обработчиком <see cref="IKrApprovalCoreTaskManager"/>.
    /// </summary>
    /// <typeparam name="T"><inheritdoc cref="IExternalContextProvider{T}" path="/typeparam[@name='T']"/></typeparam>
    /// <remarks>
    /// Связь с внешней подсистемой осуществляется через <see cref="IExternalContextProvider{T}.ExternalContext"/>.
    /// </remarks>
    public interface IKrApprovalCoreTaskManagerDataProvider<T> :
        IKrApprovalCoreTaskManagerDataProvider,
        IExternalContextProvider<T>
    {
    }
}
