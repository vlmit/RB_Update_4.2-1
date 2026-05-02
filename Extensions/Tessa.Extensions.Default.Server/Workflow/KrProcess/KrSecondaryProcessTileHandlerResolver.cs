#nullable enable

using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess
{
    /// <inheritdoc cref="IKrSecondaryProcessTileHandlerResolver"/>
    /// <inheritdoc cref="Resolver{TKey, TValue}(IUnityContainer)"/>
    public sealed class KrSecondaryProcessTileHandlerResolver(IUnityContainer unityContainer) :
        Resolver<KrSecondaryProcessTileHandlerDescriptor, IKrSecondaryProcessTileHandler>(unityContainer),
        IKrSecondaryProcessTileHandlerResolver
    {
    }
}
