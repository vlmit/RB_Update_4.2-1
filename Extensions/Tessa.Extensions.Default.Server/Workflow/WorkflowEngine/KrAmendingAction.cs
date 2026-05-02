#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Normalization;
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
using static Tessa.Extensions.Default.Shared.Workflow.KrProcess.KrConstants;
using static Tessa.Extensions.Default.Shared.Workflow.WorkflowEngine.WorkflowConstants;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    /// <summary>
    /// Обработчик действия <see cref="KrDescriptors.KrAmendingDescriptor"/>.
    /// </summary>
    public sealed class KrAmendingAction(IWorkflowTaskActionDeps deps, IKrWorkflowStateStrategy stateStrategy)
        : KrWorkflowTaskActionBase(KrDescriptors.KrAmendingDescriptor, deps)
    {
        #region Fields

        /// <summary>
        /// Имя ключа, по которому в параметрах действия содержится идентификатор обрабатываемого задания данным экземпляром действия. Тип значения: <see cref="Guid"/>.
        /// </summary>
        private const string TaskParamKey = StorageHelper.SystemKeyPrefix + "TaskID";

        private const string MainSectionName = KrAmendingActionVirtual.SectionName;

        private static readonly Guid taskTypeID = DefaultTaskTypes.KrEditTypeID;

        /// <summary>
        /// Значение по умолчанию для параметра "Длительность, рабочие дни". Данное значение используется только, если в схеме не указано значение по умолчанию для поля <see cref="KrAmendingActionVirtual.Period"/>.
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

        #endregion

        #region Fields

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
        /// <remarks>
        /// Схема выполнения действия задания:<para/>
        /// 1. Получаем ID привязанного к данному действию заданию<para/>
        /// 2. Если есть задание, то очищаем список переходов<para/>
        /// 3. Если тип сигнала - default<para/>
        /// 3.1. Если задания нет, создаем задание, создаем все необходимые подписки<para/>
        /// 3.2. Если задание есть, игнорируем создание задания<para/>
        /// 3.3. В любом случае очищаем список переходов<para/>
        /// 4. Если тип сигнала из списка обрабатываемых типов сигналов<para/>
        /// 4.1. Если задания нет, игнорируем<para/>
        /// 4.2. Если задание есть, обрабатываем сигнал<para/>
        /// 4.3. Если по окончанию обработки задания оно все еще есть, ставим KeepAlive = true.
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
                        if (await context.GetAsync<bool?>(MainSectionName, KrAmendingActionVirtual.IsIncrementCycle) == true)
                        {
                            WorkflowHelper.ProcessCycleIncrement(context.ProcessInstance!.Hash);
                        }

                        if (await context.GetAsync<bool?>(MainSectionName, KrAmendingActionVirtual.IsChangeState) == true)
                        {
                            await this.stateStrategy.SetStateIDAsync(
                                context,
                                KrState.Editing,
                                context.ValidationResult,
                                context.CancellationToken);
                        }

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
        protected override bool CheckActive(IWorkflowEngineContext context) => context.ActionInstance!.Hash.TryGet<Guid?>(TaskParamKey).HasValue;

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
            if (WorkflowEngineHelper.Get<object>(action.Hash, MainSectionName, KrAmendingActionVirtual.Role, Names.Table_ID) is null)
            {
                validationResult.AddWarning(
                    this,
                    WorkflowEngineHelper.GetValidateFieldMessage(action, node, "$CardTypes_Controls_Role"));
            }

            return ValueTask.FromResult(validationResult.Build());
        }
        
        /// <inheritdoc/>
        protected override WorkflowNormalizationSettings GetNormalizationSettings()
        {
            return new WorkflowNormalizationSettings(
            [
                new WorkflowNormalizationSetting(
                    PlatformNormalizationSources.Roles,
                    KrAmendingActionVirtual.SectionName,
                    KrAmendingActionVirtual.Role,
                    "Name"),
                new WorkflowNormalizationSetting(
                    PlatformNormalizationSources.Roles,
                    KrAmendingActionVirtual.SectionName,
                    KrAmendingActionVirtual.Author,
                    "Name"),
                new WorkflowNormalizationSetting(
                    DefaultNormalizationSources.TaskKinds,
                    KrAmendingActionVirtual.SectionName,
                    KrAmendingActionVirtual.Kind,
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
            CardTask task) =>
            await GetWithPlaceholdersAsync(
                context,
                await context.GetAsync<string>(MainSectionName, KrAmendingActionVirtual.Result),
                task);

        /// <inheritdoc/>
        protected override async Task CompleteTaskCoreAsync(
            IWorkflowEngineContext context,
            CardTask task,
            Guid optionID,
            IWorkflowEngineCompiled? scriptObject)
        {
            var isCompleted = await CompleteTaskPredefinedActionProcessingAsync(
                context,
                task,
                optionID);

            var info = new WorkflowTaskNotificationInfoBase(
                async () =>
                {
                    List<Guid> roleList;
                    var notificationRows = await context.GetAllRowsAsync(NotificationRolesSectionName);
                    if (notificationRows is null)
                    {
                        roleList = [];
                    }
                    else
                    {
                        roleList = new List<Guid>(notificationRows.Count);
                        foreach (var roleRow in notificationRows)
                        {
                            var roleID = WorkflowEngineHelper.Get<Guid?>(roleRow, "Role", "ID");
                            if (roleID.HasValue)
                            {
                                roleList.Add(roleID.Value);
                            }
                        }
                    }

                    return roleList;
                })
            {
                NotificationID = await context.GetAsync<Guid?>(MainSectionName, KrAmendingActionVirtual.CompleteOptionNotification, Names.Table_ID),
                SendToAuthor = await context.GetAsync<bool?>(MainSectionName, KrAmendingActionVirtual.CompleteOptionSendToAuthor) ?? default,
                SendToPerformer = await context.GetAsync<bool?>(MainSectionName, KrAmendingActionVirtual.CompleteOptionSendToPerformer) ?? default,
                ExcludeDeputies = await context.GetAsync<bool?>(MainSectionName, KrAmendingActionVirtual.CompleteOptionExcludeDeputies) ?? default,
                ExcludeSubscribers = await context.GetAsync<bool?>(MainSectionName, KrAmendingActionVirtual.CompleteOptionExcludeSubscribers) ?? default,
            };

            if (scriptObject is not null)
            {
                await scriptObject.ExecuteActionAsync(
                    KrWorkflowActionMethods.KrAmendingOptionMethod.MethodName,
                    KrWorkflowActionMethods.KrAmendingOptionMethod,
                    task,
                    task.Card.DynamicEntries,
                    task.Card.DynamicTables,
                    info);
            }

            await this.SendCompleteTaskNotificationAsync(
                context,
                scriptObject,
                task,
                info,
                KrWorkflowActionMethods.KrAmendingCompleteNotificationMethod.MethodName,
                KrWorkflowActionMethods.KrAmendingCompleteNotificationMethod);

            if (isCompleted
                && !context.Cancel)
            {
                context.Links[Guid.Empty] = WorkflowEngineSignal.CreateDefaultSignal(StorageHelper.Clone(context.Signal!.Hash));
            }

            context.Cancel = false;

            if (task.State == CardRowState.Deleted)
            {
                WorkflowEngineHelper.Set<object>(context.ActionInstance!.Hash, null, TaskParamKey);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Асинхронно отправляет задание.
        /// </summary>
        /// <param name="context">Контекст обработчика.</param>
        /// <param name="scriptObject">Контекст скриптов.</param>
        /// <returns>Асинхронная задача.</returns>
        private async Task SendTaskAsync(IWorkflowEngineContext context, IWorkflowEngineCompiled? scriptObject)
        {
            var roleID = await context.GetAsync<Guid?>(
                MainSectionName,
                KrAmendingActionVirtual.Role,
                Names.Table_ID);

            var roleName = await context.GetAsync<string>(
                MainSectionName,
                KrAmendingActionVirtual.Role,
                Table_Field_Name);

            var digest = await context.GetAsync<string>(
                MainSectionName,
                KrAmendingActionVirtual.Digest);

            var periodInDays = (double?) (await context.GetAsync<object>(
                        MainSectionName,
                        KrAmendingActionVirtual.Period)
                    ?? (await context.CardMetadata.GetSectionsAsync(context.CancellationToken))[MainSectionName]
                    .Columns[KrAmendingActionVirtual.Period]
                    .DefaultValue)
                ?? PeriodInDaysDefaultValue;
            var planned = await context.GetAsync<DateTime?>(
                MainSectionName,
                KrAmendingActionVirtual.Planned);

            var parentTaskRowID = context.Signal!.As<WorkflowEngineTaskSignal>().ParentTaskRowID;
            var authorID = await context.GetAsync<Guid?>(
                MainSectionName,
                KrAmendingActionVirtual.Author,
                Names.Table_ID);

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

            var kindID = await context.GetAsync<Guid?>(
                MainSectionName,
                KrAmendingActionVirtual.Kind,
                Names.Table_ID);

            var kindCaption = await context.GetAsync<string>(
                MainSectionName,
                KrAmendingActionVirtual.Kind,
                Table_Field_Caption);

            context.ValidationResult.Add(WorkflowCommonHelper.SetTaskKind(cardTask, kindID, kindCaption, this));

            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            if (await context.GetAsync<bool?>(MainSectionName, KrAmendingActionVirtual.HasEditApprovalSchemeAccess) is true)
            {
                cardTask.Settings ??= new();
                cardTask.Settings[NamesKeys.CanEditApprovalScheme] = BooleanBoxes.True;
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
                    KrWorkflowActionMethods.KrAmendingInitMethod.MethodName,
                    KrWorkflowActionMethods.KrAmendingInitMethod,
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
                methodDescriptor: KrWorkflowActionMethods.KrAmendingStartNotificationMethod);

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
        /// Обрабатывает завершение заданий в соответствии с предопределённой логикой обработки.
        /// </summary>
        /// <param name="context">Контекст обработчика процесса.</param>
        /// <param name="task">Завершаемое задание.</param>
        /// <param name="optionID">
        /// Идентификатор варианта завершения с которым завершается задание. Может отличаться от <see cref="CardTask.OptionID"/> из <paramref name="task"/>,
        /// если он был изменён в скрипте, обрабатывающим завершение задания.</param>
        /// <returns>Значение <see langword="true"/>, если вариант завершения обработан и действие завершается, иначе - <see langword="false"/>.</returns>
        private static async ValueTask<bool> CompleteTaskPredefinedActionProcessingAsync(
            IWorkflowEngineContext context,
            CardTask task,
            Guid optionID)
        {
            if (optionID == DefaultCompletionOptions.NewApprovalCycle)
            {
                if (task.Card.Sections.TryGetValue(KrTask.Name, out var commSec)
                    && commSec.Fields.TryGetValue(KrTask.Comment, out var commentObj)
                    && commentObj is string comment)
                {
                    var sCard = await context.GetKrSatelliteAsync();

                    if (sCard is null)
                    {
                        return false;
                    }

                    sCard.Sections[KrApprovalCommonInfo.Name].Fields[KrApprovalCommonInfo.AuthorComment] = comment;

                    if (!string.IsNullOrEmpty(comment))
                    {
                        task.Result = comment;
                    }
                }

                await context.TryRemoveActiveTaskAsync(
                    task.RowID,
                    context.ValidationResult,
                    context.CancellationToken);

                await StartApprovalAsync(context);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Начинает новый цикл согласования.
        /// </summary>
        /// <param name="context">Контекст обработчика процесса.</param>
        /// <returns>Асинхронная задача.</returns>
        private static async Task StartApprovalAsync(
            IWorkflowEngineContext context)
        {
            var sCard = await context.GetKrSatelliteAsync();

            if (sCard is null)
            {
                return;
            }

            var fields = sCard.Sections[KrApprovalCommonInfo.Name].Fields;
            fields[KrApprovalCommonInfo.ApprovedBy] = string.Empty;
            fields[KrApprovalCommonInfo.DisapprovedBy] = string.Empty;
        }

        #endregion
    }
}
