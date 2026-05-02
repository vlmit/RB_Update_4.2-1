#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Notices;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Configuration;
using Tessa.Platform.Data;
using Tessa.Platform.Plugins;
using Tessa.Platform.Storage;
using Tessa.Roles;
using Tessa.Scheme;

namespace Tessa.Extensions.Default.Server.Plugins.Notices
{
    /// <summary>
    /// Обработчик плагина <see cref="DefaultPluginNames.TasksNotificationsPlugin"/>.
    /// </summary>
    public sealed class TasksNotificationsPluginHandler : IPluginHandler
    {
        #region Fields

        private readonly IDbScope dbScope;
        private readonly INotificationManager notificationManager;
        private readonly IConfigurationManager configurationManager;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="notificationManager"><inheritdoc cref="INotificationManager" path="/summary"/></param>
        /// <param name="configurationManager"><inheritdoc cref="IConfigurationManager" path="/summary"/></param>
        public TasksNotificationsPluginHandler(
            IDbScope dbScope,
            INotificationManager notificationManager,
            IConfigurationManager configurationManager)
        {
            this.dbScope = NotNullOrThrow(dbScope);
            this.notificationManager = NotNullOrThrow(notificationManager);
            this.configurationManager = NotNullOrThrow(configurationManager);
        }

        #endregion

        #region IPluginHandler Implementation

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(IPluginExecutingContext context)
        {
            ThrowIfNull(context);
            ThrowIfTypeIsNot<TasksNotificationsPluginSettings>(context.Settings);
            var settings = (TasksNotificationsPluginSettings) context.Settings;

            logger.Trace("Starting task notifications plugin.");

            var maxTasksPerUserNotification = (int) settings.MaxTasksPerUserNotification;
            var nowDateTime = DateTime.UtcNow;

            await using (this.dbScope.Create())
            {
                var db = this.dbScope.Db;
                var builderFactory = this.dbScope.BuilderFactory;

                string? normalizedWebAddress = await GetNormalizedWebAddressAsync(db, builderFactory, context.CancellationToken);

                logger.Trace("Getting notifications.");

                // Получим признак возможности включения заместителей в выборку для исполнителей
                var isTakeDeputies =
                    await db.SetCommand(
                            builderFactory
                                .Select()
                                .C("fr", "CanBeDeputy")
                                .From("FunctionRoles", "fr").NoLock()
                                .Where()
                                .C("fr", "ID").Equals().P("TaskRoleID")
                                .Build(),
                            db.Parameter("TaskRoleID", CardFunctionRoles.PerformerID))
                        .LogCommand()
                        .ExecuteAsync<bool>(context.CancellationToken);

                // Список упорядочен по пользователям
                IList<ITaskNotificationInfo> notificationList =
                    await this.GetNotificationsInfoAsync(db, builderFactory, normalizedWebAddress, maxTasksPerUserNotification, isTakeDeputies, nowDateTime, context.CancellationToken);
                if (notificationList.Count == 0)
                {
                    return;
                }

                logger.Trace("Processing notifications.");

                // Получаем вычисленную локаль - в порядке приоритета - локаль пользователя, локаль типового решения, англ

                // Берем первого пользователя
                UserNotification currentUserNotification =
                    new(notificationList[0].UserID, normalizedWebAddress);
                int rowsInCurrentNotification = 0;
                var idsToRemove = new List<(Guid TaskID, Guid UserID)>();

                foreach (ITaskNotificationInfo notification in notificationList)
                {
                    // Тут мы видим, что закончился наш пользователь и должны перейти к следующему
                    if (currentUserNotification.UserID != notification.UserID)
                    {
                        if (rowsInCurrentNotification > maxTasksPerUserNotification)
                        {
                            await GetRemainingTasksCountAsync(db, builderFactory, isTakeDeputies, nowDateTime, currentUserNotification, context.CancellationToken);
                        }
                        else
                        {
                            currentUserNotification.OutdatedTasksCount = currentUserNotification.OutdatedTasks.Count;
                            currentUserNotification.OutdatedTasksInProgressCount = currentUserNotification.OutdatedTasksInProgress.Count;
                            currentUserNotification.TasksCount = currentUserNotification.Tasks.Count;
                            currentUserNotification.TasksInProgressCount = currentUserNotification.TasksInProgress.Count;
                        }

                        var sendResult = await this.notificationManager.SendAsync(
                            DefaultNotifications.TasksNotification,
                            [currentUserNotification.UserID],
                            new NotificationSendContext
                            {
                                ExcludeDeputies = true,
                                MainCardID = currentUserNotification.UserID,
                                Info = currentUserNotification.GetInfo(),
                            },
                            context.CancellationToken);

                        logger.LogResult(sendResult);
                        if (sendResult.IsSuccessful)
                        {
                            await DropAutoApprovedTaskRowsForUserAsync(db, builderFactory, idsToRemove, context.CancellationToken);
                        }

                        // Начинаем обработку следующего пользователя
                        currentUserNotification =
                            new UserNotification(
                                notification.UserID,
                                normalizedWebAddress);

                        // Обрабатываем первую запись
                        idsToRemove.Clear();
                        ProceedUserNotificationRow(notification, currentUserNotification, ref idsToRemove);
                        rowsInCurrentNotification = 1;
                        currentUserNotification.OutdatedTasksCount = 0;
                        currentUserNotification.OutdatedTasksInProgressCount = 0;
                        currentUserNotification.TasksCount = 0;
                        currentUserNotification.TasksInProgressCount = 0;
                    }
                    // Пользователь еще не законченный - обрабатываем его дальше
                    else
                    {
                        if (rowsInCurrentNotification >= maxTasksPerUserNotification)
                        {
                            ProceedUserNotificationRow(notification, currentUserNotification, ref idsToRemove, true);
                            rowsInCurrentNotification++;
                            continue;
                        }
                        ProceedUserNotificationRow(notification, currentUserNotification, ref idsToRemove);
                        rowsInCurrentNotification++;
                    }
                }

                if (rowsInCurrentNotification > maxTasksPerUserNotification)
                {
                    await GetRemainingTasksCountAsync(db, builderFactory, isTakeDeputies, nowDateTime, currentUserNotification, context.CancellationToken);
                }
                else
                {
                    currentUserNotification.OutdatedTasksCount = currentUserNotification.OutdatedTasks.Count;
                    currentUserNotification.OutdatedTasksInProgressCount = currentUserNotification.OutdatedTasksInProgress.Count;
                    currentUserNotification.TasksCount = currentUserNotification.Tasks.Count;
                    currentUserNotification.TasksInProgressCount = currentUserNotification.TasksInProgress.Count;
                }

                var lastSendResult = await this.notificationManager.SendAsync(
                    DefaultNotifications.TasksNotification,
                    [currentUserNotification.UserID],
                    new NotificationSendContext
                    {
                        ExcludeDeputies = true,
                        MainCardID = currentUserNotification.UserID,
                        Info = currentUserNotification.GetInfo(),
                    },
                    context.CancellationToken);

                logger.LogResult(lastSendResult);
                if (lastSendResult.IsSuccessful)
                {
                    await DropAutoApprovedTaskRowsForUserAsync(db, builderFactory, idsToRemove, context.CancellationToken);
                }
            }

            logger.Trace("Notifications were successfully processed.");
        }

        /// <inheritdoc/>
        public IPluginSettings ResolveSettings(Dictionary<string, object?>? info = null)
        {
            var settings = new TasksNotificationsPluginSettings(DefaultPluginNames.TasksNotificationsPlugin);
            if (info is not null)
            {
                settings.Deserialize(info);
            }

            if (this.configurationManager.Configuration.Settings.TryGet<long?>("TaskNotifications.MaxTasksPerUserNotification") is { } maxTasksPerUserNotification)
            {
                settings.MaxTasksPerUserNotification = maxTasksPerUserNotification;
            }

            return settings;
        }

        #endregion

        #region Private Methods

        private static async Task<string?> GetNormalizedWebAddressAsync(
            DbManager db,
            IQueryBuilderFactory builderFactory,
            CancellationToken cancellationToken = default)
        {
            string? webAddress = await db
                .SetCommand(
                    builderFactory
                        .Select().Top(1).C("WebAddress")
                        .From("ServerInstances").NoLock()
                        .Limit(1)
                        .Build())
                .LogCommand()
                .ExecuteAsync<string>(cancellationToken);

            return LinkHelper.NormalizeWebAddress(webAddress);
        }

        private static async Task GetRemainingTasksCountAsync(
            DbManager db,
            IQueryBuilderFactory builderFactory,
            bool isTakeDeputies,
            DateTime nowDateTime,
            UserNotification currentUserNotification,
            CancellationToken cancellationToken = default)
        {
            await using var reader = await db.SetCommand(
                    builderFactory
                        .Select()
                            .If(Dbms.SqlServer, v => v.CastAs(OutdatedTasksCountExpression, SchemeDbType.Int64))
                            .ElseIf(Dbms.PostgreSql, OutdatedTasksCountExpression)
                            .ElseThrow()
                                .As("OutdatedTasksCount")
                            .If(Dbms.SqlServer, v => v.CastAs(OutdatedTasksInProgressCountExpression, SchemeDbType.Int64))
                            .ElseIf(Dbms.PostgreSql, OutdatedTasksInProgressCountExpression)
                            .ElseThrow()
                                .As("OutdatedTasksInProgressCount")
                            .If(Dbms.SqlServer, v => v.CastAs(TasksCountExpression, SchemeDbType.Int64))
                            .ElseIf(Dbms.PostgreSql, TasksCountExpression)
                            .ElseThrow()
                                .As("TasksCount")
                            .If(Dbms.SqlServer, v => v.CastAs(TasksInProgressCountExpression, SchemeDbType.Int64))
                            .ElseIf(Dbms.PostgreSql, TasksInProgressCountExpression)
                            .ElseThrow()
                                .As("TasksInProgressCount")
                        .From("PersonalRoles", "pro").NoLock()
                        .LeftJoinLateral(tin => tin
                                .SelectDistinct()
                                    .C("tsk", "ID", "RowID")
                                    .C("rus", "UserID", "UserName")
                                .From("Tasks", "tsk").NoLock()
                                .LeftJoin("TaskAssignedRoles", "tar").NoLock()
                                    .On().C("tar", "ID").Equals().C("tsk", "RowID")
                                    .And().C("tar", "TaskRoleID").Equals().V(CardFunctionRoles.PerformerID)
                                .InnerJoin(RoleStrings.RoleUsers, "rus").NoLock()
                                    .On().C("rus", "ID").Equals().C("tar", "RoleID")
                                .Where()
                                    .C("pro", "ID").Equals().C("rus", "UserID")
                                    .If(!isTakeDeputies, q => q.And().C("rus", "IsDeputy").NotEquals().V(true))
                                    .And().E(e =>
                                        e.C("tsk", "UserID").IsNull()
                                            .Or().C("tsk", "UserID").Equals().C("pro", "ID")),
                            "tin")
                        .InnerJoin("Tasks", "ts").NoLock()
                            .On().C("ts", "RowID").Equals().C("tin", "RowID")
                        .Where()
                            .C("pro", "ID").Equals().P("UserID")
                        .Build(),
                    db.Parameter("CurrentDate", nowDateTime),
                    db.Parameter("InProgressStateID", (int) CardTaskState.InProgress),
                    db.Parameter("UserID", currentUserNotification.UserID))
                .LogCommand()
                .ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                currentUserNotification.OutdatedTasksCount = reader.GetInt64(0);
                currentUserNotification.OutdatedTasksInProgressCount = reader.GetInt64(1);
                currentUserNotification.TasksCount = reader.GetInt64(2);
                currentUserNotification.TasksInProgressCount = reader.GetInt64(3);
            }

            return;

            void OutdatedTasksCountExpression(IQueryBuilder builder) =>
                builder
                    .Sum(q => q
                        .Case(q1 => q1.When(
                                q2 => q2
                                    .C("ts", "Planned").LessOrEquals().P("CurrentDate")
                                    .And().C("ts", "StateID").NotEquals().P("InProgressStateID"),
                                q2 => q2.V(1))
                            .Else(q2 => q2.V(0))));

            void OutdatedTasksInProgressCountExpression(IQueryBuilder builder) =>
                builder
                    .Sum(q => q
                        .Case(q1 => q1.When(
                                q2 => q2
                                    .C("ts", "Planned").LessOrEquals().P("CurrentDate")
                                    .And().C("ts", "StateID").Equals().P("InProgressStateID"),
                                q2 => q2.V(1))
                            .Else(q2 => q2.V(0))));

            void TasksCountExpression(IQueryBuilder builder) =>
                builder
                    .Sum(q => q
                        .Case(q1 => q1.When(
                                q2 => q2
                                    .C("ts", "Planned").Greater().P("CurrentDate")
                                    .And().C("ts", "StateID").NotEquals().P("InProgressStateID"),
                                q2 => q2.V(1))
                            .Else(q2 => q2.V(0))));

            void TasksInProgressCountExpression(IQueryBuilder builder) =>
                builder
                    .Sum(q => q
                        .Case(q1 => q1.When(
                                q2 => q2
                                    .C("ts", "Planned").Greater().P("CurrentDate")
                                    .And().C("ts", "StateID").Equals().P("InProgressStateID"),
                                q2 => q2.V(1))
                            .Else(q2 => q2.V(0))));
        }

        private async Task<IList<ITaskNotificationInfo>> GetNotificationsInfoAsync(
            DbManager db,
            IQueryBuilderFactory builderFactory,
            string? normalizedWebAddress,
            int maxResultsPerUser,
            bool isTakeDeputies,
            DateTime nowDateTime,
            CancellationToken cancellationToken = default)
        {
            var result = new List<ITaskNotificationInfo>();

            // Обработка обычных заданий
            await using (var reader = await db.SetCommand(
                             builderFactory
                                 .Select()
                                 .C("ts", "ID") // 0
                                 .C("t", "FullNumber", "Subject", "UserID", "UserName")   // 1-4
                                 .C("ts", "StateID", "Created", "Planned", "Digest") // 5-8
                                 .C("aut", "AuthorName") // 9
                                 .C("ts", "TypeID") // 10
                                 .Coalesce(b => b
                                     .C("tco", "KindCaption").C("ts", "TypeCaption")) // 11
                                 .C("ts", "RowID") // 12
                                 .C("t", "UseApproving", "UseAutoApprove") // 13-14
                                 .Case(b => b
                                     .When(
                                         b1 => b1.C("t", "UseAutoApprove").Equals().V(true),
                                         b1 => b1
                                            .If(Dbms.SqlServer,
                                                v => v.Q(" DATEADD(minute, ").Substract().C("ts", "TimeZoneUtcOffsetMinutes").Q(", ")
                                                        .Function("CalendarAddWorkingDaysToDate", b2 => b2
                                                            .Q(" DATEADD(minute, ").C("ts", "TimeZoneUtcOffsetMinutes").Q(", ")
                                                            .P("CurrentDate").Q(")")
                                                            .RequireComma()
                                                            .C("t", "NotifyBefore")
                                                            .C("cs", "CalendarID"))
                                                        .Q(")"))

                                            .ElseIf(Dbms.PostgreSql,
                                                v => v.Q("(")
                                                    .Function("CalendarAddWorkingDaysToDate", b2 => b2
                                                        .Q("(").P("CurrentDate").Add().C("ts", "TimeZoneUtcOffsetMinutes")
                                                        .Q(" * interval '1 minute')")
                                                        .RequireComma()
                                                        .C("t", "NotifyBefore")
                                                        .C("cs", "CalendarID"))
                                                    .Substract().C("ts", "TimeZoneUtcOffsetMinutes")
                                                    .Q(" * interval '1 minute')"))
                                            .ElseThrow())
                                     .Else(b1 => b1.V(null))).As("NotifyBeforePlanned") // 15
                                 .Case(b => b
                                     .When(
                                         b1 => b1.C("t", "UseAutoApprove").Equals().V(true),
                                         b1 => b1
                                            .If(Dbms.SqlServer,
                                                v => v.Q(" DATEADD(minute, ").Substract().C("ts", "TimeZoneUtcOffsetMinutes").Q(", ")
                                                        .Function("CalendarAddWorkingDaysToDate", b2 => b2
                                                            .Q(" DATEADD(minute, ").C("ts", "TimeZoneUtcOffsetMinutes").Q(", ")
                                                            .C("ts", "Planned").Q(")")
                                                            .RequireComma()
                                                            .C("t", "ExceededDays")
                                                            .C("cs", "CalendarID"))
                                                        .Q(")"))

                                            .ElseIf(Dbms.PostgreSql,
                                                v => v.Q("(")
                                                    .Function("CalendarAddWorkingDaysToDate", b2 => b2
                                                        .Q("(").C("ts", "Planned").Add().C("ts", "TimeZoneUtcOffsetMinutes")
                                                        .Q(" * interval '1 minute')")
                                                        .RequireComma()
                                                        .C("t", "ExceededDays")
                                                        .C("cs", "CalendarID"))
                                                    .Substract().C("ts", "TimeZoneUtcOffsetMinutes")
                                                    .Q(" * interval '1 minute')"))
                                            .ElseThrow())
                                     .Else(b1 => b1.V(null))).As("AutoApprovePlanned") // 16
                                 .C("ts", "Settings") // 17
                                 .From(t => t
                                     .Select()
                                     .C("tin", "RowID")
                                     .C("dco", "FullNumber", "Subject")
                                     .C("sct", "UseDocTypes")
                                     .C("pro", "ID").As("UserID").C("pro", "Name").As("UserName")
                                     .Case(b => b
                                         .When(
                                             b1 => b1.C("sct", "UseDocTypes").Equals().V(true),
                                             b1 => b1.C("kdt", "UseApproving"))
                                         .Else(b1 => b1.C("sct", "UseApproving"))).As("UseApproving")
                                     .Case(b => b
                                         .When(
                                             b1 => b1.C("sct", "UseDocTypes").Equals().V(true),
                                             b1 => b1.C("kdt", "UseAutoApprove"))
                                         .Else(b1 => b1.C("sct", "UseAutoApprove"))).As("UseAutoApprove")
                                     .Case(b => b
                                         .When(
                                             b1 => b1.C("sct", "UseDocTypes").Equals().V(true),
                                             b1 => b1.C("kdt", "NotifyBefore"))
                                         .Else(b1 => b1.C("sct", "NotifyBefore"))).As("NotifyBefore")
                                     .Case(b => b
                                         .When(
                                             b1 => b1.C("sct", "UseDocTypes").Equals().V(true),
                                             b1 => b1.C("kdt", "ExceededDays"))
                                         .Else(b1 => b1.C("sct", "ExceededDays"))).As("ExceededDays")
                                     .From("PersonalRoles", "pro").NoLock()
                                     .LeftJoinLateral(tin => tin
                                         .SelectDistinct().Top(maxResultsPerUser + 1)
                                            .C("tsk", "ID", "RowID")
                                            .C("rus", "UserID", "UserName")
                                            .C("tsk", "Created")
                                         .From("Tasks", "tsk").NoLock()
                                         .LeftJoin("TaskAssignedRoles", "tar").NoLock()
                                            .On().C("tar", "ID").Equals().C("tsk", "RowID")
                                            .And().C("tar", "TaskRoleID").Equals().V(CardFunctionRoles.PerformerID)
                                         .InnerJoin(RoleStrings.RoleUsers, "rus").NoLock()
                                            .On().C("rus", "ID").Equals().C("tar", "RoleID")
                                         .Where()
                                            .C("pro", "ID").Equals().C("rus", "UserID")
                                            .If(!isTakeDeputies, q => q.And().C("rus", "IsDeputy").NotEquals().V(true))
                                            .And().E(e =>
                                                e.C("tsk", "UserID").IsNull()
                                                    .Or().C("tsk", "UserID").Equals().C("pro", "ID"))
                                         .OrderBy("tsk", "Created")
                                         .Limit(maxResultsPerUser + 1),
                                         "tin")
                                     .InnerJoin(Names.Instances, "ins").NoLock()
                                        .On().C("ins", "ID").Equals().C("tin", "ID")
                                     .InnerJoin("KrSettingsCardTypes", "sct").NoLock()
                                        .On().C("sct", "CardTypeID").Equals().C("ins", "TypeID")
                                     .LeftJoin("DocumentCommonInfo", "dco").NoLock()
                                        .On().C("dco", "ID").Equals().C("tin", "ID")
                                     .LeftJoin("KrDocType", "kdt").NoLock()
                                        .On().C("kdt", "ID").Equals().C("dco", "DocTypeID")
                                     .Where()
                                        .C("pro", "Email").IsNotNull()
                                        .And().C("pro", "Email").NotEquals().V(string.Empty),
                                     "t")
                                 .InnerJoin("Tasks", "ts").NoLock()
                                    .On().C("ts", "RowID").Equals().C("t", "RowID")
                                 .InnerJoin("CalendarSettings", "cs").NoLock()
                                    .On().C("cs", "ID").Equals().C("ts", "CalendarID")
                                 .LeftJoin("TaskCommonInfo", "tco").NoLock()
                                    .On().C("tco", "ID").Equals().C("ts", "RowID")
                                 .LeftJoinLateral(aut => aut
                                    .Select().Top(1)
                                        .C("tari", "RoleName").As("AuthorName")
                                    .From("TaskAssignedRoles", "tari").NoLock()
                                    .Where()
                                        .C("tari", "ID").Equals().C("ts", "RowID")
                                        .And().C("tari", "TaskRoleID").Equals().V(CardFunctionRoles.AuthorID)
                                    .OrderBy("tari", "RoleID", SortOrder.Ascending)
                                    .Limit(1),
                                     "aut")
                                 .OrderBy("t", "UserID").By("ts", "StateID").By("ts", "Planned")
                                 .Build(),
                             db.Parameter("CurrentDate", nowDateTime))
                         .LogCommand()
                         .WithoutTimeout()
                         .ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var info = new TaskNotificationInfo
                    {
                        CardID = reader.GetGuid(0),
                        CardNumber = reader.GetNullableString(1),
                        CardSubject = reader.GetNullableString(2),
                        UserID = reader.GetGuid(3),
                        UserName = reader.GetNullableString(4),
                        InProgress = reader.GetInt16(5),
                        Created = reader.GetDateTimeUtc(6),
                        Planned = reader.GetDateTimeUtc(7),
                        TaskInfo = reader.GetNullableString(8),
                        AuthorRole = reader.GetNullableString(9)
                    };

                    // т.к. ниже используется GetSequentialNullableStringAsync
                    var taskTypeID = reader.GetGuid(10);
                    info.TypeCaption = reader.GetNullableString(11);
                    info.TaskID = reader.GetGuid(12);

                    info.LinkText =
                        DefaultNotificationHelper.GetNameForLink(
                            !string.IsNullOrEmpty(info.CardNumber) &&
                            !string.IsNullOrEmpty(info.CardSubject)
                                ? info.CardNumber + ", " + info.CardSubject
                                : null,
                            info.CardNumber,
                            null);
                    info.WebLink = CardHelper.GetWebLink(normalizedWebAddress, info.CardID, normalize: false);
                    
                    var useApproving = reader.GetValue<bool>(13);
                    var useAutoApprove = reader.GetValue<bool>(14);
                    var notifyBeforePlanned = reader.GetNullableDateTimeUtc(15);
                    var autoApprovePlanned = reader.GetNullableDateTimeUtc(16);
                    var taskSettingsJson = await reader.GetSequentialNullableStringAsync(17, db.Dbms, cancellationToken);
                    var taskSettings = (string.IsNullOrEmpty(taskSettingsJson)
                            ? null
                            : StorageHelper.DeserializeFromTypedJson(taskSettingsJson))
                        ?? new Dictionary<string, object?>(StringComparer.Ordinal);

                    if (taskTypeID == DefaultTaskTypes.KrApproveTypeID &&
                        useApproving && useAutoApprove &&
                        notifyBeforePlanned.HasValue &&
                        autoApprovePlanned.HasValue &&
                        notifyBeforePlanned.Value >= autoApprovePlanned.Value &&
                        !taskSettings.TryGet<bool>(WorkflowCommonConstants.IsDisableAutoApproval))
                    {
                        info.AutoApproveString = "{$UI_Tasks_AutoApproveNotice} ";
                        info.AutoApproveDate = autoApprovePlanned.Value;
                    }
                    else
                    {
                        info.AutoApproveString = null;
                    }

                    result.Add(info);
                }
            }

            // Обработка автозавершённых заданий
            await using (var reader = await db
                .SetCommand(
                    builderFactory
                        .Select()
                            .C("aah", "CardID", "CardDigest", "UserID") // 0-2
                            .C("pr", "Name") // 3
                            .C("aah", "Date", "ID", "Comment") // 4-6
                        .From("KrAutoApproveHistory", "aah").NoLock()
                        .InnerJoin(RoleStrings.Roles, "r").NoLock()
                            .On().C("r", "ID").Equals().C("aah", "UserID")
                        .InnerJoin(RoleStrings.PersonalRoles, "pr").NoLock()
                            .On().C("pr", "ID").Equals().C("aah", "UserID")
                        .CrossJoin("KrSettings", "krs").NoLock()
                        .Build())
                .LogCommand()
                .WithoutTimeout()
                .ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var info = new AutoAprovedTaskNotificationInfo
                    {
                        CardID = reader.GetGuid(0),
                        UserID = reader.GetGuid(2),
                        UserName = reader.GetNullableString(3),
                        Date = reader.GetDateTimeUtc(4),
                        ID = reader.GetGuid(5),
                        Comment = reader.GetNullableString(6)
                    };

                    var cardDigest = reader.GetNullableString(1);

                    info.LinkText =
                        !string.IsNullOrEmpty(cardDigest)
                            ? cardDigest
                            : null;
                    info.WebLink = CardHelper.GetWebLink(normalizedWebAddress, info.CardID, normalize: false);

                    result.Add(info);
                }
            }

            return [.. result.OrderBy(p => p.UserID)];
        }

        private static void ProceedUserNotificationRow(
            ITaskNotificationInfo info,
            UserNotification currentUserNotification,
            ref List<(Guid TaskID, Guid UserID)> idsToRemove,
            bool skipNormalInfo = false)
        {
            switch (info)
            {
                case TaskNotificationInfo normalInfo:
                    {
                        if (skipNormalInfo)
                        {
                            break;
                        }

                        if (normalInfo.Planned <= DateTime.UtcNow)
                        {
                            if (normalInfo.InProgress == (int) CardTaskState.InProgress)
                            {
                                currentUserNotification.OutdatedTasksInProgress.Add(normalInfo);
                            }
                            else
                            {
                                currentUserNotification.OutdatedTasks.Add(normalInfo);
                            }
                        }
                        else
                        {
                            if (normalInfo.InProgress == (int) CardTaskState.InProgress)
                            {
                                currentUserNotification.TasksInProgress.Add(normalInfo);
                            }
                            else
                            {
                                currentUserNotification.Tasks.Add(normalInfo);
                            }
                        }

                        break;
                    }
                case AutoAprovedTaskNotificationInfo autoApprovedInfo:
                    idsToRemove.Add((autoApprovedInfo.ID, autoApprovedInfo.UserID));
                    currentUserNotification.AutoApprovedTasks.Add(autoApprovedInfo);
                    break;
            }
        }

        /// <summary>
        /// Удаляет из KrAutoApproveHistory записи по автоматически завершённым заданиям для указанного пользователя.
        /// </summary>
        /// <param name="db"><inheritdoc cref="DbManager" path="/summary"/></param>
        /// <param name="builderFactory"><inheritdoc cref="IQueryBuilderFactory" path="/summary"/></param>
        /// <param name="idsToRemove">Идентификаторы записей, который нужно удалить</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        private static async Task DropAutoApprovedTaskRowsForUserAsync(
            DbManager db,
            IQueryBuilderFactory builderFactory,
            ICollection<(Guid TaskID, Guid UserID)> idsToRemove,
            CancellationToken cancellationToken = default)
        {
            if (idsToRemove.Count == 0)
            {
                return;
            }

            foreach ((Guid taskID, Guid userID) in idsToRemove)
            {
                await db
                    .SetCommand(
                        builderFactory
                            .DeleteFrom("KrAutoApproveHistory")
                            .Where()
                                .C("ID").Equals().P("TaskID")
                                .And()
                            .C("UserID").Equals().P("UserID")
                            .Build(),
                        db.Parameter("TaskID", taskID),
                        db.Parameter("UserID", userID))
                    .LogCommand()
                    .ExecuteNonQueryAsync(cancellationToken);
            }
        }

        #endregion
    }
}
