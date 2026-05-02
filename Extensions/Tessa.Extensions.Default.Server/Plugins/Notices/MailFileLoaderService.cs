using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Content.Files;
using Tessa.Files;
using Tessa.Notices;
using Tessa.Platform.Data;
using Tessa.Platform.IO;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <summary>
    /// Default mail file content loader.
    /// </summary>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="configurationInfoProvider"><inheritdoc cref="IConfigurationInfoProvider" path="/summary"/></param>
    /// <param name="contentStrategy"><inheritdoc cref="ICardContentStrategy" path="/summary"/></param>
    /// <param name="versionStrategy"><inheritdoc cref="ICardFileVersionStrategy" path="/summary"/></param>
    /// <param name="clientRepository"><inheritdoc cref="ICardStreamClientRepository" path="/summary"/></param>
    /// <param name="permissionsProvider"><inheritdoc cref="ICardServerPermissionsProvider" path="/summary"/></param>
    /// <param name="errorManager"><inheritdoc cref="IErrorManager" path="/summary"/></param>
    public sealed class MailFileLoaderService(
        IDbScope dbScope,
        IConfigurationInfoProvider configurationInfoProvider,
        ICardContentStrategy contentStrategy,
        ICardFileVersionStrategy versionStrategy,
        ICardStreamClientRepository clientRepository,
        ICardServerPermissionsProvider permissionsProvider,
        IErrorManager errorManager)
        : IMailFileLoaderService
    {
        #region Fields

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);
        private readonly IConfigurationInfoProvider configurationInfoProvider = NotNullOrThrow(configurationInfoProvider);
        private readonly ICardContentStrategy contentStrategy = NotNullOrThrow(contentStrategy);
        private readonly ICardFileVersionStrategy versionStrategy = NotNullOrThrow(versionStrategy);
        private readonly ICardStreamClientRepository clientRepository = NotNullOrThrow(clientRepository);
        private readonly ICardServerPermissionsProvider permissionsProvider = NotNullOrThrow(permissionsProvider);
        private readonly IErrorManager errorManager = NotNullOrThrow(errorManager);

        #endregion

        #region IMailFileLoaderService Implementation

        /// <inheritdoc/>
        public async Task<CardGetFileContentResponse?> TryLoadContentAsync(
            Guid? cardID,
            Guid? cardTypeID,
            string? cardTypeName,
            MailFile file,
            Func<Stream, CancellationToken, ValueTask> processContentActionAsync,
            Guid? userID = null,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(file);
            ThrowIfNull(processContentActionAsync);

            var fileName = await LocalizeAsync(file.FileName);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "file";
            }

            return file.IsVirtual
                ? await this.LoadVirtualFileContentAsync(
                    cardID,
                    cardTypeID,
                    cardTypeName,
                    file,
                    fileName,
                    processContentActionAsync,
                    cancellationToken)
                : await this.LoadPhysicalFileContentAsync(
                    cardID,
                    file,
                    fileName,
                    processContentActionAsync,
                    cancellationToken);
        }

        #endregion

        #region Private Methods

        private async Task<CardGetFileContentResponse?> LoadVirtualFileContentAsync(
            Guid? cardID,
            Guid? cardTypeID,
            string? cardTypeName,
            MailFile file,
            string fileName,
            Func<Stream, CancellationToken, ValueTask> processContentActionAsync,
            CancellationToken cancellationToken = default)
        {
            var request = new CardGetFileContentRequest
            {
                CardID = cardID,
                CardTypeID = cardTypeID,
                CardTypeName = cardTypeName,
                FileID = file.FileID,
                VersionRowID = file.VersionID,
                FileName = fileName,
                FileTypeName = file.FileTypeName,
                Info = file.Info,
            };

            request.SetForbidStoringHistory(true);

            this.permissionsProvider.SetFullPermissions(request);

            return await this.clientRepository.GetFileContentAsync(
                request,
                processContentActionAsync,
                cancellationToken: cancellationToken);
        }

        private async Task<CardGetFileContentResponse?> LoadPhysicalFileContentAsync(
            Guid? cardID,
            MailFile file,
            string fileName,
            Func<Stream, CancellationToken, ValueTask> processContentActionAsync,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(cardID);

            Stream? contentStream = null;

            try
            {
                var validationResult = new ValidationResultBuilder();

                Guid? fileVersionID = file.VersionID;
                if (fileVersionID == Guid.Empty)
                {
                    fileVersionID = await FileContentHelper.TryGetFileVersionAsync(this.dbScope, file.FileID, cancellationToken);
                    if (!fileVersionID.HasValue)
                    {
                        ValidationSequence
                            .Begin(validationResult)
                            .SetObjectName(this)
                            .Error(CardValidationKeys.UnspecifiedVersionRowID)
                            .End();
                        throw new ValidationException(validationResult.Build());
                    }
                }

                // TODO: подумать над объединением с запросом выше?
                var fileVersion = await this.versionStrategy.GetVersionAsync(fileVersionID.Value, withStateAndError: true, cancellationToken: cancellationToken);
                if (fileVersion is null)
                {
                    ValidationSequence
                        .Begin(validationResult)
                        .SetObjectName(this)
                        .Error(CardValidationKeys.UnknownFileVersion, fileVersionID.Value)
                        .End();
                    throw new ValidationException(validationResult.Build());
                }

                switch (fileVersion.State)
                {
                    case CardFileVersionState.Uploading:
                        return null;
                    case CardFileVersionState.Error:
                        var errorText = await NotNullOrThrow(fileVersion.ErrorInfo).GetErrorTextAsync(cancellationToken);
                        throw new ValidationException(ValidationResult.FromText(this, errorText, ValidationResultType.Error));
                }

                var context = new CardContentContext(
                    cardID.Value,
                    file.FileID,
                    fileVersionID.Value,
                    file.Source ?? fileVersion.Source,
                    validationResult);

                var success = await CardComponentHelper.CheckCardAndFileRelationWithFileVersionAsync(
                    this.versionStrategy,
                    this.configurationInfoProvider,
                    cardID.Value,
                    file.FileID,
                    fileVersionID.Value,
                    context.ValidationResult,
                    this,
                    cancellationToken);

                if (!success)
                {
                    throw new ValidationException(context.ValidationResult.Build());
                }

                try
                {
                    contentStream = await this.contentStrategy.GetAsync(context, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    context.ValidationResult.AddException(
                        this,
                        ex,
                        message: await LocalizeFormatAsync("$Cards_ErrorGettingFileContentWithID", file.FileID));

                    await this.errorManager.ReportErrorSafeAsync(
                        Guid.Empty,
                        cardID.Value,
                        null,
                        new ErrorDescription(context.ValidationResult.Build()));

                    return new CardGetFileContentResponse
                    {
                        HasContent = false,
                        Size = 0L,
                    };
                }

                if (!context.ValidationResult.IsSuccessful())
                {
                    throw new ValidationException(context.ValidationResult.Build());
                }

                var modifiedByName = (FileHelper.RemoveInvalidFileNameChars(file.ModifiedByName) ?? string.Empty).Trim();
                var validFileName = FileHelper.RemoveInvalidFileNameChars(fileName, FileHelper.InvalidCharReplacement);

                var actualFileName = string.IsNullOrEmpty(modifiedByName)
                    ? validFileName
                    : $"{Path.GetFileNameWithoutExtension(validFileName)} ({modifiedByName}){Path.GetExtension(validFileName)}";
                var contentLength = contentStream.CanSeek ? contentStream.Length : 0L;

                await processContentActionAsync(contentStream, cancellationToken);

                var response = new CardGetFileContentResponse { Size = contentLength, HasContent = true };
                response.SetSuggestedFileName(actualFileName);

                return response;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var errorResponse = new CardGetFileContentResponse
                {
                    HasContent = false,
                    Size = 0L,
                };

                if (ex is ValidationException validationException)
                {
                    errorResponse.ValidationResult.Add(validationException.Result);
                }
                else
                {
                    errorResponse.ValidationResult.AddException(this, ex);
                }

                return errorResponse;
            }
            finally
            {
                if (contentStream is not null)
                {
                    await contentStream.DisposeAsync();
                }
            }
        }

        #endregion
    }
}
