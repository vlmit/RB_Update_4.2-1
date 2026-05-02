using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess.ClientCommandInterpreter
{
    [Registrator(Tag = RegistratorTag.GroupForClient)]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterType<IClientCommandInterpreter, ClientCommandInterpreter>(new ContainerControlledLifetimeManager())
                ;
        }
    }
}
