#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.TextRecognition.Constants;
using Unity;

namespace Tessa.Extensions.Default.Server.TextRecognition
{
    /// <summary>
    /// Расширение на сохранение исходной карточки, содержащей файл, распознавание которого необходимо выполнить.
    /// </summary>
    /// <remarks>
    /// Создает экземпляр класса <see cref="OcrSourceCardStoreExtension"/>.
    /// </remarks>
    /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
    public sealed class OcrSourceCardStoreExtension(
        [Dependency(CardRepositoryNames.ExtendedWithoutTransactionAndLocking)]
        ICardRepository cardRepository)
        : CardStoreExtension
    {
        #region Fields

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override Task BeforeRequest(ICardStoreExtensionContext context)
        {
            if (context.ValidationResult.IsSuccessful()
                && !context.Request.DoesNotAffectVersion
                && context.Request.TryGetInfo()?.ContainsKey(OcrCommon.OcrKey) is true)
            {
                // Если создается OCR запрос, то в любом случае увеличиваем версию карточки с исходным файлом.
                context.Request.AffectVersion = true;
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public override async Task BeforeCommitTransaction(ICardStoreExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful()
                || !context.Request.AffectVersion
                || context.Request.DoesNotAffectVersion
                || context.Request.TryGetInfo()?.TryGetValue(OcrCommon.OcrKey, out var ocrInfo) is not true
                || ocrInfo is not Dictionary<string, object?> ocrStorage
                || ocrStorage.TryGet<Dictionary<string, object?>>(nameof(OcrRequests)) is not { } request
                || ocrStorage.TryGet<List<object>>(nameof(OcrRequestsLanguages)) is not { } requestLanguages)
            {
                return;
            }

            // Ищем исходный файл, распознавание которого требуется выполнить
            var cardFile = context.Request.Card.Files.FirstOrDefault(static file => file.Info.ContainsKey(OcrCommon.OcrKey));
            if (cardFile is null)
            {
                context.ValidationResult.AddError(this, "File for OCR operation was not found at card.");
                return;
            }

            // Получаем опции OCR, которые были установлены в опциях файла
            var ocrOptions = cardFile.DeserializeOptions().Get<Dictionary<string, object?>>(OcrCommon.OcrKey);
            var ocrCardId = ocrOptions?.Get<Guid>("CardID");
            if (!ocrCardId.HasValue)
            {
                context.ValidationResult.AddError(this, "OCR operation card identifier is empty at file options.");
                return;
            }

            // Пытаемся создать и сохранить карточку OCR в системе.
            // В расширении на сохранение будет создана операция распознавания.
            var ocrCardCreationResult = await OcrExtensionHelper.CreateOcrOperationCardAsync(
                this.cardRepository,
                context.Session.User,
                cardFile,
                context.Request.Card,
                ocrCardId.Value,
                request,
                requestLanguages,
                createWithCompletedRequest: false,
                context.CancellationToken);

            context.ValidationResult.Add(ocrCardCreationResult);
        }

        #endregion
    }
}
