using Unity;

namespace Tessa.Extensions.Shared.Services
{
    [Registrator(Tag = RegistratorTag.GroupForClient)]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<IService, ServiceClient>()
                ;
        }
    }
}
