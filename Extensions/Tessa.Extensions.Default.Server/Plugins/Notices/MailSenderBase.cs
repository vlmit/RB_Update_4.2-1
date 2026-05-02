#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Forums.Notifications;
using Tessa.Extensions.Default.Server.SaaS;
using Tessa.Localization;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Formatting;
using Tessa.Platform.IO;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    public abstract class MailSenderBase(
        IDbScope dbScope,
        IOutboxManager outboxManager,
        IFormattingSettingsCache formattingSettingsCache,
        ISession session,
        Func<string?, IMailFileLoaderService> fileLoaderResolver,
        Func<string?, IMailSentNotificationService> mailNotificationResolver,
        ISaasMailFromAddressProvider? saasMailFromAddressProvider)
    {
        #region Nested Types

        private class MessageLoadingContext(MailSenderMessage message)
        {
            public long MaxFileSize { get; init; }
            public long TotalFileSize { get; set; }
            public List<MailSenderFile> TempFiles { get; } = [];
            public List<string> OversizedFiles { get; } = [];
            public ITempFile? LastTempFile { get; set; }
            public MailSenderMessage Message { get; init; } = NotNullOrThrow(message);
            public MailFile? File { get; set; }
        }

        #endregion

        #region Fields

        protected static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);
        private readonly IOutboxManager outboxManager = NotNullOrThrow(outboxManager);
        private readonly IFormattingSettingsCache formattingSettingsCache = NotNullOrThrow(formattingSettingsCache);
        private readonly ISession session = NotNullOrThrow(session);
        private readonly Func<string?, IMailFileLoaderService> fileLoaderResolver = NotNullOrThrow(fileLoaderResolver);
        private readonly Func<string?, IMailSentNotificationService> mailNotificationResolver = NotNullOrThrow(mailNotificationResolver);

        protected static readonly Regex MissedFilesRegex =
            new(@"<!--\s*[.]missed_files\s*-->", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        protected static readonly Regex OversizedFilesRegex =
            new(@"<!--\s*[.]oversized_files\s*-->", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        #endregion

        #region Properties

        /// <summary>
        /// Максимальный совокупный размер файлов, приложенных к письму, в килобайтах.
        /// </summary>
        public long MaxFilesSizeEmailKb { get; set; }

        public int MaxAttemptsBeforeDelete { get; set; }

        public Func<bool>? StopRequestedFunc { get; set; }

        protected bool StopRequested => this.StopRequestedFunc?.Invoke() ?? false;

        #endregion

        #region Public Methods

        public Task StartAsync(ConcurrentQueue<OutboxMessage> queue, CancellationToken cancellationToken = default) =>
            Task.Run(() => this.ProcessAsync(queue, cancellationToken), cancellationToken);

        #endregion

        #region Protected Methods

        protected abstract Task<bool> SendMessageAsync(MailSenderMessage message, CancellationToken cancellationToken = default);

        protected async Task LogProcessingErrorAsync(OutboxMessage message, object? error, CancellationToken cancellationToken = default)
        {
            var errorText = error is Exception ex ? ex.GetFullText() : error?.ToString();

            Logger.Error(
                "Error while processing message ID='{1}', Email='{2}', Subject='{3}'.{0}{4}",
                Environment.NewLine,
                message.ID,
                message.Email,
                message.Subject,
                errorText);

            message.Attempts++;

            if (message.Attempts < this.MaxAttemptsBeforeDelete)
            {
                await this.outboxManager.MarkAsBadMessageAsync(message.ID, message.Attempts, errorText, cancellationToken);
                return;
            }

            Logger.Trace(
                "Message has reached maximum number of attempts to send. Message is deleted from database."
                + " ID='{0}', Email='{1}', Subject='{2}'",
                message.ID,
                message.Email,
                message.Subject);

            await this.outboxManager.DeleteAsync(message.ID, cancellationToken);
        }

        protected static void AddMissedFilesInfo(ref string body, List<string> missedFiles, Regex template)
        {
            try
            {
                if (missedFiles.Count > 0)
                {
                    body = template.Replace(body, m =>
                    {
                        var sb = StringBuilderHelper.Acquire(64);
                        foreach (var missedFile in missedFiles)
                        {
                            sb.Append("<p><span class='data_description'>");
                            sb.Append(missedFile);
                            sb.Append("</span></p>");
                        }

                        return sb.ToStringAndRelease();
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, LogLevel.Error);
            }
        }

        protected async Task ProcessAsync(ConcurrentQueue<OutboxMessage> queue, CancellationToken cancellationToken = default)
        {
            if (queue.IsEmpty || this.StopRequested)
            {
                return;
            }

            await using (this.dbScope.Create())
            {
                while (queue.TryDequeue(out var outboxMessage))
                {
                    Logger.Trace("Processing message ID={0:B}, e-mail \"{1}\", subject: \"{2}\"", outboxMessage.ID, outboxMessage.Email, outboxMessage.Subject);

                    if (string.IsNullOrWhiteSpace(outboxMessage.Email)
                        || !FormattingHelper.EmailRegex.IsMatch(outboxMessage.Email))
                    {
                        await this.LogProcessingErrorAsync(outboxMessage, "Incorrect e-mail format: \"" + outboxMessage.Email + "\"", cancellationToken);
                        continue;
                    }

                    // каждое сообщение обрабатывается со своим идентификатором
                    RuntimeHelper.ServerRequestID = Guid.NewGuid();

                    using (var senderMessage = new MailSenderMessage(outboxMessage))
                    {
                        await this.ProcessMessageAsync(senderMessage, cancellationToken);
                        // диспоз senderMessage очистит временные файлы из senderMessage.MessageFiles
                    }

                    if (this.StopRequested)
                    {
                        return;
                    }
                }
            }
        }

        protected async Task ProcessMessageAsync(MailSenderMessage senderMessage, CancellationToken cancellationToken = default)
        {
            var fileLoaderService = this.fileLoaderResolver(senderMessage.Message.InstanceName);
            var mailNotificationService = this.mailNotificationResolver(senderMessage.Message.InstanceName);

            IDisposable? localizationScope = null;
            IAsyncDisposable? sessionContext = null;
            try
            {
                (localizationScope, sessionContext) = await this.PrepareLocalizationAndSessionAsync(senderMessage, cancellationToken);
                this.TrySetFromMessageAddress(senderMessage);

                Logger.Trace("Getting files.");
                var insideLocalizationScope = localizationScope is not null;
                var hasUnfinishedFiles = await this.FillFilesAndCheckHasUnfinishedFilesAsync(fileLoaderService, senderMessage, insideLocalizationScope, cancellationToken);

                if (!hasUnfinishedFiles)
                {
                    var success = await this.SendMessageAsync(senderMessage, cancellationToken);

                    if (success)
                    {
                        Logger.Trace("Deleting message from database.");
                        await this.outboxManager.DeleteAsync(senderMessage.Message.ID, cancellationToken);

                        await NotifyMailSentAsync(mailNotificationService, senderMessage, cancellationToken);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.LogProcessingErrorAsync(senderMessage.Message, ex, cancellationToken);
            }
            finally
            {
                localizationScope?.Dispose();

                if (sessionContext is not null)
                {
                    await sessionContext.DisposeAsync();
                }
            }
        }

        #endregion

        #region Private Methods

        private async ValueTask<(IDisposable? LocalizationScope, IAsyncDisposable? SessionContext)>
            PrepareLocalizationAndSessionAsync(MailSenderMessage senderMessage, CancellationToken cancellationToken)
        {
            // in normal (non SaaS) mode, instance name is always null
            // so if Instance name is not null - it's a SaaS mode and
            // there is no need to initialize localization - all data is already properly localized,
            // and session - used routes without user auth.
            if (senderMessage.Message.InstanceName is not null)
            {
                return (null, null);
            }

            var localizationScope = await this.PrepareLocalizationScopeAsync(senderMessage.Info, cancellationToken);
            var sessionContext = this.PrepareSessionContext(senderMessage.Info);

            return (localizationScope, sessionContext);
        }

        private async ValueTask<IDisposable> PrepareLocalizationScopeAsync(MailInfo mailInfo, CancellationToken cancellationToken)
        {
            var uiCulture = CultureInfo.GetCultureInfo(mailInfo.LanguageCode ?? LocalizationManager.EnglishLanguageCode);
            var formatName = mailInfo.FormatName ?? LocalizationManager.EnglishLanguageCode;

            return LocalizationManager.CreateScope(
                uiCulture,
                await this.formattingSettingsCache.TryGetCultureInfoAsync(formatName, cancellationToken)
                ?? CultureInfo.GetCultureInfo(formatName));
        }

        private IAsyncDisposable? PrepareSessionContext(MailInfo mailInfo)
        {
            if (!mailInfo.UserID.HasValue)
            {
                return null;
            }

            var token = this.session.CreateNestedSessionToken(mailInfo.UserID.Value, NotNullOrThrow(mailInfo.UserName));
            var offset = TimeSpan.FromMinutes(Convert.ToDouble(mailInfo.TimeZoneUtcOffsetMinutes));
            token.UtcOffset = offset;
            token.TimeZoneUtcOffset = offset;
            token.CalendarID = mailInfo.CalendarID!.Value;
            token.Seal();

            return SessionContext.Create(token);
        }

        private void TrySetFromMessageAddress(MailSenderMessage senderMessage)
        {
            if (saasMailFromAddressProvider is not null && senderMessage.Message.InstanceName is { } instanceName)
            {
                senderMessage.From = saasMailFromAddressProvider.GetFromAddress(instanceName);
            }
        }

        private async ValueTask<bool> FillFilesAndCheckHasUnfinishedFilesAsync(
            IMailFileLoaderService fileLoaderService,
            MailSenderMessage mailSenderMessage,
            bool insideLocalizationScope,
            CancellationToken cancellationToken)
        {
            MessageLoadingContext? context = null;
            try
            {
                if (mailSenderMessage.Info.TryGetFiles() is not { Count: not 0 } files)
                {
                    return false;
                }

                context = new MessageLoadingContext(mailSenderMessage) { MaxFileSize = this.MaxFilesSizeEmailKb * 1000 };
                // по мере вызова методов в цикле ниже, в context.TempFiles будут добавляться временные файлы;
                // их надо удалить для всех случаев, кроме тех, когда они же перенесены в mailSenderMessage.MessageFiles

                foreach (var file in files)
                {
                    context.File = file;
                    if (await LoadMailFileAsync(fileLoaderService, context, insideLocalizationScope, cancellationToken))
                    {
                        // если есть ещё недозагруженный контент, то отменяем отправку письма, но не помечаем письмо как удалённое
                        Logger.Trace(
                            "There are at least one attached file versionID='{0}' with uploading still in progress. Mail is postponed: \"{1}\", ID='{2}'.",
                            file.VersionID,
                            mailSenderMessage.Message.Subject,
                            mailSenderMessage.Message.ID);
                        return true;
                    }
                }

                mailSenderMessage.MessageFiles.AddRange(context.TempFiles);
                mailSenderMessage.OversizedFiles.AddRange(context.OversizedFiles);
                
                // все временные файлы context.TempFiles перенесены в mailSenderMessage.MessageFiles, т.е. они будут удалены вместе с mailSenderMessage.Dispose();
                // зануляем контекст, чтобы не очищать их в finally ниже
                context = null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogException(ex, LogLevel.Error);
            }
            finally
            {
                // если контекст не занулён, то временные файлы в нём необходимо удалить из временной папки
                if (context is not null)
                {
                    foreach (var senderFile in context.TempFiles)
                    {
                        senderFile.File.Dispose();
                    }
                }
            }

            return false;
        }

        private static async Task<bool> LoadMailFileAsync(
            IMailFileLoaderService fileLoaderService,
            MessageLoadingContext messageContext,
            bool insideLocalizationScope,
            CancellationToken cancellationToken)
        {
            var file = NotNullOrThrow(messageContext.File);

            var userID = messageContext.Message.Info.UserID;
            var cardID = file.CardID ?? messageContext.Message.Info.CardID;
            var cardTypeID = file.CardTypeID ?? (file.CardID.HasValue ? null : messageContext.Message.Info.CardTypeID);
            var cardTypeName = file.CardTypeName ?? (file.CardID.HasValue ? null : messageContext.Message.Info.CardTypeName);

            var fileName = file.FileName;
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "file";
            }

            Logger.Trace("Getting file '{0}'. File ID: '{1}'. Version ID: '{2}'. Card ID: '{3}'", fileName, file.FileID, file.VersionID, cardID);

            var cid = file.Info.TryGet<string?>(EmailAttachmentsHelper.CidKey);
            var getOversizeFileTextFuncAsync = () => ResolveFileErrorTextAsync(messageContext, fileName, insideLocalizationScope,
                MailInfo.FileOversizeMessageKey, MailDefaultLocalizationNames.FileOversizeMessage);

            try
            {
                var response =
                    await fileLoaderService.TryLoadContentAsync(
                        cardID,
                        cardTypeID,
                        cardTypeName,
                        file,
                        (stream, ct) => new(StoreFileAsync(messageContext, fileName, cid, getOversizeFileTextFuncAsync, stream, ct)),
                        userID,
                        cancellationToken);
                // здесь в messageContext.TempFiles мог быть добавлен временный файл, который даже в случае cancellation надо не забыть удалить

                // absent response may be only in the case of uploading file content
                if (response is null)
                {
                    return true;
                }

                // кастомную инфу из расширений прокидываем обратно в MailFile.Info,
                // чтобы можно было достучаться в расширениях после успешной отправки письма CardRequestTypes.MailSent
                StorageHelper.Merge(response.Info, file.Info);

                var validationResult = response.ValidationResult.Build();
                Logger.LogResult(validationResult);
                if (!validationResult.IsSuccessful)
                {
                    messageContext.Message.MissedFiles.Add(await ResolveFileErrorTextAsync(messageContext, fileName, insideLocalizationScope,
                        MailInfo.MissedFileMessageKey, MailDefaultLocalizationNames.MissedFileMessage));
                    return false;
                }

                var suggestedFileName = response.TryGetSuggestedFileName();
                if (messageContext.LastTempFile is { } lastTempFile && !string.IsNullOrEmpty(suggestedFileName))
                {
                    // если имена одинаковые без учёта регистра, то Rename ничего не сделает
                    lastTempFile.Rename(suggestedFileName);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.LogException(ex, LogLevel.Error);
                messageContext.Message.MissedFiles.Add(await ResolveFileErrorTextAsync(messageContext, fileName, insideLocalizationScope,
                    MailInfo.MissedFileMessageKey, MailDefaultLocalizationNames.MissedFileMessage));
            }

            return false;
        }

        private static async ValueTask<string> ResolveFileErrorTextAsync(
            MessageLoadingContext messageContext,
            string? fileName,
            bool insideLocalizationScope,
            string templateKey,
            string defaultLocalizationName)
        {
            // template is not empty, when message is sent via NotificationManager, but it may be empty for low level API IMailService.PostMessageAsync
            var template = messageContext.Message.Info.TryGetLocalization()?.TryGet(templateKey);
            if (string.IsNullOrEmpty(template))
            {
                // insideLocalizationScope is false for SaaS, therefore here we have no info on user's localization language => localize in English
                template = insideLocalizationScope
                    ? await LocalizeNameAsync(defaultLocalizationName)
                    : await LocalizeNameAsync(defaultLocalizationName, LocalizationManager.EnglishCultureInfo);
            }

            var file = messageContext.File!;
            return string.Format(template, fileName, file.FileID, file.VersionID);
        }

        private static async Task StoreFileAsync(
            MessageLoadingContext messageContext,
            string fileName,
            string? cid,
            Func<ValueTask<string>> getOversizeFileTextFuncAsync,
            Stream stream,
            CancellationToken cancellationToken)
        {
            var tempFile = TempFile.Acquire(fileName);

            try
            {
                long size;
                await using (var tempFileStream = FileHelper.Create(tempFile.Path))
                {
                    await stream.CopyToAsync(tempFileStream, cancellationToken);
                    size = tempFileStream.Position;
                }

                messageContext.TotalFileSize += size;

                if (messageContext.TotalFileSize <= messageContext.MaxFileSize)
                {
                    messageContext.LastTempFile = tempFile;
                    messageContext.TempFiles.Add(new(tempFile, cid));
                }
                else
                {
                    messageContext.LastTempFile = null;
                    messageContext.OversizedFiles.Add(await getOversizeFileTextFuncAsync());
                    tempFile.Dispose();
                }
            }
            catch (Exception)
            {
                tempFile.Dispose();
                throw;
            }
        }

        private static async Task NotifyMailSentAsync(IMailSentNotificationService notificationService, MailSenderMessage message, CancellationToken cancellationToken)
        {
            var result = await notificationService.NotifyMailSentAsync(message, cancellationToken);
            Logger.LogResult(result);
        }

        #endregion
    }
}
