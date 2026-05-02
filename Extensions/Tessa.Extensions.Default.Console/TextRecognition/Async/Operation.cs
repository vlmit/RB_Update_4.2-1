using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Jinni;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.TextRecognition;
using Tessa.TextRecognition.Constants;
using Tessa.TextRecognition.Enums;
using Unity;

namespace Tessa.Extensions.Default.Console.TextRecognition.Async
{
    /// <summary>
    /// Асинхронная операция распознавания файла.
    /// </summary>
    public sealed class Operation : Base.Operation<OperationContext, OcrAsyncRequest, OcrAsyncResponse>
    {
        #region Fields

        private readonly ISession session;
        private readonly ICardRepository cardRepository;

        #endregion

        #region Constructors

        public Operation(
            ISession session,
            [Dependency(CardRepositoryNames.Extended)]
            ICardRepository cardRepository,
            IConsoleSessionManager sessionManager,
            IConsoleLogger logger,
            IOcrAsyncService ocrService)
            : base(logger, sessionManager, ocrService, extendedInitialization: true)
        {
            this.session = NotNullOrThrow(session);
            this.cardRepository = NotNullOrThrow(cardRepository);
        }

        #endregion

        /// <inheritdoc />
        public override async Task<int> ExecuteAsync(
            OperationContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (operationID, _) = await this.CreateOperationAsync(context, cancellationToken);
                if (!operationID.HasValue)
                {
                    return -1;
                }

                cancellationToken.ThrowIfCancellationRequested();
                var isSuccessful = await this.WaitOperationAsync(operationID.Value, context, token: null, cancellationToken);
                if (!isSuccessful)
                {
                    return -1;
                }

                cancellationToken.ThrowIfCancellationRequested();
                var result = await this.GetOperationResultAsync(operationID.Value, context, token: null, cancellationToken);
                if (result is null)
                {
                    return -1;
                }

                await this.Logger.InfoAsync(
                    StringBuilderHelper.Acquire(256)
                        .AppendLine($"  - Card file identifier with recognized content: \"{result.ContentFileID}\",")
                        .AppendLine($"  - Card file identifier with recognized metadata: \"{result.MetadataFileID}\",")
                        .AppendLine(result.Info?.HasTextLayer ?? false
                            ? "  - Text layer has been detected at recognized files."
                            : "  - Text layer has not been detected at recognized files.")
                        .ToStringAndRelease());

                return 0;
            }
            catch (OperationCanceledException)
            {
                await this.Logger.InfoAsync("OCR operation was cancelled.");
                return 0;
            }
            catch (Exception ex)
            {
                await this.Logger.LogExceptionAsync("An error occurred during the file recognition process.", ex);
                return -1;
            }
        }

        /// <inheritdoc />
        protected override async Task<(Guid? OperationID, JinniBalancingToken? Token)> CreateOperationAsync(
            OperationContext context,
            CancellationToken cancellationToken = default)
        {
            await this.Logger.WriteLineAsync();
            await this.Logger.InfoAsync($"Try create OCR operation with parameters:{Environment.NewLine}{context.GetDescription()}");
            await this.Logger.WriteLineAsync();

            var requestID = Guid.NewGuid();

            // Отправляем запрос на получение идентификатора карточки операции OCR, проассоциированной с идентификатором версии файла,
            // а также идентификатора основной карточки, в которой находится распознаваемый файл
            var (ocrCardID, cardID) = await this.GetOcrInfoAsync(context.FileID, context.ValidationResult, cancellationToken);
            if (!context.ValidationResult.IsSuccessful())
            {
                await this.Logger.LogResultAsync(context.ValidationResult.Build());
                return (null, null);
            }

            // Если карточка операции OCR существует, то работаем с ней и создаем запросы через нее
            if (ocrCardID.HasValue)
            {
                var ocrCard = await this.GetCardAsync(ocrCardID.Value, context.ValidationResult, cancellationToken);
                if (ocrCard is not null)
                {
                    ocrCard.Sections[nameof(OcrRequests)].Rows.Add(CreateRequestRow(requestID, context.Parameters, this.session.User));
                    ocrCard.Sections[nameof(OcrRequestsLanguages)].Rows.AddRange(CreateRequestLanguagesRows(requestID, context.Parameters.Languages));
                    ocrCard.RemoveAllButChanged();

                    if (await this.StoreCardAsync(ocrCard, context.ValidationResult, null, cancellationToken))
                    {
                        return (requestID, null);
                    }
                }

                await this.Logger.LogResultAsync(context.ValidationResult.Build());
                return (null, null);
            }

            // В противном случае, запросы будем создавать через сохранение основной карточки
            if (!cardID.HasValue)
            {
                await this.Logger.ErrorAsync("An error occurred while creating OCR operation. Not found card identifier for file version identifier.");
                return (null, null);
            }

            var card = await this.GetCardAsync(cardID.Value, context.ValidationResult, cancellationToken);
            if (card is null)
            {
                await this.Logger.LogResultAsync(context.ValidationResult.Build());
                return (null, null);
            }

            // Установка параметров OCR в опциях файла, если их не было
            var cardFile = card.Files.First(f => f.RowID == context.FileID);
            cardFile.Info[OcrCommon.OcrKey] = BooleanBoxes.True;
            var options = cardFile.DeserializeOptions();
            if (!options.TryGetValue(OcrCommon.OcrKey, out var ocrOptions) || ocrOptions is null)
            {
                options[OcrCommon.OcrKey] = new Dictionary<string, object?> { ["CardID"] = Guid.NewGuid() };
            }
            cardFile.SetOptions(options);
            cardFile.Flags |= CardFileFlags.UpdateOptions;
            cardFile.State = CardFileState.Modified;

            card.RemoveAllButChanged();

            // Сохранение карточки с измененным файлом
            if (!await this.StoreCardAsync(
                    card,
                    context.ValidationResult,
                    storeRequest =>
                    {
                        storeRequest.Info[OcrCommon.OcrKey] = new Dictionary<string, object?>
                        {
                            [nameof(OcrRequests)] = CreateRequestRow(requestID, context.Parameters, this.session.User).GetStorage(),
                            [nameof(OcrRequestsLanguages)] = CreateRequestLanguagesRows(requestID, context.Parameters.Languages)
                                .Select(r => (object) r.GetStorage())
                                .ToList()
                        };
                    },
                    cancellationToken))
            {
                await this.Logger.LogResultAsync(context.ValidationResult.Build());
                return (null, null);
            }

            return (requestID, null);
        }

        #region Private

        private async Task<Card?> GetCardAsync(
            Guid cardID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken)
        {
            var request = new CardGetRequest { CardID = cardID };
            var response = await this.cardRepository.GetAsync(request, cancellationToken);
            validationResult.Add(response.ValidationResult);
            return response.TryGetCard();
        }

        private async Task<bool> StoreCardAsync(
            Card card,
            IValidationResultBuilder validationResult,
            Action<CardStoreRequest>? modifyStoreRequest = null,
            CancellationToken cancellationToken = default)
        {
            var request = new CardStoreRequest { Card = card };
            modifyStoreRequest?.Invoke(request);
            var response = await this.cardRepository.StoreAsync(request, cancellationToken);
            var result = response.ValidationResult.Build();
            validationResult.Add(result);
            return result.IsSuccessful;
        }

        private async Task<(Guid? ID, Guid? CardID)> GetOcrInfoAsync(
            Guid fileID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken)
        {
            var request = new CardRequest { FileID = fileID, RequestType = CardRequestTypes.GetTextRecognitionOperationInfo };
            var response = await this.cardRepository.RequestAsync(request, cancellationToken);
            validationResult.Add(response.ValidationResult);

            if (response.ValidationResult.IsSuccessful())
            {
                var ocrCardID = response.Info.TryGet<Guid?>(OcrOperations.ID);
                var mainCardID = response.Info.TryGet<Guid?>(OcrOperations.CardID);
                return (ocrCardID, mainCardID);
            }

            return (null, null);
        }

        private static CardRow CreateRequestRow(Guid requestID, OcrParameters parameters, IUser user) => new()
        {
            State = CardRowState.Inserted,
            RowID = requestID,
            [OcrRequests.Created] = DateTime.UtcNow,
            [OcrRequests.CreatedByID] = user.ID,
            [OcrRequests.CreatedByName] = user.Name,
            [OcrRequests.StateID] = Int32Boxes.Box((int) OcrRequestStates.Created),
            [OcrRequests.Confidence] = Int32Boxes.Box(parameters.Confidence),
            [OcrRequests.Preprocess] = BooleanBoxes.Box(parameters.Preprocess),
            [OcrRequests.SegmentationModeID] = Int32Boxes.Box((int) parameters.SegmentationMode),
            [OcrRequests.SegmentationModeName] = parameters.SegmentationMode.GetDescription(),
            [OcrRequests.DetectLanguages] = BooleanBoxes.Box(!parameters.Languages.Any()),
            [OcrRequests.Overwrite] = BooleanBoxes.Box(parameters.Overwrite),
            [OcrRequests.DetectRotation] = BooleanBoxes.Box(parameters.DetectRotation),
            [OcrRequests.DetectTables] = BooleanBoxes.Box(parameters.DetectTables),
            [OcrRequests.DetectBarcodes] = BooleanBoxes.Box(parameters.DetectBarcodes)
        };

        private static List<CardRow> CreateRequestLanguagesRows(Guid requestID, OcrLanguage[] languages) => languages
            .Select(language => new CardRow
            {
                State = CardRowState.Inserted,
                RowID = Guid.NewGuid(),
                ParentRowID = requestID,
                [OcrRequestsLanguages.LanguageID] = Int32Boxes.Box((int) language),
                [OcrRequestsLanguages.LanguageISO] = language.ToString(),
                [OcrRequestsLanguages.LanguageCaption] = language.GetDescription()
            }).ToList();

        #endregion
    }
}
