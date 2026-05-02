#nullable enable
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Cards
{
    public sealed class CardPermissionsGetExtension :
        CardGetExtension
    {
        #region Base Overrides

        public override Task BeforeRequestWhenTypeResolved(ICardGetExtensionContext context)
        {
            if (context.ValidationResult.IsSuccessful()
                && context.Request.ServiceType == CardServiceType.Client
                && !context.Session.User.IsAdministrator()
                && context.CardType?.InstanceType == CardInstanceType.Card
                && !CardExtensionHelper.CheckUserPermissions(context.CardType)
                && context.CardType?.Flags.HasNot(CardTypeFlags.AllowReadOnlyForAllUsers) == true)
            {
                ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .Error(ValidationKeys.UserIsNotAdmin)
                    .End();
            }

            return Task.CompletedTask;
        }

        public override Task AfterRequest(ICardGetExtensionContext context)
        {
            if (!context.RequestIsSuccessful
                || context.Response!.TryGetCard() is not { } card)
            {
                return Task.CompletedTask;
            }

            bool isAdministrator = context.Session.User.IsAdministrator();
            if (isAdministrator || CardExtensionHelper.CheckUserPermissions(context.CardType))
            {
                return CardExtensionHelper.GrantAllPermissionsExceptAdministrativeFilesAsync(
                    card,
                    isAdministrator,
                    context.CardMetadata,
                    context.CancellationToken).AsTask();
            }

            CardHelper.ProhibitAllPermissions(card, removeOtherPermissions: true);
            return Task.CompletedTask;
        }

        #endregion
    }
}
