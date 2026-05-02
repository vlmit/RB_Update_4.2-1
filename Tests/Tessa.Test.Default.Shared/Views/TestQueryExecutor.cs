#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Tessa.Views;
using Tessa.Views.Metadata;

namespace Tessa.Test.Default.Shared.Views
{
    /// <summary>
    /// Тестовый Объект, выполняющий запросы к базе данных для получения результатов представлений.
    /// </summary>
    public sealed class TestQueryExecutor : IViewQueryExecutor
    {
        #region IViewQueryExecutor Members

        /// <inheritdoc/>
        public ValueTask<ITessaViewResult> ExecuteAsync(
            string queryText,
            IViewMetadata metadata,
            ITessaViewRequest request,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(queryText);
            ThrowIfNull(metadata);
            ThrowIfNull(request);

            return new(new TessaViewResult(metadata));
        }

        #endregion
    }
}
