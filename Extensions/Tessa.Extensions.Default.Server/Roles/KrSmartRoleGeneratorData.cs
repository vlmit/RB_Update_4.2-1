#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Internal.Common;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Tessa.Platform.RefGroups;
using Tessa.Roles.Queries;
using Tessa.Roles.SmartRoles;
using Tessa.Roles.Triggers;

namespace Tessa.Extensions.Default.Server.Roles
{
    /// <summary>
    /// Объект данных для генератора умных ролей по данным карточки "Генератор умных ролей",
    /// которые учитывают обработку триггеров по типам документов.
    /// </summary>
    public class KrSmartRoleGeneratorData(
        SmartRoleGeneratorDataSource source,
        IDbScope dbScope,
        IComplexQueryBuilderFactory getItemsQueryBuilderFactory,
        IRefGroupsManager refGroupsManager)
        : SmartRoleGeneratorData(source, dbScope, getItemsQueryBuilderFactory, refGroupsManager)
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask InitializeTriggersAsync(CancellationToken cancellationToken = default)
        {
            await base.InitializeTriggersAsync(cancellationToken);

            foreach (var trigger in this.Triggers)
            {
                for (int i = trigger.Types.Count - 1; i >= 0; i--)
                {
                    var typeID = trigger.Types[i];
                    var group = await this.RefGroupsManager.GetGroupAsync(typeID, RefGroupsHelper.DocumentTypeRefGroupTypeID, cancellationToken);
                    if (group is not null)
                    {
                        trigger.Types.RemoveAt(i);
                        if (group.CalculatedValues is not { Count: > 0 })
                        {
                            continue;
                        }

                        trigger.Types.AddRange(group.CalculatedValues.Select(x => (Guid) x.ID));
                    }
                }

                trigger.Types.RemoveDuplicates();

                trigger.CheckTriggerCardAsyncFunc = this.CheckTriggerAsync;
            }
        }

        #endregion

        #region Private Methods

        private async ValueTask<bool> CheckTriggerAsync(UpdateTrigger trigger, Card triggerCard, CancellationToken cancellationToken)
        {
            if (trigger.Types.Contains(triggerCard.TypeID))
            {
                return true;
            }

            var docTypeID = await KrProcessSharedHelper.GetDocTypeIDAsync(triggerCard, this.DbScope, cancellationToken);
            return docTypeID is not null && trigger.Types.Contains(docTypeID.Value);
        }

        #endregion
    }
}
