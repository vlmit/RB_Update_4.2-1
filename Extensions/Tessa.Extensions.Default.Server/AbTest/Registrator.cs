using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.AbTest;
using Tessa.Views;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Server.AbTest
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<AbCarTableRequestExtension>()
                .RegisterSingleton<AbTestDataRequestExtension>()
                .RegisterType<AbTestWorkflowStoreExtension>(new PerResolveLifetimeManager())
                .RegisterSingleton<IExtraViewListProvider, AbTransientViewProvider>(nameof(AbTransientViewProvider))
                .RegisterSingleton<IViewInterceptor, AbChangeConnectionInterceptor>(nameof(AbChangeConnectionInterceptor))
                .RegisterSingleton<IViewInterceptor, AbChangeViewInterceptor>(nameof(AbChangeViewInterceptor))
                .RegisterSingleton<IViewInterceptor, AbViewFilesInterceptor>(nameof(AbViewFilesInterceptor))
                ;
        }

        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<ICardRequestExtension, AbCarTableRequestExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithUnity(this.UnityContainer)
                    .WhenRequestTypes(AbRequestTypes.TestCarTableRequest))
                .RegisterExtension<ICardRequestExtension, AbXmlFromExternalSystemRequestExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithSingleton()
                    .WhenRequestTypes(AbRequestTypes.GetExternalSystemData))
                .RegisterExtension<ICardRequestExtension, AbTestDataRequestExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithUnity(this.UnityContainer)
                    .WhenRequestTypes(AbRequestTypes.TestData))
                .RegisterExtension<ICardStoreExtension, AbTestWorkflowStoreExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform)
                    .WithUnity(this.UnityContainer)
                    .WhenCardTypes(AbCardTypes.AbCarTypeID))
                ;
        }
    }
}
