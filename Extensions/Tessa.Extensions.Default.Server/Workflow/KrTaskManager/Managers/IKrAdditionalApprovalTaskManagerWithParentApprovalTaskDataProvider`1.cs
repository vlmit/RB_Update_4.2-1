#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Реализация <see cref="IKrAdditionalApprovalTaskManagerDataProvider"/>, обеспечивающая взаимодействие с подсистемой <typeparamref name="T"/> и родительским заданием.
    /// </summary>
    /// <typeparam name="T"><inheritdoc cref="IExternalContextProvider{T}" path="/typeparam[@name='T']"/></typeparam>
    /// <remarks>
    /// Связь с внешней подсистемой осуществляется через <see cref="IExternalContextProvider{T}.ExternalContext"/>.
    /// </remarks>
    public interface IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider<T> :
        IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider,
        IExternalContextProvider<T>
    {
    }
}
