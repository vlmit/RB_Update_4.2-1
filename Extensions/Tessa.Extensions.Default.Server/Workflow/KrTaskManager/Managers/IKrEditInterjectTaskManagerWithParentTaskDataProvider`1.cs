#nullable enable

using Tessa.Cards;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Реализация <see cref="IKrEditInterjectTaskManagerDataProvider"/>, обеспечивающая взаимодействие с внешней подсистемой и родительским заданием.
    /// </summary>
    /// <typeparam name="T"><inheritdoc cref="IExternalContextProvider{T}" path="/typeparam[@name='T']"/></typeparam>
    /// <remarks>
    /// Связь с внешней подсистемой осуществляется через <see cref="IExternalContextProvider{T}.ExternalContext"/>.
    /// </remarks>
    public interface IKrEditInterjectTaskManagerWithParentTaskDataProvider<T> :
        IKrEditInterjectTaskManagerDataProvider,
        IExternalContextProvider<T>
    {
        /// <summary>
        /// Родительское задание.
        /// </summary>
        CardTask ParentTask { get; set; }
    }
}
