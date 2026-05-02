#nullable enable

using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.TextRecognition.Constants;
using Unity;

namespace Tessa.Extensions.Default.Server.TextRecognition
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity() => this.UnityContainer
            .RegisterSingleton<OcrSourceCardStoreExtension>()
            .RegisterSingleton<OcrSourceFileDeleteExtension>()
            .RegisterSingleton<OcrSourceCardDeleteExtension>()
            .RegisterSingleton<OcrOperationStoreExtension>()
            .RegisterSingleton<OcrOperationPermissionsGetExtension>()
            .RegisterSingleton<OcrOperationDeleteExtension>()
            ;

        public override void RegisterExtensions(IExtensionContainer extensionContainer) => extensionContainer
            // Store
            .RegisterExtension<ICardStoreExtension, OcrSourceCardStoreExtension>(x => x
                .WithOrder(ExtensionStage.AfterPlatform)
                .WithUnity(this.UnityContainer)
                .WhenMethod(CardStoreMethod.Default)
                .WhenCardStoreFunc(context => !context.CardTypeIs(OcrCardTypes.OcrOperationTypeID)))
            .RegisterExtension<ICardStoreExtension, OcrSourceFileDeleteExtension>(x => x
                .WithOrder(ExtensionStage.AfterPlatform)
                .WithUnity(this.UnityContainer)
                .WhenMethod(CardStoreMethod.Default)
                .WhenCardStoreFunc(context => !context.CardTypeIs(OcrCardTypes.OcrOperationTypeID)))
            .RegisterExtension<ICardStoreExtension, OcrOperationStoreExtension>(x => x
                .WithOrder(ExtensionStage.AfterPlatform)
                .WithUnity(this.UnityContainer)
                .WhenCardTypes(OcrCardTypes.OcrOperationTypeID))
            // Get
            .RegisterExtension<ICardGetExtension, OcrOperationPermissionsGetExtension>(x => x
                .WithOrder(ExtensionStage.AfterPlatform)
                .WithUnity(this.UnityContainer)
                .WhenCardTypes(OcrCardTypes.OcrOperationTypeID))
            // Delete
            .RegisterExtension<ICardDeleteExtension, OcrSourceCardDeleteExtension>(x => x
                .WithOrder(ExtensionStage.AfterPlatform)
                .WithUnity(this.UnityContainer)
                .WhenMethod(CardStoreMethod.Default)
                .WhenCardDeleteFunc(context => !context.CardTypeIs(OcrCardTypes.OcrOperationTypeID)))
            .RegisterExtension<ICardDeleteExtension, OcrOperationDeleteExtension>(x => x
                .WithOrder(ExtensionStage.AfterPlatform)
                .WithUnity(this.UnityContainer)
                .WhenCardTypes(OcrCardTypes.OcrOperationTypeID))
            // Request
            .RegisterExtension<ICardRequestExtension, OcrOperationInfoRequestExtension>(x => x
                .WithOrder(ExtensionStage.AfterPlatform)
                .WithSingleton()
                .WhenRequestTypes(CardRequestTypes.GetTextRecognitionOperationInfo))
            ;
    }
}
