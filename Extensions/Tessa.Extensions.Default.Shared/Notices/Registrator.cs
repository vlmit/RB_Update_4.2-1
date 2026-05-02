#nullable enable
using Tessa.Notices;
using Unity;

namespace Tessa.Extensions.Default.Shared.Notices
{
    [Registrator(Tag = RegistratorTag.ClientOther)]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<INotificationSubscriptionPermissionManager, KrNotificationSubscriptionPermissionManager>()
                ;
        }
    }
}
