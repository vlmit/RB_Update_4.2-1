#nullable enable

using Tessa.Forums.Mentions;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Server.Forums.Mention
{
    [Registrator]
    public class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterType<GetUserModelsMentionExtension>(new ContainerControlledLifetimeManager())
                .RegisterType<CheckPermissionsMentionExtension>(new ContainerControlledLifetimeManager())
                .RegisterType<CheckUsersInViewMentionExtension>(new ContainerControlledLifetimeManager())
                .RegisterType<AddUsersToTopicMentionExtension>(new ContainerControlledLifetimeManager())
                .RegisterType<NotifyUsersMentionExtension>(new ContainerControlledLifetimeManager())
                ;
        }

        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<IForumUserMentionExtension, GetUserModelsMentionExtension>(x => x
                    .WithOrder(ExtensionStage.Platform, 1)
                    .WithUnity(this.UnityContainer))
                .RegisterExtension<IForumUserMentionExtension, CheckPermissionsMentionExtension>(x => x
                    .WithOrder(ExtensionStage.Platform, 2)
                    .WithUnity(this.UnityContainer))
                .RegisterExtension<IForumUserMentionExtension, CheckUsersInViewMentionExtension>(x => x
                    .WithOrder(ExtensionStage.Platform, 3)
                    .WithUnity(this.UnityContainer))
                .RegisterExtension<IForumUserMentionExtension, AddUsersToTopicMentionExtension>(x => x
                    .WithOrder(ExtensionStage.Platform, 4)
                    .WithUnity(this.UnityContainer))
                .RegisterExtension<IForumUserMentionExtension, NotifyUsersMentionExtension>(x => x
                    .WithOrder(ExtensionStage.Platform, 5)
                    .WithUnity(this.UnityContainer))
                ;
        }
    }
}
