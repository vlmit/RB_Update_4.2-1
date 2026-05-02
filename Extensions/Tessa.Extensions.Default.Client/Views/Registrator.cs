using Tessa.Extensions.Default.Client.Workplaces;
using Tessa.Extensions.Default.Client.Workplaces.Manager;
using Tessa.Extensions.Default.Client.Workplaces.WebChart;
using Tessa.Platform;
using Tessa.UI.Views.Extensions;
using Unity;

namespace Tessa.Extensions.Default.Client.Views
{
    [Registrator]
    public sealed class Registrator : RegistratorBase
    {
        public override void RegisterUnity()
        {
            // Регистрация клиентских представлений в контейнере приложения должна осуществляется с уникальным именем
            // желательно совпадающим с алиасом представления в метаданных. В случае не уникальности имен
            // в контейнере и IViewService будет зарегистрировано представление последним осуществившее
            // регистрацию в контейнере. Регистрация в IViewService будет осуществлена по алиасу из метаданных представления

            this.UnityContainer
                .RegisterSingleton<IAdvancedFilterViewDialogManager, AdvancedFilterViewDialogManager>()
                .RegisterSingleton<IFilterViewDialogDescriptorRegistry, FilterViewDialogDescriptorRegistry>()
                ;
        }

        public override void FinalizeRegistration()
        {
            // типы могут быть не зарегистрированы в тестах или плагинах Chronos

            this.UnityContainer
                .RegisterSingleton<ImageCache>()
                .TryResolve<IWorkplaceExtensionRegistry>()
                ?
                .Register(typeof(AccessTokensContextMenuExtension))
                .RegisterConfiguratorType(
                    typeof(AccessTokensContextMenuExtension),
                    type => this.UnityContainer.Resolve<AccessTokensContextMenuExtensionConfigurator>())

                .Register(typeof(CreateCardExtension))
                .RegisterConfiguratorType(
                    typeof(CreateCardExtension),
                    type => this.UnityContainer.Resolve<CreateCardExtensionConfigurator>())
                
                .Register(typeof(CreateCardCopyExtension))
                .RegisterConfiguratorType(typeof(CreateCardCopyExtension),
                    type => this.UnityContainer.Resolve<CreateCardCopyExtensionConfigurator>())

                .Register(typeof(ViewsContextMenuExtension))
                .RegisterConfiguratorType(
                    typeof(ViewsContextMenuExtension),
                    type => this.UnityContainer.Resolve<ViewsContextMenuExtensionConfigurator>())

                .Register(typeof(AutomaticNodeRefreshExtension))
                .RegisterConfiguratorType(
                    typeof(AutomaticNodeRefreshExtension),
                    type => this.UnityContainer.Resolve<AutomaticNodeRefreshExtensionConfigurator>())

                .Register(typeof(ManagerWorkplaceExtension))
                .RegisterConfiguratorType(
                    typeof(ManagerWorkplaceExtension),
                    type => this.UnityContainer.Resolve<ManagerWorkplaceExtensionConfigurator>())

                .Register(typeof(WebChartWorkplaceExtension))
                .RegisterConfiguratorType(
                    typeof(WebChartWorkplaceExtension),
                    type => this.UnityContainer.Resolve<WebChartWorkplaceExtensionConfigurator>())

                .Register(typeof(InformationLabelViewExtension))
                .RegisterConfiguratorType(
                    typeof(InformationLabelViewExtension),
                    type => this.UnityContainer.Resolve<InformationLabelViewExtensionConfigurator>())
                
                .Register(typeof(RefSectionExtension))
                .RegisterConfiguratorType(
                    typeof(RefSectionExtension),
                    type => this.UnityContainer.Resolve<RefSectionExtensionConfigurator>())

                .Register(typeof(OverrideFilterViewExtension))
                .RegisterConfiguratorType(
                    typeof(OverrideFilterViewExtension),
                    type => this.UnityContainer.Resolve<OverrideFilterViewExtensionConfigurator>())

                .Register(typeof(AddTagButtonViewExtension))
                .RegisterConfiguratorType(
                    typeof(AddTagButtonViewExtension),
                    type => this.UnityContainer.Resolve<AddTagButtonViewExtensionConfigurator>())

                .Register(typeof(TagCardsViewExtension))
                .RegisterConfiguratorType(
                    typeof(TagCardsViewExtension),
                    type => this.UnityContainer.Resolve<TagCardsViewExtensionConfigurator>())

                .Register(typeof(HelpViewExtension))
                .RegisterConfiguratorType(
                    typeof(HelpViewExtension),
                    type => this.UnityContainer.Resolve<HelpViewExtensionConfigurator>())

                .Register(typeof(TagsInFirstColumnWorkplaceViewComponentExtension))
                .RegisterConfiguratorType(
                    typeof(TagsInFirstColumnWorkplaceViewComponentExtension),
                    type => this.UnityContainer.Resolve<TagsInFirstColumnWorkplaceViewComponentExtensionConfigurator>())
                ;
        }
    }
}
