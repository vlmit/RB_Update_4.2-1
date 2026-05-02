#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Действие, выполняемое над заданием.
    /// </summary>
    /// <param name="task">Обрабатываемое задание.</param>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
    /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
    public delegate ValueTask TaskActionAsync(
        CardTask task,
        CancellationToken cancellationToken = default);
}
