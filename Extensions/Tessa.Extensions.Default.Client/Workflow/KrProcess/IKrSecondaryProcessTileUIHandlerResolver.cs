#nullable enable

using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;

namespace Tessa.Extensions.Default.Client.Workflow.KrProcess
{
    /// <summary>
    /// Объект, предоставляющий обработчик тайла вторичного процесса на клиенте по идентификатору обработчика.
    /// </summary>
    public interface IKrSecondaryProcessTileUIHandlerResolver :
        IResolver<KrSecondaryProcessTileHandlerDescriptor, IKrSecondaryProcessTileUIHandler>
    {
    }
}
