using Tessa.Notices;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    public static class MailSenderExtensions
    {
        public static MailSentRequest ToMailSentRequest(this MailSenderMessage message) =>
            new()
            {
                ID = message.Message.ID,
                Email = message.Message.Email,
                Body = message.Message.Body,
                Subject = message.Message.Subject,
                Info = message.Info.GetStorage(),
            };
    }
}
