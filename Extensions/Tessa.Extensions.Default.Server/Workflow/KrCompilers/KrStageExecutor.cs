#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.SourceBuilders;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.SqlProcessing;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.UserAPI;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <summary>
    /// Объект, выполняющий построение маршрута по шаблонам этапов.
    /// </summary>
    /// <remarks>Единицами выполнения являются шаблоны этапов.</remarks>
    /// <remarks>
    /// Инициализирует новый экземпляр класса <see cref="KrStageExecutor"/>.
    /// </remarks>
    /// <param name="krSqlExecutor"><inheritdoc cref="IKrSqlExecutor" path="/summary"/></param>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="cardCache"><inheritdoc cref="ICardCache" path="/summary"/></param>
    /// <param name="krTypesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
    /// <param name="krStageSerializer"><inheritdoc cref="IKrStageSerializer" path="/summary"/></param>
    /// <param name="objectModelMapper"><inheritdoc cref="IObjectModelMapper" path="/summary"/></param>
    /// <param name="krScope"><inheritdoc cref="IKrScope" path="/summary"/></param>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    /// <param name="unityContainer"><inheritdoc cref="IUnityContainer" path="/summary"/></param>
    /// <param name="krProcessCache"><inheritdoc cref="IKrProcessCache" path="/summary"/></param>
    /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
    /// <param name="transactionScope"><inheritdoc cref="ITransactionScope" path="/summary"/></param>
    /// <param name="krStageTemplateCompilationCache"><inheritdoc cref="IKrStageTemplateCompilationCache" path="/summary"/></param>
    /// <param name="krStageTemplateLockStrategy"><inheritdoc cref="IKrStageTemplateLockStrategy" path="/summary"/></param>
    public sealed class KrStageExecutor(
        IKrSqlExecutor krSqlExecutor,
        IDbScope dbScope,
        ICardCache cardCache,
        IKrTypesCache krTypesCache,
        IKrStageSerializer krStageSerializer,
        IObjectModelMapper objectModelMapper,
        IKrScope krScope,
        ISession session,
        IUnityContainer unityContainer,
        IKrProcessCache krProcessCache,
        ICardMetadata cardMetadata,
        ITransactionScope transactionScope,
        IKrStageTemplateCompilationCache krStageTemplateCompilationCache,
        IKrStageTemplateLockStrategy krStageTemplateLockStrategy) :
        KrExecutorBase(
              krSqlExecutor,
              dbScope,
              cardCache,
              krTypesCache,
              krStageSerializer)
    {
        #region Fields

        private readonly IObjectModelMapper objectModelMapper = NotNullOrThrow(objectModelMapper);
        private readonly IKrScope krScope = NotNullOrThrow(krScope);
        private readonly ISession session = NotNullOrThrow(session);
        private readonly IUnityContainer unityContainer = NotNullOrThrow(unityContainer);
        private readonly IKrProcessCache krProcessCache = NotNullOrThrow(krProcessCache);
        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);
        private readonly IKrStageTemplateCompilationCache krStageTemplateCompilationCache = NotNullOrThrow(krStageTemplateCompilationCache);
        private readonly ITransactionScope transactionScope = NotNullOrThrow(transactionScope);
        private readonly IKrStageTemplateLockStrategy krStageTemplateLockStrategy = NotNullOrThrow(krStageTemplateLockStrategy);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task ExecuteAsync(
            IKrExecutionContext context)
        {
            ThrowIfNull(context);
            ThrowIf(context.GroupID, !context.GroupID.HasValue);

            await using var transactionScope = TransactionScopeContext.HasCurrent ? null : this.transactionScope.Create();
            try
            {
                if (await this.krStageTemplateLockStrategy.ObtainReaderLockAsync(context.CancellationToken) is not { IsSuccessful: true })
                {
                    context.ValidationResult.AddError(this, "$KrMessages_StageTemplatesReadErrorMessage");
                    return;
                }

                var currentContext = await this.PrepareExecutionContextAsync(context);

                var stageGroups = await this.krProcessCache.GetAllStageGroupsAsync(
                    currentContext.CancellationToken);

                if (!stageGroups.TryGetValue(currentContext.GroupID!.Value, out var stageGroup))
                {
                    currentContext.ValidationResult.AddError(
                        this,
                        $"Stage group with ID = {currentContext.GroupID.Value:B} not found.");
                    return;
                }

                var stagesContainer = new StagesContainer(
                    this.objectModelMapper,
                    currentContext.WorkflowProcess,
                    currentContext.GroupID.Value);

                var executionUnits = await this.CreateExecutionUnitsAsync(
                    currentContext,
                    stageGroup,
                    stagesContainer);

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
                    // Формируется список подтвержденных шаблонов.
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

                    await this.UpdateStagesWithConfirmedTemplatesAsync(
                        confirmedIDs,
                        stagesContainer,
                        currentContext.CancellationToken);
                }

                await this.RunForAllAsync(
                    currentContext,
                    executionUnits,
                    RunAfterAsync);

                if (!currentContext.ValidationResult.IsSuccessful())
                {
                    return;
                }

                // Пересчёт порядка этапов.
                // Вызывается косвенно при получении значения свойства, если это необходимо.
                // Ограничение: этапы могут быть изменены корректно ТОЛЬКО через методы объекта StagesContainer, т.к. они информируют о необходимости выполнения сортировки коллекции этапов.
                // Ограничение обеспечивается существующим API по работе с этапами, описанным IKrScript.
                // Изменение коллекции этапов напрямую через WorkflowProcess.Stages может привести к некорректному поведению.
                _ = stagesContainer.Stages;

                stagesContainer.RestoreFlags();
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
            var templateIDs = await KrCompilersSqlHelper.GetFilteredStageTemplates(
                this.DbScope,
                context.TypeID ?? Guid.Empty,
                context.WorkflowProcess.ProcessOwnerCurrentProcess?.AuthorID ?? context.WorkflowProcess.AuthorCurrentProcess?.AuthorID ?? this.session.User.ID,
                context.GroupID!.Value,
                context.SecondaryProcess?.ID,
                context.CancellationToken);

            var newExecutionUnitsIDs = context.ExecutionUnitIDs is null
                ? templateIDs
                : context.ExecutionUnitIDs.Intersect(templateIDs);

            return context.Copy(new HashSet<Guid>(newExecutionUnitsIDs));
        }

        /// <summary>
        /// Создаёт список единиц выполнения.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrExecutionContext" path="/summary"/></param>
        /// <param name="stageGroup">Объект, содержащий информацию о группе этапов <see cref="IKrExecutionContext.GroupID"/>.</param>
        /// <param name="stagesContainer"><inheritdoc cref="StagesContainer" path="/summary"/></param>
        /// <returns>Список единиц выполнения.</returns>
        private async ValueTask<IList<IKrExecutionUnit>> CreateExecutionUnitsAsync(
            IKrExecutionContext context,
            IKrStageGroup stageGroup,
            StagesContainer stagesContainer)
        {
            var stageTemplates = await this.krProcessCache.GetAllStageTemplatesAsync(context.CancellationToken);

            var executionUnits = new List<IKrExecutionUnit>(context.ExecutionUnitIDs!.Count);

            foreach (var stageTemplateID in context.ExecutionUnitIDs)
            {
                if (!stageTemplates.TryGetValue(stageTemplateID, out var stageTemplate))
                {
                    continue;
                }

                if (stageTemplate.StageGroupID != stageGroup.ID)
                {
                    throw new InvalidOperationException(
                        $"{this.GetType().Name} doesn't support templates from different groups."
                        + $"{Environment.NewLine}"
                        + $"{stageTemplate.Name} refers to {stageTemplate.StageGroupID:B} should refer to the {stageTemplate.StageGroupID:B}.");
                }

                var compilationObject = await this.krStageTemplateCompilationCache.GetAsync(
                    stageTemplate.ID,
                    cancellationToken: context.CancellationToken);

                var instance = compilationObject.TryCreateKrScriptInstance(
                    KrCompilersHelper.FormatClassName(
                        SourceIdentifiers.KrDesignTimeClass,
                        SourceIdentifiers.TemplateAlias,
                        stageTemplate.ID),
                    context.ValidationResult);

                if (!context.ValidationResult.IsSuccessful())
                {
                    return Array.Empty<IKrExecutionUnit>();
                }

                // Создание KrScript необходимо для передачи данных через KrExecutionUnit.Instance.
                executionUnits.Add(
                    this.CreateExecutionUnit(
                        context,
                        stageTemplate,
                        stageGroup,
                        instance ?? new KrScript(),
                        stagesContainer));
            }

            return executionUnits;
        }

        /// <summary>
        /// Создаёт единицу выполнения.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrExecutionContext" path="/summary"/></param>
        /// <param name="stageTemplate"><inheritdoc cref="IKrStageTemplate" path="/summary"/></param>
        /// <param name="stageGroup"><inheritdoc cref="IKrStageGroup" path="/summary"/></param>
        /// <param name="instance"><inheritdoc cref="IKrScript" path="/summary"/></param>
        /// <param name="stagesContainer"><inheritdoc cref="StagesContainer" path="/summary"/></param>
        /// <returns><inheritdoc cref="IKrExecutionUnit" path="/summary"/></returns>
        private KrExecutionUnit CreateExecutionUnit(
            IKrExecutionContext context,
            IKrStageTemplate stageTemplate,
            IKrStageGroup stageGroup,
            IKrScript instance,
            StagesContainer stagesContainer)
        {
            instance.StageGroupID = stageGroup.ID;
            instance.StageGroupName = stageGroup.Name;
            instance.StageGroupOrder = stageGroup.Order;

            instance.TemplateID = stageTemplate.ID;
            instance.TemplateName = stageTemplate.Name;
            instance.Order = stageTemplate.Order;
            instance.Position = stageTemplate.Position;
            instance.CanChangeOrder = stageTemplate.CanChangeOrder;
            instance.IsStagesReadonly = stageTemplate.IsStagesReadonly;

            instance.WorkflowProcess = context.WorkflowProcess;

            instance.MainCardAccessStrategy = context.MainCardAccessStrategy;
            instance.CardID = context.CardID ?? Guid.Empty;
            instance.CardType = context.CardType;
            instance.DocTypeID = context.DocTypeID ?? Guid.Empty;
            instance.CardContext = context.CardContext;

            if (context.KrComponents.HasValue)
            {
                instance.KrComponents = context.KrComponents.Value;
            }

            instance.StagesContainer = stagesContainer;
            instance.Session = this.session;
            instance.DbScope = this.DbScope;
            instance.UnityContainer = this.unityContainer;
            instance.CardMetadata = this.cardMetadata;
            instance.KrScope = this.krScope;
            instance.ValidationResult = context.ValidationResult;
            instance.CardCache = this.CardCache;
            instance.KrTypesCache = this.KrTypesCache;
            instance.StageSerializer = this.KrStageSerializer;
            instance.CancellationToken = context.CancellationToken;
            instance.Seal();

            return new KrExecutionUnit(stageTemplate, instance);
        }

        /// <summary>
        /// Объединяет существующие этапы с этапами из подтверждённых шаблонов этапов.
        /// </summary>
        /// <param name="confirmedTemplateIDs">Список идентификаторов подтверждённых шаблонов этапов.</param>
        /// <param name="stagesContainer"><inheritdoc cref="StagesContainer" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        private async Task UpdateStagesWithConfirmedTemplatesAsync(
            IList<Guid> confirmedTemplateIDs,
            StagesContainer stagesContainer,
            CancellationToken cancellationToken = default)
        {
            var templates = await this.krProcessCache.GetAllStageTemplatesAsync(cancellationToken);

            var stagesByTemplates = await this.krProcessCache
                .GetAllStagesByTemplatesAsync(cancellationToken);

            var newStages = new List<Stage>();

            foreach (var confirmedTemplateID in confirmedTemplateIDs)
            {
                if (!templates.TryGetValue(confirmedTemplateID, out var confirmedTemplate))
                {
                    continue;
                }

                var runtimeStages = stagesByTemplates.GetValueOrDefault(
                    confirmedTemplateID,
                    ImmutableList<IKrRuntimeStage>.Empty);

                var newStagesTemp = await this.objectModelMapper.GetTemplateStagesAsync(
                    confirmedTemplate,
                    runtimeStages,
                    cancellationToken);

                newStages.AddRange(newStagesTemp);
            }

            await stagesContainer.MergeStagesAsync(
                newStages,
                cancellationToken);

            stagesContainer.DeleteUnconfirmedStages();
        }

        #endregion
    }
}
