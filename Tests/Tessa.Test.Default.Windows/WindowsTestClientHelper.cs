using System;
using System.Collections.Generic;
using System.Linq;
using Tessa.Cards.Caching;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Test.Default.Shared.Views;
using Tessa.UI.Controls;
using Tessa.UI.Views;
using Tessa.UI.Views.Extensions;
using Tessa.UI.Views.Filtering;
using Tessa.UI.Views.MessagingServices;
using Tessa.UI.Views.Workplaces;
using Tessa.UI.Views.Workplaces.Tree;
using Tessa.Views;
using Tessa.Views.Json;
using Tessa.Views.Json.Converters;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Types;
using Tessa.Views.Parser;
using Tessa.Views.SearchQueries;
using Tessa.Views.Workplaces;
using Unity;
using Unity.Lifetime;
using Unity.Resolution;

namespace Tessa.Test.Default.Windows
{
    /// <summary>
    /// Предоставляет вспомогательные методы, используемые в клиентских тестах с поддержкой пользовательского интерфейса.
    /// </summary>
    public static class WindowsTestClientHelper
    {
        #region Static Methods

        /// <summary>
        /// Регистрирует зависимости для работы с представлениями в указанном контейнере.
        /// </summary>
        /// <param name="unityContainer">Контейнер в котором должны быть зарегистрированы зависимости.</param>
        /// <returns>Контейнер, указанный в <paramref name="unityContainer"/> для создания цепочки вызовов.</returns>
        public static IUnityContainer RegisterViews(this IUnityContainer unityContainer)
        {
            ThrowIfNull(unityContainer);

            ViewValidationKeys.Register();
            var mediator = new Mediator();
            var repository = new TestViewRepository(mediator, Enumerable.Empty<TessaViewModel>);

            SyntaxTreeRegistration.Register(unityContainer);
            TypeConverterRegistration.Register(unityContainer);

            unityContainer
                .RegisterWorkplaces()
                .RegisterDbScope()
                .RegisterViewsDefaults()
                .RegisterFactory<IEnumerable<ITessaView>>(c => c.ResolveAll<ITessaView>())
                .RegisterFactory<IViewServiceImplementer>(
                    c => new TestViewServiceImplementer(
                        mediator,
                        repository,
                        c.Resolve<ISession>(),
                        c.Resolve<IQueryGeneratorFactory>(),
                        c.Resolve<CreateViewMetadataEvaluationContextFunc>(),
                        c.Resolve<ResolveNormalizeParameterNameFunc>(),
                        c.Resolve<IDbScope>(),
                        c.Resolve<IJsonViewMetadataConverter<IJsonViewMetadata, IViewMetadata>>(),
                        c.Resolve<IErrorManager>(),
                        c.Resolve<ICardCache>()
                    ),
                    new ContainerControlledLifetimeManager())
                .RegisterInstance<IViewRepository>(repository)
                .RegisterSingleton<IViewService, ViewService>()
                .RegisterSingleton<ISearchQueryService, SearchQueryServiceClient>()
                ;

            ViewMetadataConverterRegistration.Register(unityContainer);
            TypeConverterRegistration.Register(unityContainer);
            TreeItemLoaderRegistration.Register(unityContainer);
            FilterRegistration.Register(unityContainer);

            unityContainer
                .RegisterType<WorkplaceView>(new PerResolveLifetimeManager())
                .RegisterFactory<Func<WorkplaceMetadata, WorkplaceView>>(
                    c => new Func<WorkplaceMetadata, WorkplaceView>(
                        metadata => c.Resolve<WorkplaceView>(new ParameterOverride("metadata", metadata))),
                    new ContainerControlledLifetimeManager())
                .RegisterType<IWorkplaceViewModel, WorkplaceViewModel>(new PerResolveLifetimeManager())
                .RegisterType<IOpenedCardObserver, OpenedCardObserver>(new ContainerControlledLifetimeManager())
                .RegisterType<IWorkplaceService, WorkplaceService>(new ContainerControlledLifetimeManager())
                .RegisterType<IWorkplaceCreationContext, WorkplaceCreationContext>(new PerResolveLifetimeManager())
                .RegisterType<ITreeItemFactory, TreeItemFactory>(new ContainerControlledLifetimeManager())
                .RegisterFactory<Func<IWorkplaceMetadata, string, IDoubleClickAction, IEnumerable<RequestParameter>, IWorkplaceCreationContext>>(
                    c => new Func<IWorkplaceMetadata, string, IDoubleClickAction, IEnumerable<RequestParameter>, IWorkplaceCreationContext>(
                        (metadata, refSection, doubleClickAction, extraParameters) =>
                            c.Resolve<IWorkplaceCreationContext>(
                                new ParameterOverride("metadata", metadata),
                                new ParameterOverride("refSection", refSection),
                                new ParameterOverride("doubleClickAction", doubleClickAction),
                                new ParameterOverride("extraParameters", extraParameters ?? Enumerable.Empty<RequestParameter>()))),
                    new ContainerControlledLifetimeManager())
                .RegisterType<IContentProviderStorage, ContentProviderStorage>(new ContainerControlledLifetimeManager())
                .RegisterWorkplaceHandlers()
                .RegisterFactory<WorkplaceViewFactory>(
                    c => new WorkplaceViewFactory(
                        model => c.Resolve<WorkplaceView>(new ParameterOverride("viewModel", model))),
                    new ContainerControlledLifetimeManager())
                .RegisterType<IDoubleClickAction, DefaultViewDoubleClickAction>()
                .RegisterFactory<RequestFactory>(
                    c => new RequestFactory(
                        metadata => c.Resolve<ITessaViewRequest>(new ParameterOverride("viewAlias", metadata.Alias))),
                    new ContainerControlledLifetimeManager())
                .RegisterType<IDialogService, DialogService>(new ContainerControlledLifetimeManager())
                .RegisterFactory<Func<Action<ISearchQueryViewModel>, ISearchQueriesViewModel>>(
                    c => new Func<Action<ISearchQueryViewModel>, ISearchQueriesViewModel>(
                        doubleClickAction => c.Resolve<SearchQueriesViewModel>(
                            new ParameterOverride("doubleClickAction", doubleClickAction))),
                    new ContainerControlledLifetimeManager())
                .RegisterFactory<Func<Action<ISearchQueryViewModel>, ISearchQueryDialogViewModel>>(
                    c => new Func<Action<ISearchQueryViewModel>, ISearchQueryDialogViewModel>(
                        doubleClickAction => c.Resolve<SearchQueryDialogViewModel>(
                            new ParameterOverride("doubleClickAction", doubleClickAction))),
                    new ContainerControlledLifetimeManager())
                .RegisterType<ISearchQueryManageDialogViewModel, SearchQueryManageDialogViewModel>()
                ;

            unityContainer.RegisterWorkplaceExtensions();

            return unityContainer;
        }

        #endregion
    }
}
