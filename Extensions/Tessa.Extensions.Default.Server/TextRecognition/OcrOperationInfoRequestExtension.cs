#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using Tessa.Cards.Extensions;
using Tessa.Platform.Data;
using Tessa.Scheme;
using Tessa.TextRecognition.Constants;

namespace Tessa.Extensions.Default.Server.TextRecognition
{
    /// <summary>
    /// Запрос на получение информации об операции OCR для заданного файла.
    /// Результат выполнения запроса может содержать одно из следующих значений:
    /// <para>- идентификатор карточки операции OCR, если она существовала для распознаваемого файла;</para>
    /// <para>- идентификатор основной карточки, в которой находится распознаваемый файл, если не был найден идентификатор карточки операции OCR;</para>
    /// <para>- пустой объект, если для распознаваемого файла не был найден идентификатор карточки операции OCR или идентификатор основной карточки.</para>
    /// </summary>
    public sealed class OcrOperationInfoRequestExtension :
        CardRequestExtension
    {
        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterRequest(ICardRequestExtensionContext context)
        {
            if (context.RequestIsSuccessful
                && context.ValidationResult.IsSuccessful()
                && context.Response is not null
                && context.DbScope is { } dbScope
                && context.Request.FileID is { } fileID)
            {
                var parameter = DataParameter.Guid(nameof(fileID), fileID);

                if (await GetOcrCardIdAsync(dbScope, parameter, context.CancellationToken) is { } ocrCardID)
                {
                    context.Response.Info[OcrOperations.ID] = ocrCardID;
                }
                else if (await GetCardIdAsync(dbScope, parameter, context.CancellationToken) is { } mainCardID)
                {
                    context.Response.Info[OcrOperations.CardID] = mainCardID;
                }
            }
        }

        #endregion

        #region Private Methods

        private static async Task<Guid?> GetOcrCardIdAsync(
            IDbScope dbScope,
            DataParameter parameter,
            CancellationToken cancellationToken)
        {
            await using var _ = dbScope.Create();

            var query = dbScope.BuilderFactory
                .Select().C(OcrOperations.ID)
                .From(nameof(OcrOperations)).NoLock()
                .Where().C(OcrOperations.FileID).Equals().P(parameter.Name!)
                .Build();

            return await dbScope.Db
                .SetCommand(query, parameter)
                .LogCommand()
                .ExecuteAsync<Guid?>(cancellationToken);
        }

        private static async Task<Guid?> GetCardIdAsync(
            IDbScope dbScope,
            DataParameter parameter,
            CancellationToken cancellationToken)
        {
            await using var _ = dbScope.Create();

            var query = dbScope.BuilderFactory
                .Select().C(Names.Table_ID)
                .From(Names.Files).NoLock()
                .Where().C(Names.Table_RowID).Equals().P(parameter.Name!)
                .Build();

            return await dbScope.Db
                .SetCommand(query, parameter)
                .LogCommand()
                .ExecuteAsync<Guid?>(cancellationToken);
        }

        #endregion
    }
}
