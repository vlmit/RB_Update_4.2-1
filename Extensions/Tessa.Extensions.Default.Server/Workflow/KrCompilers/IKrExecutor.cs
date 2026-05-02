#nullable enable

using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <summary>
    /// Объект, выполняющий построение маршрута.
    /// </summary>
    public interface IKrExecutor
    {
        /// <summary>
        /// Выполняет построение маршрута для объектов, указанных в контексте.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrExecutionContext" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        Task ExecuteAsync(IKrExecutionContext context);
    }
}
