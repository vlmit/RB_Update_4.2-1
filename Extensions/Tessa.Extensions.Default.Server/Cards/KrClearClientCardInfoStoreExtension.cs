#nullable enable

using System.Threading.Tasks;
using Tessa.Cards.Extensions;
using static Tessa.Extensions.Default.Shared.Workflow.KrProcess.KrConstants;

namespace Tessa.Extensions.Default.Server.Cards
{
    /// <summary>
    /// Расширение, выполняющее очистку чувствительной информации из Card.Info, которую нельзя передавать с клиента.
    /// </summary>
    public sealed class KrClearClientCardInfoStoreExtension : CardStoreExtension
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override Task BeforeRequest(ICardStoreExtensionContext context)
        {
            if (context.Request.ServiceType != Tessa.Cards.CardServiceType.Default
                && context.Request.TryGetCard() is { } card
                && card.TryGetInfo() is { } info)
            {
                info.Remove(Keys.DocTypeID);
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
