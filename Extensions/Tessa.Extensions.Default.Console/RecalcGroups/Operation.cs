using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.RefGroups;

namespace Tessa.Extensions.Default.Console.RecalcGroups
{
    public sealed class Operation :
        ConsoleOperation<OperationContext>
    {
        #region Constructors

        public Operation(
            IConsoleSessionManager sessionManager,
            IConsoleLogger logger,
            ICardService cardService
        )
            : base(logger, sessionManager)
        {
            ThrowIfNull(cardService);
            this.cardService = cardService;
        }

        #endregion

        #region Private Fields
        
        private readonly ICardService cardService;

        #endregion

        #region Base Overrides

        public override async Task<int> ExecuteAsync(
            OperationContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                switch (context.Mode)
                {
                    case RefGroupsRecalcMode.AllGroups:
                        await this.Logger.InfoAsync("Recalculating all groups.");
                        break;
                    case RefGroupsRecalcMode.ByGroupIDs:
                        ThrowIfNull(context.Ids);
                        await this.Logger.InfoAsync($"Recalculating {context.Ids.Length} groups.");
                        break;
                    case RefGroupsRecalcMode.ByGroupTypeIDs:
                        ThrowIfNull(context.Ids);
                        await this.Logger.InfoAsync(
                            $"Recalculating all groups of group types with ids: {string.Join(", ", context.Ids)}");
                        break;
                    default:
                        throw ArgumentOutOfRange(context.Mode);
                }
                
                var info = new Dictionary<string, object?>();
                info[RefGroupsHelper.ReCalcMode] = (int) context.Mode;
                info[RefGroupsHelper.ReCalcIDList] = context.Ids;

                var request = new CardRequest
                {
                    RequestType = RefGroupsRequestTypes.RecalculateRefGroups,
                    Info = info
                };

                var response = await this.cardService.RequestAsync(request, cancellationToken);
                var validationResult = response.ValidationResult.Build();
                await this.Logger.LogResultAsync(validationResult);

                if (!validationResult.IsSuccessful)
                {
                    await this.Logger.ErrorAsync("Recalculation has errors.");
                    return -1;
                }

                await this.Logger.InfoAsync("Recalculation complete.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await this.Logger.LogExceptionAsync("Recalculation error.", e);
                return -1;
            }
        }

        #endregion
    }
}
