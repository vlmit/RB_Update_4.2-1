#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess
{
    /// <summary>
    /// Обработчик тайла вторичного процесса.
    /// </summary>
    public interface IKrSecondaryProcessTileHandler
    {
        /// <summary>
        /// Проверяет видимость тайла.
        /// </summary>
        /// <param name="button"><inheritdoc cref="IKrProcessButton" path="/summary"/></param>
        /// <param name="card">Карточка, для которой выполняется проверка видимости, или <see langword="null"/>, если тайл глобальный.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если тайл виден, иначе <see langword="false"/>.</returns>
        ValueTask<bool> CheckVisibilityAsync(
            IKrProcessButton button,
            Card? card,
            CancellationToken cancellationToken = default);
    }
}
