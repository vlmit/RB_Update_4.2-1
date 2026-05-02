using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Numbers;
using Tessa.Cards.Workflow;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Files;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared.Kr
{
    /// <summary>
    /// Предоставляет базовую реализацию <see cref="ICardLifecycleCompanion{T}"/>.
    /// </summary>
    /// <typeparam name="T">Тип объекта, запланированные действия которого выполняются методом <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.</typeparam>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "()}")]
    public class CardLifecycleCompanion<T> :
        PendingActionsProvider<IPendingAction, T>,
        ICardLifecycleCompanion<T>
        where T : CardLifecycleCompanion<T>
    {
        #region Fields

        private ICardFileContainer cardFileContainer;

        private readonly ParentStageRowIDVisitor visitor;

        private readonly T thisObj;

        private readonly CardLifecycleCompanionData lastData = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CardLifecycleCompanion{T}"/>.
        /// </summary>
        /// <param name="deps">Зависимости, используемые при взаимодействии с карточкой.</param>
        private CardLifecycleCompanion(
            ICardLifecycleCompanionDependencies deps)
        {
            ThrowIfNull(deps);

            this.Dependencies = deps;
            this.visitor = new ParentStageRowIDVisitor(deps.CardMetadata);
            this.thisObj = (T) this;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CardLifecycleCompanion{T}"/>.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <param name="cardTypeID">Идентификатор типа карточки.</param>
        /// <param name="cardTypeName">Имя типа карточки.</param>
        /// <param name="deps">Зависимости, используемые при взаимодействии с карточкой.</param>
        public CardLifecycleCompanion(
            Guid cardID,
            Guid? cardTypeID,
            string cardTypeName,
            ICardLifecycleCompanionDependencies deps)
            : this(deps)
        {
            this.CardID = cardID;
            this.CardTypeID = cardTypeID;
            this.CardTypeName = cardTypeName;
            this.Card = null;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CardLifecycleCompanion{T}"/>.
        /// </summary>
        /// <param name="cardTypeID">Идентификатор типа карточки.</param>
        /// <param name="cardTypeName">Имя типа карточки.</param>
        /// <param name="deps">Зависимости, используемые при взаимодействии с карточкой.</param>
        public CardLifecycleCompanion(
            Guid? cardTypeID,
            string cardTypeName,
            ICardLifecycleCompanionDependencies deps)
            : this(Guid.NewGuid(), cardTypeID, cardTypeName, deps)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CardLifecycleCompanion{T}"/>.
        /// </summary>
        /// <param name="card">Карточка, жизненным циклом которой необходимо управлять.</param>
        /// <param name="deps">Зависимости, используемые при взаимодействии с карточкой.</param>
        public CardLifecycleCompanion(
            Card card,
            ICardLifecycleCompanionDependencies deps)
            : this(deps)
        {
            ThrowIfNull(card);

            this.CardID = card.ID;
            this.CardTypeID = card.TypeID;
            this.CardTypeName = card.TypeName;
            this.Card = card;
        }

        #endregion

        #region Properties

        /// <inheritdoc/>
        public Guid? CardTypeID { get; protected set; }

        /// <inheritdoc/>
        public string CardTypeName { get; protected set; }

        /// <inheritdoc/>
        public Guid CardID { get; protected set; }

        /// <inheritdoc/>
        public Card Card { get; protected set; }

        /// <inheritdoc/>
        public ICardLifecycleCompanionDependencies Dependencies { get; }

        /// <inheritdoc/>
        public ICardLifecycleCompanionData LastData => this.lastData;

        /// <inheritdoc/>
        public Dictionary<string, object> Info { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

        #endregion

        #region Public methods

        /// <inheritdoc/>
        public virtual async ValueTask<ICardFileContainer> GetCardFileContainerAsync(
            IFileRequest request = null,
            IList<IFileTag> additionalTags = null,
            CancellationToken cancellationToken = default)
        {
            return this.cardFileContainer ??= await this.Dependencies.CardFileManager.CreateContainerAsync(
                this.GetCardOrThrow(),
                request: request,
                additionalTags: additionalTags,
                cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public virtual T Create(Action<CardNewRequest> modifyRequestAction = null)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this, modifyRequestAction),
                    (action, ct) =>
                        this.CreateActionAsync(action, modifyRequestAction, ct)));
            return this.thisObj;
        }

        /// <inheritdoc/>
        public virtual T Save(Action<CardStoreRequest> modifyRequestAction = null)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (action, ct) =>
                        this.SaveActionAsync(action, modifyRequestAction, ct)));
            return this.thisObj;
        }

        /// <inheritdoc/>
        public virtual T Load(Action<CardGetRequest> modifyRequestAction = null)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (action, ct) =>
                        this.LoadActionAsync(action, modifyRequestAction, ct)));
            return this.thisObj;
        }

        /// <inheritdoc/>
        public virtual T Delete(Action<CardDeleteRequest> modifyRequestAction = null)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (action, ct) =>
                        this.DeleteActionAsync(action, modifyRequestAction, ct)));
            return this.thisObj;
        }

        /// <inheritdoc/>
        public virtual T Export(Action<CardGetRequest> modifyRequestAction = null)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (action, ct) =>
                        this.ExportActionAsync(action, modifyRequestAction, ct)));
            return this.thisObj;
        }

        /// <inheritdoc/>
        public T WithInfoPair(
            string key,
            object val)
        {
            var pendingAction = this.GetLastPendingAction();
            pendingAction.Info[key] = val;
            return this.thisObj;
        }

        /// <inheritdoc/>
        public T WithInfo(
            Dictionary<string, object> info)
        {
            this.GetLastPendingAction().SetInfo(info);
            return this.thisObj;
        }

        /// <inheritdoc/>
        public Card GetCardOrThrow()
        {
            var card = this.Card;
            if (card is null)
            {
                throw new InvalidOperationException("Card isn't specified.");
            }

            return card;
        }

        /// <inheritdoc/>
        public virtual T CreateOrLoadSingleton()
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    this.CreateOrLoadSingletonAsync));

            return this.thisObj;
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask<T> GoCoreAsync(
            Action<ValidationResult> validationFunc = null,
            CancellationToken cancellationToken = default)
        {
            await using (this.Dependencies.DbScope.Create())
            {
                return await base.GoCoreAsync(validationFunc: validationFunc, cancellationToken: cancellationToken);
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Проверяет, что <see cref="Card"/> имеет значение <see langword="null"/>, если это не так, то создаёт исключение <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Card already specified.</exception>
        protected void CheckCardNotExists()
        {
            if (this.Card is not null)
            {
                throw new InvalidOperationException("Card already specified.");
            }
        }

        /// <summary>
        /// Проверяет данные указанные в запросе на соответствие ожидаемым значениям.
        /// </summary>
        /// <param name="actualCardID">Идентификатор карточки в запросе.</param>
        /// <param name="expectedCardID">Ожидаемый идентификатор карточки.</param>
        /// <param name="additionalMessage">Сообщение, добавляемое к описанию ошибки.</param>
        protected static void CheckRequest(
            Guid? actualCardID,
            Guid? expectedCardID,
            string additionalMessage = null)
        {
            if (actualCardID != expectedCardID)
            {
                if (!string.IsNullOrEmpty(additionalMessage))
                {
                    additionalMessage = " " + additionalMessage;
                }

                throw new InvalidOperationException(
                    $"The request parameters do not match the expected card properties.{additionalMessage}{Environment.NewLine}" +
                    $"Actual card ID = {FormatNullable(actualCardID, "N")}, expected card ID = {FormatNullable(expectedCardID, "N")}.");
            }
        }

        #endregion

        #region Private methods

        private async ValueTask<ValidationResult> CreateActionAsync(
            IPendingAction action,
            Action<CardNewRequest> modifyRequestAction = null,
            CancellationToken cancellationToken = default)
        {
            this.CheckCardNotExists();

            var newRequest = new CardNewRequest
            {
                CardTypeID = this.CardTypeID,
                CardTypeName = this.CardTypeName,
            };

            if (action is not null)
            {
                newRequest.Info = action.Info;
            }

            this.Dependencies.RequestExtender?.ExtendNewRequest(newRequest);

            modifyRequestAction?.Invoke(newRequest);

            this.lastData.NewRequest = newRequest;
            this.lastData.NewResponse = null;

            var newResponse = await this.Dependencies.CardRepository.NewAsync(
                newRequest,
                cancellationToken);
            this.lastData.NewResponse = newResponse;

            if (newResponse.ValidationResult.IsSuccessful())
            {
                this.Card = newResponse.Card;

                if (newResponse.Card.ID == Guid.Empty)
                {
                    if (this.CardID == Guid.Empty)
                    {
                        this.CardID = Guid.NewGuid();
                    }

                    newResponse.Card.ID = this.CardID;
                }
                else
                {
                    this.CardID = this.Card.ID;
                }

                this.CardTypeID = this.Card.TypeID;
                this.CardTypeName = this.Card.TypeName;
            }
            else
            {
                this.Card = null;
            }

            return newResponse.ValidationResult.Build();
        }

        private async ValueTask<ValidationResult> SaveActionAsync(
            IPendingAction action,
            Action<CardStoreRequest> modifyRequestAction = null,
            CancellationToken cancellationToken = default)
        {
            var card = this.GetCardOrThrow();

            if (this.Dependencies.ServerSide)
            {
                // Имитация сериализации при передаче клиент - сервер.
                // При сохранении карточка может изменяться в расширениях на сохранение.
                // Из-за отсутствия сериализации это приводит к изменению сохраняемого объекта
                // и возникновению побочных эффектов.
                card = card.Clone();

                if (card.Sections.ContainsKey(KrConstants.KrStages.Virtual))
                {
                    await this.visitor.VisitAsync(
                        card.Sections,
                        DefaultCardTypes.KrCardTypeID,
                        KrConstants.KrStages.Virtual,
                        cancellationToken: cancellationToken);
                }

                card.RemoveAllButChanged(card.StoreMode);
            }

            var expectedCardID = card.ID;
            var storeRequest = new CardStoreRequest
            {
                Card = card,
                Info = action.Info,
            };

            this.Dependencies.RequestExtender?.ExtendStoreRequest(storeRequest);

            modifyRequestAction?.Invoke(storeRequest);

            var storeRequestCard = storeRequest.TryGetCard();

            CheckRequest(
                storeRequestCard?.ID,
                expectedCardID);

            this.lastData.StoreRequest = storeRequest;
            this.lastData.StoreResponse = null;

            CardStoreResponse storeResponse;
            if (this.cardFileContainer is null)
            {
                storeResponse = await this.Dependencies.CardRepository.StoreAsync(
                    storeRequest,
                    cancellationToken);
            }
            else
            {
                if (this.Dependencies.ServerSide)
                {
                    storeResponse = await CardHelper.StoreAsync(
                        storeRequest,
                        this.cardFileContainer?.FileContainer,
                        this.Dependencies.CardRepository,
                        this.Dependencies.CardStreamServerRepository,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    storeResponse = await CardHelper.StoreAsync(
                        storeRequest,
                        this.cardFileContainer?.FileContainer,
                        this.Dependencies.CardRepository,
                        this.Dependencies.CardStreamClientRepository,
                        cancellationToken: cancellationToken);
                }

                if (this.cardFileContainer is not null)
                {
                    await this.cardFileContainer.DisposeAsync();
                    this.cardFileContainer = null;
                }
            }

            this.lastData.StoreResponse = storeResponse;

            card.RemoveWorkflowQueue();
            card.RemoveNumberQueue();

            return storeResponse.ValidationResult.IsSuccessful()
                ? storeResponse.ValidationResult.Add(
                    await this.LoadActionAsync(
                        null,
                        null,
                        cancellationToken)).Build()
                : storeResponse.ValidationResult.Build();
        }

        private async ValueTask<ValidationResult> DeleteActionAsync(
            IPendingAction action,
            Action<CardDeleteRequest> modifyRequestAction = null,
            CancellationToken cancellationToken = default)
        {
            var deleteRequest = new CardDeleteRequest
            {
                CardID = this.CardID,
                CardTypeID = this.CardTypeID,
                CardTypeName = this.CardTypeName,
                Info = action.Info,
                DeletionMode = CardDeletionMode.WithoutBackup,
            };

            this.Dependencies.RequestExtender?.ExtendDeleteRequest(deleteRequest);

            modifyRequestAction?.Invoke(deleteRequest);

            CheckRequest(
                deleteRequest.CardID,
                this.CardID);

            this.lastData.DeleteRequest = deleteRequest;
            this.lastData.DeleteResponse = null;

            var deleteResponse = await this.Dependencies.CardRepository.DeleteAsync(
                deleteRequest,
                cancellationToken);
            this.lastData.DeleteResponse = deleteResponse;

            var validationResult = deleteResponse.ValidationResult.Build();
            if (validationResult.IsSuccessful)
            {
                this.Card = null;
                this.cardFileContainer = null;
            }

            return validationResult;
        }

        private async ValueTask<ValidationResult> LoadActionAsync(
            IPendingAction action,
            Action<CardGetRequest> modifyRequestAction = null,
            CancellationToken cancellationToken = default)
        {
            var getRequest = new CardGetRequest
            {
                CardID = this.CardID,
                CardTypeID = this.CardTypeID,
                CardTypeName = this.CardTypeName
            };

            if (action is not null)
            {
                getRequest.Info = action.Info;
            }

            this.Dependencies.RequestExtender?.ExtendGetRequest(getRequest);

            modifyRequestAction?.Invoke(getRequest);

            this.lastData.GetRequest = getRequest;
            this.lastData.GetResponse = null;

            var getResponse = await this.Dependencies.CardRepository.GetAsync(
                getRequest,
                cancellationToken);
            this.lastData.GetResponse = getResponse;

            if (getResponse.ValidationResult.IsSuccessful())
            {
                this.Card = getResponse.Card;
                this.CardID = this.Card.ID;
                this.CardTypeID = this.Card.TypeID;
                this.CardTypeName = this.Card.TypeName;
            }
            else
            {
                this.Card = null;
            }

            this.cardFileContainer = null;

            return getResponse.ValidationResult.Build();
        }

        private async ValueTask<ValidationResult> CreateOrLoadSingletonAsync(
            IPendingAction action,
            CancellationToken cancellationToken = default)
        {
            this.CheckCardNotExists();

            if (string.IsNullOrEmpty(this.CardTypeName))
            {
                throw new InvalidOperationException(nameof(this.CardTypeName) + " can't be null or empty.");
            }

            var lockObject = KrTestContext.CurrentContext.ScopeContext?.GetNamedLock($"load_singlton_{this.CardTypeName}");
            using var _ = lockObject is not null
                ? await lockObject.EnterAsync(cancellationToken)
                : null;

            var getRequest = new CardGetRequest
            {
                CardTypeName = this.CardTypeName,
                RestrictionFlags = CardGetRestrictionValues.Singleton,
            };

            this.Dependencies.RequestExtender?.ExtendGetRequest(getRequest);

            this.lastData.GetRequest = getRequest;
            this.lastData.GetResponse = null;

            var getResponse = await this.Dependencies.CardRepository.GetAsync(
                getRequest,
                cancellationToken);
            this.lastData.GetResponse = getResponse;

            if (getResponse.ValidationResult.IsSuccessful())
            {
                this.Card = getResponse.Card;
                this.CardID = this.Card.ID;
                this.CardTypeID = this.Card.TypeID;
                this.CardTypeName = this.Card.TypeName;
            }
            else if (getResponse.ValidationResult.Any(x => x.Key == CardValidationKeys.UnknownSingleton))
            {
                return await this.CreateActionAsync(
                    null,
                    null,
                    cancellationToken);
            }
            else
            {
                this.Card = null;
            }

            return getResponse.ValidationResult.Build();
        }

        private async ValueTask<ValidationResult> ExportActionAsync(
            IPendingAction action,
            Action<CardGetRequest> modifyRequestAction = null,
            CancellationToken cancellationToken = default)
        {
            var getRequest = new CardGetRequest
            {
                CardID = this.CardID,
                CardTypeID = this.CardTypeID,
                CardTypeName = this.CardTypeName,
                Info = action.Info,
                RestrictionFlags = CardGetRestrictionValues.ExportAll
            };

            this.Dependencies.RequestExtender?.ExtendGetRequest(getRequest);

            modifyRequestAction?.Invoke(getRequest);

            this.lastData.GetRequest = getRequest;
            this.lastData.GetResponse = null;

            var exportResponse = await this.Dependencies.CardManager.ExportAsync(
                getRequest,
                cancellationToken: cancellationToken);
            this.lastData.GetResponse = exportResponse;

            if (exportResponse.ValidationResult.IsSuccessful())
            {
                this.Card = exportResponse.Card;
                this.CardID = this.Card.ID;
                this.CardTypeID = this.Card.TypeID;
                this.CardTypeName = this.Card.TypeName;
            }
            else
            {
                this.Card = null;
            }

            this.cardFileContainer = null;

            return exportResponse.ValidationResult.Build();
        }

        /// <summary>
        /// Возвращает строковое представление объекта, отображаемое в окне отладчика.
        /// </summary>
        /// <returns>Строковое представление объекта, отображаемое в окне отладчика.</returns>
        private string GetDebuggerDisplay()
        {
            return $"{nameof(this.CardID)} = {this.CardID:B}, " +
                $"{nameof(this.CardTypeID)} = {FormatNullable(this.CardTypeID, "B")}, " +
                $"{nameof(this.CardTypeName)} = {FormatNullable(this.CardTypeName)}, " +
                $"CardIsSet = {this.Card is not null}";
        }

        #endregion

    }
}
