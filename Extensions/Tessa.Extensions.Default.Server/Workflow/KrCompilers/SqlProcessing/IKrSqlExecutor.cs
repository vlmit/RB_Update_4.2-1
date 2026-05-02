#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers.SqlProcessing
{
    /// <summary>
    /// Объект, выполняющий SQL-запросы в подсистеме маршрутов.
    /// </summary>
    public interface IKrSqlExecutor
    {
        /// <summary>
        /// Вычисляет условное SQL-выражение.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrSqlExecutorContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если SQL-выражение выполняется, иначе - <see langword="false"/>.</returns>
        Task<bool> ExecuteConditionAsync(IKrSqlExecutorContext context);

        /// <summary>
        /// Вычисляет список SQL-исполнителей.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrSqlExecutorContext" path="/summary"/></param>
        /// <returns>Список SQL-исполнителей.</returns>
        Task<IList<Performer>> ExecutePerformersAsync(IKrSqlExecutorContext context);
    }
}
