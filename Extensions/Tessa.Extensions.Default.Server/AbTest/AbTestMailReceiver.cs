using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Notices;
using Tessa.Extensions.Default.Server.Plugins.Notices;
using Tessa.Extensions.Default.Server.Workflow;

namespace Tessa.Extensions.Default.Server.AbTest
{
    public class AbTestMailReceiver(IMessageProcessor processor) : IMailReceiver
    {
        #region Properties

        /// <summary>
        /// Объект, выполняющий обработку сообщений.
        /// </summary>
        public IMessageProcessor Processor { get; } = NotNullOrThrow(processor);

        /// <summary>
        /// Список сообщений, полученных извне.
        /// </summary>
        public List<MailSenderMessage> ReceivedMessages { get; set; } = [];

        #endregion

        #region IMailReceiver Methods

        /// <inheritdoc/>
        public Func<bool>? StopRequestedFunc { get; set; }

        /// <inheritdoc/>
        public async Task ReceiveMessagesAsync(CancellationToken cancellationToken = default)
        {
            // Перебираем сообщения из свойства ReceivedMessages и отдаём их процессору
            foreach (var message in this.ReceivedMessages)
            {
                var attachments = new List<NoticeAttachment>(message.MessageFiles.Count);
                foreach (var messageFile in message.MessageFiles)
                {
                    attachments.Add(
                        new()
                        {
                            Name = messageFile.File.Name,
                            Data = await File.ReadAllBytesAsync(messageFile.File.Path, cancellationToken),
                        });
                }

                await this.Processor.ProcessMessageAsync(
                    new()
                    {
                        From = message.Message.Email,
                        Subject = message.Message.Subject,
                        Body = message.Message.Body?.Trim(),
                        Attachments = attachments.ToArray(),
                    },
                    cancellationToken);
            }
        }

        #endregion
    }
}
