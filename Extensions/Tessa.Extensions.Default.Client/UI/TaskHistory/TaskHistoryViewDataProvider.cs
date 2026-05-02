#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.UI.Cards.Controls;

namespace Tessa.Extensions.Default.Client.UI.TaskHistory
{
    public sealed class TaskHistoryViewDataProvider(IDataProvider defaultDataProvider) : IDataProvider
    {
        #region Nested Types

        private sealed class RowsComparer(string columnName, bool ascending = true) : IComparer<Dictionary<string, object?>>
        {
            public int Compare(Dictionary<string, object?>? x, Dictionary<string, object?>? y)
            {
                if (x is null)
                {
                    return y is null ? 0 : -1;
                }

                if (y is null)
                {
                    return 1;
                }

                var right = x[columnName];
                var left = y[columnName];

                int result;
                if (right is null || left is null)
                {
                    result = string.Compare(right?.ToString(), left?.ToString(), StringComparison.Ordinal);
                }
                else if (x[columnName] is DateTime)
                {
                    result = DateTime.Compare((DateTime) right, (DateTime) left);
                }
                else
                {
                    result = string.Compare(right.ToString(), left.ToString(), StringComparison.Ordinal);
                }

                return ascending ? result : -result;
            }
        }

        #endregion

        #region Fields

        private readonly IDataProvider defaultDataProvider = NotNullOrThrow(defaultDataProvider);

        private volatile IGetDataResponse? cachedResponse;

        #endregion

        #region Methods

        /// <summary>
        /// Сбросить рассчитанный кэш.
        /// </summary>
        public void ResetCache() => this.cachedResponse = null;

        #endregion

        #region IDataProvider Members

        /// <inheritdoc/>
        public async ValueTask<IGetDataResponse> GetDataAsync(IGetDataRequest request, CancellationToken cancellationToken = default)
        {
            IGetDataResponse response;
            if (this.cachedResponse is not null)
            {
                response = this.cachedResponse;
            }
            else
            {
                response = await this.defaultDataProvider.GetDataAsync(request, cancellationToken).ConfigureAwait(false);
                this.cachedResponse = response;
            }

            if (request.SortingColumns?.Count == 1)
            {
                var columnName = request.SortingColumns[0].Alias;
                var comparer = new RowsComparer(columnName, !request.SortingColumns[0].Descending);

                var sortedRows = response.Rows.OrderBy(x => x, comparer).ToArray();

                response.Rows.Clear();
                response.Rows.AddRange(sortedRows);
            }

            return response;
        }

        #endregion
    }
}
