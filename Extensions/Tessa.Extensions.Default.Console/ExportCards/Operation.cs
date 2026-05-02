using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Applications;
using Tessa.Cards;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.SourceProviders;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Console.ExportCards
{
    public sealed class Operation(
        IConsoleSessionManager sessionManager,
        IConsoleLogger logger,
        ICardRepository cardRepository,
        ICardManager cardManager)
        : ConsoleOperation<OperationContext>(logger, sessionManager, extendedInitialization: true)
    {
        #region Fields

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        private readonly ICardManager cardManager = NotNullOrThrow(cardManager);

        #endregion

        #region Private Constants

        private const string ExportingAddOperationPrefix = "UI_Cards_ExportingAddOperationPrefix";

        #endregion

        #region Private Methods

        private async Task<(bool result, List<string> successfulFileNames)> ExportCardsAsync(
            List<CardInfo> cardInfoList,
            string? outputFolder,
            bool overwriteModifiedValues,
            IReadOnlyCollection<IStorageContentMapping>? extraContentMapping,
            CancellationToken cancellationToken = default)
        {
            const CardFileFormat format = CardFileFormat.Json;

            IValidationResultBuilder validationResult = new ValidationResultBuilder();
            var successfulCardNames = new List<string>(cardInfoList.Count);
            var successfulFileNames = new List<string>(cardInfoList.Count);

            try
            {
                var cardTypeIDList = await this.cardRepository.GetTypeIDListAsync(
                    cardInfoList.Select(x => x.CardID).ToArray(), cancellationToken: cancellationToken);

                for (var i = 0; i < cardInfoList.Count; i++)
                {
                    var cardInfo = cardInfoList[i];
                    var cardTypeID = cardTypeIDList[i];

                    var cardName = cardInfo.CardName;
                    var fileName = $"{CardHelper.GetCardFileNameWithoutExtension(cardName)}.jcard";
                    var filePath = fileName;

                    if (!string.IsNullOrEmpty(outputFolder))
                    {
                        filePath = Path.Combine(outputFolder, fileName);
                    }

                    var response = await this.ExportCardCoreAsync(
                        filePath,
                        cardInfo.CardID,
                        cardTypeID,
                        format,
                        overwriteModifiedValues,
                        extraContentMapping,
                        cancellationToken);

                    var result = response.ValidationResult.Build();
                    if (result.IsSuccessful)
                    {
                        successfulCardNames.Add(cardName);
                        successfulFileNames.Add(fileName);
                    }
                    else
                    {
                        DefaultConsoleHelper.AddOperationToValidationResult(
                            ExportingAddOperationPrefix,
                            cardName,
                            result,
                            validationResult);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                validationResult.AddException(this, ex);
            }

            if (successfulCardNames.Count != 0)
            {
                validationResult = GetExportingResultWithPreamble(validationResult, successfulCardNames);
            }

            var totalResult = validationResult.Build();
            await this.Logger.LogResultAsync(totalResult);

            return (totalResult.IsSuccessful, successfulFileNames);
        }


        private static IValidationResultBuilder GetExportingResultWithPreamble(
            IValidationResultBuilder validationResult,
            ICollection<string> successfulCardNames)
        {
            var infoText =
                DefaultConsoleHelper.GetQuotedItemsText(
                        new StringBuilder(),
                        "UI_Cards_CardExported",
                        "UI_Cards_MultipleCardsExported",
                        successfulCardNames)
                    .ToString();

            return new ValidationResultBuilder()
                .AddInfo(typeof(Operation), infoText)
                .Add(validationResult);
        }


        private async Task<CardGetResponse> ExportCardCoreAsync(
            string fileName,
            Guid cardID,
            Guid? cardTypeID,
            CardFileFormat format,
            bool overwriteModifiedValues,
            IReadOnlyCollection<IStorageContentMapping>? extraContentMapping,
            CancellationToken cancellationToken = default)
        {
            CardGetResponse response;

            try
            {
                var request = new CardGetRequest
                {
                    CardID = cardID,
                    CardTypeID = cardTypeID,
                    RestrictionFlags = CardGetRestrictionValues.ExportAll
                };

                if (!Path.IsPathFullyQualified(fileName))
                {
                    fileName = Path.GetFullPath(fileName);
                }

                ISourceContentProvider fileSource = new FileSourceContentProvider(fileName);
                response = await this.cardManager.ExportAsync(
                    request,
                    fileSource,
                    format: format,
                    overwriteModifiedValues: overwriteModifiedValues,
                    extraContentMapping: extraContentMapping,
                    cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                response = new CardGetResponse();
                response.ValidationResult.AddException(this, ex);
            }

            return response;
        }


        private static void CreateFoldersAndGetPaths(
            OperationContext context,
            out string? subfolderPartPathInLibrary,
            out string? cardFilesOutputFolder,
            out string? libraryFilePath)
        {
            var outputFolder = context.OutputFolder?.Trim().NormalizePathOnCurrentPlatform();
            var cardLibraryPath = context.CardLibraryPath?.Trim().NormalizePathOnCurrentPlatform();

            libraryFilePath = cardLibraryPath;
            if (!string.IsNullOrEmpty(libraryFilePath))
            {
                if (!Path.GetFileName(libraryFilePath).Contains(".", StringComparison.Ordinal))
                {
                    // если есть любое расширение, или просто точка на конце - сохраняем расширение
                    libraryFilePath += ".jcardlib";
                }
            }

            var cardLibraryDirectory = cardLibraryPath;
            if (!string.IsNullOrEmpty(cardLibraryDirectory))
            {
                cardLibraryDirectory = cardLibraryDirectory == "."
                    ? null
                    : Path.GetDirectoryName(cardLibraryDirectory);
            }

            // В библиотеках карточек путь прописывается по той же логике, что и при сериализации CardLibrary.Items, т.е. с прямым слэшом "/", независимо от ОС.
            subfolderPartPathInLibrary = outputFolder?.Equals(".", StringComparison.Ordinal) ?? false ? null : outputFolder?.NormalizePathForApplications();

            cardFilesOutputFolder = string.IsNullOrEmpty(cardLibraryDirectory)
                ? outputFolder
                : string.IsNullOrEmpty(outputFolder)
                    ? cardLibraryDirectory
                    : Path.Combine(cardLibraryDirectory, outputFolder);

            if (cardFilesOutputFolder == ".")
            {
                cardFilesOutputFolder = null;
            }
            else if (!string.IsNullOrEmpty(cardFilesOutputFolder)
                     && !Directory.Exists(cardFilesOutputFolder))
            {
                Directory.CreateDirectory(cardFilesOutputFolder);
            }
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

            try
            {
                if (context.CardInfoList is null || context.CardInfoList.Count == 0)
                {
                    await this.Logger.InfoAsync("No cards to export");
                    return 0;
                }

                DefaultConsoleHelper.RemoveCardInfoListDuplicates(context.CardInfoList);

                if (context.LocalizationCulture is not null)
                {
                    foreach (var cardInfo in context.CardInfoList)
                    {
                        cardInfo.CardName = await LocalizeFormatAsync(context.LocalizationCulture, cardInfo.CardName);
                    }
                }

                CreateFoldersAndGetPaths(
                    context,
                    out var subfolderPartPathInLibrary,
                    out var cardFilesOutputFolder,
                    out var libraryFilePath);

                await this.Logger.InfoAsync(
                    "Exporting cards ({0}):{1}{2}",
                    context.CardInfoList.Count,
                    Environment.NewLine,
                    string.Join(Environment.NewLine, context.CardInfoList.Select(x => $"\"{x.CardName}\"")));

                IStorageContentMapping[]? storageContentMapping = null;
                if (context.StorageContentMappingFileName is not null)
                {
                    var json = await File.ReadAllTextAsync(context.StorageContentMappingFileName, cancellationToken);
                    var list = StorageHelper.DeserializeListFromTypedJson(json);
                    if (list is not null)
                    {
                        storageContentMapping = new ListStorage<IStorageContentMapping>(
                                list,
                                new DictionaryStorageValueFactory<int, IStorageContentMapping>(
                                    (index, storage) =>
                                    {
                                        var innerContentMapping = new StorageContentMapping();
                                        innerContentMapping.Deserialize(storage);

                                        return innerContentMapping;
                                    }))
                            .ToArray();
                    }
                }

                var (result, successfulFileNames) = await this.ExportCardsAsync(
                    context.CardInfoList,
                    cardFilesOutputFolder,
                    context.OverwriteModifiedValues,
                    storageContentMapping,
                    cancellationToken);

                if (!result)
                {
                    return -1;
                }

                if (successfulFileNames.Count > 0 && !string.IsNullOrEmpty(libraryFilePath))
                {
                    var library = new CardLibrary();

                    if (File.Exists(libraryFilePath))
                    {
                        await this.Logger.InfoAsync("Appending to existent card library: \"{0}\"", libraryFilePath);

                        var json = await File.ReadAllTextAsync(libraryFilePath, cancellationToken);
                        library = await CardSerializableObject.DeserializeFromJsonAsync<CardLibrary>(json, null, cancellationToken);
                    }
                    else
                    {
                        await this.Logger.InfoAsync("Creating card library: \"{0}\"", libraryFilePath);
                    }

                    var hasChanges = false;
                    foreach (var fileName in successfulFileNames)
                    {
                        var filePath = string.IsNullOrEmpty(subfolderPartPathInLibrary)
                            ? fileName
                            // В библиотеках карточек путь прописывается по той же логике, что и при сериализации CardLibrary.Items, т.е. с прямым слэшом "/", независимо от ОС.
                            : $"{subfolderPartPathInLibrary}/{fileName}";

                        if (library.Items.Any(x => x.Path == filePath))
                        {
                            await this.Logger.InfoAsync("Skipping card: \"{0}\"", filePath);
                        }
                        else
                        {
                            await this.Logger.InfoAsync("Adding card: \"{0}\"", filePath);
                            library.Items.Add(new CardLibraryItem(filePath));
                            hasChanges = true;
                        }
                    }

                    if (hasChanges)
                    {
                        await this.Logger.InfoAsync("Saving card library");

                        // чтобы при ошибке сериализации случайно не перезаписать файл с библиотекой - сериализуем в памяти
                        var json = await library.SerializeToJsonAsync(indented: true, cancellationToken);

                        await File.WriteAllTextAsync(libraryFilePath, json, Encoding.UTF8, cancellationToken);
                    }
                    else
                    {
                        await this.Logger.InfoAsync("Card library isn't changed");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.Logger.LogExceptionAsync("Error exporting cards", ex);
                return -1;
            }

            await this.Logger.InfoAsync("Cards are exported successfully");
            return 0;
        }

        #endregion
    }
}
