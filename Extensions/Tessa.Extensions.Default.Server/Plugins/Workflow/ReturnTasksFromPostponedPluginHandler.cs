#nullable enable

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Shared;
using Tessa.Notices;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Plugins;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Plugins.Workflow
{
    /// <summary>
    /// Обработчик плагина <see cref="DefaultPluginNames.ReturnTasksFromPostponedPlugin"/>.
    /// </summary>
    public sealed class ReturnTasksFromPostponedPluginHandler :
        IPluginHandler
    {
        #region Nested Types

        public sealed record TaskRecord(Guid CardID, Guid TaskID);

        #endregion

        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IDbScope dbScope;
        private readonly ICardRepository cardRepository;
        private readonly ICardMetadata cardMetadata;
        private readonly ICardGetStrategy cardGetStrategy;
        private readonly ICardServerPermissionsProvider permissionsProvider;
        private readonly INotificationManager notificationManager;
        private readonly ITessaServerSettings serverSettings;

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
        /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
        /// <param name="cardGetStrategy"><inheritdoc cref="ICardGetStrategy" path="/summary"/></param>
        /// <param name="permissionsProvider"><inheritdoc cref="ICardServerPermissionsProvider" path="/summary"/></param>
        /// <param name="notificationManager"><inheritdoc cref="INotificationManager" path="/summary"/></param>
        /// <param name="serverSettings"><inheritdoc cref="ITessaServerSettings" path="/summary"/></param>
        public ReturnTasksFromPostponedPluginHandler(
            IDbScope dbScope,
            ICardRepository cardRepository,
            ICardMetadata cardMetadata,
            ICardGetStrategy cardGetStrategy,
            ICardServerPermissionsProvider permissionsProvider,
            INotificationManager notificationManager,
            ITessaServerSettings serverSettings)
        {
            this.dbScope = NotNullOrThrow(dbScope);
            this.cardRepository = NotNullOrThrow(cardRepository);
            this.cardMetadata = NotNullOrThrow(cardMetadata);
            this.cardGetStrategy = NotNullOrThrow(cardGetStrategy);
            this.permissionsProvider = NotNullOrThrow(permissionsProvider);
            this.notificationManager = NotNullOrThrow(notificationManager);
            this.serverSettings = NotNullOrThrow(serverSettings);
        }

        #endregion

        #region Private Methods

        private static async Task<List<TaskRecord>> GetTasksToReturnFromPostponedAsync(
            DbManager db,
            IQueryBuilderFactory builderFactory,
            CancellationToken cancellationToken = default)
        {
            db
                .SetCommand(
                    builderFactory
                        .Select()
                        .C("t", "ID", "RowID")
                        .From("Tasks", "t").NoLock()
                        .Where().C("t", "PostponedTo").LessOrEquals().P("UtcNow")
                        .Build(),
                    db.Parameter("UtcNow", DateTime.UtcNow))
                .LogCommand();

            var result = new List<TaskRecord>();

            await using DbDataReader reader = await db.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(
                    new TaskRecord(
                        CardID: reader.GetGuid(0),
                        TaskID: reader.GetGuid(1)));
            }

            return result;
        }

        #endregion

        #region IPluginHandler Implementation

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(IPluginExecutingContext context)
        {
            await using (this.dbScope.Create())
            {
                var db = this.dbScope.Db;
                var builderFactory = this.dbScope.BuilderFactory;

                List<TaskRecord> records = await GetTasksToReturnFromPostponedAsync(db, builderFactory, context.CancellationToken);
                if (records.Count > 0)
                {
                    // инициализируем все зависимости из UnityContainer только после того, как мы нашли задания, возвращаемые из отложенных

                    // загрузка выполняется без расширений через cardGetStrategy,
                    // т.к. они могут неадекватно реагировать на загрузку только заданий без секций
                    // на момент написания плагина неадекватная реакция была у расширений на KrProcess

                    // для каждой карточки, для которой найдены отложенные задания
                    foreach (IGrouping<Guid, TaskRecord> recordsByCardID in records.GroupBy(x => x.CardID))
                    {
                        Guid cardID = recordsByCardID.Key;
                        logger.Trace("Loading tasks from card with identifier '{0}'.", cardID);

                        // загружаем системную информацию по заданиям карточки
                        var validationResult = new ValidationResultBuilder();

                        // ... загрузка выполняется без расширений через cardGetStrategy,
                        // ... т.к. расширения могут неадекватно реагировать на загрузку только заданий без секций;
                        // ... серверные расширения невозможно отключить с клиента, поэтому используем серверное API;
                        // ... на момент написания плагина неадекватная реакция была у расширений на KrProcess
                        CardGetContext? getContext = await this.cardGetStrategy.TryLoadCardInstanceAsync(
                            cardID, db, this.cardMetadata, validationResult, cancellationToken: context.CancellationToken);

                        if (getContext is null)
                        {
                            logger.Warn("Card with identifier '{0}' isn't found.", cardID);
                            continue;
                        }

                        Card card = getContext.Card;
                        IList<CardGetContext>? tasks = await this.cardGetStrategy.TryLoadTaskInstancesAsync(
                            cardID,
                            card,
                            db,
                            this.cardMetadata,
                            validationResult,
                            Session.CreateSystemSession(SessionType.Server, this.serverSettings),
                            getTaskMode: CardGetTaskMode.All,
                            loadCalendarInfo: false,
                            taskRowIDList: recordsByCardID.Select(x => x.TaskID),
                            cancellationToken: context.CancellationToken);

                        if (tasks is null)
                        {
                            logger.Warn("Can't find tasks for card with identifier '{0}'.", cardID);
                            continue;
                        }

                        // если есть ошибки или нет заданий - переходим к следующей карточке
                        ValidationResult getResult = validationResult.Build();
                        logger.LogResult(getResult);

                        if (!getResult.IsSuccessful)
                        {
                            continue;
                        }

                        ListStorage<CardTask>? cardTasks = card.TryGetTasks();
                        // если не было помеченных заданий - переходим к следующей карточке
                        if (cardTasks is null || cardTasks.Count == 0)
                        {
                            continue;
                        }

                        foreach (CardTask task in cardTasks)
                        {
                            task.State = CardRowState.Modified;
                            task.Action = CardTaskAction.Progress;
                        }

                        // иначе выполняем сохранение с возвратом заданий
                        logger.Trace(
                            "Returning {0} task(s) from postponed state for CardID = '{1}'.",
                            cardTasks.Count,
                            cardID);

                        var cardClone = card.Clone();
                        var storeRequest = new CardStoreRequest { Card = cardClone };
                        storeRequest.SetPluginType(CardPluginTypes.ReturnTasksFromPostponed);

                        if (await this.cardGetStrategy.LoadSectionsAsync(getContext, context.CancellationToken))
                        {
                            string? digest = await this.cardRepository.GetDigestAsync(cardClone, CardDigestEventNames.ActionHistoryReturnTasksFromPostponed, context.CancellationToken);
                            if (digest is not null)
                            {
                                storeRequest.SetDigest(digest);
                            }
                        }

                        cardClone.RemoveAllButChanged();

                        this.permissionsProvider.SetFullPermissions(storeRequest);

                        CardStoreResponse storeResponse = await this.cardRepository.StoreAsync(storeRequest, context.CancellationToken);

                        // пишем, завершилась ли операция успехом или нет
                        // в случае успеха отсылаем сообщения на почту пользователям
                        ValidationResult storeResult = storeResponse.ValidationResult.Build();
                        logger.LogResult(storeResult);

                        if (storeResult.IsSuccessful)
                        {
                            logger.Trace(
                                "{0} task(s) were successfully returned from postponed state for CardID = '{1}'.",
                                cardTasks.Count,
                                cardID);

                            var postMessageResult = new ValidationResultBuilder();
                            foreach (CardTask task in cardTasks)
                            {
                                if (task.UserID.HasValue)
                                {
                                    postMessageResult.Add(
                                        await this.notificationManager.SendAsync(
                                            DefaultNotifications.ReturnFromPostponeNotification,
                                            [task.UserID.Value],
                                            new NotificationSendContext
                                            {
                                                MainCardID = card.ID,
                                                TaskTypeID = task.TypeID,
                                                GetCardFuncAsync = (_, ct) => ValueTask.FromResult((Card?) card),
                                                ExcludeDeputies = true,
                                                Info = Shared.Notices.DefaultNotificationHelper.GetInfoWithTask(task),
                                                ModifyEmailActionAsync = (email, ct) =>
                                                {
                                                    Shared.Notices.DefaultNotificationHelper.ModifyTaskCaption(email, task);
                                                    return Task.CompletedTask;
                                                }
                                            }));
                                }
                            }

                            ValidationResult result = postMessageResult.Build();
                            logger.LogResult(result);
                        }
                        else
                        {
                            logger.Warn("Can't return task(s) from postponed state for CardID = '{0}'.", cardID);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public IPluginSettings ResolveSettings(Dictionary<string, object?>? info = null)
        {
            var settings = new PluginSettings(DefaultPluginNames.ReturnTasksFromPostponedPlugin);
            if (info is not null)
            {
                settings.Deserialize(info);
            }

            return settings;
        }

        #endregion
    }
}
