#nullable enable
using Tessa.Forums.Notifications;
using Unity;

namespace Tessa.Extensions.Default.Server.Forums.Notifications
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<ITopicNotificationService, TopicNotificationService>();
        }
    }
}
