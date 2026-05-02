using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tessa.Platform;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.SearchQueries;
using Tessa.Views.Workplaces;

namespace Tessa.Test.Default.Shared.Views
{
    /// <summary>
    /// Помощник для тестирование пользовательских расширений
    /// </summary>
    public static class UserExtensionTestHelper
    {
        #region Nested Types

        /// <summary>
        /// Поставщик порядковых номеров.
        /// </summary>
        private static class OrderPosProvider
        {
            private static int orderPos;

            /// <summary>
            /// Возвращает следующий порядковый номер.
            /// </summary>
            /// <returns>Порядковый номер.</returns>
            public static int GetNextOrderPos() => ++orderPos;
        }

        private sealed class TestSearchQueryService : ISearchQueryService
        {
            #region Fields

            private readonly Dictionary<Guid, ISearchQueryMetadata> searchQueries;

            #endregion

            #region Constructors and Destructors

            public TestSearchQueryService()
            {
                this.searchQueries = new();
                var firstSearchQuery = new SearchQueryMetadata
                {
                    Alias = "Тестовый сохранённый запрос номер 1",
                    ID = firstTestSearchQueryId,
                    IsPublic = true,
                    CreatedByUserID = Guid.Empty,
                    ModificationDateTime = DateTime.Now,
                    ViewAlias = TestViewName
                };
                this.searchQueries[firstSearchQuery.ID] = firstSearchQuery;

                var secondSearchQuery = new SearchQueryMetadata
                {
                    Alias = "Тестовый сохранённый запрос номер 2",
                    ID = secondTestSearchQueryId,
                    IsPublic = false,
                    CreatedByUserID = GetUserId(),
                    ModificationDateTime = DateTime.Now,
                    ViewAlias = TestViewName
                };
                this.searchQueries[secondSearchQuery.ID] = secondSearchQuery;
            }

            #endregion

            #region ISearchQueryService Members

            /// <inheritdoc/>
            public ValueTask<IReadOnlyList<ISearchQueryMetadata>> GetUserAvailableAsync(CancellationToken cancellationToken = default) =>
                new(this.searchQueries.Values.ToArray());

            /// <inheritdoc/>
            public ValueTask<IReadOnlyList<ISearchQueryMetadata>> GetPublicAsync(CancellationToken cancellationToken = default) =>
                new(this.searchQueries.Values.Where(x => x.IsPublic).ToArray());

            /// <inheritdoc/>
            public ValueTask<ISearchQueryMetadata> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
                new(this.searchQueries.GetValueOrDefault(id));

            /// <inheritdoc/>
            public Task SaveAsync(ISearchQueryMetadata metadata, CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();

            /// <inheritdoc/>
            public Task DeleteAsync(IReadOnlyCollection<Guid> queries, CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();

            /// <inheritdoc/>
            public Task ImportAsync(IReadOnlyCollection<ISearchQueryMetadata> searchQueries, CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();

            /// <inheritdoc/>
            public Task InvalidateCacheAsync() =>
                throw new NotSupportedException();

            #endregion
        }

        private sealed class TestView : ITessaView
        {
            #region Properties

            public IViewMetadata Metadata { get; } = new ViewMetadata { Alias = TestViewName, Caption = "Test view" };

            #endregion

            #region Public Methods and Operators

            /// <inheritdoc/>
            public string Alias => this.Metadata.Alias;

            /// <inheritdoc />
            public ValueTask<IViewMetadata> GetMetadataAsync(CancellationToken cancellationToken = default) =>
                new(this.Metadata);

            /// <inheritdoc/>
            public ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default) =>
                new(new TessaViewResult(this.Metadata));

            #endregion
        }

        private sealed class TestViewService : IViewService
        {
            #region Fields

            private readonly Dictionary<string, ITessaView> views;

            #endregion

            #region Constructors and Destructors

            public TestViewService()
            {
                this.views = new(StringComparer.OrdinalIgnoreCase);
                var view = new TestView();
                this.views[view.Metadata.Alias] = view;
            }

            #endregion

            #region IViewService Members

            /// <inheritdoc />
            public ValueTask<IReadOnlyList<ITessaView>> GetAllViewsAsync(CancellationToken cancellationToken = default) =>
                new(this.views.Values.ToArray());

            /// <inheritdoc />
            public ValueTask<ITessaView> GetByNameAsync(string viewName, CancellationToken cancellationToken = default) =>
                new(this.views.TryGetValue(viewName, out var view) ? view : null);

            /// <inheritdoc />
            public ValueTask<IReadOnlyList<ITessaView>> GetByNamesAsync(IEnumerable<string> viewsNames, CancellationToken cancellationToken = default)
            {
                var result = new List<ITessaView>();
                foreach (var name in viewsNames)
                {
                    if (this.views.TryGetValue(name, out var view))
                    {
                        result.Add(view);
                    }
                }

                return new(result);
            }

            /// <inheritdoc/>
            public ValueTask<IReadOnlyList<ITessaView>> GetByReferencesAsync(IEnumerable<string> refSection, CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();

            #endregion
        }

        #endregion

        #region Constants

        /// <summary>
        /// The test view name.
        /// </summary>
        private const string TestViewName = "TestView";

        #endregion

        #region Static Fields

        /// <summary>
        /// The first test search query id.
        /// </summary>
        private static readonly Guid
            firstTestSearchQueryId = new(0x0A9769F8, 0xE718, 0x4012, 0xBD, 0x4F, 0x0C, 0x35, 0x2A, 0x6E, 0xCA, 0x53); // 0A9769F8-E718-4012-BD4F-0C352A6ECA53

        /// <summary>
        /// The second test search query id.
        /// </summary>
        private static readonly Guid
            secondTestSearchQueryId = new(0x235CE789, 0x6A66, 0x4822, 0xAD, 0xAB, 0xC6, 0x47, 0x8F, 0x4D, 0x8A, 0xA4); // 235CE789-6A66-4822-ADAB-C6478F4D8AA4

        /// <summary>
        /// The test search query service.
        /// </summary>
        private static readonly ISearchQueryService testSearchQueryService = new TestSearchQueryService();

        /// <summary>
        /// The test user id.
        /// </summary>
        private static readonly Guid testUserId = new(0x915E9279, 0x99C6, 0x4A48, 0xB4, 0xB9, 0xCF, 0x17, 0x08, 0x16, 0x32, 0x30); // 915E9279-99C6-4A48-B4B9-CF1708163230

        /// <summary>
        /// The test view service.
        /// </summary>
        private static readonly IViewService testViewService = new TestViewService();

        /// <summary>
        /// The test workplace id.
        /// </summary>
        private static readonly Guid testWorkplaceId = new(0xC9EB9065, 0xC061, 0x4A03, 0xBE, 0x3B, 0x1E, 0xD0, 0xFA, 0x20, 0x9F, 0x48); // C9EB9065-C061-4A03-BE3B-1ED0FA209F48

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Возвращает тестовые метаданные пользовательских расширений
        /// </summary>
        /// <returns>
        /// Метаданные пользовательских расширений
        /// </returns>
        public static async Task<IWorkplaceUserExtensionMetadata> GetTestMetadataAsync()
        {
            var extension = new WorkplaceUserExtensionMetadata
            {
                CompositionId = Guid.NewGuid(),
                IsOwnedByUser = true,
                Alias = "Тестовый пользователь"
            };
            var folder = new FolderNodeMetadata
            {
                Alias = "Узел, добавленный в корень",
                CompositionId = Guid.NewGuid(),
                ParentCompositionId = GetTestWorkplaceId(),
                OrderPos = OrderPosProvider.GetNextOrderPos(),
                IsOwnedByUser = true,
                ShowMode = ShowMode.Always
            };

            var childFolder = new FolderNodeMetadata
            {
                Alias = "Дочерний узел",
                CompositionId = Guid.NewGuid(),
                ParentCompositionId = folder.CompositionId,
                OrderPos = OrderPosProvider.GetNextOrderPos(),
                IsOwnedByUser = true,
                ShowMode = ShowMode.Always
            };

            var firstSearchQuery = new WorkplaceSearchQueryMetadata
            {
                Alias = "Поисковый запрос 1",
                CompositionId = Guid.NewGuid(),
                Caption = "Поисковый запрос 1",
                OrderPos = OrderPosProvider.GetNextOrderPos(),
                ParentCompositionId = childFolder.CompositionId,
                IsOwnedByUser = true,
                SearchQueryId = firstTestSearchQueryId,
                ShowMode = ShowMode.Always,
                Metadata =
                    await GetTestSearchQueryService()
                        .GetByIdAsync(firstTestSearchQueryId)
            };
            childFolder.AddMetadata(firstSearchQuery);

            folder.AddMetadata(childFolder);

            var otherRootFolder = new FolderNodeMetadata
            {
                Alias = "Другой узел, добавленный в корень",
                CompositionId = Guid.NewGuid(),
                ParentCompositionId = GetTestWorkplaceId(),
                OrderPos = OrderPosProvider.GetNextOrderPos(),
                IsOwnedByUser = true,
                ShowMode = ShowMode.Always
            };

            var secondSearchQuery = new WorkplaceSearchQueryMetadata
            {
                Alias = "Поисковый запрос 2",
                CompositionId = Guid.NewGuid(),
                Caption = "Поисковый запрос 2",
                OrderPos = OrderPosProvider.GetNextOrderPos(),
                ParentCompositionId =
                    otherRootFolder.CompositionId,
                IsOwnedByUser = true,
                SearchQueryId = secondTestSearchQueryId,
                ShowMode = ShowMode.Always,
                Metadata =
                    await GetTestSearchQueryService()
                        .GetByIdAsync(secondTestSearchQueryId)
            };

            otherRootFolder.AddMetadata(secondSearchQuery);
            extension.AddMetadata(folder);
            extension.AddMetadata(otherRootFolder);
            return extension;
        }

        public static void AssertTestMetadata(IWorkplaceUserExtensionMetadata metadata)
        {
            var items = metadata.Items.ToArray();
            Assert.That(items, Has.Length.EqualTo(2));

            var firstItem = items.FirstOrDefault(x => x.Alias.Equals("Узел, добавленный в корень", StringComparison.Ordinal)) as FolderNodeMetadata;
            Assert.That(firstItem, Is.Not.Null);
            Assert.That(firstItem.ShowMode, Is.EqualTo(ShowMode.Always));
            Assert.That(firstItem.IsOwnedByUser, Is.EqualTo(true));

            var firstItemsChildren = firstItem.Items.ToArray();
            Assert.That(firstItemsChildren, Has.Length.EqualTo(1));

            var firstItemChild = firstItemsChildren.FirstOrDefault(x => x.Alias.Equals("Дочерний узел", StringComparison.Ordinal)) as FolderNodeMetadata;
            Assert.That(firstItemChild, Is.Not.Null);
            Assert.That(firstItemChild.ShowMode, Is.EqualTo(ShowMode.Always));
            Assert.That(firstItemChild.IsOwnedByUser, Is.EqualTo(true));
            Assert.That(firstItemChild.Items.Count, Is.EqualTo(1));

            var firstItemChildSearchQuery = firstItemChild.Items
                .FirstOrDefault(x => x.Alias.Equals("Тестовый сохранённый запрос номер 1", StringComparison.Ordinal)) as WorkplaceSearchQueryMetadata;
            Assert.That(firstItemChildSearchQuery, Is.Not.Null);
            Assert.That(firstItemChildSearchQuery.IsOwnedByUser, Is.EqualTo(true));

            var secondItem = items.FirstOrDefault(x => x.Alias.Equals("Другой узел, добавленный в корень", StringComparison.Ordinal)) as FolderNodeMetadata;
            Assert.That(secondItem, Is.Not.Null);
            Assert.That(secondItem.ShowMode, Is.EqualTo(ShowMode.Always));
            Assert.That(secondItem.IsOwnedByUser, Is.EqualTo(true));

            var secondItemsChildren = secondItem.Items.ToArray();
            Assert.That(secondItemsChildren, Has.Length.EqualTo(1));

            var secondItemSearchQuery = secondItemsChildren
                .FirstOrDefault(x => x.Alias.Equals("Тестовый сохранённый запрос номер 2", StringComparison.Ordinal)) as WorkplaceSearchQueryMetadata;
            Assert.That(secondItemSearchQuery, Is.Not.Null);
            Assert.That(secondItemSearchQuery.IsOwnedByUser, Is.EqualTo(true));
        }

        /// <summary>
        /// Возвращает тестовый сервис поисковых запросов
        /// </summary>
        /// <returns>
        /// The <see cref="ISearchQueryService"/>.
        /// </returns>
        public static ISearchQueryService GetTestSearchQueryService() => testSearchQueryService;

        /// <summary>
        /// Возвращает тестовый сервис представлений
        /// </summary>
        /// <returns>
        /// The <see cref="IViewService"/>.
        /// </returns>
        public static IViewService GetTestViewService() => testViewService;

        /// <summary>
        /// Возвращает идентификатор тестового рабочего места
        /// </summary>
        /// <returns>Идентификатор тестового рабочего места</returns>
        public static Guid GetTestWorkplaceId() => testWorkplaceId;

        /// <summary>
        /// Возвращает текстовое представление тестовых метаданных загруженных из файла расположенного в встроенных ресурсах указанной сборки.
        /// </summary>
        /// <param name="assembly">Сборка, содержащая ресурсы.</param>
        /// <returns>Текстовое представление метаданных.</returns>
        public static string GetWorkplaceUserExtensionText(Assembly assembly) => AssemblyHelper.GetResourceTextFile(assembly, @"Resources/Views/TestUserExtensions.txt");


        /// <summary>
        /// Возвращает идентификатор тестового пользователя
        /// </summary>
        /// <returns>Идентификатор тестового пользователя</returns>
        public static Guid GetUserId() => testUserId;

        #endregion
    }
}
