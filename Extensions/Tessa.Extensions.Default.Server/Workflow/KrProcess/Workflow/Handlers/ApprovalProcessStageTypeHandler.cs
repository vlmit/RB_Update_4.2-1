#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Workflow.ApprovalProcess;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers
{
    /// <summary>
    /// Обработчик этапа <see cref="StageTypeDescriptors.ApprovalProcessDescriptor"/>.
    /// </summary>
    /// <param name="approvalProcessManager"><inheritdoc cref="IApprovalProcessManager" path="/summary"/></param>
    /// <param name="approvalProcessInstanceRepository"><inheritdoc cref="IApprovalProcessInstanceRepository" path="/summary"/></param>
    /// <param name="approvalProcessValidator"><inheritdoc cref="IApprovalProcessValidator" path="/summary"/></param>
    /// <param name="krScope"><inheritdoc cref="IKrScope" path="/summary"/></param>
    /// <param name="objectModelMapper"><inheritdoc cref="IObjectModelMapper" path="/summary"/></param>
    /// <param name="krProcessLauncher"><inheritdoc cref="IKrProcessLauncher" path="/summary"/></param>
    /// <param name="approvalProcessRunner"><inheritdoc cref="IApprovalProcessRunner" path="/summary"/></param>
    public class ApprovalProcessStageTypeHandler(
        IApprovalProcessManager approvalProcessManager,
        IApprovalProcessInstanceRepository approvalProcessInstanceRepository,
        IApprovalProcessValidator approvalProcessValidator,
        IKrScope krScope,
        IObjectModelMapper objectModelMapper,
        IKrProcessLauncher krProcessLauncher,
        IApprovalProcessRunner approvalProcessRunner) :
        StageTypeHandlerBase
    {
        #region Constants And Static Fields

        /// <summary>
        /// Ключ, по которому в <see cref="Stage.InfoStorage"/>, содержится идентификатор запущенного процесса согласования. Тип значения: <see cref="Nullable{T}"/>, где T - <see cref="Guid"/>.
        /// </summary>
        protected const string ProcessInstanceKey = "ProcessInstance";

        /// <inheritdoc cref="KrConstants.Keys.Revoked"/>
        protected const string Revoked = KrConstants.Keys.Revoked;

        /// <inheritdoc cref="KrConstants.Keys.Disapproved"/>
        protected const string Disapproved = KrConstants.Keys.Disapproved;

        #endregion

        #region Properties

        /// <inheritdoc cref="IApprovalProcessManager" path="/summary"/>
        protected IApprovalProcessManager ApprovalProcessManager { get; } = NotNullOrThrow(approvalProcessManager);

        /// <inheritdoc cref="IApprovalProcessInstanceRepository" path="/summary"/>
        protected IApprovalProcessInstanceRepository ApprovalProcessInstanceRepository { get; } = NotNullOrThrow(approvalProcessInstanceRepository);

        /// <inheritdoc cref="IApprovalProcessValidator" path="/summary"/>
        protected IApprovalProcessValidator ApprovalProcessValidator { get; } = NotNullOrThrow(approvalProcessValidator);

        /// <inheritdoc cref="IKrScope" path="/summary"/>
        protected IKrScope KrScope { get; } = NotNullOrThrow(krScope);

        /// <inheritdoc cref="IObjectModelMapper" path="/summary"/>
        protected IObjectModelMapper ObjectModelMapper { get; } = NotNullOrThrow(objectModelMapper);

        /// <inheritdoc cref="IKrProcessLauncher" path="/summary"/>
        protected IKrProcessLauncher KrProcessLauncher { get; } = NotNullOrThrow(krProcessLauncher);

        /// <inheritdoc cref="IApprovalProcessRunner" path="/summary"/>
        protected IApprovalProcessRunner ApprovalProcessRunner { get; } = NotNullOrThrow(approvalProcessRunner);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task<StageHandlerResult> HandleStageStartAsync(
            IStageTypeHandlerContext context)
        {
            context.Stage.InfoStorage[Disapproved] = BooleanBoxes.False;
            context.Stage.InfoStorage.Remove(ProcessInstanceKey);

            var instance = await this.GetOrCreateInstanceAsync(context);
            if (instance is null)
            {
                return StageHandlerResult.EmptyResult;
            }

            if (!await this.ApprovalProcessValidator.ValidateIntegrityAsync(
                new ApprovalProcessValidationContext()
                {
                    Process = instance.Process,
                    ProcessState = instance.State,
                    ValidationResult = context.ValidationResult,
                    CancellationToken = context.CancellationToken
                }))
            {
                return StageHandlerResult.EmptyResult;
            }

            var mainCard = await context.MainCardAccessStrategy.GetCardAsync(
                context.ValidationResult,
                cancellationToken: context.CancellationToken);

            if (mainCard is null)
            {
                return StageHandlerResult.EmptyResult;
            }

            // Обновление карточек данными из объектной модели для возможности получения актуальных данных в запускаемом процессе.
            await this.ObjectModelMapper.ObjectModelToCardRowsAsync(
                context.ProcessHolder,
                context.ProcessHolderSatellite!,
                context.ContextualSatellite!,
                mainCard,
                context.CancellationToken);

            var stageSettings = context.Stage.SettingsStorage;

            instance.ExternalProcessID = context.ProcessInfo!.ProcessID;
            instance.ExternalWorkflowType = context.ProcessInfo.ProcessTypeName;
            this.KrScope.Info[KrConstants.IsNotUpdateExternalProcess] = BooleanBoxes.True;

            instance.Settings.ReturnAfterDisapproval = stageSettings.TryGet<bool>(KrConstants.KrApprovalProcessSettingsVirtual.ReturnAfterDisapproval);
            instance.Settings.ShowRevokeButton = stageSettings.TryGet<bool>(KrConstants.KrApprovalProcessSettingsVirtual.ShowRevokeButton);
            instance.Settings.ChangeStateOnStart = stageSettings.Get<bool>(KrConstants.KrApprovalProcessSettingsVirtual.ChangeState);
            instance.Settings.ChangeStateOnEnd = false; // Окончательное состояние устанавливается в этапе.
            instance.Settings.InfoMode = (ApprovalProcessInfoMode?) stageSettings.TryGet<int?>(KrConstants.KrApprovalProcessSettingsVirtual.InfoModeID) ?? ApprovalProcessInfoMode.Default;

            var actualSecondaryRevokeProcessID = instance.Settings.Info?.TryGet<Guid>(KrConstants.Keys.ApprovalProcessSecondaryRevokeProcessID);
            var secondaryRevokeProcessID = context.Stage.SettingsStorage.TryGet<Guid?>(KrConstants.KrApprovalProcessSettingsVirtual.SecondaryRevokeProcessID);

            if (actualSecondaryRevokeProcessID != secondaryRevokeProcessID)
            {
                instance.Settings.Info ??= [];
                instance.Settings.Info[KrConstants.Keys.ApprovalProcessSecondaryRevokeProcessID] = secondaryRevokeProcessID;
            }

            var historyGroupID = await HandlerHelper.GetTaskHistoryGroupAsync(
                context,
                this.KrScope,
                context.ValidationResult,
                context.CancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                return StageHandlerResult.EmptyResult;
            }

            if (historyGroupID is not null)
            {
                instance.Settings.HistoryGroupID = historyGroupID;
                instance.Settings.Cycle = context
                    .WorkflowProcess
                    .InfoStorage
                    .TryGet(KrConstants.Keys.Cycle, 1);
            }

            var approvalRequest = new ApprovalProcessExecutionRequest()
            {
                CardID = context.MainCardID!.Value,
                InstanceID = instance.ID,
                Instance = instance,
                StoreCard = (context.CardExtensionContext as ICardStoreExtensionContext)?.Request?.TryGetCard(),
                ValidationResult = context.ValidationResult,
                GetCardFuncAsync = async (cardID, result, forceLoadTasks, ct) =>
                {
                    var isMainCard = context.MainCardID == cardID;
                    Card? card;
                    if (isMainCard)
                    {
                        card = await context
                            .MainCardAccessStrategy
                            .GetCardAsync(
                                result,
                                cancellationToken: ct);
                    }
                    else
                    {
                        card = await this.KrScope.GetMainCardAsync(
                            cardID,
                            validationResult: result,
                            cancellationToken: ct);
                    }

                    if (forceLoadTasks
                        && card is not null)
                    {
                        if (isMainCard)
                        {
                            await context.MainCardAccessStrategy.EnsureTasksLoadedAsync(
                                result,
                                ct);
                        }
                        else
                        {
                            await this.KrScope.EnsureTasksLoadedAsync(
                                cardID,
                                result,
                                ct);
                        }
                    }

                    return card;
                },
                GetCardFileContainerFuncAsync = (cardID, result, ct) =>
                    cardID == context.MainCardID
                        ? context
                            .MainCardAccessStrategy
                            .GetFileContainerAsync(
                                validationResult: result,
                                cancellationToken: ct)
                        : new(this.KrScope
                            .GetMainCardFileContainerAsync(
                                cardID,
                                validationResult: result,
                                cancellationToken: ct)),
                GetSatelliteFuncAsync = (typeID, cardID, taskID, result, ct) =>
                    this.KrScope.GetSatelliteAsync(
                        cardID,
                        taskID,
                        typeID,
                        validationResult: result,
                        cancellationToken: ct),
                PreventNextCardStore = true,
                ModifyStoreRequestActionOverride = this.KrScope.ModifyStoreRequest,
            };

            var result = await this.ApprovalProcessManager.StartApprovalProcessAsync(
                approvalRequest,
                context.CancellationToken);

            if (!result.IsSuccessful)
            {
                return StageHandlerResult.EmptyResult;
            }

            instance = result.Instance ?? instance;

            if (instance.State == ApprovalProcessState.Running)
            {
                // Полное обновление объектной модели маршрута.
                await HandlerHelper.UpdateWorkflowProcessAsync(
                    context,
                    this.ObjectModelMapper);

                context.Stage.InfoStorage[ProcessInstanceKey] = instance.ID;

                return StageHandlerResult.InProgressResult;
            }

            return await this.FinishProcessAsync(
                context,
                instance);
        }

        /// <inheritdoc/>
        public override async Task<StageHandlerResult> HandleSignalAsync(
            IStageTypeHandlerContext context)
        {
            if (context.Stage.InfoStorage.TryGet<Guid?>(ProcessInstanceKey) is not { } instanceID)
            {
                return StageHandlerResult.EmptyResult;
            }

            var signal = NotNullOrThrow(context.SignalInfo).Signal;

            if (signal.Info.TryGetValue(KrConstants.ApprovalProcessSignalInstanceIDParam, out var signalInstanceIDObj)
                && signalInstanceIDObj is Guid signalInstanceID
                && signalInstanceID != instanceID)
            {
                return StageHandlerResult.EmptyResult;
            }

            switch (signal.Name)
            {
                case KrConstants.ApprovalProcessCompletedSignal:
                    {
                        var instance = await this.GetInstanceIfUndefinedAsync(
                            instanceID,
                            null,
                            context.CancellationToken);

                        if (instance is null)
                        {
                            await this.AddProcessInstanceNotFoundErrorAsync(
                                context,
                                instanceID);

                            return StageHandlerResult.EmptyResult;
                        }

                        return await this.FinishProcessAsync(
                            context,
                            instance);
                    }

                case KrConstants.ApprovalProcessContinueSignal:
                    {
                        var execResult = await this.ApprovalProcessManager.ContinueApprovalProcessAsync(
                            this.CreateApprovalProcessExecutionRequestForComplete(
                                context,
                                instanceID),
                            context.CancellationToken);

                        if (!execResult.IsSuccessful)
                        {
                            return StageHandlerResult.EmptyResult;
                        }

                        var instance = await this.GetInstanceIfUndefinedAsync(
                            instanceID,
                            execResult.Instance,
                            context.CancellationToken);

                        if (instance is null)
                        {
                            await this.AddProcessInstanceNotFoundErrorAsync(
                                context,
                                instanceID);

                            return StageHandlerResult.EmptyResult;
                        }

                        return await this.FinishProcessAsync(
                            context,
                            instance);
                    }

                case KrConstants.ApprovalProcessStopSignal:
                    {
                        var instance = await this.GetInstanceIfUndefinedAsync(
                            instanceID,
                            null,
                            context.CancellationToken);

                        if (instance is null)
                        {
                            await this.AddProcessInstanceNotFoundErrorAsync(
                                context,
                                instanceID);

                            return StageHandlerResult.EmptyResult;
                        }

                        // Обновление карточек данными из объектной модели для возможности получения актуальных данных в запускаемом процессе.
                        var mainCard = await context.MainCardAccessStrategy.GetCardAsync(
                            context.ValidationResult,
                            cancellationToken: context.CancellationToken);

                        if (mainCard is null)
                        {
                            return StageHandlerResult.EmptyResult;
                        }

                        await this.ObjectModelMapper.ObjectModelToCardRowsAsync(
                            context.ProcessHolder,
                            context.ProcessHolderSatellite!,
                            context.ContextualSatellite!,
                            mainCard,
                            context.CancellationToken);

                        var revokeResult = await this.ApprovalProcessRunner.RevokeProcessAsync(
                            instance,
                            context.CancellationToken);

                        context.ValidationResult.Add(revokeResult);

                        if (!revokeResult.IsSuccessful)
                        {
                            return StageHandlerResult.EmptyResult;
                        }

                        // Полное обновление объектной модели маршрута.
                        await HandlerHelper.UpdateWorkflowProcessAsync(
                            context,
                            this.ObjectModelMapper);

                        return StageHandlerResult.CompleteResult;
                    }

                case KrConstants.ApprovalProcessStopCompletedSignal:
                    context.Stage.InfoStorage[Revoked] = BooleanBoxes.True;
                    context.Stage.InfoStorage.Remove(ProcessInstanceKey);

                    return StageHandlerResult.CompleteResult;
            }

            return StageHandlerResult.EmptyResult;
        }

        /// <inheritdoc/>
        public override async Task<bool> HandleStageInterruptAsync(
            IStageTypeHandlerContext context)
        {
            context.Stage.InfoStorage[Revoked] = BooleanBoxes.True;

            if (context.Stage.InfoStorage.TryGet<Guid?>(ProcessInstanceKey) is not { } instanceID)
            {
                return true;
            }

            context.Stage.InfoStorage.Remove(ProcessInstanceKey);

            var stopResult = await this.ApprovalProcessManager.StopApprovalProcessAsync(
                this.CreateApprovalProcessExecutionRequestForComplete(
                    context,
                    instanceID),
                context.CancellationToken);

            return stopResult.IsSuccessful;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Обрабатывает завершение процесса согласования.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="instance"><inheritdoc cref="ApprovalProcessInstance" path="/summary"/></param>
        /// <returns><inheritdoc cref="StageHandlerResult" path="/summary"/></returns>
        protected virtual ValueTask<StageHandlerResult> FinishProcessAsync(
            IStageTypeHandlerContext context,
            ApprovalProcessInstance instance)
        {
            switch (instance.State)
            {
                case ApprovalProcessState.Running:
                    return ValueTask.FromResult(StageHandlerResult.InProgressResult);
                case ApprovalProcessState.Approved:
                    return this.ApproveAndCompleteAsync(context);
                case ApprovalProcessState.Disapproved:
                    return this.DisapproveAndCompleteAsync(context);
                default:
                    throw ArgumentOutOfRange(instance.State);
            }
        }

        /// <summary>
        /// Возвращает экземпляр процесса согласования или создаёт новый из шаблона.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns>Экземпляр процесса согласования или значение <see langword="null"/>, если произошла ошибка.</returns>
        protected async ValueTask<ApprovalProcessInstance?> GetOrCreateInstanceAsync(
            IStageTypeHandlerContext context)
        {
            var stage = context.Stage;
            var stageSettings = stage.SettingsStorage;

            if (stageSettings.Get<bool>(KrConstants.KrApprovalProcessSettingsVirtual.UseProcessFromCard))
            {
                var instances = await this.ApprovalProcessInstanceRepository.GetInstancesForCardWithCacheAsync(
                    await context.MainCardAccessStrategy.GetCardAsync(
                        context.ValidationResult,
                        cancellationToken: context.CancellationToken),
                    context.CancellationToken);

                if (instances.Count > 0)
                {
                    return instances[0];
                }

                context.ValidationResult.AddError(
                    this,
                    await LocalizeFormatAsync(
                        "$KrProcess_ErrorMessage_ErrorFormat2",
                        KrErrorHelper.GetTraceTextFromStage(stage),
                        await LocalizeFormatAsync(
                            "$KrStages_ApprovalProcess_ProcessInstanceUndefined",
                            context.MainCardID)));

                return null;
            }

            var templateID = stageSettings.TryGet<Guid?>(KrConstants.KrApprovalProcessSettingsVirtual.TemplateID);
            if (!templateID.HasValue)
            {
                context.ValidationResult.AddError(
                    this,
                    await LocalizeFormatAsync(
                        "$KrProcess_ErrorMessage_ErrorFormat2",
                        KrErrorHelper.GetTraceTextFromStage(stage),
                        "$KrStages_ApprovalProcess_TemplateUndefined"));

                return null;
            }

            var instanceFromTemplate = await this.ApprovalProcessInstanceRepository.CreateInstanceFromTemplateAsync(
                templateID.Value,
                context.CancellationToken);

            if (instanceFromTemplate is null)
            {
                context.ValidationResult.AddError(
                    this,
                    await LocalizeFormatAsync(
                        "$KrProcess_ErrorMessage_ErrorFormat2",
                        KrErrorHelper.GetTraceTextFromStage(stage),
                        await LocalizeFormatAsync(
                            "$ApprovalProcess_Errors_ProcessTemplateNotFound",
                            templateID.Value)));

                return null;
            }

            instanceFromTemplate.CardID = NotNullOrThrow(context.MainCardID);

            return instanceFromTemplate;
        }

        /// <summary>
        /// Создаёт <see cref="ApprovalProcessExecutionRequest"/> для выполнения действия над процессом согласования, заполняя параметры данными из <paramref name="context"/>.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="instanceID">Идентификатор экземпляра процесса согласования.</param>
        /// <returns><inheritdoc cref="ApprovalProcessExecutionRequest" path="/summary"/></returns>
        protected ApprovalProcessExecutionRequest CreateApprovalProcessExecutionRequestForComplete(
            IStageTypeHandlerContext context,
            Guid instanceID)
        {
            var cardStoreExtensionContext = context.CardExtensionContext as ICardStoreExtensionContext;

            return new ApprovalProcessExecutionRequest()
            {
                CardID = context.MainCardID!.Value,
                InstanceID = instanceID,
                StoreCard = cardStoreExtensionContext?.Request?.TryGetCard(),
                ValidationResult = context.ValidationResult,
                GetCardFuncAsync = async (cardID, result, forceLoadTasks, ct) =>
                {
                    var isMainCard = context.MainCardID == cardID;
                    Card? card;
                    if (isMainCard)
                    {
                        card = await context
                            .MainCardAccessStrategy
                            .GetCardAsync(
                                result,
                                cancellationToken: ct);
                    }
                    else
                    {
                        card = await this.KrScope.GetMainCardAsync(
                            cardID,
                            validationResult: result,
                            cancellationToken: ct);
                    }

                    if (forceLoadTasks
                        && card is not null)
                    {
                        if (isMainCard)
                        {
                            await context.MainCardAccessStrategy.EnsureTasksLoadedAsync(
                                result,
                                ct);
                        }
                        else
                        {
                            await this.KrScope.EnsureTasksLoadedAsync(
                                cardID,
                                result,
                                ct);
                        }
                    }

                    return card;
                },
                GetCardFileContainerFuncAsync = (cardID, result, ct) =>
                    cardID == context.MainCardID
                        ? context
                            .MainCardAccessStrategy
                            .GetFileContainerAsync(
                                validationResult: result,
                                cancellationToken: ct)
                        : new(this.KrScope
                            .GetMainCardFileContainerAsync(
                                cardID,
                                validationResult: result,
                                cancellationToken: ct)),
                GetSatelliteFuncAsync = (typeID, cardID, taskID, result, ct) =>
                    this.KrScope.GetSatelliteAsync(
                        cardID,
                        taskID,
                        typeID,
                        validationResult: result,
                        cancellationToken: ct),
                PreventNextCardStore = true,
                ExecutionDateTime = cardStoreExtensionContext?.StoreDateTime ?? DateTime.UtcNow,
                ModifyStoreRequestActionOverride = this.KrScope.ModifyStoreRequest,
            };
        }

        /// <summary>
        /// Загружает экземпляр процесса согласования, если он не задан.
        /// </summary>
        /// <param name="instanceID">Идентификатор экземпляра процесса согласования.</param>
        /// <param name="instance">Экземпляр процесса согласования или значение <see langword="null"/>, если его требуется загрузить.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Экземпляр процесса согласования или значение <see langword="null"/>, если произошла ошибка.</returns>
        protected async ValueTask<ApprovalProcessInstance?> GetInstanceIfUndefinedAsync(
            Guid instanceID,
            ApprovalProcessInstance? instance,
            CancellationToken cancellationToken = default) =>
            instance ??= await this.ApprovalProcessInstanceRepository.GetInstanceAsync(
                instanceID,
                cancellationToken);

        /// <summary>
        /// Добавляет в <see cref="IStageTypeHandlerContext.ValidationResult"/> ошибку об отсутствии экземпляра процесса.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="instanceID">Идентификатор экземпляра процесса.</param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        protected async ValueTask AddProcessInstanceNotFoundErrorAsync(
            IStageTypeHandlerContext context,
            Guid instanceID) =>
            context.ValidationResult.AddError(
                this,
                await LocalizeFormatAsync(
                    "$KrProcess_ErrorMessage_ErrorFormat2",
                    KrErrorHelper.GetTraceTextFromStage(context.Stage),
                    await LocalizeFormatAsync(
                        "$ApprovalProcess_Errors_ProcessInstanceNotFound",
                        instanceID)));

        /// <summary>
        /// Обрабатывает согласование.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns><inheritdoc cref="StageHandlerResult" path="/summary"/></returns>
        protected virtual async ValueTask<StageHandlerResult> ApproveAndCompleteAsync(
            IStageTypeHandlerContext context)
        {
            var stage = context.Stage;
            var (hasPreviouslyDisapproved, hasNext) = HandlerHelper.GetApprovalStagesInfo(
                context.WorkflowProcess.Stages,
                stage,
                KrConstants.DefaultApprovalStageTypeIDList);

            if (stage.SettingsStorage.Get<bool>(KrConstants.KrApprovalProcessSettingsVirtual.ChangeState))
            {
                if (hasPreviouslyDisapproved)
                {
                    this.KrScope.Info[KrConstants.Keys.IgnoreChangeState] = BooleanBoxes.True;
                    context.WorkflowProcess.State = KrState.Disapproved;
                }
                else
                {
                    context.WorkflowProcess.State = KrState.Approved;
                }
            }

            if (!stage.SettingsStorage.Get<bool>(KrConstants.KrApprovalProcessSettingsVirtual.NotReturnEdit))
            {
                if (hasPreviouslyDisapproved
                    && !hasNext)
                {
                    // Последний этап завершен. Этот согласован, но предыдущие могли быть и не согласованы.
                    // Если были несогласованные, то возвращаемся в начало текущей группы на доработку.
                    return StageHandlerResult.CurrentGroupTransition();
                }
            }

            return StageHandlerResult.CompleteResult;
        }

        /// <summary>
        /// Обрабатывает несогласование.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns><inheritdoc cref="StageHandlerResult" path="/summary"/></returns>
        protected virtual ValueTask<StageHandlerResult> DisapproveAndCompleteAsync(
            IStageTypeHandlerContext context)
        {
            var stage = context.Stage;

            stage.InfoStorage[Disapproved] = BooleanBoxes.True;

            if (!stage.SettingsStorage.Get<bool>(KrConstants.KrApprovalProcessSettingsVirtual.ReturnAfterDisapproval))
            {
                (_, var hasNext) = HandlerHelper.GetApprovalStagesInfo(
                    context.WorkflowProcess.Stages,
                    stage,
                    KrConstants.DefaultApprovalStageTypeIDList);

                if (hasNext)
                {
                    context.WorkflowProcess.InfoStorage[Disapproved] = BooleanBoxes.True;

                    return ValueTask.FromResult(StageHandlerResult.CompleteResult);
                }
            }

            if (stage.SettingsStorage.Get<bool>(KrConstants.KrApprovalProcessSettingsVirtual.ChangeState))
            {
                context.WorkflowProcess.State = KrState.Disapproved;
                this.KrScope.Info[KrConstants.Keys.IgnoreChangeState] = BooleanBoxes.True;
            }

            return ValueTask.FromResult(
                stage.SettingsStorage.Get<bool>(KrConstants.KrApprovalProcessSettingsVirtual.NotReturnEdit)
                    ? StageHandlerResult.CompleteResult
                    : StageHandlerResult.GroupTransition(stage.StageGroupID));
        }

        #endregion
    }
}
