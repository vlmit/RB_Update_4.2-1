using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Cards.Metadata;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess
{
    public sealed class KrSettingsInvalidateTypeCacheStoreExtension :
        CardStoreExtension
    {
        #region Constructors

        public KrSettingsInvalidateTypeCacheStoreExtension(
            ICardCachedMetadata cardCachedMetadata,
            IKrTypesCache cache,
            ISession session)
        {
            // конструктор для вызова на клиенте

            this.cardCachedMetadata = cardCachedMetadata;
            this.cache = cache;
            this.session = session;
        }


        public KrSettingsInvalidateTypeCacheStoreExtension(
            CardMetadataCache cardMetadataCache,
            IKrTypesCache cache,
            ISession session)
        {
            // конструктор для вызова на сервере

            this.cardMetadataCache = cardMetadataCache;
            this.cache = cache;
            this.session = session;
        }

        #endregion

        #region Fields

        private readonly ICardCachedMetadata cardCachedMetadata;

        private readonly CardMetadataCache cardMetadataCache;

        private readonly IKrTypesCache cache;

        private readonly ISession session;

        #endregion

        #region Private Methods

        private ValueTask InvalidateMetadataCacheAsync() =>
            this.session.Type switch
            {
                SessionType.Client => this.cardCachedMetadata.InvalidateAsync(),
                SessionType.Server => this.cardMetadataCache.InvalidateGlobalAsync(),
                _ => throw ArgumentOutOfRange(this.session.Type)
            };

        #endregion

        #region Base Overrides

        public override async Task AfterRequestFinally(ICardStoreExtensionContext context)
        {
            StringDictionaryStorage<CardSection> sections;
            if (!context.RequestIsSuccessful
                || !context.ValidationResult.IsSuccessful()
                || (sections = context.Request.Card.TryGetSections()) == null)
            {
                return;
            }

            if (sections.TryGetValue("KrSettingsCardTypes", out CardSection cardTypesSection))
            {
                ListStorage<CardRow> rows = cardTypesSection.TryGetRows();
                if (rows is { Count: > 0 })
                {
                    await this.InvalidateMetadataCacheAsync().ConfigureAwait(false);
                    await this.cache.InvalidateAsync().ConfigureAwait(false);
                    return;
                }
            }

            if (sections.TryGetValue("KrSettings", out CardSection settings)
                && (settings.RawFields.ContainsKey("PermissionsExtensionTypeID")
                    || settings.RawFields.ContainsKey("AllowManualInputAndAutoCreatePartners")))
            {
                //менялся тип, расширяющий безопасность
                await this.InvalidateMetadataCacheAsync().ConfigureAwait(false);
            }
        }

        #endregion
    }
}
