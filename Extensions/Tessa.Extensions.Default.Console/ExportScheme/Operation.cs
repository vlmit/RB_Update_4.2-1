using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.ConsoleApps;
using Tessa.Scheme;
using Tessa.Scheme.Differences;

namespace Tessa.Extensions.Default.Console.ExportScheme
{
    public sealed class Operation : ConsoleOperation<OperationContext>
    {
        private readonly ISchemeService schemeService;

        public Operation(
            IConsoleSessionManager sessionManager,
            IConsoleLogger logger,
            ISchemeService schemeService)
            : base(logger, sessionManager)
        {
            this.schemeService = schemeService;
        }

        /// <inheritdoc />
        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            try
            {
                await this.Logger.InfoAsync("Reading scheme from web service");

                SchemeDatabase tessaDatabase = new(SchemeDatabaseNames.Original);
                await tessaDatabase.RefreshAsync(this.schemeService, cancellationToken);

                string exportPath = DefaultConsoleHelper.NormalizeFolderAndCreateIfNotExists(context.OutputFolder);
                if (string.IsNullOrEmpty(exportPath))
                {
                    exportPath = Directory.GetCurrentDirectory();
                }

                string tsdFilePath = Directory.EnumerateFiles(exportPath, "*.tsd").MinBy(x => x)
                    ?? Path.Combine(exportPath, "Platform.tsd");

                await this.Logger.InfoAsync("Reading scheme from file \"{0}\"", tsdFilePath);

                string[] partitions = FileSchemeService.GetPartitionPaths(tsdFilePath);
                var fileSchemeService = new FileSchemeService(tsdFilePath, partitions);

                if (!await fileSchemeService.IsStorageExistsAsync(cancellationToken))
                {
                    await this.Logger.InfoAsync("Scheme doesn't exist in the file folder, creating it...");
                    await fileSchemeService.CreateStorageAsync(cancellationToken);
                }

                if (!await fileSchemeService.IsStorageUpToDateAsync(cancellationToken))
                {
                    await this.Logger.InfoAsync("Scheme isn't up-to-date in the file folder, upgrading it...");
                    await fileSchemeService.UpdateStorageAsync(cancellationToken);
                }

                await this.Logger.InfoAsync("Exporting the scheme using web service to folder \"{0}\"", exportPath);
                var sq = new SchemeSubmittingQueue(tessaDatabase, fileSchemeService);
                try
                {
                    await sq.RefreshAsync(cancellationToken);
                    await sq.SubmitAsync(cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException && sq.FaultedItem is not null)
                {
                    // ignored
                }

                if (sq.FaultedItem is { } faultedItem)
                {
                    var message = SchemeHelper.GetFaultedItemMessage(sq.FaultedItem);
                    if (faultedItem.Exception is { } ex)
                    {
                        await this.Logger.LogExceptionAsync(message, ex);
                    }
                    else
                    {
                        await this.Logger.ErrorAsync(message);
                    }

                    return -1;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await this.Logger.LogExceptionAsync("Error exporting scheme", e);
                return -1;
            }

            await this.Logger.InfoAsync("Scheme has been exported successfully");
            return 0;
        }
    }
}
