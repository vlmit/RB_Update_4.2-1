using System.Threading.Tasks;
using Tessa.Cards.Extensions;
using Tessa.Tags;

namespace Tessa.Extensions.Default.Client.Cards
{
    public sealed class RemoveTagsCardStoreExtension
        : CardStoreExtension
    {
        public override Task BeforeRequest(ICardStoreExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful() ||
                context.Request.TryGetCard()?.TryGetInfo() is not { } info)
            {
                return Task.CompletedTask;
            }

            TagsForCard.Remove(info);

            return Task.CompletedTask;
        }
    }
}
