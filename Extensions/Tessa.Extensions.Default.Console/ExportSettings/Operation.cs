using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Css;
using SharpCompress.Common;
using Tessa.Platform.Collections;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.IO;
using Tessa.Platform.Validation;
using Tessa.Platform.Web;
using Tessa.SettingsUnits;

namespace Tessa.Extensions.Default.Console.ExportSettings
{
    public sealed class Operation(
        IConsoleLogger logger,
        IConsoleSessionManager sessionManager,
        IWebProxyFactory webProxyFactory,
        ISettingsUnitRepository settingsUnitRepository,
        ISettingsUnitDescriptorRegistry settingsUnitDescriptorRegistry)
        : ConsoleOperation<OperationContext>(logger, sessionManager)
    {
        #region Fields

        private readonly IWebProxyFactory webProxyFactory = NotNullOrThrow(webProxyFactory);
        private readonly ISettingsUnitRepository settingsUnitRepository = NotNullOrThrow(settingsUnitRepository);
        private readonly ISettingsUnitDescriptorRegistry settingsUnitDescriptorRegistry = NotNullOrThrow(settingsUnitDescriptorRegistry);

        #endregion

        #region Base Overrides

        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            try
            {
                var statistics = new Statistics();
                var exportPath = await this.PrepareOutputFolderAsync(context);

                await using var proxy = await this.webProxyFactory.UseProxyAsync<SettingsUnitEditorWebProxy>(cancellationToken: cancellationToken);

                if (context.UnitNames is { Count: > 0 })
                {
                    var names = ParseUnitNames(context);
                    await this.ExportByNamesAsync(proxy, exportPath, statistics, names, cancellationToken);
                }
                else
                {
                    await this.ExportAllAsync(proxy, exportPath, statistics, cancellationToken);
                }

                await statistics.LogSummaryAsync(this.Logger);
                return statistics.Error > 0 ? -2 : 0;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.Logger.LogExceptionAsync("Error exporting settings", ex);
                return -1;
            }
        }

        #endregion

        #region Private Methods

        private async ValueTask<string> PrepareOutputFolderAsync(OperationContext context)
        {
            var exportPath = DefaultConsoleHelper.NormalizeFolderAndCreateIfNotExists(context.OutputFolder);
            exportPath = string.IsNullOrEmpty(exportPath) ? Directory.GetCurrentDirectory() : exportPath;

            if (context.ClearOutputFolder)
            {
                await this.Logger.InfoAsync("Removing existent settings from output folder \"{0}\"", exportPath);
                FileHelper.DeleteFilesByPatterns(exportPath, true, "*.jsettings");
            }

            return exportPath;
        }

        private static IReadOnlyCollection<string> ParseUnitNames(OperationContext context) => string
            .Join(" ", context.UnitNames ?? [])
            .Split([' ', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .AsReadOnlyCollection();

        private async Task ExportAllAsync(SettingsUnitEditorWebProxy proxy, string exportPath, Statistics statistics, CancellationToken cancellationToken)
        {
            await this.Logger.InfoAsync("Exporting all settings to folder \"{0}\"", exportPath);

            await this.Logger.InfoAsync("Loading information about settings units from service...");
            var unitRecordIDs = await this.settingsUnitRepository.GetRecordsIDsAsync(new(Fragment: false), cancellationToken: cancellationToken);

            foreach (var unitRecordID in unitRecordIDs)
            {
                if (!this.settingsUnitDescriptorRegistry.TryGet(unitRecordID, out var descriptor))
                {
                    statistics.RegisterError();
                    await this.Logger.ErrorAsync("Not found descriptor for settings unit with UnitID=\"{0:B}\"", unitRecordID);
                    continue;
                }

                await this.ExportAsync(proxy, exportPath, statistics, descriptor, fragmentName: null, cancellationToken);
            }
        }

        private async Task ExportByNamesAsync(SettingsUnitEditorWebProxy proxy, string exportPath, Statistics statistics, IReadOnlyCollection<string> names, CancellationToken cancellationToken)
        {
            var displayNames = string.Join(", ", names.Select(x => $"\"{x}\""));
            await this.Logger.InfoAsync("Exporting settings units to folder \"{0}\": {1}", exportPath, displayNames);

            foreach (var name in names)
            {
                var settingNames = name.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var unitName = settingNames.ElementAtOrDefault(0);
                var fragmentName = settingNames.ElementAtOrDefault(1);

                if (!this.settingsUnitDescriptorRegistry.TryGet(unitName, out var descriptor))
                {
                    statistics.RegisterError();
                    await this.Logger.ErrorAsync("Not found descriptor for settings unit with UnitName=\"{0}\"", unitName);
                    continue;
                }

                await this.ExportAsync(proxy, exportPath, statistics, descriptor, fragmentName, cancellationToken);
            }
        }

        private async Task ExportAsync(SettingsUnitEditorWebProxy proxy, string exportPath, Statistics statistics, string unitName, string? fragmentName, CancellationToken cancellationToken)
        {
            var fileName = fragmentName is not null ? $"{unitName}.{fragmentName}" : unitName;
            var filePath = Path.Combine(exportPath, $"{FileHelper.RemoveInvalidFileNameChars(fileName)}.jsettings");

            try
            {
                await this.Logger.InfoAsync("Exporting settings unit to {0}", filePath);

                await using var stream = await proxy.ExportUnitAsync(unitName, fragmentName, cancellationToken);
                await using var output = FileHelper.OpenWrite(filePath, bufferSize: FileHelper.DefaultFileBufferSize);
                await stream.CopyToAsync(output, FileHelper.DefaultCopyBufferSize, cancellationToken);
                statistics.RegisterSuccess();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                statistics.RegisterError();
                await this.Logger.LogExceptionAsync($"Failed to export settings unit to {filePath}", ex);
            }
        }

        #endregion
    }
}
