#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Cards.Extensions.Templates;
using Tessa.Cards.Numbers;
using Tessa.Cards.Workflow;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Files;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.ObjectLocking;
using Tessa.Platform.Redis;
using Tessa.Platform.Scopes;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope
{
    /// <summary>
    /// Объект, предоставляющий методы для управления текущим контекстом <see cref="IKrScope"/>.
    /// </summary>
    /// <remarks>После завершения работы с объектом, для выполнения задач связанных с освобождением ресурсов, вызовите метод <see cref="ExitAsync(IValidationResultBuilder)"/>.</remarks>
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public sealed class KrScopeLevel
    {
        #region Fields

        private readonly ICardRepository cardRepository;
        private readonly ICardRepository cardRepositoryEwt;
        private readonly IKrTokenProvider tokenProvider;
        private readonly IKrTypesCache krTypesCache;
        private readonly IKrStageSerializer serializer;
        private readonly ICardGetStrategy getStrategy;
        private readonly IDbScope dbScope;
        private readonly ICardMetadata cardMetadata;
        private readonly ICardStreamServerRepository cardStreamServerRepository;
        private readonly ICardStreamServerRepository cardStreamServerRepositoryEwt;
        private readonly IKrScope krScope;
        private readonly IInheritableScopeInstance<KrScopeContext> scope;

        #endregion

        #region Constructors

        public KrScopeLevel(
            ICardRepository cardRepository,
            ICardRepository cardRepositoryEwt,
            IKrTokenProvider tokenProvider,
            IKrTypesCache krTypesCache,
            IKrStageSerializer serializer,
            ICardGetStrategy getStrategy,
            ICardTransactionStrategy cardCardTransactionStrategy,
            IDbScope dbScope,
            ICardMetadata cardMetadata,
            ICardStreamServerRepository cardStreamServerRepository,
            ICardStreamServerRepository cardStreamServerRepositoryEwt,
            IKrScope krScope)
        {
            this.cardRepository = NotNullOrThrow(cardRepository);
            this.cardRepositoryEwt = NotNullOrThrow(cardRepositoryEwt);
            this.tokenProvider = NotNullOrThrow(tokenProvider);
            this.krTypesCache = NotNullOrThrow(krTypesCache);
            this.serializer = NotNullOrThrow(serializer);
            this.getStrategy = NotNullOrThrow(getStrategy);
            this.CardTransactionStrategy = NotNullOrThrow(cardCardTransactionStrategy);
            this.dbScope = NotNullOrThrow(dbScope);
            this.cardMetadata = NotNullOrThrow(cardMetadata);
            this.cardStreamServerRepository = NotNullOrThrow(cardStreamServerRepository);
            this.cardStreamServerRepositoryEwt = NotNullOrThrow(cardStreamServerRepositoryEwt);
            this.krScope = NotNullOrThrow(krScope);

            var scope = KrScopeContext.Create();
            scope.Value!.LevelStack.Push(this);
            this.scope = scope;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Идентификатор объекта.
        /// </summary>
        public Guid LevelID { get; } = Guid.NewGuid();

        /// <summary>
        /// Значение, показывающее, что для этого объекта были выполнены действия по освобождению ресурсов.
        /// </summary>
        public bool Exited { get; private set; }

        /// <inheritdoc cref="ICardTransactionStrategy" path="/summary"/>
        public ICardTransactionStrategy CardTransactionStrategy { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Обрабатывает изменения в карточках управляемых <see cref="IKrScope"/>.
        /// </summary>
        /// <param name="mainCardID">Идентификатор карточки документа, в которой запущен процесс.</param>
        /// <param name="isExistsMainCard">Значение <see langword="true"/>, если основная карточка существует, иначе - <see langword="false"/>.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public async Task ApplyChangesAsync(
            Guid mainCardID,
            bool isExistsMainCard,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(validationResult);

            if (!this.krScope.Exists)
            {
                return;
            }

            foreach (var card in this.krScope.GetLoadedCards(true))
            {
                var hasProcessed = await this.SpecialProcessingAsync(
                    mainCardID,
                    card,
                    isExistsMainCard,
                    validationResult,
                    cancellationToken);

                if (hasProcessed)
                {
                    continue;
                }

                if (!validationResult.IsSuccessful())
                {
                    return;
                }

                if (CardSatelliteHelper.TryGetMainCardIDAndTaskRowID(card) is { result: true, mainCardID: not null } satelliteInfo)
                {
                    await this.StoreSatelliteAsync(
                        satelliteInfo.mainCardID.Value,
                        card,
                        mainCardID != satelliteInfo.mainCardID || isExistsMainCard,
                        validationResult,
                        cancellationToken);
                }
                else
                {
                    await this.StoreCardAsync(
                        card,
                        validationResult,
                        cancellationToken);
                }

                if (!validationResult.IsSuccessful())
                {
                    return;
                }
            }

            // Карточка не загружена, но нужно обязательно увеличить ее версию.
            foreach (var cardID in this.krScope.GetForceIncrementCardVersionIdentifiers())
            {
                if (this.krScope.CardIsLoaded(cardID))
                {
                    continue;
                }

                await this.ForceIncrementVersionAsync(
                    cardID,
                    validationResult,
                    cancellationToken);

                if (!validationResult.IsSuccessful())
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Выполняет задачи, связанные с высвобождением ресурсов этого объекта.
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        public async ValueTask ExitAsync(
            IValidationResultBuilder validationResult)
        {
            ThrowIfNull(validationResult);

            if (this.Exited
                || !this.krScope.Exists)
            {
                return;
            }

            this.Exited = true;

            var topLevel = this.krScope.PopCurrentLevel();

            if (topLevel != this)
            {
                validationResult.AddError(this, $"Trying to exit from non-top level. Top level ID = {topLevel.LevelID:B}. Current level ID = {this.LevelID:B}");
                return;
            }

            var ctx = KrScopeContext.Current;
            if (ctx is null)
            {
                return;
            }

            await this.scope.DisposeAsync();

            // Не выполнен выход за внешнюю область видимости в текущем потоке для ctx?
            if (ctx.IsUsed)
            {
                return;
            }

            var lockedCards = ctx.Locks;

            if (lockedCards.Count > 0)
            {
                validationResult.AddError(
                    this,
                    $"Exited {nameof(KrScopeLevel)} contains locked cards ({string.Join(", ", lockedCards)}). All cards inside scope must be saved at the end (no card should be locked).");
                return;
            }

            await ctx.InvalidateAsync();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Обрабатывает карточки специфичным для типа способом.
        /// </summary>
        /// <param name="mainCardID">Идентификатор основной карточки.</param>
        /// <param name="card">Сохраняемая карточка.</param>
        /// <param name="isExistsMainCard">Значение <see langword="true"/>, если основная карточка существует, иначе - <see langword="false"/>.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Значение <see langword="true"/>, если <paramref name="card"/> была обработана, иначе - <see langword="false"/>.</returns>
        private async Task<bool> SpecialProcessingAsync(
            Guid mainCardID,
            Card card,
            bool isExistsMainCard,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken)
        {
            var cardTypeID = card.TypeID;
            if (cardTypeID == DefaultCardTypes.KrSatelliteTypeID)
            {
                await this.StoreKrSatelliteAsync(
                    mainCardID,
                    card,
                    isExistsMainCard,
                    validationResult,
                    cancellationToken);

                return true;
            }

            if (cardTypeID == DefaultCardTypes.KrSecondarySatelliteTypeID)
            {
                await this.StoreKrSecondarySatelliteAsync(
                    mainCardID,
                    card,
                    validationResult,
                    cancellationToken);

                return true;
            }

            return false;
        }

        private async Task StoreKrSatelliteAsync(
            Guid mainCardID,
            Card card,
            bool isExistsMainCard,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken)
        {
            if (this.krScope.IsCardLocked(card.ID))
            {
                return;
            }

            ProcessInfoCacheHelper.Update(this.serializer, card);

            if (card.StoreMode != CardStoreMode.Insert
                && !card.HasChanges())
            {
                return;
            }

            await this.StoreSatelliteAsync(
                mainCardID,
                card,
                isExistsMainCard,
                validationResult,
                cancellationToken);
        }

        private async Task StoreKrSecondarySatelliteAsync(
            Guid mainCardID,
            Card card,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken)
        {
            if (this.krScope.IsCardLocked(card.ID)
                || card
                    .GetApprovalInfoSection()
                    .RawFields
                    .Get<Guid?>(KrConstants.KrProcessCommonInfo.MainCardID) != mainCardID)
            {
                return;
            }

            var isCompletedProcess = card
                .GetApprovalInfoSection()
                .RawFields[KrConstants.KrProcessCommonInfo.CurrentApprovalStageRowID] is null;

            // Процесс по сателлиту закончился, сателлит больше не нужен.
            if (isCompletedProcess)
            {
                // Если карточка уже создана, ее нужно удалить
                if (card.StoreMode == CardStoreMode.Update)
                {
                    await this.DeleteCardAsync(
                        card,
                        validationResult,
                        cancellationToken);
                }

                KrScopeContext.Current!.RemoveCard(card.ID);
            }
            else
            {
                ProcessInfoCacheHelper.Update(this.serializer, card);

                if (card.StoreMode == CardStoreMode.Insert
                    || card.HasChanges())
                {
                    await this.StoreSatelliteAsync(
                        mainCardID,
                        card,
                        true,
                        validationResult,
                        cancellationToken);
                }
            }
        }

        private async Task StoreCardAsync(
            Card card,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var forceAffectVersion = this.krScope.GetForceIncrementCardVersion(card.ID);
            if (!this.krScope.IsCardLocked(card.ID))
            {
                if (!forceAffectVersion
                    && card.StoreMode != CardStoreMode.Insert
                    && !card.HasChanges()
                    && !(card.TryGetWorkflowQueue()?.Items?.Count > 0)
                    && !card.HasNumberQueueToProcess())
                {
                    return;
                }

                ICardRepository suitableCardRepository;
                ICardStreamServerRepository suitableCardStreamServerRepository;

                if (TransactionScopeContext.HasCurrent
                    && TransactionScopeContext.Current.Locks.TryGetValue(new (card.ID, RedisLockKeys.CardsObjectKey), out var objectLockInfo)
                    && objectLockInfo.LockType == ObjectLockTypes.WriteLock)
                {
                    suitableCardRepository = this.cardRepositoryEwt;
                    suitableCardStreamServerRepository = this.cardStreamServerRepositoryEwt;
                }
                else
                {
                    suitableCardRepository = this.cardRepository;
                    suitableCardStreamServerRepository = this.cardStreamServerRepository;
                }

                await this.StoreCardCoreAsync(
                    suitableCardRepository,
                    suitableCardStreamServerRepository,
                    card,
                    validationResult,
                    cancellationToken);
            }
            else if (forceAffectVersion)
            {
                // Карточка загружена, но сохранение заблокировано.
                var newVersion = await this.ForceIncrementVersionAsync(
                    card.ID,
                    validationResult,
                    cancellationToken);

                if (validationResult.IsSuccessful())
                {
                    card.Version = newVersion;
                }
            }
        }

        private async Task StoreSatelliteAsync(
            Guid mainCardID,
            Card satellite,
            bool lockMainCard,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            if (this.krScope.IsCardLocked(satellite.ID)
                || satellite.StoreMode == CardStoreMode.Update
                && !satellite.HasChanges())
            {
                return;
            }

            if (lockMainCard)
            {
                await this.CardTransactionStrategy.ExecuteInWriterLockAsync(
                    mainCardID,
                    CardComponentHelper.DoNotCheckVersion,
                    validationResult,
                    async p =>
                    {
                        await this.StoreCardCoreAsync(
                            this.cardRepository,
                            this.cardStreamServerRepository,
                            satellite,
                            p.ValidationResult,
                            cancellationToken: p.CancellationToken);

                        p.ReportError = !p.ValidationResult.IsSuccessful();
                    },
                    cancellationToken: cancellationToken);
            }
            else
            {
                await this.StoreCardCoreAsync(
                    this.cardRepository,
                    this.cardStreamServerRepository,
                    satellite,
                    validationResult,
                    cancellationToken: cancellationToken);
            }
        }

        private async Task StoreCardCoreAsync(
            ICardRepository cardRepository,
            ICardStreamServerRepository cardStreamServerRepository,
            Card card,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var storedCard = card.Clone();
            card.RemoveChanges(CardRemoveChangesDeletedHandling.Remove);
            card.RemoveWorkflowQueue();
            card.RemoveNumberQueue();

            // Освобождение ресурсов файлового контейнера будет выполнено в методе KrScopeLevel.ExitAsync() после завершения сохранения карточки.
            var fileContainer = this.krScope
                .TryGetLoadedCardFileContainer(card.ID)
                ?.FileContainer;

            await this.StoreCardCoreAsync(
                cardRepository,
                cardStreamServerRepository,
                card,
                storedCard,
                fileContainer,
                validationResult,
                cancellationToken);
        }

        private async Task StoreCardCoreAsync(
            ICardRepository cardRepository,
            ICardStreamServerRepository cardStreamServerRepository,
            Card originalCard,
            Card storedCard,
            IFileContainer? fileContainer,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var storeMode = storedCard.StoreMode;
            if (storeMode == CardStoreMode.Update)
            {
                storedCard.UpdateStates();
            }

            storedCard.RemoveAllButChanged(storeMode);
            var request = new CardStoreRequest
            {
                Card = storedCard,
            };

            this.krScope.ModifyStoreRequest(request);

            if (await KrComponentsHelper.HasBaseAsync(originalCard.TypeID, this.krTypesCache, cancellationToken))
            {
                this.tokenProvider.CreateFullToken(storedCard).Set(storedCard.Info);
            }

            var digest = TryGetCurrentRequestDigest(storedCard.ID)
                ?? await cardRepository.GetDigestAsync(
                    originalCard,
                    CardDigestEventNames.ActionHistoryStoreRouteProcess,
                    cancellationToken);

            if (digest is not null)
            {
                request.SetDigest(digest);
            }

            var response = await CardHelper.StoreAsync(
                request,
                fileContainer,
                cardRepository,
                cardStreamServerRepository,
                cancellationToken);

            if (response is not null)
            {
                validationResult.Add(response.ValidationResult);
                originalCard.Version = response.CardVersion;
            }
        }

        private async Task DeleteCardAsync(
            Card card,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var request = new CardDeleteRequest
            {
                CardID = card.ID,
                CardTypeID = card.TypeID,
                CardTypeName = card.TypeName,
                DeletionMode = CardDeletionMode.WithoutBackup,
            };
            var resp = await this.cardRepository.DeleteAsync(request, cancellationToken);
            validationResult.Add(resp.ValidationResult);
        }

        private async Task<int> ForceIncrementVersionAsync(
            Guid cardID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var newVersion = -1;
            await this.CardTransactionStrategy.ExecuteInReaderLockAsync(
                cardID,
                validationResult,
                async p =>
                    newVersion = await this.ForceIncrementVersionInternalAsync(p),
                cancellationToken: cancellationToken
            );
            return newVersion;
        }

        private async Task<int> ForceIncrementVersionInternalAsync(
            ICardTransactionParameter p)
        {
            var cardID = p.CardID!.Value;
            var getContext = await this.getStrategy.TryLoadCardInstanceAsync(
                cardID,
                this.dbScope.Db,
                this.cardMetadata,
                p.ValidationResult,
                cancellationToken: p.CancellationToken);

            if (!p.ValidationResult.IsSuccessful())
            {
                p.ReportError = true;
                return -1;
            }

            var card = getContext!.Card;
            var token = this.tokenProvider.CreateFullToken(card);
            token.Set(card.Info);

            var digest = TryGetCurrentRequestDigest(cardID)
                ?? await this.cardRepository.GetDigestAsync(
                    card,
                    CardDigestEventNames.ActionHistoryStoreRouteProcess,
                    p.CancellationToken);

            var storeRequest = new CardStoreRequest
            {
                Card = card,
                AffectVersion = true,
            };

            if (digest is not null)
            {
                storeRequest.SetDigest(digest);
            }

            var storeResponse = await this.cardRepository.StoreAsync(
                storeRequest,
                p.CancellationToken);

            p.ValidationResult.Add(storeResponse.ValidationResult);

            if (!p.ValidationResult.IsSuccessful())
            {
                p.ReportError = true;
            }

            return storeResponse.CardVersion;
        }

        private string GetDebuggerDisplay() =>
            $"{DebugHelper.GetTypeName(this)}: {nameof(this.LevelID)}={this.LevelID:B}";

        private static string? TryGetCurrentRequestDigest(Guid cardID) =>
            WorkflowScopeContext.HasCurrent
                && WorkflowScopeContext.Current.StoreContext?.Request is { } workflowRequest
                && workflowRequest.Card.ID == cardID
                ? workflowRequest.TryGetDigest()
                : null;

        #endregion
    }
}
