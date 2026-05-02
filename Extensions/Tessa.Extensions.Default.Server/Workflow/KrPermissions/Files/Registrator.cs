using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions.Files
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterType<IKrFileOwnershipChecker, KrFileOwnershipChecker>(new ContainerControlledLifetimeManager())
                ;
        }
    }
}
