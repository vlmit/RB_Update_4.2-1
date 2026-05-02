using Tessa.Extensions.Default.Shared.Workflow.KrCompilers;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <summary>
    /// Описывает объект предоставляющий информацию определяющую доступность для выполнения.
    /// </summary>
    public interface IExecutionSources :
        IKrHasSource
    {
        /// <summary>
        /// Возвращает текст SQL запроса с условием, определяющим доступность выполнения.
        /// </summary>
        string ExecutionSqlCondition { get; }

        /// <summary>
        /// Возвращает C# код, определяющий доступность выполнения.
        /// </summary>
        string ExecutionSourceCondition { get; }
    }
}
