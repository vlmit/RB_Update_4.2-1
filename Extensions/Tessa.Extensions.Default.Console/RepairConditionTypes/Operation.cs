using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Conditions;
using Tessa.Platform.ConsoleApps;

namespace Tessa.Extensions.Default.Console.RepairConditionTypes
{
    public sealed class Operation : ConsoleOperation<OperationContext>
    {
        #region Fields

        private readonly IConditionRepairManager conditionRepairManager;

        #endregion

        #region Constructors

        public Operation(
            IConsoleSessionManager sessionManager,
            IConsoleLogger logger,
            IConditionRepairManager conditionRepairManager)
            : base(logger, sessionManager, extendedInitialization: true)
        {
            this.conditionRepairManager = conditionRepairManager;
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            if (context.ConditionTypeIDs is { Count: > 0 })
            {
                await this.Logger.InfoAsync(
                    "Repairing condition type started, repairing condition type with IDs = " +
                    string.Join($";{Environment.NewLine}", context.ConditionTypeIDs));
            }
            else
            {
                await this.Logger.InfoAsync("Repairing condition types started, repairing all condition types");
            }

            bool hasErrors;
            try
            {
                var result =
                    await this.conditionRepairManager.RepairConditionTypesAsync(
                        context.ConditionTypeIDs?.ToArray(),
                        cancellationToken: cancellationToken);
                await this.Logger.LogResultAsync(result);
                hasErrors = result.HasErrors;

            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await this.Logger.LogExceptionAsync("Error repairing condition types", e);
                return -3;
            }

            if (!hasErrors)
            {
                await this.Logger.InfoAsync("Condition types has been repaired successfully");
            }
            else
            {
                await this.Logger.InfoAsync("Condition types has been repaired with errors");
            }

            return 0;
        }

        #endregion
    }
}
