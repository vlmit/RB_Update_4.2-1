#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Exchange.WebServices.Data;
using Tessa.Platform;
using Tessa.Platform.Plugins;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <summary>
    /// Настройки плагина <see cref="DefaultPluginNames.MailSenderPlugin"/>.
    /// </summary>
    public sealed class MailSenderPluginSettings : PluginSettings
    {
        #region Constructors

        /// <inheritdoc cref="PluginSettings(string)"/>
        public MailSenderPluginSettings(
            string pluginName)
            : base(pluginName)
        {
        }

        #endregion

        #region Properties

        public string? Mode { get; set; }

        /// <summary>
        /// Ключ API, сгенерированный на сервере Exchange для аутентификации OAuth при подключении к Exchange.
        /// </summary>
        public string? ExchangeOAuthToken { get; set; }

        /// <summary>
        /// Имя пользователя при подключении к Exchange.
        /// </summary>
        public string? ExchangeUser { get; set; }

        /// <summary>
        /// Пароль пользователя при подключении к Exchange.
        /// </summary>
        public string? ExchangePassword { get; set; }

        /// <summary>
        /// Адрес сервера при подключении к Exchange.
        /// </summary>
        public string? ExchangeServer { get; set; }

        /// <summary>
        /// Адрес прокси сервера при подключении к Exchange.
        /// </summary>
        public string? ExchangeProxyAddress { get; set; }

        /// <summary>
        /// Имя пользователя для прокси сервера при подключении к Exchange.
        /// </summary>
        public string? ExchangeProxyUser { get; set; }

        /// <summary>
        /// Пароль пользователя для прокси сервера при подключении к Exchange.
        /// </summary>
        public string? ExchangeProxyPassword { get; set; }

        /// <summary>
        /// Почта, от имени которой идёт отправка уведомлений при подключении к Exchange.
        /// </summary>
        public string? ExchangeFrom { get; set; }

        /// <summary>
        /// Отображаемое имя отправителя уведомлений при подключении к Exchange.
        /// </summary>
        public string? ExchangeFromDisplayName { get; set; }

        /// <summary>
        /// Версия Exchange сервера.
        /// </summary>
        public ExchangeVersion? ExchangeVersion { get; set; }

        /// <summary>
        /// Адрес, от имени которого выполняется отправка уведомлений при отправке их черех SMTP.
        /// </summary>
        public string? SmtpFrom { get; set; }

        /// <summary>
        /// Отображаемое имя отправителя при отправке уведомлений через SMTP.
        /// </summary>
        public string? SmtpFromDisplayName { get; set; }

        /// <summary>
        /// Имя пользователя для подключения к серверу при отправке уведомлений через SMTP.
        /// </summary>
        public string? SmtpUserName { get; set; }

        /// <summary>
        /// Адрес сервера при отправке уведомлений через SMTP.
        /// </summary>
        public string? SmtpHost { get; set; }

        /// <summary>
        /// Порт для подключения к серверу при отправке уведомлений через SMTP.
        /// </summary>
        public int SmtpPort { get; set; }

        /// <summary>
        /// Определяет, что при подключении должен использоваться SSL при отправке уведомлений через SMTP.
        /// </summary>
        public bool SmtpEnableSsl { get; set; }

        /// <summary>
        /// Определяет, следует ли использовать учетные данные пользователя по умолчанию для доступа к SMTP-серверу для SMTP-транзакций при отправке уведомлений через SMTP.
        /// </summary>
        public bool SmtpDefaultCredentials { get; set; }

        /// <summary>
        /// Пароль пользователя для подключения к серверу при отправке уведомлений через SMTP.
        /// </summary>
        public string? SmtpPassword { get; set; }

        /// <summary>
        /// Определяет имя домена клиента, используемое запросом протокола SMTP для подключения к почтовому SMTP-серверу при отправке уведомлений через SMTP.
        /// </summary>
        public string? SmtpClientDomain { get; set; }

        /// <summary>
        /// Таймаут подключения к SMTP-серверу в миллисекундах при отправке уведомлений через SMTP.
        /// </summary>
        public int SmtpTimeout { get; set; }

        /// <summary>
        /// Настройки для выгрузки почты в папку. При использовании настройки подключения к серверу не применяются. Используется при отправке уведомлений через SMTP.
        /// </summary>
        public string? SmtpPickupDirectoryLocation { get; set; }

        /// <summary>
        /// Число сообщений, обрабатываемых за один запуск плагина.
        /// </summary>
        public int NumberOfMessagesToProcessAtOnce { get; set; }

        /// <summary>
        /// Количество неудачных попыток отправки сообщения до того, как оно будет удалено из папки исходящих сообщений.
        /// </summary>
        public int MaxAttemptsBeforeDelete { get; set; }

        /// <summary>
        /// Интервал времени, который должен пройти прежде, чем будет совершена новая попытка отправки сообщения, по которому произошла ошибка отправки.
        /// </summary>
        public int RetryIntervalMinutes { get; set; }

        /// <summary>
        /// Максимально допустимый общий размер всех приложенных к письму файлов. Указывается в килобайтах.
        /// </summary>
        public long MaxFilesSizeEmail { get; set; }

        /// <summary>
        /// Максимальное количество потоков, которые используются для параллельной отправки почты.
        /// </summary>
        public int MaxNumberWorkingProcesses { get; set; }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);

            storage[nameof(this.Mode)] = this.Mode;

            storage[nameof(this.SmtpFrom)] = this.SmtpFrom;
            storage[nameof(this.SmtpFromDisplayName)] = this.SmtpFromDisplayName;
            storage[nameof(this.SmtpUserName)] = this.SmtpUserName;
            storage[nameof(this.SmtpHost)] = this.SmtpHost;
            storage[nameof(this.SmtpPort)] = this.SmtpPort;
            storage[nameof(this.SmtpEnableSsl)] = BooleanBoxes.Box(this.SmtpEnableSsl);
            storage[nameof(this.SmtpDefaultCredentials)] = BooleanBoxes.Box(this.SmtpDefaultCredentials);
            storage[nameof(this.SmtpPassword)] = this.SmtpPassword;
            storage[nameof(this.SmtpClientDomain)] = this.SmtpClientDomain;
            storage[nameof(this.SmtpTimeout)] = this.SmtpTimeout;
            storage[nameof(this.SmtpPickupDirectoryLocation)] = this.SmtpPickupDirectoryLocation;

            storage[nameof(this.ExchangeOAuthToken)] = this.ExchangeOAuthToken;
            storage[nameof(this.ExchangeUser)] = this.ExchangeUser;
            storage[nameof(this.ExchangePassword)] = this.ExchangePassword;
            storage[nameof(this.ExchangeServer)] = this.ExchangeServer;
            storage[nameof(this.ExchangeProxyAddress)] = this.ExchangeProxyAddress;
            storage[nameof(this.ExchangeProxyUser)] = this.ExchangeProxyUser;
            storage[nameof(this.ExchangeProxyPassword)] = this.ExchangeProxyPassword;
            storage[nameof(this.ExchangeVersion)] = this.ExchangeVersion?.ToString();
            storage[nameof(this.ExchangeFrom)] = this.ExchangeFrom;
            storage[nameof(this.ExchangeFromDisplayName)] = this.ExchangeFromDisplayName;

            storage[nameof(this.NumberOfMessagesToProcessAtOnce)] = this.NumberOfMessagesToProcessAtOnce;
            storage[nameof(this.MaxAttemptsBeforeDelete)] = Int32Boxes.Box(this.MaxAttemptsBeforeDelete);
            storage[nameof(this.RetryIntervalMinutes)] = Int32Boxes.Box(this.RetryIntervalMinutes);
            storage[nameof(this.MaxFilesSizeEmail)] = this.MaxFilesSizeEmail;
            storage[nameof(this.MaxNumberWorkingProcesses)] = Int32Boxes.Box(this.MaxNumberWorkingProcesses);
        }

        /// <inheritdoc/>
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);

            this.Mode = storage.TryGet<string>(nameof(this.Mode));

            this.SmtpFrom = storage.TryGet<string>(nameof(this.SmtpFrom));
            this.SmtpFromDisplayName = storage.TryGet<string>(nameof(this.SmtpFromDisplayName));
            this.SmtpUserName = storage.TryGet<string>(nameof(this.SmtpUserName));
            this.SmtpHost = storage.TryGet<string>(nameof(this.SmtpHost));
            this.SmtpPort = storage.TryGet<int>(nameof(this.SmtpPort));
            this.SmtpEnableSsl = storage.TryGet<bool>(nameof(this.SmtpEnableSsl));
            this.SmtpDefaultCredentials = storage.TryGet<bool>(nameof(this.SmtpDefaultCredentials));
            this.SmtpPassword = storage.TryGet<string>(nameof(this.SmtpPassword));
            this.SmtpClientDomain = storage.TryGet<string>(nameof(this.SmtpClientDomain));
            this.SmtpTimeout = storage.TryGet<int>(nameof(this.SmtpTimeout));
            this.SmtpPickupDirectoryLocation = storage.TryGet<string>(nameof(this.SmtpPickupDirectoryLocation));

            this.ExchangeOAuthToken = storage.TryGet<string>(nameof(this.ExchangeOAuthToken));
            this.ExchangeUser = storage.TryGet<string>(nameof(this.ExchangeUser));
            this.ExchangePassword = storage.TryGet<string>(nameof(this.ExchangePassword));
            this.ExchangeServer = storage.TryGet<string>(nameof(this.ExchangeServer));
            this.ExchangeProxyAddress = storage.TryGet<string>(nameof(this.ExchangeProxyAddress));
            this.ExchangeProxyUser = storage.TryGet<string>(nameof(this.ExchangeProxyUser));
            this.ExchangeProxyPassword = storage.TryGet<string>(nameof(this.ExchangeProxyPassword));
            this.ExchangeVersion = storage.TryGet<string>(nameof(this.ExchangeVersion)) is { } exchangeVersionString
                && Enum.TryParse(exchangeVersionString, out ExchangeVersion exchangeVersion)
                ? exchangeVersion
                : null;
            this.ExchangeFrom = storage.TryGet<string>(nameof(this.ExchangeFrom));
            this.ExchangeFromDisplayName = storage.TryGet<string>(nameof(this.ExchangeFromDisplayName));

            this.NumberOfMessagesToProcessAtOnce = storage.TryGet<int>(nameof(this.NumberOfMessagesToProcessAtOnce));
            this.MaxAttemptsBeforeDelete = storage.TryGet<int>(nameof(this.MaxAttemptsBeforeDelete));
            this.RetryIntervalMinutes = storage.TryGet<int>(nameof(this.RetryIntervalMinutes));
            this.MaxFilesSizeEmail = storage.TryGet<long>(nameof(this.MaxFilesSizeEmail));
            this.MaxNumberWorkingProcesses = storage.TryGet<int>(nameof(this.MaxNumberWorkingProcesses));
        }

        #endregion
    }
}
