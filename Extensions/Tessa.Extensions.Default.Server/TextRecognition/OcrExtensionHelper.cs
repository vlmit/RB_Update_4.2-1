#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Files;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.TextRecognition.Constants;
using Tessa.TextRecognition.Enums;

namespace Tessa.Extensions.Default.Server.TextRecognition
{
    /// <summary>
    /// Общие вспомогательные методы для расширений по решению OCR.
    /// </summary>
    public static class OcrExtensionHelper
    {
        #region Public Methods

        /// <summary>
        /// Создание в системе карточки операции OCR без связи с исходной карточкой документа.
        /// </summary>
        /// <param name="cardRepositoryExtended"><inheritdoc cref="ICardRepository" path="/summary"/></param>
        /// <param name="cardFileManagerExtended"><inheritdoc cref="ICardFileManager" path="/summary"/></param>
        /// <param name="user"><inheritdoc cref="IUser" path="/summary"/></param>
        /// <param name="fileInfo">Информация с контентом и именем распознаваемого файла.</param>
        /// <param name="sourceCardTypeID">Идентификатор типа исходной карточки документа.</param>
        /// <param name="sourceCardTypeName">Название типа исходной карточки документа.</param>
        /// <param name="sourceDocTypeID">Идентификатор типа документа исходной карточки.</param>
        /// <param name="sourceDocTypeTitle">Заголовок типа документа исходной карточки.</param>
        /// <param name="ocrCardID">Идентификатор создаваемой карточко операции OCR.</param>
        /// <param name="request">
        /// Инфорация с параметрами запроса на распознавание файла.
        /// Если информация не задана, то используются параметры по умолчанию.
        /// </param>
        /// <param name="requestLanguages">
        /// Инфорация о языках, используемых в запросе на распознавание файла.
        ///  Если информация не задана, то в запросе устанавливается признак автоматического опеределения языка.
        /// </param>
        /// <param name="createWithCompletedRequest">
        /// Признак создания запроса на распознавание в состоянии <see cref="OcrRequestStates.Completed"/>.
        /// </param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValidationResult" path="/summary"/></returns>
        public static async Task<ValidationResult> CreateOcrOperationCardAsync(
            ICardRepository cardRepositoryExtended,
            ICardFileManager cardFileManagerExtended,
            IUser user,
            (Stream Content, string Name) fileInfo,
            Guid sourceCardTypeID,
            string? sourceCardTypeName,
            Guid? sourceDocTypeID = null,
            string? sourceDocTypeTitle = null,
            Guid? ocrCardID = null,
            IDictionary<string, object?>? request = null,
            List<object>? requestLanguages = null,
            bool createWithCompletedRequest = false,
            CancellationToken cancellationToken = default)
        {
            var validationResult = new ValidationResultBuilder();
            var ocrCard = await CreateOperationCardAsync(ocrCardID, cardRepositoryExtended, validationResult, cancellationToken);
            if (ocrCard is not null)
            {
                var ocrOperations = ocrCard.Sections[nameof(OcrOperations)].Fields;
                CopyOperationCardInfo(null, sourceCardTypeID, sourceCardTypeName, ocrOperations);
                CopyOperationCardTypeInfo(sourceDocTypeID, sourceDocTypeTitle, ocrOperations);

                var requestRow = CreateRequestRow(user, createWithCompletedRequest, request);
                ocrCard.Sections[nameof(OcrRequests)].Rows.Add(requestRow);

                var requestLanguagesRows = CreateRequestLanguagesRows(requestRow.RowID, requestLanguages);
                ocrCard.Sections[nameof(OcrRequestsLanguages)].Rows.AddRange(requestLanguagesRows);

                if (requestLanguages is not { Count: > 0 })
                {
                    requestRow[OcrRequests.DetectLanguages] = BooleanBoxes.True;
                }

                await SaveOperationCardWithFilesAsync(ocrCard, user, fileInfo, validationResult, cardFileManagerExtended, cancellationToken);
            }

            return validationResult.Build();
        }

        /// <summary>
        /// Создание в системе карточки операции OCR, связанной с исходной карточкой документа.
        /// </summary>
        /// <param name="cardRepositoryExtended"><inheritdoc cref="ICardRepository" path="/summary"/></param>
        /// <param name="user"><inheritdoc cref="IUser" path="/summary"/></param>
        /// <param name="sourceCardFile">Карточка файла с информацией о распознаваемом файле.</param>
        /// <param name="sourceCard">Исходная карточка документа, содержащая распознаваемый файл.</param>
        /// <param name="ocrCardID">Идентификатор создаваемой карточко операции OCR.</param>
        /// <param name="request">
        /// Инфорация с параметрами запроса на распознавание файла.
        /// Если информация не задана, то используются параметры по умолчанию.
        /// </param>
        /// <param name="requestLanguages">
        /// Инфорация о языках, используемых в запросе на распознавание файла.
        ///  Если информация не задана, то в запросе устанавливается признак автоматического опеределения языка.
        /// </param>
        /// <param name="createWithCompletedRequest">
        /// Признак создания запроса на распознавание в состоянии <see cref="OcrRequestStates.Completed"/>.
        /// </param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValidationResult" path="/summary"/></returns>
        public static async Task<ValidationResult> CreateOcrOperationCardAsync(
            ICardRepository cardRepositoryExtended,
            IUser user,
            CardFile sourceCardFile,
            Card sourceCard,
            Guid? ocrCardID = null,
            IDictionary<string, object?>? request = null,
            List<object>? requestLanguages = null,
            bool createWithCompletedRequest = false,
            CancellationToken cancellationToken = default)
        {
            var validationResult = new ValidationResultBuilder();
            var ocrCard = await CreateOperationCardAsync(ocrCardID, cardRepositoryExtended, validationResult, cancellationToken);
            if (ocrCard is not null)
            {
                var ocrOperations = ocrCard.Sections[nameof(OcrOperations)].Fields;
                if (await CopyOperationCardTypeInfoAsync(sourceCard.ID, ocrOperations, cardRepositoryExtended, validationResult, cancellationToken))
                {
                    CopyOperationCardInfo(sourceCard.ID, sourceCard.TypeID, sourceCard.TypeName, ocrOperations);
                    CopyOperationCardFileInfo(sourceCardFile, ocrOperations);

                    var requestRow = CreateRequestRow(user, createWithCompletedRequest, request);
                    ocrCard.Sections[nameof(OcrRequests)].Rows.Add(requestRow);

                    var requestLanguagesRows = CreateRequestLanguagesRows(requestRow.RowID, requestLanguages);
                    ocrCard.Sections[nameof(OcrRequestsLanguages)].Rows.AddRange(requestLanguagesRows);

                    if (requestLanguages is not { Count: > 0 })
                    {
                        requestRow[OcrRequests.DetectLanguages] = BooleanBoxes.True;
                    }

                    await SaveOperationCardAsync(ocrCard, cardRepositoryExtended, validationResult, cancellationToken);
                }
            }

            return validationResult.Build();
        }

        #endregion

        #region Private Methods

        private static async Task<Card?> CreateOperationCardAsync(
            Guid? ocrCardID,
            ICardRepository cardRepository,
            ValidationResultBuilder validationResult,
            CancellationToken cancellationToken)
        {
            var newRequest = new CardNewRequest
            {
                CardTypeID = OcrCardTypes.OcrOperationTypeID,
                CardTypeName = OcrCardTypes.OcrOperationTypeName
            };

            var newResponse = await cardRepository.NewAsync(newRequest, cancellationToken);
            validationResult.Add(newResponse.ValidationResult);
            if (!newResponse.ValidationResult.IsSuccessful())
            {
                return null;
            }

            var ocrCard = newResponse.Card;
            ocrCard.ID = ocrCardID ?? Guid.NewGuid();
            return ocrCard;
        }

        private static async Task<bool> SaveOperationCardAsync(
            Card ocrCard,
            ICardRepository cardRepository,
            ValidationResultBuilder validationResult,
            CancellationToken cancellationToken)
        {
            // Пытаемся сохранить карточку OCR. В расширении на сохранение будет создана операция распознавания.
            var storeRequest = new CardStoreRequest { Card = ocrCard };
            var storeResponse = await cardRepository.StoreAsync(storeRequest, cancellationToken);
            validationResult.Add(storeResponse.ValidationResult);
            return storeResponse.ValidationResult.IsSuccessful();
        }

        private static async Task<bool> SaveOperationCardWithFilesAsync(
            Card ocrCard,
            IUser user,
            (Stream Content, string Name) fileInfo,
            ValidationResultBuilder validationResult,
            ICardFileManager cardFileManager,
            CancellationToken cancellationToken)
        {
            await using var container = await cardFileManager.CreateContainerAsync(ocrCard, cancellationToken: cancellationToken);
            validationResult.Add(container.CreationResult);
            if (!container.CreationResult.IsSuccessful)
            {
                return false;
            }

            var (file, buildResult) = await container.FileContainer
                .BuildFile(fileInfo.Name)
                .SetContent(fileInfo.Content)
                .SetModified(DateTime.UtcNow, user.ID, user.Name)
                .AddWithNotificationAsync(cancellationToken: cancellationToken);

            validationResult.Add(buildResult);
            if (!buildResult.IsSuccessful || file is null)
            {
                return false;
            }

            var storeResponse = await container.StoreAsync(cancellationToken: cancellationToken);
            var storeResult = storeResponse.ValidationResult.Build();
            validationResult.Add(storeResult);
            return storeResult.IsSuccessful;
        }

        private static void CopyOperationCardInfo(
            Guid? sourceCardID,
            Guid sourceCardTypeID,
            string? sourceCardTypeName,
            IDictionary<string, object?> target)
        {
            target[OcrOperations.CardID] = sourceCardID;
            target[OcrOperations.CardTypeID] = sourceCardTypeID;
            target[OcrOperations.CardTypeName] = sourceCardTypeName;
        }

        private static void CopyOperationCardFileInfo(
            CardFile sourceCardFile,
            IDictionary<string, object?> target)
        {
            target[OcrOperations.FileID] = sourceCardFile.Card.ID;
            target[OcrOperations.FileName] = sourceCardFile.Name;
            target[OcrOperations.FileTypeID] = sourceCardFile.TypeID;
            target[OcrOperations.FileTypeName] = sourceCardFile.TypeName;
            target[OcrOperations.VersionRowID] = sourceCardFile.VersionRowID;
        }

        private static void CopyOperationCardTypeInfo(
            Guid? sourceDocTypeID,
            string? sourceDocTypeTitle,
            IDictionary<string, object?> target)
        {
            target[OcrOperations.DocTypeID] = sourceDocTypeID;
            target[OcrOperations.DocTypeTitle] = sourceDocTypeTitle;
        }

        private static async Task<bool> CopyOperationCardTypeInfoAsync(
            Guid sourceCardID,
            IDictionary<string, object?> target,
            ICardRepository cardRepository,
            ValidationResultBuilder validationResult,
            CancellationToken cancellationToken)
        {
            var (result, _, docTypeID, docTypeTitle) =
                await DefaultExtensionHelper.GetDocTypeInfoAsync(cardRepository, sourceCardID, cancellationToken);

            validationResult.Add(result);

            if (result.IsSuccessful)
            {
                CopyOperationCardTypeInfo(docTypeID, docTypeTitle, target);
                return true;
            }

            return false;
        }

        private static CardRow CreateRequestRow(
            IUser user,
            bool createWithCompletedRequest,
            IDictionary<string, object?>? request = null) =>
            new()
            {
                State = CardRowState.Inserted,
                RowID = request?.TryGet<Guid?>(nameof(OcrRequests.RowID)) ?? Guid.NewGuid(),
                [OcrRequests.Created] = DateTime.UtcNow,
                [OcrRequests.CreatedByID] = user.ID,
                [OcrRequests.CreatedByName] = user.Name,
                [OcrRequests.Overwrite] = request?[OcrRequests.Overwrite] ?? BooleanBoxes.False,
                [OcrRequests.Preprocess] = request?[OcrRequests.Preprocess] ?? BooleanBoxes.False,
                [OcrRequests.DetectTables] = request?[OcrRequests.DetectTables] ?? BooleanBoxes.False,
                [OcrRequests.DetectRotation] = request?[OcrRequests.DetectRotation] ?? BooleanBoxes.False,
                [OcrRequests.DetectBarcodes] = request?[OcrRequests.DetectBarcodes] ?? BooleanBoxes.False,
                [OcrRequests.DetectLanguages] = request?[OcrRequests.DetectLanguages] ?? BooleanBoxes.True,
                [OcrRequests.Confidence] = request?[OcrRequests.Confidence] ?? Int32Boxes.Box(50),
                [OcrRequests.SegmentationModeID] = request?[OcrRequests.SegmentationModeID] ?? Int32Boxes.Box((int) OcrSegmentationMode.AutoOsd),
                [OcrRequests.SegmentationModeName] = request?[OcrRequests.SegmentationModeName] ?? "$Enum_OcrSegmentationModes_AutoOsd",
                [OcrRequests.StateID] = Int32Boxes.Box((int) (createWithCompletedRequest ? OcrRequestStates.Completed : OcrRequestStates.Created))
            };

        private static CardRow CreateLanguageRow(
            Guid requestID,
            IDictionary<string, object?>? language = null) =>
            new()
            {
                State = CardRowState.Inserted,
                RowID = Guid.NewGuid(),
                ParentRowID = requestID,
                [OcrRequestsLanguages.LanguageID] = language?[OcrRequestsLanguages.LanguageID] ?? Int32Boxes.MinusOne,
                [OcrRequestsLanguages.LanguageISO] = language?[OcrRequestsLanguages.LanguageISO] ?? null,
                [OcrRequestsLanguages.LanguageCaption] = language?[OcrRequestsLanguages.LanguageCaption] ?? "Auto"
            };

        private static CardRow[] CreateRequestLanguagesRows(
            Guid requestID,
            List<object>? requestLanguages) =>
            requestLanguages is not { Count: > 0 }
                ? [CreateLanguageRow(requestID)]
                : requestLanguages
                    .Cast<IDictionary<string, object?>>()
                    .DistinctBy(static language => language.Get<int>(OcrRequestsLanguages.LanguageID))
                    .Select(language => CreateLanguageRow(requestID, language))
                    .ToArray();

        #endregion
    }
}
