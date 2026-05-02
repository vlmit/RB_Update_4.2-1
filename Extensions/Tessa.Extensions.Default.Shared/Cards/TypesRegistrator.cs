#nullable enable

using Tessa.Cards;

namespace Tessa.Extensions.Default.Shared.Cards
{
    /// <summary>
    /// Default types registrations should be called prior to other type-dependent registrators, such as for Repair, for console and for desktop-client.
    /// </summary>
    [Registrator(Tag = RegistratorTag.GroupForServer | RegistratorTag.GroupForClient, Order = -1)]
    public sealed class TypesRegistrator : RegistratorBase
    {
        public override void FinalizeRegistration() =>
            DefaultCardTypeExtensionTypes.Register(CardTypeExtensionTypeRegistry.Instance.Register);
    }
}
