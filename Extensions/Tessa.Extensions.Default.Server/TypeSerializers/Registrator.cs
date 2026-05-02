#nullable enable
using Tessa.Cards.TypeSerializers;
using Tessa.Extensions.Default.Server.TypeSerializers.Extensions;
using Tessa.Extensions.Default.Shared.Cards;
using Tessa.Platform;
using Unity;

namespace Tessa.Extensions.Default.Server.TypeSerializers
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterSingleton<InitializeFilesViewExtensionSerializer>()
                .RegisterSingleton<MakeViewTaskHistoryExtensionSerializer>()
                .RegisterSingleton<OpenCardInViewExtensionSerializer>();

        public override void FinalizeRegistration() =>
            this.UnityContainer.TryResolve<ITypeExtensionSerializerResolver>()?
                .Register<InitializeFilesViewExtensionSerializer>(DefaultCardTypeExtensionTypes.InitializeFilesView)
                .Register<MakeViewTaskHistoryExtensionSerializer>(DefaultCardTypeExtensionTypes.MakeViewTaskHistory)
                .Register<OpenCardInViewExtensionSerializer>(DefaultCardTypeExtensionTypes.OpenCardInView);
    }
}
