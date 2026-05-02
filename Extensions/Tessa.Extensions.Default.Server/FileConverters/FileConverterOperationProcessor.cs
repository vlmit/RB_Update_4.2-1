#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.FileConverters;
using Tessa.Files;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.IO;
using Tessa.Platform.Operations;
using Tessa.Platform.Runtime;
using Tessa.Platform.Scopes;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Server.FileConverters
{
    /// <inheritdoc cref="IFileConverterOperationProcessor"/>
    /// <summary>
    /// Создаёт экземпляр класса <see cref="FileConverterOperationProcessor"/>.
    /// </summary>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="cardCache"><inheritdoc cref="ICardCache" path="/summary"/></param>
    /// <param name="signatureProvider"><inheritdoc cref="ISignatureProvider" path="/summary"/></param>
    /// <param name="errorManager"><inheritdoc cref="IErrorManager" path="/summary"/></param>
    /// <param name="extensionContainer"><inheritdoc cref="IExtensionContainer" path="/summary"/></param>
    /// <param name="operationRepository"><inheritdoc cref="IOperationRepository" path="/summary"/></param>
    /// <param name="fileConverterCache"><inheritdoc cref="IFileConverterCache" path="/summary"/></param>
    /// <param name="fileConverterWorker"><inheritdoc cref="IFileConverterWorker" path="/summary"/></param>
    /// <param name="cardStreamServerRepository"><inheritdoc cref="ICardStreamServerRepository" path="/summary"/></param>
    /// <param name="cardServerPermissionsProvider"><inheritdoc cref="ICardServerPermissionsProvider" path="/summary"/></param>
    /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
    public sealed class FileConverterOperationProcessor(
        IDbScope dbScope,
        ICardCache cardCache,
        IErrorManager errorManager,
        IExtensionContainer extensionContainer,
        IOperationRepository operationRepository,
        IFileConverterCache fileConverterCache,
        IFileConverterWorker fileConverterWorker,
        ICardStreamServerRepository cardStreamServerRepository,
        ICardServerPermissionsProvider cardServerPermissionsProvider,
        ICardRepository cardRepository,
        [Dependency(SignatureProviderNames.Operations)] ISignatureProvider? signatureProvider = null) :
        IFileConverterOperationProcessor
    {
        #region Fields

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);
        private readonly ICardCache cardCache = NotNullOrThrow(cardCache);
        private readonly IErrorManager errorManager = NotNullOrThrow(errorManager);
        private readonly ISignatureProvider signatureProvider = signatureProvider ?? HashSignatureProvider.Operations;
        private readonly IExtensionContainer extensionContainer = NotNullOrThrow(extensionContainer);
        private readonly IOperationRepository operationRepository = NotNullOrThrow(operationRepository);
        private readonly IFileConverterCache fileConverterCache = NotNullOrThrow(fileConverterCache);
        private readonly IFileConverterWorker fileConverterWorker = NotNullOrThrow(fileConverterWorker);
        private readonly ICardStreamServerRepository cardStreamServerRepository = NotNullOrThrow(cardStreamServerRepository);
        private readonly ICardServerPermissionsProvider cardServerPermissionsProvider = NotNullOrThrow(cardServerPermissionsProvider);
        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region IFileConverterOperationProcessor Implementation

        /// <inheritdoc/>
        public async ValueTask<bool> TryProcessOperationAsync(
            IOperation operation,
            IFileConverterRequest request,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(request);
            ThrowIfNull(operation);

            if (operation.TypeID != OperationTypes.ConvertingFile)
            {
                throw new ArgumentException($"Operation with ID={operation.ID:B} is not a file conversion operation.");
            }

            var validationResult = new ValidationResultBuilder();

            var converterType = await GetConverterTypeAsync(this.cardCache, cancellationToken);
            if (converterType is FileConverterType.None)
            {
                validationResult.AddError(this, "File converter is not set.");
                await this.CompleteWithErrorAsync(operation.ID, request, validationResult);
                return false;
            }

            if (this.fileConverterWorker is IFileConverterAggregateWorker aggregateWorker
                && !aggregateWorker.IsRegistered(request.OutputFormat))
            {
                validationResult.AddError(this, $"Unsupported output format \"{request.OutputFormat}\" while trying to convert file \"{request.FileName}\".");
                await this.CompleteWithErrorAsync(operation.ID, request, validationResult);
                return false;
            }

            if (!FileConverterFormat.IsSupportedConversion(request.OutputFormat, request.InputFormat))
            {
                validationResult.AddError(this, $"Unsupported input format \"{request.InputFormat}\" while trying to convert file \"{request.FileName}\".");
                await this.CompleteWithErrorAsync(operation.ID, request, validationResult);
                return false;
            }

            logger.Info("Converting file \"{0}\" with CardID={1:B} and FileID={2:B}.", request.FileName, request.CardID, request.FileID);

            string? suggestedName = null;
            Func<CancellationToken, ValueTask<Stream>> getInputContentAsync = static _ => new(Stream.Null);
            Func<CancellationToken, ValueTask<(Stream, long)>> getOutputContentAsync = static _ => new((Stream.Null, -1L));

            if (converterType is not FileConverterType.OnlyOfficeService)
            {
                var cardGetRequest = new CardGetRequest
                {
                    CardID = request.CardID,
                    GetMode = CardGetMode.ReadOnly,
                    RestrictionFlags = CardGetRestrictionFlags.RestrictFiles | CardGetRestrictionFlags.RestrictTasks
                };

                cardGetRequest.SetCalculateDigest(true);
                this.cardServerPermissionsProvider.SetFullPermissions(cardGetRequest);

                var cardGetResponse = await this.cardRepository.GetAsync(
                    cardGetRequest,
                    cancellationToken: cancellationToken);

                var digest = cardGetResponse.ValidationResult.IsSuccessful()
                    ? cardGetResponse.Card.TryGetDigest()
                    : null;

                // получаем файл из карточки
                var fileRequest = request.CreateCardGetFileContentRequest(digest);

                // в момент открытия файла нам мог прийти или не прийти токен, здесь мы его выбрасываем и устанавливаем пермишены "всегда можно"
                this.cardServerPermissionsProvider.SetFullPermissions(fileRequest);

                var fileResult = await this.cardStreamServerRepository.GetFileContentAsync(fileRequest, cancellationToken);
                var result = fileResult.Response.ValidationResult.Build();
                validationResult.Add(result);

                if (!result.IsSuccessful || !fileResult.HasContent)
                {
                    await this.CompleteWithErrorAsync(operation.ID, request, validationResult);
                    return false;
                }

                suggestedName = fileResult.Response.TryGetSuggestedFileName();
                getInputContentAsync = fileResult.GetContentOrThrowAsync;
            }

            var context = new FileConverterContext(getInputContentAsync, getOutputContentAsync, suggestedName, request, operation, cancellationToken);

            try
            {
                // преобразуем файл с расширениями (расширения выполняются, только если конвертация выполняется в плагине)
                await this.ConvertWithExtensionsAsync(context, withExtensions: request.Flags.HasNot(FileConverterRequestFlags.ExecuteSynchronously),
                    cancellationToken);
                validationResult.Add(context.ValidationResult);

                if (!context.ValidationResult.IsSuccessful())
                {
                    await this.CompleteWithErrorAsync(operation.ID, request, validationResult);
                    return false;
                }

                var convertedFileID = Guid.NewGuid();

                // сохраняем файл в кеш с заданным идентификатором
                if (request.Flags.HasNot(FileConverterRequestFlags.DoNotCacheResult))
                {
                    var requestHash = request.CalculateHash(this.signatureProvider);
                    var convertedFileName = FileHelper.GetFileNameWithoutExtension(request.FileName, ignoreFolder: true)
                        + (string.IsNullOrEmpty(context.OutputExtension) ? null : $".{context.OutputExtension}");

                    ValidationResult conversionResult;
                    var (stream, length) = await context.GetOutputContentAsync(cancellationToken);
                    await using (stream)
                    {
                        await using var fileContent = await RemoteFileContent.FromStreamAndSizeAsync(
                            getContentFuncAsync: _ => new(stream),
                            length,
                            cancellationToken);

                        conversionResult = await this.fileConverterCache
                            .StoreFileAsync(
                                request.VersionID,
                                requestHash,
                                convertedFileID,
                                convertedFileName,
                                fileContent,
                                context.ResponseInfo.GetStorage(),
                                cancellationToken);
                    }

                    validationResult.Add(conversionResult);
                    if (!conversionResult.IsSuccessful)
                    {
                        await this.CompleteWithErrorAsync(operation.ID, request, validationResult);
                        return false;
                    }
                }

                // завершаем операцию, указывая идентификатор файла в кэше
                await this.CompleteSuccessfulAsync(operation.ID, convertedFileID, request, context, validationResult);

                logger.Info("File \"{0}\" with CardID={1:B} and FileID={2:B} has been converted.", request.FileName, request.CardID, request.FileID);
                return true;
            }
            catch (OperationCanceledException)
            {
                // при отмене выполняется удаление операции по конвертации файла
                await this.DeleteOperationSafeAsync(operation.ID, CancellationToken.None);
                logger.Info("Convertion of file \"{0}\" with CardID={1:B} and FileID={2:B} has been cancelled.", request.FileName, request.CardID,
                    request.FileID);
                return false;
            }
            catch (Exception ex)
            {
                validationResult.AddException(this, ex);
                await this.CompleteWithErrorAsync(operation.ID, request, validationResult);
                return false;
            }
            finally
            {
                foreach (var callback in context.FinalizationQueue.Reverse())
                {
                    await ExecuteCallbackSafeAsync(callback);
                }
            }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Получает и возвращает тип конвертера для преобразования файла в другие форматы из кэша карточки настроек сервера.
        /// Если тип конвертера не задан в настройках или значение не определено, то по умолчанию используется <see cref="FileConverterType.LibreOffice"/>.
        /// </summary>
        /// <param name="cardCache"><inheritdoc cref="ICardCache" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns><inheritdoc cref="FileConverterType" path="/summary"/></returns>
        public static async Task<FileConverterType> GetConverterTypeAsync(ICardCache cardCache, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(cardCache);

            var result = await cardCache.Cards.GetAsync(CardHelper.ServerInstanceTypeName, cancellationToken);
            var fields = result.GetValue().Sections["ServerInstances"].RawFields;
            return (FileConverterType?) fields.TryGet<int?>("FileConverterTypeID") ?? FileConverterType.LibreOffice;
        }

        #endregion

        #region Private Methods

        private static async ValueTask ExecuteCallbackSafeAsync(Func<ValueTask>? callback)
        {
            if (callback is not null)
            {
                try
                {
                    await callback();
                }
                catch (Exception ex)
                {
                    logger.LogException(ex, LogLevel.Warn);
                }
            }
        }

        private async Task DeleteOperationSafeAsync(Guid operationID, CancellationToken cancellationToken)
        {
            try
            {
                await this.operationRepository.DeleteAsync(operationID, OperationTypes.ConvertingFile, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogException($"Error during removing file conversion operation with ID={operationID:B}:", ex, LogLevel.Warn);
            }
        }

        private async Task ExecuteExtensionSafeAsync(IExtensionExecutor executor, string methodName, FileConverterContext context)
        {
            try
            {
                await executor.ExecuteAsync(methodName, context);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.ValidationResult.AddException(this, ex);
            }
        }

        private async Task ConvertWithExtensionsAsync(FileConverterContext context, bool withExtensions, CancellationToken cancellationToken)
        {
            await using var dbScopeInstance = this.dbScope.CreateNew();
            await using var executor = await this.extensionContainer.ResolveExecutorAsync<IFileConverterExtension>();
            await using var scopeInstance = ScopeHolderContext.Create();

            try
            {
                RuntimeHelper.ServerRequestID = Guid.NewGuid();

                if (withExtensions)
                {
                    await this.ExecuteExtensionSafeAsync(executor, nameof(IFileConverterExtension.BeforeRequest), context);
                }

                context.RequestIsSuccessful = context.ValidationResult.IsSuccessful();
                if (context.RequestIsSuccessful)
                {
                    try
                    {
                        await this.fileConverterWorker.ConvertFileAsync(context, cancellationToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.ValidationResult.AddException(this, ex);
                    }
                }

                if (withExtensions)
                {
                    await this.ExecuteExtensionSafeAsync(executor, nameof(IFileConverterExtension.AfterRequest), context);
                }
            }
            finally
            {
                RuntimeHelper.ServerRequestID = null;
            }
        }

        private Task CompleteSuccessfulAsync(
            Guid operationID,
            Guid convertedFileID,
            IFileConverterRequest request,
            IFileConverterContext context,
            IValidationResultBuilder validationResult)
        {
            if (request.Flags.Has(FileConverterRequestFlags.WithoutResponse))
            {
                return this.DeleteOperationSafeAsync(operationID, CancellationToken.None);
            }

            var suggestedName = context.SuggestedName;
            if (!string.IsNullOrEmpty(suggestedName))
            {
                suggestedName = FileHelper.RemoveInvalidFileNameChars(
                    FileHelper.GetFileNameWithoutExtension(suggestedName, ignoreFolder: true),
                    FileHelper.InvalidCharReplacement) + "." + context.OutputExtension;
            }

            var response = new OperationResponse();
            response.Info[nameof(IFileConverterRequest.FileID)] = convertedFileID;
            response.Info[nameof(IFileConverterContext.SuggestedName)] = suggestedName;
            response.Info[nameof(IFileConverterContext.ResponseInfo)] = StorageHelper.Clone(context.ResponseInfo.GetStorage());
            response.ValidationResult.Add(validationResult);

            return this.operationRepository.CompleteAsync(operationID, OperationTypes.ConvertingFile, response, CancellationToken.None);
        }

        private async Task CompleteWithErrorAsync(
            Guid operationID,
            IFileConverterRequest request,
            ValidationResultBuilder validationResult)
        {
            var additionalMessageHeader = string.Format(
                "Error converting file \"{0}\" to {1} with CardID={2:B} and FileID={3:B}.",
                request.FileName,
                request.OutputFormat.GetExtension().ToUpperInvariant(),
                request.CardID,
                request.FileID);

            var result = validationResult.Build();
            logger.Error(additionalMessageHeader + Environment.NewLine + result.ToString(ValidationLevel.Detailed));
            await this.ReportErrorSafeAsync(result, operationID, request, additionalMessageHeader);

            if (request.Flags.Has(FileConverterRequestFlags.WithoutResponse))
            {
                await this.DeleteOperationSafeAsync(operationID, CancellationToken.None);
                return;
            }

            var response = new OperationResponse();
            response.ValidationResult.Add(validationResult);
            await this.operationRepository.CompleteAsync(operationID, OperationTypes.ConvertingFile, response, CancellationToken.None);
        }

        private Task<Guid?> ReportErrorSafeAsync(
            ValidationResult result,
            Guid operationID,
            IFileConverterRequest? request,
            string? additionalMessageHeader = null)
        {
            var header = !string.IsNullOrWhiteSpace(additionalMessageHeader)
                ? additionalMessageHeader + Environment.NewLine
                : string.Empty;

            return this.errorManager.ReportErrorSafeAsync(
                CardHelper.FileConverterCacheTypeID,
                request?.CardID ?? FileConverterHelper.CacheCardID,
                request is null ? "Unhandled exception" : request.EventName,
                new ErrorDescription(
                    result,
                    ErrorCategories.FileConverterFailed,
                    header + result.ToString(ValidationLevel.Message)),
                id: operationID);
        }

        #endregion
    }
}
