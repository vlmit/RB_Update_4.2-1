using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.OnlyOffice.Token;
using Tessa.Extensions.Default.Shared;
using Unity;

namespace Tessa.Extensions.Default.Server.OnlyOffice
{
    [Registrator]
    public sealed class Registrator :
        RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<IOnlyOfficeSettingsProvider, OnlyOfficeSettingsProvider>()
                .RegisterSingleton<IOnlyOfficeFileCacheInfoStrategy, OnlyOfficeFileCacheInfoStrategy>()
                .RegisterSingleton<IOnlyOfficeFileCache, OnlyOfficeFileCache>()
                .RegisterSingleton<IOnlyOfficeTokenManager, OnlyOfficeTokenManager>()
                .RegisterSingleton<IOnlyOfficeR7TokenManager, OnlyOfficeR7TokenManager>()
                .RegisterSingleton<IOnlyOfficeService, OnlyOfficeService>()
                .RegisterSingleton<OnlyOfficeGetExtension>()
                .RegisterSingleton<OnlyOfficeSettingsGetExtension>()
                .RegisterSingleton<OnlyOfficeJWTSecretRequestExtension>()
                .RegisterSingleton<IOnlyOfficeLockingStrategy, OnlyOfficeLockingStrategy>()
                ;
        }

        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<ICardGetExtension, OnlyOfficeGetExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 10)
                    .WithUnity(this.UnityContainer))
                .RegisterExtension<ICardGetExtension, OnlyOfficeSettingsGetExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 10)
                    .WithSingleton()
                    .WhenCardTypes(CardHelper.OnlyOfficeSettingsTypeID))
                .RegisterExtension<ICardRequestExtension, OnlyOfficeJWTSecretRequestExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithUnity(this.UnityContainer)
                    .WhenRequestTypes(DefaultRequestTypes.OnlyOfficeJWTSecretRequest))
                ;
        }
    }
}
