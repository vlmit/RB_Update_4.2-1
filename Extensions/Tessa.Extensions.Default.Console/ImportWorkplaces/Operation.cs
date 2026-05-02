using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.ConsoleApps;
using Tessa.Views.Workplaces;

namespace Tessa.Extensions.Default.Console.ImportWorkplaces
{
    /// <summary>
    /// Операция импорта рабочих мест
    /// </summary>
    public sealed class Operation : ConsoleOperation<OperationContext>
    {
        private readonly WorkplaceFilePersistent workplaceFilePersistent;

        private readonly ITessaWorkplaceService workplaceService;

        /// <inheritdoc />
        public Operation(
            IConsoleLogger logger,
            IConsoleSessionManager sessionManager,
            WorkplaceFilePersistent workplaceFilePersistent,
            ITessaWorkplaceService workplaceService)
            : base(logger, sessionManager)
        {
            this.workplaceFilePersistent = NotNullOrThrow(workplaceFilePersistent);
            this.workplaceService = NotNullOrThrow(workplaceService);
        }

        /// <inheritdoc />
        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            await this.Logger.InfoAsync("Reading workplaces from: \"{0}\"", context.Source);

            var models = (await this.workplaceFilePersistent.ReadAsync(
                NotNullOrThrow(context.Source),
                async (fileName, e, ct) =>
                {
                    await this.Logger.LogExceptionAsync(string.Format("Error reading file: \"{0}\"", fileName), e);
                    return true;
                },
                cancellationToken)).ToList();

            if (models.Count == 0)
            {
                await this.Logger.InfoAsync("No files in \"{0}\" to import.", context.Source);
                return 0;
            }

            await this.Logger.InfoAsync("Found workplaces ({0})", models.Count);
            var request = new WorkplaceImportRequest
            {
                Models = models,
                ImportViews = context.ImportViews,
                ImportRoles = context.ImportRoles,
                ImportSearchQueries = context.ImportSearchQueries,
                NeedClear = context.ClearWorkplaces
            };

            try
            {
                await this.workplaceService.ImportWorkplacesAsync(request, cancellationToken);
                await this.Logger.InfoAsync("Workplaces are imported successfully");
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await this.Logger.LogExceptionAsync("Error importing workplaces", e);
                return -1;
            }
        }
    }
}
