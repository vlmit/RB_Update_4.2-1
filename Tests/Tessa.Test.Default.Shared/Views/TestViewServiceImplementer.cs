#nullable enable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards.Caching;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Test.Default.Shared.Roles;
using Tessa.Views;
using Tessa.Views.Json;
using Tessa.Views.Json.Converters;
using Tessa.Views.Metadata;
using Tessa.Views.Parser;

namespace Tessa.Test.Default.Shared.Views
{
    /// <summary>
    /// Тестовая реализация сервиса представлений.
    /// </summary>
    public sealed class TestViewServiceImplementer : IViewServiceImplementer
    {
        #region Constructors

        public TestViewServiceImplementer(
            IOneWayMediatorClient mediator,
            IViewRepository repository,
            ISession session,
            IQueryGeneratorFactory queryGeneratorFactory,
            CreateViewMetadataEvaluationContextFunc createEvaluationContextFunc,
            ResolveNormalizeParameterNameFunc resolveNormalizeParameterNameFunc,
            IDbScope dbScope,
            IJsonViewMetadataConverter<IJsonViewMetadata, IViewMetadata> jsonMetadataConverter,
            IErrorManager errorManager,
            ICardCache cardCache)
        {
            ThrowIfNull(mediator);

            this.repository = NotNullOrThrow(repository);
            this.session = NotNullOrThrow(session);
            this.queryGeneratorFactory = NotNullOrThrow(queryGeneratorFactory);
            this.createEvaluationContextFunc = NotNullOrThrow(createEvaluationContextFunc);
            this.resolveNormalizeParameterNameFunc = NotNullOrThrow(resolveNormalizeParameterNameFunc);
            this.dbScope = NotNullOrThrow(dbScope);
            this.jsonMetadataConverter = NotNullOrThrow(jsonMetadataConverter);
            this.errorManager = NotNullOrThrow(errorManager);
            this.cardCache = NotNullOrThrow(cardCache);

            mediator.RegisterCallback(() => this.allViews = null);
        }

        #endregion

        #region Fields

        private readonly IViewRepository repository;

        private readonly ISession session;

        private readonly IQueryGeneratorFactory queryGeneratorFactory;

        private readonly CreateViewMetadataEvaluationContextFunc createEvaluationContextFunc;

        private readonly ResolveNormalizeParameterNameFunc resolveNormalizeParameterNameFunc;

        private readonly IDbScope dbScope;

        private readonly IJsonViewMetadataConverter<IJsonViewMetadata, IViewMetadata> jsonMetadataConverter;

        private readonly IErrorManager errorManager;

        private readonly ICardCache cardCache;

        private volatile ReadOnlyCollection<ITessaView>? allViews;

        #endregion

        #region Private Methods

        private async ValueTask<ReadOnlyCollection<ITessaView>> GetAllViewsCoreAsync(CancellationToken cancellationToken = default)
        {
            var allViews = this.allViews;
            if (allViews is not null)
            {
                return allViews;
            }

            var newValue = await this.InitializeAsync(cancellationToken);
            return Interlocked.CompareExchange(ref this.allViews, newValue, null) ?? newValue;
        }

        private async Task<ReadOnlyCollection<ITessaView>> InitializeAsync(CancellationToken cancellationToken = default) =>
            (await this.repository.GetAsync(new ViewGetRequest(), cancellationToken))
            .Select(ITessaView (model) =>
                new TessaViewModelAdapter(
                    new(Dbms.SqlServer, RuntimeHelper.ZeroVersion),
                    model,
                    new TessaViewModelAdapterDependencies(
                        new TestQueryExecutor(),
                        this.session,
                        this.queryGeneratorFactory,
                        this.createEvaluationContextFunc,
                        this.dbScope,
                        this.resolveNormalizeParameterNameFunc,
                        new DefaultViewGetDataExecutor(),
                        this.jsonMetadataConverter,
                        new TestDeputiesManagementSettingsProvider(),
                        this.errorManager,
                        this.cardCache)
                )
            )
            .ToList()
            .AsReadOnly();

        #endregion

        #region IViewServiceImplementer Members

        /// <inheritdoc/>
        public async ValueTask<IReadOnlyList<ITessaView>> GetAllViewsAsync(CancellationToken cancellationToken = default) =>
            await this.GetAllViewsCoreAsync(cancellationToken);

        /// <inheritdoc/>
        public async ValueTask<ITessaView?> GetByNameAsync(string viewName, CancellationToken cancellationToken = default)
        {
            foreach (var view in await this.GetAllViewsCoreAsync(cancellationToken))
            {
                if (ParserNames.IsEquals(view.Alias, viewName))
                {
                    return view;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public async ValueTask<IReadOnlyList<ITessaView>> GetByNamesAsync(IEnumerable<string> viewsNames, CancellationToken cancellationToken = default)
        {
            if (viewsNames.TryGetNonEnumeratedCount(out var count) && count == 0)
            {
                return [];
            }

            var viewsNamesArray = viewsNames.AsReadOnlyCollection();
            if (viewsNamesArray.Count == 0)
            {
                return [];
            }

            var result = new List<ITessaView>(viewsNamesArray.Count);
            foreach (var view in await this.GetAllViewsCoreAsync(cancellationToken))
            {
                if (ParserNames.Contains(view.Alias, viewsNamesArray))
                {
                    result.Add(view);
                }
            }

            return result;
        }

        #endregion
    }
}
