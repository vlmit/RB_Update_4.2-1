using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared.Cards
{
    internal class TestConfigurationCardLibraryImportListener :
        CardLibraryImportListenerBase
    {
        private readonly HashSet<Guid> cardsForReimport = new();

        /// <inheritdoc/>
        public override ValueTask NotifyCardImportFinishedAsync(
            Guid cardID,
            Guid cardTypeID,
            string cardName,
            ValidationResult cardImportResult,
            CancellationToken cancellationToken = default)
        {
            var reimport = this.cardsForReimport.Contains(cardID);

            if (!reimport && cardImportResult.Items.Any(x => x.Type == ValidationResultType.Error && !CardValidationKeys.IsCardExists(x.Key)))
            {
                throw new InvalidOperationException(
                    $"{nameof(TestConfigurationCardLibraryImportListener)} error: can't import card {cardName}:{Environment.NewLine}{cardImportResult.ToString(ValidationLevel.Detailed)}");
            }
            else if (reimport && !cardImportResult.IsSuccessful)
            {
                this.cardsForReimport.Remove(cardID);

                throw new InvalidOperationException(
                    $"{nameof(TestConfigurationCardLibraryImportListener)} error during card {cardName} import:{Environment.NewLine}{cardImportResult.ToString(ValidationLevel.Detailed)}");
            }

            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override ValueTask NotifyCardWillBeDeletedAndReimportedAsync(
            Guid cardID,
            CancellationToken cancellationToken = default)
        {
            this.cardsForReimport.Add(cardID);
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override ValueTask NotifyExportedRequestReadedAsync(
            CardStoreRequest cardStoreRequest,
            ValidationResult validationResult,
            CancellationToken cancellationToken = default)
        {
            ValidationAssert.IsSuccessful(validationResult);
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public override ValueTask NotifyBundledImportFinishedAsync(
            ValidationResult validationResult,
            CancellationToken cancellationToken = default)
        {
            ValidationAssert.IsSuccessful(validationResult);
            return ValueTask.CompletedTask;
        }
    }
}
