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
using Tessa.TextRecognition.Constants;
using Unity;

namespace Tessa.Extensions.Default.Server.TextRecognition
{
    /// <summary>
    /// Расширение для удаления карточки операции OCR при удалении исходной карточки.
    /// </summary>
    /// <remarks>
    /// Создает экземпляр класса <see cref="OcrSourceCardDeleteExtension"/>.
    /// </remarks>
    /// <param name="cardRepositoryExtended"><inheritdoc cref="ICardRepository" path="/summary"/></param>
    public sealed class OcrSourceCardDeleteExtension(
        [Dependency(CardRepositoryNames.Extended)]
        ICardRepository cardRepositoryExtended)
        : CardDeleteExtension
    {
        #region Private Fields

        public readonly ICardRepository cardRepositoryExtended = NotNullOrThrow(cardRepositoryExtended);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterBeginTransaction(ICardDeleteExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful()
                || !context.Request.CardID.HasValue
                || context.DbScope is null)
            {
                return;
            }

            List<Guid> ocrCardIdentifiers;

            // Получение из БД набора идентификаторов карточек операций OCR для удаления
            await using (context.DbScope.Create())
            {
                var parameter = DataParameter.Guid(
                    OcrOperations.CardID,
                    context.Request.CardID.Value);

                var query = context.DbScope.BuilderFactory
                    .Select().C(OcrOperations.ID)
                    .From(nameof(OcrOperations)).NoLock()
                    .Where().C(OcrOperations.CardID).Equals().P(parameter.Name!)
                    .Build();

                ocrCardIdentifiers = await context.DbScope.Db
                    .SetCommand(query, parameter)
                    .LogCommand()
                    .ExecuteListAsync<Guid>(context.CancellationToken);
            }

            // Удаление карточек операций OCR посредством репозитория. Если при удалении одной из карточек
            // операций OCR произошла ошибка, то процесс будет прерван. Если на данный момент была активна
            // операция распознавания, то она будет отменена в расширении на удаление карточки операции OCR.
            foreach (var ocrCardIdentifier in ocrCardIdentifiers)
            {
                var deleteRequest = new CardDeleteRequest
                {
                    CardID = ocrCardIdentifier,
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

        #endregion
    }
}
