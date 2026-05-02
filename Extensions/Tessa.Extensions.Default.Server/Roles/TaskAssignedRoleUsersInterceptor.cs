#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Roles;
using Tessa.Views;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Server.Roles
{
    /// <summary>
    /// Перехватывает представление, отображающее пользователей для диалога "Роли задания".
    /// Используются отдельное представление для "контекстных ролей" и отдельное представление для всех остальных типов ролей.
    /// </summary>
    public sealed class TaskAssignedRoleUsersInterceptor(
        Func<IViewService> viewService,
        ICardGetStrategy cardGetStrategy) :
        ViewInterceptorBase(["TaskAssignedRoleUsers"])
    {
        #region Constants and Fields

        private const string UsersViewAlias = "Users";
        private const string TarRowIDParameterName = "TaskAssignedRoleRowID";

        private readonly Func<IViewService> viewService = NotNullOrThrow(viewService);
        private readonly ICardGetStrategy cardGetStrategy = NotNullOrThrow(cardGetStrategy);

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default)
        {
            var view = this.GetInterceptedView(request.ViewAlias);

            var roleIDParameter = request.Parameters.FindByName(CardTaskAssignedRole.RoleIDKey);

            var roleTypeId = roleIDParameter?.CriteriaValues
                .FirstOrDefault(x => x.CriteriaName == CriteriaOperatorConst.EqualsTo)
                ?.Values.FirstOrDefault()?.Value is Guid roleID
                ? await this.cardGetStrategy.GetTypeIDAsync(roleID, CardInstanceType.Card, cancellationToken)
                : null;

            if (roleTypeId == RoleHelper.ContextRoleTypeID)
            {
                request.Parameters.RemoveAllByName(CardTaskAssignedRole.RoleIDKey);
                return await view.GetDataAsync(request, cancellationToken);
            }

            request.Parameters.RemoveAllByName(TarRowIDParameterName);

            view = await this.viewService().GetByNameAsync(UsersViewAlias, cancellationToken)
                ?? throw new InvalidOperationException($"Can't find view by alias: \"{UsersViewAlias}\".");

            request.ViewAlias = UsersViewAlias;
            return await view.GetDataAsync(request, cancellationToken);
        }

        #endregion
    }
}
