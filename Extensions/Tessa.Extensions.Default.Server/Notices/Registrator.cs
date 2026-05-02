#nullable enable
using Tessa.Notices;
using Unity;

namespace Tessa.Extensions.Default.Server.Notices
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<INotificationDefaultLanguagePicker, KrNotificationDefaultLanguagePicker>()
                .RegisterSingleton<INotificationSubscriptionPermissionManager, KrNotificationSubscriptionPermissionManagerServer>()
                ;
        }
    }
}
