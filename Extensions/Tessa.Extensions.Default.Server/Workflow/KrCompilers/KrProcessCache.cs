#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Platform;
using Tessa.Platform.Data;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <inheritdoc cref="IKrProcessCache"/>
    public sealed class KrProcessCache :
        IKrProcessCache,
        IDisposable
    {
        #region Nested Types

        private sealed class CachedObject
        {
            #region Constructors

            public CachedObject(
                IEnumerable<IKrStageTemplate> stageTemplates,
                IEnumerable<IKrRuntimeStage> runtimeStages,
                IReadOnlyCollection<IKrStageGroup> stageGroups,
                IEnumerable<IKrPureProcess> pureProcesses,
                IEnumerable<IKrAction> actions,
                IEnumerable<IKrProcessButton> buttons,
                IEnumerable<IKrCommonMethod> methods)
            {
                this.StageTemplates = new ReadOnlyDictionary<Guid, IKrStageTemplate>(stageTemplates
                    .ToDictionary(
                        static k => k.ID,
                        static v => v));

                this.StageTemplatesByGroups = new ReadOnlyDictionary<Guid, IReadOnlyList<IKrStageTemplate>>(this.StageTemplates
                    .GroupBy(static p => p.Value.StageGroupID)
                    .ToDictionary(
                        static k => k.Key,
                        static v => (IReadOnlyList<IKrStageTemplate>) v
                            .Select(static p => p.Value)
                            .ToList()
                            .AsReadOnly()));

                this.RuntimeStages = new ReadOnlyDictionary<Guid, IKrRuntimeStage>(
                    runtimeStages.ToDictionary(
                        static k => k.StageID,
                        static v => v));

                this.RuntimeStagesByTemplates = new ReadOnlyDictionary<Guid, IReadOnlyList<IKrRuntimeStage>>(this.RuntimeStages
                    .GroupBy(static p => p.Value.TemplateID)
                    .ToDictionary(
                        static k => k.Key,
                        static v => (IReadOnlyList<IKrRuntimeStage>) v
                            .Select(static p => p.Value)
                            .ToList()
                            .AsReadOnly()));

                this.StageGroups = new ReadOnlyDictionary<Guid, IKrStageGroup>(stageGroups
                    .ToDictionary(
                        static k => k.ID,
                        static v => v));

                this.OrderedStageGroups = stageGroups
                    .OrderBy(static p => p.Order)
                    .ThenBy(static p => p.ID)
                    .ToList()
                    .AsReadOnly();

                this.StageGroupsByProcesses = new ReadOnlyDictionary<Guid, IReadOnlyList<IKrStageGroup>>(this.StageGroups
                    .GroupBy(static p => p.Value.SecondaryProcessID ?? Guid.Empty)
                    .ToDictionary(
                        static k => k.Key,
                        static v => (IReadOnlyList<IKrStageGroup>) v
                            .Select(static p => p.Value)
                            .ToList()
                            .AsReadOnly()));

                this.PureProcesses = new ReadOnlyDictionary<Guid, IKrPureProcess>(pureProcesses
                    .ToDictionary(
                        static k => k.ID,
                        static v => v));

                this.Actions = new ReadOnlyDictionary<Guid, IKrAction>(actions
                    .ToDictionary(
                        static k => k.ID,
                        static v => v));

                this.ActionsByTypes = new ReadOnlyDictionary<string, IReadOnlyList<IKrAction>>(this.Actions
                    .Where(static p => p.Value.EventType is not null)
                    .GroupBy(static p => p.Value.EventType)
                    .ToDictionary(
                        static k => k.Key,
                        static v => (IReadOnlyList<IKrAction>) v
                            .Select(static p => p.Value)
                            .ToList()
                            .AsReadOnly()));

                this.Buttons = new ReadOnlyDictionary<Guid, IKrProcessButton>(buttons
                    .ToDictionary(
                        static k => k.ID,
                        static v => v));

                this.Methods = methods
                    .ToList()
                    .AsReadOnly();
            }

            #endregion

            #region Properties

            public IReadOnlyDictionary<Guid, IKrStageTemplate> StageTemplates { get; }

            public IReadOnlyDictionary<Guid, IReadOnlyList<IKrStageTemplate>> StageTemplatesByGroups { get; }

            public IReadOnlyDictionary<Guid, IKrRuntimeStage> RuntimeStages { get; }

            public IReadOnlyDictionary<Guid, IReadOnlyList<IKrRuntimeStage>> RuntimeStagesByTemplates { get; }

            public IReadOnlyDictionary<Guid, IKrStageGroup> StageGroups { get; }

            public IReadOnlyList<IKrStageGroup> OrderedStageGroups { get; }

            public IReadOnlyDictionary<Guid, IReadOnlyList<IKrStageGroup>> StageGroupsByProcesses { get; }

            public IReadOnlyDictionary<Guid, IKrPureProcess> PureProcesses { get; }

            public IReadOnlyDictionary<Guid, IKrAction> Actions { get; }

            public IReadOnlyDictionary<string, IReadOnlyList<IKrAction>> ActionsByTypes { get; }

            public IReadOnlyDictionary<Guid, IKrProcessButton> Buttons { get; }

            public IReadOnlyList<IKrCommonMethod> Methods { get; }

            #endregion
        }

        #endregion

        #region Constants And Static Fields

        private const string CacheKey = "KrProcessCache";

        #endregion

        #region Fields

        private readonly ICardCache cardCache;
        private readonly IDbScope dbScope;
        private readonly IExtraSourceSerializer extraSourceSerializer;
        private readonly IKrStageSerializer stageSerializer;

        private readonly AsyncLock asyncLock = new AsyncLock();

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="KrProcessCache"/>.
        /// </summary>
        /// <param name="cardCache">Потокобезопасный кэш с карточками и дополнительными настройками.</param>
        /// <param name="dbScope">Объект для взаимодействия с базой данных.</param>
        /// <param name="extraSourceSerializer">Сериализатор объектов, содержащих информацию о дополнительных методах.</param>
        /// <param name="stageSerializer">Объект, предоставляющий методы для сериализации параметров этапов.</param>
        /// <param name="container">Контейнер, содержащий объекты <see cref="IDisposable"/>, которые будут освобождены при закрытии контейнеров <see cref="IUnityContainer"/>.</param>
        public KrProcessCache(
            ICardCache cardCache,
            IDbScope dbScope,
            IExtraSourceSerializer extraSourceSerializer,
            IKrStageSerializer stageSerializer,
            [OptionalDependency] IUnityDisposableContainer? container = null)
        {
            ThrowIfNull(cardCache);
            ThrowIfNull(dbScope);
            ThrowIfNull(extraSourceSerializer);
            ThrowIfNull(stageSerializer);

            this.cardCache = cardCache;
            this.dbScope = dbScope;
            this.extraSourceSerializer = extraSourceSerializer;
            this.stageSerializer = stageSerializer;

            container?.Register(this);
        }

        #endregion

        #region Private Methods

        private async Task<CachedObject> UpdateCachedObjectAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            using (await this.asyncLock.EnterAsync(cancellationToken))
            {
                // соединение с БД для заполнения кэша не должно зависеть от текущего соединения и его транзакции
                await using (this.dbScope.CreateNew())
                {
                    var stageTemplates = await KrCompilersSqlHelper.SelectStageTemplatesAsync(
                        this.dbScope,
                        cancellationToken: cancellationToken);

                    stageTemplates.AddRange(await KrCompilersSqlHelper.SelectVirtualStageTemplatesAsync(
                        this.dbScope,
                        cancellationToken: cancellationToken));

                    var runtimeStages = await KrCompilersSqlHelper.SelectRuntimeStagesAsync(
                        this.dbScope,
                        this.stageSerializer,
                        this.extraSourceSerializer,
                        cancellationToken: cancellationToken);

                    runtimeStages.AddRange(await KrCompilersSqlHelper.SelectSecondaryProcessRuntimeStagesAsync(
                        this.dbScope,
                        this.stageSerializer,
                        this.extraSourceSerializer,
                        cancellationToken: cancellationToken));

                    var stageGroups = await KrCompilersSqlHelper.SelectStageGroupsAsync(
                        this.dbScope,
                        cancellationToken: cancellationToken);

                    stageGroups.AddRange(await KrCompilersSqlHelper.SelectVirtualStageGroupsAsync(
                        this.dbScope,
                        cancellationToken: cancellationToken));

                    var methods = await KrCompilersSqlHelper.SelectCommonMethodsAsync(
                        this.dbScope,
                        cancellationToken: cancellationToken);

                    (var pureProcesses, var actions, var buttons) = await KrCompilersSqlHelper.SelectKrSecondaryProcessesAsync(
                        this.dbScope,
                        null,
                        cancellationToken);

                    return new CachedObject(
                        stageTemplates,
                        runtimeStages,
                        stageGroups,
                        pureProcesses,
                        actions,
                        buttons,
                        methods);
                }
            }
        }

        private ValueTask<CachedObject> GetCachedObjectAsync(
            CancellationToken cancellationToken = default) =>
            this.cardCache.Settings.GetAsync(CacheKey, this.UpdateCachedObjectAsync, cancellationToken);

        #endregion

        #region IKrProcessCache Members

        /// <inheritdoc />
        public async ValueTask<IReadOnlyDictionary<Guid, IKrStageGroup>> GetAllStageGroupsAsync(
            CancellationToken cancellationToken = default) =>
            (await this.GetCachedObjectAsync(cancellationToken)).StageGroups;

        /// <inheritdoc />
        public async ValueTask<IReadOnlyList<IKrStageGroup>> GetOrderedStageGroupsAsync(
            CancellationToken cancellationToken = default) =>
            (await this.GetCachedObjectAsync(cancellationToken)).OrderedStageGroups;

        /// <inheritdoc />
        public async ValueTask<IReadOnlyList<IKrStageGroup>> GetStageGroupsForSecondaryProcessAsync(
            Guid? process,
            CancellationToken cancellationToken = default)
        {
            var groupsByProcesses = (await this.GetCachedObjectAsync(cancellationToken)).StageGroupsByProcesses;

            return groupsByProcesses.GetValueOrDefault(process ?? Guid.Empty, ImmutableList<IKrStageGroup>.Empty);
        }

        /// <inheritdoc />
        public async ValueTask<IReadOnlyDictionary<Guid, IKrStageTemplate>> GetAllStageTemplatesAsync(
            CancellationToken cancellationToken = default) =>
            (await this.GetCachedObjectAsync(cancellationToken)).StageTemplates;

        /// <inheritdoc />
        public async ValueTask<IReadOnlyList<IKrStageTemplate>> GetStageTemplatesForGroupAsync(
            Guid groupID,
            CancellationToken cancellationToken = default)
        {
            var stagesByGroups = (await this.GetCachedObjectAsync(cancellationToken)).StageTemplatesByGroups;

            return stagesByGroups.GetValueOrDefault(groupID, ImmutableList<IKrStageTemplate>.Empty);
        }

        /// <inheritdoc />
        public async ValueTask<IReadOnlyDictionary<Guid, IKrRuntimeStage>> GetAllRuntimeStagesAsync(
            CancellationToken cancellationToken = default) =>
            (await this.GetCachedObjectAsync(cancellationToken)).RuntimeStages;

        /// <inheritdoc />
        public async ValueTask<IReadOnlyDictionary<Guid, IReadOnlyList<IKrRuntimeStage>>> GetAllStagesByTemplatesAsync(
            CancellationToken cancellationToken = default) =>
            (await this.GetCachedObjectAsync(cancellationToken)).RuntimeStagesByTemplates;

        /// <inheritdoc />
        public async ValueTask<IReadOnlyList<IKrCommonMethod>> GetAllCommonMethodsAsync(
            CancellationToken cancellationToken = default) =>
            (await this.GetCachedObjectAsync(cancellationToken)).Methods;

        /// <inheritdoc />
        public async ValueTask<IKrSecondaryProcess?> TryGetSecondaryProcessAsync(
            Guid processID,
            CancellationToken cancellationToken = default)
        {
            var cachedObject = await this.GetCachedObjectAsync(cancellationToken);
            if (cachedObject.PureProcesses.TryGetValue(processID, out var pure))
            {
                return pure;
            }
            if (cachedObject.Buttons.TryGetValue(processID, out var button))
            {
                return button;
            }
            if (cachedObject.Actions.TryGetValue(processID, out var action))
            {
                return action;
            }

            return null;
        }

        /// <inheritdoc />
        public async ValueTask<IReadOnlyDictionary<Guid, IKrPureProcess>> GetAllPureProcessesAsync(
            CancellationToken cancellationToken = default) =>
            (await this.GetCachedObjectAsync(cancellationToken)).PureProcesses;

        /// <inheritdoc />
        public async ValueTask<IReadOnlyCollection<IKrAction>> GetActionsByTypeAsync(
            string actionType,
            CancellationToken cancellationToken = default)
        {
            var actionsByTypes = (await this.GetCachedObjectAsync(cancellationToken)).ActionsByTypes;

            return actionsByTypes.GetValueOrDefault(actionType, ImmutableList<IKrAction>.Empty);
        }

        /// <inheritdoc />
        public async ValueTask<IReadOnlyDictionary<Guid, IKrAction>> GetAllActionsAsync(
            CancellationToken cancellationToken = default) =>
            (await this.GetCachedObjectAsync(cancellationToken)).Actions;

        /// <inheritdoc />
        public async ValueTask<IReadOnlyDictionary<Guid, IKrProcessButton>> GetAllButtonsAsync(
            CancellationToken cancellationToken = default) =>
            (await this.GetCachedObjectAsync(cancellationToken)).Buttons;

        /// <inheritdoc />
        public async Task InvalidateAsync()
        {
            using (await this.asyncLock.EnterAsync())
            {
                await this.cardCache.Settings.InvalidateAsync(CacheKey);
            }
        }

        #endregion

        #region IDisposable Members

        /// <inheritdoc/>
        public void Dispose() => this.asyncLock.Dispose();

        #endregion
    }
}
