#nullable enable

using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess
{
    /// <summary>
    /// Объект для получения обработчика тайла вторичного процесса по идентификатору обработчика.
    /// </summary>
    public interface IKrSecondaryProcessTileHandlerResolver :
        IResolver<KrSecondaryProcessTileHandlerDescriptor, IKrSecondaryProcessTileHandler>
    {
    }
}
