#nullable enable
using Tessa.Cards.Repair;
using Tessa.Extensions.Default.Shared.Cards;
using Tessa.Platform;
using Unity;

namespace Tessa.Extensions.Default.Server.Cards.Repair
{
    /// <summary>
    /// Registrator is called later than <see cref="Tessa.Extensions.Default.Shared.Cards.TypesRegistrator"/>.
    /// </summary>
    [Registrator]
    public class Registrator : RegistratorBase
    {
        /// <inheritdoc/>
        public override void RegisterUnity() =>
            this.UnityContainer
                .RegisterSingleton<OpenCardInViewExtensionTypeRepairer>();

        /// <inheritdoc/>
        public override void FinalizeRegistration() =>
            this.UnityContainer.TryResolve<IExtensionTypeRepairerResolver>()?
                .Register<ExtensionTypeRepairerDefault>(DefaultCardTypeExtensionTypes.InitializeFilesView)
                .Register<ExtensionTypeRepairerDefault>(DefaultCardTypeExtensionTypes.MakeViewTaskHistory)
                .Register<OpenCardInViewExtensionTypeRepairer>(DefaultCardTypeExtensionTypes.OpenCardInView);
    }
}
