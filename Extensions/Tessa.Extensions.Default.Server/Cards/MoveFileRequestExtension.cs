#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Server.Cards
{
    /// <summary>
    /// Запрос на перенос контента файла на файловую систему.
    /// </summary>
    public sealed class MoveFileRequestExtension :
        CardRequestExtension
    {
        #region Fields

        private readonly ICardContentStrategy contentStrategy;
        private readonly ITransactionStrategy transactionStrategy;
        private readonly ICardFileVersionStrategy versionStrategy;
        private readonly ICardFileVersionStrategy deletedVersionStrategy;
        private readonly IErrorManager errorManager;

        #endregion

        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="MoveFileRequestExtension"/>.
        /// </summary>
        /// <param name="contentStrategy"><inheritdoc cref="ICardContentStrategy" path="/summary"/></param>
        /// <param name="transactionStrategy"><inheritdoc cref="ITransactionStrategy" path="/summary"/></param>
        /// <param name="versionStrategy"><inheritdoc cref="ICardFileVersionStrategy" path="/summary"/></param>
        /// <param name="deletedVersionStrategy"><inheritdoc cref="ICardFileVersionStrategy" path="/summary"/></param>
        /// <param name="errorManager"><inheritdoc cref="IErrorManager" path="/summary"/></param>
        public MoveFileRequestExtension(
            ICardContentStrategy contentStrategy,
            ITransactionStrategy transactionStrategy,
            ICardFileVersionStrategy versionStrategy,
            [Dependency(CardFileVersionStrategyNames.Backup)]
            ICardFileVersionStrategy deletedVersionStrategy,
            IErrorManager errorManager)
        {
            this.contentStrategy = NotNullOrThrow(contentStrategy);
            this.transactionStrategy = NotNullOrThrow(transactionStrategy);
            this.versionStrategy = NotNullOrThrow(versionStrategy);
            this.deletedVersionStrategy = NotNullOrThrow(deletedVersionStrategy);
            this.errorManager = NotNullOrThrow(errorManager);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task AfterRequest(ICardRequestExtensionContext context)
        {
            if (!context.RequestIsSuccessful || !context.ValidationResult.IsSuccessful())
            {
                return;
            }

            if (!context.Session.User.IsAdministrator())
            {
                ValidationSequence
                     .Begin(context.ValidationResult)
                     .SetObjectName(this)
                     .Error(ValidationKeys.UserIsNotAdmin)
                     .End();

                return;
            }

            if (context.Request.CardID is not { } requestCardID)
            {
                ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .Error(CardValidationKeys.UnspecifiedCardID)
                    .End();

                return;
            }

            if (context.Request.Info.TryGet<int?>(DefaultExtensionHelper.SourceIDKey) is not { } sourceID)
            {
                context.ValidationResult.AddError(this,
                    $"Parameter request.Info[\"{DefaultExtensionHelper.SourceIDKey}\"] is not specified."
                    + " Please, use method DefaultExtensionHelper.SetSourceID(...)");

                return;
            }

            CardFileSourceType targetSourceType = new(sourceID);
            Guid[]? requestFileIDs = context.Request.FileID is { } fileID ? new[] { fileID } : null;

            // Получение контекстов для существующих файлов в карточке
            var contentContexts = await CardComponentHelper.GetContentContextsAsync(
                    requestCardID,
                    context.ValidationResult,
                    this.versionStrategy,
                    requestFileIDs,
                    context.CancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            List<CardContentContext> contentsToMove = contentContexts
                .Where(c => c.Source != targetSourceType)
                .ToList();

            // Получение контекстов для удалённых файлов в карточке
            List<CardContentContext> deletedContentContexts = await CardComponentHelper.GetContentContextsAsync(
                    requestCardID,
                    context.ValidationResult,
                    this.deletedVersionStrategy,
                    requestFileIDs,
                    context.CancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            List<CardContentContext> deletedContentsToMove = deletedContentContexts
                .Where(c => c.Source != targetSourceType)
                .ToList();

            // Выход, если ни одного контента не нашлось
            if (!contentsToMove.Any() && !deletedContentsToMove.Any())
            {
                return;
            }

            // Группировка контентов по таблицам, к которым они относятся
            Dictionary<string, IEnumerable<CardContentContext>> groupedContentsToMove = new(2)
            {
                ["FileVersions"] = contentsToMove,
                ["DeletedFileVersions"] = deletedContentContexts
            };

            await using IDbScopeInstance _ = context.DbScope!.Create();
            DbManager db = context.DbScope!.Db;
            IQueryBuilderFactory builderFactory = context.DbScope!.BuilderFactory;

            // Транзакция используется только в том случае, если она отсутствовала на момент запуска
            bool useTransaction = db.DataConnection.Transaction is null;

            if (useTransaction)
            {
                await db.ExecuteSetXactAbortOnAsync(context.CancellationToken);
            }

            foreach ((string tableName, IEnumerable<CardContentContext> contents) in groupedContentsToMove)
            {
                foreach (var content in contents)
                {
                    CardContentContext newContent = new(
                        content.CardID,
                        content.FileID,
                        content.VersionRowID,
                        targetSourceType,
                        content.ValidationResult);

                    try
                    {
                        await using Stream contentStream = await this.contentStrategy.GetAsync(content, context.CancellationToken);
                        await this.contentStrategy.StoreAsync(newContent, contentStream, context.CancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        newContent.ValidationResult.AddException(
                            this,
                            e,
                            message: await LocalizeFormatAsync("$Cards_ErrorGettingFileContentWithID", content.FileID));

                        await this.errorManager.ReportErrorSafeAsync(
                            context.Request.CardTypeID ?? Guid.Empty,
                            context.Request.CardID ?? Guid.Empty,
                            context.Request.TryGetDigest(),
                            new ErrorDescription(context.ValidationResult.Build()));
                        
                        return;
                    }

                    async Task ExecuteAsync()
                    {
                        try
                        {
                            await this.contentStrategy.DeleteAsync(content);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception e)
                        {
                            newContent.ValidationResult.AddException(
                                this,
                                e,
                                message: await LocalizeFormatAsync("$Cards_ErrorGettingFileContentWithID", content.FileID));

                            await this.errorManager.ReportErrorSafeAsync(
                                context.Request.CardTypeID ?? Guid.Empty,
                                context.Request.CardID ?? Guid.Empty,
                                context.Request.CardTypeName,
                                new ErrorDescription(context.ValidationResult.Build()));

                            return;
                        }

                        await db
                            .SetCommand(
                                builderFactory
                                    .Update(tableName).C("SourceID").Assign().P("SourceID")
                                    .Where().C("RowID").Equals().P("RowID")
                                    .Build(),
                                db.Parameter("RowID", content.VersionRowID, DataType.Guid),
                                db.Parameter("SourceID", (short) targetSourceType, DataType.Int16))
                            .LogCommand()
                            .ExecuteNonQueryAsync();
                    }

                    if (useTransaction)
                    {
                        await this.transactionStrategy.ExecuteInTransactionAsync(
                            context.ValidationResult,
                            p => ExecuteAsync(),
                            context.CancellationToken);
                    }
                    else
                    {
                        await ExecuteAsync();
                    }
                }
            }
        }

        #endregion
    }
}
