#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Server.Views
{
    /// <summary>
    /// Перехватчик представления MyTasks.
    /// Проверяет возможность смотреть представление с параметром User, отличным от текущего сотрудника.
    /// </summary>
    public class MyTasksViewInterceptor(Func<IViewService> viewServiceFunc)
        : ViewInterceptorBase(["MyTasks"])
    {
        #region Private Fields

        private const string PermissionsViewName = "ReportPermissionsMyTasks";

        private const string UserParamName = "User";

        private readonly Func<IViewService> viewServiceFunc = NotNullOrThrow(viewServiceFunc);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default)
        {
            var view = this.GetInterceptedView(request.ViewAlias);

            if (request.Parameters.FindByName(UserParamName) is not { } userParam)
            {
                // Если не указан параметр User - выполняем представление без изменений.
                return await view.GetDataAsync(request, cancellationToken);
            }

            var currentUserParam = request.Parameters.FindByName(ViewSpecialParametersConst.CurrentUserId)
                ?? throw new InvalidOperationException(
                    $"Can't get {ViewSpecialParametersConst.CurrentUserId} in request for intercepted view with alias \"{request.ViewAlias}\"");

            var userID = GetSingleGuid(userParam);
            var currentUserID = GetSingleGuid(currentUserParam);

            // Если текущий сотрудник совпадает с тем, что указан в параметре User.
            if (userID.Equals(currentUserID))
            {
                // Если указан параметр User и он совпадает с параметром СurrentUserID - выполняем представление без изменений.
                return await view.GetDataAsync(request, cancellationToken);
            }

            var permissionsView = await this.viewServiceFunc().GetByNameAsync(PermissionsViewName, cancellationToken)
                // у пользователя нет доступа к одному из необходимых для работы представлений, или нет самого представления
                ?? throw new InvalidOperationException($"Can't find view by alias \"{PermissionsViewName}\"");

            var permissionsViewMetadata = await permissionsView.GetMetadataAsync(cancellationToken);

            // получаем данные представления
            var viewResult = await permissionsView.GetDataAsync(
                new TessaViewRequest(permissionsViewMetadata.Alias)
                {
                    permissionsViewMetadata.Parameters.IsDefinedByName(UserParamName)
                        ? new RequestParameter(UserParamName).Add(EqualsToCriteriaOperator.Instance, userID)
                        : null
                }, cancellationToken);

            var hasPermissions = viewResult.Rows.FirstOrDefault()?.Cast<bool>().FirstOrDefault() ?? false;
            return hasPermissions
                ? await view.GetDataAsync(request, cancellationToken)
                : new TessaViewResult(await view.GetMetadataAsync(cancellationToken));
        }

        #endregion

        #region Static Methods

        private static Guid GetSingleGuid(RequestParameter parameter) =>
            (Guid) NotNullOrThrow(parameter.CriteriaValues
                .Single(x => x.CriteriaName == CriteriaOperatorConst.EqualsTo)
                .Values.Single().Value);

        #endregion
    }
}
