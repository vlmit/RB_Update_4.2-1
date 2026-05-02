#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Cards.Extensions.Templates;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Localization;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope
{
    /// <inheritdoc cref="IKrScope"/>
    public sealed class KrScope :
        IKrScope
    {
        #region Fields

        private const int MaxDepth = 20;

        private readonly ICardRepository cardRepository;

        private readonly ICardRepository cardRepositoryEwt;

        private readonly ICardTransactionStrategy cardTransactionStrategy;

        private readonly ICardGetStrategy cardGetStrategy;

        private readonly IDbScope dbScope;

        private readonly ICardMetadata cardMetadata;

        private readonly IKrTokenProvider tokenProvider;

        private readonly IKrStageSerializer serializer;

        private readonly IKrTypesCache typesCache;

        private readonly ICardFileManager cardFileManager;

        private readonly ICardStreamServerRepository cardStreamServerRepository;

        private readonly ICardStreamServerRepository cardStreamServerRepositoryEwt;

        private readonly ICardTypePriorityComparer? cardTypePriorityComparer;

        private readonly ISession session;

        #endregion

        #region Constructors

        public KrScope(
            ICardRepository cardRepository,
            [Unity.Dependency(CardRepositoryNames.ExtendedWithoutTransactionAndLocking)] ICardRepository cardRepositoryEwt,
            ICardTransactionStrategy cardTransactionStrategy,
            ICardGetStrategy cardGetStrategy,
            IDbScope dbScope,
            ICardMetadata cardMetadata,
            IKrTokenProvider tokenProvider,
            IKrStageSerializer serializer,
            IKrTypesCache typesCache,
            ICardFileManager cardFileManager,
            ICardStreamServerRepository cardStreamServerRepository,
            [Unity.Dependency(CardRepositoryNames.ExtendedWithoutTransactionAndLocking)] ICardStreamServerRepository cardStreamServerRepositoryEwt,
            ICardTypePriorityComparer cardTypePriorityComparer,
            ISession session)
        {
            this.cardRepository = NotNullOrThrow(cardRepository);
            this.cardRepositoryEwt = NotNullOrThrow(cardRepositoryEwt);
            this.cardTransactionStrategy = NotNullOrThrow(cardTransactionStrategy);
            this.cardGetStrategy = NotNullOrThrow(cardGetStrategy);
            this.dbScope = NotNullOrThrow(dbScope);
            this.cardMetadata = NotNullOrThrow(cardMetadata);
            this.tokenProvider = NotNullOrThrow(tokenProvider);
            this.serializer = NotNullOrThrow(serializer);
            this.typesCache = NotNullOrThrow(typesCache);
            this.cardFileManager = NotNullOrThrow(cardFileManager);
            this.cardStreamServerRepository = NotNullOrThrow(cardStreamServerRepository);
            this.cardStreamServerRepositoryEwt = NotNullOrThrow(cardStreamServerRepositoryEwt);
            this.cardTypePriorityComparer = NotNullOrThrow(cardTypePriorityComparer);
            this.session = NotNullOrThrow(session);
        }

        #endregion

        #region IKrScope Members

        /// <inheritdoc />
        public bool Exists => KrScopeContext.HasCurrent;

        /// <inheritdoc />
        public Dictionary<string, object?> Info => GetContext().Info;

        /// <inheritdoc />
        public int Depth => KrScopeContext.Current?.LevelStack.Count ?? 0;

        /// <inheritdoc />
        public IValidationResultBuilder ValidationResult => GetContext().ValidationResult;

        /// <inheritdoc />
        public KrScopeLevel? CurrentLevel => KrScopeContext.Current?.LevelStack.Peek();

        /// <inheritdoc />
        public KrScopeLevel PopCurrentLevel() => GetContext().LevelStack.Pop();

        /// <inheritdoc />
        public KrScopeLevel EnterNewLevel()
        {
            if (MaxDepth <= this.Depth)
            {
                throw new InvalidOperationException(
                    LocalizeFormat(
                        "$KrProcess_MaximumKrScopeDepth",
                        "$CardTypes_Controls_RunOnce"));
            }

            var level = new KrScopeLevel(
                this.cardRepository,
                this.cardRepositoryEwt,
                this.tokenProvider,
                this.typesCache,
                this.serializer,
                this.cardGetStrategy,
                this.cardTransactionStrategy,
                this.dbScope,
                this.cardMetadata,
                this.cardStreamServerRepository,
                this.cardStreamServerRepositoryEwt,
                this);

            return level;
        }

        /// <inheritdoc />
        public async ValueTask<Card?> GetMainCardAsync(
            Guid mainCardID,
            IValidationResultBuilder? validationResult = null,
            bool withoutTransaction = false,
            bool isStore = true,
            Guid? cardTypeID = null,
            CancellationToken cancellationToken = default)
        {
            var context = KrScopeContext.Current;

            if (context is null)
            {
                ThrowIfNull(validationResult);

                return await this.GetMainCardInternalAsync(
                    mainCardID,
                    validationResult,
                    withoutTransaction ? this.cardRepositoryEwt : this.cardRepository,
                    cardTypeID,
                    cancellationToken);
            }

            if (context.Cards.TryGetValue(mainCardID, out var card))
            {
                return card;
            }

            card = await this.GetMainCardInternalAsync(
                mainCardID,
                validationResult ?? this.ValidationResult,
                this.cardRepository,
                cardTypeID,
                cancellationToken);
            if (isStore
                && card is not null)
            {
                context.AddCard(card);
            }

            return card;
        }

        /// <inheritdoc />
        public async Task<ICardFileContainer?> GetMainCardFileContainerAsync(
            Guid mainCardID,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            Guid? cardTypeID = null,
            CancellationToken cancellationToken = default)
        {
            var context = KrScopeContext.Current;

            ICardFileContainer? container = null;
            if (context is null)
            {
                ThrowIfNull(validationResult);
            }
            else if (context.TryGetCardFileContainer(mainCardID, out container))
            {
                return container;
            }

            var card = await this.GetMainCardAsync(
                mainCardID,
                isStore: isStore,
                validationResult: validationResult,
                cardTypeID: cardTypeID,
                cancellationToken: cancellationToken);

            if (card is not null)
            {
                container = await this.GetFileContainerInternalAsync(card, cancellationToken);

                if (container is not null
                    && context is not null)
                {
                    context.AddCardFileContainer(container);
                }
            }

            return container;
        }

        /// <inheritdoc />
        public void ForceIncrementMainCardVersion(Guid cardID)
        {
            GetContext().ForceIncrementCardVersion.Add(cardID);

            this.ModifyStoreRequest(
                cardID,
                static i => i.AffectVersion = true);
        }

        /// <inheritdoc />
        public bool GetForceIncrementCardVersion(Guid cardID) =>
            GetContext().ForceIncrementCardVersion.Remove(cardID);

        /// <inheritdoc />
        public IReadOnlyCollection<Guid> GetForceIncrementCardVersionIdentifiers() =>
            GetContext().ForceIncrementCardVersion;

        /// <inheritdoc/>
        public void ModifyStoreRequest(Guid cardID, Action<CardStoreRequest> requestModifier) =>
            GetContext().ModifyStoreRequest(cardID, requestModifier);

        /// <inheritdoc/>
        public void ModifyStoreRequest(CardStoreRequest request) =>
            GetContext().ModifyStoreRequest(request);

        /// <inheritdoc />
        public async Task EnsureMainCardHasTaskHistoryAsync(
            Guid mainCardID,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            var context = GetContext();

            if (context.CardsWithTaskHistory.Contains(mainCardID))
            {
                return;
            }

            var card = this.GetLoadedCard(mainCardID);

            validationResult ??= this.ValidationResult;
            await this.LoadTaskHistoryAsync(
                card,
                validationResult,
                cancellationToken);

            if (validationResult.IsSuccessful())
            {
                context.CardsWithTaskHistory.Add(mainCardID);
            }
        }
        /// <inheritdoc />

        public async Task EnsureTasksLoadedAsync(
            Guid cardID,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            var context = GetContext();

            if (context.CardsWithTasks.Contains(cardID))
            {
                return;
            }

            var card = this.GetLoadedCard(cardID);

            validationResult ??= this.ValidationResult;
            await this.LoadTasksAsync(
                card,
                validationResult,
                cancellationToken);

            if (validationResult.IsSuccessful())
            {
                context.CardsWithTasks.Add(cardID);
            }
        }

        /// <inheritdoc />
        public async ValueTask<Card?> GetKrSatelliteAsync(
            Guid mainCardID,
            bool noLockingMainCard = false,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            var context = KrScopeContext.Current;
            if (context is null)
            {
                ThrowIfNull(validationResult);

                return await this.GetSatelliteAsync(
                    mainCardID,
                    null,
                    DefaultCardTypes.KrSatelliteTypeID,
                    noLockingMainCard,
                    isStore,
                    validationResult,
                    cancellationToken);
            }

            if (context.TryGetSatellite(
                    mainCardID,
                    null,
                    DefaultCardTypes.KrSatelliteTypeID,
                    out var satellite))
            {
                return satellite;
            }

            return await this.GetSatelliteAsync(
                mainCardID,
                null,
                DefaultCardTypes.KrSatelliteTypeID,
                noLockingMainCard,
                isStore,
                validationResult,
                cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask<Card?> TryGetKrSatelliteAsync(
            Guid mainCardID,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            var context = KrScopeContext.Current;

            if (context is null)
            {
                ThrowIfNull(validationResult);

                return await this.TryGetSatelliteAsync(
                    mainCardID,
                    null,
                    DefaultCardTypes.KrSatelliteTypeID,
                    isStore,
                    validationResult,
                    cancellationToken);
            }

            if (context.TryGetSatellite(
                    mainCardID,
                    null,
                    DefaultCardTypes.KrSatelliteTypeID,
                    out var satellite))
            {
                return satellite;
            }

            satellite = await this.TryGetSatelliteAsync(
                mainCardID,
                null,
                DefaultCardTypes.KrSatelliteTypeID,
                isStore,
                validationResult,
                cancellationToken);

            return satellite;
        }

        /// <inheritdoc />
        public async ValueTask<Guid?> GetCurrentHistoryGroupAsync(
            Guid mainCardID,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfCalledOutsideContext();

            var satellite = await this.GetKrSatelliteAsync(
                mainCardID,
                isStore: isStore,
                validationResult: validationResult,
                cancellationToken: cancellationToken);

            return satellite is null
                ? null
                : satellite.TryGetKrApprovalCommonInfoSection(out var aci)
                && aci.RawFields.TryGetValue(KrConstants.KrApprovalCommonInfo.CurrentHistoryGroup, out var chgObj)
                    ? chgObj as Guid?
                    : null;
        }

        /// <inheritdoc />
        public async Task SetCurrentHistoryGroupAsync(
            Guid mainCardID,
            Guid? newGroupHistoryID,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfCalledOutsideContext();

            var satellite = await this.GetKrSatelliteAsync(
                mainCardID,
                isStore: isStore,
                validationResult: validationResult,
                cancellationToken: cancellationToken);

            if (satellite is not null
                && satellite.TryGetKrApprovalCommonInfoSection(out var aci))
            {
                aci.Fields[KrConstants.KrApprovalCommonInfo.CurrentHistoryGroup] = newGroupHistoryID;
            }
        }

        /// <inheritdoc />
        public async Task<Card?> CreateSecondaryKrSatelliteAsync(
            Guid mainCardID,
            Guid processID,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            var context = GetContext();

            const string errorFormat = $"{DefaultCardTypes.KrSecondarySatelliteTypeName} already exists. Process ID = {{0}}.";

            validationResult ??= this.ValidationResult;

            if (context.TryGetSecondaryKrSatellite(processID, out _))
            {
                validationResult.AddError(
                    this,
                    errorFormat,
                    processID);

                return null;
            }

            Card? satellite = null;

            await this.cardTransactionStrategy.ExecuteInWriterLockAsync(
                mainCardID,
                CardComponentHelper.DoNotCheckVersion,
                validationResult,
                async p =>
                {
                    var satelliteID = await this.GetKrSecondarySatelliteIDByProcessIDAsync(
                        processID,
                        p.CancellationToken);

                    if (satelliteID.HasValue)
                    {
                        p.ValidationResult.AddError(
                            this,
                            errorFormat,
                            processID);

                        p.ReportError = true;
                        return;
                    }

                    var newResponse = await this.cardRepositoryEwt.NewAsync(
                        new CardNewRequest
                        {
                            CardTypeID = DefaultCardTypes.KrSecondarySatelliteTypeID,
                            NewMode = CardNewMode.Valid,
                        },
                        p.CancellationToken);

                    p.ValidationResult.Add(newResponse.ValidationResult);
                    if (!p.ValidationResult.IsSuccessful())
                    {
                        p.ReportError = true;
                        return;
                    }

                    var internalSatellite = newResponse.Card;
                    internalSatellite.ID = processID;

                    SetMainCardID(internalSatellite, p.CardID!.Value);

                    var storedSatellite = internalSatellite.Clone();
                    storedSatellite.RemoveAllButChanged(CardStoreMode.Insert);

                    var storeResponse = await this.cardRepositoryEwt.StoreAsync(
                        new CardStoreRequest
                        {
                            Card = storedSatellite,
                        },
                        p.CancellationToken);

                    p.ValidationResult.Add(storeResponse.ValidationResult);
                    if (!storeResponse.ValidationResult.IsSuccessful())
                    {
                        p.ReportError = true;
                        return;
                    }

                    internalSatellite.RemoveChanges(CardRemoveChangesDeletedHandling.Remove);
                    internalSatellite.Version = storeResponse.CardVersion;

                    satellite = internalSatellite;
                },
                cancellationToken: cancellationToken);

            if (isStore &&
                satellite is not null)
            {
                context.AddSecondaryKrSatellite(processID, satellite);
            }

            return satellite;
        }

        /// <inheritdoc />
        public async ValueTask<Card?> GetSecondaryKrSatelliteAsync(
            Guid processID,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            var context = KrScopeContext.Current;

            Card? satellite;
            if (context is null)
            {
                ThrowIfNull(validationResult);
            }
            else if (context.TryGetSecondaryKrSatellite(processID, out satellite))
            {
                return satellite;
            }

            await using var _ = this.dbScope.Create();
            var satelliteID = await this.GetKrSecondarySatelliteIDByProcessIDAsync(
                processID,
                cancellationToken);

            if (!satelliteID.HasValue)
            {
                return null;
            }

            var request = new CardGetRequest
            {
                CardID = satelliteID.Value,
                CardTypeID = DefaultCardTypes.KrSecondarySatelliteTypeID,
                GetMode = CardGetMode.ReadOnly,
                RestrictionFlags = CardGetRestrictionValues.Satellite,
                SkipTypeResolving = true,
            };

            request.SetNoLockingMainCard(true);

            var response = await this.cardRepository.GetAsync(request, cancellationToken);
            (validationResult ?? this.ValidationResult).Add(response.ValidationResult);

            if (!response.ValidationResult.IsSuccessful())
            {
                return null;
            }

            satellite = response.Card;

            if (isStore)
            {
                context?.AddSecondaryKrSatellite(processID, satellite);
            }

            return satellite;
        }

        /// <inheritdoc />
        public Guid? LockCard(Guid cardID)
        {
            if (this.IsCardLocked(cardID))
            {
                return null;
            }

            var key = Guid.NewGuid();
            GetContext().Locks[cardID] = key;
            return key;
        }

        /// <inheritdoc />
        public bool IsCardLocked(Guid cardID) =>
            GetContext().Locks.ContainsKey(cardID);

        /// <inheritdoc />
        public bool ReleaseCard(
            Guid cardID,
            Guid key)
        {
            var context = GetContext();

            if (!context.Locks.TryGetValue(cardID, out var cardKey)
                || cardKey != key)
            {
                return false;
            }

            context.Locks.Remove(cardID);
            return true;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<Guid> GetLockedCardIDs() => GetContext().Locks.Keys;

        /// <inheritdoc />
        public void AddProcessHolder(
            ProcessHolder processHolder) => GetContext().ProcessHolders.Add(NotNullOrThrow(processHolder));

        /// <inheritdoc />
        public ProcessHolder? GetProcessHolder(
            Guid processHolderID)
        {
            GetContext().ProcessHolders.TryGetItem(processHolderID, out var holder);
            return holder;
        }

        /// <inheritdoc />
        public void RemoveProcessHolder(Guid processHolderID) =>
            GetContext().ProcessHolders.RemoveByKey(processHolderID);

        /// <inheritdoc/>
        public void AddDisposableObject(IDisposable obj) =>
            GetContext().AddDisposableObject(NotNullOrThrow(obj));

        /// <inheritdoc/>
        public void AddDisposableObject(IAsyncDisposable obj) =>
            GetContext().AddAsyncDisposableObject(NotNullOrThrow(obj));

        /// <inheritdoc/>
        public bool CardIsLoaded(Guid cardID) => GetContext().Cards.ContainsKey(cardID);

        /// <inheritdoc/>
        public void AddCard(Card card) =>
            // Параметр будет проверен в AddCard.
            GetContext().AddCard(card);

        /// <inheritdoc/>
        public void RemoveCard(Guid id) => GetContext().RemoveCard(id);

        /// <inheritdoc/>
        public void AddCardFileContainer(ICardFileContainer cardFileContainer) =>
            // Параметр будет проверен в AddCardFileContainer.
            GetContext().AddCardFileContainer(cardFileContainer);

        /// <inheritdoc/>
        public IReadOnlyCollection<Card> GetLoadedCards(bool isSaveOrder = false)
        {
            var context = GetContext();

            return isSaveOrder && this.cardTypePriorityComparer is not null
                ? context
                    .Cards
                    .Values
                    .OrderBy(static i => i, this.cardTypePriorityComparer)
                    .ToArray()
                : context.Cards.Values.AsReadOnlyCollection();
        }

        /// <inheritdoc/>
        public Card? TryGetLoadedCard(Guid cardID)
        {
            GetContext().Cards.TryGetValue(cardID, out var card);
            return card;
        }

        /// <inheritdoc/>
        public ICardFileContainer? TryGetLoadedCardFileContainer(Guid cardID)
        {
            GetContext().TryGetCardFileContainer(cardID, out var cardFileContainer);
            return cardFileContainer;
        }

        /// <inheritdoc/>
        public ValueTask InvalidateAsync() =>
            GetContext().InvalidateAsync();

        /// <inheritdoc/>
        public async ValueTask<Card?> GetSatelliteAsync(
            Guid mainCardID,
            Guid? taskID,
            Guid satelliteTypeID,
            bool noLockingMainCard = false,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            var context = KrScopeContext.Current;
            Card? satellite;

            if (context is null)
            {
                ThrowIfNull(validationResult);
            }
            else if ((satellite = this.TryGetLoadedSatellite(mainCardID, taskID, satelliteTypeID)) is not null)
            {
                return satellite;
            }

            var getRequest = new CardGetRequest()
            {
                CardID = taskID ?? mainCardID,
                CardTypeID = satelliteTypeID,
                RestrictionFlags = CardGetRestrictionValues.Satellite,
            };

            getRequest.SetForbidStoringHistory(true);
            getRequest.SetNoLockingMainCard(noLockingMainCard);

            var getResponse = await this.cardRepository.GetAsync(
                getRequest,
                cancellationToken);

            (validationResult ?? this.ValidationResult).Add(getResponse.ValidationResult);

            if (!getResponse.ValidationResult.IsSuccessful())
            {
                return null;
            }

            satellite = getResponse.Card;

            if (isStore)
            {
                context?.AddSatellite(satellite);
            }

            return satellite;
        }

        /// <inheritdoc/>
        public async ValueTask<Card?> TryGetSatelliteAsync(
            Guid mainCardID,
            Guid? taskID,
            Guid satelliteTypeID,
            bool isStore = true,
            IValidationResultBuilder? validationResult = null,
            CancellationToken cancellationToken = default)
        {
            var context = KrScopeContext.Current;
            Card? satellite;

            if (context is null)
            {
                ThrowIfNull(validationResult);
            }
            else if ((satellite = this.TryGetLoadedSatellite(mainCardID, taskID, satelliteTypeID)) is not null)
            {
                return satellite;
            }

            var satelliteID = await CardSatelliteHelper.TryGetUniversalSatelliteIDAsync(
               this.dbScope,
               mainCardID,
               taskID,
               satelliteTypeID,
               cancellationToken);

            if (!satelliteID.HasValue)
            {
                return null;
            }

            var getRequest = new CardGetRequest()
            {
                CardID = satelliteID,
                CardTypeID = satelliteTypeID,
                RestrictionFlags = CardGetRestrictionValues.Satellite,
                SkipTypeResolving = true,
            };

            getRequest.SetForbidStoringHistory(true);

            var getResponse = await this.cardRepository.GetAsync(
                getRequest,
                cancellationToken);

            (validationResult ?? this.ValidationResult).Add(getResponse.ValidationResult);

            if (!getResponse.ValidationResult.IsSuccessful())
            {
                return null;
            }

            satellite = getResponse.Card;

            if (isStore)
            {
                context?.AddSatellite(satellite);
            }

            return satellite;
        }

        /// <inheritdoc/>
        public Card? TryGetLoadedSatellite(
            Guid mainCardID,
            Guid? taskID,
            Guid satelliteTypeID)
        {
            GetContext().TryGetSatellite(mainCardID, taskID, satelliteTypeID, out var satellite);
            return satellite;
        }

        #endregion

        #region Private Methods

        private static void SetMainCardID(Card satelliteCard, Guid mainCardID)
        {
            CardSatelliteHelper.SetupUniversalSatellite(satelliteCard, mainCardID);
            if (!satelliteCard.TryGetKrApprovalCommonInfoSection(out var satelliteInfoSection))
            {
                return;
            }

            satelliteInfoSection.Fields[KrConstants.KrProcessCommonInfo.MainCardID] = mainCardID;
        }

        private async Task<Card?> GetMainCardInternalAsync(
            Guid mainCardID,
            IValidationResultBuilder validationResult,
            ICardRepository cardRepository,
            Guid? cardTypeID,
            CancellationToken cancellationToken = default)
        {
            var request = new CardGetRequest
            {
                CardID = mainCardID,
                CardTypeID = cardTypeID,
                GetMode = CardGetMode.ReadOnly,
                RestrictionFlags = CardGetRestrictionFlags.RestrictTasks | CardGetRestrictionFlags.RestrictTaskHistory,
            };
            request.IgnoreButtons();
            request.IgnoreKrSatellite();
            request.SetForbidStoringHistory(true);
            // Основной карточке создаем токен, чтобы процесс мог к ней обращаться
            // вне зависимости от прав пользователя.
            var token = this.tokenProvider.CreateFullToken(mainCardID);
            token.Set(request.Info);

            var response = await cardRepository.GetAsync(request, cancellationToken);
            validationResult.Add(response.ValidationResult);

            return response.ValidationResult.IsSuccessful()
                ? response.Card
                : null;
        }

        /// <summary>
        /// Возвращает контейнер, содержащий информацию по карточке и её файлам созданный для указанной карточки.
        /// </summary>
        /// <param name="card">Карточка для которой должны быть создан файловый контейнер.</param>
        /// <param name="cancellationToken">Объект, посредством которого может быть отменена асинхронная задача.</param>
        /// <returns>Контейнер, содержащий информацию по карточке и её файлам.</returns>
        private async Task<ICardFileContainer?> GetFileContainerInternalAsync(
            Card card,
            CancellationToken cancellationToken = default)
        {
            ICardFileContainer? container = null;

            try
            {
                container = await this.cardFileManager.CreateContainerAsync(card, cancellationToken: cancellationToken);
                if (!container.CreationResult.IsSuccessful)
                {
                    return null;
                }

                var result = container;
                container = null;

                return result;
            }
            finally
            {
                if (container is not null)
                {
                    await container.DisposeAsync();
                }
            }
        }

        private async Task LoadTaskHistoryAsync(
            Card card,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var insertedHistoryItems = card
                .TaskHistory
                .Where(p => p.State == CardTaskHistoryState.Inserted)
                .ToList();
            var insertedGroupItems = card
                .TaskHistoryGroups
                .Where(p => p.State == CardTaskHistoryState.Inserted)
                .ToList();

            card.TaskHistory.Clear();
            card.TaskHistoryGroups.Clear();

            await using (this.dbScope.Create())
            {
                await this.cardGetStrategy.LoadTaskHistoryAsync(
                    card.ID,
                    card,
                    this.dbScope.Db,
                    this.cardMetadata,
                    validationResult,
                    [],
                    cancellationToken: cancellationToken);
            }

            card.TaskHistory.AddRange(insertedHistoryItems);
            card.TaskHistoryGroups.AddRange(insertedGroupItems);
        }

        private async Task LoadTasksAsync(
            Card card,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var existingTasks = card
                .Tasks
                .Select(x => x.RowID)
                .ToHashSet();
            var tasksByRowID = new Dictionary<Guid, CardTask>();

            await using var _ = this.dbScope.Create();
            var db = this.dbScope.Db;
            var taskContexts = await this.cardGetStrategy.TryLoadTaskInstancesAsync(
                card.ID,
                card,
                db,
                this.cardMetadata,
                validationResult,
                this.session,
                getTaskMode: CardGetTaskMode.All,
                loadCalendarInfo: false,
                tasksByRowID: tasksByRowID,
                cancellationToken: cancellationToken);

            if (taskContexts is { Count: > 0 })
            {
                foreach (var taskContext in taskContexts)
                {
                    if (existingTasks.Contains(taskContext.CardID))
                    {
                        card.Tasks.Remove(tasksByRowID[taskContext.CardID]);
                        continue;
                    }

                    await this.cardGetStrategy.LoadSectionsAsync(taskContext, cancellationToken);
                }

                foreach (var (_, task) in tasksByRowID)
                {
                    task.Flags |= CardTaskFlags.HistoryItemCreated;
                }
            }
        }

        private static KrScopeContext GetContext(
            [CallerMemberName] string? memberName = null)
        {
            var context = KrScopeContext.Current;
            if (context is null)
            {
                ThrowMemberCalledOutsideOfContext(memberName);
            }

            return context;
        }

        private static void ThrowIfCalledOutsideContext(
            [CallerMemberName] string? memberName = null)
        {
            var context = KrScopeContext.Current;
            if (context is null)
            {
                ThrowMemberCalledOutsideOfContext(memberName);
            }
        }

        [DoesNotReturn]
        private static void ThrowMemberCalledOutsideOfContext(
            string? memberName = null) =>
            throw new InvalidOperationException($"Member {memberName} called outside of {nameof(IKrScope)} context. It is necessary to create a context by calling the {nameof(IKrScope.EnterNewLevel)} method.");

        private Task<Guid?> GetKrSecondarySatelliteIDByProcessIDAsync(
            Guid processID,
            CancellationToken cancellationToken = default)
        {
            var db = this.dbScope.Db;
            return db
                .SetCommand(
                    this.dbScope.BuilderFactory
                        .Select()
                        .C("ID")
                        .From("WorkflowProcesses").NoLock()
                        .Where().C("RowID").Equals().P(nameof(processID))
                        .Build(),
                    db.Parameter(nameof(processID), processID))
                .LogCommand()
                .ExecuteAsync<Guid?>(cancellationToken);
        }

        private Card GetLoadedCard(
            Guid cardID) =>
            this.TryGetLoadedCard(cardID) ?? throw new InvalidOperationException($"Card with ID = {cardID:B} not found in {nameof(IKrScope)}.");

        #endregion
    }
}
