using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.IO;
using Tessa.Views;
using Tessa.Views.SearchQueries;
using Tessa.Views.Workplaces;

namespace Tessa.Extensions.Default.Console.ExportWorkplaces
{
    public sealed class Operation(
            IConsoleLogger logger,
            IConsoleSessionManager sessionManager,
            ITessaWorkplaceService workplaceService,
            WorkplaceFilePersistent workplaceFilePersistent,
            ITessaViewService viewService,
            IViewRepository viewRepository)
        // расширенная инициализация нужна для корректной локализации имён выгружаемых рабочих мест
        : ConsoleOperation<OperationContext>(logger, sessionManager, extendedInitialization: true)
    {
        #region Fields

        private readonly ITessaWorkplaceService workplaceService = NotNullOrThrow(workplaceService);

        private readonly WorkplaceFilePersistent workplaceFilePersistent = NotNullOrThrow(workplaceFilePersistent);

        private readonly ITessaViewService viewService = NotNullOrThrow(viewService);

        private readonly IViewRepository viewRepository = NotNullOrThrow(viewRepository);

        #endregion

        #region Private Methods

        private async ValueTask<bool> ExportWorkplaceCoreAsync(
            WorkplaceModel workplace,
            string exportPath,
            bool includeViews,
            bool includeSearchQueries,
            IEnumerable<TessaViewModel>? availableViews,
            IReadOnlyList<ISearchQueryMetadata>? availableQueries,
            CancellationToken cancellationToken = default)
        {
            var result = await this.workplaceFilePersistent.WriteAsync(
                workplace,
                exportPath,
                includeViews,
                includeSearchQueries,
                availableViews,
                availableQueries,
                cancellationToken);

            if (result.IsSuccessful)
            {
                await this.Logger.InfoAsync("Saving workplace \"{0}\".", GetWorkplaceDisplayName(workplace));
                return true;
            }
            else
            {
                await this.Logger.InfoAsync("Cannot export workplace \"{0}\".", GetWorkplaceDisplayName(workplace));
                await this.Logger.ErrorAsync(result.ToString());
                return false;
            }
        }

        private static string GetWorkplaceDisplayName(WorkplaceModel workplace) =>
            LocalizeOrGetName(
                workplace.Name,
                LocalizationManager.EnglishCultureInfo);

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            int exportedCount = 0;
            int errorCount = 0;
            int notFoundCount = 0;

            List<WorkplaceModel> workplaces;

            try
            {
                string exportPath = DefaultConsoleHelper.NormalizeFolderAndCreateIfNotExists(context.OutputFolder);
                if (string.IsNullOrEmpty(exportPath))
                {
                    exportPath = Directory.GetCurrentDirectory();
                }

                if (context.ClearOutputFolder)
                {
                    await this.Logger.InfoAsync("Removing existent workplaces from output folder \"{0}\"", exportPath);
                    FileHelper.DeleteFilesByPatterns(exportPath, includeSubfolders: false, "*.jworkplace");
                }

                await this.Logger.InfoAsync("Loading workplaces from service...");
                workplaces = await this.workplaceService.GetModelsAsync(new WorkplaceGetRequest { WithRoles = true }, cancellationToken);

                string? optionsSuffix = null;
                if (context.IncludeViews)
                {
                    optionsSuffix += ", include views";
                }

                if (context.IncludeSearchQueries)
                {
                    optionsSuffix += ", include search queries";
                }

                var availableViews =
                    context.IncludeViews
                        ? await this.viewRepository.GetAsync(new ViewGetRequest { WithRoles = true }, cancellationToken)
                        : null;

                var availableQueries =
                    context.IncludeSearchQueries ? await this.viewService.GetCurrentUserSearchQueriesAsync(cancellationToken) : null;

                if (context.WorkplaceNamesOrIdentifiers is null || context.WorkplaceNamesOrIdentifiers.Count == 0)
                {
                    await this.Logger.InfoAsync("Exporting all workplaces to folder \"{0}\"{1}.", exportPath, optionsSuffix);

                    var workplaceModels = workplaces.OrderBy(x => x.ID);

                    foreach (WorkplaceModel workplace in workplaceModels)
                    {
                        var exportResult = await this.ExportWorkplaceCoreAsync(
                            workplace,
                            exportPath,
                            context.IncludeViews,
                            context.IncludeSearchQueries,
                            availableViews,
                            availableQueries,
                            cancellationToken);
                        if (exportResult)
                        {
                            exportedCount++;
                        }
                        else
                        {
                            errorCount++;
                        }
                    }
                }
                else
                {
                    await this.Logger.InfoAsync(
                        "Exporting workplaces to folder \"{0}\"{1}: {2}.",
                        exportPath,
                        optionsSuffix,
                        string.Join(", ", context.WorkplaceNamesOrIdentifiers.Select(name => $"\"{name}\"")));

                    var workplacesByID = new Dictionary<Guid, WorkplaceModel>(workplaces.Count);
                    var workplacesByName = new Dictionary<string, WorkplaceModel>(workplaces.Count * 2, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < workplaces.Count; i++)
                    {
                        string displayName = GetWorkplaceDisplayName(workplaces[i]);

                        // также запишем идентификатор для поиска по нему
                        workplacesByID[workplaces[i].ID] = workplaces[i];

                        // запишем как нелокализованное, так и локализованное имя, которые могут даже совпасть
                        workplacesByName[workplaces[i].Name] = workplaces[i];
                        workplacesByName[displayName] = workplaces[i];
                    }

                    foreach (string nameOrIdentifier in context.WorkplaceNamesOrIdentifiers)
                    {
                        if (workplacesByName.TryGetValue(nameOrIdentifier, out WorkplaceModel? workplace)
                            || Guid.TryParse(nameOrIdentifier, out Guid workplaceID)
                            && workplacesByID.TryGetValue(workplaceID, out workplace))
                        {
                            var exportResult = await this.ExportWorkplaceCoreAsync(
                                workplace,
                                exportPath,
                                context.IncludeViews,
                                context.IncludeSearchQueries,
                                availableViews,
                                availableQueries,
                                cancellationToken);
                            if (exportResult)
                            {
                                exportedCount++;
                            }
                            else
                            {
                                errorCount++;
                            }
                        }
                        else
                        {
                            await this.Logger.ErrorAsync("Workplace \"{0}\" isn't found.", nameOrIdentifier);
                            notFoundCount++;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await this.Logger.LogExceptionAsync("Error exporting workplaces.", e);
                return -1;
            }

            if (exportedCount > 0)
            {
                await this.Logger.InfoAsync("Workplaces ({0}) are exported successfully.", exportedCount);
            }

            if (errorCount > 0)
            {
                await this.Logger.InfoAsync("Workplaces ({0}) failed to export.", errorCount);
            }

            if (notFoundCount != 0)
            {
                await this.Logger.ErrorAsync("Workplaces ({0}) aren't found by provided names or identifiers.", notFoundCount);
            }
            else if (workplaces.Count == 0)
            {
                await this.Logger.InfoAsync("No workplaces to export.");
            }

            return errorCount > 0 ? -1 : 0;
        }

        #endregion
    }
}
