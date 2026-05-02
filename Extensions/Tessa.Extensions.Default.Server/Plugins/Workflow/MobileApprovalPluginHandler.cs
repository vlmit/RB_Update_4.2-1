#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using Tessa.Exchange.WebServices.Data;
using Tessa.Extensions.Default.Server.Notices;
using Tessa.Extensions.Default.Server.Workflow;
using Tessa.Platform.Data;
using Tessa.Platform.Licensing;
using Tessa.Platform.Plugins;
using Unity;

namespace Tessa.Extensions.Default.Server.Plugins.Workflow
{
    /// <summary>
    /// Обработчик плагина <see cref="DefaultPluginNames.MobileApprovalPlugin"/>.
    /// </summary>
    public sealed class MobileApprovalPluginHandler : IPluginHandler
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly MobileApprovalConfig mobileApprovalConfig;
        private readonly IDbScope dbScope;
        private readonly ILicenseManager licenseManager;
        private readonly ILicenseValidator licenseValidator;
        private readonly Func<IMailReceiver> exchangeMailReceiverFunc;
        private readonly Func<IMailReceiver> imapMailReceiverFunc;
        private readonly Func<IMailReceiver> pop3MailReceiverFunc;

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="mobileApprovalConfig"><inheritdoc cref="MobileApprovalConfig" path="/summary"/></param>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="licenseManager"><inheritdoc cref="ILicenseManager" path="/summary"/></param>
        /// <param name="licenseValidator"><inheritdoc cref="ILicenseValidator" path="/summary"/></param>
        /// <param name="exchangeMailReceiverFunc">
        /// Функция для получения <see cref="IMailReceiver"/>, зарегистрированного по имени <see cref="MailReceiverNames.ExchangeMailReceiver"/>.
        /// </param>
        /// <param name="imapMailReceiverFunc">
        /// Функция для получения <see cref="IMailReceiver"/>, зарегистрированного по имени <see cref="MailReceiverNames.ImapMailReceiver"/>.
        /// </param>
        /// <param name="pop3MailReceiverFunc">
        /// Функция для получения <see cref="IMailReceiver"/>, зарегистрированного по имени <see cref="MailReceiverNames.Pop3MailReceiver"/>.
        /// </param>
        public MobileApprovalPluginHandler(
            MobileApprovalConfig mobileApprovalConfig,
            IDbScope dbScope,
            ILicenseManager licenseManager,
            ILicenseValidator licenseValidator,
            [Dependency(MailReceiverNames.ExchangeMailReceiver)] Func<IMailReceiver> exchangeMailReceiverFunc,
            [Dependency(MailReceiverNames.ImapMailReceiver)] Func<IMailReceiver> imapMailReceiverFunc,
            [Dependency(MailReceiverNames.Pop3MailReceiver)] Func<IMailReceiver> pop3MailReceiverFunc)
        {
            this.mobileApprovalConfig = NotNullOrThrow(mobileApprovalConfig);
            this.dbScope = NotNullOrThrow(dbScope);
            this.licenseManager = NotNullOrThrow(licenseManager);
            this.licenseValidator = NotNullOrThrow(licenseValidator);
            this.exchangeMailReceiverFunc = NotNullOrThrow(exchangeMailReceiverFunc);
            this.imapMailReceiverFunc = NotNullOrThrow(imapMailReceiverFunc);
            this.pop3MailReceiverFunc = NotNullOrThrow(pop3MailReceiverFunc);
        }

        #endregion

        #region MailingMode Enum

        private enum MailingMode
        {
            Exchange,
            Pop3,
            Imap,
            Disabled,
            Unknown,
        }

        private static MailingMode ParseMailingMode(string? mode) =>
            mode?.ToLowerInvariant() switch
            {
                null => MailingMode.Disabled,
                "" => MailingMode.Disabled,
                "exchange" => MailingMode.Exchange,
                "pop3" => MailingMode.Pop3,
                "imap" => MailingMode.Imap,
                "disabled" => MailingMode.Disabled,
                _ => MailingMode.Unknown
            };

        #endregion

        #region Private Methods

        private static bool ParseSettings(MobileApprovalConfig mobileApprovalConfig, MobileApprovalPluginSettings settings)
        {
            settings.Mode = mobileApprovalConfig.Mode;
            var mode = ParseMailingMode(settings.Mode);

            if (mode == MailingMode.Disabled)
            {
                return false;
            }

            if (mode == MailingMode.Unknown)
            {
                logger.Error(
                    "Invalid mode setting specified in setting {0}.",
                    MobileApprovalConfig.MobileApproval_Mode_PropertyName);

                return false;
            }

            bool invalidSettings = false;

            switch (mode)
            {
                case MailingMode.Exchange:
                    string? exchangeOAuthToken = mobileApprovalConfig.ExchangeOAuthToken;
                    string? exchangeUser = mobileApprovalConfig.ExchangeUser;
                    string? exchangePassword = mobileApprovalConfig.ExchangePassword;
                    string? exchangeServer = mobileApprovalConfig.ExchangeServer;
                    string? exchangeProxyAddress = mobileApprovalConfig.ExchangeProxyAddress;
                    string? exchangeProxyUser = mobileApprovalConfig.ExchangeProxyUser;
                    string? exchangeProxyPassword = mobileApprovalConfig.ExchangeProxyPassword;
                    ExchangeVersion? exchangeVersion = mobileApprovalConfig.ExchangeVersion;

                    if (string.IsNullOrEmpty(exchangeUser))
                    {
                        invalidSettings = true;
                        logger.Error(
                            "Invalid settings specified for connection to Exchange server: {0}.",
                            MobileApprovalConfig.MobileApproval_ExchangeUser_PropertyName);
                    }

                    if (!exchangeVersion.HasValue)
                    {
                        invalidSettings = true;
                        logger.Error(
                            "Invalid settings specified for connection to Exchange server: {0}.",
                            MobileApprovalConfig.MobileApproval_ExchangeVersion_PropertyName);
                    }

                    if (invalidSettings)
                    {
                        return false;
                    }

                    settings.ExchangeOAuthToken = exchangeOAuthToken;
                    settings.ExchangeUser = exchangeUser;
                    settings.ExchangePassword = exchangePassword;
                    settings.ExchangeServer = exchangeServer;
                    settings.ExchangeProxyAddress = exchangeProxyAddress;
                    settings.ExchangeProxyUser = exchangeProxyUser;
                    settings.ExchangeProxyPassword = exchangeProxyPassword;
                    settings.ExchangeVersion = exchangeVersion.GetValueOrDefault();
                    break;

                case MailingMode.Pop3:
                case MailingMode.Imap:
                    var pop3ImapHost = mobileApprovalConfig.Pop3ImapHost;
                    var pop3ImapPort = mobileApprovalConfig.Pop3ImapPort;
                    var pop3ImapUser = mobileApprovalConfig.Pop3ImapUser;
                    var pop3ImapPassword = mobileApprovalConfig.Pop3ImapPassword;
                    var pop3ImapUseSsl = mobileApprovalConfig.Pop3ImapUseSsl;

                    if (string.IsNullOrEmpty(pop3ImapHost))
                    {
                        invalidSettings = true;
                        logger.Error(
                            "Invalid POP3/IMAP settings: {0}.", MobileApprovalConfig.MobileApproval_Pop3ImapHost_PropertyName);
                    }

                    if (!pop3ImapPort.HasValue)
                    {
                        invalidSettings = true;
                        logger.Error(
                            "Invalid POP3/IMAP settings: {0}.", MobileApprovalConfig.MobileApproval_Pop3ImapPort_PropertyName);
                    }

                    if (string.IsNullOrEmpty(pop3ImapUser))
                    {
                        invalidSettings = true;
                        logger.Error(
                            "Invalid POP3/IMAP settings: {0}.", MobileApprovalConfig.MobileApproval_Pop3ImapUser_PropertyName);
                    }

                    if (string.IsNullOrEmpty(pop3ImapPassword))
                    {
                        invalidSettings = true;
                        logger.Error(
                            "Invalid POP3/IMAP settings: {0}.", MobileApprovalConfig.MobileApproval_Pop3ImapPassword_PropertyName);
                    }

                    if (!pop3ImapUseSsl.HasValue)
                    {
                        invalidSettings = true;
                        logger.Error(
                            "Invalid POP3/IMAP settings: {0}.", MobileApprovalConfig.MobileApproval_Pop3ImapUseSsl_PropertyName);
                    }

                    if (invalidSettings)
                    {
                        return false;
                    }

                    settings.Pop3ImapHost = pop3ImapHost;
                    settings.Pop3ImapPort = pop3ImapPort;
                    settings.Pop3ImapUser = pop3ImapUser;
                    settings.Pop3ImapPassword = pop3ImapPassword;
                    settings.Pop3ImapUseSsl = pop3ImapUseSsl;
                    break;

                default:
                    throw ArgumentOutOfRange(mode);
            }

            return true;
        }

        #endregion

        #region IPluginHandler Implementation

        public async ValueTask ExecuteAsync(IPluginExecutingContext context)
        {
            ThrowIfNull(context);
            ThrowIfTypeIsNot<MobileApprovalPluginSettings>(context.Settings);
            var settings = (MobileApprovalPluginSettings) context.Settings;

            var mode = ParseMailingMode(settings.Mode);
            if (mode is MailingMode.Disabled or MailingMode.Unknown)
            {
                logger.Trace("Plugin disabled.");
                return;
            }

            if (context.StopRequested)
            {
                return;
            }

            await using (this.dbScope.Create())
            {
                ILicense license = await this.licenseManager.GetLicenseAsync(context.CancellationToken);

                if (!license.Modules.Contains(LicenseModules.MobileApprovalID))
                {
                    logger.Error("Mobile approval license is not found");
                    return;
                }

                int userCount = await this.licenseValidator.GetMobileLicenseCountAsync(context.CancellationToken);
                int count = license.GetMobileCount();

                if (userCount > count)
                {
                    logger.Error("Mobile approval license limit exceeded. Selected employees: {0}. " +
                        "The total number of licenses: {1}", userCount, count);
                    return;
                }

                if (context.StopRequested)
                {
                    return;
                }

                logger.Trace("Mail processing.");

                IMailReceiver? receiver = mode switch
                {
                    MailingMode.Exchange => this.exchangeMailReceiverFunc(),
                    MailingMode.Pop3 => this.pop3MailReceiverFunc(),
                    MailingMode.Imap => this.imapMailReceiverFunc(),
                    _ => null
                };

                if (receiver is not null)
                {
                    receiver.StopRequestedFunc = () => context.StopRequested;

                    // может имплементить один из интерфейсов, оба или ни одного
                    if (receiver is IPop3ImapSettingsContainer pop3ImapReceiver)
                    {
                        pop3ImapReceiver.Pop3ImapSettings = new Pop3ImapSettings
                        {
                            Host = settings.Pop3ImapHost,
                            Port = settings.Pop3ImapPort,
                            User = settings.Pop3ImapUser,
                            Password = settings.Pop3ImapPassword,
                            UseSsl = settings.Pop3ImapUseSsl
                        };
                    }

                    if (receiver is IExchangeSettingsContainer exchangeReceiver)
                    {
                        exchangeReceiver.ExchangeSettings = new ExchangeSettings
                        {
                            OAuthToken = settings.ExchangeOAuthToken,
                            User = settings.ExchangeUser,
                            Password = settings.ExchangePassword,
                            Server = settings.ExchangeServer,
                            ProxyAddress = string.IsNullOrEmpty(settings.ExchangeProxyAddress) ? null : new Uri(settings.ExchangeProxyAddress),
                            ProxyUser = settings.ExchangeProxyUser,
                            ProxyPassword = settings.ExchangeProxyPassword,
                            Version = settings.ExchangeVersion.GetValueOrDefault()
                        };
                    }

                    await receiver.ReceiveMessagesAsync(context.CancellationToken);
                }
            }
        }

        /// <inheritdoc/>
        public IPluginSettings ResolveSettings(Dictionary<string, object?>? info = null)
        {
            var settings = new MobileApprovalPluginSettings(DefaultPluginNames.MobileApprovalPlugin);
            if (info is not null)
            {
                settings.Deserialize(info);
            }

            if (!ParseSettings(this.mobileApprovalConfig, settings))
            {
                settings.Mode = null;
            }

            return settings;
        }

        #endregion
    }
}
