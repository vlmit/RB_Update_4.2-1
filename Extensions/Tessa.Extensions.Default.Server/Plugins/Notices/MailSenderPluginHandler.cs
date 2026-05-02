#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using Tessa.Exchange.WebServices.Data;
using Tessa.Extensions.Default.Server.Notices;
using Tessa.Platform.Plugins;
using Task = System.Threading.Tasks.Task;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <summary>
    /// Обработчик плагина <see cref="DefaultPluginNames.MailSenderPlugin"/>.
    /// </summary>
    public sealed class MailSenderPluginHandler :
        IPluginHandler
    {
        #region Fields

        private readonly MailSenderConfig mailSenderConfig;
        private readonly IOutboxManager outboxManager;
        private readonly Func<SmtpSender> smtpSenderFunc;
        private readonly Func<ExchangeSender> exchangeSenderFunc;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="mailSenderConfig"><inheritdoc cref="MailSenderConfig" path="/summary"/></param>
        /// <param name="outboxManager"><inheritdoc cref="IOutboxManager" path="/summary"/></param>
        /// <param name="smtpSenderFunc">Функция для получения объекта <see cref="SmtpSender"/>.</param>
        /// <param name="exchangeSenderFunc">Функция для получения объекта <see cref="ExchangeSender"/>.</param>
        public MailSenderPluginHandler(
            MailSenderConfig mailSenderConfig,
            IOutboxManager outboxManager,
            Func<SmtpSender> smtpSenderFunc,
            Func<ExchangeSender> exchangeSenderFunc)
        {
            this.mailSenderConfig = NotNullOrThrow(mailSenderConfig);
            this.outboxManager = NotNullOrThrow(outboxManager);
            this.smtpSenderFunc = NotNullOrThrow(smtpSenderFunc);
            this.exchangeSenderFunc = NotNullOrThrow(exchangeSenderFunc);
        }

        #endregion

        #region MailingMode Enum

        private enum MailingMode
        {
            Exchange,
            Smtp,
            Disabled,
            Unknown,
        }

        private static MailingMode ParseMailingMode(string? mode) =>
            mode?.ToLowerInvariant() switch
            {
                null => MailingMode.Disabled,
                "" => MailingMode.Disabled,
                "exchange" => MailingMode.Exchange,
                "smtp" => MailingMode.Smtp,
                "disabled" => MailingMode.Disabled,
                _ => MailingMode.Unknown
            };

        #endregion

        #region Private methods

        private static bool ParseSettings(MailSenderConfig mailSenderConfig, MailSenderPluginSettings settings)
        {
            settings.Mode = mailSenderConfig.Mode;
            var mode = ParseMailingMode(settings.Mode);

            switch (mode)
            {
                case MailingMode.Disabled:
                    return false;

                case MailingMode.Unknown:
                    logger.Error(
                        "Invalid mail sending mode specified in setting {0}",
                        MailSenderConfig.Mode_PropertyName);
                    return false;
            }

            long? maxFilesSizeEmailTemp = mailSenderConfig.MaxFilesSizeEmail;
            if (maxFilesSizeEmailTemp is null)
            {
                logger.Error(
                    "Invalid max files size specified in setting {0}",
                    MailSenderConfig.MaxFilesSizeEmail_PropertyName);

                return false;
            }

            settings.MaxFilesSizeEmail = maxFilesSizeEmailTemp.Value;

            int? maxNumberWorkingProcessesTemp = mailSenderConfig.MaxNumberWorkingProcesses;
            if (maxNumberWorkingProcessesTemp is null)
            {
                logger.Error(
                    "Invalid max number working processes specified in setting {0}",
                    MailSenderConfig.MaxNumberWorkingProcesses_PropertyName);

                return false;
            }

            settings.MaxNumberWorkingProcesses = maxNumberWorkingProcessesTemp.Value;

            bool isInvalidSettings = false;
            switch (mode)
            {
                case MailingMode.Exchange:
                    settings.ExchangeOAuthToken = mailSenderConfig.ExchangeOAuthToken;
                    settings.ExchangeUser = mailSenderConfig.ExchangeUser;
                    settings.ExchangePassword = mailSenderConfig.ExchangePassword;
                    settings.ExchangeServer = mailSenderConfig.ExchangeServer;
                    settings.ExchangeProxyAddress = mailSenderConfig.ExchangeProxyAddress;
                    settings.ExchangeProxyUser = mailSenderConfig.ExchangeProxyUser;
                    settings.ExchangeProxyPassword = mailSenderConfig.ExchangeProxyPassword;
                    settings.ExchangeFrom = mailSenderConfig.ExchangeFrom;
                    settings.ExchangeFromDisplayName = mailSenderConfig.ExchangeFromDisplayName;
                    ExchangeVersion? exchangeVersionTemp = mailSenderConfig.ExchangeVersion;

                    if (string.IsNullOrEmpty(settings.ExchangeUser))
                    {
                        isInvalidSettings = true;
                        logger.Error(
                            "Invalid settings specified for connection to Exchange server: {0}",
                            MailSenderConfig.ExchangeUser_PropertyName);
                    }

                    if (exchangeVersionTemp is null)
                    {
                        isInvalidSettings = true;
                        logger.Error(
                            "Invalid settings specified for connection to Exchange server: {0}",
                            MailSenderConfig.ExchangeVersion_PropertyName);
                    }

                    if (isInvalidSettings)
                    {
                        return false;
                    }

                    settings.ExchangeVersion = exchangeVersionTemp.GetValueOrDefault();
                    break;

                case MailingMode.Smtp:
                    settings.SmtpFrom = mailSenderConfig.SmtpFrom;
                    settings.SmtpUserName = mailSenderConfig.SmtpUserName;
                    if (string.IsNullOrEmpty(settings.SmtpFrom) && string.IsNullOrEmpty(settings.SmtpUserName))
                    {
                        logger.Error(
                            "Invalid address used to send mail using SMTP in settings {0} and {1}",
                            MailSenderConfig.SmtpFrom_PropertyName,
                            MailSenderConfig.SmtpUserName_PropertyName);

                        return false;
                    }

                    settings.SmtpFromDisplayName = mailSenderConfig.SmtpFromDisplayName; // может быть null или пустой строкой
                    settings.SmtpHost = mailSenderConfig.SmtpHost;
                    settings.SmtpPort = mailSenderConfig.SmtpPort;
                    settings.SmtpEnableSsl = mailSenderConfig.SmtpEnableSsl;
                    settings.SmtpDefaultCredentials = mailSenderConfig.SmtpDefaultCredentials;
                    settings.SmtpPassword = mailSenderConfig.SmtpPassword;
                    settings.SmtpClientDomain = mailSenderConfig.SmtpClientDomain;
                    settings.SmtpTimeout = mailSenderConfig.SmtpTimeout;
                    settings.SmtpPickupDirectoryLocation = mailSenderConfig.SmtpPickupDirectoryLocation;
                    break;

                default:
                    throw ArgumentOutOfRange(mode);
            }

            int? numberOfMessagesToProcessAtOnceTmp = mailSenderConfig.NumberOfMessagesToProcessAtOnce;
            if (numberOfMessagesToProcessAtOnceTmp is null)
            {
                logger.Error(
                    "Invalid number of messages to process at once in setting {0}",
                    MailSenderConfig.NumberOfMessagesToProcessAtOnce_PropertyName);

                return false;
            }

            settings.NumberOfMessagesToProcessAtOnce = numberOfMessagesToProcessAtOnceTmp.Value;

            var maxAttemptsBeforeDeleteTmp = mailSenderConfig.MaxAttemptsBeforeDelete;
            if (maxAttemptsBeforeDeleteTmp is null)
            {
                logger.Error(
                    "Invalid attempt count before message is deleted in setting {0}",
                    MailSenderConfig.MaxAttemptsBeforeDelete_PropertyName);

                return false;
            }

            settings.MaxAttemptsBeforeDelete = maxAttemptsBeforeDeleteTmp.Value;

            var retryIntervalMinutesTmp = mailSenderConfig.RetryIntervalMinutes;
            if (retryIntervalMinutesTmp is null)
            {
                logger.Error(
                    "Invalid interval before failed message is sent again in setting {0}",
                    MailSenderConfig.RetryIntervalMinutes_PropertyName);

                return false;
            }

            settings.RetryIntervalMinutes = retryIntervalMinutesTmp.Value;
            return true;
        }

        #endregion

        #region IPluginHandler Implementation

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(IPluginExecutingContext context)
        {
            ThrowIfNull(context);
            ThrowIfTypeIsNot<MailSenderPluginSettings>(context.Settings);
            var settings = (MailSenderPluginSettings) context.Settings;

            var mode = ParseMailingMode(settings.Mode);
            if (mode is MailingMode.Disabled
                or MailingMode.Unknown)
            {
                logger.Trace("Plugin disabled.");
                return;
            }

            // за раз выполняется обработка только части сообщений - думаю нет смысла напрягаться
            logger.Trace("Retrieving list of messages to send.");

            ConcurrentQueue<OutboxMessage> messagesQueue =
                await this.outboxManager.GetTopMessagesAsync(settings.NumberOfMessagesToProcessAtOnce, settings.RetryIntervalMinutes, context.CancellationToken);

            if (context.StopRequested)
            {
                return;
            }

            var disposables = new List<IAsyncDisposable>(settings.MaxNumberWorkingProcesses);
            Task[] tasks = new Task[settings.MaxNumberWorkingProcesses];

            try
            {
                switch (mode)
                {
                    case MailingMode.Exchange:
                        var exchangeSettings = new ExchangeSettings
                        {
                            OAuthToken = settings.ExchangeOAuthToken,
                            User = settings.ExchangeUser,
                            Password = settings.ExchangePassword,
                            Server = settings.ExchangeServer,
                            ProxyAddress =
                                string.IsNullOrWhiteSpace(settings.ExchangeProxyAddress)
                                    ? null
                                    : new Uri(settings.ExchangeProxyAddress),
                            ProxyUser = settings.ExchangeProxyUser,
                            ProxyPassword = settings.ExchangeProxyPassword,
                            Version = settings.ExchangeVersion.GetValueOrDefault(),
                            From = string.IsNullOrWhiteSpace(settings.ExchangeFrom) ? settings.ExchangeUser : settings.ExchangeFrom,
                            FromDisplayName = settings.ExchangeFromDisplayName,
                        };

                        for (int i = 0; i < tasks.Length; i++)
                        {
                            var exchangeSender = this.exchangeSenderFunc();
                            exchangeSender.StopRequestedFunc = () => context.StopRequested;
                            exchangeSender.Settings = exchangeSettings;
                            exchangeSender.MaxFilesSizeEmailKb = settings.MaxFilesSizeEmail;
                            exchangeSender.MaxAttemptsBeforeDelete = settings.MaxAttemptsBeforeDelete;

                            disposables.Add(exchangeSender);

                            tasks[i] = exchangeSender.StartAsync(messagesQueue, context.CancellationToken);
                        }

                        break;

                    case MailingMode.Smtp:

                        var smtpSettings = new SmtpSettings
                        {
                            SmtpFrom = string.IsNullOrWhiteSpace(settings.SmtpFrom) ? settings.SmtpUserName : settings.SmtpFrom,
                            SmtpFromDisplayName = settings.SmtpFromDisplayName,
                            SmtpClientDomain = settings.SmtpClientDomain,
                            SmtpDefaultCredentials = settings.SmtpDefaultCredentials,
                            SmtpEnableSsl = settings.SmtpEnableSsl,
                            SmtpHost = settings.SmtpHost,
                            SmtpPassword = settings.SmtpPassword,
                            SmtpPickupDirectoryLocation = settings.SmtpPickupDirectoryLocation,
                            SmtpPort = settings.SmtpPort,
                            SmtpTimeout = settings.SmtpTimeout,
                            SmtpUserName = settings.SmtpUserName,
                        };

                        for (int i = 0; i < tasks.Length; i++)
                        {
                            var smtpSender = this.smtpSenderFunc();
                            smtpSender.StopRequestedFunc = () => context.StopRequested;
                            smtpSender.Settings = smtpSettings;
                            smtpSender.MaxFilesSizeEmailKb = settings.MaxFilesSizeEmail;
                            smtpSender.MaxAttemptsBeforeDelete = settings.MaxAttemptsBeforeDelete;

                            disposables.Add(smtpSender);

                            tasks[i] = smtpSender.StartAsync(messagesQueue, context.CancellationToken);
                        }

                        break;
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                foreach (IAsyncDisposable disposable in disposables)
                {
                    await disposable.DisposeAsync();
                }
            }
        }

        /// <inheritdoc/>
        public IPluginSettings ResolveSettings(Dictionary<string, object?>? info = null)
        {
            var settings = new MailSenderPluginSettings(DefaultPluginNames.MailSenderPlugin);
            if (info is not null)
            {
                settings.Deserialize(info);
            }

            if (!ParseSettings(this.mailSenderConfig, settings))
            {
                settings.Mode = null;
            }

            return settings;
        }

        #endregion
    }
}
