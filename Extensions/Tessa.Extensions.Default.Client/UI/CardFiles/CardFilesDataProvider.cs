#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Cards;
using Tessa.Extensions.Default.Shared.Views;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Scheme;
using Tessa.UI;
using Tessa.UI.Cards.Controls;
using Tessa.UI.Files;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Client.UI.CardFiles
{
    /// <summary>
    /// File data provider.
    /// </summary>
    /// <param name="viewMetadata"><inheritdoc cref="ViewMetadata" path="/summary"/></param>
    /// <param name="fileControl"><inheritdoc cref="FileControl" path="/summary"/></param>
    public class CardFilesDataProvider(IViewMetadata viewMetadata, IFileControl fileControl) : IDataProvider
    {
        #region Nested Types

        /// <summary>
        /// Column description for sorting.
        /// </summary>
        /// <param name="Alias">Column name.</param>
        /// <param name="Descending">Sorting by descending.</param>
        protected readonly record struct SortingColumn(string Alias, bool Descending);

        /// <summary>
        /// File sorter.
        /// </summary>
        /// <param name="dataProvider">Data provider.</param>
        /// <param name="request">Data request.</param>
        protected class FileSorter(CardFilesDataProvider dataProvider, IGetDataRequest request)
            : IAsyncInitializable, IComparer<Dictionary<string, object?>>
        {
            #region Properties

            /// <summary>
            /// Columns for sorting data.
            /// </summary>
            public List<SortingColumn> SortingColumns { get; } = [];

            /// <summary>
            /// Data provider.
            /// </summary>
            protected CardFilesDataProvider DataProvider { get; } = NotNullOrThrow(dataProvider);

            /// <summary>
            /// Data request.
            /// </summary>
            protected IGetDataRequest Request { get; } = NotNullOrThrow(request);

            #endregion

            #region IAsyncInitializable Members

            /// <inheritdoc />
            public virtual ValueTask InitializeAsync(CancellationToken cancellationToken = default)
            {
                if (this.Request.SortingColumns is { } sortingColumns)
                {
                    foreach (var column in sortingColumns)
                    {
                        this.SortingColumns.Add(
                            new(NotNullOrThrow(this.DataProvider.ViewMetadata.Columns.FindByName(column.Alias)?.SortBy),
                                column.Descending));
                    }
                }

                return ValueTask.CompletedTask;
            }

            #endregion

            #region IComparer Members

            /// <inheritdoc />
            public virtual int Compare(Dictionary<string, object?>? lhv, Dictionary<string, object?>? rhv)
            {
                if (lhv is null && rhv is null)
                {
                    return 0;
                }

                if (lhv is null)
                {
                    return -1;
                }

                if (rhv is null)
                {
                    return 1;
                }

                var lvm = NotNullOrThrow(lhv.Get<IFileViewModel>(ColumnsConst.FileViewModel));
                var rvm = NotNullOrThrow(rhv.Get<IFileViewModel>(ColumnsConst.FileViewModel));

                // оригинал всегда "выше" чем копия файла независимо от направления сортировки
                if (lvm.Model.Origin == rvm.Model)
                {
                    return 1;
                }

                if (rvm.Model.Origin == lvm.Model)
                {
                    return -1;
                }

                foreach (var sortingColumn in this.SortingColumns)
                {
                    var comparison = UIHelper.CurrentUICultureStringComparer.Compare(lhv.Get<object>(sortingColumn.Alias), rhv.Get<object>(sortingColumn.Alias));
                    if (comparison == 0)
                    {
                        continue;
                    }

                    if (sortingColumn.Descending)
                    {
                        comparison = -comparison;
                    }

                    return comparison;
                }

                return 0;
            }

            #endregion
        }

        /// <summary>
        /// Parameter filter criteria joined by logical OR (for single parameter).
        /// </summary>
        /// <param name="CriteriaFilters">Filters for given parameter.</param>
        protected sealed record ParameterFilterItem(List<Func<IFileViewModel, bool>> CriteriaFilters)
        {
            /// <summary>
            /// Creates filter for single parameter joined by logical OR from given criteria.
            /// </summary>
            /// <param name="criteriaFilters">Filtering criteria.</param>
            public ParameterFilterItem(params Func<IFileViewModel, bool>[] criteriaFilters)
                : this(new List<Func<IFileViewModel, bool>>(criteriaFilters))
            {
            }
        }

        /// <summary>
        /// Filter for files in view.
        /// </summary>
        /// <param name="dataProvider">Data provider.</param>
        /// <param name="request">Data request.</param>
        protected class FileFilter(
            CardFilesDataProvider dataProvider,
            IGetDataRequest request)
            : IAsyncInitializable
        {
            #region Properties

            /// <summary>
            /// Filtering parameters. One item for one parameter joined by logical AND.
            /// </summary>
            public List<ParameterFilterItem> ParameterFilters { get; } = [];

            /// <summary>
            /// Data provider.
            /// </summary>
            protected CardFilesDataProvider DataProvider { get; } = NotNullOrThrow(dataProvider);

            /// <summary>
            /// Data request.
            /// </summary>
            protected IGetDataRequest Request { get; } = NotNullOrThrow(request);

            #endregion

            #region IAsyncInitializable Members

            /// <inheritdoc />
            public virtual ValueTask InitializeAsync(CancellationToken cancellationToken = default)
            {
                var alwaysTrueBlock = new ParameterFilterItem(AlwaysTrue);

                this.ParameterFilters.Clear();
                this.ParameterFilters.Add(alwaysTrueBlock);

                var requestParameters = this.DataProvider.CreateParameters(this.Request);
                foreach (var parameter in requestParameters)
                {
                    if (parameter.Name == ColumnsConst.Caption)
                    {
                        var filterBlock = new ParameterFilterItem();
                        foreach (var criteria in parameter.CriteriaValues)
                        {
                            this.AppendCriteriaToCaptionFilterFunc(criteria, filterBlock);
                        }

                        this.ParameterFilters.Add(filterBlock);
                    }
                }

                return ValueTask.CompletedTask;
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// Make filtration.
            /// </summary>
            /// <param name="fileObject">Target view model.</param>
            /// <returns>Whether given view model must be in resulting data set.</returns>
            public bool Filter(IFileViewModel fileObject) =>
                this.ParameterFilters.All(block => block.CriteriaFilters.Any(func => func(fileObject)));

            #endregion

            #region Protected Methods

            /// <summary>
            /// Build up criteria for name filtration.
            /// </summary>
            /// <param name="criteria">Source criteria.</param>
            /// <param name="filterBlock">Target filtering block.</param>
            /// <exception cref="ArgumentOutOfRangeException">Given criterion is unsupported.</exception>
            protected virtual void AppendCriteriaToCaptionFilterFunc(
                RequestCriteria criteria, ParameterFilterItem filterBlock)
            {
                switch (criteria.CriteriaName)
                {
                    case CriteriaOperatorConst.Contains:
                        filterBlock.CriteriaFilters.Add(row =>
                            criteria.Values[0].Value is string { Length: > 0 } value
                            && row.Caption.Contains(value, StringComparison.OrdinalIgnoreCase));
                        break;

                    case CriteriaOperatorConst.EqualsTo:
                        filterBlock.CriteriaFilters.Add(row =>
                            criteria.Values[0].Value is string { Length: > 0 } value
                            && string.Equals(row.Caption, value, StringComparison.OrdinalIgnoreCase));
                        break;

                    case CriteriaOperatorConst.StartsWith:
                        filterBlock.CriteriaFilters.Add(row =>
                            criteria.Values[0].Value is string { Length: > 0 } value
                            && row.Caption.StartsWith(value, StringComparison.OrdinalIgnoreCase));
                        break;

                    case CriteriaOperatorConst.EndsWith:
                        filterBlock.CriteriaFilters.Add(row =>
                            criteria.Values[0].Value is string { Length: > 0 } value
                            && row.Caption.EndsWith(value, StringComparison.OrdinalIgnoreCase));
                        break;

                    default:
                        throw ArgumentOutOfRange(criteria.CriteriaName);
                }
            }

            /// <summary>
            /// Always true criteria filter.
            /// </summary>
            /// <param name="fileObject">Target view model.</param>
            /// <returns>True.</returns>
            protected static bool AlwaysTrue(IFileViewModel? fileObject) => true;

            #endregion
        }

        #endregion

        #region Private Methods

        private static IEnumerable<Dictionary<string, object?>> ApplyPaging(
            IEnumerable<Dictionary<string, object?>> rows,
            IReadOnlyCollection<RequestParameter> requestParameters)
        {
            if (requestParameters.Count == 0)
            {
                return rows;
            }

            int? pageOffset = null;
            int? pageLimit = null;

            foreach (var parameter in requestParameters)
            {
                switch (parameter.Name)
                {
                    case ViewSpecialParametersConst.PageOffset:
                        pageOffset = parameter.CriteriaValues.FirstOrDefault(x => x.CriteriaName == CriteriaOperatorConst.EqualsTo)?
                            .Values.FirstOrDefault()?
                            .Value as int?;
                        break;
                    case ViewSpecialParametersConst.PageLimit:
                        pageLimit = parameter.CriteriaValues.FirstOrDefault(x => x.CriteriaName == CriteriaOperatorConst.EqualsTo)?
                            .Values.FirstOrDefault()?
                            .Value as int?;
                        break;
                }
            }

            if (pageOffset is null)
            {
                return rows;
            }

            pageLimit ??= DefaultTypeExtensionTypeHelper.DefaultPageLimit;
            return rows.Skip(pageOffset.Value - 1).Take(pageLimit.Value).ToList();
        }

        #endregion

        #region Protected Declarations

        /// <summary>
        /// Source view metadata.
        /// </summary>
        protected IViewMetadata ViewMetadata { get; } = NotNullOrThrow(viewMetadata);

        /// <summary>
        /// Target file control.
        /// </summary>
        protected IFileControl FileControl { get; } = NotNullOrThrow(fileControl);

        /// <summary>
        /// Make base description of data response.
        /// </summary>
        /// <param name="response">Data response.</param>
        protected virtual void AddColumns(IGetDataResponse response)
        {
            response.Columns.Add(new(ColumnsConst.GroupName, SchemeType.NullableString));
            response.Columns.Add(new(ColumnsConst.GroupCaption, SchemeType.NullableString));
            response.Columns.Add(new(ColumnsConst.Caption, SchemeType.NullableString));
            response.Columns.Add(new(ColumnsConst.CategoryCaption, SchemeType.NullableString));
            response.Columns.Add(new(ColumnsConst.Size, SchemeType.String));
            response.Columns.Add(new(ColumnsConst.SizeAbsolute, SchemeType.Int64));
        }

        /// <summary>
        /// Fill in the data.
        /// </summary>
        /// <param name="request">Data request</param>
        /// <param name="response">Data response.</param>
        /// <param name="cancellationToken">Object for cancelling asynchronous operation.</param>
        protected virtual async ValueTask PopulateDataRowsAsync(
            IGetDataRequest request,
            IGetDataResponse response,
            CancellationToken cancellationToken = default)
        {
            var filter = await this.GetFilterAsync(request, cancellationToken).ConfigureAwait(false);
            var requestParameters = this.CreateParameters(request).ToArray();
            var files = this.GetItems();

            // если фильтры сбросили через кнопку в представлении, то нужно сбросить фильтрацию в файловом контроле.
            if (this.FileControl.SelectedFiltering != null &&
                requestParameters.All(x => x.Name != ColumnsConst.FilterParameter))
            {
                await this.FileControl.SelectFilteringAsync(null, cancellationToken);
            }

            if (this.FileControl.SelectedFiltering is { } filtering)
            {
                files = files.Where(filtering.IsVisible);
            }

            var rows = files.Where(filter).Select(this.MapFileToRow).ToArray();

            var sorter = await this.GetSorterAsync(request, cancellationToken).ConfigureAwait(false);
            Array.Sort(rows, sorter);

            response.CalculatedRowCount = rows.Length;
            response.Rows.AddRange(ApplyPaging(rows, requestParameters));
        }

        /// <summary>
        /// Provide data filter.
        /// </summary>
        /// <param name="request">Data request.</param>
        /// <param name="cancellationToken">Object for cancelling asynchronous operation.</param>
        /// <returns>Filter.</returns>
        protected virtual async ValueTask<Func<IFileViewModel, bool>> GetFilterAsync(
            IGetDataRequest request,
            CancellationToken cancellationToken = default)
        {
            var filter = new FileFilter(this, request);
            await filter.InitializeAsync(cancellationToken).ConfigureAwait(false);
            return filter.Filter;
        }

        /// <summary>
        /// Provide data sorter.
        /// </summary>
        /// <param name="request">Data request.</param>
        /// <param name="cancellationToken">Object for cancelling asynchronous operation.</param>
        /// <returns>Sorter.</returns>
        protected virtual async ValueTask<IComparer<Dictionary<string, object?>>> GetSorterAsync(
            IGetDataRequest request,
            CancellationToken cancellationToken = default)
        {
            var sorter = new FileSorter(this, request);
            await sorter.InitializeAsync(cancellationToken).ConfigureAwait(false);
            return sorter;
        }

        /// <summary>
        /// Provide data items.
        /// </summary>
        /// <returns>File view model data items.</returns>
        protected virtual IEnumerable<IFileViewModel> GetItems() => this.FileControl.Items;

        /// <summary>
        /// Create request parameters.
        /// </summary>
        /// <param name="request">Data request.</param>
        /// <returns>Final request parameters.</returns>
        protected virtual IEnumerable<RequestParameter> CreateParameters(IGetDataRequest request) => request.CreateParameters();

        /// <summary>
        /// Make mapping between file view model and target view.
        /// </summary>
        /// <param name="file">Source file view model.</param>
        /// <returns>Data row.</returns>
        protected virtual Dictionary<string, object?> MapFileToRow(IFileViewModel file)
        {
            // using &nbsp; to separate units
            var size = $"{FormatSize(file.Model.Size, SizeUnit.Kilobytes)}\u00A0{FormatUnit(SizeUnit.Kilobytes)}";

            return new()
            {
                [ColumnsConst.FileViewModel] = file,
                [ColumnsConst.GroupName] = file.Group.ID?.ToString() ?? string.Empty,
                [ColumnsConst.GroupCaption] = file.Group.Caption,

                // если в "( Без категории )" не заменять обычные пробелы на неделимые, то автосайз начинает колбасить
                // при наличии нескольких таких строк, и расчёт ширины происходит заметно длительнее
                [ColumnsConst.CategoryCaption] = file.Model.Category?.Caption
                    ?? Localize("$UI_Cards_FileNoCategory").ReplaceSpacesToNonBreakable(),

                [ColumnsConst.Caption] = file.Caption,
                [ColumnsConst.SizeAbsolute] = file.Model.Size,
                [ColumnsConst.Size] = size,
            };
        }

        #endregion

        #region IDataProvider Members

        /// <inheritdoc/>
        public async ValueTask<IGetDataResponse> GetDataAsync(
            IGetDataRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = new GetDataResponse();
            this.AddColumns(result);
            await this.PopulateDataRowsAsync(request, result, cancellationToken).ConfigureAwait(false);
            return result;
        }

        #endregion
    }
}
