using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.ImportLocalization
{
    public sealed class Operation :
        ConsoleOperation<OperationContext>
    {
        #region Constructors

        public Operation(
            IConsoleSessionManager sessionManager,
            IConsoleLogger logger,
            [Dependency(nameof(LocalizationServiceClient))]
            ILocalizationService localizationService)
            : base(logger, sessionManager)
        {
            this.localizationService = localizationService;
        }

        #endregion

        #region Fields

        private readonly ILocalizationService localizationService;

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            await this.Logger.InfoAsync("Reading localization from: \"{0}\"", context.Source);

            try
            {
                // сначала читаем все библиотеки локализации из файлов, чтобы убедиться, что там нет ошибок
                var libraries = new List<LocalizationLibrary>();

                List<string> fileNames = DefaultConsoleHelper.GetSourceFiles(context.Source, "*.jlocalization", throwIfNotFound: false);

                var jsonFileLocalizationService = new JsonFileLocalizationService(fileNames);
                libraries.AddRange(await jsonFileLocalizationService.GetLibrariesAsync(returnComments: true, cancellationToken: cancellationToken));

                if (libraries.Count == 0)
                {
                    throw new FileNotFoundException($"Couldn't locate *.jlocalization files in \"{context.Source}\"", context.Source);
                }

                var libraryNames = string.Join(", ", libraries.Select(l => $"\"{l.Name}\""));
                await this.Logger.InfoAsync($"Importing libraries{(context.ClearLibraries ? " (with removing existent ones)" : string.Empty)}: {libraryNames}");

                await this.localizationService.ImportLibrariesAsync(libraries, context.ClearLibraries, cancellationToken);

                await this.Logger.InfoAsync("Libraries are saved");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await this.Logger.LogExceptionAsync("Error importing localizations", e);
                return -1;
            }

            await this.Logger.InfoAsync("Localizations are imported successfully");
            return 0;
        }

        #endregion
    }
}
