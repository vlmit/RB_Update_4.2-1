#nullable enable

using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Unity;

namespace Tessa.Extensions.Default.Client.Workflow.KrProcess
{
    /// <inheritdoc cref="IKrSecondaryProcessTileUIHandlerResolver"/>
    /// <inheritdoc cref="Resolver{TKey, TValue}(IUnityContainer)"/>
    public sealed class KrSecondaryProcessTileUIHandlerResolver(IUnityContainer unityContainer) :
        Resolver<KrSecondaryProcessTileHandlerDescriptor, IKrSecondaryProcessTileUIHandler>(unityContainer),
        IKrSecondaryProcessTileUIHandlerResolver
    {
    }
}
