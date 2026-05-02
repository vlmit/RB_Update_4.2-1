using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Cards.Metadata;
using Tessa.Platform.Runtime;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess
{
    public sealed class KrSettingsInvalidateTypeCacheDeleteExtension :
        CardDeleteExtension
    {
        #region Constructors

        public KrSettingsInvalidateTypeCacheDeleteExtension(
            ICardCachedMetadata cardCachedMetadata,
            IKrTypesCache cache,
            ISession session)
        {
            // конструктор для вызова на клиенте

            this.cardCachedMetadata = cardCachedMetadata;
            this.cache = cache;
            this.session = session;
        }


        public KrSettingsInvalidateTypeCacheDeleteExtension(
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

        public override async Task AfterRequestFinally(ICardDeleteExtensionContext context)
        {
            if (!context.RequestIsSuccessful || !context.ValidationResult.IsSuccessful())
            {
                return;
            }

            await this.InvalidateMetadataCacheAsync().ConfigureAwait(false);
            await this.cache.InvalidateAsync().ConfigureAwait(false);
        }

        #endregion
    }
}
