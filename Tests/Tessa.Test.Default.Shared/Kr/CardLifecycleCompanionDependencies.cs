using System;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Platform;
using Tessa.Platform.Configuration;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Unity;

namespace Tessa.Test.Default.Shared.Kr
{
    /// <inheritdoc cref="ICardLifecycleCompanionDependencies"/>
    public class CardLifecycleCompanionDependencies :
        ICardLifecycleCompanionDependencies
    {
        #region Fields

        private ICardFileManager cardFileManager;
        private readonly Func<ICardFileManager> cardFileManagerFunc;

        private ICardStreamServerRepository cardStreamServerRepository;
        private readonly Func<ICardStreamServerRepository> cardStreamServerRepositoryFunc;

        private ICardStreamClientRepository cardStreamClientRepository;
        private readonly Func<ICardStreamClientRepository> cardStreamClientRepositoryFunc;

        #endregion

        #region Properties

        /// <inheritdoc/>
        public ICardRepository CardRepository { get; }

        /// <inheritdoc/>
        public ICardManager CardManager { get; }

        /// <inheritdoc/>
        public ICardMetadata CardMetadata { get; }

        /// <inheritdoc/>
        public IDbScope DbScope { get; }

        /// <inheritdoc/>
        public ICardFileManager CardFileManager => this.cardFileManager ??= this.cardFileManagerFunc();

        /// <inheritdoc/>
        public ICardStreamServerRepository CardStreamServerRepository
        {
            get
            {
                return this.cardStreamServerRepositoryFunc is null
                    ? null
                    : this.cardStreamServerRepository ??= this.cardStreamServerRepositoryFunc();
            }
        }

        /// <inheritdoc/>
        public ICardStreamClientRepository CardStreamClientRepository
        {
            get
            {
                return this.cardStreamClientRepositoryFunc is null
                    ? null
                    : this.cardStreamClientRepository ??= this.cardStreamClientRepositoryFunc();
            }
        }

        /// <inheritdoc/>
        public ICardCache CardCache { get; }

        /// <inheritdoc/>
        public ICardLifecycleCompanionRequestExtender RequestExtender { get; }

        /// <inheritdoc/>
        public ISession Session { get; }

        /// <inheritdoc/>
        public bool ServerSide { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CardLifecycleCompanionDependencies"/>.
        /// </summary>
        /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
        /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
        /// <param name="cardFileManagerFunc">Функция, возвращающая объект управляющий объектами контейнеров <see cref="ICardFileContainer"/>, объединяющих карточку с её файлами.</param>
        /// <param name="cardCache"><inheritdoc cref="ICardCache" path="/summary"/></param>
        /// <param name="cardManager"><inheritdoc cref="ICardManager" path="/summary"/></param>
        /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
        /// <param name="dbScope">Объект для взаимодействия с базой данных. Должен быть обязательно задан, если используются серверные зависимости.</param>
        /// <param name="requestExtender"><inheritdoc cref="ICardLifecycleCompanionRequestExtender" path="/summary"/></param>
        private CardLifecycleCompanionDependencies(
            ICardRepository cardRepository,
            ICardMetadata cardMetadata,
            Func<ICardFileManager> cardFileManagerFunc,
            ICardCache cardCache,
            ICardManager cardManager,
            ISession session,
            IDbScope dbScope,
            ICardLifecycleCompanionRequestExtender requestExtender = null)
        {
            this.CardRepository = NotNullOrThrow(cardRepository);
            this.CardMetadata = NotNullOrThrow(cardMetadata);
            this.cardFileManagerFunc = NotNullOrThrow(cardFileManagerFunc);
            this.CardManager = NotNullOrThrow(cardManager);
            this.CardCache = NotNullOrThrow(cardCache);
            this.Session = NotNullOrThrow(session);
            this.DbScope = NotNullOrThrow(dbScope);
            this.RequestExtender = requestExtender;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CardLifecycleCompanionDependencies"/> серверными зависимостями.
        /// </summary>
        /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
        /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
        /// <param name="cardFileManagerFunc">Функция, возвращающая объект управляющий объектами контейнеров <see cref="ICardFileContainer"/>, объединяющих карточку с её файлами.</param>
        /// <param name="cardStreamServerRepositoryFunc">Функция, возвращающая репозиторий для потокового управления карточками на сервере.</param>
        /// <param name="cardCache"><inheritdoc cref="ICardCache" path="/summary"/></param>
        /// <param name="cardManager"><inheritdoc cref="ICardManager" path="/summary"/></param>
        /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="requestExtender"><inheritdoc cref="ICardLifecycleCompanionRequestExtender" path="/summary"/></param>
        public CardLifecycleCompanionDependencies(
            ICardRepository cardRepository,
            ICardMetadata cardMetadata,
            Func<ICardFileManager> cardFileManagerFunc,
            Func<ICardStreamServerRepository> cardStreamServerRepositoryFunc,
            ICardCache cardCache,
            ICardManager cardManager,
            ISession session,
            IDbScope dbScope,
            [OptionalDependency] ICardLifecycleCompanionRequestExtender requestExtender = default)
            : this(
                  cardRepository,
                  cardMetadata,
                  cardFileManagerFunc,
                  cardCache,
                  cardManager,
                  session,
                  dbScope,
                  requestExtender)
        {
            this.cardStreamServerRepositoryFunc = NotNullOrThrow(cardStreamServerRepositoryFunc);
            this.ServerSide = true;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CardLifecycleCompanionDependencies"/> клиентскими зависимостями.
        /// </summary>
        /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
        /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
        /// <param name="cardFileManagerFunc">Функция, возвращающая объект управляющий объектами контейнеров <see cref="ICardFileContainer"/>, объединяющих карточку с её файлами.</param>
        /// <param name="cardStreamClientRepositoryFunc">Функция, возвращающая репозиторий для потокового управления карточками на клиенте.</param>
        /// <param name="cardCache"><inheritdoc cref="ICardCache" path="/summary"/></param>
        /// <param name="cardManager"><inheritdoc cref="ICardManager" path="/summary"/></param>
        /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
        /// <param name="dbScope">
        /// Объект для взаимодействия с базой данных. Если параметр не задан,
        /// то он устанавливается равным результату метода <see cref="DbScope.CreateDefault(IConfigurationManager)"/>.
        /// </param>
        /// <param name="requestExtender"><inheritdoc cref="ICardLifecycleCompanionRequestExtender" path="/summary"/></param>
        public CardLifecycleCompanionDependencies(
            ICardRepository cardRepository,
            ICardMetadata cardMetadata,
            Func<ICardFileManager> cardFileManagerFunc,
            Func<ICardStreamClientRepository> cardStreamClientRepositoryFunc,
            ICardCache cardCache,
            ICardManager cardManager,
            ISession session,
            [OptionalDependency] IDbScope dbScope = null,
            [OptionalDependency] ICardLifecycleCompanionRequestExtender requestExtender = null)
            : this(
                  cardRepository,
                  cardMetadata,
                  cardFileManagerFunc,
                  cardCache,
                  cardManager,
                  session,
                  dbScope ?? Tessa.Platform.Data.DbScope.CreateDefault(ConfigurationManager.Default),
                  requestExtender)
        {
            this.cardStreamClientRepositoryFunc = NotNullOrThrow(cardStreamClientRepositoryFunc);
        }

        #endregion
    }
}
