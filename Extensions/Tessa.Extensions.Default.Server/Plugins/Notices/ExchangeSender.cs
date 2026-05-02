#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Exchange.WebServices.Data;
using Tessa.Extensions.Default.Server.Notices;
using Tessa.Extensions.Default.Server.SaaS;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Formatting;
using Tessa.Platform.Runtime;
using Unity;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    public sealed class ExchangeSender(
        IDbScope dbScope,
        IOutboxManager outboxManager,
        IFormattingSettingsCache formattingSettingsCache,
        ISession session,
        Func<string?, IMailFileLoaderService> fileLoaderResolver,
        Func<string?, IMailSentNotificationService> mailNotificationResolver,
        [OptionalDependency] ISaasMailFromAddressProvider? saasMailFromAddressProvider = null)
        :
            MailSenderBase(dbScope, outboxManager, formattingSettingsCache, session, fileLoaderResolver, mailNotificationResolver, saasMailFromAddressProvider),
            IAsyncDisposable
    {
        #region Fields

        private ExchangeService? exchangeService;

        private ExchangeSettings? settings;

        private readonly AsyncLock asyncLock = new();

        #endregion

        #region Properties

        [DisallowNull]
        public ExchangeSettings? Settings
        {
            get => this.settings;
            set
            {
                if (this.settings != value)
                {
                    this.settings = value;
                    this.exchangeService = null;
                }
            }
        }

        #endregion

        #region Private Methods

        private async ValueTask EnsureInitializedAsync(CancellationToken cancellationToken = default)
        {
            if (this.settings is null)
            {
                throw new InvalidOperationException("Can't initialize exchange sender without its settings.");
            }

            if (this.exchangeService is null)
            {
                using (await this.asyncLock.EnterAsync(cancellationToken))
                {
                    this.exchangeService ??= await ExchangeServiceHelper.CreateExchangeServiceAsync(this.settings, cancellationToken);
                }
            }
        }

        #endregion

        #region Base Overrides

        protected override async Task<bool> SendMessageAsync(MailSenderMessage message, CancellationToken cancellationToken = default)
        {
            await this.EnsureInitializedAsync(cancellationToken);

            ThrowIfNull(this.settings);

            try
            {
                var body = message.Message.Body ?? string.Empty;

                AddMissedFilesInfo(ref body, message.MissedFiles, MissedFilesRegex);
                AddMissedFilesInfo(ref body, message.OversizedFiles, OversizedFilesRegex);

                var exchangeMessage = new EmailMessage(this.exchangeService)
                {
                    Subject = message.Message.Subject,
                    Body = new MessageBody(message.Info.Format == MailFormat.PlainText ? BodyType.Text : BodyType.HTML, body),
                    From = new EmailAddress(
                        string.IsNullOrWhiteSpace(this.settings.FromDisplayName) ? null : this.settings.FromDisplayName,
                        string.IsNullOrWhiteSpace(message.From ?? this.settings.From) ? this.settings.User : message.From ?? this.settings.From),
                };

                var mainRecipientDisplayName = message.Info.MainRecipientDisplayName;

                var toAddress = string.IsNullOrWhiteSpace(mainRecipientDisplayName)
                    ? new EmailAddress(message.Message.Email)
                    : new EmailAddress(mainRecipientDisplayName, message.Message.Email);

                exchangeMessage.ToRecipients.Add(toAddress);

                var recipients = message.Info.TryGetRecipients();
                if (recipients is { Count: > 0 })
                {
                    foreach (var recipient in recipients)
                    {
                        var displayName = recipient.DisplayName;

                        var recipientAddress = string.IsNullOrWhiteSpace(displayName)
                            ? new EmailAddress(recipient.Email)
                            : new EmailAddress(displayName, recipient.Email);

                        switch (recipient.Type)
                        {
                            case MailRecipientType.To:
                                exchangeMessage.ToRecipients.Add(recipientAddress);
                                break;

                            case MailRecipientType.Cc:
                                exchangeMessage.CcRecipients.Add(recipientAddress);
                                break;

                            case MailRecipientType.Bcc:
                                exchangeMessage.BccRecipients.Add(recipientAddress);
                                break;

                            default:
                                throw ArgumentOutOfRange(recipient.Type);
                        }
                    }
                }

                foreach (var mailSenderFile in message.MessageFiles)
                {
                    try
                    {
                        var attachment = exchangeMessage.Attachments.AddFileAttachment(mailSenderFile.File.Path);
                        if (!string.IsNullOrEmpty(mailSenderFile.ContentID))
                        {
                            attachment.ContentId = $"<{mailSenderFile.ContentID}>";
                            attachment.IsInline = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogException(ex);
                    }
                }

                Logger.Trace("Sending message ID='{0}'", message.Message.ID);
                await exchangeMessage.Send(cancellationToken);
                Logger.Trace("Message sent. ID='{0}'", message.Message.ID);

                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                await this.LogProcessingErrorAsync(message.Message, ex, cancellationToken);
                return false;
            }
        }

        #endregion

        #region IAsyncDisposable Members

        public ValueTask DisposeAsync()
        {
            this.asyncLock.Dispose();
            return ValueTask.CompletedTask;
        }

        #endregion
    }
}
