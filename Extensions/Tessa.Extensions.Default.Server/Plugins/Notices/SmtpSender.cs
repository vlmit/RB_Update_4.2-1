#nullable enable

using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using NLog;
using Tessa.Extensions.Default.Server.SaaS;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Formatting;
using Tessa.Platform.IO;
using Tessa.Platform.Runtime;
using Unity;
using MailKitSmtpClient = MailKit.Net.Smtp.SmtpClient;
using NetSmtpClient = System.Net.Mail.SmtpClient;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    public sealed class SmtpSender(
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

        private NetSmtpClient? netSmtpClient;

        private ISmtpClient? mailKitSmtpClient;

        private readonly AsyncLock asyncLock = new();

        #endregion

        #region Private Methods

        private async ValueTask<object> EnsureSmtpClientInitializedAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfNull(this.Settings);

            var client = (object?) this.mailKitSmtpClient ?? this.netSmtpClient;
            if (client is null)
            {
                using (await this.asyncLock.EnterAsync(cancellationToken))
                {
                    var pickupDirectoryLocation = this.Settings.SmtpPickupDirectoryLocation;
                    if (!string.IsNullOrEmpty(pickupDirectoryLocation))
                    {
                        var netSmtpClient = CreateNetSmtpClient(pickupDirectoryLocation);

                        this.netSmtpClient = netSmtpClient;
                        client = netSmtpClient;
                    }
                    else
                    {
                        var mailKitSmtpClient = await CreateMailKitSmtpClientAsync(cancellationToken);

                        this.mailKitSmtpClient = mailKitSmtpClient;
                        client = mailKitSmtpClient;
                    }
                }
            }

            return client;
        }

        #endregion

        #region Properties

        /// <inheritdoc cref="SmtpSettings"/>
        public SmtpSettings? Settings { get; set; }

        #endregion

        #region Base Overrides

        protected override async Task<bool> SendMessageAsync(MailSenderMessage message, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(this.Settings);

            var client = await this.EnsureSmtpClientInitializedAsync(cancellationToken);

            try
            {
                var body = message.Message.Body ?? string.Empty;

                AddMissedFilesInfo(ref body, message.MissedFiles, MissedFilesRegex);
                AddMissedFilesInfo(ref body, message.OversizedFiles, OversizedFilesRegex);

                var mainRecipientDisplayName = message.Info.MainRecipientDisplayName;

                switch (client)
                {
                    case NetSmtpClient netSmtpClient:
                        var fromAddress = new MailAddress(
                            NotEmptyOrThrow(message.From ?? this.Settings.SmtpFrom),
                            string.IsNullOrWhiteSpace(this.Settings.SmtpFromDisplayName) ? null : this.Settings.SmtpFromDisplayName,
                            Encoding.UTF8);

                        var toAddress = new MailAddress(
                            NotEmptyOrThrow(message.Message.Email),
                            string.IsNullOrWhiteSpace(mainRecipientDisplayName) ? null : mainRecipientDisplayName,
                            Encoding.UTF8);

                        using (var smtpMessage = new MailMessage(fromAddress, toAddress))
                        {
                            smtpMessage.Subject = message.Message.Subject;
                            smtpMessage.Body = body;
                            smtpMessage.IsBodyHtml = message.Info.Format != MailFormat.PlainText;
                            smtpMessage.SubjectEncoding = Encoding.UTF8;
                            smtpMessage.BodyEncoding = Encoding.UTF8;
                            smtpMessage.BodyTransferEncoding = TransferEncoding.EightBit;
                            var recipients = message.Info.TryGetRecipients();
                            if (recipients is { Count: > 0 })
                            {
                                foreach (var recipient in recipients)
                                {
                                    var displayName = recipient.DisplayName;

                                    var recipientAddress = new MailAddress(
                                        NotEmptyOrThrow(recipient.Email),
                                        string.IsNullOrWhiteSpace(displayName) ? null : displayName,
                                        Encoding.UTF8);

                                    switch (recipient.Type)
                                    {
                                        case MailRecipientType.To:
                                            smtpMessage.To.Add(recipientAddress);
                                            break;

                                        case MailRecipientType.Cc:
                                            smtpMessage.CC.Add(recipientAddress);
                                            break;

                                        case MailRecipientType.Bcc:
                                            smtpMessage.Bcc.Add(recipientAddress);
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
                                    var data = new Attachment(mailSenderFile.File.Path);
                                    if (!string.IsNullOrEmpty(mailSenderFile.ContentID))
                                    {
                                        data.ContentId = mailSenderFile.ContentID;
                                        data.ContentDisposition!.Inline = true;
                                    }

                                    smtpMessage.Attachments.Add(data);
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogException(ex, LogLevel.Error);
                                }
                            }

                            Logger.Trace("Sending message ID='{0}'", message.Message.ID);
                            await netSmtpClient.SendMailAsync(smtpMessage, cancellationToken);
                            Logger.Trace("Message was sent, ID='{0}'", message.Message.ID);
                        }

                        break;

                    case ISmtpClient mailKitSmtpClient:
                        var mimeMessage = new MimeMessage
                        {
                            Subject = message.Message.Subject ?? string.Empty,
                        };

                        var bodyBuilder = new BodyBuilder();
                        if (message.Info.Format != MailFormat.PlainText)
                        {
                            bodyBuilder.HtmlBody = body;
                        }
                        else
                        {
                            bodyBuilder.TextBody = body;
                        }

                        foreach (var mailSenderFile in message.MessageFiles)
                        {
                            try
                            {
                                var isCidNull = string.IsNullOrEmpty(mailSenderFile.ContentID);
                                var attachment = isCidNull
                                    ? await bodyBuilder.Attachments.AddAsync(mailSenderFile.File.Path, cancellationToken)
                                    : await bodyBuilder.LinkedResources.AddAsync(mailSenderFile.File.Path, cancellationToken);
                                if (!isCidNull)
                                {
                                    attachment.ContentId = mailSenderFile.ContentID;

                                    // for gmail
                                    if (!string.IsNullOrEmpty(mailSenderFile.ContentID))
                                    {
                                        attachment.Headers.Add("X-Attachment-Id", mailSenderFile.ContentID);
                                    }
                                }

                                MakeupRfcSpecName(attachment);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogException(ex, LogLevel.Error);
                            }
                        }

                        mimeMessage.Body = bodyBuilder.ToMessageBody();

                        mimeMessage.From.Add(
                            new MailboxAddress(
                                string.IsNullOrWhiteSpace(this.Settings.SmtpFromDisplayName) ? null : this.Settings.SmtpFromDisplayName,
                                NotNullOrThrow(message.From ?? this.Settings.SmtpFrom)));

                        mimeMessage.To.Add(
                            new MailboxAddress(
                                string.IsNullOrWhiteSpace(mainRecipientDisplayName) ? null : mainRecipientDisplayName,
                                NotNullOrThrow(message.Message.Email)));

                        var mimeRecipients = message.Info.TryGetRecipients();
                        if (mimeRecipients is { Count: > 0 })
                        {
                            foreach (var recipient in mimeRecipients)
                            {
                                var displayName = recipient.DisplayName;

                                var recipientAddress = new MailboxAddress(
                                    string.IsNullOrWhiteSpace(displayName) ? null : displayName,
                                    NotNullOrThrow(recipient.Email));

                                switch (recipient.Type)
                                {
                                    case MailRecipientType.To:
                                        mimeMessage.To.Add(recipientAddress);
                                        break;

                                    case MailRecipientType.Cc:
                                        mimeMessage.Cc.Add(recipientAddress);
                                        break;

                                    case MailRecipientType.Bcc:
                                        mimeMessage.Bcc.Add(recipientAddress);
                                        break;

                                    default:
                                        throw ArgumentOutOfRange(recipient.Type);
                                }
                            }
                        }

                        Logger.Trace("Sending message ID='{0}'", message.Message.ID);
                        await mailKitSmtpClient.SendAsync(mimeMessage, cancellationToken);
                        Logger.Trace("Message was sent, ID='{0}'", message.Message.ID);

                        break;

                    default:
                        throw new InvalidOperationException("Unsupported smtp client of type " + client.GetType().AssemblyQualifiedName);
                }

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

        #region Methods

        private static void MakeupRfcSpecName(MimeEntity attachment)
        {
            // For greater compatibility with Outlook/Exchange clients, we'll have
            // to specify the filename in Rfc2047 encoding as well. So what we'll
            // do is this: for Content-Disposition we'll follow Rfc2231, and for
            // Content-Type we'll (incorrectly) follow Rfc2047.
            // https://github.com/jstedfast/MimeKit/issues/585
            if (string.IsNullOrEmpty(attachment.ContentType.Name))
            {
                return;
            }

            attachment.ContentDisposition?.FileName = attachment.ContentType.Name;
            if (attachment.ContentType.Parameters.TryGetValue("name", out Parameter? name))
            {
                name.EncodingMethod = ParameterEncodingMethod.Rfc2047;
            }
        }

        private static NetSmtpClient CreateNetSmtpClient(string pickupDirectoryLocation)
        {
            // складываем письма в папку, которая соответствует либо абсолютному пути, либо пути относительно папки с текущей сборкой
            if (!Path.IsPathRooted(pickupDirectoryLocation))
            {
                var currentFolder = Assembly.GetExecutingAssembly().GetActualLocationFolder();
                pickupDirectoryLocation = Path.Combine(currentFolder, pickupDirectoryLocation);
            }

            // если папка уже есть или не удалось её создать - игнорируем ошибки, их будет выбрасывать сам SmtpClient
            FileHelper.CreateDirectoryIfNotExists(pickupDirectoryLocation);

            // указываем хост "localhost", чтобы не было обращений к конфигурации
            return new NetSmtpClient("localhost")
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = pickupDirectoryLocation,
            };
        }


        private async Task<ISmtpClient> CreateMailKitSmtpClientAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfNull(this.Settings);

            // отправляем письма по SMTP, читаем все настройки из конфига
            var host = this.Settings.SmtpHost;
            var port = this.Settings.SmtpPort;
            var enableSsl = this.Settings.SmtpEnableSsl;
            var defaultCredentials = this.Settings.SmtpDefaultCredentials;
            var userName = this.Settings.SmtpUserName;
            var password = this.Settings.SmtpPassword;
            var clientDomain = this.Settings.SmtpClientDomain;
            var timeout = this.Settings.SmtpTimeout;

            MailKitSmtpClient? client = null;

            try
            {
                client = new MailKitSmtpClient { ServerCertificateValidationCallback = (s, c, h, e) => true };

                if (timeout > 0)
                {
                    client.Timeout = timeout;
                }

                await client.ConnectAsync(host, port, enableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None, cancellationToken);

                if ((client.Capabilities & SmtpCapabilities.Authentication) != 0
                    && (defaultCredentials || !string.IsNullOrEmpty(userName)))
                {
                    var credentials = defaultCredentials
                        ? CredentialCache.DefaultCredentials
                        : string.IsNullOrEmpty(clientDomain)
                            ? new NetworkCredential(userName, password)
                            : new NetworkCredential(userName, password, clientDomain);

                    await client.AuthenticateAsync(credentials, cancellationToken);
                }

                var result = client;
                client = null;

                return result;
            }
            finally
            {
                if (client is not null)
                {
                    if (client.IsConnected)
                    {
                        await client.DisconnectAsync(true, cancellationToken);
                    }

                    client.Dispose();
                }
            }
        }

        #endregion

        #region IAsyncDisposable Members

        public async ValueTask DisposeAsync()
        {
            if (this.netSmtpClient is not null)
            {
                this.netSmtpClient.Dispose();
                this.netSmtpClient = null;
            }

            if (this.mailKitSmtpClient is not null)
            {
                try
                {
                    await this.mailKitSmtpClient.DisconnectAsync(true);
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, LogLevel.Warn);
                }

                this.mailKitSmtpClient.Dispose();
                this.mailKitSmtpClient = null;
            }

            this.asyncLock.Dispose();
        }

        #endregion
    }
}
