#nullable enable

using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Forums.Satellite;
using Tessa.Extensions.Default.Shared.Settings;
using Tessa.Forums;
using Tessa.Views.Workplaces;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Server.Forums
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterType<InjectForumCardMetadataExtension>(
                    new ContainerControlledLifetimeManager())
                .RegisterType<ForumProviderRequestExtension>(new ContainerControlledLifetimeManager())
                .RegisterType<IForumPermissionsProvider, KrForumPermissionsProvider>(new ContainerControlledLifetimeManager())
                .RegisterType<IForumPermissionsDependencies, ForumPermissionsDependencies>(new ContainerControlledLifetimeManager())
                .RegisterWorkplaceInitializationRule<ForumWorkplaceInitialization>(new PerResolveLifetimeManager())
                ;
        }

        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<ICardMetadataExtension, InjectForumCardMetadataExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform)
                    .WithUnity(this.UnityContainer))
                .RegisterExtension<ICardStoreExtension, ForumSatelliteStoreExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform)
                    .WithSingleton()
                    .WhenCardTypes(ForumHelper.ForumSatelliteTypeID))
                .RegisterExtension<ICardRequestExtension, ForumProviderRequestExtension>(x => x
                    .WithOrder(ExtensionStage.Platform, 1)
                    .WithUnity(this.UnityContainer)
                    .WhenRequestTypes(
                        ForumRequestTypes.AddMessage,
                        ForumRequestTypes.UpdateMessage,
                        ForumRequestTypes.GetTopic,
                        ForumRequestTypes.GetMessages,
                        ForumRequestTypes.AddTopic,
                        ForumRequestTypes.GetMessagesAfterAdding,
                        ForumRequestTypes.GetTopicsAfterAdding,
                        ForumRequestTypes.AddParticipants,
                        ForumRequestTypes.GetTopicsWithMessages,
                        ForumRequestTypes.Subscribe,
                        ForumRequestTypes.CheckPermission,
                        ForumRequestTypes.ArchiveTopic,
                        ForumRequestTypes.AddRoles,
                        ForumRequestTypes.SetForumSettings,
                        ForumRequestTypes.RemoveParticipants,
                        ForumRequestTypes.RemoveRoles,
                        ForumRequestTypes.UpdateParticipants,
                        ForumRequestTypes.UpdateRoles,
                        ForumRequestTypes.GetSatelliteID))
                ;
        }
    }
}
