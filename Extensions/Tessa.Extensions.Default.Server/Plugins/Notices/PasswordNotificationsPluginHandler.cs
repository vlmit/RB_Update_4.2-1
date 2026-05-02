#nullable enable

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using NLog;
using Tessa.Cards.Extensions.Templates;
using Tessa.Extensions.Default.Shared;
using Tessa.Localization;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Formatting;
using Tessa.Platform.Plugins;
using Tessa.Platform.Runtime;
using Tessa.Roles;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <summary>
    /// Обработчик плагина <see cref="DefaultPluginNames.PasswordNotificationsPlugin"/>.
    /// </summary>
    public sealed class PasswordNotificationsPluginHandler : IPluginHandler
    {
        #region Fields

        private readonly IServerSecurityProvider serverSecurityProvider;
        private readonly IDbScope dbScope;
        private readonly INotificationManager notificationManager;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="serverSecurityProvider"><inheritdoc cref="IServerSecurityProvider" path="/summary"/></param>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="notificationManager"><inheritdoc cref="INotificationManager" path="/summary"/></param>
        public PasswordNotificationsPluginHandler(
            IServerSecurityProvider serverSecurityProvider,
            IDbScope dbScope,
            INotificationManager notificationManager)
        {
            this.serverSecurityProvider = NotNullOrThrow(serverSecurityProvider);
            this.dbScope = NotNullOrThrow(dbScope);
            this.notificationManager = NotNullOrThrow(notificationManager);
        }

        #endregion

        #region IPluginHandler Implementation

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(IPluginExecutingContext context)
        {
            logger.Trace("Starting password notifications plugin.");

            IServerSecurityOptions options = await this.serverSecurityProvider
                .GetSecurityOptionsAsync(context.CancellationToken);

            if (!options.PasswordExpirationNotificationTime.HasValue || !options.PasswordExpirationTime.HasValue)
            {
                logger.Trace("Password expiration notifications are disabled in security settings.");
                return;
            }

            // Нижняя граница фильтра.
            // Если пользователь уже заблокирован, то ненужно ничего не отправлять.
            DateTime notificationEndDateTime = DateTime.UtcNow - options.PasswordExpirationTime.Value;

            // Верхняя граница фильтра.
            // Дата/время начала отправки уведомлений об истечении паролей.
            // Если пароль истекает раньше этой даты, то отправляем уведомление.
            DateTime notificationStartDateTime = notificationEndDateTime + options.PasswordExpirationNotificationTime.Value;

            if (context.StopRequested)
            {
                return;
            }

            var loginTypesWithPassword = UserLoginTypes.All
                .Where(x => x.Flags.Has(UserLoginTypeFlags.UseInternalPassword))
                .Select(x => (short) x.ID)
                .ToArray();

            if (loginTypesWithPassword.Length == 0)
            {
                return;
            }

            await using (this.dbScope.Create())
            {
                var db = this.dbScope.Db;
                var builderFactory = this.dbScope.BuilderFactory;

                var recipientList = new List<NotificationRecipient>();

                db
                    .SetCommand(
                        builderFactory
                            .Select()
                            .C("pr", "Email", "ID", "Name")
                            .Coalesce(b => b.C("prs", "LanguageCode").C("krs", "NotificationsDefaultLanguageCode"))
                            .Coalesce(b => b.C("prs", "FormatName").C("krs", "NotificationsDefaultFormatName"))
                            .C("m", "Value")
                            .From(RoleStrings.PersonalRoles, "pr").NoLock()
                            .LeftJoinLateral(b => b
                                    .Select().C("prs", "LanguageCode", "FormatName")
                                    .From(CardSatelliteHelper.SatellitesSectionName, "sat").NoLock()
                                    .InnerJoin(RoleStrings.PersonalRoleSatellite, "prs").NoLock()
                                    .On().C("prs", "ID").Equals().C("sat", "ID")
                                    .Where().C("sat", CardSatelliteHelper.MainCardIDColumn).Equals().C("pr", "ID")
                                    .And().C("sat", CardSatelliteHelper.SatelliteTypeIDColumn).Equals().V(RoleHelper.PersonalRoleSatelliteTypeID),
                                "prs")
                            .LeftJoinLateral(b => b
                                    .Select().V(true).As("Value")
                                    .From("MobileLicenses", "ml").NoLock()
                                    .Where().C("ml", "UserID").Equals().C("pr", "ID"),
                                "m")
                            .CrossJoin("KrSettings", "krs").NoLock()
                            .Where()
                            .C("pr", "PasswordChanged")
                            .Between(b => b.P("EndDateTime"), b => b.P("StartDateTime"))
                            .AppendLine()
                            .And().C("pr", "LoginTypeID").In(loginTypesWithPassword)
                            .And().C("pr", "Email").IsNotNull()
                            .And().C("pr", "Email").NotEquals().V(string.Empty)
                            .Build(),
                        db.Parameter("StartDateTime", notificationStartDateTime, DataType.DateTime),
                        db.Parameter("EndDateTime", notificationEndDateTime, DataType.DateTime))
                    .LogCommand();

                await using (DbDataReader reader = await db.ExecuteReaderAsync(context.CancellationToken))
                {
                    while (await reader.ReadAsync(context.CancellationToken))
                    {
                        string? email = reader.GetNullableString(0);

                        if (string.IsNullOrWhiteSpace(email)
                            || !FormattingHelper.EmailRegex.IsMatch(email))
                        {
                            continue;
                        }

                        Guid userID = reader.GetGuid(1);
                        string userName = reader.GetString(2);

                        string? languageCode = reader.GetNullableString(3);
                        if (string.IsNullOrEmpty(languageCode))
                        {
                            languageCode = LocalizationManager.EnglishLanguageCode;
                        }

                        string? formatName = reader.GetNullableString(4);
                        if (string.IsNullOrEmpty(formatName))
                        {
                            formatName = LocalizationManager.EnglishLanguageCode;
                        }

                        bool hasMobileApproval = !await reader.IsDBNullAsync(5, context.CancellationToken);

                        recipientList.Add(new NotificationRecipient
                        {
                            Email = email,
                            LanguageCode = languageCode,
                            FormatName = formatName,
                            UserID = userID,
                            UserName = userName,
                            HasMobileApproval = hasMobileApproval,
                        });
                    }
                }

                if (recipientList.Count == 0)
                {
                    logger.Trace("There are no users with specified emails to notify about theirs passwords expiration.");
                    return;
                }

                foreach (NotificationRecipient recipient in recipientList)
                {
                    if (context.StopRequested)
                    {
                        return;
                    }

                    logger.Trace(
                        "Sending notification to user \"{0}\", ID={1:B}, Email={2}, Language={3}",
                        recipient.UserName, recipient.UserID, recipient.Email, recipient.LanguageCode);

                    await this.notificationManager.SendUsersAsync(
                        DefaultNotifications.PasswordExpiresID,
                        [recipient],
                        new NotificationSendContext
                        {
                            MainCardID = recipient.UserID,
                        },
                        context.CancellationToken);
                }
            }

            logger.Trace("Notifications were successfully processed.");
        }

        /// <inheritdoc/>
        public IPluginSettings ResolveSettings(Dictionary<string, object?>? info = null)
        {
            var settings = new PluginSettings(DefaultPluginNames.PasswordNotificationsPlugin);
            if (info is not null)
            {
                settings.Deserialize(info);
            }

            return settings;
        }

        #endregion
    }
}
