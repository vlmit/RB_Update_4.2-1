#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Platform.Data;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Server.Workflow.KrPermissions
{
    /// <summary>
    /// Перехватчики представлений правил доступа для обработки параметров фильтрации по настройкам прав доступа.
    /// </summary>
    public sealed class KrPermissionsViewInterceptor(IDbScope dbScope)
        : ViewInterceptorBase(["KrPermissions", "KrPermissionsReport"])
    {
        #region Fields

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override async ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default)
        {
            var view = this.GetInterceptedView(request.ViewAlias);

            // Удаляемый любую попытку извне прокинуть этот параметр в представление
            request.Parameters.RemoveAllByName("PermissionExpression");
            request.Parameters.RemoveAllByName("ByPermissionExpression");

            await using (this.dbScope.Create())
            {
                var permissionsParameter = request.Parameters.FindByName("Permission");
                if (permissionsParameter is not null)
                {
                    string permissionExpressionValue = string.Empty;
                    var flagTuples = permissionsParameter.CriteriaValues.Select(x => (
                        x.CriteriaName,
                        Flag: KrPermissionFlagDescriptors.Full.IncludedPermissions.FirstOrDefault(y => y.ID == ((Guid?) x.Values[0].Value))
                    )).Where(x => x.Flag is not null).ToArray();

                    if (flagTuples.Length > 0)
                    {
                        var builder = this.dbScope.BuilderFactory.Create().And().E(b =>
                        {
                            bool first = true;
                            foreach (var (criteriaName, flag) in flagTuples)
                            {
                                if (flag?.IsVirtual == false)
                                {
                                    if (first)
                                    {
                                        first = false;
                                    }
                                    else
                                    {
                                        b.Or();
                                    }

                                    switch (criteriaName)
                                    {
                                        case CriteriaOperatorConst.EqualsTo:
                                            b.C("t", flag.SqlName).Equals().V(true);
                                            break;
                                        case CriteriaOperatorConst.NotEqualsTo:
                                            b.C("t", flag.SqlName).Equals().V(false);
                                            break;
                                    }
                                }
                            }
                        });

                        // ToString т.к. Build добавляет ";" в конце
                        permissionExpressionValue = builder.ToString() ?? string.Empty;
                        builder.Return();
                    }

                    request.Parameters.Add(new RequestParameter("PermissionExpression")
                        .Add(EqualsToCriteriaOperator.Instance, permissionExpressionValue));
                }

                if (ParserNames.IsEquals(request.SubsetName, "ByPermission"))
                {
                    var builder = this.dbScope.BuilderFactory.Create();
                    var first = true;

                    foreach (var flag in KrPermissionFlagDescriptors.Full.IncludedPermissions)
                    {
                        if (flag.IsVirtual)
                        {
                            continue;
                        }

                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            builder.UnionAll();
                        }

                        builder
                            .Select()
                            .V(flag.ID).As("PermissionID")
                            .V(flag.Description).As("PermissionName")
                            .C("ID")
                            .From("KrPermissions").NoLock()
                            .Where().C(flag.SqlName).Equals().V(true);
                    }

                    // builder.ToString() returns string w/o semicolon
                    request.Parameters.Add(new RequestParameter("ByPermissionExpression")
                        .Add(EqualsToCriteriaOperator.Instance, builder.ToString()));

                    // Освобождаем билдер
                    builder.Build();
                }

                return await view.GetDataAsync(request, cancellationToken);
            }
        }

        #endregion
    }
}
