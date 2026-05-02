#nullable enable

using System;
using System.Globalization;
using NLog;
using Tessa.Exchange.WebServices.Data;
using Tessa.Platform.Configuration;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <summary>
    /// Настройки по отправке почты.
    /// </summary>
    /// <param name="configurationManager"><inheritdoc cref="IConfigurationManager" path="/summary"/></param>
    public sealed class MailSenderConfig(IConfigurationManager configurationManager)
    {
        #region Fields

        private readonly IConfigurationManager configurationManager = NotNullOrThrow(configurationManager);

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Constants

        // ReSharper disable InconsistentNaming
        public const string Mode_PropertyName = "MailSender.Mode";
        public const string ExchangeOAuthToken_PropertyName = "MailSender.ExchangeOAuthToken";
        public const string ExchangeUser_PropertyName = "MailSender.ExchangeUser";
        public const string ExchangeServer_PropertyName = "MailSender.ExchangeServer";
        public const string ExchangePassword_PropertyName = "MailSender.ExchangePassword";
        public const string ExchangeVersion_PropertyName = "MailSender.ExchangeVersion";
        public const string ExchangeProxyAddress_PropertyName = "MailSender.ExchangeProxyAddress";
        public const string ExchangeProxyUser_PropertyName = "MailSender.ExchangeProxyUser";
        public const string ExchangeProxyPassword_PropertyName = "MailSender.ExchangeProxyPassword";
        public const string ExchangeFrom_PropertyName = "MailSender.ExchangeFrom";
        public const string ExchangeFromDisplayName_PropertyName = "MailSender.ExchangeFromDisplayName";
        public const string SmtpPickupDirectoryLocation_PropertyName = "MailSender.SmtpPickupDirectoryLocation";
        public const string SmtpHost_PropertyName = "MailSender.SmtpHost";
        public const string SmtpPort_PropertyName = "MailSender.SmtpPort";
        public const string SmtpEnableSsl_PropertyName = "MailSender.SmtpEnableSsl";
        public const string SmtpDefaultCredentials_PropertyName = "MailSender.SmtpDefaultCredentials";
        public const string SmtpUserName_PropertyName = "MailSender.SmtpUserName";
        public const string SmtpPassword_PropertyName = "MailSender.SmtpPassword";
        public const string SmtpClientDomain_PropertyName = "MailSender.SmtpClientDomain";
        public const string SmtpFrom_PropertyName = "MailSender.SmtpFrom";
        public const string SmtpFromDisplayName_PropertyName = "MailSender.SmtpFromDisplayName";
        public const string SmtpTimeout_PropertyName = "MailSender.SmtpTimeout";
        public const string NumberOfMessagesToProcessAtOnce_PropertyName = "MailSender.NumberOfMessagesToProcessAtOnce";
        public const string MaxAttemptsBeforeDelete_PropertyName = "MailSender.MaxAttemptsBeforeDelete";
        public const string RetryIntervalMinutes_PropertyName = "MailSender.RetryIntervalMinutes";
        public const string MaxFilesSizeEmail_PropertyName = "MailSender.MaxFilesSizeEmail";
        public const string MaxNumberWorkingProcesses_PropertyName = "MailSender.MaxNumberWorkingProcesses";
        public const string SaasAuthTokenExpiration_PropertyName = "MailSender.SaasAuthTokenExpiration";
        
        // ReSharper restore InconsistentNaming

        public const ExchangeVersion DefaultExchangeVersion = Exchange.WebServices.Data.ExchangeVersion.Exchange2010;

        private static readonly TimeSpan defaultAuthTokenExpiration = TimeSpan.FromMinutes(3);
        
        #endregion

        #region Private Methods

        private T? GetSetting<T>(string settingName, bool nullable = false)
        {
            T? attribute = this.configurationManager.Configuration.Settings.TryGet<T>(settingName);

            if (attribute is null && !nullable)
            {
                // для типов, допускающих null
                logger.Warn("Configuration setting \"{0}\" is not found.", settingName);
            }

            return attribute;
        }

        #endregion

        #region Properties

        public string? Mode => this.GetSetting<string>(Mode_PropertyName);

        public string? ExchangeOAuthToken => this.GetSetting<string>(ExchangeOAuthToken_PropertyName);

        public string? ExchangeUser => this.GetSetting<string>(ExchangeUser_PropertyName);

        public string? ExchangePassword => this.GetSetting<string>(ExchangePassword_PropertyName);

        public string? ExchangeServer => this.GetSetting<string>(ExchangeServer_PropertyName);

        public string? ExchangeProxyAddress => this.GetSetting<string>(ExchangeProxyAddress_PropertyName, nullable: true);

        public string? ExchangeProxyUser => this.GetSetting<string>(ExchangeProxyUser_PropertyName, nullable: true);

        public string? ExchangeProxyPassword => this.GetSetting<string>(ExchangeProxyPassword_PropertyName, nullable: true);

        public string? ExchangeFrom => this.GetSetting<string>(ExchangeFrom_PropertyName, nullable: true);

        public string? ExchangeFromDisplayName => this.GetSetting<string>(ExchangeFromDisplayName_PropertyName, nullable: true);

        public ExchangeVersion? ExchangeVersion
        {
            get
            {
                string? exchangeVersion = this.GetSetting<string>(ExchangeVersion_PropertyName);
                if (string.IsNullOrWhiteSpace(exchangeVersion))
                {
                    return DefaultExchangeVersion;
                }

                if (!Enum.TryParse(exchangeVersion, out ExchangeVersion version))
                {
                    return null;
                }

                return version;
            }
        }

        public string? SmtpPickupDirectoryLocation => this.GetSetting<string>(SmtpPickupDirectoryLocation_PropertyName, nullable: true);

        public string? SmtpHost => this.GetSetting<string>(SmtpHost_PropertyName);

        public int SmtpPort => (int) (this.GetSetting<long?>(SmtpPort_PropertyName) ?? 0L);

        public bool SmtpEnableSsl => this.GetSetting<bool>(SmtpEnableSsl_PropertyName);

        public bool SmtpDefaultCredentials => this.GetSetting<bool>(SmtpDefaultCredentials_PropertyName);

        public string? SmtpUserName => this.GetSetting<string>(SmtpUserName_PropertyName);

        public string? SmtpPassword => this.GetSetting<string>(SmtpPassword_PropertyName);

        public string? SmtpClientDomain => this.GetSetting<string>(SmtpClientDomain_PropertyName);

        public string? SmtpFrom => this.GetSetting<string>(SmtpFrom_PropertyName);

        public string? SmtpFromDisplayName => this.GetSetting<string>(SmtpFromDisplayName_PropertyName);

        public int SmtpTimeout => (int) (this.GetSetting<long?>(SmtpTimeout_PropertyName) ?? 0L);

        public int? NumberOfMessagesToProcessAtOnce => (int?) this.GetSetting<long?>(NumberOfMessagesToProcessAtOnce_PropertyName);

        public int? MaxAttemptsBeforeDelete => (int?) this.GetSetting<long?>(MaxAttemptsBeforeDelete_PropertyName);

        public int? RetryIntervalMinutes => (int?) this.GetSetting<long?>(RetryIntervalMinutes_PropertyName);

        public long? MaxFilesSizeEmail => this.GetSetting<long?>(MaxFilesSizeEmail_PropertyName);

        public int? MaxNumberWorkingProcesses => (int?) this.GetSetting<long?>(MaxNumberWorkingProcesses_PropertyName);

        public TimeSpan SaasAuthTokenExpiration
        {
            get
            {
                string? attributeText = this.configurationManager.Configuration.Settings.TryGet<string>(SaasAuthTokenExpiration_PropertyName);
                return !string.IsNullOrEmpty(attributeText)
                    ? TimeSpan.Parse(attributeText, CultureInfo.InvariantCulture)
                    : defaultAuthTokenExpiration;
            }
        }

        #endregion
    }
}
