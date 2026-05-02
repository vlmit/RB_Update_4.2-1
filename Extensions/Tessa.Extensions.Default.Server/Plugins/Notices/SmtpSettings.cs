#nullable enable

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <summary>
    /// Настройки <see cref="SmtpSender"/>.
    /// </summary>
    public sealed class SmtpSettings
    {
        /// <summary>
        /// Адрес, от имени которого выполняется отправка уведомлений.
        /// </summary>
        public string? SmtpFrom { get; set; }

        /// <summary>
        /// Отображаемое имя отправителя при отправке уведомлений через SMTP.
        /// </summary>
        public string? SmtpFromDisplayName { get; set; }

        /// <summary>
        /// Имя пользователя для подключения к серверу.
        /// </summary>
        public string? SmtpUserName { get; set; }

        /// <summary>
        /// Адрес сервера.
        /// </summary>
        public string? SmtpHost { get; set; }

        /// <summary>
        /// Порт для подключения к серверу.
        /// </summary>
        public int SmtpPort { get; set; }

        /// <summary>
        /// Определяет, что при подключении должен использоваться SSL.
        /// </summary>
        public bool SmtpEnableSsl { get; set; }

        /// <summary>
        /// Определяет, следует ли использовать учетные данные пользователя по умолчанию для доступа к SMTP-серверу для SMTP-транзакций.
        /// </summary>
        public bool SmtpDefaultCredentials { get; set; }

        /// <summary>
        /// Пароль пользователя для подключения к серверу.
        /// </summary>
        public string? SmtpPassword { get; set; }

        /// <summary>
        /// Определяет имя домена клиента, используемое запросом протокола SMTP для подключения к почтовому SMTP-серверу.
        /// </summary>
        public string? SmtpClientDomain { get; set; }

        /// <summary>
        /// Таймаут подключения к SMTP-серверу в миллисекундах.
        /// </summary>
        public int SmtpTimeout { get; set; }

        /// <summary>
        /// Настройки для выгрузки почты в папку. При использовании настройки подключения к серверу не применяются.
        /// </summary>
        public string? SmtpPickupDirectoryLocation { get; set; }
    }
}
