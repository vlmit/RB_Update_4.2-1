#nullable enable

using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Platform.Storage;
using Tessa.Tags;

namespace Tessa.Extensions.Default.Server.Cards
{
    /// <summary>
    /// Расширение на получение карточки, возвращающее теги для карточек в которых разрешено их использование, а сами теги установлены.
    /// </summary>
    public sealed class TagsGetExtension : CardNewGetExtension
    {
        #region Fields

        private readonly ITagManager tagManager;

        #endregion

        #region Constructors

        public TagsGetExtension(
            ITagManager tagManager)
        {
            this.tagManager = NotNullOrThrow(tagManager);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterRequest(ICardGetExtensionContext context)
        {
            // Если запрос серверный, то не нужно загружать теги.
            if (!context.RequestIsSuccessful || context.Request.ServiceType == CardServiceType.Default)
            {
                return;
            }
            
            if (context.CardType is null ||
                !context.CardType.Flags.HasFlag(CardTypeFlags.AllowTags))
            {
                return;
            }

            // Если карточка в диалоге с режимом сохранения "Info" или "Settings", то теги недоступны.
            var storeMode = (CardTaskDialogStoreMode?) context.Request.TryGetInfo()?.TryGet<int?>(CardTaskDialogHelper.StoreMode);
            if (storeMode is CardTaskDialogStoreMode.Info or CardTaskDialogStoreMode.Settings
                || context.Response?.TryGetCard() is not {} card)
            {
                return;
            }

            var tags = await this.tagManager.GetTagsAsync(card.ID, context.ValidationResult, context.CancellationToken);
            if (!context.ValidationResult.IsSuccessful() || tags is not {Count: > 0})
            {
                return;
            }

            var tagsForCard = new TagsForCard();
            tagsForCard.Tags.AddRange(tags);
            TagsForCard.Pack(tagsForCard, card.Info);
        }

        #endregion
    }
}
