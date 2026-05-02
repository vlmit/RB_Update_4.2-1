using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.IO;
using Tessa.Platform.Json;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Scheme;
using Tessa.Views;
using Tessa.Views.Json;
using Tessa.Views.Metadata;
using Tessa.Views.Workplaces.Json;

namespace Tessa.Extensions.Default.Console.ConvertConfiguration
{
    public sealed class Operation(
        IConsoleLogger logger,
        IConsoleSessionManager sessionManager,
        IJsonViewModelExporter jsonViewModelExporter,
        IViewServiceImplementer viewServiceImplementer,
        IJsonViewModelUpgrader jsonViewModelUpgrader)
        : ConsoleOperation<OperationContext>(logger, sessionManager)
    {
        #region Private Fields

        private readonly IJsonViewModelExporter jsonViewModelExporter = NotNullOrThrow(jsonViewModelExporter);

        private readonly IJsonViewModelUpgrader jsonViewModelUpgrader = NotNullOrThrow(jsonViewModelUpgrader);

        private readonly IViewServiceImplementer viewServiceImplementer = NotNullOrThrow(viewServiceImplementer);

        private SchemeTableCollection? schemeTables;

        #endregion

        #region Private Methods

        private async ValueTask<(bool, string)> ConvertItemsAsync(
            string sourcePath,
            string targetPath,
            string? schemePath,
            bool doNotDelete,
            ConversionMode conversionMode,
            CancellationToken cancellationToken = default)
        {
            // Подменяем источник данных в поставщике представлений, чтоб он не пытался вытягивать их через сессию.
            if (this.viewServiceImplementer is IViewServiceInitializer viewServiceInitializer)
            {
                viewServiceInitializer.Initialize(new List<IViewMetadata>());
            }

            var items = new List<ConversionItem>();

            var attr = File.GetAttributes(sourcePath);
            var sourcePathIsDirectory = (attr & FileAttributes.Directory) == FileAttributes.Directory;
            var sourceFiles = DefaultConsoleHelper.GetSourceFiles(sourcePath, "*.*", false);

            // Если есть типы, то для конвертации понадобится файловая схема,
            // файл схемы должен быть передан либо в опциональном аргументе, либо присутствовать в обрабатываемой папке.
            if (sourceFiles.Any(x => Path.GetExtension(x).Equals(".jtype", StringComparison.OrdinalIgnoreCase)))
            {
                if (string.IsNullOrWhiteSpace(schemePath))
                {
                    schemePath = sourceFiles.FirstOrDefault(x => Path.GetExtension(x).Equals(".tsd", StringComparison.OrdinalIgnoreCase));
                }
                else if (File.GetAttributes(schemePath).HasFlag(FileAttributes.Directory))
                {
                    // Если передана папка
                    var schemePathDirectoryFiles = DefaultConsoleHelper.GetSourceFiles(schemePath, "*.tsd", false);
                    schemePath = schemePathDirectoryFiles.FirstOrDefault();
                }

                if (string.IsNullOrWhiteSpace(schemePath))
                {
                    throw new InvalidOperationException(
                        "There are card types (\".jtype\" files) for convert but no scheme files has been found. Provide path to scheme folder or \".tsd\" file by argument \"-scheme:database.tsd\", or place scheme files inside \"source\" directory.");
                }

                var schemeFullPath = Path.GetFullPath(schemePath);
                var fileSchemeService = new FileSchemeService(
                    schemeFullPath,
                    DefaultConsoleHelper.GetSchemePartitions(schemeFullPath));

                await fileSchemeService.UpdateStorageAsync(cancellationToken);

                SchemeDatabase tessaDatabase = new(SchemeDatabaseNames.Original);
                await tessaDatabase.RefreshAsync(fileSchemeService, cancellationToken);

                this.schemeTables = tessaDatabase.Tables;
            }

            foreach (var sourceFilePath in sourceFiles)
            {
                // Имя директории может быть с точкой, тогда Path.GetDirectoryName() даст ошибочный результат, отбросив
                // часть пути, на самом деле являющегося директорией, поэтому выше проверяется через файловую систему
                // является ли sourcePath директорией.
                var relativeFilePath =
                    sourcePathIsDirectory
                        ? Path.GetRelativePath(sourcePath, sourceFilePath)
                        : Path.GetRelativePath(Path.GetDirectoryName(sourcePath) ?? string.Empty, sourceFilePath);

                var targetFilePath = Path.Combine(targetPath, relativeFilePath);
                var extension = Path.GetExtension(sourceFilePath).ToLowerInvariant();
                ConversionItem? item;

                try
                {
                    switch (conversionMode)
                    {
                        case ConversionMode.Upgrade:
                            item = await this.ReadForConvertAsync(extension, sourceFilePath, targetFilePath,
                                cancellationToken);
                            break;

                        case ConversionMode.LF:
                        case ConversionMode.CRLF:
                        case ConversionMode.BOM:
                            switch (extension)
                            {
                                case ".jcardlib": // библиотека карточек в json
                                case ".jcard": // карточка в json
                                case ".json": // произвольный текстовый json, например, app.json
                                case ".jlocalization": // библиотека локализации в json
                                case ".jtype": // тип карточки в json
                                case ".jview": // представление в json
                                case ".jworkplace": // рабочее место в json
                                case ".jquery": // поисковый запрос в json
                                case ".sql": // sql-скрипт с процедурой, функцией или миграцией
                                case ".tpf": // функция схемы в xml
                                case ".tpm": // миграция схемы в xml
                                case ".tpp": // процедура схемы в xml
                                case ".tsd": // база данных схемы в xml
                                case ".tsp": // библиотека схемы в xml
                                case ".tst": // таблица схемы в xml
                                case ".txt": // текстовые файлы вида readme.txt
                                case ".xml": // произвольный текстовый xml, например, extensions.xml
                                    // только наши текстовые файлы, не трогаем бинарные .card, и другие файлы (например, файлы реестра .reg)
                                    item = new ConversionItem(sourceFilePath, targetFilePath, null);
                                    break;

                                default:
                                    item = null;
                                    break;
                            }

                            break;

                        default:
                            throw ArgumentOutOfRange(conversionMode);
                    }
                }
                catch (Exception ex)
                {
                    await this.Logger.LogExceptionAsync($"Error when loading file \"{sourceFilePath}\"", ex);
                    item = null;
                }

                if (item is not null)
                {
                    items.Add(item);
                }
            }

            var errorCount = 0;

            if (items.Count > 0)
            {
                await this.Logger.InfoAsync("Converting configuration files ({0})", items.Count);

                foreach (var item in items)
                {
                    var itemWasConverted = true;
                    var targetDirectoryName = Path.GetDirectoryName(item.NewPath);
                    if (string.IsNullOrEmpty(targetDirectoryName))
                    {
                        itemWasConverted = false;
                        errorCount++;
                        await this.Logger.ErrorAsync("Can't get target directory name from: \"{0}\"", item.NewPath);
                    }
                    else
                    {
                        try
                        {
                            // Подготовить директорию для target
                            FileHelper.CreateDirectoryIfNotExists(targetDirectoryName, true);

                            switch (item.Object)
                            {
                                case null: // преобразование переводов строк
                                    var text = await File.ReadAllTextAsync(item.OldPath, Encoding.UTF8, cancellationToken);

                                    if (conversionMode == ConversionMode.BOM)
                                    {
                                        await File.WriteAllTextAsync(item.NewPath, text.NormalizeLineEndingsUnixStyle(), Encoding.UTF8, cancellationToken);
                                        await this.Logger.InfoAsync("Line endings are converted: \"{0}\"", item.NewPath);
                                        break;
                                    }

                                    var newText = conversionMode == ConversionMode.LF
                                        ? text.NormalizeLineEndingsUnixStyle()
                                        : text.NormalizeLineEndingsWindowsStyle();

                                    if (!string.Equals(text, newText, StringComparison.Ordinal))
                                    {
                                        await File.WriteAllTextAsync(item.NewPath, newText, Encoding.UTF8, cancellationToken);
                                        await this.Logger.InfoAsync("Line endings are converted: \"{0}\"", item.NewPath);
                                    }

                                    break;

                                case CardType cardType:
                                    var typeText = await cardType.SerializeToJsonAsync(indented: true, cancellationToken);
                                    await File.WriteAllTextAsync(item.NewPath, typeText, Encoding.UTF8, cancellationToken);
                                    await this.Logger.InfoAsync("Type is converted: \"{0}\"", item.NewPath);
                                    break;

                                case LocalizationLibrary localizationLibrary:
                                    if (conversionMode == ConversionMode.Upgrade)
                                    {
                                        var localizationService = new JsonFileLocalizationService(targetDirectoryName);
                                        await localizationService.SaveLibraryAsync(localizationLibrary, item.NewPath, cancellationToken);
                                    }

                                    await this.Logger.InfoAsync("Localization library is converted: \"{0}\"", item.NewPath);
                                    break;

                                case JsonWorkplace:
                                    var jWorkplaceFileName = await this.ConvertJWorkplaceAsync(item.OldPath, item.NewPath, cancellationToken);
                                    if (!string.IsNullOrWhiteSpace(jWorkplaceFileName))
                                    {
                                        await this.Logger.InfoAsync("Json workplace is converted: \"{0}\"", jWorkplaceFileName);
                                    }
                                    else
                                    {
                                        itemWasConverted = false;
                                        errorCount++;
                                        await this.Logger.InfoAsync("Cannot convert json workplace: \"{0}\"", item.OldPath);
                                    }

                                    break;

                                case JsonSearchQueryMetadata:
                                    var jSearchQueryFileName = await this.ConvertJSearchQueryAsync(item.OldPath, item.NewPath, cancellationToken);
                                    if (!string.IsNullOrWhiteSpace(jSearchQueryFileName))
                                    {
                                        await this.Logger.InfoAsync("Json search query is converted: \"{0}\"", jSearchQueryFileName);
                                    }
                                    else
                                    {
                                        itemWasConverted = false;
                                        errorCount++;
                                        await this.Logger.InfoAsync("Cannot convert json search query: \"{0}\"", item.OldPath);
                                    }

                                    break;

                                case JsonViewModel:
                                    var (viewResult, jViewFileName) = await this.ConvertJViewAsync(item.OldPath, item.NewPath, cancellationToken);
                                    if (!viewResult.IsSuccessful)
                                    {
                                        itemWasConverted = false;
                                        errorCount++;
                                        await this.Logger.InfoAsync("Cannot convert json view: \"{0}\"", item.OldPath);
                                        await this.Logger.ErrorAsync(viewResult.ToString());
                                    }
                                    else if (string.IsNullOrEmpty(jViewFileName))
                                    {
                                        await this.Logger.InfoAsync(
                                            "Skipped. Json view: \"{0}\" is no need to convert and output path equals source.",
                                            item.OldPath);
                                    }
                                    else
                                    {
                                        await this.Logger.InfoAsync("Json view is converted: \"{0}\"", jViewFileName);
                                    }

                                    break;

                                case CardLibrary cardLibrary:
                                    if (LibraryHasLegacyItems(cardLibrary))
                                    {
                                        throw new InvalidOperationException(
                                            $"Card Library \"{item.OldPath}\" has legacy items. Conversion in not supported in this version.");
                                    }

                                    var libraryText = await cardLibrary.SerializeToJsonAsync(true, cancellationToken);

                                    await File.WriteAllTextAsync(
                                        item.NewPath,
                                        libraryText,
                                        Encoding.UTF8,
                                        cancellationToken);

                                    await this.Logger.InfoAsync("Card library is converted: \"{0}\"", item.NewPath);
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            itemWasConverted = false;
                            errorCount++;
                            await this.Logger.LogExceptionAsync($"Error when converting file \"{item.OldPath}\"", ex);

                            // Удалить созданную target directory если в ней ничего нет.
                            if (Directory.Exists(targetDirectoryName)
                                && !Directory.EnumerateFileSystemEntries(targetDirectoryName).Any())
                            {
                                Directory.Delete(targetDirectoryName);
                            }
                        }
                    }

                    // Удалить файл-источник если: нет флага "Не удалять", объект успешно сконвертирован и путь назначения отличается от источника. 
                    if (!doNotDelete
                        && itemWasConverted
                        && item.OldPath != item.NewPath)
                    {
                        FileHelper.DeleteFileSafe(item.OldPath);
                    }
                }
            }

            return (errorCount == 0, $"{items.Count - errorCount} of {items.Count} files were converted successfully.");
        }

        private static bool LibraryHasLegacyItems(CardLibrary cardLibrary) =>
            cardLibrary.Items.Any(x => x.Path?.EndsWith(".card") == true || x.Path?.EndsWith(".cardlib") == true);

        private async ValueTask<(ValidationResult, string?)> ConvertJViewAsync(string itemOldPath, string itemNewPath, CancellationToken cancellationToken)
        {
            IJsonViewModel? jsonViewModel;
            await using (TessaJsonSerializationContext.Create(new TessaJsonSerializationContext()))
            {
                await using var readStream = FileHelper.OpenRead(itemOldPath);
                var reader = new TextPartReader(readStream);
                var text = await reader.ReadAsync(cancellationToken);
                jsonViewModel = text.FromJsonString<JsonViewModel>();
            }

            var result = new ValidationResultBuilder();
            if (jsonViewModel is null or { JsonMetadataSource: null })
            {
                result.AddError($"Can't create {nameof(JsonViewModel)} from json string.");
                return (result.Build(), null);
            }

            var upgraded = await this.jsonViewModelUpgrader.UpgradeAsync(jsonViewModel, repairTypes: false, result, jsonIndented: true, cancellationToken);
            if (!result.IsSuccessful())
            {
                return (result.Build(), null);
            }

            // Если изменений нет и путь назначения равен источнику.
            if (!upgraded && itemOldPath == itemNewPath)
            {
                return (result.Build(), string.Empty);
            }

            await using var writeStream = FileHelper.Create(itemNewPath);
            // TessaJsonSerializationContext is created in exporter
            await this.jsonViewModelExporter.ExportAsync(jsonViewModel, writeStream, CancellationToken.None);

            return (result.Build(), itemNewPath);
        }

        private async ValueTask<string?> ConvertJWorkplaceAsync(string oldPath, string newPath, CancellationToken cancellationToken)
        {
            JsonWorkplaceModel? jsonWorkplaceModel;
            await using (TessaJsonSerializationContext.Create(new TessaJsonSerializationContext()))
            {
                await using var readStream = FileHelper.OpenRead(oldPath);
                var reader = new TextPartReader(readStream);
                var text = await reader.ReadAsync(cancellationToken);
                jsonWorkplaceModel = text.FromJsonString<JsonWorkplaceModel>();
            }

            if (jsonWorkplaceModel is null)
            {
                return null;
            }

            if (jsonWorkplaceModel.Content is not { Metadata.FormatVersion: < TessaJsonSerializationContext.WorkplaceJsonVersion } jsonWorkplace)
            {
                return oldPath;
            }

            var formatVersion = jsonWorkplace.Metadata.FormatVersion;
            if (formatVersion < TessaJsonSerializationContext.MinimalSupportedWorkplaceJsonVersion)
            {
                await this.Logger.ErrorAsync(
                    "Workplace has unsupported format version {0}, requires at least version {1}: \"{2}\". Please, convert the file using tools version from a previous release.",
                    formatVersion,
                    TessaJsonSerializationContext.MinimalSupportedWorkplaceJsonVersion,
                    oldPath);
                return null;
            }

            await this.Logger.WriteLineAsync($"Upgrading JSON format metadata for {Path.GetFileName(oldPath)}.");

            jsonWorkplace.Metadata.FormatVersion = TessaJsonSerializationContext.WorkplaceJsonVersion;

            var viewUpgradeError = false;
            foreach (var jsonViewModel in jsonWorkplaceModel.Views)
            {
                var result = new ValidationResultBuilder();
                await this.Logger.WriteLineAsync($"Upgrading metadata format for included view {jsonViewModel.Alias}.");
                await this.jsonViewModelUpgrader.UpgradeAsync(jsonViewModel, repairTypes: false, result, jsonIndented: true, cancellationToken);
                if (!result.IsSuccessful())
                {
                    viewUpgradeError = true;
                    await this.Logger.WriteLineAsync(result.ToString());
                }
            }

            if (viewUpgradeError)
            {
                return null;
            }

            if (jsonWorkplaceModel.Content?.Metadata is { } metadata)
            {
                metadata.FormatVersion = TessaJsonSerializationContext.WorkplaceJsonVersion;
            }

            jsonWorkplaceModel.PrepareForExport();

            await using (TessaJsonSerializationContext.Create(new TessaJsonSerializationContext()))
            {
                await using var writeStream = FileHelper.Create(newPath);
                var jsonString = jsonWorkplaceModel.ToJsonString(indented: true);
                await writeStream.WriteTextAsync(jsonString, Encoding.UTF8, cancellationToken: cancellationToken);

                var textPartWriter = new TextPartWriter(writeStream);
                await textPartWriter.WriteAsync(cancellationToken);
            }

            return newPath;
        }

        private async ValueTask<string?> ConvertJSearchQueryAsync(string oldPath, string newPath, CancellationToken cancellationToken)
        {
            // see SearchQueryFilePersistent - no text parts and no JsonSearchQueryMetadata
            JsonSearchQueryMetadata? searchQueryMetadata;
            await using (var readStream = FileHelper.OpenRead(oldPath))
            {
                using var streamReader = new StreamReader(readStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                var text = (await streamReader.ReadToEndAsync(cancellationToken)).NormalizeLineEndingsUnixStyle();
                searchQueryMetadata = text.FromJsonString<JsonSearchQueryMetadata>();
            }

            if (searchQueryMetadata is null)
            {
                return null;
            }

            if (searchQueryMetadata is not { FormatVersion: < TessaJsonSerializationContext.SearchQueryJsonVersion })
            {
                return oldPath;
            }

            await this.Logger.WriteLineAsync($"Upgrading JSON format metadata for {Path.GetFileName(oldPath)}.");
            UpdateVersionToLatest(searchQueryMetadata);

            await using (var writeStream = FileHelper.Create(newPath))
            {
                var jsonString = searchQueryMetadata.ToJsonString(indented: true);
                await writeStream.WriteTextAsync(jsonString, Encoding.UTF8, cancellationToken: cancellationToken);
            }

            return newPath;

            static void UpdateVersionToLatest(IJsonSearchQueryMetadata metadata)
            {
                metadata.FormatVersion = TessaJsonSerializationContext.SearchQueryJsonVersion;
                foreach (var item in metadata.Items)
                {
                    UpdateVersionToLatest(item);
                }
            }
        }

        private async Task<ConversionItem?> ReadForConvertAsync(string extension, string sourceFilePath,
            string targetFilePath, CancellationToken cancellationToken = default)
        {
            switch (extension)
            {
                case ".jtype":
                    string typeJson;

                    await using (var fileStream = FileHelper.OpenRead(sourceFilePath, synchronousOnly: true))
                    {
                        using var sr = new StreamReader(fileStream);
                        typeJson = await sr.ReadToEndAsync(cancellationToken);
                    }

                    // Если есть .jtype-файлы, то this.schemeTables обязательно будет определен на этапе инициализации списка файлов для конвертации.
                    var restoredJson = await ConvertTypesService.RepairReferencesAsync(typeJson, NotNullOrThrow(this.schemeTables), cancellationToken);
                    var storage = StorageHelper.DeserializeFromTypedJson(restoredJson);

                    if (storage is null)
                    {
                        return null;
                    }

                    // Если тип карточки имеет текущий формат версии и перезаписывается по такому же пути что и источник,
                    // а также при восстановлении связей виртуальных схем не было изменений, пропускаем.
                    var formatVersion = CardSerializableObject.TryGetFormatVersionFromStorage(storage);
                    if (formatVersion < CardType.MinimalSupportedFormatVersion)
                    {
                        await this.Logger.ErrorAsync(
                            "Type has unsupported format version {0}, requires at least version {1}: \"{2}\". Please, convert the file using tools version from a previous release.",
                            formatVersion,
                            CardType.MinimalSupportedFormatVersion,
                            sourceFilePath);
                        return null;
                    }

                    if (sourceFilePath == targetFilePath
                        && formatVersion == CardType.CurrentFormatVersion
                        && typeJson == restoredJson)
                    {
                        await this.Logger.InfoAsync(
                            "Skipping type from: \"{0}\". Output path equals source path, format version equals current version and no virtual scheme was changed.",
                            sourceFilePath);
                        return null;
                    }

                    await this.Logger.InfoAsync("Type is pending to convert from: \"{0}\"", sourceFilePath);

                    var cardType = NotNullOrThrow(await CardSerializableObject.DeserializeFromStorageAsync<CardType>(storage, null, cancellationToken));
                    return new ConversionItem(sourceFilePath, targetFilePath, cardType);

                case ".jlocalization":
                {
                    await this.Logger.InfoAsync("Reading localization library from: \"{0}\"", sourceFilePath);

                    var localizationService = new JsonFileLocalizationService([sourceFilePath]);
                    var localizationLibrary = (await localizationService.GetLibrariesAsync(returnComments: true, cancellationToken: cancellationToken)).First();
                    return new ConversionItem(sourceFilePath, targetFilePath, localizationLibrary);
                }

                case ".jworkplace":
                    await this.Logger.InfoAsync("Json workplace is pending to convert: \"{0}\"", sourceFilePath);
                    return new ConversionItem(sourceFilePath, targetFilePath, new JsonWorkplace());

                case ".jquery":
                    await this.Logger.InfoAsync("Json search query is pending to convert: \"{0}\"", sourceFilePath);
                    return new ConversionItem(sourceFilePath, targetFilePath, new JsonSearchQueryMetadata());

                case ".jview":
                    await this.Logger.InfoAsync("Json view is pending to convert: \"{0}\"", sourceFilePath);
                    return new ConversionItem(sourceFilePath, targetFilePath, new JsonViewModel());

                default:
                    return null;
            }
        }

        #endregion

        #region Base Overrides

        public override async Task<int> ExecuteAsync(
            OperationContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await this.Logger.InfoAsync("Converting configuration from: \"{0}\"", context.Source);
                var (isSuccessful, resultMessage) = await this.ConvertItemsAsync(
                    NotNullOrThrow(context.Source),
                    NotNullOrThrow(context.Target),
                    context.SchemePath,
                    context.DoNotDelete,
                    context.ConversionMode,
                    cancellationToken);
                await this.Logger.InfoAsync(resultMessage);
                return isSuccessful ? 0 : -1;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.Logger.LogExceptionAsync("Error converting configuration", ex);
                return -1;
            }
        }

        #endregion
    }
}
