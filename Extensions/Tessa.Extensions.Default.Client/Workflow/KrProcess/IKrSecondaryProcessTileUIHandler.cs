#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.UI;

namespace Tessa.Extensions.Default.Client.Workflow.KrProcess
{
    /// <summary>
    /// Обработчик тайла вторичного процесса на клиенте.
    /// </summary>
    public interface IKrSecondaryProcessTileUIHandler
    {
        /// <summary>
        /// Выполняет обработку тайла.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IUIContext" path="/summary"/></param>
        /// <param name="tileInfo"><inheritdoc cref="KrTileInfo" path="/summary"/></param>
        /// <param name="additionalInfo">Дополнительная информация, отправляемая при стандартной обработке тайла.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если по завершению обработки требуется вызов стандартной обработки тайла, иначе <see langword="false"/>.</returns>
        ValueTask<bool> HandleTileAsync(
            IUIContext context,
            KrTileInfo tileInfo,
            IDictionary<string, object?> additionalInfo,
            CancellationToken cancellationToken = default);
    }
}
