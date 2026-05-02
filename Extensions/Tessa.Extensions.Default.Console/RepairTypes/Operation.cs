using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Repair;
using Tessa.Platform.ConsoleApps;

namespace Tessa.Extensions.Default.Console.RepairTypes
{
    public sealed class Operation(
        IConsoleSessionManager sessionManager,
        IConsoleLogger logger,
        ICardTypeRepairManager cardTypeRepairManager,
        ICardTypeClientRepository cardTypeRepository)
        : ConsoleOperation<OperationContext>(logger, sessionManager, extendedInitialization: true)
    {
        #region Fields

        private readonly ICardTypeRepairManager cardTypeRepairManager = NotNullOrThrow(cardTypeRepairManager);
        private readonly ICardTypeClientRepository cardTypeRepository = NotNullOrThrow(cardTypeRepository);

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            if (context.CardTypeIDs is { Count: > 0 })
            {
                await this.Logger.InfoAsync(
                    "Repairing card type started, repairing card type with IDs = " +
                    string.Join($";{Environment.NewLine}", context.CardTypeIDs));
            }
            else
            {
                await this.Logger.InfoAsync("Repairing card types started, repairing all card types");
            }

            bool hasErrors = false;
            try
            {
                if (context.CardTypeIDs is { Count: > 0 })
                {
                    foreach (var cardTypeID in context.CardTypeIDs)
                    {
                        var cardType = await this.cardTypeRepository.GetCardTypeAsync(cardTypeID, cancellationToken);
                        if (cardType is null)
                        {
                            await this.Logger.InfoAsync($"Card find card type with ID = {cardTypeID}");
                            hasErrors = true;
                            continue;
                        }

                        var (repairResult, repairedCardType) = await this.cardTypeRepairManager.RepairCardTypeAsync(cardType, context.RepairLevel, cancellationToken);
                        await this.Logger.LogResultAsync(repairResult);

                        await this.cardTypeRepository.StoreAsync(repairedCardType, cancellationToken);
                    }
                }
                else
                {
                    var cardTypes = await this.cardTypeRepository.GetAllCardTypesAsync(cancellationToken);
                    foreach (var cardType in cardTypes)
                    {
                        var (repairResult, repairedCardType) = await this.cardTypeRepairManager.RepairCardTypeAsync(cardType, context.RepairLevel, cancellationToken);
                        await this.Logger.LogResultAsync(repairResult);

                        await this.cardTypeRepository.StoreAsync(repairedCardType, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await this.Logger.LogExceptionAsync("Error repairing card types", e);
                return -3;
            }

            await this.Logger.InfoAsync($"Card types has been repaired {(hasErrors ? "with errors" : "successfully")}");

            return 0;
        }

        #endregion
    }
}
