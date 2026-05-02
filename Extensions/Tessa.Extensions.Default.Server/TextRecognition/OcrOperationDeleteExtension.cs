#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Tessa.Cards.Extensions;
using Tessa.Platform.Data;
using Tessa.Platform.Operations;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.TextRecognition;
using Tessa.TextRecognition.Constants;
using Tessa.TextRecognition.Enums;

namespace Tessa.Extensions.Default.Server.TextRecognition
{
    /// <summary>
    /// Расширение, которое выполняет:
    /// - проверку наличия файла в системе для удаляемой карточки операции OCR, если выполняется независимое удаление карточки
    /// - отмену и удаление (при успешной отмене) операций OCR при удалении карточки операции OCR.
    /// </summary>
    /// <remarks>
    /// Создает экземпляр класса <see cref="OcrOperationDeleteExtension"/>.
    /// </remarks>
    /// <param name="ocrAsyncService"><inheritdoc cref="IOcrAsyncService" path="/summary"/></param>
    /// <param name="operationRepository"><inheritdoc cref="IOperationRepository" path="/summary"/></param>
    public sealed class OcrOperationDeleteExtension(
        IOcrAsyncService ocrAsyncService,
        IOperationRepository operationRepository)
        : CardDeleteExtension
    {
        #region Private Fields

        private readonly IOcrAsyncService ocrAsyncService = NotNullOrThrow(ocrAsyncService);
        private readonly IOperationRepository operationRepository = NotNullOrThrow(operationRepository);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task BeforeRequestWhenTypeResolved(ICardDeleteExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful()
                || context.Request.CardID is not { } cardID
                || context.DbScope is not { } dbScope
                || context.Request.Info.ContainsKey(OcrCommon.OcrSourceDeleteProcessingKey))
            {
                return;
            }

            await using var _ = dbScope.Create();

            var parameter = DataParameter.Guid(OcrOperations.ID, cardID);

            var query = dbScope.BuilderFactory
                .Select().Top(1).V(true)
                .From(nameof(OcrOperations), "o").NoLock()
                .InnerJoin("Files", "f").NoLock()
                    .On().C("f", "RowID").Equals().C("o", OcrOperations.FileID)
                .Where().C("o", OcrOperations.ID).Equals().P(parameter.Name!)
                    .And().C("o", OcrOperations.CardID).IsNotNull()
                .Limit(1)
                .Build();

            var hasSourceFile = await dbScope.Db
                .SetCommand(query, parameter)
                .LogCommand()
                .ExecuteAsync<bool>(context.CancellationToken);

            if (hasSourceFile)
            {
                context.ValidationResult.AddError(this, "$TextRecognition_ValidationMessage_SourceFileConstraintViolation");
            }
        }

        /// <inheritdoc/>
        public override async Task AfterBeginTransaction(ICardDeleteExtensionContext context)
        {
            if (!context.ValidationResult.IsSuccessful()
                || context.Request.CardID is not { } cardID
                || context.DbScope is not { } dbScope)
            {
                return;
            }

            // Удаление было инициировано в ходе обработки исходной карточки при файла или самой карточки
            if (context.Request.Info.ContainsKey(OcrCommon.OcrSourceDeleteProcessingKey))
            {
                // Получение опций файлов и удаление признака OCR для каждого файла
                foreach (var (fileID, optionsJson) in await GetFilesOptionsAsync(cardID, context.DbScope, context.CancellationToken))
                {
                    await UpdateFileOptionsAsync(fileID, optionsJson, context.DbScope, context.CancellationToken);
                }
            }

            // Для каждой созданной или активной операции выполняется ее отмена
            foreach (var operationID in await GetActualOcrOperationsAsync(cardID, dbScope, context.CancellationToken))
            {
                await this.ocrAsyncService.CancelOperationAsync(operationID, context.ValidationResult, token: null, context.CancellationToken);
                // Удаление операций выполняется плагином RemoveOperationsPlugin службы Chronos по расписанию
            }
        }

        #endregion

        #region Private Methods

        private static async Task<Dictionary<Guid, string>> GetFilesOptionsAsync(Guid cardID, IDbScope dbScope, CancellationToken cancellationToken)
        {
            await using var _ = dbScope.Create();
            var dbms = dbScope.Db.Dbms;

            var cardParameter = DataParameter.Guid(OcrOperations.ID, cardID);

            var query = dbScope.BuilderFactory
                .Select().C("f", "RowID", "Options")
                .From("Files", "f").NoLock()
                .InnerJoin(nameof(OcrOperations), "o").NoLock()
                    .On().C("o", OcrOperations.FileID).Equals().C("f", "RowID")
                .Where().C("o", "ID").Equals().P(cardParameter.Name!)
                    .And().C("f", "Options").IsNotNull()
                .Build();

            var fileVersionOptions = new Dictionary<Guid, string>();
            await using (var reader = await dbScope.Db
                .SetCommand(query, cardParameter)
                .LogCommand()
                .ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    fileVersionOptions[reader.GetGuid(0)] = await reader.GetSequentialStringAsync(1, dbms, cancellationToken);
                }
            }

            return fileVersionOptions;
        }

        private static async ValueTask UpdateFileOptionsAsync(Guid fileID, string optionsJson, IDbScope dbScope, CancellationToken cancellationToken)
        {
            var options = StorageHelper.DeserializeFromTypedJson(optionsJson);
            if (options?.Remove(OcrCommon.OcrKey) is not true)
            {
                return;
            }

            await using var _ = dbScope.Create();

            var fileParameter = DataParameter.Guid("RowID", fileID);
            var optionsParameter = DataParameter.BinaryJson("Options", StorageHelper.SerializeToTypedJson(options));

            var query = dbScope.BuilderFactory
                .Update("Files")
                .C("Options").Assign().P(optionsParameter.Name!)
                .Where().C("RowID").Equals().P(fileParameter.Name!)
                .Build();

            await dbScope.Db
                .SetCommand(query, fileParameter, optionsParameter)
                .LogCommand()
                .ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task<List<Guid>> GetActualOcrOperationsAsync(Guid cardID, IDbScope dbScope, CancellationToken cancellationToken)
        {
            await using var _ = dbScope.Create();

            var parameter = DataParameter.Guid(OcrOperations.ID, cardID);

            var query = dbScope.BuilderFactory
                .Select().C(OcrRequests.RowID)
                .From(nameof(OcrRequests)).NoLock()
                .Where().C(OcrOperations.ID).Equals().P(parameter.Name!)
                    .And().C(OcrRequests.StateID).In((int) OcrRequestStates.Created, (int) OcrRequestStates.Active)
                .Build();

            return await dbScope.Db
                .SetCommand(query, parameter)
                .LogCommand()
                .ExecuteListAsync<Guid>(cancellationToken);
        }

        #endregion
    }
}
