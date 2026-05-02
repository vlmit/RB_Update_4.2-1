#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using NLog;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.IO;
using Tessa.Platform.Operations;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.TextRecognition;
using Tessa.TextRecognition.Constants;
using Tessa.TextRecognition.Enums;

namespace Tessa.Extensions.Default.Server.TextRecognition
{
    /// <summary>
    /// Расширение на сохранение карточки операции OCR, в котором выполняется создание операции для распознавания текста в файле.
    /// </summary>
    /// <remarks>
    /// Создает экземпляр класса <see cref="OcrOperationStoreExtension"/>.
    /// </remarks>
    /// <param name="ocrService"><inheritdoc cref="IOcrAsyncService" path="/summary"/></param>
    /// <param name="operationRepository"><inheritdoc cref="IOperationRepository" path="/summary"/></param>
    /// <param name="operationProcessor"><inheritdoc cref="IOcrOperationProcessor" path="/summary"/></param>
    /// <param name="backgroundServiceQueue"><inheritdoc cref="IBackgroundServiceQueue" path="/summary"/></param>
    public sealed class OcrOperationStoreExtension(
        IOcrAsyncService ocrService,
        IOperationRepository operationRepository,
        IOcrOperationProcessor operationProcessor,
        IBackgroundServiceQueue backgroundServiceQueue)
        : CardStoreExtension
    {
        #region Private Fields

        private readonly IOcrAsyncService ocrService = NotNullOrThrow(ocrService);
        private readonly IOperationRepository operationRepository = NotNullOrThrow(operationRepository);
        private readonly IOcrOperationProcessor operationProcessor = NotNullOrThrow(operationProcessor);
        private readonly IBackgroundServiceQueue backgroundServiceQueue = NotNullOrThrow(backgroundServiceQueue);

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Base overrides

        /// <inheritdoc/>
        public override async Task BeforeCommitTransaction(ICardStoreExtensionContext context)
        {
            var card = context.Request.Card;
            var cancellationToken = context.CancellationToken;
            var validationResult = context.ValidationResult;

            if (!context.ValidationResult.IsSuccessful() ||
                !card.Sections.TryGetValue(nameof(OcrRequests), out var requests))
            {
                return;
            }

            // Сначала выполняется попытка отменить операции по прерванным запросам
            foreach (var request in GetInterruptedRequests(requests))
            {
                await this.ocrService.CancelOperationAsync(request.RowID, validationResult, token: null, cancellationToken);
                if (!validationResult.IsSuccessful())
                {
                    return;
                }
            }

            // Затем выполняется поиск созданных запросов, по которым необходимо запустить операции
            var createdRequests = GetCreatedRequests(requests);
            if (createdRequests.Length == 0)
            {
                return;
            }

            var parameter = DataParameter.Guid(nameof(card.ID), card.ID);

            // В БД выполняется проверка на наличие дублирующих запросов с аналогичными параметрами для распознавания
            if (await IsSameRequestAlreadyExistsAsync(context.DbScope!, parameter, cancellationToken))
            {
                validationResult.AddError(this, "$TextRecognition_ValidationMessage_RequestAlreadyExists");
                return;
            }

            // Выполняется попытка получения объекта-запроса на получение контента файла
            var fileContentRequest = await TryGetFileContentRequestAsync(context.DbScope!, parameter, cancellationToken);
            if (fileContentRequest is null)
            {
                validationResult.AddError(
                    this,
                    "$TextRecognition_ValidationMessage_OperationInfoNotExists",
                    card.ID.ToString("B"));

                return;
            }

            // Так как карточка операции OCR может быть не связана с исходной карточкой документа, то в запросе может быть пустой CardID.
            // В таком случае контент распознаваемого файла располагается в самой карточки операции OCR, поэтому устанавливается её параметры.
            if (fileContentRequest.CardID is null)
            {
                fileContentRequest.CardID = card.ID;
                fileContentRequest.CardTypeID = card.TypeID;
                fileContentRequest.CardTypeName = card.TypeName;
            }

            // Если в запросе на получение контента распознаваемого файла отсутствует идентификатор файла и карточка операции OCR сохраняется впервые,
            // то в таком случае ожидается, что файл для распознавания будет приложен в карточку операции OCR. При таком сценарии информация о распознаваемом
            // файле может быть заполнена только после того как эта информация запишется в таблицы Files и FileVerions (из за ограничений FK в БД).
            if (fileContentRequest.FileID is null && card.StoreMode == CardStoreMode.Insert)
            {
                // Ищем исходный файл, распознавание которого требуется выполнить
                var cardFile = card.Files.FirstOrDefault(static file =>
                    file is { IsVirtual: false, State: CardFileState.Inserted }
                    && OcrCommon.SupportedFileExtensions.Contains(FileHelper.GetExtension(file.Name)));

                if (cardFile is null)
                {
                    context.ValidationResult.AddError(this, "File for OCR operation was not found at card.");
                    return;
                }

                await UpdateFileContentRequestAsync(context.DbScope!, parameter, cardFile, fileContentRequest, cancellationToken);
            }

            // Создание операций на распознавание файла по каждому запросу
            List<int>? languages = null;
            var ocrOperations = new List<Guid>(createdRequests.Length);

            foreach (var request in createdRequests)
            {
                var ocrRequest = CreateOcrAsyncRequest(card, request, fileContentRequest, ref languages);
                var (operationID, _) = await this.ocrService.CreateOperationAsync(ocrRequest, validationResult, token: null, cancellationToken);
                if (!validationResult.IsSuccessful() || !operationID.HasValue)
                {
                    if (!operationID.HasValue)
                    {
                        validationResult.AddError(this, "$TextRecognition_ValidationMessage_CreateOperationError", request.RowID.ToString("B"));
                    }

                    return;
                }

                ocrOperations.Add(operationID.Value);
            }

            context.Info["OcrCreatedOperations"] = ocrOperations;
        }

        /// <inheritdoc/>
        public override async Task AfterRequestFinally(ICardStoreExtensionContext context)
        {
            if (!context.RequestIsSuccessful
                || !context.ValidationResult.IsSuccessful()
                || context.Info.TryGet<IList>("OcrCreatedOperations") is not { Count: > 0 } operations)
            {
                return;
            }

            foreach (Guid operationID in operations)
            {
                IOperation? operation = await this.operationRepository.TryGetAsync(operationID, loadEverything: true, CancellationToken.None);
                if (operation is null)
                {
                    context.ValidationResult.AddError(this, "$TextRecognition_ValidationMessage_CreateOperationError", operationID.ToString("B"));
                    continue;
                }

                await this.backgroundServiceQueue.EnqueueAsync(
                    async ct =>
                    {
                        try
                        {
                            await this.operationProcessor.TryProcessOperationAsync(operation, ct);
                        }
                        catch (Exception ex)
                        {
                            logger.LogException(ex);
                        }
                    },
                    parallel: true);
            }
        }

        #endregion

        #region Private Methods

        private static CardRow[] GetInterruptedRequests(CardSection requests) =>
            requests.Rows
                .Where(static row =>
                    row.State == CardRowState.Modified &&
                    row.TryGet<int?>(OcrRequests.StateID) == (int) OcrRequestStates.Interrupted)
                .ToArray();

        private static CardRow[] GetCreatedRequests(CardSection requests) =>
            requests.Rows
                .Where(static row =>
                    row.State == CardRowState.Inserted &&
                    row.TryGet<int?>(OcrRequests.StateID) == (int) OcrRequestStates.Created)
                .ToArray();

        private static OcrAsyncRequest CreateOcrAsyncRequest(
            Card card,
            CardRow request,
            CardGetFileContentRequest fileContentRequest,
            ref List<int>? languages)
        {
            var ocrRequest = new OcrAsyncRequest(fileContentRequest)
            {
                OcrCardID = card.ID,
                Confidence = request.Get<int>(OcrRequests.Confidence),
                Preprocess = request.Get<bool>(OcrRequests.Preprocess),
                Overwrite = request.Get<bool>(OcrRequests.Overwrite),
                DetectRotation = request.Get<bool>(OcrRequests.DetectRotation),
                DetectTables = request.Get<bool>(OcrRequests.DetectTables),
                DetectBarcodes = request.Get<bool>(OcrRequests.DetectBarcodes),
                SegmentationModeID = request.Get<int>(OcrRequests.SegmentationModeID),
                Languages = request.Get<bool>(OcrRequests.DetectLanguages)
                        ? languages ??= Enum.GetValues<OcrLanguage>().Cast<int>().ToList()
                        : card.Sections.TryGet(nameof(OcrRequestsLanguages))?.Rows
                            .Where(row => row.State == CardRowState.Inserted && row.ParentRowID == request.RowID)
                            .Select(static row => row.Get<int>(OcrRequestsLanguages.LanguageID))
                            .ToList(),
                Info = { ["OperationID"] = request.RowID }
            };

            ocrRequest.SetDigest(ocrRequest.FileName);

            return ocrRequest;
        }

        private static async Task UpdateFileContentRequestAsync(
            IDbScope dbScope,
            DataParameter parameter,
            CardFile cardFile,
            CardGetFileContentRequest fileContentRequest,
            CancellationToken cancellationToken)
        {
            fileContentRequest.FileID = cardFile.RowID;
            fileContentRequest.FileName = cardFile.Name;
            fileContentRequest.FileTypeID = cardFile.TypeID;
            fileContentRequest.FileTypeName = cardFile.TypeName;
            fileContentRequest.VersionRowID = cardFile.VersionRowID;

            await using var _ = dbScope.Create();

            var query = dbScope.BuilderFactory
                .Update(nameof(OcrOperations))
                    .C(OcrOperations.FileID).Assign().V(fileContentRequest.FileID)
                    .C(OcrOperations.FileName).Assign().V(fileContentRequest.FileName)
                    .C(OcrOperations.FileTypeID).Assign().V(fileContentRequest.FileTypeID)
                    .C(OcrOperations.FileTypeName).Assign().V(fileContentRequest.FileTypeName)
                    .C(OcrOperations.VersionRowID).Assign().V(fileContentRequest.VersionRowID)
                .Where().C(OcrOperations.ID).Equals().P(parameter.Name!)
                .Build();

            await dbScope.Db
                .SetCommand(query, parameter)
                .LogCommand()
                .ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task<CardGetFileContentRequest?> TryGetFileContentRequestAsync(
            IDbScope dbScope,
            DataParameter parameter,
            CancellationToken cancellationToken)
        {
            await using var _ = dbScope.Create();

            var query = dbScope.BuilderFactory
                .Select()
                    .C(OcrOperations.CardID).As(nameof(CardGetFileContentRequest.CardID))
                    .C(OcrOperations.CardTypeID).As(nameof(CardGetFileContentRequest.CardTypeID))
                    .C(OcrOperations.CardTypeName).As(nameof(CardGetFileContentRequest.CardTypeName))
                    .C(OcrOperations.FileID).As(nameof(CardGetFileContentRequest.FileID))
                    .C(OcrOperations.FileName).As(nameof(CardGetFileContentRequest.FileName))
                    .C(OcrOperations.FileTypeID).As(nameof(CardGetFileContentRequest.FileTypeID))
                    .C(OcrOperations.FileTypeName).As(nameof(CardGetFileContentRequest.FileTypeName))
                    .C(OcrOperations.VersionRowID).As(nameof(CardGetFileContentRequest.VersionRowID))
                .From(nameof(OcrOperations)).NoLock()
                .Where().C(OcrOperations.ID).Equals().P(parameter.Name!)
                .Build();

            return await dbScope.Db
                .SetCommand(query, parameter)
                .LogCommand()
                .ExecuteAsync<CardGetFileContentRequest>(cancellationToken);
        }

        private static async Task<bool> IsSameRequestAlreadyExistsAsync(
            IDbScope dbScope,
            DataParameter parameter,
            CancellationToken cancellationToken)
        {
            await using var _ = dbScope.Create();

            var query = dbScope.BuilderFactory
                .SelectExists(a => a
                    .Select().V(null)
                    .From(nameof(OcrRequests), "r").NoLock()
                    .LeftJoinLateral(b => b
                        .Select().E(c => c
                            .Select()
                            .If(Dbms.SqlServer, d => d
                                .CastAs(e => e.C("l", OcrRequestsLanguages.LanguageID), SchemeDbType.String)
                                .Add().V("."))
                            .ElseIf(Dbms.PostgreSql, d => d
                                .Q(" string_agg(")
                                .CastAs(e => e.C("l", OcrRequestsLanguages.LanguageID), SchemeDbType.String)
                                .Q(", '.' ")
                                .OrderBy("l", OcrRequestsLanguages.LanguageID)
                                .Q(")"))
                            .ElseThrow()
                            .From(nameof(OcrRequestsLanguages), "l").NoLock()
                            .Where().C("l", OcrRequestsLanguages.ParentRowID).Equals().C("r", OcrRequests.RowID)
                            .If(Dbms.SqlServer, d => d
                                .OrderBy("l", OcrRequestsLanguages.LanguageID)
                                .Q(" FOR XML PATH('')"))
                            .EndIf())
                        .As("LanguagesHash"),
                        "l")
                    .Where().C("r", OcrRequests.ID).Equals().P(parameter.Name!)
                        .And().C("r", OcrRequests.StateID).NotEquals().V((int) OcrRequestStates.Interrupted)
                    .GroupBy()
                        .C("r", OcrRequests.SegmentationModeID)
                        .C("r", OcrRequests.Confidence)
                        .C("r", OcrRequests.Preprocess)
                        .C("r", OcrRequests.DetectLanguages)
                        .C("r", OcrRequests.DetectRotation)
                        .C("r", OcrRequests.DetectTables)
                        .C("r", OcrRequests.DetectBarcodes)
                        .C("r", OcrRequests.Overwrite)
                        .C("l", "LanguagesHash")
                    .Having(b => b.Count().Greater().V(1)))
                .Build();

            return await dbScope.Db
                .SetCommand(query, parameter)
                .LogCommand()
                .ExecuteAsync<bool>(cancellationToken);
        }

        #endregion
    }
}
