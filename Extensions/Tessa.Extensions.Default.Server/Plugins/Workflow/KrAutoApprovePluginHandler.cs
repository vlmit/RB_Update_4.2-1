#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Platform.Data;
using Tessa.Platform.Plugins;
using Tessa.Platform.Runtime;
using Unity;

namespace Tessa.Extensions.Default.Server.Plugins.Workflow
{
    /// <summary>
    /// Обработчик плагина <see cref="DefaultPluginNames.KrAutoApprovePlugin"/>
    /// </summary>
    public sealed class KrAutoApprovePluginHandler : IPluginHandler
    {
        #region Fields

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IDbScope dbScope;
        private readonly ICardRepository cardRepository;
        private readonly ICardMetadata cardMetadata;
        private readonly ISession session;
        private readonly ICardGetStrategy cardGetStrategy;
        private readonly ICardServerPermissionsProvider cardServerPermissionsProvider;
        private readonly ICardTransactionStrategy cardTransactionStrategy;

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
        /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
        /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
        /// <param name="cardGetStrategy"><inheritdoc cref="ICardGetStrategy" path="/summary"/></param>
        /// <param name="cardServerPermissionsProvider"><inheritdoc cref="ICardServerPermissionsProvider" path="/summary"/></param>
        /// <param name="cardTransactionStrategy"><inheritdoc cref="ICardTransactionStrategy" path="/summary"/></param>
        public KrAutoApprovePluginHandler(
            IDbScope dbScope,
            [Dependency(CardRepositoryNames.ExtendedWithoutTransactionAndLocking)] ICardRepository cardRepository,
            ICardMetadata cardMetadata,
            ISession session,
            ICardGetStrategy cardGetStrategy,
            ICardServerPermissionsProvider cardServerPermissionsProvider,
            ICardTransactionStrategy cardTransactionStrategy)
        {
            this.dbScope = NotNullOrThrow(dbScope);
            this.cardRepository = NotNullOrThrow(cardRepository);
            this.cardMetadata = NotNullOrThrow(cardMetadata);
            this.session = NotNullOrThrow(session);
            this.cardGetStrategy = NotNullOrThrow(cardGetStrategy);
            this.cardServerPermissionsProvider = NotNullOrThrow(cardServerPermissionsProvider);
            this.cardTransactionStrategy = NotNullOrThrow(cardTransactionStrategy);
        }

        #endregion

        #region IPluginHandler Implementation

        public async ValueTask ExecuteAsync(IPluginExecutingContext context)
        {
            await using (this.dbScope.Create())
            {
                var db = this.dbScope.Db;
                var builderFactory = this.dbScope.BuilderFactory;

                List<KrAutoApproveTaskRecord> tasksToApprove = await KrAutoApprovePluginHelper.GetTasksToAutoApproveAsync(db, builderFactory, context.CancellationToken)
                    ;

                if (tasksToApprove.Count == 0)
                {
                    return;
                }

                const string defaultComment = "{$UI_Tasks_DefaultAutoApprovedMessage}";
                foreach (KrAutoApproveTaskRecord taskRecord in tasksToApprove)
                {
                    if (string.IsNullOrEmpty(taskRecord.ApprovalComment))
                    {
                        taskRecord.ApprovalComment = defaultComment;
                    }
                }

                // Обрабатываем в цикле задания согласования
                await KrAutoApprovePluginHelper.CompleteApproveTasksAsync(
                    tasksToApprove,
                    db,
                    builderFactory,
                    this.cardRepository,
                    this.cardMetadata,
                    this.session,
                    this.cardGetStrategy,
                    this.cardServerPermissionsProvider,
                    this.cardTransactionStrategy,
                    logger,
                    () => context.StopRequested,
                    context.CancellationToken);
            }
        }

        /// <inheritdoc/>
        public IPluginSettings ResolveSettings(Dictionary<string, object?>? info = null)
        {
            var settings = new PluginSettings(DefaultPluginNames.RefGroupsRecalculatePlugin);
            if (info is not null)
            {
                settings.Deserialize(info);
            }

            return settings;
        }

        #endregion
    }
}
