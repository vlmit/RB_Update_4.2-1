#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <summary>
    /// Mail send notification service.
    /// </summary>
    public interface IMailSentNotificationService
    {
        /// <summary>
        /// Notify mail message send.
        /// </summary>
        /// <param name="message"><inheritdoc cref="MailSenderMessage" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Makes all needed notifications about mail message send.</returns>
        Task<ValidationResult> NotifyMailSentAsync(MailSenderMessage message, CancellationToken cancellationToken = default);
    }
}
