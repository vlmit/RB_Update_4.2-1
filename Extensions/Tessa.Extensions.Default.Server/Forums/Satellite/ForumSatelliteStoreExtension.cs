#nullable enable
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;

namespace Tessa.Extensions.Default.Server.Forums.Satellite
{
    public sealed class ForumSatelliteStoreExtension : CardStoreExtension
    {
        public override Task BeforeRequest(ICardStoreExtensionContext context)
        {
            if (context.Request.Card.StoreMode == CardStoreMode.Update && context.Request.TryGetCard()?.TryGetFiles()?.Count > 0)
            {
                context.Request.DoesNotAffectVersion = true;
            }

            return Task.CompletedTask;
        }
    }
}
