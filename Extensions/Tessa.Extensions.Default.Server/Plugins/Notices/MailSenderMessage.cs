#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Notices;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    public sealed class MailSenderMessage(OutboxMessage message) : IDisposable, IMailSentNotificationInfo
    {
        #region Properties

        public string? From { get; set; }

        public OutboxMessage Message { get; } = NotNullOrThrow(message);

        /// <inheritdoc cref="IMailSentNotificationInfo.Info"/>
        public MailInfo Info { get; } = MailInfo.TryDeserialize(message.Info) ?? new MailInfo();

        public List<MailSenderFile> MessageFiles { get; } = [];

        public List<string> MissedFiles { get; } = [];

        public List<string> OversizedFiles { get; } = [];

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            foreach (var messageFile in this.MessageFiles)
            {
                messageFile.File.Dispose();
            }
        }

        #endregion

        #region IMailSentNotificationInfo Members

        /// <inheritdoc/>
        Guid IMailSentNotificationInfo.ID => this.Message.ID;

        /// <inheritdoc/>
        string? IMailSentNotificationInfo.Email => this.Message.Email;

        /// <inheritdoc/>
        string? IMailSentNotificationInfo.Subject => this.Message.Subject;

        /// <inheritdoc/>
        string? IMailSentNotificationInfo.Body => this.Message.Body;

        /// <inheritdoc/>
        Dictionary<string, object?> IMailSentNotificationInfo.Info => this.Info.GetStorage();

        #endregion
    }
}
