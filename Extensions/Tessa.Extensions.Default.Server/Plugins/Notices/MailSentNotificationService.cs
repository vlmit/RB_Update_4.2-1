using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    public sealed class MailSentNotificationService : IMailSentNotificationService
    {
        private readonly ICardRepository cardRepository;

        public MailSentNotificationService(ICardRepository cardRepository) =>
            this.cardRepository = NotNullOrThrow(cardRepository);
        
        public async Task<ValidationResult> NotifyMailSentAsync(MailSenderMessage message, CancellationToken cancellationToken = default)
        {
            var response = await this.cardRepository.NotifyMailSentAsync(message, cancellationToken);
            return response.ValidationResult.Build();
        }
    }
}
