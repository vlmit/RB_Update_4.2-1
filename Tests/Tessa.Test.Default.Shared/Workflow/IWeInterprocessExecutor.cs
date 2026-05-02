#nullable enable
using System.Threading.Tasks;
using Tessa.Workflow;

namespace Tessa.Test.Default.Shared.Workflow
{
    /// <summary>
    /// Обработчик действия, выполняемого в рамках выполнения процесса.
    /// </summary>
    public interface IWeInterprocessExecutor
    {
        /// <summary>
        /// Выполняет заданное действие в рамках выполнения процесса.
        /// </summary>
        /// <param name="workflowEngineContext">Контекст обработки процесса.</param>
        /// <param name="additionalData">Дополнительные данные, передаваемые из процесса.</param>
        /// <returns>Асинхронная задача.</returns>
        ValueTask ExecuteAsync(IWorkflowEngineContext workflowEngineContext, params object[] additionalData);
    }
}
