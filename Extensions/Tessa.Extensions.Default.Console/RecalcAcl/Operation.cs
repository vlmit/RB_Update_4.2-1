using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.ConsoleApps;
using Tessa.Roles.Acl;
using Tessa.Roles.Acl.Manager.Params;

namespace Tessa.Extensions.Default.Console.RecalcAcl
{
    public sealed class Operation(
        IConsoleSessionManager sessionManager,
        IConsoleLogger logger,
        IAclManager aclManager)
        : ConsoleOperation<OperationContext>(logger, sessionManager)
    {
        #region Base Overrides

        public override async Task<int> ExecuteAsync(
            OperationContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var rulesCountStr = context is { All: false, Ids.Length: > 0 } ? context.Ids?.Length.ToString() : "all";
                var cardsCountStr = context.Cards?.Length > 0 ? context.Cards?.Length.ToString() : "all";
                await this.Logger.InfoAsync($"Recalculating {rulesCountStr} acl rules for {cardsCountStr} cards.");
                if (context is { All: false, Ids.Length: > 0 })
                {
                    await this.Logger.InfoAsync("Rule IDs:");
                    await this.Logger.InfoAsync(string.Join(Environment.NewLine, context.Ids));
                }

                if (context.Cards?.Length > 0)
                {
                    await this.Logger.InfoAsync("Card IDs:");
                    await this.Logger.InfoAsync(string.Join(Environment.NewLine, context.Cards));
                }

                var aclRequest = new AclManagerRequest(
                    context.All ? null : new GetRulesByRuleIDsParam { RuleIDs = context.Ids },
                    context.Cards?.Length == 0 ? null : new GetCardsByCardsIDsParam { CardIDs = context.Cards },
                    options: new()
                    {
                        CanDefer = false
                    });

                var result = await aclManager.UpdateAclAsync(aclRequest, cancellationToken);
                var validationResult = result.ValidationResult;
                await this.Logger.LogResultAsync(validationResult);

                if (!validationResult.IsSuccessful)
                {
                    await this.Logger.ErrorAsync("Acl recalculation has errors.");
                    return -1;
                }

                await this.Logger.InfoAsync("Acl recalculation complete.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await this.Logger.LogExceptionAsync("Acl recalculation error.", e);
                return -1;
            }
        }

        #endregion
    }
}
