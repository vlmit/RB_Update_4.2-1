#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared.Cards
{
    public class TestCardLibraryImportListener : CardLibraryImportListenerBase
    {
        #region Private Fields

        private readonly TestCardLibraryImportListenerInfo info;

        #endregion

        #region Constructor

        public TestCardLibraryImportListener(TestCardLibraryImportListenerInfo info)
        {
            this.info = NotNullOrThrow(info);
        }

        #endregion

        #region ICardLibraryImportListener Implementation

        public override ValueTask NotifyCardImportFinishedAsync(
            Guid cardID,
            Guid cardTypeID,
            string cardName,
            ValidationResult cardImportResult,
            CancellationToken cancellationToken = default)
        {
            this.info.ImportSequence.Add(cardID);

            var skipped = cardImportResult.Items.Any(x => x.Key == CardValidationKeys.CardIsSkippedDuringImport);
            var cardHasNotBeenModified = cardImportResult.Items.Any(x => x.Key == CardValidationKeys.CardHasNotBeenModified);

            if (cardImportResult.IsSuccessful)
            {
                if (skipped)
                {
                    this.info.Skipped++;
                }
                else if (cardHasNotBeenModified)
                {
                    this.info.NotModified++;
                }
                else
                {
                    this.info.ImportedNames.Add(cardName);
                }

                return ValueTask.CompletedTask;
            }


            this.info.Errors++;
            return ValueTask.CompletedTask;
        }

        public override ValueTask NotifyFilesForImportNotFoundAsync(
            CancellationToken cancellationToken = default)
        {
            this.info.FilesForImportNotFound = true;
            return ValueTask.CompletedTask;
        }

        #endregion
    }
}
