using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform.Initialization;
using Tessa.Platform.Runtime;
using Unity;

namespace Tessa.Extensions.Default.Server.EDS.SignatureArchive
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<ISignatureArchiveManager, SignatureArchiveManager>()
                .RegisterSingleton<SignaturesArchiveRequestExtension>()
                .RegisterSingleton<ISignatureArchivePermissionProvider, SignatureArchivePermissionProvider>()
                ;
        }

        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<IServerInitializationExtension, SignatureArchiveInitializationExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithUnity(this.UnityContainer)
                    .WhenApplications(ApplicationIdentifiers.WebClient))
                .RegisterExtension<ICardRequestExtension, SignaturesArchiveRequestExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 2)
                    .WithUnity(this.UnityContainer)
                    .WhenRequestTypes(DefaultRequestTypes.SignaturesArchiveRequest))
                ;
        }
    }
}
