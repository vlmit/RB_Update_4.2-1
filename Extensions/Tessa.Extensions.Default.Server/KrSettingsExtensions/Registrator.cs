#nullable enable

using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;

namespace Tessa.Extensions.Default.Server.KrSettingsExtensions
{
    [Registrator]
    public class Registrator : RegistratorBase
    {
        #region Base Overrides
        
        /// <inheritdoc/>
        public override void RegisterExtensions(IExtensionContainer extensionContainer)
        {
            extensionContainer
                .RegisterExtension<ICardStoreExtension, KrSettingsStoreExtension>(x => x
                    .WithOrder(ExtensionStage.AfterPlatform)
                    .WithSingleton()
                    .WhenCardTypes(DefaultCardTypes.KrSettingsTypeID))
                ;
        }

        #endregion
    }
}
