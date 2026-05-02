#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Numbers;
using Tessa.Extensions.Default.Server.Normalization;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Serialization;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine;
using Tessa.Normalization;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Scheme;
using Tessa.Workflow;
using Tessa.Workflow.Actions;
using Tessa.Workflow.Compilation;
using Tessa.Workflow.Helpful;
using Tessa.Workflow.Normalization;
using Tessa.Workflow.Signals;
using Tessa.Workflow.Storage;
using static Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine.WorkflowConstants;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    /// <summary>
    /// Обработчик действия <see cref="KrDescriptors.KrTaskRegistrationDescriptor"/>.
    /// </summary>
    public sealed class KrTaskRegistrationAction(
        IWorkflowTaskActionDeps deps,
        INumberDirectorContainer numberDirectorContainer,
        IKrStageSerializer krStageSerializer,
        IKrWorkflowStateStrategy stateStrategy)
        : KrWorkflowTaskActionBase(KrDescriptors.KrTaskRegistrationDescriptor, deps)
    {
        #region Fields

        /// <summary>
        /// Имя ключа, по которому в параметрах действия содержится идентификатор обрабатываемого задания данным экземпляром действия. Тип значения: <see cref="Guid"/>.
        /// </summary>
        private const string TaskParamKey = StorageHelper.SystemKeyPrefix + "TaskID";

        private const string MainSectionName = KrTaskRegistrationActionVirtual.SectionName;

        private static readonly Guid taskTypeID = DefaultTaskTypes.KrRegistrationTypeID;

        /// <summary>
        /// Значение по умолчанию для параметра "Длительность, рабочие дни". Данное значение используется,
        /// только если в схеме не указано значение по умолчанию для поля <see cref="KrTaskRegistrationActionVirtual.Period"/>.
        /// </summary>
        private const double PeriodInDaysDefaultValue = 1d;

        /// <summary>
        /// Массив типов обрабатываемых сигналов.
        /// </summary>
        private static readonly string[] signalTypes =
        [
            WorkflowSignalTypes.CompleteTask,
            WorkflowSignalTypes.DeleteTask,
            WorkflowSignalTypes.UpdateTask,
        ];

        private readonly INumberDirectorContainer numberDirectorContainer = NotNullOrThrow(numberDirectorContainer);
        private readonly IKrStageSerializer krStageSerializer = NotNullOrThrow(krStageSerializer);
        private readonly IKrWorkflowStateStrategy stateStrategy = NotNullOrThrow(stateStrategy);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override bool Compile(
            IWorkflowCompilationSyntaxTreeBuilder builder,
            WorkflowActionStorage action)
        {
            ThrowIfNull(builder);
            ThrowIfNull(action);

            if (base.Compile(builder, action))
            {
                CompileEvents(builder, action);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override void PrepareForExecute(WorkflowActionStateStorage actionState, IWorkflowEngineContext context)
        {
            ThrowIfNull(actionState);

            base.PrepareForExecute(actionState, context);
            actionState.Hash.Remove(KrTaskRegistrationActionOptionsVirtual.SectionName);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Схема выполнения действия:<para/>
        /// 1. Получаем ID привязанного к данному действию задания;<para/>
        /// 2. Если есть задание, то очищаем список переходов;<para/>
        /// 3. Если тип сигнала - default;<para/>
        /// 3.1. Если задания нет, создаем задание, создаем все необходимые подписки;<para/>
        /// 3.2. Если задание есть, игнорируем создание задания;<para/>
        /// 3.3. В любом случае очищаем список переходов;<para/>
        /// 4. Если тип сигнала из списка обрабатываемых типов сигналов;<para/>
        /// 4.1. Если задания нет, игнорируем;<para/>
        /// 4.2. Если задание есть, обрабатываем сигнал.<para/>
        /// </remarks>
        protected override async Task ExecuteAsync(IWorkflowEngineContext context, IWorkflowEngineCompiled? scriptObject)
        {
            await base.ExecuteAsync(context, scriptObject);

            var currentTaskID = context.ActionInstance!.Hash.TryGet<Guid?>(TaskParamKey);
            var signalType = context.Signal!.Type;

            if (currentTaskID.HasValue)
            {
                context.Links.Clear();
            }

            using (context.CreateTasksContext())
            {
                if (signalType == WorkflowSignalTypes.Default)
                {
                    if (!currentTaskID.HasValue)
                    {
                        await this.stateStrategy.SetStateIDAsync(
                            context,
                            KrState.Registration,
                            context.ValidationResult,
                            cancellationToken: context.CancellationToken);

                        await this.SendTaskAsync(context, scriptObject);

                        foreach (var newSignalType in signalTypes)
                        {
                            CreateSubscription(context, newSignalType);
                        }

                        SubscribeOnEvents(context);
                    }

                    context.Links.Clear();
                }
                else if (currentTaskID.HasValue)
                {
                    switch (signalType)
                    {
                        case WorkflowSignalTypes.CompleteTask:
                            await this.CompleteTaskAsync(context, scriptObject, currentTaskID.Value);
                            break;

                        case WorkflowSignalTypes.DeleteTask:
                            if (await this.DeleteTaskAsync(context, currentTaskID.Value))
                            {
                                WorkflowEngineHelper.Set<object>(context.ActionInstance.Hash, null, TaskParamKey);
                            }

                            break;

                        case WorkflowSignalTypes.UpdateTask:
                            await this.UpdateTaskAsync(context, currentTaskID.Value);
                            break;

                        default:
                            await this.PerformEvent(context, scriptObject, currentTaskID.Value);
                            break;
                    }
                }
            }
        }

        /// <inheritdoc/>
        protected override bool CheckActive(IWorkflowEngineContext context)
        {
            return context.ActionInstance!.Hash.TryGet<Guid?>(TaskParamKey).HasValue;
        }
        
        /// <inheritdoc/>
        protected override WorkflowNormalizationSettings GetNormalizationSettings()
        {
            return new WorkflowNormalizationSettings(
            [
                new WorkflowNormalizationSetting(
                    PlatformNormalizationSources.Roles,
                    KrTaskRegistrationActionVirtual.SectionName,
                    KrTaskRegistrationActionVirtual.Performer,
                    "Name"),
                new WorkflowNormalizationSetting(
                    PlatformNormalizationSources.Roles,
                    KrTaskRegistrationActionVirtual.SectionName,
                    KrTaskRegistrationActionVirtual.Author,
                    "Name"),
                new WorkflowNormalizationSetting(
                    DefaultNormalizationSources.TaskKinds,
                    KrTaskRegistrationActionVirtual.SectionName,
                    KrTaskRegistrationActionVirtual.Kind,
                    "Caption"),
                new WorkflowNormalizationSetting(
                    PlatformNormalizationSources.Roles,
                    NotificationRolesSectionName,
                    "Role",
                    "Name",
                    true)
            ]);
        }

        #endregion

        #region WorkflowTaskActionBase Overrides

        /// <inheritdoc/>
        protected override async Task<string?> GetResultAsync(
            IWorkflowEngineContext context,
            CardTask task)
        {
            return
                await GetWithPlaceholdersAsync(
                    context,
                    await context.GetAsync<string>(MainSectionName, KrTaskRegistrationActionVirtual.Result),
                    task);
        }

        /// <inheritdoc/>
        protected override async Task CompleteTaskCoreAsync(
            IWorkflowEngineContext context,
            CardTask task,
            Guid optionID,
            IWorkflowEngineCompiled? scriptObject)
        {
            if (optionID == DefaultCompletionOptions.RegisterDocument)
            {
                await this.RegisterAsync(context);
            }

            var optionRows = await context.GetAllRowsAsync(context.ActionTemplate!.Hash, KrTaskRegistrationActionOptionsVirtual.SectionName);

            if (optionRows?.Count > 0)
            {
                var processRows = optionRows
                    .Where(x => WorkflowEngineHelper.Get<Guid?>(x, ActionOptionsBase.Option, Names.Table_ID) == optionID);

                var linksForPerforming = new HashSet<Guid>();

                foreach (var row in processRows)
                {
                    var completionResult = row.TryGet<string>(ActionSeveralTaskTypesOptionsBase.Result);
                    var info = new WorkflowTaskNotificationInfo(
                        context,
                        row,
                        KrTaskRegistrationActionNotificationRolesVitrual.Option,
                        KrTaskRegistrationActionNotificationRolesVitrual.SectionName);

                    if (!string.IsNullOrWhiteSpace(completionResult))
                    {
                        if (this.BindingParser.IsBinding(completionResult))
                        {
                            completionResult = await this.BindingExecutor.GetAsync<string>(context, completionResult);
                        }

                        task.Result = await GetWithPlaceholdersAsync(
                            context,
                            completionResult,
                            task);
                    }

                    var rowID = WorkflowEngineHelper.Get<Guid>(row, Names.Table_RowID);
                    if (scriptObject is not null)
                    {
                        await scriptObject.ExecuteActionAsync(
                            KrWorkflowActionMethods.KrTaskRegistrationTaskOptionMethod.GetMethodName(row),
                            KrWorkflowActionMethods.KrTaskRegistrationTaskOptionMethod,
                            task,
                            task.Card.DynamicEntries, task.Card.DynamicTables,
                            info,
                            new List<WorkflowTaskNotificationInfo> { info });
                    }

                    var linkRows = context.ActionTemplate.Hash.TryGet<IList>(KrTaskRegistrationActionOptionLinksVirtual.SectionName);
                    var linkIDs =
                        linkRows?
                            .Cast<Dictionary<string, object?>>()
                            .Where(x => WorkflowEngineHelper.Get<Guid?>(
                                x, ActionOptionLinksBase.Option, Names.Table_RowID) == rowID)
                            .Select(x => WorkflowEngineHelper.Get<Guid>(
                                x, ActionOptionLinksBase.Link, Names.Table_ID))
                        ?? [];

                    await this.SendCompleteTaskNotificationAsync(
                        context,
                        scriptObject,
                        task,
                        info);

                    if (!context.Cancel)
                    {
                        foreach (var linkID in linkIDs)
                        {
                            linksForPerforming.Add(linkID);
                        }
                    }

                    context.Cancel = false;
                }

                foreach (var linkID in linksForPerforming)
                {
                    context.Links[linkID] = WorkflowEngineSignal.CreateDefaultSignal(StorageHelper.Clone(context.Signal!.Hash));
                }
            }

            if (task.State == CardRowState.Deleted)
            {
                WorkflowEngineHelper.Set<object>(context.ActionInstance!.Hash, null, TaskParamKey);
            }
        }

        /// <inheritdoc/>
        public override ValueTask<ValidationResult> ValidateAsync(
            WorkflowActionStorage action,
            WorkflowNodeStorage node,
            WorkflowProcessStorage process,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(action);
            ThrowIfNull(node);

            var validationResult = new ValidationResultBuilder();
            if (WorkflowEngineHelper.Get<object>(action.Hash, MainSectionName, KrTaskRegistrationActionVirtual.Performer, Names.Table_ID) is null)
            {
                validationResult.AddWarning(
                    this,
                    WorkflowEngineHelper.GetValidateFieldMessage(action, node, "$CardTypes_Controls_Role"));
            }

            return ValueTask.FromResult(validationResult.Build());
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Асинхронно отправляет задание.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="scriptObject"><inheritdoc cref="IWorkflowEngineCompiled" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        private async Task SendTaskAsync(IWorkflowEngineContext context, IWorkflowEngineCompiled? scriptObject)
        {
            var roleID = await context.GetAsync<Guid?>(
                MainSectionName,
                KrTaskRegistrationActionVirtual.Performer,
                Names.Table_ID);

            var roleName = await context.GetAsync<string>(
                MainSectionName,
                KrTaskRegistrationActionVirtual.Performer,
                Table_Field_Name);

            var digest = await context.GetAsync<string>(MainSectionName,
                KrTaskRegistrationActionVirtual.Digest);

            var periodInDays = (double?) (await context.GetAsync<object>(MainSectionName, KrTaskRegistrationActionVirtual.Period)
                    ?? (await context.CardMetadata.GetSectionsAsync(context.CancellationToken))[MainSectionName]
                    .Columns[KrTaskRegistrationActionVirtual.Period]
                    .DefaultValue)
                ?? PeriodInDaysDefaultValue;
            var planned = await context.GetAsync<DateTime?>(MainSectionName, KrTaskRegistrationActionVirtual.Planned);

            var parentTaskRowID = context.Signal!.As<WorkflowEngineTaskSignal>().ParentTaskRowID;
            var authorID = await context.GetAsync<Guid?>(MainSectionName, KrTaskRegistrationActionVirtual.Author, Names.Table_ID);

            var kindID = await context.GetAsync<Guid?>(MainSectionName, KrTaskRegistrationActionVirtual.Kind, Names.Table_ID);
            var kindCaption = await context.GetAsync<string>(MainSectionName, KrTaskRegistrationActionVirtual.Kind, Table_Field_Caption);

            digest = await GetWithPlaceholdersAsync(
                context,
                digest,
                context.Task);

            var cardTask =
                await context.SendTaskAsync(
                    taskTypeID,
                    digest,
                    planned,
                    null,
                    periodInDays,
                    roleID,
                    roleName,
                    parentRowID: parentTaskRowID,
                    cancellationToken: context.CancellationToken);

            if (cardTask is null
                || !context.ValidationResult.IsSuccessful())
            {
                return;
            }

            context.ValidationResult.Add(WorkflowCommonHelper.SetTaskKind(cardTask, kindID, kindCaption, this));

            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            if (await context.GetAsync<bool?>(MainSectionName, KrTaskRegistrationActionVirtual.CanEditCard) == true)
            {
                cardTask.Settings ??= [];
                cardTask.Settings[WorkflowCommonConstants.CanEditCard] = BooleanBoxes.True;
            }

            if (await context.GetAsync<bool?>(MainSectionName, KrTaskRegistrationActionVirtual.CanEditAnyFiles) == true)
            {
                cardTask.Settings ??= [];
                cardTask.Settings[WorkflowCommonConstants.CanEditAnyFiles] = BooleanBoxes.True;
            }

            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            context.AddTaskToNextContextTasks(cardTask);

            authorID = await context.GetAuthorIDAsync(this.RoleGetStrategy, this.ContextRoleManager, this.ContextRoleCache, authorID);
            if (!authorID.HasValue)
            {
                return;
            }

            cardTask.AddAuthor(authorID.Value);

            if (scriptObject is not null)
            {
                await scriptObject.ExecuteActionAsync(
                    KrWorkflowActionMethods.KrTaskRegistrationTaskInitMethod.MethodName,
                    KrWorkflowActionMethods.KrTaskRegistrationTaskInitMethod,
                    cardTask,
                    cardTask.Card.DynamicEntries,
                    cardTask.Card.DynamicTables);
            }

            if (cardTask.TaskAssignedRoles.All(p => p.TaskRoleID != CardFunctionRoles.PerformerID))
            {
                context.ValidationResult.AddError(
                    this,
                    WorkflowEngineHelper.GetValidateFieldMessage(context.ActionTemplate!, context.NodeTemplate!, "$CardTypes_Controls_Role"));

                return;
            }

            await this.SendStartTaskNotificationAsync(
                context,
                scriptObject,
                cardTask,
                MainSectionName,
                methodDescriptor: KrWorkflowActionMethods.KrTaskRegistrationTaskStartNotificationMethod);

            context.ActionInstance!.Hash[TaskParamKey] = cardTask.RowID;

            await context.AddActiveTaskAsync(
                cardTask.RowID,
                context.ValidationResult,
                context.CancellationToken);

            await context.AddToHistoryAsync(
                cardTask.RowID,
                WorkflowHelper.GetProcessCycle(context.ProcessInstance!.Hash),
                context.ValidationResult,
                cancellationToken: context.CancellationToken);
        }

        /// <summary>
        /// Регистрирует документ.
        /// </summary>
        /// <param name="context">Контекст обработчика.</param>
        /// <returns>Асинхронная операция.</returns>
        private async Task RegisterAsync(IWorkflowEngineContext context)
        {
            var mainCard = await context.GetMainCardAsync(context.CancellationToken);
            if (mainCard is null)
            {
                return;
            }

            var cardType = (await context.CardMetadata.GetCardTypesAsync(context.CancellationToken))[mainCard.TypeID];

            // Выделение номера при регистрации.
            var numberProvider = this.numberDirectorContainer.GetProvider(cardType.ID);
            var numberDirector = numberProvider.GetDirector();
            var numberComposer = numberProvider.GetComposer();
            var numberContext = await numberDirector.CreateContextAsync(
                numberComposer,
                mainCard,
                cardType,
                CardServiceType.Default,
                transactionMode: NumberTransactionMode.SeparateTransaction,
                cancellationToken: context.CancellationToken);

            await numberDirector.NotifyOnRegisteringCardAsync(numberContext, context.CancellationToken);
            context.ValidationResult.Add(numberContext.ValidationResult);

            if (context.ValidationResult.IsSuccessful())
            {
                // Состояние документа до изменения его этим действием.
                var oldStateID = this.stateStrategy.TryGetPreviousState(context);

                var sCard = await context.GetKrSatelliteAsync();

                if (sCard is null)
                {
                    return;
                }

                var info = ProcessInfoCacheHelper.Get(this.krStageSerializer, sCard);
                info[KrConstants.Keys.StateBeforeRegistration] = Int32Boxes.Box(oldStateID);
                ProcessInfoCacheHelper.Update(this.krStageSerializer, sCard);

                await this.stateStrategy.SetStateIDAsync(
                    context,
                    KrState.Registered,
                    context.ValidationResult,
                    cancellationToken: context.CancellationToken);

                // Сохранение состояния документа до его изменения этим действием.
                this.stateStrategy.StorePreviousState(context, oldStateID);
            }
        }

        #endregion
    }
}
