#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using Tessa.Extensions.Default.Shared;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Plugins;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <summary>
    /// Обработчик плагина <see cref="DefaultPluginNames.TokenNotificationsPlugin"/>.
    /// </summary>
    public sealed class TokenNotificationsPluginHandler : IPluginHandler
    {
        #region Fields

        private readonly IDbScope dbScope;
        private readonly INotificationManager notificationManager;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="notificationManager"><inheritdoc cref="INotificationManager" path="/summary"/></param>
        public TokenNotificationsPluginHandler(
            IDbScope dbScope,
            INotificationManager notificationManager)
        {
            this.dbScope = NotNullOrThrow(dbScope);
            this.notificationManager = NotNullOrThrow(notificationManager);
        }

        #endregion

        #region IPluginHandler Implementation

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(IPluginExecutingContext context)
        {
            ThrowIfNull(context);

            logger.Trace("Starting token notification plugin.");

            logger.Trace("Collecting token notification receivers.");

            await using (this.dbScope.Create())
            {
                List<Guid> users = await SettingNotificationHelper.GetEmailSettingsAsync(this.dbScope, "NotificationTokenRoles", context.CancellationToken);
                if (users.Count > 0)
                {
                    var validationResult = new ValidationResultBuilder();
                    try
                    {
                        validationResult.Add(
                            await this.notificationManager
                                .SendAsync(
                                    DefaultNotifications.TokenNotification,
                                    users,
                                    new NotificationSendContext
                                    {
                                        MainCardID = Session.SystemID,
                                        IgnoreUserSessions = true,
                                        ExcludeDeputies = true,
                                        DisableSubscribers = true,
                                    },
                                    context.CancellationToken));
                    }
                    finally
                    {
                        logger.LogResult(validationResult);
                    }
                }
            }

            logger.Trace("Token notification plugin completed.");
        }

        /// <inheritdoc/>
        public IPluginSettings ResolveSettings(Dictionary<string, object?>? info = null)
        {
            var settings = new PluginSettings(DefaultPluginNames.TokenNotificationsPlugin);
            if (info is not null)
            {
                settings.Deserialize(info);
            }

            return settings;
        }

        #endregion
    }
}
