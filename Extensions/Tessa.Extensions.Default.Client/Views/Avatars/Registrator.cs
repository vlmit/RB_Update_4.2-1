using Tessa.Platform;
using Tessa.UI.Views.Extensions;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Client.Views.Avatars
{
    [Registrator]
    public class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterType<UserAvatarInRowViewExtension>(new ContainerControlledLifetimeManager())
                .RegisterType<UserAvatarInRowViewExtensionConfigurator>(new ContainerControlledLifetimeManager())
                ;
        }

        public override void FinalizeRegistration() =>
            this.UnityContainer
                .TryResolve<IWorkplaceExtensionRegistry>()
                ?
                .Register(typeof(UserAvatarInRowViewExtension))
                .RegisterConfiguratorType(
                    typeof(UserAvatarInRowViewExtension),
                    type => this.UnityContainer.Resolve<UserAvatarInRowViewExtensionConfigurator>())
                ;
    }
}
