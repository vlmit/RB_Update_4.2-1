using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Localization;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Cards
{
    public sealed class CardPermissionsStoreExtension :
        CardStoreExtension
    {
        public override async Task BeforeRequestWhenTypeResolved(ICardStoreExtensionContext context)
        {
            if (context.Session.User.IsAdministrator())
            {
                return;
            }

            if (CardExtensionHelper.CheckUserPermissions(context.CardType))
            {
                Card card = context.Request.TryGetCard();
                ListStorage<CardFile> files;
                if (card != null
                    && (files = card.TryGetFiles()) != null
                    && files.Count > 0)
                {
                    var cardTypes = await context.CardMetadata.GetCardTypesAsync(context.CancellationToken);
                    foreach (CardFile file in files)
                    {
                        if (cardTypes.TryGetValue(file.TypeID, out CardType fileType)
                            && fileType.Flags.Has(CardTypeFlags.Administrative))
                        {
                            context.ValidationResult.AddError(this,
                                "$UI_Common_Messages_CardSavingFileIsProhibited",
                                file.Name,
                                await LocalizeAsync(fileType.Caption));
                        }
                    }
                }
            }
            else
            {
                context.ValidationResult.AddError(this, "$UI_Common_Messages_CardSavingIsProhibited");
            }
        }
    }
}
