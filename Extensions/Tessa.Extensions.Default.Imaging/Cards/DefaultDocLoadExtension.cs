using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Tessa.Cards;
using Tessa.Files;
using Tessa.Imaging.DocLoad;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Imaging.Cards
{
    /// <summary>
    /// Расширение для модуля потокового ввода, используемое платформой по умолчанию.
    /// </summary>
    public class DefaultDocLoadExtension(
        ISession session,
        IUnityContainer container,
        ICardFileManager cardFileManager,
        ICardRepository cardRepository,
        IPlaceholderManager placeholderManager,
        ICardServerPermissionsProvider permissionsProvider)
        : DocLoadExtension
    {
        #region Fields

        private readonly ISession session = NotNullOrThrow(session);

        private readonly IUnityContainer container = NotNullOrThrow(container);

        private readonly ICardFileManager cardFileManager = NotNullOrThrow(cardFileManager);

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        private readonly IPlaceholderManager placeholderManager = NotNullOrThrow(placeholderManager);

        private readonly ICardServerPermissionsProvider permissionsProvider = NotNullOrThrow(permissionsProvider);

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async ValueTask RequestAsync(IDocLoadExtensionContext context)
        {
            ThrowIfNull(context.Settings);
            ThrowIfNullOrWhiteSpace(context.Settings.TableName);
            ThrowIfNullOrWhiteSpace(context.Settings.FieldName);

            DbManager db = context.DbScope.Db;
            context.CardID = await db
                .SetCommand(
                    context.DbScope.BuilderFactory
                        .Select().C("ID")
                        .From(context.Settings.TableName).NoLock()
                        .Where().LowerC(context.Settings.FieldName).Equals().LowerP("Barcode")
                        .Build(),
                    db.Parameter("Barcode", context.Barcode))
                .ExecuteAsync<Guid?>(context.CancellationToken);
        }

        /// <inheritdoc/>
        public override async ValueTask SaveAsync(IDocLoadExtensionContext context)
        {
            var document = NotNullOrThrow(context.Document);

            string outputFileName = await LocalizeAsync(
                await this.GetFileNameAsync(context.DbScope, NotNullOrThrow(context.Settings), document.CardID, context.CancellationToken)) + document.GetExtension();

            await document.CloseAsync(context.CancellationToken);
            if (document.PageCount == 0)
            {
                logger.Warn($"Document with barcode {document.Barcode} doesn't have any pages. Skipped!");
                return;
            }

            var getRequest = new CardGetRequest { CardID = document.CardID, Info = new() { { CardHelper.DocLoadFlagKey, BooleanBoxes.True } } };
            this.permissionsProvider.SetFullPermissions(getRequest);

            var getResponse = await this.cardRepository.GetAsync(getRequest, context.CancellationToken);
            if (!getResponse.ValidationResult.IsSuccessful())
            {
                logger.LogResult(getResponse.ValidationResult.Build());
                return;
            }

            await using ICardFileContainer container = await this.cardFileManager
                .CreateContainerAsync(getResponse.Card, cancellationToken: context.CancellationToken);

            await container.FileContainer
                .BuildFile(outputFileName)
                .SetContentReadOnly(document.File.Path)
                .AddWithNotificationAsync(cancellationToken: context.CancellationToken);

            CardStoreResponse storeResponse = await container.StoreAsync(
                (fileContainer, storeRequest, ct) =>
                {
                    storeRequest.Info = new() { { CardHelper.DocLoadFlagKey, BooleanBoxes.True } };
                    this.permissionsProvider.SetFullPermissions(storeRequest);
                    return ValueTask.CompletedTask;
                },
                cancellationToken: context.CancellationToken);

            if (!storeResponse.ValidationResult.IsSuccessful())
            {
                logger.LogResult(storeResponse.ValidationResult.Build());
            }
        }

        /// <inheritdoc/>
        public override ValueTask<bool> IsScanFileAsync(string filePath, CancellationToken cancellationToken = default) =>
            new(Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".tiff" => true,
                ".tif" => true,
                ".png" => true,
                ".jpg" => true,
                ".jpeg" => true,
                ".pdf" => !filePath.EndsWith(".tmp.pdf", StringComparison.OrdinalIgnoreCase),
                _ => false
            });

        #endregion

        #region Private Methods

        /// <summary>
        /// Получение имени файла.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="settings">Настройки модуля потокового сканирования.</param>
        /// <param name="cardID">ID карточки.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
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
    }
}
