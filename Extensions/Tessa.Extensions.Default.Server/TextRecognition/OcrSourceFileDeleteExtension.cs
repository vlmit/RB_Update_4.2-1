#nullable enable

using LinqToDB.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.TextRecognition.Constants;
using Unity;

namespace Tessa.Extensions.Default.Server.TextRecognition
{
    /// <summary>
    /// Расширение для удаления карточки операции OCR при удалении файла исходной карточки.
    /// </summary>
    /// <remarks>
    /// Создает экземпляр класса <see cref="OcrSourceFileDeleteExtension"/>.
    /// </remarks>
    /// <param name="cardRepositoryExtended"><inheritdoc cref="ICardRepository" path="/summary"/></param>
    public sealed class OcrSourceFileDeleteExtension(
        [Dependency(CardRepositoryNames.Extended)]
        ICardRepository cardRepositoryExtended)
        : CardStoreExtension
    {
        #region Private Fields

        public readonly ICardRepository cardRepositoryExtended = NotNullOrThrow(cardRepositoryExtended);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterBeginTransaction(ICardStoreExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful()
                || context.Request.Card.Files is { Count: 0 }
                || context.DbScope is null)
            {
                return;
            }

            List<OcrOperationInfo>? ocrOperationsInfo = null;

            var deletedFileIdentifier = context.Request.Card.Files
                .Where(static f => f.State == CardFileState.Deleted)
                .Select(static f => f.RowID)
                .ToArray();

            if (deletedFileIdentifier is { Length: > 0 })
            {
                // Получение из БД информации по карточкам операций OCR для измененных файлов
                await using (context.DbScope.Create())
                {
                    var parameter = DataParameter.Guid(
                        OcrOperations.CardID,
                        context.Request.Card.ID);

                    var query = context.DbScope.BuilderFactory
                        .Select()
                            .C(OcrOperations.ID)
                            .C(OcrOperations.FileID)
                        .From(nameof(OcrOperations)).NoLock()
                        .Where().C(OcrOperations.CardID).Equals().P(parameter.Name!)
                            .And().C(OcrOperations.FileID).In(deletedFileIdentifier)
                        .Build();

                    ocrOperationsInfo = await context.DbScope.Db
                        .SetCommand(query, parameter)
                        .LogCommand()
                        .ExecuteListAsync<OcrOperationInfo>(context.CancellationToken);
                }
            }

            // Удаление карточек операций OCR посредством репозитория. Если при удалении одной из карточек
            // операций OCR произошла ошибка, то процесс будет прерван. Если на данный момент была активна
            // операция распознавания, то она будет отменена в расширении на удаление карточки операции OCR.
            foreach (var file in context.Request.Card.Files)
            {
                var ocrCardID = file.State == CardFileState.Deleted
                    ? ocrOperationsInfo?.FirstOrDefault(o => o.FileID == file.RowID)?.ID
                    : file.State != CardFileState.None
                        ? file.Info.TryGet<Guid?>(OcrCommon.OcrDeleteOperationIdKey)
                        : null;

                if (ocrCardID.HasValue)
                {
                    var deleteRequest = new CardDeleteRequest
                    {
                        CardID = ocrCardID.Value,
                        CardTypeID = OcrCardTypes.OcrOperationTypeID,
                        CardTypeName = OcrCardTypes.OcrOperationTypeName
                    };

                    // Добавление признака, что удаление инициируется в ходе обработки исходной карточки
                    deleteRequest.Info[OcrCommon.OcrSourceDeleteProcessingKey] = BooleanBoxes.True;

                    var deleteResponse = await this.cardRepositoryExtended.DeleteAsync(
                        deleteRequest,
                        context.CancellationToken);

                    var deleteResult = deleteResponse.ValidationResult.Build();
                    context.ValidationResult.Add(deleteResult);
                    if (!deleteResult.IsSuccessful)
                    {
                        return;
                    }
                }
            }
        }

        #endregion

        #region Nested Types

        private class OcrOperationInfo
        {
            public Guid ID { get; set; }
            public Guid FileID { get; set; }
        }

        #endregion
    }
}
