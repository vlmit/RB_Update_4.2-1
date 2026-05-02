using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;
using Tessa.Views;
using Unity;

namespace Tessa.Extensions.Default.Server.EDS
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            this.UnityContainer
                .RegisterSingleton<CAdESSignatureRequestExtension>()
                .RegisterSingleton<IViewInterceptor, EdsManagerInterceptor>(nameof(EdsManagerInterceptor))
                ;
        }


        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<ICardRequestExtension, CAdESSignatureRequestExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 1)
                    .WithUnity(this.UnityContainer)
                    .WhenRequestTypes(DefaultRequestTypes.CAdESSignature))
                ;
        }
    }
}
