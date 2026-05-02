using System;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.Runtime;
using Tessa.Test.Default.Shared.Cards;
using Tessa.Test.Default.Shared.GC;
using Tessa.Test.Default.Shared.GC.Handlers;
using Unity;

namespace Tessa.Test.Default.Shared
{
    /// <summary>
    /// Базовый абстрактный класс для серверных тестов.
    /// </summary>
    public abstract class ServerTestBase :
        TestBase,
        ICardTypeRepositoryContainer
    {
        #region Properties

        /// <summary>
        /// Возвращает значение, показывающее, необходимо ли в качестве источника файлов по умолчанию использовать базу данных или нет.
        /// </summary>
        public virtual bool UseDatabaseAsDefault => false;

        /// <summary>
        /// Возвращает значение, показывающее, необходимо ли использовать коммуникация между процессами или нет.
        /// </summary>
        public virtual bool EnableInterprocessCommunication => false;

        #region ICardTypeRepositoryContainer Members

        /// <inheritdoc/>
        public ICardTypeServerRepository CardTypeRepository { get; set; }

        #endregion

        #endregion

        #region Base override

        /// <inheritdoc/>
        protected override async ValueTask InitializeContainerAsync(IUnityContainer container)
        {
            await base.InitializeContainerAsync(container);

            var fileSourceSettings = await this.CreateDefaultFileSourceSettingsAsync(
                randomizeFileBasePath: true,
                useDatabaseAsDefault: this.UseDatabaseAsDefault);

            var token = Tessa.Platform.Runtime.Session.CreateSystemToken();

            var serverCodeOverride = await TestHelper.GetServerCodeAsync(this);

            // Планирование удаления ключей из Redis.
            var obj = RedisExternalObjectHandler.CreateObjectInfo(
                $"{serverCodeOverride}*",
                this.GetHashCode());

            var externalObjectManager = container.Resolve<IExternalObjectManager>();
            externalObjectManager.RegisterForFinalize(obj);

            await TestHelper.InitializeServerContainerAsync(
                container,
                this.DbFactory is null ? null : this.DbFactory.Create,
                this.DbScope,
                () => token,
                this.EnableInterprocessCommunication,
                fileSourceSettings,
                this.BeforeRegisterExtensionsOnServer,
                this.BeforeFinalizeServerRegistration,
                serverCodeOverride: serverCodeOverride);

            container
                .RegisterServerTestDependencies(this.ResourceAssembly);
        }

        /// <inheritdoc/>
        protected override void SetServerUtcNow(DateTime? utcNow) =>
            this.UnityContainer
                .Resolve<MutableClock>()
                .MutableUtcNow = utcNow;

        #endregion

        #region Protected Methods

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

        #endregion
    }
}
