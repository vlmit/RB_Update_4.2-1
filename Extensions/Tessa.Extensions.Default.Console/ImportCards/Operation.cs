using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Applications.Package;
using Tessa.Cards;
using Tessa.Platform;
using Tessa.Platform.Configuration;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Data;
using Tessa.Platform.SourceProviders;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Console.ImportCards
{
    public sealed class Operation(
        IConsoleSessionManager sessionManager,
        IConsoleLogger logger,
        IConfigurationManager configurationManager,
        ICardLibraryManager cardLibraryManager)
        : ConsoleOperation<OperationContext>(logger, sessionManager, extendedInitialization: true)
    {
        #region Private Methods

        private async Task<int> ImportFilesAsync(
            string filePath,
            CardLibraryImportGlobalSettings settings,
            bool ignoreExistentCards,
            bool ignoreRepairMessages,
            CancellationToken cancellationToken)
        {
            IValidationResultBuilder validationResult = new ValidationResultBuilder();
            var successfulCardNames = new List<string>();
            var skippedCardNames = new List<string>();

            try
            {
                var listener = new CardLibraryConsoleImportListener(
                    this.Logger,
                    validationResult,
                    successfulCardNames,
                    skippedCardNames,
                    ignoreExistentCards,
                    ignoreRepairMessages);

                await cardLibraryManager.ImportAsync(
                    new CardLibraryImportItem
                    {
                        CardOrLibraryProvider = new FileSourceContentProvider(Path.GetFullPath(filePath), true)
                    },
                    settings,
                    listener,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                validationResult.AddException(this, ex);
            }

            AddImportingResultWithPreamble(validationResult, successfulCardNames, skippedCardNames);

            ValidationResult totalResult = validationResult.Build();
            await this.Logger.LogResultAsync(totalResult);

            return totalResult.IsSuccessful
                ? successfulCardNames.Count + skippedCardNames.Count
                : -1;
        }

        private static void AddImportingResultWithPreamble(
            IValidationResultBuilder validationResult,
            ICollection<string> successfulCardNames,
            ICollection<string> skippedCardNames)
        {
            if (successfulCardNames.Count > 0)
            {
                string infoText =
                    DefaultConsoleHelper.GetQuotedItemsText(
                            new StringBuilder(),
                            "UI_Cards_CardImported",
                            "UI_Cards_MultipleCardsImported",
                            successfulCardNames)
                        .ToStringAndRelease();

                validationResult.AddInfo(typeof(Operation), infoText);
            }

            if (skippedCardNames.Count > 0)
            {
                string infoText =
                    DefaultConsoleHelper.GetQuotedItemsText(
                            new StringBuilder(),
                            "UI_Cards_CardSkipped",
                            "UI_Cards_MultipleCardsSkipped",
                            skippedCardNames)
                        .ToStringAndRelease();

                validationResult.AddInfo(typeof(Operation), infoText);
            }
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task<int> ExecuteAsync(
            OperationContext context,
            CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            var configuration = configurationManager.Configuration;
            (_, ConfigurationConnection configurationConnection) = configuration.GetConfigurationDataProvider();

            DbProviderFactory factory = configuration
                .GetConfigurationDataProviderFromType(configurationConnection.DataProvider)
                .GetDbProviderFactory();

            Dbms dbms = factory.GetDbms();

            var anyCardImported = false;
            try
            {
                // если указана папка, то находим первый файл с подходящим расширением
                var libraries = new HashSet<int>();
                var contextSources = context.Sources as IReadOnlyList<string> ?? context.Sources?.ToArray() ?? [];
                var sources = contextSources.SelectMany((x, i) =>
                {
                    var sourceFiles = DefaultConsoleHelper.GetSourceFiles(
                        x,
                        "*.jcardlib",
                        throwIfNotFound: false,
                        checkPatternMatch: true);
                    if (sourceFiles.Count > 0)
                    {
                        libraries.Add(i);
                    }

                    return sourceFiles;
                }).ToList();

                sources.AddRange(contextSources
                    .Where((x, i) => !libraries.Contains(i))
                    .SelectMany(x => DefaultConsoleHelper.GetSourceFiles(x, "*.jcard", throwIfNotFound: false, checkPatternMatch: true)));

                var settings = new CardLibraryImportGlobalSettings
                {
                    Dbms = dbms,
                    FileWithIgnoreProvider = !string.IsNullOrEmpty(context.IgnoredFilesPath)
                        ? new FileSourceContentProvider(Path.GetFullPath(context.IgnoredFilesPath), true)
                        : null,
                    IgnoredFilesProvider = new FileSystemIgnoredFilesProvider(),
                    IgnoreExistentCards = context.IgnoreExistentCards,
                    GeneralMergeOptionsProvider = !string.IsNullOrEmpty(context.MergeOptionsPath)
                        ? new FileSourceContentProvider(Path.GetFullPath(context.MergeOptionsPath), true)
                        : null,
                    Bundled = context.Bundled
                };

                foreach (string source in sources)
                {
                    var extension = Path.GetExtension(source);
                    if (extension.Equals(".jcardlib", StringComparison.OrdinalIgnoreCase))
                    {
                        await this.Logger.InfoAsync("Reading card library from: \"{0}\"", source);
                    }
                    else
                    {
                        await this.Logger.InfoAsync("Importing card from: \"{0}\"", source);
                    }

                    var importResult = await this.ImportFilesAsync(
                        source,
                        settings,
                        context.IgnoreExistentCards,
                        context.IgnoreRepairMessages,
                        cancellationToken);

                    if (importResult < 0)
                    {
                        return -1;
                    }

                    anyCardImported = importResult > 0;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await this.Logger.LogExceptionAsync("Error importing cards", e);
                return -1;
            }

            if (anyCardImported)
            {
                await this.Logger.InfoAsync("Cards are imported successfully");
            }
            else
            {
                await this.Logger.InfoAsync("Cards for import aren't found.");
            }

            return 0;
        }

        #endregion
    }
}
