#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Действие, выполняемое над делегированным заданием.
    /// </summary>
    /// <typeparam name="T">Тип объекта, содержащего информацию о исполнителе.</typeparam>
    /// <param name="originalTask">Делегируемое задание.</param>
    /// <param name="delegatedTask">Делегированное задание.</param>
    /// <param name="performer">Исполнитель <paramref name="delegatedTask"/>.</param>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
    /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
    public delegate ValueTask DelegateTaskActionAsync<in T>(
        CardTask originalTask,
        CardTask delegatedTask,
        T performer,
        CancellationToken cancellationToken = default);
}
