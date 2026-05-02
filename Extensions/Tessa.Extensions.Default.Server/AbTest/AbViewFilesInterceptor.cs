#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.AbTest;
using Tessa.Scheme;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Server.AbTest
{
    public sealed class AbViewFilesInterceptor() : ViewInterceptorBase([AbViewAliases.AbViewFiles])
    {
        #region Base Overrides

        /// <inheritdoc />
        public override async ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default)
        {
            var view = this.GetInterceptedView(request.ViewAlias);

            var metadata = await view.GetMetadataAsync(cancellationToken);
            var interceptor = new ViewFilesInterceptor(metadata);

            return await interceptor.GetDataAsync(request, cancellationToken);
        }

        #endregion

        #region Nested Types

        private sealed class ViewFilesInterceptor(IViewMetadata metadata) : ITessaView
        {
            #region Constants

            private const string ByFolders = "ByFolder";

            private const string DefaultDirectory = @"c:\temp\1\";

            private const string DefaultFilter = "*.*";

            private const string Folder = "Folder";

            private const string NameParameter = "NAME";

            private const string ParentFolder = "ParentFolder";

            #endregion

            #region Private Methods

            /// <summary>
            /// Преобразует список файлов в результат выполнения представления.
            /// </summary>
            private static TessaViewResult GetResult(IEnumerable<string> files) =>
                new()
                {
                    Columns = { ("FileName", SchemeType.String), ("FullFileName", SchemeType.String) },
                    Rows = files.Select(file => new List<object?> { Path.GetFileName(file), file }).ToList()
                };

            /// <summary>
            /// Возвращает список файлов из каталога <paramref name="path"/> в соответствии с фильтром <paramref name="filter"/>.
            /// Если каталог не существует, будет возвращен пустой список.
            /// </summary>
            /// <param name="path">Путь к папке с файлами.</param>
            /// <param name="filter">Шаблон отбора имен файлов.</param>
            /// <param name="getFolders">Признак того, что требуется получить подпапки, а не файлы.</param>
            /// <returns>Список файлов или подпапок.</returns>
            private static string[] GetItems(string? path, string filter, bool getFolders) =>
                string.IsNullOrEmpty(path) || !Directory.Exists(path) ? [] : getFolders ? Directory.GetDirectories(path, filter) : Directory.GetFiles(path, filter);

            /// <summary>
            /// Возвращает список файлов в папке по умолчанию в соответствии с заданным фильтром
            /// в параметре <see cref="NameParameter"/>.
            /// </summary>
            private static TessaViewResult GetDefaultFolderResult(ITessaViewRequest request)
            {
                var filter = GetDirectoryFilter(request);
                var files = GetItems(DefaultDirectory, filter, false);
                return GetResult(files);
            }

            /// <summary>
            /// Возвращает шаблон отбора имён файлов.
            /// Если в запросе задано одно значение в параметре с именем <see cref="NameParameter"/>, то, независимо от заданного условия параметра,
            /// будет возвращен фильтр, указанный в данном параметре.
            /// Если параметр не задан, то возвращает фильтр по умолчанию <c>*.*</c>.
            /// </summary>
            /// <param name="request">Запрос к представлению.</param>
            /// <returns>Шаблон отбора имён файлов.</returns>
            private static string GetDirectoryFilter(ITessaViewRequest request)
            {
                if (request.Parameters.FindByName(NameParameter) is { } nameParam
                    && nameParam.CriteriaValues.FirstOrDefault() is { } criteria
                    && criteria.Values.FirstOrDefault() is { } value)
                {
                    var isContains = ParserNames.IsEquals(criteria.CriteriaName, CriteriaOperatorConst.Contains);
                    return value.IsNull() ? DefaultFilter : isContains ? $"{value}{DefaultFilter}" : value.ToString() is { Length: > 0 } filter ? filter : DefaultFilter;
                }

                return DefaultFilter;
            }

            /// <summary>
            /// Обрабатывает наличие параметра <see cref="Folder"/> на получение списка файлов указанной папки.
            /// </summary>
            private static TessaViewResult? GetFolderItems(ITessaViewRequest request)
            {
                if (request.Parameters.FindByName(Folder) is { } param)
                {
                    foreach (var criteria in param.CriteriaValues)
                    {
                        if (criteria.CriteriaName == CriteriaOperatorConst.EqualsTo)
                        {
                            var files = GetItems(
                                criteria.Values.FirstOrDefault()?.Value?.ToString(),
                                GetDirectoryFilter(request),
                                false);

                            return GetResult(files);
                        }
                    }
                }

                return null;
            }

            /// <summary>
            /// Обрабатывает наличие параметра <see cref="ParentFolder"/> на получение списка папок указанной папки.
            /// </summary>
            private static TessaViewResult? GetParentFolderItems(ITessaViewRequest request)
            {
                if (ParserNames.IsEquals(request.SubsetName, ByFolders)
                    && request.Parameters.FindByName(ParentFolder) is { } param)
                {
                    foreach (var criteria in param.CriteriaValues)
                    {
                        switch (criteria.CriteriaName)
                        {
                            case CriteriaOperatorConst.IsNull:
                                return new()
                                {
                                    Columns = { ("FileName", SchemeType.String), ("FullFileName", SchemeType.String) },
                                    Rows =
                                    {
                                        new() { "Root", DefaultDirectory }
                                    }
                                };

                            case CriteriaOperatorConst.EqualsTo:
                                var folders = GetItems(
                                    criteria.Values.FirstOrDefault()?.Value?.ToString(),
                                    GetDirectoryFilter(request),
                                    true);

                                return GetResult(folders);
                        }
                    }
                }

                return null;
            }

            #endregion

            #region ITessaView Members

            /// <inheritdoc/>
            public string Alias => metadata.Alias;

            /// <inheritdoc/>
            public ValueTask<IViewMetadata> GetMetadataAsync(CancellationToken cancellationToken = default) =>
                new(metadata);

            /// <inheritdoc/>
            public ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default) =>
                new(GetParentFolderItems(request) ?? GetFolderItems(request) ?? GetDefaultFolderResult(request));

            #endregion
        }

        #endregion
    }
}
