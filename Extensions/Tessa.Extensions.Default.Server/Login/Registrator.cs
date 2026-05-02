#nullable enable

using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform;
using Tessa.Platform.Runtime;
using Unity;

namespace Tessa.Extensions.Default.Server.Login
{
    /// <summary>
    /// Регистратор для обработчиков 2FA.
    /// </summary>
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        /// <inheritdoc/>
        public override void RegisterUnity() => NotNullOrThrow(this.UnityContainer)
            .RegisterSingleton<TwoFactorAuthEmailHandler>()
            .RegisterSingleton<TwoFactorAuthTotpHandler>()
            .RegisterSingleton<ITwoFactorAuthConfigurator, TwoFactorAuthTotpConfigurator>(TwoFactorAuthDefaultTypes.TOTP.Name)
            .RegisterSingleton<ITwoFactorAuthPermissionsManager, KrTwoFactorAuthPermissionsManager>()
            ;

        /// <inheritdoc/>
        public override void RegisterExtensions(IExtensionContainer extensionContainer) => NotNullOrThrow(extensionContainer)
            .RegisterExtension<ICardRequestExtension, TwoFactorAuthTotpCodeRequest>(x => x
                .WithOrder(ExtensionStage.AfterPlatform)
                .WithSingleton()
                .WhenRequestTypes(DefaultRequestTypes.GenerateTwoFactorAuthTotpCode))
            ;

        /// <inheritdoc/>
        public override void FinalizeRegistration() => NotNullOrThrow(this.UnityContainer)
            .TryResolve<ITwoFactorAuthContainer>()?
            .RegisterHandler<TwoFactorAuthEmailHandler>(TwoFactorAuthDefaultTypes.Email)
            .RegisterHandler<TwoFactorAuthTotpHandler>(TwoFactorAuthDefaultTypes.TOTP)
            ;
    }
}
