using System.Threading;
using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Console.PrintDiscoveryInfo
{
    public interface IComponentsProvider
    {
        Task<DiscoveryComponentsInfo> GetComponentsAsync(bool loadPlugins, CancellationToken cancellationToken = default);
    }
}
