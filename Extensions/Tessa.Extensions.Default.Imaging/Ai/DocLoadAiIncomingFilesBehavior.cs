using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Plugins;
using System.Diagnostics;
using LinqToDB;
using Tessa.Ai.Agent.Models;
using Tessa.Ai.Agent;
using Tessa.Ai.Files.Models;
using Tessa.Cards;
using Tessa.Files;
using Tessa.Ai.Files;
using Tessa.Imaging.DocLoad;
using Tessa.WorkflowProcesses;
using Unity;
using Tessa.Imaging.BarcodeImport;
using Tessa.Platform.IO;
using Tessa.Imaging.BarcodeImport.BmpConverters;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Tessa.Ai.Models;
using Tessa.Ai.Settings;
using Tessa.Platform.Data;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using PdfDocument = Tessa.Imaging.BarcodeImport.PdfDocument;

namespace Tessa.Extensions.Default.Imaging.Ai
{
    /// <inheritdoc cref="IDocLoadFilesBehavior"/>
    /// <param name="container"><inheritdoc cref="IUnityContainer" path="/summary"/></param>
    /// <param name="aiAgent"><inheritdoc cref="IAiAgent" path="/summary"/></param>
    /// <param name="aiFileService"><inheritdoc cref="IAiFileService" path="/summary"/></param>
    /// <param name="aiFileTokenProvider"><inheritdoc cref="IAiFileTokenProvider" path="/summary"/></param>
    /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
    /// <param name="cardFileManager"><inheritdoc cref="ICardFileManager" path="/summary"/></param>
    /// <param name="serverPermissionsProvider"><inheritdoc cref="ICardServerPermissionsProvider" path="/summary"/></param>
    /// <param name="cardToolProcessRunner"><inheritdoc cref="IWorkflowProcessRunner" path="/summary"/></param>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="placeholderManager"><inheritdoc cref="IPlaceholderManager" path="/summary"/></param>
    /// <param name="barcodeManager"><inheritdoc cref="IDocLoadBarcodeManager" path="/summary"/></param>
    /// <param name="aiSettingsProvider"><inheritdoc cref="IAiSettingsProvider" path="/summary"/></param>
    /// <param name="extension"><inheritdoc cref="IDocLoadExtension" path="/summary"/></param>
    /// <remarks>
    /// Стандартный обработчик потокового ввода.
    /// </remarks>
    public sealed class DocLoadAiIncomingFilesBehavior(
        IUnityContainer container,
        IAiAgent aiAgent,
        IAiFileService aiFileService,
        IAiFileTokenProvider aiFileTokenProvider,
        ICardRepository cardRepository,
        ICardFileManager cardFileManager,
        ICardServerPermissionsProvider serverPermissionsProvider,
        IWorkflowProcessRunner cardToolProcessRunner,
        ISession session,
        IDbScope dbScope,
        IPlaceholderManager placeholderManager,
        IDocLoadBarcodeManager barcodeManager,
        IAiSettingsProvider aiSettingsProvider,
        [OptionalDependency] IDocLoadExtension? extension = null) : IDocLoadFilesBehavior
    {
        #region Constants

        private const string AiToolName = "incoming_create_card";

        private const double AiFileServiceStoreTimeoutHours = 0.5d;

        /// Идентификатор процесса Регистрация. 109dd446-11cb-4ef5-a218-7fa2ee105a9e
        private static readonly Guid registrationProcessID = new(0x109dd446, 0x11cb, 0x4ef5, 0xa2, 0x18, 0x7f, 0xa2, 0xee, 0x10, 0x5a, 0x9e);

        // Пока закоментированно, т.к. настройки для DocLoadAiIncomingFilesBehavior с разделением по белой странцие не будут использованы.
        // public const string SeparateByWhitePageKey = "SeparateByWhitePage";

        #endregion

        #region Fields

        private readonly IUnityContainer container = NotNullOrThrow(container);
        private readonly IAiAgent aiAgent = NotNullOrThrow(aiAgent);
        private readonly IAiFileService aiFileService = NotNullOrThrow(aiFileService);
        private readonly IAiFileTokenProvider aiFileTokenProvider = NotNullOrThrow(aiFileTokenProvider);
        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);
        private readonly ICardFileManager cardFileManager = NotNullOrThrow(cardFileManager);
        private readonly ICardServerPermissionsProvider serverPermissionsProvider = NotNullOrThrow(serverPermissionsProvider);
        private readonly IWorkflowProcessRunner cardToolProcessRunner = NotNullOrThrow(cardToolProcessRunner);
        private readonly ISession session = NotNullOrThrow(session);
        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);
        private readonly IPlaceholderManager placeholderManager = NotNullOrThrow(placeholderManager);
        private readonly IDocLoadBarcodeManager barcodeManager = NotNullOrThrow(barcodeManager);
        private readonly IAiSettingsProvider aiSettingsProvider = NotNullOrThrow(aiSettingsProvider);

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Private Members

        /// <summary>
        /// Проверка расширения файла для ИИ обработки.
        /// </summary>
        /// <param name="filePath">Расширение файла.</param>
        /// <returns>Является ли расширение допустимым для ИИ обработки.</returns>
        private static bool HasValidExtensionForAiProcessing(string filePath) =>
            filePath.ToLowerInvariant() switch
            {
                ".tiff" => true,
                ".tif" => true,
                ".png" => true,
                ".jpg" => true,
                ".jpeg" => true,
                ".pdf" => true,
                ".docx" => true,
                ".doc" => true,
                ".xlsx" => true,
                ".xls" => true,
                _ => false
            };

        /// <summary>
        /// Проверка по расширению файла является ли он офисным документом.
        /// </summary>
        /// <remarks>По таким документам не нужно искать разбиения на страницы, штрихкоды и пустые листы, а сразу отправлять на обработку.</remarks>
        /// <param name="filePath">Расширение файла.</param>
        /// <returns>Признак того, что файл является офисным документом.</returns>
        private static bool IsOfficeDocument(string filePath) =>
            filePath.ToLowerInvariant() switch
            {
                ".docx" => true,
                ".doc" => true,
                ".xlsx" => true,
                ".xls" => true,
                _ => false
            };

        /// <summary>
        /// Обрабатывает обычный (не офисный документ) файл со всеми страницами.
        /// Если в процессе обработки возникла какая-либо ошибка, возвращает false,
        /// иначе true.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IPluginExecutingContext" path="/summary"/></param>
        /// <param name="settings"><inheritdoc cref="IDocLoadSettings" path="/summary"/></param>
        /// <param name="subfolderSettingsInfo">Объект с настройками характерными для обрабатываемой подпапки.</param>
        /// <param name="inputFilePath">Файл, полученный на распознавание.</param>
        /// <param name="converter"><inheritdoc cref="IBmpConverter" path="/summary"/></param>
        /// <param name="tempFiles">Список временных файлов, которые используются при обработке.</param>
        /// <param name="aiFileSettings"><inheritdoc cref="AiFileSettings" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Признак успешности операции обработки.</returns>
        private async Task<bool> ProcessOrdinalFileAsync(IPluginExecutingContext context,
            IDocLoadSettings settings,
            Dictionary<string, object?>? subfolderSettingsInfo,
            string inputFilePath,
            IBmpConverter converter,
            List<ITempFile> tempFiles,
            AiFileSettings aiFileSettings,
            CancellationToken cancellationToken = default)
        {
            bool isSucceedFile = true;
            int countFiles = 0;
            int pageCount = converter.GetPageCount();
            IDocLoadDocument? currentDocument = null;
            for (var i = 0; i < pageCount; i++)
            {
                using var page = await converter.ReadAsync(i, settings.ForceRender, cancellationToken);
                (ScanningState result, currentDocument) = await this.ProcessPageAsync(
                    context, currentDocument, settings, page, inputFilePath, i + 1, tempFiles, aiFileSettings, cancellationToken);
                switch (result)
                {
                    case ScanningState.Error:
                        isSucceedFile = false;
                        break;

                    case ScanningState.Barcode:
                        countFiles++;
                        break;
                }
            }

            if (currentDocument is { PageCount: > 0 })
            {
                try
                {
                    return await this.ProcessDocumentAsync(context, settings, currentDocument, aiFileSettings, cancellationToken);
                }
                finally
                {
                    currentDocument.File.Dispose();
                    await currentDocument.DisposeAsync();
                }
            }

            logger.Trace($"{countFiles} documents have been found");
            return isSucceedFile;
        }

        /// <summary>
        /// Обработка очередной страницы в документе.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IPluginExecutingContext" path="/summary"/></param>
        /// <param name="document">Текущий обрабатываемый документ или <c>null</c>, если документ ещё не создан.</param>
        /// <param name="settings"><inheritdoc cref="IDocLoadSettings" path="/summary"/></param>
        /// <param name="bmpPage">Страница в формате bmp.</param>
        /// <param name="inputFilePath">Файл, полученный на распознавание.</param>
        /// <param name="pageNumber">Номер страницы в файле.</param>
        /// <param name="tempFiles">Список временных файлов, которые используются при обработке.</param>
        /// <param name="aiFileSettings"><inheritdoc cref="AiFileSettings" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Состояние сканирования страницы и документ, к которому относится текущая обработанная страница.</returns>
        private async Task<(ScanningState, IDocLoadDocument?)> ProcessPageAsync(IPluginExecutingContext context,
            IDocLoadDocument? document,
            IDocLoadSettings settings,
            Image<Rgba32> bmpPage,
            string inputFilePath,
            int pageNumber,
            List<ITempFile> tempFiles,
            AiFileSettings aiFileSettings,
            CancellationToken cancellationToken = default)
        {
            var barcode =
                await this.barcodeManager.FindBarcodeOnBitmapAsync(
                    bmpPage,
                    settings.BarcodeReadIds,
                    settings.StartScale,
                    settings.IncrementScale,
                    settings.StopScale,
                    cancellationToken)
                ?? string.Empty;

            // Не нашли ШК
            if (string.IsNullOrEmpty(barcode))
            {
                if (document is not null)
                {
                    await document.AppendPageAsync(bmpPage, cancellationToken);
                    return (ScanningState.Page, document);
                }

                // Не нашли ШК и нету документа, создаём новый документ для новой карточки.
                document = await this.CreateNewDocumentAsync(settings, barcode, inputFilePath, tempFiles, cancellationToken);
                await document.AppendPageAsync(bmpPage, cancellationToken);
                logger.Trace("Document found without barcode");
                return (ScanningState.Page, document);
            }

            // Нашли ШК - обрабатываем предыдущий документ.
            if (document is { PageCount: > 0 })
            {
                try
                {
                    if (!await this.ProcessDocumentAsync(context, settings, document, aiFileSettings, cancellationToken: cancellationToken))
                    {
                        return (ScanningState.Error, null);
                    }
                }
                finally
                {
                    document.File.Dispose();
                    await document.DisposeAsync();
                }
            }

            document = await this.CreateNewDocumentAsync(settings, barcode, inputFilePath, tempFiles, cancellationToken);

            if (!settings.ExcludeBarcodePage)
            {
                await document.AppendPageAsync(bmpPage, cancellationToken);
            }

            logger.Trace($"Document found with barcode {barcode}");
            return (ScanningState.Barcode, document);
        }

        /// <summary>
        /// Если метод найдёт карточку со штрих-кодом <paramref name="barcode" />, то возвращает новый объект <see cref="IDocLoadDocument" />,
        /// иначе <c>null</c>.
        /// </summary>
        /// <param name="settings"><inheritdoc cref="IDocLoadSettings" path="/summary"/></param>
        /// <param name="barcode">Найденный штрих-код.</param>
        /// <param name="inputFilePath">Файл, полученный на распознавание.</param>
        /// <param name="tempFiles">Список временных файлов, которые используются при обработке.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Объект документа, найденный по штрих-коду, или <c>null</c>, если документ не найден.</returns>
        private async Task<IDocLoadDocument> CreateNewDocumentAsync(
            IDocLoadSettings settings,
            string barcode,
            string inputFilePath,
            List<ITempFile> tempFiles,
            CancellationToken cancellationToken = default)
        {
            Guid? cardID = null;
            if (extension is not null &&
                !string.IsNullOrEmpty(barcode))
            {
                var context = new DocLoadExtensionContext(this.dbScope, settings, null, inputFilePath, barcode, cancellationToken);
                await using (this.dbScope.Create())
                {
                    try
                    {
                        await extension.RequestAsync(context);
                    }
                    catch (OperationCanceledException)
                    {
                        // ignored
                    }
                    catch (Exception ex)
                    {
                        logger.LogException(ex, LogLevel.Error);
                    }
                }

                cardID = context.CardID;
            }

            // Если был определён ШК и не найденно карточки с таким ШК.
            // Или если ШК не было, то карточку надо будет создать, так что выделим для ней ID.
            cardID ??= Guid.NewGuid();

            var fileExt = Path.GetExtension(inputFilePath);
            switch (fileExt.ToLowerInvariant())
            {
                case ".png":
                case ".jpg":
                case ".pdf":
                    ITempFile pdfTempFile = TempFile.Acquire("temp.pdf");
                    tempFiles.Add(pdfTempFile);
                    return new PdfDocument(pdfTempFile, inputFilePath, barcode, cardID.Value);

                case ".tiff":
                case ".tif":
                    ITempFile tiffTempFile = TempFile.Acquire("temp.tif");
                    tempFiles.Add(tiffTempFile);
                    return new TiffDocument(tiffTempFile, inputFilePath, barcode, cardID.Value);

                default:
                    throw ArgumentOutOfRange(fileExt, $"Unknown file extension: {fileExt}");
            }
        }


        /// <summary>
        /// Обрабатывает документ с использованием AI плагина.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IPluginExecutingContext" path="/summary"/></param>
        /// <param name="settings"><inheritdoc cref="IDocLoadSettings" path="/summary"/></param>
        /// <param name="document"><inheritdoc cref="IDocLoadDocument" path="/summary"/></param>
        /// <param name="aiFileSettings"><inheritdoc cref="AiFileSettings" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Признак успешности операции обработки.</returns>
        private async Task<bool> ProcessDocumentAsync(IPluginExecutingContext context,
            IDocLoadSettings settings,
            IDocLoadDocument document,
            AiFileSettings aiFileSettings,
            CancellationToken cancellationToken = default)
        {
            await document.CloseAsync(context.CancellationToken);
            return await this.ProcessDocumentAsync(context, settings, document.File.Path, document.CardID, aiFileSettings, document.Barcode, cancellationToken);
        }

        /// <summary>
        /// Обрабатывает документ с использованием AI плагина.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IPluginExecutingContext" path="/summary"/></param>
        /// <param name="settings"><inheritdoc cref="IDocLoadSettings" path="/summary"/></param>
        /// <param name="documentFilePath">Путь к файлу документа.</param>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <param name="barcode">Штрих-код.</param>
        /// <param name="aiFileSettings"><inheritdoc cref="AiFileSettings" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Признак успешности операции обработки.</returns>
        private async Task<bool> ProcessDocumentAsync(IPluginExecutingContext context,
            IDocLoadSettings settings,
            string documentFilePath,
            Guid cardID,
            AiFileSettings aiFileSettings,
            string? barcode = null,
            CancellationToken cancellationToken = default)
        {
            var fileName = await LocalizeAsync(await this.GetFileNameAsync(this.dbScope, settings, cardID, cancellationToken)) + Path.GetExtension(documentFilePath);
            await using var content = await RemoteFileContent.FromFilePathAsync(documentFilePath, cancellationToken: cancellationToken);
            var status = await this.aiFileService.StoreLooseFileAsync(fileName, content, AiFileRequestOperation.Text, context.CancellationToken);
            if (status.State == AiFileResponseState.InProgress)
            {
                var fileID = status.FileID;
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                do
                {
                    status = await this.aiFileService.TryGetStatusAsync(fileID, AiFileRequestOperation.Text, context.CancellationToken);
                    await Task.Delay(aiFileSettings.ContentExtractionCheckInterval, cancellationToken);
                } while (status?.State == AiFileResponseState.InProgress && stopWatch.Elapsed <= TimeSpan.FromHours(AiFileServiceStoreTimeoutHours));
            }

            if (status?.State == AiFileResponseState.Completed)
            {
                var token =
                    await this.aiFileTokenProvider.CreateTokenAsync(
                        new() { BaseFileID = status.FileID, Permission = AiFileTokenPermission.All }, context.CancellationToken);

                var aiRequest =
                    new AiRequest
                    {
                        Tool = AiToolName,
                        Context = new AiContext(AiContextType.None, Guid.Empty, null, null),
                        Messages =
                        [
                            new AiMessage
                            {
                                Content =
                                [
                                    new AiFileMessagePart
                                    {
                                        FileID = status.FileID,
                                        Kind = AiFileRequestOperation.Text,
                                        Token = token
                                    }
                                ],
                                Role = AiRoles.User
                            }
                        ]
                    };
                var aiResponse = await this.aiAgent.ProcessAsync(aiRequest, context.CancellationToken);
                if (aiResponse.Type == AiResponseType.ToolConfirmation)
                {
                    // Эмулируем согласие пользоваиеля.
                    aiRequest.Type = AiRequestType.ToolActionConfirmation;
                    aiRequest.Action = AiActions.ConfirmID;
                    aiRequest.Data = aiResponse.Data;
                    aiResponse = await this.aiAgent.ProcessAsync(aiRequest, context.CancellationToken);
                    if (aiResponse is
                        {
                            Type: AiResponseType.ToolResult,
                            Action.Data.PreparedNewCard: { } preparedNewCard,
                            Action.Data.PreparedNewCardSignature: { } preparedNewCardSignature,
                            Action.Data.CardTypeID: { } cardTypeID,
                            Action.Data.DocTypeID: { } docTypeID,
                            Action.Data.DocTypeTitle: { } docTypeTitle,
                        })
                    {
                        Card card;
                        var info =
                            new Dictionary<string, object?>
                            {
                                { "docTypeID", docTypeID },
                                { "docTypeTitle", docTypeTitle },
                                { CardHelper.NewCardBilletKey, preparedNewCard },
                                { CardHelper.NewCardBilletSignatureKey, preparedNewCardSignature },
                            };
                        if (await this.HasCardAsync(cardID, cancellationToken))
                        {
                            var cardGetRequest = new CardGetRequest
                            {
                                CardID = cardID,
                                Info = info
                            };
                            this.serverPermissionsProvider.SetFullPermissions(cardGetRequest);
                            var getResponse = await this.cardRepository.GetAsync(cardGetRequest, cancellationToken);
                            if (!getResponse.ValidationResult.IsSuccessful())
                            {
                                return false;
                            }

                            card = getResponse.Card;
                        }
                        else
                        {
                            var cardNewRequest = new CardNewRequest
                            {
                                CardTypeID = cardTypeID,
                                Info = info
                            };
                            this.serverPermissionsProvider.SetFullPermissions(cardNewRequest);
                            var cardNewResponse = await this.cardRepository.NewAsync(cardNewRequest, context.CancellationToken);
                            if (!cardNewResponse.ValidationResult.IsSuccessful())
                            {
                                return false;
                            }

                            card = cardNewResponse.Card;
                            if (!string.IsNullOrWhiteSpace(barcode))
                            {
                                card.Sections["DocumentCommonInfo"].Fields["Barcode"] = barcode;
                            }

                            // Присвоим идентификатор карточки, чтобы не было проблем с прикладыванием файла,
                            // из которого происходило распознавание в созданную карточку.
                            card.ID = cardID;
                        }

                        var fileContainer = await this.cardFileManager.CreateContainerAsync(card, cancellationToken: context.CancellationToken);
                        var fileContainerStoreResult =
                            await fileContainer.StoreAsync(
                                (_, cardStoreRequest, _) =>
                                {
                                    this.serverPermissionsProvider.SetFullPermissions(cardStoreRequest);
                                    return ValueTask.CompletedTask;
                                },
                                cancellationToken: cancellationToken);

                        if (fileContainerStoreResult.ValidationResult.IsSuccessful())
                        {
                            await this.cardToolProcessRunner.StartProcessAsync(
                                new WorkflowProcessRunnerContext
                                {
                                    CardID = card.ID,
                                    ProcessID = registrationProcessID,
                                    ProcessType = WorkflowProcessTypes.KrProcess,
                                    ValidationResult = context.ValidationResult
                                },
                                context.CancellationToken);
                            if (context.ValidationResult.IsSuccessful())
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Проверяет наличие в системе карточки с указанным идентификатором.
        /// </summary>
        /// <param name="cardID">Проверяемый идентификатор.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если карточка с указанным <paramref name="cardID"/> существует, иначе <see langword="false"/></returns>
        private async Task<bool> HasCardAsync(Guid cardID, CancellationToken cancellationToken = default)
        {
            await using (this.dbScope.Create())
            {
                DbManager db = this.dbScope.Db;
                return await db
                    .SetCommand(
                        this.dbScope.BuilderFactory
                            .Cached(this, nameof(this.HasCardAsync), q => q
                            .Select().Top(1).V(true)
                            .From("Instances").NoLock()
                            .Where().C("ID").Equals().P("ID")
                            .Limit(1)
                            .Build()),
                        db.Parameter("ID", cardID, DataType.Guid))
                    .LogCommand()
                    .ExecuteAsync<bool>(cancellationToken);
            }
        }

        /// <summary>
        /// Получение имени файла.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="settings"><inheritdoc cref="IDocLoadSettings" path="/summary"/></param>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Имя файла.</returns>
        private async Task<string> GetFileNameAsync(IDbScope dbScope, IDocLoadSettings settings, Guid cardID, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(settings.DocFormatName);

            if (settings.DocFormatName.Contains('{', StringComparison.Ordinal))
            {
                var info = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    { PlaceholderHelper.ContextKey, null },
                    { PlaceholderHelper.SessionKey, this.session },
                    { PlaceholderHelper.UnityContainerKey, this.container },
                    { PlaceholderHelper.DbScopeKey, dbScope },
                    { PlaceholderHelper.CardIDKey, cardID },
                };

                var fileNameDocument = new StringPlaceholderDocument(settings.DocFormatName);

                ValidationResult result = await this.placeholderManager.FindAndReplaceAsync(
                    fileNameDocument, info, FindingOptions.SkipUnknown, cancellationToken: cancellationToken);

                logger.LogResult(result);

                return result.IsSuccessful ? fileNameDocument.Text : "$CardTypes_TypesNames_DocLoad_Filename";
            }

            return settings.DocFormatName;
        }

        #endregion

        #region IDocLoadFilesBehavior Members

        /// <inheritdoc />
        public async Task ProcessFilesAsync(
            IPluginExecutingContext context,
            IDocLoadSettings settings,
            Dictionary<string, object?>? subfolderSettingsInfo,
            IList<string> inputFilePaths,
            string outputFolder,
            CancellationToken cancellationToken = default)
        {
            var aiSettings = await this.aiSettingsProvider.GetCommonSettingsAsync(cancellationToken);
            if (!aiSettings.Enabled)
            {
                throw new InvalidOperationException("$Ai_AiAgent_Validation_AiModuleDisabled");
            }

            var aiFilesSettings = await this.aiSettingsProvider.GetFileSettingsAsync(cancellationToken);

            var documentFiles = new List<string>();
            foreach (var file in inputFilePaths)
            {
                bool checkExtension = extension is not null && await extension.IsScanFileAsync(file, context.CancellationToken);

                if (checkExtension || HasValidExtensionForAiProcessing(Path.GetExtension(file)))
                {
                    documentFiles.Add(file);
                }
            }

            // Обработка документов из подпапок с настроенными ИИ инструментом.
            foreach (var documentFilePath in documentFiles)
            {
                if (context.StopRequested)
                {
                    return;
                }

                logger.Trace($"Processing file \"{documentFilePath}\"");

                try
                {
                    bool isSuccess;
                    if (!IsOfficeDocument(Path.GetExtension(documentFilePath)))
                    {
                        var tempFiles = new List<ITempFile>();
                        await using var document = new ScannedDocument(documentFilePath);

                        await document.OpenAsync(context.CancellationToken);
                        isSuccess = await this.ProcessOrdinalFileAsync(
                            context, settings, subfolderSettingsInfo, documentFilePath, document.Converter, tempFiles, aiFilesSettings, context.CancellationToken);
                    }
                    else
                    {
                        isSuccess = await this.ProcessDocumentAsync(context, settings, documentFilePath, Guid.NewGuid(), aiFilesSettings, cancellationToken: context.CancellationToken);
                    }

                    await ProcessErrorOrSuccessFileAsync(
                        documentFilePath,
                        isSuccess
                            ? settings.OutputPath!
                            : settings.ErrorPath!,
                        isSuccess
                            ? $"File \"{documentFilePath}\" processed successfully"
                            : $"File \"{documentFilePath}\" processed with errors");
                }
                catch (Exception ex)
                {
                    logger.LogException($"Error processing file \"{documentFilePath}\"", ex);
                    if (ex is not IOException)
                    {
                        await ProcessErrorOrSuccessFileAsync(documentFilePath, settings.ErrorPath!, $"File \"{documentFilePath}\" processed with errors");
                    }
                }
            }
            return;

            async Task ProcessErrorOrSuccessFileAsync(string fileNameParam, string basePath, string logMessage)
            {
                logger.Trace(logMessage);
                if (Directory.Exists(basePath))
                {
                    var relativePath = Path.GetRelativePath(Path.GetDirectoryName(settings.InputPath)!, Path.GetDirectoryName(fileNameParam)!);
                    var errorPath = Path.Combine(basePath, relativePath);
                    var targetPath = Path.Combine(errorPath, outputFolder);
                    await DocLoadHelper.MoveFileToDirAsync(targetPath, fileNameParam, context.CancellationToken);
                }
                else
                {
                    await DocLoadHelper.DeleteFileAsync(fileNameParam, context.CancellationToken);
                }
            }
        }

        /// <inheritdoc />
        public Dictionary<string, object?> DefaultSettings =>
            new();
            // Пока закоментированно, т.к. настройки для DocLoadAiIncomingFilesBehavior с разделением по белой странцие не будут использованы.
            //{
            //    { SeparateByWhitePageKey, false }
            //};

        /// <inheritdoc />
        public string Name => "$DocLoad_AI_Incoming_Create_Card_Behavior";

        /// <inheritdoc />
        public string Description => "$DocLoad_AI_Incoming_Create_Card_Behavior_Description";

        #endregion
    }
}
