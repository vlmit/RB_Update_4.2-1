using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Server.Web.DeskiMobile
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        #region Base Overrides

        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterType<IDeskiMobileTokenManager, DeskiMobileTokenManager>(new ContainerControlledLifetimeManager())
                .RegisterType<IDeskiMobileManager, DeskiMobileManager>(new ContainerControlledLifetimeManager());
        }

        public override void InitializeRegistration()
        {
            DeskiMobileValidationKeys.Register();
        }

        #endregion
    }
}
