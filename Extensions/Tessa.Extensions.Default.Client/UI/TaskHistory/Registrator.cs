#nullable enable

using Tessa.UI.Cards;
using Unity;

namespace Tessa.Extensions.Default.Client.UI.TaskHistory
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterSingleton<MakeViewTaskHistoryUIExtension>();

        public override void RegisterExtensions(IExtensionContainer extensionContainer) =>
            extensionContainer
                .RegisterExtension<ICardUIExtension, MakeViewTaskHistoryUIExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform, 34)
                    .WithUnity(this.UnityContainer));
    }
}
