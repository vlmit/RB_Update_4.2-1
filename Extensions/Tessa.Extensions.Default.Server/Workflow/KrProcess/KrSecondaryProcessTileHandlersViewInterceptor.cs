#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Views;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess
{
    /// <summary>
    /// Перехватчик представления с обработчиками кнопок вторичных процессов.
    /// </summary>
    /// <param name="resolver"><inheritdoc cref="IKrSecondaryProcessTileHandlerResolver" path="/summary"/></param>
    public sealed class KrSecondaryProcessTileHandlersViewInterceptor(IKrSecondaryProcessTileHandlerResolver resolver)
        : ViewInterceptorBase(["KrSecondaryProcessTileHandlers"])
    {
        #region Fields

        private readonly IKrSecondaryProcessTileHandlerResolver resolver = NotNullOrThrow(resolver);

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override async ValueTask<ITessaViewResult> GetDataAsync(
            ITessaViewRequest request,
            CancellationToken cancellationToken = default)
        {
            var view = this.GetInterceptedView(request.ViewAlias);

            var metadata = await view.GetMetadataAsync(cancellationToken);
            var rows = this.GetRows(request);

            return new TessaViewResult(metadata) { Rows = rows };
        }

        #endregion

        #region Private Methods

        private List<List<object?>> GetRows(ITessaViewRequest request)
        {
            var allRows = this.resolver.GetAllKeys();

            if (allRows.Count == 0)
            {
                return [];
            }

            IEnumerable<KrSecondaryProcessTileHandlerDescriptor> rows = allRows;

            if (request.Parameters.FindByName("Name") is { } nameParam)
            {
                rows = rows.Where(row =>
                {
                    var locName = Localize(row.Name);

                    foreach (var criteria in nameParam.CriteriaValues)
                    {
                        var criteriaValue = (string?) criteria.Values.FirstOrDefault()?.Value ?? string.Empty;
                        if (locName.Contains(criteriaValue, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    return false;
                });
            }

            return rows.Select(static row => new List<object?> { row.ID, row.Name }).ToList();
        }

        #endregion
    }
}
