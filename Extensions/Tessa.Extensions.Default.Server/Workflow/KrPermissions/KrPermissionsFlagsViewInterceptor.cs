#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Views;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions
{
    /// <summary>
    /// Перехватчик представления для формирования списка настроек прав доступа.
    /// </summary>
    public sealed class KrPermissionsFlagsViewInterceptor()
        : ViewInterceptorBase(["KrPermissionFlags"])
    {
        #region Base Overrides

        /// <inheritdoc />
        public override async ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default)
        {
            var view = this.GetInterceptedView(request.ViewAlias);

            var metadata = await view.GetMetadataAsync(cancellationToken);
            request.ProvideDefaults(metadata);
            var sortDirectionDesc = request.SortingColumns.FindByName("FlagCaption")?.Descending;

            var preparedData = KrPermissionFlagDescriptors.Full.IncludedPermissions
                .Select(x => (x.ID, Name: x.Description, LocalizedName: Localize(x.Description)));
            preparedData = sortDirectionDesc switch
            {
                false => preparedData.OrderBy(x => x.LocalizedName),
                true => preparedData.OrderByDescending(x => x.LocalizedName),
                null => preparedData
            };

            if (request.Parameters.FindByName("CaptionParam") is { } param)
            {
                foreach (var criteria in param.CriteriaValues)
                {
                    if (criteria.Values.FirstOrDefault()?.Value is string { Length: not 0 } value)
                    {
                        preparedData = preparedData.Where(x => x.LocalizedName.Contains(value, StringComparison.OrdinalIgnoreCase));
                    }
                }
            }

            return new TessaViewResult(metadata)
            {
                Rows = preparedData.Select(x => new List<object?> { x.ID, x.Name }).ToList(),
            };
        }

        #endregion
    }
}
