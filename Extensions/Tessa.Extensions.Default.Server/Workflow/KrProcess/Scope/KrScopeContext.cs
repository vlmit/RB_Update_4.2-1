#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Tessa.Cards;
using Tessa.Cards.Extensions.Templates;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Scopes;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope
{
    /// <summary>
    /// Контекст Kr расширений на сохранение.
    /// </summary>
    public sealed class KrScopeContext :
        IDisposable
    {
        #region Constants And Static Fields

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Fields

        /// <summary>
        /// Словарь, содержащий идентификаторы карточек сателлитов вторичных процессов (<see cref="DefaultCardTypes.KrSecondarySatelliteTypeName"/>), расположенных в контексте. Ключом является идентификатор вторичного процесса. Объекты карточек содержатся в <see cref="Cards"/>.
        /// </summary>
        private readonly Dictionary<Guid, Guid> secondaryKrSatellites = [];

        /// <summary>
        /// Словарь объектов <see cref="ICardFileContainer"/>, расположенных в контексте. Доступ осуществляется по идентификатору карточки, к которой относится объект.
        /// </summary>
        private readonly Dictionary<Guid, ICardFileContainer> cardFileContainers = [];

        private readonly List<object> disposableObjects = [];

        /// <summary>
        /// Идентификаторы карточек сателлитов, загруженных в <see cref="Cards"/>.
        /// </summary>
        /// <remarks>
        /// Ключ состоит из идентификаторов основной карточки, идентификатора задания и типа карточки сателлита.
        /// </remarks>
        private readonly Dictionary<(Guid MainCardID, Guid? TaskID, Guid SatelliteTypeID), Guid> satelliteIdentifiers = [];

        private readonly Dictionary<Guid, Action<CardStoreRequest>> requestModifiers = [];

        private readonly Dictionary<Guid, Card> cards = [];

        #endregion

        #region Constructors

        private KrScopeContext()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Объект, выполняющий построение результата валидации.
        /// </summary>
        public ValidationResultBuilder ValidationResult { get; } = [];

        /// <summary>
        /// Набор объектов <see cref="ProcessHolder"/>, доступ к которым осуществляется по <see cref="ProcessHolder.ProcessHolderID"/>.
        /// </summary>
        public HashSet<Guid, ProcessHolder> ProcessHolders { get; } = new(static p => p.ProcessHolderID);

        /// <summary>
        /// Словарь карточек, расположенных в контексте. Доступ осуществляется по их идентификатору.
        /// </summary>
        public IReadOnlyDictionary<Guid, Card> Cards => this.cards;

        /// <summary>
        /// Набор идентификаторов карточек, для которых была загружена история заданий. Объекты карточек содержатся в <see cref="Cards"/>.
        /// </summary>
        public HashSet<Guid> CardsWithTaskHistory { get; } = [];

        /// <summary>
        /// Набор идентификаторов карточек, для которых были загружены задания. Объекты карточек содержатся в <see cref="Cards"/>.
        /// </summary>
        public HashSet<Guid> CardsWithTasks { get; } = [];

        /// <summary>
        /// Набор идентификаторов карточек, для которых должен быть принудительно увеличен номер версии при сохранении. Объекты карточек могут не содержатся в <see cref="Cards"/>.
        /// </summary>
        public HashSet<Guid> ForceIncrementCardVersion { get; } = [];

        /// <summary>
        /// Словарь с дополнительной информацией, сохранённой в контексте.
        /// </summary>
        public Dictionary<string, object?> Info { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Значение, показывающее, что текущий объект может быть доступен через свойство <see cref="Current"/> в соответствующем потоке.
        /// </summary>
        public bool IsUsed { get; private set; } = true;

        /// <summary>
        /// Словарь, содержащий идентификаторы заблокированных карточек (ключи) и ключи для снятия блокировки (значения).
        /// </summary>
        internal Dictionary<Guid, Guid> Locks { get; } = [];

        internal Stack<KrScopeLevel> LevelStack { get; } = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Добавляет карточку сателлита в контекст.
        /// </summary>
        /// <param name="satellite">Карточка сателлита.</param>
        public void AddSatellite(Card satellite)
        {
            ThrowIfNull(satellite);
            var fields = satellite
                .Sections[CardSatelliteHelper.SatellitesSectionName]
                .RawFields;

            var mainCardID = fields.Get<Guid>(CardSatelliteHelper.MainCardIDColumn);
            var taskRowID = fields.TryGet<Guid?>(CardSatelliteHelper.TaskRowIDColumn);

            this.AddSatelliteIdentifier(
                mainCardID,
                taskRowID,
                satellite.TypeID,
                satellite.ID);

            this.AddCard(satellite);
        }

        /// <summary>
        /// Возвращает карточку сателлита, содержащуюся в контексте.
        /// </summary>
        /// <param name="mainCardID">Идентификатор основной карточки.</param>
        /// <param name="taskID">Идентификатор задания, если сателлит относится к заданию.</param>
        /// <param name="satelliteTypeID">Идентификатор типа сателлита.</param>
        /// <param name="satellite">Сателлит или значение <see langword="null"/>, если он не содержится в контексте.</param>
        /// <returns>Значение <see langword="true"/>, если сателлит содержится в контексте, иначе - <see langword="false"/>.</returns>
        public bool TryGetSatellite(
            Guid mainCardID,
            Guid? taskID,
            Guid satelliteTypeID,
            [MaybeNullWhen(false)] out Card satellite)
        {
            satellite = null;
            return this.satelliteIdentifiers.TryGetValue((mainCardID, taskID, satelliteTypeID), out var satelliteID)
                && this.Cards.TryGetValue(satelliteID, out satellite);
        }

        /// <summary>
        /// Добавляет карточку сателлита вторичного процесса в контекст.
        /// </summary>
        /// <param name="secondaryProcessID">Идентификатор вторичного процесса.</param>
        /// <param name="satellite">Сателлит вторичного процесса.</param>
        public void AddSecondaryKrSatellite(
            Guid secondaryProcessID,
            Card satellite)
        {
            ThrowIfNull(satellite);
            ThrowIf(satellite, satellite.TypeID != DefaultCardTypes.KrSecondarySatelliteTypeID);

            var satelliteID = satellite.ID;
            this.secondaryKrSatellites[secondaryProcessID] = satelliteID;
            this.AddCard(satellite);

            var mainCardID = satellite
                .Sections[CardSatelliteHelper.SatellitesSectionName]
                .RawFields
                .Get<Guid>(CardSatelliteHelper.MainCardIDColumn);

            this.AddSatelliteIdentifier(
                mainCardID,
                null,
                DefaultCardTypes.KrSecondarySatelliteTypeID,
                satelliteID);
        }

        /// <summary>
        /// Возвращает карточку сателлита вторичного процесса, содержащуюся в контексте.
        /// </summary>
        /// <param name="secondaryProcessID">Идентификатор вторичного процесса.</param>
        /// <param name="satellite">Карточка сателлита вторичного процесса или значение <see langword="null"/>, если она не найдена.</param>
        /// <returns>Значение <see langword="true"/>, если сателлит вторичного процесса найден в контексте, иначе - <see langword="false"/>.</returns>
        public bool TryGetSecondaryKrSatellite(
            Guid secondaryProcessID,
            [MaybeNullWhen(false)] out Card satellite)
        {
            satellite = null;
            return this.secondaryKrSatellites.TryGetValue(secondaryProcessID, out var satelliteID)
                && this.Cards.TryGetValue(satelliteID, out satellite);
        }

        /// <summary>
        /// Добавляет <see cref="ICardFileContainer"/> в контекст.
        /// </summary>
        /// <param name="cardFileContainer"><inheritdoc cref="ICardFileContainer" path="/summary"/></param>
        public void AddCardFileContainer(
            ICardFileContainer cardFileContainer)
        {
            ThrowIfNull(cardFileContainer);

            var cardID = cardFileContainer.Card.ID;

            if (!this.Cards.TryGetValue(cardID, out var card))
            {
                throw new ArgumentException($"The scope does not contain the card contained in the specified {nameof(ICardFileContainer)}. Add the card to this context.", nameof(cardFileContainer));
            }

            if (!ReferenceEquals(card, cardFileContainer.Card))
            {
                throw new ArgumentException($"The scope does not contain the same card object that is contained in the specified {nameof(ICardFileContainer)}. Add the card to this context.", nameof(cardFileContainer));
            }

            this.cardFileContainers[cardID] = cardFileContainer;

            this.AddAsyncDisposableObject(cardFileContainer);
        }

        /// <summary>
        /// Возвращает <see cref="ICardFileContainer"/>, содержащийся в контексте.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <param name="cardFileContainer"><see cref="ICardFileContainer"/> или значение <see langword="null"/>, если он не найден.</param>
        /// <returns>Значение <see langword="true"/>, если <see cref="ICardFileContainer"/> содержится в контексте, иначе - <see langword="false"/>.</returns>
        public bool TryGetCardFileContainer(
            Guid cardID,
            [MaybeNullWhen(false)] out ICardFileContainer cardFileContainer) =>
            this.cardFileContainers.TryGetValue(cardID, out cardFileContainer);

        /// <summary>
        /// Добавляет объект, для которого нужно вызвать метод <see cref="IDisposable.Dispose"/> при выполнении метода <see cref="InvalidateAsync"/>.
        /// </summary>
        /// <param name="disposable">Объект, ресурсы которого требуется освободить.</param>
        public void AddDisposableObject(IDisposable disposable) =>
            this.AddDisposableObjectCore(disposable);

        /// <summary>
        /// Добавляет объект, для которого нужно вызвать метод <see cref="IAsyncDisposable.DisposeAsync"/> при выполнении метода <see cref="InvalidateAsync"/>.
        /// </summary>
        /// <param name="asyncDisposable">Объект, ресурсы которого требуется освободить.</param>
        public void AddAsyncDisposableObject(IAsyncDisposable asyncDisposable) =>
            this.AddDisposableObjectCore(asyncDisposable);

        /// <summary>
        /// Метод для отложенной модификации запроса на сохранение карточки.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <param name="requestModifier">Метод для модификации запроса на сохранение карточки.</param>
        public void ModifyStoreRequest(
            Guid cardID,
            Action<CardStoreRequest> requestModifier)
        {
            ThrowIfNull(requestModifier);

            if (this.requestModifiers.TryGetValue(cardID, out var prevModifier))
            {
                this.requestModifiers[cardID] = (r) =>
                {
                    prevModifier(r);
                    requestModifier(r);
                };
            }
            else
            {
                this.requestModifiers[cardID] = requestModifier;
            }
        }

        /// <summary>
        /// Метод для модификации запроса на сохранения с помощью отложенных методов, добавленных <see cref="ModifyStoreRequest(Guid, Action{CardStoreRequest})"/>.
        /// </summary>
        /// <param name="request">Обрабатываемый запрос на сохранение.</param>
        public void ModifyStoreRequest(CardStoreRequest request)
        {
            ThrowIfNull(request);

            if (request.Card is not null
                && this.requestModifiers.TryGetValue(request.Card.ID, out var modifier))
            {
                modifier(request);
            }
        }

        /// <summary>
        /// Добавляет карточку в контекст.
        /// </summary>
        /// <param name="card">Добавляемая карточка.</param>
        public void AddCard(
            Card card)
        {
            ThrowIfNull(card);

            this.cards[card.ID] = card;
        }

        /// <summary>
        /// Удаляет карточку из контекста.
        /// </summary>
        /// <param name="id">Идентификатор карточки.</param>
        public void RemoveCard(Guid id)
        {
            if (!this.cards.Remove(id, out var card))
            {
                return;
            }

            this.CardsWithTaskHistory.Remove(id);
            this.CardsWithTasks.Remove(id);

            foreach (var satelliteKey in this.satelliteIdentifiers
                .Where(i => i.Value == id)
                .Select(static i => i.Key)
                .ToArray())
            {
                this.satelliteIdentifiers.Remove(satelliteKey);
            }

            if (this.secondaryKrSatellites.FirstOrDefault(i => i.Value == id) is { } secondaryKrSatellitePair)
            {
                this.secondaryKrSatellites.Remove(secondaryKrSatellitePair.Key);
            }
        }

        /// <summary>
        /// Выполняет действия по освобождению ресурсов, занимаемых этим объектом.
        /// </summary>
        /// <returns>Асинхронная задача.</returns>
        public async ValueTask InvalidateAsync()
        {
            if (this.IsUsed)
            {
                throw new InvalidOperationException($"{this.GetType().Name} is used.");
            }

            // освобождение в порядке, обратном регистрации
            var instancesToDispose = Enumerable.Reverse(this.disposableObjects).ToArray();
            this.disposableObjects.Clear();

            foreach (var disposableObject in instancesToDispose)
            {
                try
                {
                    switch (disposableObject)
                    {
                        case IAsyncDisposable asyncDisposable:
                            await asyncDisposable.DisposeAsync();
                            break;
                        case IDisposable disposable:
                            disposable.Dispose();
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
                catch (Exception ex)
                {
                    logger.LogException(ex, LogLevel.Warn);
                }
            }

            this.cardFileContainers.Clear();

            this.cards.Clear();
            this.CardsWithTaskHistory.Clear();
            this.ForceIncrementCardVersion.Clear();
            this.LevelStack.Clear();
            this.Locks.Clear();
            this.ProcessHolders.Clear();
            this.secondaryKrSatellites.Clear();
        }

        #endregion

        #region Static Members

        /// <summary>
        /// Текущий контекст <see cref="KrScopeContext"/> или значение <see langword="null"/>, если он недоступен.
        /// </summary>
        public static KrScopeContext? Current => InheritableRetainingScope<KrScopeContext>.Value;

        /// <summary>
        /// Признак того, что текущий код выполняется внутри операции с контекстом <see cref="KrScopeContext"/>,
        /// а свойство <see cref="Current"/> ссылается на действительный контекст.
        /// </summary>
        /// <remarks>
        /// Если текущее свойство возвращает <c>false</c>, то свойство <see cref="Current"/>
        /// возвращает ссылку на пустой контекст.
        /// </remarks>
        public static bool HasCurrent => Current is not null;

        /// <summary>
        /// Создаёт область видимости для значения в текущем потоке.
        /// </summary>
        /// <returns>Созданная область видимости.</returns>
        public static IInheritableScopeInstance<KrScopeContext> Create() =>
            InheritableRetainingScope<KrScopeContext>.Create(() => new KrScopeContext());

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Уведомляет о том, что объект больше не доступен по свойству <see cref="Current"/>.
        /// </summary>
        public void Dispose() => this.IsUsed = false;

        #endregion

        #region Private Methods

        private void AddDisposableObjectCore(object disposable)
        {
            ThrowIfNull(disposable);

            this.disposableObjects.Add(disposable);
        }

        private void AddSatelliteIdentifier(
            Guid mainCardID,
            Guid? taskID,
            Guid satelliteTypeID,
            Guid satelliteID) =>
            this.satelliteIdentifiers[(mainCardID, taskID, satelliteTypeID)] = satelliteID;

        #endregion
    }
}
