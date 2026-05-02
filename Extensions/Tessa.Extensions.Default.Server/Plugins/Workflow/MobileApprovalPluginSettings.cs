#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Exchange.WebServices.Data;
using Tessa.Platform;
using Tessa.Platform.Plugins;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Plugins.Workflow
{
    /// <summary>
    /// Настройки плагина <see cref="DefaultPluginNames.MobileApprovalPlugin"/>.
    /// </summary>
    public sealed class MobileApprovalPluginSettings : PluginSettings
    {
        #region Constructors

        /// <inheritdoc cref="PluginSettings(string)"/>
        public MobileApprovalPluginSettings(string name)
            : base(name)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Режим работы мобильного согласования.
        /// </summary>
        public string? Mode { get; set; }

        /// <summary>
        /// Адрес сервера при подключении к Imap или POP3.
        /// </summary>
        public string? Pop3ImapHost { get; set; }

        /// <summary>
        /// Порт при подключении к Imap или POP3.
        /// </summary>
        public int? Pop3ImapPort { get; set; }

        /// <summary>
        /// Имя пользователя при подключении к Imap или POP3.
        /// </summary>
        public string? Pop3ImapUser { get; set; }

        /// <summary>
        /// Пароль пользователя при подключении к Imap или POP3.
        /// </summary>
        public string? Pop3ImapPassword { get; set; }

        /// <summary>
        /// Флаг, определяющий, что при подключении к Imap или POP3 используется SSL.
        /// </summary>
        public bool? Pop3ImapUseSsl { get; set; }

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
        /// Версия Exchange сервера.
        /// </summary>
        public ExchangeVersion? ExchangeVersion { get; set; }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);

            storage[nameof(this.Mode)] = this.Mode;
            storage[nameof(this.Pop3ImapHost)] = this.Pop3ImapHost;
            storage[nameof(this.Pop3ImapPort)] = this.Pop3ImapPort;
            storage[nameof(this.Pop3ImapUser)] = this.Pop3ImapUser;
            storage[nameof(this.Pop3ImapPassword)] = this.Pop3ImapPassword;
            storage[nameof(this.Pop3ImapUseSsl)] = BooleanBoxes.Box(this.Pop3ImapUseSsl);
            storage[nameof(this.ExchangeOAuthToken)] = this.ExchangeOAuthToken;
            storage[nameof(this.ExchangeUser)] = this.ExchangeUser;
            storage[nameof(this.ExchangePassword)] = this.ExchangePassword;
            storage[nameof(this.ExchangeServer)] = this.ExchangeServer;
            storage[nameof(this.ExchangeProxyAddress)] = this.ExchangeProxyAddress;
            storage[nameof(this.ExchangeProxyUser)] = this.ExchangeProxyUser;
            storage[nameof(this.ExchangeProxyPassword)] = this.ExchangeProxyPassword;
            storage[nameof(this.ExchangeVersion)] = this.ExchangeVersion?.ToString();
        }

        /// <inheritdoc/>
        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);

            this.Mode = storage.TryGet<string>(nameof(this.Mode));
            this.Pop3ImapHost = storage.TryGet<string>(nameof(this.Pop3ImapHost));
            this.Pop3ImapPort = storage.TryGet<int?>(nameof(this.Pop3ImapPort));
            this.Pop3ImapUser = storage.TryGet<string>(nameof(this.Pop3ImapUser));
            this.Pop3ImapPassword = storage.TryGet<string>(nameof(this.Pop3ImapPassword));
            this.Pop3ImapUseSsl = storage.TryGet<bool?>(nameof(this.Pop3ImapUseSsl));
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
        }

        #endregion
    }
}
