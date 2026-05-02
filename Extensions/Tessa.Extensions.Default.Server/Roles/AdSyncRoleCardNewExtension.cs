#nullable enable

using System.Threading.Tasks;
using Tessa.Cards.Extensions;
using Tessa.Roles;

namespace Tessa.Extensions.Default.Server.Roles
{
    /// <summary>
    /// Расширение, которое при копировании / создании карточки по шаблону
    /// очищает все поля, относящиеся к синхронизации AD/LDAP.
    /// </summary>
    public sealed class AdSyncRoleCardNewExtension : CardNewExtension
    {
        public override async Task AfterRequest(ICardNewExtensionContext context)
        {
            if (!context.RequestIsSuccessful
                || !context.ValidationResult.IsSuccessful()
                || context.Response?.TryGetCard()?.TryGetSections()?.TryGet(RoleStrings.Roles) is not { } rolesSection)
            {
                return;
            }

            var fields = rolesSection.RawFields;

            fields["AdSyncWhenChanged"] = null;
            fields["AdSyncDistinguishedName"] = null;
            fields["AdSyncHash"] = null;
            fields["AdSyncID"] = null;
        }
    }
}
