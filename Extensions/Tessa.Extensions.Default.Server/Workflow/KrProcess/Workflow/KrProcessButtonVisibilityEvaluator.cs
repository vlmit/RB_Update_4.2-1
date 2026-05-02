#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.SourceBuilders;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.SqlProcessing;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers.UserAPI;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Conditions;
using Tessa.Platform.Data;
using Tessa.Platform.RefGroups;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Tessa.Roles.NestedRoles;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <inheritdoc cref="IKrProcessButtonVisibilityEvaluator" path="/summary"/>
    /// <param name="processCache"><inheritdoc cref="IKrProcessCache" path="/summary"/></param>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="session"><inheritdoc cref="ISession" path="/summary"/></param>
    /// <param name="compilationCache"><inheritdoc cref="IKrSecondaryProcessCompilationCache" path="/summary"/></param>
    /// <param name="unityContainer"><inheritdoc cref="IUnityContainer" path="/summary"/></param>
    /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
    /// <param name="scope"><inheritdoc cref="IKrScope" path="/summary"/></param>
    /// <param name="typesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
    /// <param name="cardCache"><inheritdoc cref="ICardCache" path="/summary"/></param>
    /// <param name="sqlExecutor"><inheritdoc cref="IKrSqlExecutor" path="/summary"/></param>
    /// <param name="stageSerializer"><inheritdoc cref="IKrStageSerializer" path="/summary"/></param>
    /// <param name="conditionExecutor"><inheritdoc cref="IConditionExecutor" path="/summary"/></param>
    /// <param name="nestedRoleContextSelector"><inheritdoc cref="INestedRoleContextSelector" path="/summary"/></param>
    /// <param name="krSecondaryProcessTileHandlerResolver"><inheritdoc cref="IKrSecondaryProcessTileHandlerResolver" path="/summary"/></param>
    public sealed class KrProcessButtonVisibilityEvaluator(
        IKrProcessCache processCache,
        IDbScope dbScope,
        ISession session,
        IKrSecondaryProcessCompilationCache compilationCache,
        IUnityContainer unityContainer,
        ICardMetadata cardMetadata,
        IKrScope scope,
        IKrTypesCache typesCache,
        ICardCache cardCache,
        IKrSqlExecutor sqlExecutor,
        IKrStageSerializer stageSerializer,
        IConditionExecutor conditionExecutor,
        INestedRoleContextSelector nestedRoleContextSelector,
        IKrSecondaryProcessTileHandlerResolver krSecondaryProcessTileHandlerResolver) :
        IKrProcessButtonVisibilityEvaluator
    {
        #region Fields

        private readonly IKrProcessCache processCache = NotNullOrThrow(processCache);

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);

        private readonly ISession session = NotNullOrThrow(session);

        private readonly IKrSecondaryProcessCompilationCache compilationCache = NotNullOrThrow(compilationCache);

        private readonly IUnityContainer unityContainer = NotNullOrThrow(unityContainer);

        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);

        private readonly IKrScope scope = NotNullOrThrow(scope);

        private readonly IKrTypesCache typesCache = NotNullOrThrow(typesCache);

        private readonly ICardCache cardCache = NotNullOrThrow(cardCache);

        private readonly IKrSqlExecutor sqlExecutor = NotNullOrThrow(sqlExecutor);

        private readonly IKrStageSerializer stageSerializer = NotNullOrThrow(stageSerializer);

        private readonly IConditionExecutor conditionExecutor = NotNullOrThrow(conditionExecutor);

        private readonly INestedRoleContextSelector nestedRoleContextSelector = NotNullOrThrow(nestedRoleContextSelector);

        private readonly IKrSecondaryProcessTileHandlerResolver krSecondaryProcessTileHandlerResolver = NotNullOrThrow(krSecondaryProcessTileHandlerResolver);

        #endregion

        #region IKrProcessButtonVisibilityEvaluator Members

        /// <inheritdoc />
        public async Task<IList<IKrProcessButton>> EvaluateGlobalButtonsAsync(
            IKrProcessButtonVisibilityEvaluatorContext context)
        {
            ThrowIfNull(context);

            var buttonIDs = await this.GetGlobalButtonsIDsAsync(context.CancellationToken);
            return await this.EvaluateButtonsVisibilityAsync(buttonIDs, context);
        }

        /// <inheritdoc />
        public async Task<IList<IKrProcessButton>> EvaluateLocalButtonsAsync(
            IKrProcessButtonVisibilityEvaluatorContext context)
        {
            ThrowIfNull(context);

            await using (this.dbScope.Create())
            {
                var typeID = context.DocTypeID ?? context.CardType?.ID ?? Guid.Empty;
                var nestedContextID = context.Card is null
                    ? null
                    : await this.nestedRoleContextSelector.GetContextAsync(context.Card);

                var buttonIDs = await this.GetLocalButtonIDsAsync(
                    typeID,
                    context.State?.ID ?? -1,
                    nestedContextID,
                    context.CancellationToken);

                return await this.EvaluateButtonsVisibilityAsync(buttonIDs, context);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Возвращает список отображаемых вторичных процессов, работающих в режиме "Кнопка".
        /// </summary>
        /// <param name="buttonIDs">Идентификаторы вторичных процессов, работающих в режиме "Кнопка", для которых требуется проверить дополнительное условие видимости.</param>
        /// <param name="context">Контекст, используемый при определении видимости тайла вторичного процесса, работающего в режиме "Кнопка".</param>
        /// <returns>Список доступных вторичных процессов, работающих в режиме "Кнопка".</returns>
        private async ValueTask<IList<IKrProcessButton>> EvaluateButtonsVisibilityAsync(
            IReadOnlyCollection<Guid> buttonIDs,
            IKrProcessButtonVisibilityEvaluatorContext context)
        {
            await using var _ = this.dbScope.Create();
            var buttons = await this.processCache.GetAllButtonsAsync(
                context.CancellationToken);

            var filteredButtons = new List<IKrProcessButton>(buttonIDs.Count);
            foreach (var buttonID in buttonIDs)
            {
                if (!buttons.TryGetValue(buttonID, out var button))
                {
                    continue;
                }

                if (await this.EvaluateVisibilityAsync(button, context))
                {
                    filteredButtons.Add(button);
                }

                if (!context.ValidationResult.IsSuccessful())
                {
                    break;
                }
            }

            return filteredButtons;
        }

        /// <summary>
        /// Возвращает список идентификаторов глобальных вторичных процессов, работающих в режиме "Кнопка" доступных текущему пользователю.
        /// </summary>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Список идентификаторов глобальных вторичных процессов, работающих в режиме "Кнопка" доступных текущему пользователю.</returns>
        private async Task<List<Guid>> GetGlobalButtonsIDsAsync(CancellationToken cancellationToken = default)
        {
            await using (this.dbScope.Create())
            {
                var db = this.dbScope.Db;
                var builder = this.dbScope.BuilderFactory
                    .Select().C("t", KrConstants.KrSecondaryProcesses.ID)
                    .From(KrConstants.KrSecondaryProcesses.Name, "t").NoLock()
                    .Where()
                    .C(KrConstants.KrSecondaryProcesses.IsGlobal).Equals().V(true)
                    .And()
                    .E(w => w
                        .NotExists(e => e
                            .Select().V(null)
                            .From(KrConstants.KrStageRoles.Name, "r").NoLock()
                            .Where().C("r", KrConstants.KrStageRoles.ID).Equals().C("t", KrConstants.KrSecondaryProcesses.ID))
                        .Or()
                        .Exists(e => e
                            .Select().V(null)
                            .From(KrConstants.KrStageRoles.Name, "r").NoLock()
                            .InnerJoin("RoleUsers", "ru").NoLock()
                            .On().C("ru", "ID").Equals().C("r", KrConstants.KrStageRoles.RoleID)
                            .Where().C("r", KrConstants.KrStageRoles.ID).Equals().C("t", KrConstants.KrSecondaryProcesses.ID)
                            .And().C("ru", "UserID").Equals().P("UserID")));

                return await db
                    .SetCommand(
                        builder.Build(),
                        db.Parameter("UserID", this.session.User.ID))
                    .LogCommand()
                    .ExecuteListAsync<Guid>(cancellationToken);
            }
        }

        /// <summary>
        /// Возвращает список идентификаторов локальных вторичных процессов, работающих в режиме "Кнопка", удовлетворяющих заданным критериям и доступных текущему пользователю.
        /// </summary>
        /// <param name="typeID">Идентификатор, если задан, типа документа или типа карточки или значение <see cref="Guid.Empty"/>.</param>
        /// <param name="stateID">Идентификатор состояния карточки документа.</param>
        /// <param name="nestedContextID">Идентификатор контекста вложенных ролей или null, если вложенные роли не проверяются.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Список идентификаторов локальных вторичных процессов, работающих в режиме "Кнопка", удовлетворяющих заданным критериям и доступных текущему пользователю.</returns>
        private async Task<List<Guid>> GetLocalButtonIDsAsync(
            Guid typeID,
            int stateID,
            Guid? nestedContextID,
            CancellationToken cancellationToken = default)
        {
            await using (this.dbScope.Create())
            {
                var db = this.dbScope.Db;
                var query = this.dbScope.BuilderFactory.Cached(this, nestedContextID.HasValue
                    ? "SelectRowsWithContext"
                    : "SelectRowsWithoutContext",
                    builderFactory =>
                    builderFactory
                        .SelectDistinct().C("t", KrConstants.KrSecondaryProcesses.ID)
                        .From(KrConstants.KrSecondaryProcesses.Name, "t").NoLock()
                        .LeftJoin(KrConstants.KrStageTypes.Name, "tt").NoLock()
                            .On().C("tt", KrConstants.KrStageTypes.ID).Equals().C("t", KrConstants.KrSecondaryProcesses.ID)
                        .LeftJoin(KrConstants.KrStageDocStates.Name, "tds").NoLock()
                            .On().C("tds", KrConstants.KrStageDocStates.ID).Equals().C("t", KrConstants.KrSecondaryProcesses.ID)
                        .Where()
                        .C(KrConstants.KrSecondaryProcesses.IsGlobal).Equals().V(false)
                        .And()
                        .E(w => w
                            .C("tt", KrConstants.KrStageTypes.TypeID).IsNull()
                            .Or()
                            .C("tt", KrConstants.KrStageTypes.TypeID).Equals().P("TypeID")
                            .Or()
                            .Exists(b => b
                                .Select().V(null)
                                .From(RefGroupsHelper.RefGroupsGuidValuesTable, "v").NoLock()
                                .Where().C("v", RefGroupsHelper.RefGroupsValuesID).Equals().C("tt", KrConstants.KrStageTypes.TypeID)
                                    .And().C("v", RefGroupsHelper.RefGroupsValuesValue).Equals().P("TypeID")))
                        .And()
                        .E(w => w
                            .C("tds", KrConstants.KrStageDocStates.StateID).IsNull()
                            .Or()
                            .C("tds", KrConstants.KrStageDocStates.StateID).Equals().P("StateID")
                            .Or()
                            .Exists(b => b
                                .Select().V(null)
                                .From(RefGroupsHelper.RefGroupsMainSectionName, "ref").NoLock()
                                .InnerJoin(RefGroupsHelper.RefGroupsIntValuesTable, "v").NoLock()
                                    .On().C("ref", RefGroupsHelper.RefGroupsID).Equals().C("v", RefGroupsHelper.RefGroupsValuesID)
                                .Where().C("ref", RefGroupsHelper.RefGroupsIntID).Equals().C("tds", KrConstants.KrStageDocStates.StateID)
                                    .And().C("v", RefGroupsHelper.RefGroupsValuesValue).Equals().P("StateID")))
                        .And()
                        .E(w => w
                            .NotExists(e => e
                                .Select().V(null)
                                .From(KrConstants.KrStageRoles.Name, "r").NoLock()
                                .Where().C("r", KrConstants.KrStageRoles.ID).Equals().C("t", KrConstants.KrSecondaryProcesses.ID))
                            .Or()
                            .Exists(e => e
                                .Select().V(null)
                                .From(KrConstants.KrStageRoles.Name, "r").NoLock()
                                .InnerJoin("RoleUsers", "ru").NoLock()
                                    .On().C("ru", "ID").Equals().C("r", KrConstants.KrStageRoles.RoleID)
                                .Where().C("r", KrConstants.KrStageRoles.ID).Equals().C("t", KrConstants.KrSecondaryProcesses.ID)
                                    .And().C("ru", "UserID").Equals().P("UserID")

                                .If(nestedContextID.HasValue, b => b
                                    .UnionAll()
                                    .Select().V(null)
                                    .From(KrConstants.KrStageRoles.Name, "r").NoLock()
                                    .InnerJoin("NestedRoles", "nr").NoLock()
                                        .On().C("nr", "ParentID").Equals().C("r", KrConstants.KrStageRoles.RoleID)
                                    .InnerJoin("RoleUsers", "ru").NoLock()
                                        .On().C("ru", "ID").Equals().C("nr", "ID")
                                    .Where().C("r", KrConstants.KrStageRoles.ID).Equals().C("t", KrConstants.KrSecondaryProcesses.ID)
                                        .And().C("ru", "UserID").Equals().P("UserID")
                                        .And().C("nr", "ContextID").Equals().P("ContextID"))))
                        .Build());

                return await db
                    .SetCommand(
                        query,
                        db.Parameter("UserID", this.session.User.ID),
                        db.Parameter("TypeID", typeID),
                        db.Parameter("StateID", stateID),
                        db.Parameter("ContextID", nestedContextID))
                    .LogCommand()
                    .ExecuteListAsync<Guid>(cancellationToken);
            }
        }

        /// <summary>
        /// Выполняет проверку дополнительных условий видимости.
        /// </summary>
        /// <param name="button"><inheritdoc cref="IKrProcessButton" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="IKrProcessButtonVisibilityEvaluatorContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если дополнительные условия истинны или не заданы, иначе - <see langword="false"/>.</returns>
        private async ValueTask<bool> EvaluateVisibilityAsync(
            IKrProcessButton button,
            IKrProcessButtonVisibilityEvaluatorContext context)
        {
            if (!await this.EvaluateVisibilityByConditionsAsync(button, context)
                || !context.ValidationResult.IsSuccessful())
            {
                return false;
            }

            if (!await this.EvaluateVisibilityBySourceConditionAsync(button, context)
                || !context.ValidationResult.IsSuccessful())
            {
                return false;
            }

            if (!await this.EvaluateVisibilityBySqlConditionAsync(button, context)
                || !context.ValidationResult.IsSuccessful())
            {
                return false;
            }

            if (!await this.EvaluateVisibilityByHandlersAsync(button, context)
                || !context.ValidationResult.IsSuccessful())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Выполняет проверку условного выражения дополнительной настройки видимости тайла вторичного процесса работающего в режиме "Кнопка".
        /// </summary>
        /// <param name="button"><inheritdoc cref="IKrProcessButton" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="IKrProcessButtonVisibilityEvaluatorContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если условное выражение дополнительной настройки видимости истинно или не задано, иначе - <see langword="false"/>.</returns>
        private async ValueTask<bool> EvaluateVisibilityBySourceConditionAsync(
            IKrProcessButton button,
            IKrProcessButtonVisibilityEvaluatorContext context)
        {
            var source = button.VisibilitySourceCondition;

            if (string.IsNullOrWhiteSpace(source))
            {
                return true;
            }

            var compilationResult = await this.compilationCache.GetAsync(
                button.ID,
                cancellationToken: context.CancellationToken);

            var instance = compilationResult.TryCreateKrScriptInstance(
                KrCompilersHelper.FormatClassName(
                    SourceIdentifiers.KrVisibilityClass,
                    SourceIdentifiers.SecondaryProcessAlias,
                    button.ID),
                context.ValidationResult);

            if (!context.ValidationResult.IsSuccessful())
            {
                return false;
            }

            if (instance is null)
            {
                return true;
            }

            this.InitializeInstance(
                instance,
                button,
                context);

            try
            {
                return await instance.RunVisibilityAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                var text = KrErrorHelper.ButtonVisibilityError(
                    button.Name,
                    e.Message);
                ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .ErrorDetails(text, source)
                    .ErrorException(e)
                    .End();

                return false;
            }
        }

        /// <summary>
        /// Выполняет проверку SQL-условия выражения дополнительной настройки видимости тайла вторичного процесса работающего в режиме "Кнопка".
        /// </summary>
        /// <param name="button"><inheritdoc cref="IKrProcessButton" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="IKrProcessButtonVisibilityEvaluatorContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если SQL-условие выражения дополнительной настройки видимости истинно или не задано, иначе - <see langword="false"/>.</returns>
        private async ValueTask<bool> EvaluateVisibilityBySqlConditionAsync(
            IKrProcessButton button,
            IKrProcessButtonVisibilityEvaluatorContext context)
        {
            var sqlText = button.VisibilitySqlCondition;

            if (string.IsNullOrWhiteSpace(sqlText))
            {
                return true;
            }

            try
            {
                var ctx = new KrSqlExecutorContext(
                    sqlText,
                    context.ValidationResult,
                    (_, txt, args) =>
                        KrErrorHelper.ButtonSqlVisibilityError(
                            button.Name,
                            txt,
                            args),
                    button,
                    context.Card?.ID,
                    context.CardType?.ID,
                    context.DocTypeID,
                    context.State,
                    cancellationToken: context.CancellationToken);

                return await this.sqlExecutor.ExecuteConditionAsync(ctx);
            }
            catch (QueryExecutionException qee)
            {
                var validator = ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .ErrorDetails(qee.ErrorMessageText, qee.SourceText);

                if (qee.InnerException is not null)
                {
                    validator.ErrorException(qee.InnerException);
                }

                validator.End();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                var text = KrErrorHelper.ButtonSqlVisibilityError(
                    button.Name,
                    e.Message);
                ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .ErrorDetails(text, sqlText)
                    .ErrorException(e)
                    .End();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Выполняет проверку условий видимости тайла вторичного процесса, работающего в режиме "Кнопка".
        /// </summary>
        /// <param name="button"><inheritdoc cref="IKrProcessButton" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="IKrProcessButtonVisibilityEvaluatorContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если условия видимости истинны или не заданы, иначе - <see langword="false"/>.</returns>
        private async ValueTask<bool> EvaluateVisibilityByConditionsAsync(
            IKrProcessButton button,
            IKrProcessButtonVisibilityEvaluatorContext context)
        {
            if (button.Conditions is null)
            {
                return true;
            }

            var conditionContext =
                new ConditionContext(
                    context.Card?.ID ?? Guid.Empty,
                    (ct) => context.MainCardAccessStrategy.GetCardAsync(cancellationToken: ct),
                    (context.CardContext as CardStoreExtensionContext)?.Request.TryGetCard(),
                    this.dbScope,
                    this.session,
                    context.ValidationResult,
                    this.unityContainer)
                {
                    CancellationToken = context.CancellationToken,
                };

            await using (this.dbScope.Create())
            {
                return await
                    this.conditionExecutor.CheckConditionAsync(
                        button.Conditions,
                        conditionContext);
            }
        }

        /// <summary>
        /// Выполняет проверку в соответствии с заданным обработчиком тайла вторичного процесса, работающего в режиме "Кнопка".
        /// </summary>
        /// <param name="button"><inheritdoc cref="IKrProcessButton" path="/summary"/></param>
        /// <param name="context"><inheritdoc cref="IKrProcessButtonVisibilityEvaluatorContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если обработчик разрешает отображение тайла или не задан, иначе - <see langword="false"/>.</returns>
        private ValueTask<bool> EvaluateVisibilityByHandlersAsync(
            IKrProcessButton button,
            IKrProcessButtonVisibilityEvaluatorContext context,
            CancellationToken cancellationToken = default)
        {
            if (!button.HandlerID.HasValue)
            {
                return ValueTask.FromResult(true);
            }

            var handler = this.krSecondaryProcessTileHandlerResolver.TryResolve(button.HandlerID.Value);
            if (handler is null)
            {
                return ValueTask.FromResult(true);
            }

            return handler.CheckVisibilityAsync(
                button,
                context.Card,
                cancellationToken);
        }

        private void InitializeInstance(
            IKrScript instance,
            IKrProcessButton button,
            IKrProcessButtonVisibilityEvaluatorContext context)
        {
            instance.MainCardAccessStrategy = context.MainCardAccessStrategy;
            instance.CardID = context.Card?.ID ?? Guid.Empty;
            instance.CardType = context.CardType;
            instance.DocTypeID = context.DocTypeID ?? Guid.Empty;
            if (context.KrComponents.HasValue)
            {
                instance.KrComponents = context.KrComponents.Value;
            }

            instance.SecondaryProcess = button;
            instance.CardContext = context.CardContext;
            instance.ValidationResult = context.ValidationResult;
            instance.Session = this.session;
            instance.DbScope = this.dbScope;
            instance.UnityContainer = this.unityContainer;
            instance.CardMetadata = this.cardMetadata;
            instance.KrScope = this.scope;
            instance.CardCache = this.cardCache;
            instance.KrTypesCache = this.typesCache;
            instance.StageSerializer = this.stageSerializer;
        }

        #endregion
    }
}
