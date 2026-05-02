#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Unity;

namespace Tessa.Extensions.Default.Server.Cards
{
    /// <inheritdoc cref="ICycleGroupingSettings"/>
    public sealed class CycleGroupingSettings :
        ICycleGroupingSettings,
        IDisposable
    {
        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей для получения значений вручную.
        /// </summary>
        /// <param name="cardCache"><inheritdoc cref="ICardCache" path="/summary"/></param>
        /// <param name="disposableContainer"><inheritdoc cref="IUnityDisposableContainer" path="/summary"/></param>
        public CycleGroupingSettings(
            ICardCache cardCache,
            [OptionalDependency] IUnityDisposableContainer? disposableContainer = null)
        {
            this.cardCache = NotNullOrThrow(cardCache);

            this.data = new(ImmutableHashSet<Guid>.Empty);

            disposableContainer?.Register(this);
        }

        #endregion

        #region Constants And Static Fields

        /// <summary>
        /// Идентификаторы типов заданий, которые не учитываются при расчёте информации по циклам согласования.
        /// </summary>
        private static readonly ImmutableHashSet<Guid> ignoreTaskTypeIDList = [
            KrConstants.KrDeregistrationTypeID,
            DefaultTaskTypes.KrRequestCommentTypeID,
            DefaultTaskTypes.KrInfoRequestCommentTypeID,
            DefaultTaskTypes.KrInfoAdditionalApprovalTypeID
        ];

        #endregion

        #region Nested Types

        private sealed record Data(
            IReadOnlySet<Guid> CycleTaskGroupTypeIDList);

        #endregion

        #region Fields

        private bool isDisposed;

        private volatile Data data;

        private volatile Card? card;

        private readonly ICardCache cardCache;

        private readonly AsyncLock asyncLock = new();

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Private Methods

        private async ValueTask RebuildCacheIfRequiredAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                // если card is null или отличается от карточки в кэше actualCard,
                // то кэш ещё не построен или что-то в кэше изменилось, надо его пересчитать

                if (this.card is null
                    || await this.cardCache.Cards.GetAsync(
                          DefaultCardTypes.KrSettingsTypeName,
                          cancellationToken) is { IsSuccess: true } serverInstance
                    && this.card != serverInstance.GetValue())
                {
                    // lock with double check
                    using var _ = await this.asyncLock.EnterAsync(cancellationToken);

                    var card = this.card;

                    serverInstance = await this.cardCache.Cards.GetAsync(
                        DefaultCardTypes.KrSettingsTypeName,
                        cancellationToken);

                    if (serverInstance.IsSuccess)
                    {
                        var actualCard = serverInstance.GetValue();
                        if (card != actualCard)
                        {
                            this.RebuildCache(actualCard);
                            this.card = actualCard;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // при ошибке ничего не перестраиваем и не достраиваем
                logger.LogException(ex, LogLevel.Warn);
            }
        }

        private void RebuildCache(
            Card card)
        {
            HashSet<Guid>? cycleTaskGroupTypeIDList = null;

            if (card.TryGetSections() is { } sections
                && sections.TryGetValue("KrSettingsCycleGroupingTaskHistoryGroupTypes", out var cycleGroupingTaskHistoryGroupTypesSection)
                && cycleGroupingTaskHistoryGroupTypesSection.TryGetRows() is { Count: > 0 } cycleGroupingTaskHistoryGroupTypesRows)
            {
                cycleTaskGroupTypeIDList = new HashSet<Guid>(cycleGroupingTaskHistoryGroupTypesRows.Count);

                foreach (var row in cycleGroupingTaskHistoryGroupTypesRows)
                {
                    var groupTypeID = row.Get<Guid>("GroupTypeID");

                    cycleTaskGroupTypeIDList.Add(groupTypeID);
                }
            }

            this.data = new(
                (IReadOnlySet<Guid>?) cycleTaskGroupTypeIDList ?? ImmutableHashSet<Guid>.Empty);
        }

        #endregion

        #region ICycleGroupingSettings Members

        /// <inheritdoc/>
        public async ValueTask<IReadOnlySet<Guid>> GetCycleTaskGroupTypeIDListAsync(
            CancellationToken cancellationToken = default)
        {
            await this.RebuildCacheIfRequiredAsync(cancellationToken);
            return this.data.CycleTaskGroupTypeIDList;
        }

        /// <inheritdoc/>
        public ValueTask<IReadOnlySet<Guid>> GetIgnoreTaskTypeIDListAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlySet<Guid>>(ignoreTaskTypeIDList);

        #endregion

        #region IDisposable Members

        /// <doc path='info[@type="IDisposable" and @item="Dispose"]'/>
        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.asyncLock.Dispose();
            this.isDisposed = true;
        }

        #endregion
    }
}
