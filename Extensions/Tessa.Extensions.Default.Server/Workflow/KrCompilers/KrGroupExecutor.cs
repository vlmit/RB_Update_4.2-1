#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.SourceBuilders;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.SqlProcessing;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.UserAPI;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Shared.Workflow.KrCompilers;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <summary>
    /// Объект, выполняющий построение маршрута по группам этапов.
    /// </summary>
    /// <remarks>Единицами выполнения являются группы этапов.</remarks>
    /// <remarks>
    /// Инициализирует новый экземпляр класса <see cref="KrGroupExecutor"/>.
    /// </remarks>
    /// <param name="krSqlExecutor"><inheritdoc cref="IKrSqlExecutor" path="/summary"/></param>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="cardCache"><inheritdoc cref="ICardCache" path="/summary"/></param>
    /// <param name="krTypesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
    /// <param name="krStageSerializer"><inheritdoc cref="IKrStageSerializer" path="/summary"/></param>
    /// <param name="krProcessCache"><inheritdoc cref="IKrProcessCache" path="/summary"/></param>
    /// <param name="stageExecutor">Объект, выполняющий построение маршрута по шаблонам этапов.</param>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    /// <param name="unityContainer"><inheritdoc cref="IUnityContainer" path="/summary"/></param>
    /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
    /// <param name="krScope"><inheritdoc cref="IKrScope" path="/summary"/></param>
    /// <param name="krStageGroupCompilationCache"><inheritdoc cref="IKrStageGroupCompilationCache" path="/summary"/></param>
    /// <param name="transactionScope"><inheritdoc cref="ITransactionScope" path="/summary"/></param>
    /// <param name="krStageTemplateLockStrategy"><inheritdoc cref="IKrStageTemplateLockStrategy" path="/summary"/></param>
    public sealed class KrGroupExecutor(
        IKrSqlExecutor krSqlExecutor,
        IDbScope dbScope,
        ICardCache cardCache,
        IKrTypesCache krTypesCache,
        IKrStageSerializer krStageSerializer,
        IKrProcessCache krProcessCache,
        [Dependency(KrExecutorNames.StageExecutor)] IKrExecutor stageExecutor,
        ISession session,
        IUnityContainer unityContainer,
        ICardMetadata cardMetadata,
        IKrScope krScope,
        IKrStageGroupCompilationCache krStageGroupCompilationCache,
        ITransactionScope transactionScope,
        IKrStageTemplateLockStrategy krStageTemplateLockStrategy) :
        KrExecutorBase(
              krSqlExecutor,
              dbScope,
              cardCache,
              krTypesCache,
              krStageSerializer)
    {
        #region Fields

        private readonly IKrProcessCache krProcessCache = NotNullOrThrow(krProcessCache);
        private readonly IKrExecutor stageExecutor = NotNullOrThrow(stageExecutor);
        private readonly ISession session = NotNullOrThrow(session);
        private readonly IUnityContainer unityContainer = NotNullOrThrow(unityContainer);
        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);
        private readonly IKrScope krScope = NotNullOrThrow(krScope);
        private readonly IKrStageGroupCompilationCache krStageGroupCompilationCache = NotNullOrThrow(krStageGroupCompilationCache);
        private readonly ITransactionScope transactionScope = NotNullOrThrow(transactionScope);
        private readonly IKrStageTemplateLockStrategy krStageTemplateLockStrategy = NotNullOrThrow(krStageTemplateLockStrategy);

        #endregion

        #region IKrExecutor Members

        /// <inheritdoc />
        public override async Task ExecuteAsync(
            IKrExecutionContext context)
        {
            ThrowIfNull(context);

            await using var transactionScope = TransactionScopeContext.HasCurrent ? null : this.transactionScope.Create();
            try
            {
                if (await this.krStageTemplateLockStrategy.ObtainReaderLockAsync(context.CancellationToken) is not { IsSuccessful: true })
                {
                    context.ValidationResult.AddError(this, "$KrMessages_StageTemplatesReadErrorMessage");
                    return;
                }

                var currentContext = await this.PrepareExecutionContextAsync(context);

                var executionUnits = await this.CreateExecutionUnitsAsync(currentContext);

                if (!currentContext.ValidationResult.IsSuccessful())
                {
                    return;
                }

                await this.RunForAllAsync(
                    currentContext,
                    executionUnits,
                    RunBeforeAsync);

                if (!currentContext.ValidationResult.IsSuccessful())
                {
                    return;
                }

                await using (this.DbScope.Create())
                {
                    // Формируется список подтвержденных групп.
                    var confirmedIDs = new List<Guid>();

                    await this.RunForAllAsync(
                        currentContext,
                        executionUnits,
                        p => this.RunConditionsAsync(
                            p,
                            currentContext,
                            confirmedIDs));

                    if (!currentContext.ValidationResult.IsSuccessful())
                    {
                        return;
                    }

                    await this.ExecuteStagesAsync(
                        currentContext,
                        confirmedIDs);

                    if (!currentContext.ValidationResult.IsSuccessful())
                    {
                        return;
                    }

                    DeleteUnconfirmedGroups(
                        context,
                        currentContext,
                        confirmedIDs);
                }

                await this.RunForAllAsync(
                    currentContext,
                    executionUnits,
                    RunAfterAsync);
            }
            finally
            {
                if (transactionScope is not null)
                {
                    await this.transactionScope.PerformHandlersAsync(
                        context.ValidationResult.IsSuccessful(),
                        context.ValidationResult,
                        context.CancellationToken);
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Подготавливает контекст для текущего <see cref="IKrExecutor"/>.
        /// </summary>
        /// <param name="context">Исходный контекст.</param>
        /// <returns>Контекст для текущего <see cref="IKrExecutor"/>.</returns>
        private async Task<IKrExecutionContext> PrepareExecutionContextAsync(
            IKrExecutionContext context)
        {
            var stageGroupIDs = await KrCompilersSqlHelper.SelectFilteredStageGroupsAsync(
                this.DbScope,
                context.TypeID ?? Guid.Empty,
                context.WorkflowProcess.ProcessOwnerCurrentProcess?.AuthorID ?? context.WorkflowProcess.AuthorCurrentProcess?.AuthorID ?? this.session.User.ID,
                secondaryProcessID: context.SecondaryProcess?.ID,
                cancellationToken: context.CancellationToken);

            var newExecutionUnitsIDs = context.ExecutionUnitIDs is null
                ? stageGroupIDs
                : context.ExecutionUnitIDs.Intersect(stageGroupIDs);

            return context.Copy(new HashSet<Guid>(newExecutionUnitsIDs));
        }

        /// <summary>
        /// Создаёт список единиц выполнения.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrExecutionContext" path="/summary"/></param>
        /// <returns>Список единиц выполнения.</returns>
        private async ValueTask<IList<IKrExecutionUnit>> CreateExecutionUnitsAsync(
            IKrExecutionContext context)
        {
            var stageGroups = await this.krProcessCache
                .GetOrderedStageGroupsAsync(context.CancellationToken);

            var executionUnits = new List<IKrExecutionUnit>(context.ExecutionUnitIDs!.Count);

            foreach (var stageGroup in stageGroups)
            {
                if (!context.ExecutionUnitIDs.Contains(stageGroup.ID))
                {
                    continue;
                }

                var compilationObject = await this.krStageGroupCompilationCache.GetAsync(
                    stageGroup.ID,
                    cancellationToken: context.CancellationToken);

                var instance = compilationObject.TryCreateKrScriptInstance(
                    KrCompilersHelper.FormatClassName(
                        SourceIdentifiers.KrDesignTimeClass,
                        SourceIdentifiers.GroupAlias,
                        stageGroup.ID),
                    context.ValidationResult);

                if (!context.ValidationResult.IsSuccessful())
                {
                    return Array.Empty<IKrExecutionUnit>();
                }

                // Создание KrScript необходимо для передачи данных через KrExecutionUnit.Instance.
                executionUnits.Add(this.CreateExecutionUnit(
                    context,
                    stageGroup,
                    instance ?? new KrScript()));
            }

            return executionUnits;
        }

        /// <summary>
        /// Создаёт единицу выполнения.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrExecutionContext" path="/summary"/></param>
        /// <param name="stageGroup"><inheritdoc cref="IKrStageGroup" path="/summary"/></param>
        /// <param name="instance"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <returns><inheritdoc cref="IKrExecutionUnit" path="/summary"/></returns>
        private KrExecutionUnit CreateExecutionUnit(
            IKrExecutionContext context,
            IKrStageGroup stageGroup,
            IKrScript instance)
        {
            instance.StageGroupID = stageGroup.ID;
            instance.StageGroupName = stageGroup.Name;
            instance.StageGroupOrder = stageGroup.Order;

            // Шаблона сейчас нет
            instance.TemplateID = Guid.Empty;
            instance.TemplateName = string.Empty;
            instance.Order = -1;
            instance.Position = GroupPosition.Unspecified;
            instance.CanChangeOrder = false;
            instance.IsStagesReadonly = false;

            // На данном этапе нет контейнера, способного пересчитывать положения этапов.
            instance.StagesContainer = null;
            instance.WorkflowProcess = context.WorkflowProcess;

            instance.MainCardAccessStrategy = context.MainCardAccessStrategy;
            instance.CardID = context.CardID ?? Guid.Empty;
            instance.CardType = context.CardType;
            instance.DocTypeID = context.DocTypeID ?? Guid.Empty;

            if (context.KrComponents.HasValue)
            {
                instance.KrComponents = context.KrComponents.Value;
            }

            instance.CardContext = context.CardContext;
            instance.Session = this.session;
            instance.DbScope = this.DbScope;
            instance.UnityContainer = this.unityContainer;
            instance.CardMetadata = this.cardMetadata;
            instance.ValidationResult = context.ValidationResult;
            instance.KrScope = this.krScope;
            instance.CardCache = this.CardCache;
            instance.KrTypesCache = this.KrTypesCache;
            instance.StageSerializer = this.KrStageSerializer;
            instance.CancellationToken = context.CancellationToken;
            instance.Seal();

            return new KrExecutionUnit(
                stageGroup,
                instance);
        }

        /// <summary>
        /// Выполняет пересчёт подтверждённых групп этапов.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrExecutionContext" path="/summary"/></param>
        /// <param name="confirmedGroupIDs">Список идентификаторов подтверждённых групп этапов.</param>
        /// <returns>Асинхронная задача.</returns>
        private async Task ExecuteStagesAsync(
            IKrExecutionContext context,
            IEnumerable<Guid> confirmedGroupIDs)
        {
            foreach (var confirmedGroupID in confirmedGroupIDs)
            {
                var ctx = context.Copy(confirmedGroupID);

                await this.stageExecutor.ExecuteAsync(ctx);

                if (!ctx.ValidationResult.IsSuccessful())
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Удаляет этапы из неподтверждённых групп.
        /// </summary>
        /// <param name="context">Исходный контекст.</param>
        /// <param name="currentContext">Контекст <see cref="IKrExecutor"/> для текущего объекта.</param>
        /// <param name="confirmedIDs">Список идентификаторов подтверждённых единиц выполнения.</param>
        private static void DeleteUnconfirmedGroups(
            IKrExecutionContext context,
            IKrExecutionContext currentContext,
            IEnumerable<Guid> confirmedIDs)
        {
            if (context.ExecutionUnitIDs is null)
            {
                // Случай полного пересчета.
                // Удаляем все неподтверждённые группы.
                var newStages = new SealableObjectList<Stage>();

                foreach (var stage in currentContext.WorkflowProcess.Stages)
                {
                    if (!stage.BasedOnTemplate
                        || confirmedIDs.Contains(stage.StageGroupID))
                    {
                        newStages.Add(stage);
                    }
                }

                currentContext.WorkflowProcess.Stages = newStages;
            }
            else
            {
                // Случай частичного пересчета.
                // Удаляем группы из context.ExecutionUnitIDs, не попавшие в currentContext.ExecutionUnitIDs.
                var redundantGroups = context
                    .ExecutionUnitIDs
                    .Except(currentContext.ExecutionUnitIDs!)
                    .ToList();

                var newStages = new SealableObjectList<Stage>();

                foreach (var stage in currentContext.WorkflowProcess.Stages)
                {
                    if (!stage.BasedOnTemplate
                        || !redundantGroups.Contains(stage.StageGroupID)
                        && (confirmedIDs.Contains(stage.StageGroupID)
                        || currentContext.ExecutionUnitIDs!.All(q => q != stage.StageGroupID)))
                    {
                        newStages.Add(stage);
                    }
                }

                currentContext.WorkflowProcess.Stages = newStages;
            }
        }

        #endregion
    }
}
