#nullable enable

using Tessa.Stamping;
using Unity;

namespace Tessa.Extensions.Default.Server.Stamping
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity() => this.UnityContainer
            .RegisterSingleton<IStampingProcessor, StampingProcessor>()
            ;
    }
}
