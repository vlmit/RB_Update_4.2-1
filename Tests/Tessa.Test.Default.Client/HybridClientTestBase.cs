using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Web;
using Tessa.Test.Default.Client.Web;
using Tessa.Test.Default.Shared;
using Tessa.Test.Default.Shared.GC;
using Tessa.Test.Default.Shared.GC.Handlers;
using Tessa.Test.Default.Shared.Web;
using Tessa.Web;
using Tessa.Web.Services;
using Unity;
using Unity.Injection;
using Unity.Lifetime;

namespace Tessa.Test.Default.Client
{
    /// <summary>
    /// Абстрактный базовый класс, предоставляющий методы для выполнения клиентских тестов
    /// без поддержки пользовательского интерфейса на специально подготовленном сервере приложений.
    /// </summary>
    public abstract class HybridClientTestBase :
        ClientTestBase
    {
        #region Constants And Static Fields

        private static readonly AsyncSynchronizedOneTimeRegistrator initializeWebServerRegistrator = new(() => WebHelper.InitializeWebServerAsync(initializeLocalization: false));

        /// <summary>
        /// Базовый адрес сервера приложений по умолчанию.
        /// </summary>
        public const string DefaultBaseAddress = "http://localhost/";

        #endregion

        #region Fields

        /// <summary>
        /// Unity контейнер, используемый при инициализации серверного контейнера.<para/>
        /// Необходим из-за недоступности контекста в <see cref="WebUnityFactory.CreateContainerAsync(WebContainerCreationOptions,CancellationToken)"/>.
        /// </summary>
        private IUnityContainer unityContainerForInitializeServerContainer;

        #endregion

        #region Properties

        /// <summary>
        /// Возвращает фабрику, предназначенную для создания объектов, с помощью которых можно реализовать функциональные тесты для web-приложений.
        /// </summary>
        public IWebApplicationFactory WebApplicationFactory { get; private set; }

        /// <summary>
        /// Возвращает значение, показывающее, необходимо ли в качестве источника файлов по умолчанию использовать базу данных или нет.
        /// </summary>
        public virtual bool UseDatabaseAsDefault => false;

        /// <summary>
        /// Возвращает значение, показывающее, необходимо ли использовать коммуникация между процессами или нет.
        /// </summary>
        public virtual bool EnableInterprocessCommunication => false;

        /// <summary>
        /// Возвращает Unity-контейнер, используемый на сервере.
        /// </summary>
        /// <remarks>
        /// Не используйте значение этого свойства для регистрации зависимостей. Для регистрации зависимостей, используемых на сервере, необходимо переопределить метод <see cref="InitializeContainerServerAsync(IUnityContainer, IWebContextAccessor)"/>. Для изменения создаваемого серверного контейнера необходимо переопределить метод <see cref="CreateContainerServerAsync()"/>.
        /// </remarks>
        public IUnityContainer UnityContainerServer
        {
            get
            {
                if (this.WebApplicationFactory is not { } factory)
                {
                    throw new InvalidOperationException($"{nameof(HybridClientTestBase)}.{nameof(this.WebApplicationFactory)} is not initialized.");
                }

                var unityHolder = factory.Server.Services.GetRequiredService<IWebUnityHolder>();
                return unityHolder.Container;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="HybridClientTestBase"/>.
        /// </summary>
        protected HybridClientTestBase() =>
            this.PlannedInitializeTestServer();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override string BaseAddressOverride => DefaultBaseAddress;

        /// <inheritdoc/>
        protected override string UserNameOverride => "admin";

        /// <inheritdoc/>
        protected override string PasswordOverride => "admin";

        /// <inheritdoc/>
        protected override async ValueTask InitializeContainerAsync(IUnityContainer container)
        {
            await base.InitializeContainerAsync(container);

            var applicationFolders = container.Resolve<IApplicationFolders>();
            applicationFolders.LocalData = Path.Combine(await this.GetFileStoragePathAsync(), "local_data");
            applicationFolders.RoamingData = Path.Combine(await this.GetFileStoragePathAsync(), "roaming_data");
        }

        protected override async ValueTask CreateAndInitializeContainerAsync()
        {
            await base.CreateAndInitializeContainerAsync();

            // действие необходимо выполнить, когда и произошла инициализация серверного контейнера (ActionStage.BeforeInitializeContainer),
            // и выполнены действия атрибута SetupTempDb, которые создали БД и заменили свойство this.DbScope (ActionStage.AfterInitializeContainer);
            // в этом случае метод EnsureRedisInitializedAsync сможет выполнить получение из БД значений для записи в Redis

            await this.EnsureRedisInitializedAsync(this.UnityContainerServer);
        }

        /// <inheritdoc/>
        protected override void BeforeRegisterExtensionsOnClient(IUnityContainer unityContainer)
        {
            base.BeforeRegisterExtensionsOnClient(unityContainer);

            unityContainer
                .RegisterType<IHttpClientFactory, TestServerHttpClientFactory>(
                    new ContainerControlledLifetimeManager(),
                    new InjectionConstructor(
                        new InjectionParameter<IWebApplicationFactory>(this.WebApplicationFactory)));
        }

        /// <inheritdoc/>
        protected override async ValueTask<string> GetBaseAddressAsync()
        {
            var address = await base.GetBaseAddressAsync();

            if (!this.GetType().IsDefined(typeof(SetupTempDbAttribute), true))
            {
                return address;
            }

            if (string.IsNullOrEmpty(address))
            {
                address = DefaultBaseAddress;
            }

            var builder = new UriBuilder(address);
            builder.Host = $"{builder.Host}_{(await this.GetFixtureDateTimeAsync()).FormatDateTimeCode()}_{await this.GetFixtureNameAsync()}";
            return builder.ToString();
        }

        /// <inheritdoc/>
        protected override async Task OneTimeTearDownCoreAsync()
        {
            if (this.WebApplicationFactory is not null)
            {
                await this.WebApplicationFactory.Host.StopAsync();
                await this.WebApplicationFactory.DisposeAsync();
            }

            await base.OneTimeTearDownCoreAsync();
        }

        /// <inheritdoc/>
        protected override void SetServerUtcNow(DateTime? utcNow) =>
            this.UnityContainerServer
                .Resolve<MutableClock>()
                .MutableUtcNow = utcNow;

        #endregion

        #region Protected Methods

        /// <summary>
        /// Создаёт Unity-контейнер, используемый на сервере.
        /// </summary>
        /// <param name="options">Опции создания веб сервера.</param>
        /// <param name="context">Дополнительный контекст.</param>
        /// <param name="webContextAccessor"><inheritdoc cref="IWebContextAccessor" path="/summary"/></param>
        /// <returns>Созданный Unity-контейнер.</returns>
        protected virtual async ValueTask<IUnityContainer> CreateContainerServerAsync(
            WebContainerCreationOptions options,
            object context,
            IWebContextAccessor webContextAccessor)
        {
            var container = await this.CreateContainerServerAsync();
            await this.InitializeContainerServerAsync(container, webContextAccessor);
            return container;
        }

        /// <summary>
        /// Создаёт серверный Unity контейнер.
        /// </summary>
        /// <returns>Созданный серверный Unity контейнер.</returns>
        /// <remarks>
        /// В созданной контейнере должна быть зарегистрирована зависимость <see cref="ITestNameResolver"/>.
        /// </remarks>
        protected virtual ValueTask<IUnityContainer> CreateContainerServerAsync() =>
            new(new UnityContainer().RegisterSingleton<ITestNameResolver, TestNameResolver>());

        /// <summary>
        /// Инициализирует серверный Unity контейнер.
        /// </summary>
        /// <param name="container">Инициализируемый серверный Unity контейнер.</param>
        /// <param name="webContextAccessor"><inheritdoc cref="IWebContextAccessor" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        protected virtual async ValueTask InitializeContainerServerAsync(
            IUnityContainer container,
            IWebContextAccessor webContextAccessor)
        {
            this.NameResolver = container.Resolve<ITestNameResolver>();

            var fileSourceSettings = await this.CreateDefaultFileSourceSettingsAsync(
                randomizeFileBasePath: true,
                useDatabaseAsDefault: this.UseDatabaseAsDefault);

            var serverCodeOverride = await TestHelper.GetServerCodeAsync(this, useRandomName: false);

            await TestHelper.InitializeServerContainerAsync(
                container,
                () => NotNullOrThrow(this.DbFactory).Create(),
                new DbScopeProxy(() => NotNullOrThrow(this.DbScope)),
                () => webContextAccessor.TryGetWebContext()?.TryGetSessionToken(),
                this.EnableInterprocessCommunication,
                fileSourceSettings,
                this.BeforeRegisterExtensionsOnServer,
                this.BeforeFinalizeServerRegistration,
                serverCodeOverride);

            container
                .RegisterServerTestDependencies(this.ResourceAssembly)
                .RegisterExternalObjects();

            // перерегистрируем менеджер на нужный, а также сохраняем идентичность ITestNameResolver в обоих контейнерах
            this.unityContainerForInitializeServerContainer
                .RegisterFactory<IExternalObjectManager>(
                    _ => container.Resolve<IExternalObjectManager>(),
                    new ContainerControlledLifetimeManager())
                .RegisterInstance(this.NameResolver);

            // Планирование удаления ключей из Redis.
            var obj = RedisExternalObjectHandler.CreateObjectInfo($"{serverCodeOverride}*", this.GetHashCode());

            var externalObjectManager = container.Resolve<IExternalObjectManager>();
            externalObjectManager.RegisterForFinalize(obj);
        }

        /// <summary>
        /// Выполняет действия перед поиском и выполнением серверных регистраторов расширений в папке приложения.
        /// </summary>
        /// <param name="unityContainer">Unity-контейнер.</param>
        protected virtual void BeforeRegisterExtensionsOnServer(IUnityContainer unityContainer)
        {
        }

        /// <summary>
        /// Выполняет действия перед завершением регистрации сервера приложений.
        /// </summary>
        /// <param name="unityContainer">Unity-контейнер.</param>
        protected virtual void BeforeFinalizeServerRegistration(IUnityContainer unityContainer)
        {
        }

        /// <summary>
        /// Выполняет создание и инициализацию экземпляра <see cref="IWebApplicationFactory"/>.
        /// </summary>
        /// <remarks>
        /// По умолчанию используется <see cref="TessaWebApplicationFactory"/>.
        /// </remarks>
        /// <returns>Экземпляр инициализированного <see cref="IWebApplicationFactory"/>.</returns>
        protected virtual async ValueTask<IWebApplicationFactory> InitializeWebApplicationFactoryAsync()
        {
            var factory = new TessaWebApplicationFactory(this.CreateContainerServerAsync);
            await factory.InitializeAndStartAsync();
            return factory;
        }

        #endregion

        #region Private Methods

        private void PlannedInitializeTestServer()
        {
            this.GetTestActions(ActionStage.BeforeInitialize).Add(
                new TestAction(
                    this,
                    static _ => initializeWebServerRegistrator.RegisterAsync()));

            this.GetTestActions(ActionStage.BeforeInitializeContainer).Add(
                new TestAction(
                    this,
                    static async sender =>
                    {
                        var senderT = (HybridClientTestBase) sender;

                        senderT.unityContainerForInitializeServerContainer = senderT.UnityContainer;
                        senderT.WebApplicationFactory = await senderT.InitializeWebApplicationFactoryAsync();
                    }));
        }

        #endregion
    }
}
