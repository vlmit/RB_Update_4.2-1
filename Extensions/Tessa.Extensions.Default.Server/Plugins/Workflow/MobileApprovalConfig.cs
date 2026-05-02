using System;
using NLog;
using Tessa.Exchange.WebServices.Data;
using Tessa.Platform.Configuration;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Plugins.Workflow
{
    /// <summary>
    /// Настройки мобильного согласования.
    /// </summary>
    /// <param name="configurationManager"><inheritdoc cref="IConfigurationManager" path="/summary"/></param>
    public sealed class MobileApprovalConfig(IConfigurationManager configurationManager)
    {
        #region Fields

        private readonly IConfigurationManager configurationManager = NotNullOrThrow(configurationManager);

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Constants

        // ReSharper disable InconsistentNaming
        public const string MobileApproval_Mode_PropertyName = "MobileApproval.Mode";
        public const string MobileApproval_Pop3ImapHost_PropertyName = "MobileApproval.Pop3ImapHost";
        public const string MobileApproval_Pop3ImapPort_PropertyName = "MobileApproval.Pop3ImapPort";
        public const string MobileApproval_Pop3ImapUser_PropertyName = "MobileApproval.Pop3ImapUser";
        public const string MobileApproval_Pop3ImapPassword_PropertyName = "MobileApproval.Pop3ImapPassword";
        public const string MobileApproval_Pop3ImapUseSsl_PropertyName = "MobileApproval.Pop3ImapUseSsl";

        public const string MobileApproval_ExchangeOAuthToken_PropertyName = "MobileApproval.ExchangeOAuthToken";
        public const string MobileApproval_ExchangeUser_PropertyName = "MobileApproval.ExchangeUser";
        public const string MobileApproval_ExchangeServer_PropertyName = "MobileApproval.ExchangeServer";
        public const string MobileApproval_ExchangePassword_PropertyName = "MobileApproval.ExchangePassword";
        public const string MobileApproval_ExchangeProxyAddress_PropertyName = "MobileApproval.ExchangeProxyAddress";
        public const string MobileApproval_ExchangeProxyUser_PropertyName = "MobileApproval.ExchangeProxyUser";
        public const string MobileApproval_ExchangeProxyPassword_PropertyName = "MobileApproval.ExchangeProxyPassword";

        public const string MobileApproval_ExchangeVersion_PropertyName = "MobileApproval.ExchangeVersion";
        // ReSharper restore InconsistentNaming

        private const ExchangeVersion DefaultExchangeVersion = Exchange.WebServices.Data.ExchangeVersion.Exchange2010;

        #endregion

        #region Private methods

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

        public string? Mode => this.GetSetting<string>(MobileApproval_Mode_PropertyName);

        public string? Pop3ImapHost => this.GetSetting<string>(MobileApproval_Pop3ImapHost_PropertyName);

        public int? Pop3ImapPort => (int?) this.GetSetting<long?>(MobileApproval_Pop3ImapPort_PropertyName);

        public string? Pop3ImapUser => this.GetSetting<string>(MobileApproval_Pop3ImapUser_PropertyName);

        public string? Pop3ImapPassword => this.GetSetting<string>(MobileApproval_Pop3ImapPassword_PropertyName);

        public bool? Pop3ImapUseSsl => this.GetSetting<bool?>(MobileApproval_Pop3ImapUseSsl_PropertyName);

        public string? ExchangeOAuthToken => this.GetSetting<string>(MobileApproval_ExchangeOAuthToken_PropertyName);

        public string? ExchangeUser => this.GetSetting<string>(MobileApproval_ExchangeUser_PropertyName);

        public string? ExchangePassword => this.GetSetting<string>(MobileApproval_ExchangePassword_PropertyName);

        public string? ExchangeServer => this.GetSetting<string>(MobileApproval_ExchangeServer_PropertyName);

        public string? ExchangeProxyAddress => this.GetSetting<string>(MobileApproval_ExchangeProxyAddress_PropertyName, nullable: true);

        public string? ExchangeProxyUser => this.GetSetting<string>(MobileApproval_ExchangeProxyUser_PropertyName, nullable: true);

        public string? ExchangeProxyPassword => this.GetSetting<string>(MobileApproval_ExchangeProxyPassword_PropertyName, nullable: true);

        public ExchangeVersion? ExchangeVersion
        {
            get
            {
                string? exchangeVersion = this.GetSetting<string>(MobileApproval_ExchangeVersion_PropertyName);
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

        #endregion
    }
}
