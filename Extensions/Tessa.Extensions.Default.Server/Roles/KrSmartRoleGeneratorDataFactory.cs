#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Data;
using Tessa.Platform.RefGroups;
using Tessa.Roles.Queries;
using Tessa.Roles.SmartRoles;

namespace Tessa.Extensions.Default.Server.Roles
{
    /// <summary>
    /// Фабрика для создания объектов данных для генераторов умных ролей по данным карточки "Генератор умных ролей",
    /// которые учитывают обработку триггеров по типам документов.
    /// </summary>
    public class KrSmartRoleGeneratorDataFactory(
        IDbScope dbScope,
        IComplexQueryBuilderFactory getItemsQueryBuilderFactory,
        IRefGroupsManager refGroupsManager)
        : SmartRoleGeneratorDataFactory(dbScope, getItemsQueryBuilderFactory, refGroupsManager)
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override async ValueTask<ISmartRoleGeneratorData> CreateAsync(
            SmartRoleGeneratorDataSource source,
            CancellationToken cancellationToken = default)
        {
            var data = new KrSmartRoleGeneratorData(
                source,
                this.DbScope,
                this.GetItemsQueryBuilderFactory,
                this.RefGroupsManager);

            await data.InitializeAsync(cancellationToken);

            return data;
        }

        #endregion
    }
}
