using System;
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
using Tessa.Platform;
using Tessa.Platform.Conditions;
using Tessa.Platform.Data;
using Tessa.Platform.RefGroups;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Tessa.Roles.NestedRoles;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <inheritdoc cref="IKrSecondaryProcessExecutionEvaluator"/>
    public sealed class KrSecondaryProcessExecutionEvaluator(
        IKrSecondaryProcessCompilationCache compilationCache,
        IKrSqlExecutor sqlExecutor,
        ISession session,
        IDbScope dbScope,
        ICardMetadata cardMetadata,
        IUnityContainer unityContainer,
        IKrScope scope,
        ICardCache cardCache,
        IKrTypesCache krTypesCache,
        IContextRoleManager contextRoleManager,
        ICardContextRoleCache contextRoleCache,
        IKrStageSerializer stageSerializer,
        IConditionExecutor conditionExecutor,
        INestedRoleContextSelector nestedRoleContextSelector)
        : IKrSecondaryProcessExecutionEvaluator
    {
        #region Fields

        private readonly IKrSecondaryProcessCompilationCache compilationCache = NotNullOrThrow(compilationCache);

        private readonly IKrSqlExecutor sqlExecutor = NotNullOrThrow(sqlExecutor);

        private readonly ISession session = NotNullOrThrow(session);

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);

        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);

        private readonly IUnityContainer unityContainer = NotNullOrThrow(unityContainer);

        private readonly IKrScope scope = NotNullOrThrow(scope);

        private readonly ICardCache cardCache = NotNullOrThrow(cardCache);

        private readonly IKrTypesCache krTypesCache = NotNullOrThrow(krTypesCache);

        private readonly IContextRoleManager contextRoleManager = NotNullOrThrow(contextRoleManager);

        private readonly ICardContextRoleCache contextRoleCache = NotNullOrThrow(contextRoleCache);

        private readonly IKrStageSerializer stageSerializer = NotNullOrThrow(stageSerializer);

        private readonly IConditionExecutor conditionExecutor = NotNullOrThrow(conditionExecutor);

        private readonly INestedRoleContextSelector nestedRoleContextSelector = NotNullOrThrow(nestedRoleContextSelector);

        #endregion

        #region IKrSecondaryProcessExecutionEvaluator Members

        /// <inheritdoc />
        public async Task<bool> EvaluateAsync(
            IKrSecondaryProcessEvaluatorContext context)
        {
            var process = context.SecondaryProcess;

            if (!RunOnce(process, this.scope))
            {
                return false;
            }

            if (process is KrPureProcess { CheckRecalcRestrictions: false })
            {
                return true;
            }

            await using (this.dbScope.Create())
            {
                var restrictionsResult = context.CardID.HasValue
                    ? await this.CheckPermissionLocalContextAsync(context)
                    : await this.CheckPermissionGlobalContextAsync(context.SecondaryProcess.ID, context.CancellationToken);

                if (!restrictionsResult)
                {
                    return false;
                }

                return await this.CheckConditionsAsync(context)
                    && await this.RunScriptAsync(context)
                    && await this.RunSqlAsync(context);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Выполняет сценарий условия возможности выполнения процесса.
        /// </summary>
        /// <param name="context">Контекст содержащий информацию о запускаемом вторичном процессе.</param>
        /// <returns>Значение <see langword="true"/>, если условие выполняется или не задано, иначе - <see langword="false"/>.</returns>
        private async ValueTask<bool> RunScriptAsync(
            IKrSecondaryProcessEvaluatorContext context)
        {
            var script = context.SecondaryProcess.ExecutionSourceCondition;

            if (string.IsNullOrWhiteSpace(script))
            {
                return true;
            }

            var compilationResult = await this.compilationCache.GetAsync(
                context.SecondaryProcess.ID,
                cancellationToken: context.CancellationToken);

            var instance = compilationResult.TryCreateKrScriptInstance(
                KrCompilersHelper.FormatClassName(
                    SourceIdentifiers.KrExecutionClass,
                    SourceIdentifiers.SecondaryProcessAlias,
                    context.SecondaryProcess.ID),
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
                context);

            try
            {
                return await instance.RunExecutionAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                var text = KrErrorHelper.SecondaryProcessExecutionError(
                    context.SecondaryProcess.Name,
                    e.Message);
                ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .ErrorDetails(text, script)
                    .ErrorException(e)
                    .End();

                return false;
            }
        }

        /// <summary>
        /// Выполняет SQL-сценарий условия возможности выполнения процесса.
        /// </summary>
        /// <param name="context">Контекст содержащий информацию о запускаемом вторичном процессе.</param>
        /// <returns>Значение <see langword="true"/>, если условие выполняется или не задано, иначе - <see langword="false"/>.</returns>
        private async Task<bool> RunSqlAsync(
            IKrSecondaryProcessEvaluatorContext context)
        {
            var sql = context.SecondaryProcess.ExecutionSqlCondition;

            if (string.IsNullOrWhiteSpace(sql))
            {
                return true;
            }

            try
            {
                var ctx = new KrSqlExecutorContext(
                    sql,
                    context.ValidationResult,
                    (c, txt, args) =>
                        KrErrorHelper.SecondaryProcessSqlExecutionError(
                            context.SecondaryProcess.Name,
                            txt,
                            args),
                    context.SecondaryProcess,
                    context.CardID,
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
                if (qee.InnerException != null)
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
                var text = KrErrorHelper.SecondaryProcessSqlExecutionError(
                    context.SecondaryProcess.Name,
                    e.Message);
                ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .ErrorDetails(text, sql)
                    .ErrorException(e)
                    .End();
            }

            return false;
        }

        /// <summary>
        /// Выполняет условия выполнения процесса.
        /// </summary>
        /// <param name="context">Контекст содержащий информацию о запускаемом вторичном процессе.</param>
        /// <returns>Значение <see langword="true"/>, если условия выполняются или не заданы, иначе - <see langword="false"/>.</returns>
        private async ValueTask<bool> CheckConditionsAsync(
            IKrSecondaryProcessEvaluatorContext context)
        {
            if (context.SecondaryProcess is not KrProcessButton process
                || process.Conditions is null)
            {
                return true;
            }

            var conditionContext =
                new ConditionContext(
                    context.CardID ?? Guid.Empty,
                    (ct) => context.MainCardAccessStrategy.GetCardAsync(cancellationToken: ct),
                    context.CardContext is CardStoreExtensionContext storeContext ? storeContext.Request.Card : null,
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
                        process.Conditions,
                        conditionContext);
            }
        }

        private async Task<bool> CheckPermissionGlobalContextAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var userID = this.session.User.ID;

            var db = this.dbScope.Db;
            var builder = this.dbScope.BuilderFactory
                .Select().Top(1).V(null)
                .From(KrConstants.KrSecondaryProcesses.Name, "t").NoLock()
                .Where()
                .C("t", KrConstants.KrSecondaryProcesses.ID).Equals().P("ID")
                .And()
                .C("t", KrConstants.KrSecondaryProcesses.IsGlobal).Equals().V(true)
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
                        .And().C("ru", "UserID").Equals().P("UserID")))
                .Limit(1);

            return await db
                .SetCommand(
                    builder.Build(),
                    db.Parameter("UserID", userID),
                    db.Parameter("ID", id))
                .LogCommand()
                .ExecuteNonQueryAsync(cancellationToken) != 0;
        }

        private async Task<bool> CheckPermissionLocalContextAsync(
            IKrSecondaryProcessEvaluatorContext context)
        {
            var userID = this.session.User.ID;
            var nestedContextID = context.CardID.HasValue
                ? await this.nestedRoleContextSelector.GetContextAsync(context.CardID.Value, context.CancellationToken)
                : null;

            var db = this.dbScope.Db;
            var builder = this.dbScope.BuilderFactory
                .Select().Top(1).V(true)
                .From(KrConstants.KrSecondaryProcesses.Name, "t").NoLock()
                .LeftJoin(KrConstants.KrStageTypes.Name, "tt").NoLock()
                .On().C("tt", KrConstants.KrStageTypes.ID).Equals().C("t", KrConstants.KrSecondaryProcesses.ID)
                .LeftJoin(KrConstants.KrStageDocStates.Name, "tds").NoLock()
                .On().C("tds", KrConstants.KrStageDocStates.ID).Equals().C("t", KrConstants.KrSecondaryProcesses.ID)
                .Where()
                .C("t", KrConstants.KrSecondaryProcesses.ID).Equals().P("ID").N()
                .And()
                .C(KrConstants.KrSecondaryProcesses.IsGlobal).Equals().V(false).N()
                .And()
                .E(w => w
                    .C("tt", KrConstants.KrStageTypes.TypeID).IsNull()
                    .Or().N()
                    .C("tt", KrConstants.KrStageTypes.TypeID).Equals().P("TypeID")
                    .Or()
                    .Exists(b => b
                        .Select().V(null)
                        .From(RefGroupsHelper.RefGroupsGuidValuesTable, "v").NoLock()
                        .Where().C("v", RefGroupsHelper.RefGroupsValuesID).Equals().C("tt", KrConstants.KrStageTypes.TypeID)
                        .And().C("v", RefGroupsHelper.RefGroupsValuesValue).Equals().P("TypeID"))).N()
                .And()
                .E(w => w
                    .C("tds", KrConstants.KrStageDocStates.StateID).IsNull().N()
                    .Or()
                    .C("tds", KrConstants.KrStageDocStates.StateID).Equals().P("StateID")
                    .Or()
                    .Exists(b => b
                        .Select().V(null)
                        .From(RefGroupsHelper.RefGroupsMainSectionName, "ref").NoLock()
                        .InnerJoin(RefGroupsHelper.RefGroupsIntValuesTable, "v").NoLock()
                        .On().C("ref", RefGroupsHelper.RefGroupsID).Equals().C("v", RefGroupsHelper.RefGroupsValuesID)
                        .Where().C("ref", RefGroupsHelper.RefGroupsIntID).Equals().C("tds", KrConstants.KrStageDocStates.StateID)
                        .And().C("v", RefGroupsHelper.RefGroupsValuesValue).Equals().P("StateID"))).N()
                .And()
                .E(w => w
                    .NotExists(e => e
                        .Select().V(null)
                        .From(KrConstants.KrStageRoles.Name, "r").NoLock()
                        .Where().C("r", KrConstants.KrStageRoles.ID).Equals().C("t", KrConstants.KrSecondaryProcesses.ID)).N()
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
                .Limit(1);

            var result = await db
                .SetCommand(
                    builder.Build(),
                    db.Parameter("UserID", userID),
                    db.Parameter("ID", context.SecondaryProcess.ID),
                    db.Parameter("TypeID", context.DocTypeID ?? context.CardType?.ID ?? Guid.Empty),
                    db.Parameter("StateID", context.State?.ID ?? KrState.Draft.ID),
                    db.Parameter("ContextID", nestedContextID))
                .LogCommand()
                .ExecuteAsync<bool>(context.CancellationToken);
            if (!result)
            {
                return false;
            }

            builder = this.dbScope.BuilderFactory
                .Select().Top(1).V(true)
                .From(KrConstants.KrSecondaryProcesses.Name, "t").NoLock()
                .Where()
                .C("t", KrConstants.KrSecondaryProcesses.ID).Equals().P("ID")
                .And()
                .E(w => w
                    .NotExists(e => e
                        .Select().V(null)
                        .From(KrConstants.KrSecondaryProcessRoles.Name, "r").NoLock()
                        .Where().C("r", KrConstants.KrSecondaryProcessRoles.ID).Equals().C("t", KrConstants.KrSecondaryProcesses.ID))
                    .Or()
                    .Exists(e => e
                        .Select().V(null)
                        .From(KrConstants.KrSecondaryProcessRoles.Name, "r").NoLock()
                        .InnerJoin("RoleUsers", "ru").NoLock()
                        .On().C("ru", "ID").Equals().C("r", KrConstants.KrSecondaryProcessRoles.RoleID)
                        .Where().C("r", KrConstants.KrSecondaryProcessRoles.ID).Equals().C("t", KrConstants.KrSecondaryProcesses.ID)
                        .And().C("ru", "UserID").Equals().P("UserID")
                        .If(nestedContextID.HasValue, b => b
                            .UnionAll()
                            .Select().V(null)
                            .From(KrConstants.KrSecondaryProcessRoles.Name, "r").NoLock()
                            .InnerJoin("NestedRoles", "nr").NoLock()
                            .On().C("nr", "ParentID").Equals().C("r", KrConstants.KrSecondaryProcessRoles.RoleID)
                            .InnerJoin("RoleUsers", "ru").NoLock()
                            .On().C("ru", "ID").Equals().C("nr", "ID")
                            .Where().C("r", KrConstants.KrSecondaryProcessRoles.ID).Equals().C("t", KrConstants.KrSecondaryProcesses.ID)
                            .And().C("ru", "UserID").Equals().P("UserID")
                            .And().C("nr", "ContextID").Equals().P("ContextID"))))
                .Limit(1);
            result = await db
                .SetCommand(
                    builder.Build(),
                    db.Parameter("UserID", userID),
                    db.Parameter("ID", context.SecondaryProcess.ID),
                    db.Parameter("ContextID", nestedContextID))
                .LogCommand()
                .ExecuteAsync<bool>(context.CancellationToken);

            if (result)
            {
                return true;
            }

            foreach (var roleID in context.SecondaryProcess.ContextRolesIDs)
            {
                var contextRole = await this.contextRoleCache.GetContextRoleAsync(roleID, context.CancellationToken);
                var userInRole = await this.contextRoleManager.CheckUserInCardContextAsync(
                    contextRole,
                    context.CardID ?? Guid.Empty,
                    userID,
                    cancellationToken: context.CancellationToken);

                if (userInRole)
                {
                    return true;
                }
            }

            return false;
        }

        private void InitializeInstance(
            IKrScript instance,
            IKrSecondaryProcessEvaluatorContext context)
        {
            instance.MainCardAccessStrategy = context.MainCardAccessStrategy;
            instance.CardID = context.CardID ?? Guid.Empty;
            instance.CardType = context.CardType;
            instance.DocTypeID = context.DocTypeID ?? Guid.Empty;

            if (context.KrComponents.HasValue)
            {
                instance.KrComponents = context.KrComponents.Value;
            }

            instance.SetContextualSatellite(context.ContextualSatellite);
            instance.SecondaryProcess = context.SecondaryProcess;
            instance.CardContext = context.CardContext;
            instance.ValidationResult = context.ValidationResult;
            instance.Session = this.session;
            instance.DbScope = this.dbScope;
            instance.UnityContainer = this.unityContainer;
            instance.CardMetadata = this.cardMetadata;
            instance.KrScope = this.scope;
            instance.CardCache = this.cardCache;
            instance.KrTypesCache = this.krTypesCache;
            instance.StageSerializer = this.stageSerializer;
        }

        /// <summary>
        /// Возвращает <c>true</c>, если одиночный запуск не требуется или скрипт запускается в первый раз.
        /// <c>false</c>, если скрипт запускается повторно, хотя требуется один раз.
        /// </summary>
        /// <param name="secondaryProcess"></param>
        /// <param name="krScope"></param>
        /// <returns></returns>
        public static bool RunOnce(IKrSecondaryProcess secondaryProcess, IKrScope krScope)
        {
            if (krScope is null
                || secondaryProcess is null
                || !secondaryProcess.RunOnce)
            {
                return true;
            }

            var info = krScope.Info;
            var key = FormatKey(secondaryProcess);
            if (info.ContainsKey(key))
            {
                return false;
            }

            info[key] = BooleanBoxes.True;
            return true;
        }

        private static string FormatKey(
            IKrSecondaryProcess secondaryProcess) =>
            $"RunExecutionScriptOnce_{secondaryProcess.ID:N}";

        #endregion
    }
}
