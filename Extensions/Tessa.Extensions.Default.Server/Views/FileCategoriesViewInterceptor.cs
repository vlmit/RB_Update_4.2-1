#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Internal.Extensions;
using Tessa.Platform;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Server.Views
{
    /// <summary>
    /// Перехватчик представлений файловых категорий, добавляющий вариант "Без категории" на первую страницу первым пунктом.
    /// По умолчанию перехватывает стандартные представления отображения категорий файлов.
    /// Класс можно наследовать и применять для своих представлений, отображающих категорию файлов.
    /// </summary>
    public class FileCategoriesViewInterceptor : ViewInterceptorBase
    {
        #region Fields

        /// <summary>
        /// Имя параметра, наличие которого включает отображение варианта "Без категории" в результате представления на первой странице.
        /// </summary>
        public const string IncludeWithoutCategoryParamName = "IncludeWithoutCategory";

        /// <summary>
        /// Отображаемое значение варианта "Без категории".
        /// </summary>
        public const string WithoutCategoryValue = "$UI_Cards_FileNoCategory";

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса перехватчика для представлений по умолчанию.
        /// </summary>
        public FileCategoriesViewInterceptor()
            : base(["FileCategoriesAll", "FileCategoriesFiltered"])
        {
        }

        /// <summary>
        /// Создаёт экземпляр класса перехватчика для представлений по указанным алиасам.
        /// </summary>
        /// <param name="viewAliases">Алиасы представлений для перехватывания.</param>
        protected FileCategoriesViewInterceptor(string[] viewAliases)
            : base(viewAliases)
        {
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default)
        {
            var view = this.GetInterceptedView(request.ViewAlias);

            // Не нашли искомый параметр, выполняем представление без перехвата
            if (!request.Parameters.IsDefinedByName(IncludeWithoutCategoryParamName))
            {
                return await view.GetDataAsync(request, cancellationToken);
            }

            var viewMetadata = await view.GetMetadataAsync(cancellationToken);
            if (!await this.FilterWithoutCategoryAsync(request, viewMetadata, cancellationToken))
            {
                return await view.GetDataAsync(request, cancellationToken);
            }

            var showWithoutCategory = true;
            if (request.Parameters.FindByName(ViewSpecialParametersConst.PageOffset) is { } offsetParameter
                && request.Parameters.FindByName(ViewSpecialParametersConst.PageLimit) is { } limitParameter
                && offsetParameter.CriteriaValues[0].Values[0].Value is int offset
                && limitParameter.CriteriaValues[0].Values[0].Value is int limit)
            {
                if (offset == 1)
                {
                    limitParameter.CriteriaValues[0].Values[0].Value = limit - 1;
                }
                else
                {
                    showWithoutCategory = false;
                    offsetParameter.CriteriaValues[0].Values[0].Value = offset - 1;
                }
            }

            var result = await view.GetDataAsync(request, cancellationToken);

            if (showWithoutCategory
                && this.TryCreateRowData(viewMetadata) is { } rowData)
            {
                result.Rows.Insert(0, rowData);
                result.RowCount++;
            }

            return result;
        }

        #endregion

        #region Virtual properties and Methods

        /// <summary>
        /// Имя параметра для поиска файловой категории по имени.
        /// </summary>
        protected virtual string NameParameterAlias => "Name";

        /// <summary>
        /// Рассчитывает по параметрам фильтрации признак, который показывает, можно ли отображать значение "Без категории" или нет в результате представления.
        /// По умолчанию выполняет фильтрацию по названию с именем параметр <see cref="NameParameterAlias"/>
        /// </summary>
        /// <param name="request">Запрос к представлению.</param>
        /// <param name="viewMetadata">Метаданные представления.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Значение <c>true</c>, если по результатам фильтрации значение "Без категории" можно отображать, иначе <c>false</c>.</returns>
        protected virtual ValueTask<bool> FilterWithoutCategoryAsync(ITessaViewRequest request, IViewMetadata viewMetadata, CancellationToken cancellationToken = default)
        {
            if (request.Parameters.FindByName(this.NameParameterAlias) is { } nameParameter)
            {
                var withoutCategoryName = Localize(WithoutCategoryValue);
                foreach (var criteria in nameParameter.CriteriaValues)
                {
                    Func<string, string?, bool>? valueCheck = criteria.CriteriaName switch
                    {
                        CriteriaOperatorConst.Contains => (s1, s2) => !string.IsNullOrEmpty(s2) && s1.Contains(s2, StringComparison.OrdinalIgnoreCase),
                        CriteriaOperatorConst.EqualsTo => (s1, s2) => s1.Equals(s2, StringComparison.OrdinalIgnoreCase),
                        CriteriaOperatorConst.NotEqualsTo => (s1, s2) => !s1.Equals(s2, StringComparison.OrdinalIgnoreCase),
                        CriteriaOperatorConst.StartsWith => (s1, s2) => !string.IsNullOrEmpty(s2) && s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase),
                        CriteriaOperatorConst.EndsWith => (s1, s2) => !string.IsNullOrEmpty(s2) && s1.EndsWith(s2, StringComparison.OrdinalIgnoreCase),
                        CriteriaOperatorConst.IsNotNull => (s1, s2) => true,
                        _ => null
                    };

                    if (valueCheck is null)
                    {
                        return new(false);
                    }

                    foreach (var value in criteria.Values)
                    {
                        if (!valueCheck.Invoke(withoutCategoryName, value.Value as string))
                        {
                            return new(false);
                        }
                    }
                }
            }

            return new(true);
        }

        /// <summary>
        /// Создаёт строку с данными по переданным метаданным представления или <c>null</c>, если строку не удалось создать.
        /// Реализация по умолчанию выбирает первый объект <see cref="IViewReferenceMetadata"/> из метаданных представления,
        /// для колонок выбранной ссыкли записывает <see cref="Guid.Empty"/> в качестве идентификатора и <see cref="WithoutCategoryValue"/> в качествве отображаемого имени,
        /// а для прочих записывает значение по умолчанию в соответствии с типом колонки.
        /// </summary>
        /// <param name="viewMetadata">Метаданные представления.</param>
        /// <returns>Строка с данными или <c>null</c>, если строку не удалось создать.</returns>
        protected virtual List<object?>? TryCreateRowData(IViewMetadata viewMetadata)
        {
            if (viewMetadata.References.FirstOrDefault() is not { } reference)
            {
                return null;
            }

            var referenceIDColumn = reference.RefSection + "ID";

            var rowData = new List<object?>(viewMetadata.Columns.Count);
            foreach (var column in viewMetadata.Columns)
            {
                object? value;
                if (column.Alias.Equals(referenceIDColumn, StringComparison.Ordinal))
                {
                    value = GuidBoxes.Empty;
                }
                else if (column.Alias == reference.DisplayValueColumn)
                {
                    value = WithoutCategoryValue;
                }
                else
                {
                    value = column.SchemeType.ClrType.GetDefaultValue();
                }

                rowData.Add(value);
            }

            return rowData;
        }

        #endregion
    }
}
