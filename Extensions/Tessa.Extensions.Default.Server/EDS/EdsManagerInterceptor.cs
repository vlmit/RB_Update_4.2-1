#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.EDS;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Server.EDS
{
    /// <summary>
    /// Перехватчик для представления <c>EdsManagers</c>, которое возвращает список имён объектов <see cref="ICAdESManager"/>
    /// из метаинформации по таблице <c>SignatureManagerVirtual</c>.
    /// </summary>
    public sealed class EdsManagerInterceptor(ICardMetadata cardMetadata)
        : ViewInterceptorBase(["EdsManagers"])
    {
        #region Nested Types

        private sealed class DataRow(string name)
        {
            public string Name { get; } = name;

            public List<object?> GetRow() => [this.Name];
        }

        private abstract class FilterOperation
        {
            public abstract bool IsSatisfied(DataRow row);
        }

        private sealed class NoFilterOperation : FilterOperation
        {
            public override bool IsSatisfied(DataRow row) => true;
        }

        private sealed class EqualsFilterOperation : FilterOperation
        {
            public required string? Text { get; init; }

            public override bool IsSatisfied(DataRow row) =>
                string.Equals(row.Name, this.Text, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class ContainsFilterOperation : FilterOperation
        {
            public required string? Text { get; init; }

            public override bool IsSatisfied(DataRow row) =>
                !string.IsNullOrEmpty(this.Text)
                && row.Name.Contains(this.Text, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Constants and Fields

        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);

        #endregion

        #region Private Methods

        private async ValueTask<IReadOnlyList<DataRow>> GenerateDataAsync(CancellationToken cancellationToken = default) =>
            (await this.cardMetadata.GetEnumerationsAsync(cancellationToken))["SignatureManagerVirtual"].Records
            .Select(record => record["Name"] as string)
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x => new DataRow(x!))
            .OrderBy(x => x.Name)
            .ToArray();

        private static FilterOperation GetFilterOperation(ITessaViewRequest request)
        {
            var parameter = request.Parameters.FindByName("Name");
            if (parameter is null)
            {
                return new NoFilterOperation();
            }

            var criteria = parameter.CriteriaValues.FirstOrDefault();
            return criteria?.CriteriaName switch
            {
                CriteriaOperatorConst.EqualsTo => new EqualsFilterOperation { Text = criteria.Values[0].Value?.ToString() },
                CriteriaOperatorConst.Contains => new ContainsFilterOperation { Text = criteria.Values[0].Value?.ToString() },
                _ => new NoFilterOperation()
            };
        }

        private static TessaViewResult GetDataCore(IViewMetadata metadata, FilterOperation filterOperation, IEnumerable<DataRow> data) =>
            new(metadata)
            {
                Rows = data.Where(filterOperation.IsSatisfied).Select(d => d.GetRow()).ToList()
            };

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default)
        {
            var view = this.GetInterceptedView(request.ViewAlias);
            return GetDataCore(
                await view.GetMetadataAsync(cancellationToken),
                GetFilterOperation(request),
                await this.GenerateDataAsync(cancellationToken));
        }

        #endregion
    }
}
