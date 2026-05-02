using Tessa.Extensions.Default.Shared.Cards;
using Tessa.Platform;
using Tessa.UI.Cards;
using Unity;

namespace Tessa.Extensions.Default.Client.Extensions
{
    /// <summary>
    /// Registrator is called later than <see cref="Tessa.Extensions.Default.Shared.Cards.TypesRegistrator"/>.
    /// </summary>
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        /// <inheritdoc/>
        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterSingleton<InitializeFilesViewExtensionType>()
                .RegisterSingleton<MakeViewTaskHistoryExtensionType>()
                .RegisterSingleton<OpenCardInViewExtensionType>();

        /// <inheritdoc/>
        public override void FinalizeRegistration() =>
            this.UnityContainer.TryResolve<ITypeExtensionTypeResolver>()?
                .Register<InitializeFilesViewExtensionType>(DefaultCardTypeExtensionTypes.InitializeFilesView)
                .Register<MakeViewTaskHistoryExtensionType>(DefaultCardTypeExtensionTypes.MakeViewTaskHistory)
                .Register<OpenCardInViewExtensionType>(DefaultCardTypeExtensionTypes.OpenCardInView);
    }
}
