using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.IO;
using Tessa.Platform.Validation;
using Tessa.Views;
using Tessa.Views.Json;

namespace Tessa.Extensions.Default.Console.ExportViews
{
    public sealed class Operation :
        ConsoleOperation<OperationContext>
    {
        #region Constructors

        public Operation(
            IConsoleLogger logger,
            IConsoleSessionManager sessionManager,
            ITessaViewService viewService,
            ViewFilePersistent viewFilePersistent)
            : base(logger, sessionManager)
        {
            this.viewService = NotNullOrThrow(viewService);
            this.viewFilePersistent = NotNullOrThrow(viewFilePersistent);
        }

        #endregion

        #region Fields

        private readonly ViewFilePersistent viewFilePersistent;

        private readonly ITessaViewService viewService;

        #endregion

        #region Private Methods

        private async Task<bool> ExportViewCoreAsync(TessaViewModel tessaViewModel, string exportPath, bool groupBySubfolders)
        {
            var result = new ValidationResultBuilder();
            JsonViewMetadataHelper.ValidateViewFormat(tessaViewModel, result);
            if (result.IsSuccessful())
            {
                try
                {
                    var jsonViews = new List<IJsonViewModel>
                    {
                        new JsonViewModel
                        {
                            ID = tessaViewModel.Id,
                            Alias = tessaViewModel.Alias,
                            Caption = tessaViewModel.Caption,
                            Description = tessaViewModel.Description,
                            GroupName = tessaViewModel.GroupName,
                            JsonMetadataSource = tessaViewModel.JsonMetadataSource,
                            ModifiedByID = tessaViewModel.ModifiedById,
                            ModifiedByName = tessaViewModel.ModifiedByName,
                            ModifiedDateTime = tessaViewModel.ModifiedDateTime,
                            MsQuerySource = tessaViewModel.MsQuerySource,
                            PgQuerySource = tessaViewModel.PgQuerySource,
                            Roles = tessaViewModel.Roles
                        }
                    };

                    await this.viewFilePersistent.WriteJsonAsync(jsonViews, exportPath, groupBySubfolders: groupBySubfolders);
                }
                catch (Exception ex)
                {
                    await this.Logger.LogExceptionAsync(null, ex);
                    await this.Logger.InfoAsync("Skipped view \"{0}\".", tessaViewModel.Alias);
                    return false;
                }

                await this.Logger.InfoAsync("Saved view \"{0}\".", tessaViewModel.Alias);
                return true;
            }

            await this.Logger.ErrorAsync(result.ToString());
            await this.Logger.InfoAsync("Skipped view \"{0}\".", tessaViewModel.Alias);
            return false;
        }

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
            int notFoundCount = 0;
            int skippedCount = 0;

            try
            {
                string exportPath = DefaultConsoleHelper.NormalizeFolderAndCreateIfNotExists(context.OutputFolder);
                if (string.IsNullOrEmpty(exportPath))
                {
                    exportPath = Directory.GetCurrentDirectory();
                }

                if (context.ClearOutputFolder)
                {
                    await this.Logger.InfoAsync("Removing existent views from output folder \"{0}\"", exportPath);
                    FileHelper.DeleteFilesByPatterns(exportPath, includeSubfolders: true, "*.jview");
                }

                await this.Logger.InfoAsync("Loading views from service...");
                var views = await this.viewService.GetModelsAsync(new ViewGetRequest { WithRoles = true }, cancellationToken);

                if (context.ViewAliasesOrIdentifiers is null || context.ViewAliasesOrIdentifiers.Count == 0)
                {
                    await this.Logger.InfoAsync("Exporting all views to folder \"{0}\".", exportPath);

                    foreach (TessaViewModel view in views.OrderBy(x => x.Alias))
                    {
                        if (await this.ExportViewCoreAsync(view, exportPath, context.GroupBySubfolders))
                        {
                            exportedCount++;
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                }
                else
                {
                    await this.Logger.InfoAsync(
                        "Exporting views to folder \"{0}\": {1}.",
                        exportPath,
                        string.Join(", ", context.ViewAliasesOrIdentifiers.Select(name => $"\"{name}\"")));

                    var viewsByID = new Dictionary<Guid, TessaViewModel>(views.Count);
                    var viewsByAlias = new Dictionary<string, TessaViewModel>(views.Count, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < views.Count; i++)
                    {
                        viewsByID[views[i].Id] = views[i];
                        viewsByAlias[views[i].Alias] = views[i];
                    }

                    foreach (string aliasOrIdentifier in context.ViewAliasesOrIdentifiers)
                    {
                        if (viewsByAlias.TryGetValue(aliasOrIdentifier, out TessaViewModel? view)
                            || Guid.TryParse(aliasOrIdentifier, out Guid viewID)
                            && viewsByID.TryGetValue(viewID, out view))
                        {
                            if (await this.ExportViewCoreAsync(view, exportPath, context.GroupBySubfolders))
                            {
                                exportedCount++;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                        else
                        {
                            await this.Logger.ErrorAsync("View \"{0}\" isn't found.", aliasOrIdentifier);
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
                await this.Logger.LogExceptionAsync("Error exporting views.", e);
                return -1;
            }

            // количество экспортированных выводим как при наличии, так и при отсутствии ошибок
            if (exportedCount > 0)
            {
                await this.Logger.InfoAsync("Views ({0}) are exported successfully.", exportedCount);
            }

            if (skippedCount > 0)
            {
                await this.Logger.InfoAsync("Views ({0}) are skipped.", skippedCount);
            }

            if (notFoundCount != 0)
            {
                await this.Logger.ErrorAsync("Views ({0}) aren't found by provided aliases or identifiers.", notFoundCount);
            }
            else if (exportedCount == 0 && skippedCount == 0)
            {
                await this.Logger.InfoAsync("No views to export.");
            }

            return skippedCount == 0 ? 0 : -1;
        }

        #endregion
    }
}
