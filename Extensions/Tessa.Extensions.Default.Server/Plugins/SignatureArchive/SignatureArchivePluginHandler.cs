#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Server.EDS.SignatureArchive;
using Tessa.Platform;
using Tessa.Platform.EDS;
using Tessa.Platform.Plugins;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Views;
using Tessa.Views.Metadata;

namespace Tessa.Extensions.Default.Server.Plugins.SignatureArchive
{
    /// <summary>
    /// Обработчик плагина <see cref="DefaultPluginNames.SignatureArchivePlugin"/>.
    /// </summary>
    /// <remarks>
    /// Создаёт экземпляр класса с указанием его зависимостей.
    /// </remarks>
    /// <param name="viewService"><inheritdoc cref="IViewService" path="/summary"/></param>
    /// <param name="viewSpecialParameters"><inheritdoc cref="IViewSpecialParameters" path="/summary"/></param>
    /// <param name="errorManager"><inheritdoc cref="IErrorManager" path="/summary"/></param>
    /// <param name="signatureArchiveManager"><inheritdoc cref="ISignatureArchiveManager" path="/summary"/></param>
    /// <param name="cardCache"><inheritdoc cref="ICardCache" path="/summary"/></param>
    public sealed class SignatureArchivePluginHandler(
        IViewService viewService,
        IViewSpecialParameters viewSpecialParameters,
        IErrorManager errorManager,
        ISignatureArchiveManager signatureArchiveManager,
        ICardCache cardCache) 
        : IPluginHandler
    {
        #region Constants

        private const string viewName = "SignatureArchive";

        #endregion

        #region Fields

        /// <inheritdoc cref="IViewService" path="/summary"/>
        private readonly IViewService viewService = NotNullOrThrow(viewService);

        /// <inheritdoc cref="IViewSpecialParameters" path="/summary"/>
        private readonly IViewSpecialParameters viewSpecialParameters = NotNullOrThrow(viewSpecialParameters);

        /// <inheritdoc cref="IErrorManager" path="/summary"/>
        private readonly IErrorManager errorManager = NotNullOrThrow(errorManager);

        /// <inheritdoc cref="ISignatureArchiveManager" path="/summary"/>
        private readonly ISignatureArchiveManager signatureArchiveManager = NotNullOrThrow(signatureArchiveManager);

        /// <inheritdoc cref="ICardCache" path="/summary"/>
        private readonly ICardCache cardCache = NotNullOrThrow(cardCache);

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region IPluginHandler Implementation

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(IPluginExecutingContext context)
        {
            ThrowIfNull(context);

            logger.Trace("Starting signature archive plugin.");

            var settingsGetCardResponse = await this.cardCache.Cards.GetAsync(SignatureHelper.SignatureSettingsType, context.CancellationToken);

            if (!settingsGetCardResponse.ValidationResult.IsSuccessful)
            {
                logger.LogResult(settingsGetCardResponse.ValidationResult);
                return;
            }

            var settingsCard = settingsGetCardResponse.GetValue();

            if (!settingsCard.Sections.TryGetValue(SignatureHelper.SignatureSettingsSectionName, out var signatureSettingsSection)
                || string.IsNullOrEmpty(signatureSettingsSection.RawFields.TryGet<string?>(SignatureHelper.SignatureSettingsTspAddress)))
            {
                logger.Warn("No TSP address provided in settings, can not archive signatures.");
                return;
            }

            try
            {
                var view = await this.viewService.GetByNameAsync(viewName, context.CancellationToken)
                    ?? throw new InvalidOperationException($"Can not find view with alias \"{viewName}\" at view service.");

                var (viewMetadata, validationResult) = await view.TryGetMetadataAsync(context.CancellationToken);
                if (viewMetadata is null || !validationResult.IsSuccessful)
                {
                    throw new ValidationException(validationResult, new InvalidOperationException($"Can not find metadata for view \"{viewName}\"."));
                }

                var viewRequest = new TessaViewRequest(viewName);
                viewRequest.SortingColumns.Add(new SortingColumn { Alias = "CardID" });

                var currentPage = 1;
                var pageLimit = viewMetadata.ExportDataPageLimit;
                if (pageLimit <= 0)
                {
                    pageLimit = ViewMetadata.DefaultExportDataPageLimit;
                }

                this.viewSpecialParameters.ProvidePageLimitParameter(viewRequest.Parameters, pageLimit);

                while (true)
                {
                    this.viewSpecialParameters.ProvidePageOffsetParameter(viewRequest.Parameters, currentPage++, pageLimit);

                    var viewResult = await view.GetDataAsync(viewRequest, context.CancellationToken);
                    if (viewResult.Rows.Count == 0)
                    {
                        break;
                    }

                    var cardIdColumnIndex = viewResult.GetColumnIndex("CardID");
                    var fileIdColumnIndex = viewResult.GetColumnIndex("FileID");

                    var signingInfo = new SignaturesArchiveRequestInfo()
                    { 
                        CardID = (Guid) viewResult.Rows[0][cardIdColumnIndex]!,
                        FileIDs = [(Guid) viewResult.Rows[0][fileIdColumnIndex]!]
                    };

                    for (var i = 1; i < viewResult.Rows.Count; i++) 
                    {
                        var cardID = (Guid) viewResult.Rows[i][cardIdColumnIndex]!;

                        if (signingInfo.CardID != cardID)
                        {
                            await this.ProcessBatchAsync(signingInfo, context.CancellationToken);
                            signingInfo = new()
                            {
                                CardID = cardID
                            };
                        }

                        signingInfo.FileIDs.Add((Guid) viewResult.Rows[i][fileIdColumnIndex]!);
                    }

                    await this.ProcessBatchAsync(signingInfo, context.CancellationToken);

                    if (viewResult.Rows.Count < pageLimit)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) 
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogException(ex);

                await this.errorManager.ReportErrorSafeAsync(
                    Guid.Empty,
                    Guid.Empty,
                    "Signature archiving failure",
                    new ErrorDescription(ValidationResult.FromException(ex), ErrorCategories.SignatureArchiveFailed)
                );
            }

            logger.Trace("Archiving signatures is successfully completed.");
        }

        /// <inheritdoc/>
        public IPluginSettings ResolveSettings(Dictionary<string, object?>? info = null)
        {
            var settings = new PluginSettings(DefaultPluginNames.SignatureArchivePlugin);
            if (info is not null)
            {
                settings.Deserialize(info);
            }

            return settings;
        }

        #endregion

        #region Private Methods

        private async Task ProcessBatchAsync(
            SignaturesArchiveRequestInfo signingInfo,
            CancellationToken cancellationToken)
        {
            logger.Trace($"Archiving signature(s) for card '{signingInfo.CardID}'");

            var result = await this.signatureArchiveManager.ArchiveSignaturesAsync(signingInfo.CardID, signingInfo.FileIDs, cancellationToken);

            if (!result.IsSuccessful)
            {
                throw new ValidationException(result);
            }
        }

        #endregion
    }
}
