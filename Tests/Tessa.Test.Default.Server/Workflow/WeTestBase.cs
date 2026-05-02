#nullable enable

using System;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Test.Default.Shared;
using Tessa.Test.Default.Shared.Kr;
using Tessa.Test.Default.Shared.Workflow;
using Unity;
using Unity.Lifetime;

namespace Tessa.Test.Default.Server.Workflow
{
    /// <summary>
    /// Абстрактный базовый класс для тестов WorkflowEngine.
    /// </summary>
    public abstract class WeTestBase :
        KrServerTestBase
    {
        #region Constants

        /// <summary>
        /// Идентификатор типа карточки шаблона бизнес-процесса.
        /// </summary>
        protected static readonly Guid ProcessTemplateType = CardHelper.BusinessProcessTemplateTypeID;

        /// <summary>
        /// Имя типа карточки шаблона бизнес-процесса.
        /// </summary>
        protected const string ProcessTemplateTypeName = CardHelper.BusinessProcessTemplateTypeName;

        #endregion

        #region Fields

        private IWeLifecycleCompanionDependencies? weDependencies;

        #endregion

        #region Properties

        /// <inheritdoc cref="IWeLifecycleCompanionDependencies" path="/summary"/>
        protected IWeLifecycleCompanionDependencies WeDependencies => this.weDependencies ??= this.UnityContainer.Resolve<IWeLifecycleCompanionDependencies>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Создаёт новый экземпляр класса <see cref="WeProcessTemplateBuilder"/>.
        /// </summary>
        /// <param name="id">Идентификатор карточки или значение <see langword="null"/>, если он должен быть создан автоматически.</param>
        /// <returns>Новый экземпляр класса <see cref="WeProcessTemplateBuilder"/>.</returns>
        public WeProcessTemplateBuilder CreateWeProcessTemplateBuilder(
            Guid? id = null) =>
            new WeProcessTemplateBuilder(
                id ?? Guid.NewGuid(),
                this.CardLifecycleDependencies,
                this.WeDependencies);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask InitializeContainerAsync(
            IUnityContainer container)
        {
            await base.InitializeContainerAsync(container);

            container.RegisterType<IWeLifecycleCompanionDependencies, WeLifecycleCompanionDependencies>(new ContainerControlledLifetimeManager());
        }

        /// <inheritdoc/>
        protected override async Task InitializeCoreAsync()
        {
            await base.InitializeCoreAsync();

            await this.TestConfigurationBuilder
                .GetPermissionsConfigurator()
                .GetPermissionsCard(Guid.NewGuid())
                .AddFlags(KrPermissionFlagDescriptors.Full)
                .AddRole(this.Session.User.ID)
                .ModifyStates(static _ => KrState.DefaultStates)
                .AddType(this.TestDocTypeID)
                .Complete()
                .GoAsync()
                ;
        }

        #endregion
    }
}
