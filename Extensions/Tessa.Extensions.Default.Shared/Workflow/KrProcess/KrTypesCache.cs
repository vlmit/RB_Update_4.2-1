using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Unity;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess
{
    public sealed class KrTypesCache :
        IKrTypesCache,
        IDisposable
    {
        #region Constructors

        public KrTypesCache(
            IDbScope dbScope,
            ICardMetadata cardMetadata,
            ICardCache cardCache,
            [OptionalDependency] IUnityDisposableContainer container = null)
        {
            this.dbScope = dbScope ?? throw new ArgumentNullException(nameof(dbScope));
            this.cardMetadata = cardMetadata ?? throw new ArgumentNullException(nameof(cardMetadata));
            this.cardCache = cardCache ?? throw new ArgumentNullException(nameof(cardCache));

            container?.Register(this);
        }

        public KrTypesCache(
            ICardRepository cardRepository,
            ICardMetadata cardMetadata,
            ICardCache cardCache,
            [OptionalDependency] IUnityDisposableContainer container = null)
        {
            this.cardRepository = cardRepository ?? throw new ArgumentNullException(nameof(cardRepository));
            this.cardMetadata = cardMetadata ?? throw new ArgumentNullException(nameof(cardMetadata));
            this.cardCache = cardCache ?? throw new ArgumentNullException(nameof(cardCache));

            container?.Register(this);
        }

        #endregion

        #region Fields

        /// <summary>
        /// Заполнен на сервере.
        /// </summary>
        private readonly IDbScope dbScope;

        /// <summary>
        /// Заполнен на клиенте.
        /// </summary>
        private readonly ICardRepository cardRepository;

        /// <summary>
        /// Заполнен на клиенте и на сервере.
        /// </summary>
        private readonly ICardMetadata cardMetadata;

        /// <summary>
        /// Заполнен на клиенте и на сервере.
        /// </summary>
        private readonly ICardCache cardCache;

        /// <summary>
        /// Все операции, которые могут изменить кэш, должны выполняться внутри lock'а.
        /// </summary>
        private readonly AsyncLock asyncLock = new();

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly IStorageValueFactory<int, KrDocType> docTypeFactory =
            new DictionaryStorageValueFactory<int, KrDocType>(
                (key, storage) => new KrDocType(storage));

        private static readonly IStorageValueFactory<int, KrCardType> cardTypeFactory =
            new DictionaryStorageValueFactory<int, KrCardType>(
                (key, storage) => new KrCardType(storage));

        private const string KrTypesKey = "KrTypes";

        #endregion

        #region Private Classes

        private sealed record KrTypes(IReadOnlyList<IKrType> Types, IReadOnlyList<KrCardType> CardTypes, IReadOnlyList<KrDocType> DocTypes, IReadOnlyDictionary<Guid, KrComponents> TypeComponents);

        #endregion

        #region Private Methods

        private async Task<ListStorage<KrDocType>> GetDocTypesListStorageOnServerAsync(CancellationToken cancellationToken = default)
        {
            await using (this.dbScope.Create())
            {
                var db = this.dbScope.Db;
                var builder = this.dbScope.BuilderFactory
                    .Select().C(null,
                        "ID", "Title", "CardTypeID", "CardTypeName", "UseApproving", "UseRegistration", "UseResolutions",
                        "DisableChildResolutionDateCheck", "DocNumberRegularSequence", "DocNumberRegularFormat",
                        "AllowManualRegularDocNumberAssignment", "DocNumberRegistrationSequence", "DocNumberRegistrationFormat",
                        "AllowManualRegistrationDocNumberAssignment", "DocNumberRegistrationAutoAssignmentID",
                        "DocNumberRegularAutoAssignmentID", "ReleaseRegistrationNumberOnFinalDeletion",
                        "ReleaseRegularNumberOnFinalDeletion", "HideCreationButton", "HideRouteTab", "UseForum", "UseDefaultDiscussionTab",
                        "UseRoutesInWorkflowEngine")
                    .From("KrDocType").NoLock();

                await using DbDataReader reader = await db
                    .SetCommand(builder.Build())
                    .LogCommand()
                    .ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                var items = new List<object>();

                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    Guid id = reader.GetGuid(0);
                    string name = reader.GetString(1);

                    var item = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        { "ID", id },
                        { "Name", name },
                        { "Caption", name },
                        { "CardTypeID", reader.GetGuid(2) },
                        { "CardTypeName", reader.GetString(3) },
                        { "UseApproving", reader.GetBoolean(4) },
                        { "UseRegistration", reader.GetBoolean(5) },
                        { "UseResolutions", reader.GetBoolean(6) },
                        { "DisableChildResolutionDateCheck", reader.GetBoolean(7) },
                        { "DocNumberRegularSequence", reader.GetValue<string>(8) ?? string.Empty },
                        { "DocNumberRegularFormat", reader.GetValue<string>(9) ?? string.Empty },
                        { "AllowManualRegularDocNumberAssignment", reader.GetBoolean(10) },
                        { "DocNumberRegistrationSequence", reader.GetValue<string>(11) ?? string.Empty },
                        { "DocNumberRegistrationFormat", reader.GetValue<string>(12) ?? string.Empty },
                        { "AllowManualRegistrationDocNumberAssignment", reader.GetBoolean(13) },
                        { "DocNumberRegistrationAutoAssignmentID", reader.GetInt32(14) },
                        { "DocNumberRegularAutoAssignmentID", reader.GetInt32(15) },
                        { "ReleaseRegistrationNumberOnFinalDeletion", reader.GetBoolean(16) },
                        { "ReleaseRegularNumberOnFinalDeletion", reader.GetBoolean(17) },
                        { "HideCreationButton", reader.GetBoolean(18) },
                        { "HideRouteTab", reader.GetBoolean(19) },
                        { "UseForum", reader.GetBoolean(20) },
                        { "UseDefaultDiscussionTab", reader.GetBoolean(21) },
                        { "UseRoutesInWorkflowEngine", reader.GetBoolean(22) },
                    };

                    items.Add(item);
                }

                var result = new ListStorage<KrDocType>(items, docTypeFactory);
                result.EnsureCacheResolved();

                return result;
            }
        }

        private async Task<ListStorage<KrDocType>> GetDocTypesListStorageOnClientAsync(CancellationToken cancellationToken = default)
        {
            //спрашиваем у кэша, передавая в лямбде запрос, который на сервере перехватим
            //расширениями, в респонс забьем данные и здесь забьем в кэш
            var request = new CardRequest { RequestType = DefaultRequestTypes.KrGetDocTypes };
            CardResponse response = await this.cardRepository.RequestAsync(request, cancellationToken).ConfigureAwait(false);

            var items = new List<object>();
            if (response.ValidationResult.IsSuccessful())
            {
                foreach (KeyValuePair<string, object> pair in response.Info)
                {
                    if (pair.Key.StartsWith("KrDocTypes_", StringComparison.Ordinal))
                    {
                        items.Add(pair.Value);
                    }
                }
            }

            var result = new ListStorage<KrDocType>(items, docTypeFactory);
            result.EnsureCacheResolved();

            return result;
        }

        private async Task<ListStorage<KrCardType>> GetCardTypesListStorageFromCacheAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CardRow> krCardTypes = await GetKrCardTypesAsync(this.cardCache, cancellationToken).ConfigureAwait(false);

            var items = new List<object>();
            foreach (CardRow row in krCardTypes)
            {
                Guid cardTypeID = row.Get<Guid>("CardTypeID");

                if (!(await this.cardMetadata.GetCardTypesAsync(cancellationToken).ConfigureAwait(false))
                    .TryGetValue(cardTypeID, out CardType cardType))
                {
                    continue;
                }

                var item = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    { "ID", cardTypeID },
                    { "Name", cardType.Name },
                    { "Caption", cardType.Caption },
                    { "UseDocTypes", row["UseDocTypes"] },
                    { "UseApproving", row["UseApproving"] },
                    { "UseRegistration", row["UseRegistration"] },
                    { "UseResolutions", row["UseResolutions"] },
                    { "DisableChildResolutionDateCheck", row["DisableChildResolutionDateCheck"] },
                    { "DocNumberRegularSequence", row["DocNumberRegularSequence"] },
                    { "DocNumberRegularFormat", row["DocNumberRegularFormat"] },
                    { "AllowManualRegularDocNumberAssignment", row["AllowManualRegularDocNumberAssignment"] },
                    { "DocNumberRegistrationSequence", row["DocNumberRegistrationSequence"] },
                    { "DocNumberRegistrationFormat", row["DocNumberRegistrationFormat"] },
                    { "AllowManualRegistrationDocNumberAssignment", row["AllowManualRegistrationDocNumberAssignment"] },
                    { "DocNumberRegistrationAutoAssignmentID", row["DocNumberRegistrationAutoAssignmentID"] },
                    { "DocNumberRegularAutoAssignmentID", row["DocNumberRegularAutoAssignmentID"] },
                    { "ReleaseRegistrationNumberOnFinalDeletion", row["ReleaseRegistrationNumberOnFinalDeletion"] },
                    { "ReleaseRegularNumberOnFinalDeletion", row["ReleaseRegularNumberOnFinalDeletion"] },
                    { "HideCreationButton", row.TryGet<object>("HideCreationButton") ?? BooleanBoxes.False },
                    { "HideRouteTab", row.TryGet<object>("HideRouteTab") ?? BooleanBoxes.False },
                    { "UseForum", row.TryGet<object>("UseForum") ?? BooleanBoxes.False },
                    { "UseDefaultDiscussionTab", row.TryGet<object>("UseDefaultDiscussionTab") ?? BooleanBoxes.False },
                    { "UseRoutesInWorkflowEngine", row.TryGet<object>("UseRoutesInWorkflowEngine") ?? BooleanBoxes.False },
                };

                items.Add(item);
            }

            var result = new ListStorage<KrCardType>(items, cardTypeFactory);
            result.EnsureCacheResolved();

            return result;
        }

        private async ValueTask<ListStorage<KrDocType>> GetDocTypesWithNoLockAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CardRow> cardTypes = await GetKrCardTypesAsync(this.cardCache, cancellationToken).ConfigureAwait(false);
            CardRow[] krCardTypesUsesDocTypes = cardTypes is { Count: > 0 }
                ? cardTypes.Where(x => x.Get<bool>("UseDocTypes")).ToArray()
                : Array.Empty<CardRow>();

            ListStorage<KrDocType> docTypes = this.dbScope != null
                ? await this.GetDocTypesListStorageOnServerAsync(cancellationToken).ConfigureAwait(false)
                : await this.GetDocTypesListStorageOnClientAsync(cancellationToken).ConfigureAwait(false);

            for (int i = docTypes.Count - 1; i >= 0; i--)
            {
                if (krCardTypesUsesDocTypes.All(x => docTypes[i].CardTypeID != x.Get<Guid>("CardTypeID")))
                {
                    docTypes.RemoveAt(i);
                }
            }

            docTypes.EnsureCacheResolved();
            return docTypes;
        }

        private async ValueTask<KrTypes> RebuildAllTypesAsync(CancellationToken cancellationToken)
        {
            var types = new List<IKrType>();
            var cardTypes = new List<KrCardType>();
            var docTypes = new List<KrDocType>();
            var typeComponents = new Dictionary<Guid, KrComponents>();

            ListStorage<KrDocType> allDocTypes = null;
            foreach (KrCardType cardType in await this.GetCardTypesListStorageFromCacheAsync(cancellationToken).ConfigureAwait(false))
            {
                cardTypes.Add(cardType);
                typeComponents.Add(cardType.ID, GetKrComponents(cardType));

                if (cardType.UseDocTypes)
                {
                    allDocTypes ??= await this.GetDocTypesWithNoLockAsync(cancellationToken).ConfigureAwait(false);

                    foreach (KrDocType docType in allDocTypes)
                    {
                        if (docType.CardTypeID == cardType.ID)
                        {
                            typeComponents.Add(docType.ID, GetKrComponents(docType));
                            types.Add(docType);
                            docTypes.Add(docType);
                        }
                    }
                }
                else
                {
                    types.Add(cardType);
                }
            }

            var krTypes = new KrTypes(types.AsReadOnly(), cardTypes.AsReadOnly(), docTypes.AsReadOnly(), typeComponents.AsReadOnly());
            return await this.cardCache.Settings.GetAsync(KrTypesKey, _ => krTypes, cancellationToken).ConfigureAwait(false);
        }

        private static KrComponents GetKrComponents(KrDocType docType) =>
            docType is null
                ? KrComponents.None
                : KrComponents.Base
                | KrComponents.DocTypes
                | GetKrTypeComponents(docType);

        private static KrComponents GetKrComponents(KrCardType cardType)
        {
            if (cardType is null)
            {
                return KrComponents.None;
            }

            if (cardType.UseDocTypes)
            {
                // Если карточка использует типы документов, то для неё не работают все остальные настройки
                return KrComponents.Base | KrComponents.DocTypes;
            }

            return KrComponents.Base | GetKrTypeComponents(cardType);
        }

        private static KrComponents GetKrTypeComponents(IKrType krType)
        {
            var result = KrComponents.None;

            if (krType.UseApproving)
            {
                result |= KrComponents.Routes;
            }

            if (krType.UseRegistration)
            {
                result |= KrComponents.Registration;
            }

            if (krType.UseResolutions)
            {
                result |= KrComponents.Resolutions;
            }

            if (krType.UseForum)
            {
                result |= KrComponents.UseForum;
            }

            return result;
        }

        #endregion

        #region IKrTypesCache Members

        /// <inheritdoc />
        public async ValueTask<IReadOnlyList<IKrType>> GetTypesAsync(CancellationToken cancellationToken = default)
        {
            var types = await this.cardCache.Settings.TryGetAlreadyCachedAsync<KrTypes>(KrTypesKey, cancellationToken).ConfigureAwait(false);
            if (types != null)
            {
                return types.Types;
            }

            using (await this.asyncLock.EnterAsync(cancellationToken).ConfigureAwait(false))
            {
                types = await this.cardCache.Settings.TryGetAlreadyCachedAsync<KrTypes>(KrTypesKey, cancellationToken).ConfigureAwait(false);
                if (types != null)
                {
                    return types.Types;
                }

                types = await this.RebuildAllTypesAsync(cancellationToken).ConfigureAwait(false);
                return types.Types;
            }
        }

        /// <inheritdoc />
        public async ValueTask<IReadOnlyList<KrCardType>> GetCardTypesAsync(CancellationToken cancellationToken = default)
        {
            var types = await this.cardCache.Settings.TryGetAlreadyCachedAsync<KrTypes>(KrTypesKey, cancellationToken).ConfigureAwait(false);
            if (types != null)
            {
                return types.CardTypes;
            }

            using (await this.asyncLock.EnterAsync(cancellationToken).ConfigureAwait(false))
            {
                types = await this.cardCache.Settings.TryGetAlreadyCachedAsync<KrTypes>(KrTypesKey, cancellationToken).ConfigureAwait(false);
                if (types != null)
                {
                    return types.CardTypes;
                }

                types = await this.RebuildAllTypesAsync(cancellationToken).ConfigureAwait(false);
                return types.CardTypes;
            }
        }

        /// <inheritdoc />
        public async ValueTask<IReadOnlyList<KrDocType>> GetDocTypesAsync(CancellationToken cancellationToken = default)
        {
            var types = await this.cardCache.Settings.TryGetAlreadyCachedAsync<KrTypes>(KrTypesKey, cancellationToken).ConfigureAwait(false);
            if (types != null)
            {
                return types.DocTypes;
            }

            using (await this.asyncLock.EnterAsync(cancellationToken).ConfigureAwait(false))
            {
                types = await this.cardCache.Settings.TryGetAlreadyCachedAsync<KrTypes>(KrTypesKey, cancellationToken).ConfigureAwait(false);
                if (types != null)
                {
                    return types.DocTypes;
                }

                types = await this.RebuildAllTypesAsync(cancellationToken).ConfigureAwait(false);
                return types.DocTypes;
            }
        }

        /// <inheritdoc />
        public async ValueTask<IReadOnlyDictionary<Guid, KrComponents>> GetKrComponentsAsync(CancellationToken cancellationToken = default)
        {
            var types = await this.cardCache.Settings.TryGetAlreadyCachedAsync<KrTypes>(KrTypesKey, cancellationToken).ConfigureAwait(false);
            if (types != null)
            {
                return types.TypeComponents;
            }

            using (await this.asyncLock.EnterAsync(cancellationToken).ConfigureAwait(false))
            {
                types = await this.cardCache.Settings.TryGetAlreadyCachedAsync<KrTypes>(KrTypesKey, cancellationToken).ConfigureAwait(false);
                if (types != null)
                {
                    return types.TypeComponents;
                }

                types = await this.RebuildAllTypesAsync(cancellationToken).ConfigureAwait(false);
                return types.TypeComponents;
            }
        }

        /// <inheritdoc />
        public async Task InvalidateAsync()
        {
            using (await this.asyncLock.EnterAsync().ConfigureAwait(false))
            {
                await this.cardCache.Settings.InvalidateAsync(KrTypesKey).ConfigureAwait(false);
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose() => this.asyncLock.Dispose();

        #endregion

        #region Static Methods

        /// <summary>
        /// Возвращает типы карточек, которые были указаны в настройках типового решения
        /// </summary>
        /// <param name="cardCache">Кэш карточек.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Типы карточек, которые были указаны в настройках типового решения</returns>
        public static async ValueTask<IReadOnlyList<CardRow>> GetKrCardTypesAsync(ICardCache cardCache, CancellationToken cancellationToken = default)
        {
            var krSettings = await cardCache.Cards.GetAsync("KrSettings", cancellationToken).ConfigureAwait(false);
            if (krSettings.Result == CardCacheResult.SingletonNotFound)
            {
                // может возникать при первичном импорте карточек

                logger.Info(
                    "Can't acquire card types from KrSettings due to settings card isn't present in cache." +
                    " Ignore the message during initial installation.");

                return Array.Empty<CardRow>();
            }

            try
            {
                return krSettings.GetValue().Sections["KrSettingsCardTypes"].Rows;
            }
            catch (Exception ex)
            {
                // здесь могут быть другие исключения, такие как ArgumentException;

                // в любом случае при обращении к кэшу нельзя падать,
                // или это может привести к невозможности запустить TessaAdmin, который надо использовать для исправления кэша

                logger.LogException("Can't acquire card types from KrSettings. Error occurred:", ex);

                return Array.Empty<CardRow>();
            }
        }

        #endregion
    }
}
