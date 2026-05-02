#nullable enable

using Tessa.Platform;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Объект, предоставляющий внешний контекст.
    /// </summary>
    /// <typeparam name="T">Тип объекта внешнего контекста.</typeparam>
    public interface IExternalContextProvider<T> :
        ISealable
    {
        /// <summary>
        /// Внешний контекст.
        /// </summary>
        T ExternalContext { get; set; }
    }
}
