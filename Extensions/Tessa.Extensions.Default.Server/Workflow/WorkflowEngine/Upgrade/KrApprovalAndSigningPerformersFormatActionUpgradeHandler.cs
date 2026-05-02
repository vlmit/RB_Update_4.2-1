#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Workflow.Storage;
using Tessa.Workflow.Upgrade;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine.Upgrade
{
    /// <summary>
    /// Исправляет формат полей коллекции <see cref="WorkflowConstants.NamesKeys.RoleList"/> в экземплярах действий: <see cref="KrApprovalAction"/> и <see cref="KrSigningAction"/>.
    /// </summary>
    public sealed class KrApprovalAndSigningPerformersFormatActionUpgradeHandler :
        IWorkflowEngineActionUpgradeHandler
    {
        #region Constants

        /// <summary>
        /// Версия действия бизнес-процесса, на которую происходит обновление.
        /// </summary>
        public const int UpgradeToVersion = 2;

        #endregion

        #region IWorkflowEngineActionUpgradeHandler Members

        /// <inheritdoc />
        public Task UpgradeActionInstanceAsync(
            WorkflowActionStorage actionTemplateStorage,
            WorkflowActionStateStorage actionStateStorage,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            const string oldKeyName = "ParentRoleID";
            const string newKeyName = nameof(RoleEntryStorage.RowID);

            var roleList = actionStateStorage.Hash.TryGet<IReadOnlyCollection<object>>(WorkflowConstants.NamesKeys.RoleList);

            if (roleList is { Count: > 0 })
            {
                foreach (var role in roleList.Cast<Dictionary<string, object?>>())
                {
                    if (role.Remove(oldKeyName, out var value))
                    {
                        role[newKeyName] = value;
                    }
                }
            }

            actionStateStorage.Version = UpgradeToVersion;

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task UpgradeActionTemplateAsync(
            WorkflowActionStorage actionStorage,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            actionStorage.Version = UpgradeToVersion;

            return Task.CompletedTask;
        }

        #endregion
    }
}
