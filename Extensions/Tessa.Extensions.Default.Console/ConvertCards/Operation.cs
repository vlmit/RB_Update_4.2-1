using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Console.ConvertConfiguration;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.IO;
using Tessa.Platform.SourceProviders;
using Tessa.Platform.Storage.Mapping;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Console.ConvertCards
{
    public sealed class Operation(
        IConsoleSessionManager sessionManager,
        IConsoleLogger logger,
        IStorageMappingProvider storageMappingProvider,
        ICardExternalSourceLogic cardExternalSourceLogic,
        ICardMetadata cardMetadata,
        Func<string, ISourceProviderLinker> getProviderLinker)
        : ConsoleOperation<OperationContext>(logger, sessionManager)
    {
        #region Fields

        private readonly IStorageMappingProvider storageMappingProvider = NotNullOrThrow(storageMappingProvider);

        private readonly ICardExternalSourceLogic cardExternalSourceLogic = NotNullOrThrow(cardExternalSourceLogic);

        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);

        private ISourceProviderLinker? providerLinker;

        private readonly Func<string, ISourceProviderLinker> getProviderLinker = NotNullOrThrow(getProviderLinker);

        private const string FilePatterns = "*.jcard;*.jcardlib";

        #endregion

        #region Private Methods

        private async ValueTask<string> ConvertItemsAsync(
            string sourcePath,
            string targetPath,
            bool doNotDelete,
            CancellationToken cancellationToken = default)
        {
            var items = new List<ConversionItem>();

            var attr = File.GetAttributes(sourcePath);
            var sourcePathIsDirectory = (attr & FileAttributes.Directory) == FileAttributes.Directory;
            foreach (var sourceFilePath in DefaultConsoleHelper.GetSourceFiles(sourcePath, FilePatterns, false))
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
                    item = await this.ReadForConvertAsync(extension, sourceFilePath, targetFilePath, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
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

                // Дополнительный список файлов на удаление.
                IList<ISourceContentProvider>? additionalFilesToDelete = null;
                Debug.Assert(this.providerLinker is not null);

                foreach (var item in items)
                {
                    var itemWasConverted = true;
                    var targetDirectoryName = NotNullOrThrow(Path.GetDirectoryName(item.NewPath));
                    try
                    {
                        // Подготовить директорию для target
                        FileHelper.CreateDirectoryIfNotExists(targetDirectoryName, true);
                        switch (item.Object)
                        {
                            case CardLibrary cardLibrary:
                                additionalFilesToDelete = await ConvertCardLibraryItemsAsync(cardLibrary, item.OldPath, item.NewPath);
                                var libraryText = await cardLibrary.SerializeToJsonAsync(true, cancellationToken);

                                var actualTarget = new FileSourceContentProvider(item.NewPath);

                                // Временная рабочая папка.
                                var workingTarget = NotNullOrThrow(await this.providerLinker.LinkProviderAsync(actualTarget, cancellationToken: cancellationToken));

                                await File.WriteAllTextAsync(
                                    workingTarget.GetFullName(),
                                    libraryText,
                                    Encoding.UTF8,
                                    cancellationToken);

                                // Перезапись из временной папки в конечную.
                                await this.providerLinker.OverwriteAll(true, cancellationToken);

                                await this.Logger.InfoAsync("Card library is converted: \"{0}\"", item.NewPath);
                                break;

                            case Card:
                                await this.ConvertCardAsync(item.OldPath, item.NewPath, cancellationToken);
                                await this.Logger.InfoAsync("Card is converted: \"{0}\"", item.NewPath);
                                break;
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        await this.providerLinker.UnlinkAll(true, cancellationToken);

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

                    // Удаление старых файлов после конвертации
                    if (!doNotDelete
                        && itemWasConverted
                        && item.NewPath != item.OldPath)
                    {
                        // Удаление основного файла
                        FileHelper.DeleteFileSafe(item.OldPath);

                        // Если конвертировали .jcard, то необходимо удалить еще и подпапку.
                        if (item.OldPath.EndsWith(".jcard"))
                        {
                            var oldFileProvider = new FileSourceContentProvider(item.OldPath);
                            var oldSubFolderProvider = CardHelper.GetSubContentDirectoryProvider(oldFileProvider);
                            if (await oldSubFolderProvider.IsExistsAsync(cancellationToken))
                            {
                                await oldSubFolderProvider.DeleteAsync(true, cancellationToken);
                            }
                        }

                        // Если в доп. списке на удаление что-то есть
                        if (additionalFilesToDelete is not null)
                        {
                            foreach (var fileProvider in additionalFilesToDelete)
                            {
                                if (await fileProvider.IsExistsAsync(cancellationToken))
                                {
                                    await fileProvider.DeleteAsync(cancellationToken);
                                }
                            }
                        }
                    }
                }
            }

            return $"{items.Count - errorCount} of {items.Count} files were converted successfully.";
        }

        private async Task ConvertCardAsync(
            string oldPath,
            string newPath,
            CancellationToken cancellationToken = default)
        {
            // Т.к. теперь карточки могут быть с одинаковым именем, надо обработать такую ситуацию,
            // т.е. нельзя открывать и target-stream и source-stream одовременно.
            var sourceProvider = new FileSourceContentProvider(oldPath);
            var targetProvider = new FileSourceContentProvider(newPath);

            await this.ConvertCardToJsonAsync(sourceProvider, targetProvider, cancellationToken);
        }

        private async Task ConvertCardToJsonAsync(
            ISourceContentProvider sourceProvider,
            ISourceContentProvider targetProvider,
            CancellationToken cancellationToken = default)
        {
            var targetFullName = targetProvider.GetFullName();

            // Проверка на существование подпапки (если не перезаписываем файл карточки)
            if (await targetProvider.IsExistsAsync(cancellationToken) is not true)
            {
                var targetSubDirectory = CardHelper.GetSubContentDirectoryProvider(targetProvider);
                if (await targetSubDirectory.IsExistsAsync(cancellationToken))
                {
                    var targetDirectory = Path.GetDirectoryName(targetFullName) ?? string.Empty;
                    var shortSubDirectoryName = Path.GetRelativePath(targetDirectory, targetSubDirectory.GetFullName());
                    // Имя конвертируемого файла конкретизировано в catch.
                    throw new InvalidOperationException(
                        $"Card's subdirectory \"{shortSubDirectoryName}\" already exists at target path \"{targetDirectory}\".");
                }
            }

            var parsedCard = await this.cardExternalSourceLogic.ParseJsonCardAsync(
                sourceProvider,
                null,
                false,
                cancellationToken);

            if (parsedCard.ErrorResponse is not null)
            {
                throw new ValidationException(parsedCard.ErrorResponse.ValidationResult.Build());
            }

            // Преобразование json-строк в Dictionary.
            await CardHelper.DeserializeJsonFieldsInSectionsAndFilesAsync(NotNullOrThrow(parsedCard.Request).Card, this.cardMetadata, cancellationToken);

            // Получить маппинги.
            var storageMappings = this.storageMappingProvider.TryGetStorageMappings(parsedCard.Request.Card);

            // добавляем информацию по файлам и формируем GuidDictionary стримов прикрепленых файлов
            var cardFiles = parsedCard.Request.Card.TryGetFiles()?.ToArray();

            var fileStreams = parsedCard.FileStreams?.ToArray() ?? [];
            var fileStreamsDictionary = new Dictionary<Guid, Func<CancellationToken, ValueTask<Stream?>>>();

            if (cardFiles?.Length > 0)
            {
                FillFileStreamsDictionary(parsedCard, fileStreamsDictionary, fileStreams);
            }

            var validationResult = new ValidationResultBuilder();

            var sourceSubDirectoryProvider = CardHelper.GetSubContentDirectoryProvider(sourceProvider);
            var targetSubDirectoryProvider = CardHelper.GetSubContentDirectoryProvider(targetProvider);

            Debug.Assert(this.providerLinker is not null);
            this.providerLinker.SetValidationResult(validationResult);

            // Создаем "рабочий" target-provider в который будем конвертировать.
            var workingTargetProvider = await this.providerLinker.LinkProviderAsync(targetProvider, cancellationToken: cancellationToken);
            if (!validationResult.IsSuccessful())
            {
                throw new InvalidOperationException(
                    $"Can't create link for content provider \"{targetProvider.GetFullName()}\".\r\n\r\n{validationResult.ToString(ValidationLevel.Detailed)}");
            }

            ThrowIfNull(workingTargetProvider);

            // Cоздаем провайдер для "рабочей" target-поддиректории.
            var workingTargetSubDirectoryProvider = await this.providerLinker.LinkProviderAsync(targetSubDirectoryProvider, cancellationToken: cancellationToken);
            if (!validationResult.IsSuccessful())
            {
                throw new InvalidOperationException(
                    $"Can't create link for directory provider \"{targetProvider.GetFullName()}\".\r\n\r\n{validationResult.ToString(ValidationLevel.Detailed)}");
            }

            ThrowIfNull(workingTargetSubDirectoryProvider);

            await this.cardExternalSourceLogic.WriteJsonCardAsync(
                workingTargetProvider,
                parsedCard.Request,
                cardFiles,
                storageMappings,
                validationResult,
                fileStreamsDictionary,
                workingTargetSubDirectoryProvider,
                cancellationToken: cancellationToken);

            if (!validationResult.IsSuccessful())
            {
                throw new ValidationException(validationResult.Build());
            }

            // Скопировать спец-файлы из подпапки.
            var specialFileNames = this.cardExternalSourceLogic.GetSpecialFileNames();

            foreach (var specialFileName in specialFileNames)
            {
                var sourceSpecialFileProvider = sourceSubDirectoryProvider.GetContentProvider(specialFileName);

                if (await sourceSpecialFileProvider.IsExistsAsync(cancellationToken))
                {
                    // Создаем поддиректорию в target, если ее еще нет на данном этапе.
                    await workingTargetSubDirectoryProvider.CreateIfNotExistsAsync(cancellationToken);

                    var targetSpecialFileProvider = workingTargetSubDirectoryProvider.GetContentProvider(specialFileName);

                    await using var specialFileSourceStream =
                        await sourceSpecialFileProvider.CreateStreamReadAsync(cancellationToken);
                    await using var specialFileTargetStream =
                        await targetSpecialFileProvider.CreateStreamWriteAsync(cancellationToken);

                    await specialFileSourceStream.CopyToAsync(specialFileTargetStream, cancellationToken);
                }
            }

            // Все прошло успешно, перезаписываем временные файлы и директории в конечные.
            await this.providerLinker.OverwriteOriginalProviderAsync(workingTargetProvider, true, cancellationToken);

            if (!validationResult.IsSuccessful())
            {
                throw new InvalidOperationException(
                    $"Can't overwrite card file providers.\r\n\r\n{validationResult.ToString(ValidationLevel.Detailed)}");
            }

            if (await workingTargetSubDirectoryProvider.IsExistsAsync(cancellationToken))
            {
                await this.providerLinker.OverwriteOriginalProviderAsync(workingTargetSubDirectoryProvider, true, cancellationToken);

                if (!validationResult.IsSuccessful())
                {
                    throw new InvalidOperationException(
                        $"Can't overwrite card subdirectory providers.\r\n\r\n{validationResult.ToString(ValidationLevel.Detailed)}");
                }
            }

            await this.providerLinker.UnlinkAll(cancellationToken: cancellationToken);
        }

        private static void FillFileStreamsDictionary(
            ParsedCard parsedCard,
            IDictionary<Guid, Func<CancellationToken, ValueTask<Stream?>>> fileStreamsGuidDictionary,
            IReadOnlyList<Func<CancellationToken, ValueTask<Stream?>>> fileStreams)
        {
            var fileCounter = 0;
            foreach (var s in NotNullOrThrow(parsedCard.Header).GetOrderedFiles())
            {
                fileStreamsGuidDictionary[s.ID] = fileStreams[fileCounter++];
            }
        }

        private async Task<ConversionItem?> ReadForConvertAsync(
            string extension,
            string sourceFilePath,
            string targetFilePath,
            CancellationToken cancellationToken = default)
        {
            switch (extension)
            {
                case ".jcard":
                    await this.Logger.InfoAsync("Card is pending to convert: \"{0}\"", sourceFilePath);
                    return new ConversionItem(sourceFilePath, targetFilePath, new Card());

                case ".jcardlib":
                    await this.Logger.InfoAsync("Reading card library from: \"{0}\"", sourceFilePath);
                    var cardLibraryJson = await File.ReadAllTextAsync(sourceFilePath, Encoding.UTF8, cancellationToken);
                    var cardLibrary = await CardSerializableObject.DeserializeFromJsonAsync<CardLibrary>(cardLibraryJson, null, cancellationToken);
                    return new ConversionItem(sourceFilePath, targetFilePath, cardLibrary);

                default:
                    return null;
            }
        }

        private static async ValueTask<IList<ISourceContentProvider>> ConvertCardLibraryItemsAsync(
            CardLibrary cardLibrary,
            string oldFilePath,
            string newFilePath)
        {
            var oldPath = Path.GetDirectoryName(oldFilePath) ?? string.Empty;
            var newPath = Path.GetDirectoryName(newFilePath) ?? string.Empty;
            var samePath = oldPath.Equals(newPath, StringComparison.Ordinal);
            var optionsList = new List<ISourceContentProvider>();

            foreach (var item in cardLibrary.Items)
            {
                // Если путь назначения отличается от source, то переписываем опции слияния (если есть)
                if (!samePath && !string.IsNullOrEmpty(item.Options))
                {
                    var sourceDirectoryProvider = new FileSourceDirectoryProvider(NotNullOrThrow(Path.GetDirectoryName(oldFilePath)));
                    var targetDirectoryProvider = new FileSourceDirectoryProvider(NotNullOrThrow(Path.GetDirectoryName(newFilePath)));

                    var sourceOptionsFile = sourceDirectoryProvider.GetContentProvider(item.Options);
                    var targetOptionsFile = targetDirectoryProvider.GetContentProvider(item.Options);
                    var targetOptionsFileDirectory = targetOptionsFile.GetCurrentDirectoryProvider();
                    await targetOptionsFileDirectory.CreateIfNotExistsAsync();

                    await using var sourceStream = await sourceOptionsFile.CreateStreamReadAsync();
                    await using var targetStream = await targetOptionsFile.CreateStreamWriteAsync();

                    await sourceStream.CopyToAsync(targetStream);

                    optionsList.Add(sourceOptionsFile);
                }
            }

            return optionsList;
        }

        #endregion

        #region Base Overrides

        public override async Task<int> ExecuteAsync(
            OperationContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                this.providerLinker = this.getProviderLinker(FileSystemSourceProviderLinker.LinkerForWriteCardsName);

                await this.Logger.InfoAsync("Converting cards from: \"{0}\"", context.Source);
                var resultMessage = await this.ConvertItemsAsync(
                    NotNullOrThrow(context.Source),
                    NotNullOrThrow(context.Target),
                    context.DoNotDelete,
                    cancellationToken);
                await this.Logger.InfoAsync(resultMessage);
                return 0;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await this.Logger.LogExceptionAsync("Error converting configuration", e);
                return -1;
            }
        }

        #endregion
    }
}
