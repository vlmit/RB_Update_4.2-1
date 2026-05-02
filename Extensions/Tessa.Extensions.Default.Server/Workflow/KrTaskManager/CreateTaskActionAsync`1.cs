#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Действие, выполняемое над созданным заданием.
    /// </summary>
    /// <typeparam name="T">Тип объекта, содержащего информацию о исполнителе.</typeparam>
    /// <param name="task">Созданное задание.</param>
    /// <param name="performer">Исполнитель <paramref name="performer"/>.</param>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
    /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
    public delegate ValueTask CreateTaskActionAsync<in T>(
        CardTask task,
        T? performer,
        CancellationToken cancellationToken = default);
}
