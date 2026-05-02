#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Контекст <see cref="IKrTaskManager{T}"/>. Связь с внешней подсистемой осуществляется через <see cref="IExternalContextProvider{T}"/>.
    /// </summary>
    /// <typeparam name="T"><inheritdoc cref="IExternalContextProvider{T}" path="/typeparam[@name='T']"/></typeparam>
    public interface IKrTaskManagerContext<T> :
        IKrTaskManagerContext,
        IExternalContextProvider<T>
    {
    }
}
