#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Server.Workflow.KrObjectModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Workflow.ApprovalProcess;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow.Handlers
{
    /// <summary>
    /// Обработчик этапа <see cref="StageTypeDescriptors.ApprovalProcessManagementDescriptor"/>.
    /// </summary>
    /// <param name="approvalProcessManager"><inheritdoc cref="IApprovalProcessManager" path="/summary"/></param>
    /// <param name="approvalProcessInstanceRepository"><inheritdoc cref="IApprovalProcessInstanceRepository" path="/summary"/></param>
    /// <param name="approvalProcessValidator"><inheritdoc cref="IApprovalProcessValidator" path="/summary"/></param>
    /// <param name="approvalProcessRunner"><inheritdoc cref="IApprovalProcessRunner" path="/summary"/></param>
    /// <param name="krScope"><inheritdoc cref="IKrScope" path="/summary"/></param>
    /// <param name="objectModelMapper"><inheritdoc cref="IObjectModelMapper" path="/summary"/></param>
    public class ApprovalProcessManagementStageTypeHandler(
        IApprovalProcessManager approvalProcessManager,
        IApprovalProcessInstanceRepository approvalProcessInstanceRepository,
        IApprovalProcessValidator approvalProcessValidator,
        IApprovalProcessRunner approvalProcessRunner,
        IKrScope krScope,
        IObjectModelMapper objectModelMapper) :
        StageTypeHandlerBase
    {
        #region Constants And Static Fields

        /// <summary>
        /// Ключ, по которому в <see cref="Stage.InfoStorage"/>, содержится идентификатор запущенного процесса согласования. Тип значения: <see cref="Nullable{T}"/>, где T - <see cref="Guid"/>.
        /// </summary>
        protected const string ProcessInstanceKey = "ProcessInstance";

        #endregion

        #region Properties

        /// <inheritdoc cref="IApprovalProcessManager" path="/summary"/>
        protected IApprovalProcessManager ApprovalProcessManager { get; } = NotNullOrThrow(approvalProcessManager);

        /// <inheritdoc cref="IApprovalProcessInstanceRepository" path="/summary"/>
        protected IApprovalProcessInstanceRepository ApprovalProcessInstanceRepository { get; } = NotNullOrThrow(approvalProcessInstanceRepository);

        /// <inheritdoc cref="IApprovalProcessValidator" path="/summary"/>
        protected IApprovalProcessValidator ApprovalProcessValidator { get; } = NotNullOrThrow(approvalProcessValidator);

        /// <inheritdoc cref="IApprovalProcessRunner" path="/summary"/>
        protected IApprovalProcessRunner ApprovalProcessRunner { get; } = NotNullOrThrow(approvalProcessRunner);

        /// <inheritdoc cref="IKrScope" path="/summary"/>
        protected IKrScope KrScope { get; } = NotNullOrThrow(krScope);

        /// <inheritdoc cref="IObjectModelMapper" path="/summary"/>
        protected IObjectModelMapper ObjectModelMapper { get; } = NotNullOrThrow(objectModelMapper);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task<StageHandlerResult> HandleStageStartAsync(
            IStageTypeHandlerContext context)
        {
            var stageSettings = context.Stage.SettingsStorage;

            var controlType = stageSettings.Get<Guid?>(KrConstants.KrApprovalProcessManagementSettingsVirtual.ControlTypeID);
            if (controlType is null)
            {
                context.ValidationResult.AddError(
                    this,
                    await LocalizeFormatAsync(
                        "$KrProcess_ErrorMessage_ErrorFormat2",
                        KrErrorHelper.GetTraceTextFromStage(context.Stage),
                        "$KrStages_ApprovalProcessManagement_ManagementTypeNotSpecified"));

                return StageHandlerResult.EmptyResult;
            }

            if (controlType == ApprovalProcessHelper.RevokeControlTypeID)
            {
                return await this.RevokeAsync(context);
            }

            if (controlType == ApprovalProcessHelper.ChangeStateControlTypeID)
            {
                return await this.ChangeStateAsync(context);
            }

            if (controlType == ApprovalProcessHelper.DeleteControlTypeID)
            {
                return await this.DeleteAsync(context);
            }

            throw ArgumentOutOfRange(controlType);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Выполняет обновление состояния процесса согласования.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns><inheritdoc cref="StageHandlerResult" path="/summary"/></returns>
        protected virtual async Task<StageHandlerResult> ChangeStateAsync(IStageTypeHandlerContext context)
        {
            ThrowIfNull(context.MainCardID);

            var instances = await this.GetProcessInstancesAsync(context);
            if (instances is null)
            {
                return StageHandlerResult.EmptyResult;
            }

            var stageSettings = context.Stage.SettingsStorage;

            var state = stageSettings.Get<int?>(KrConstants.KrApprovalProcessManagementSettingsVirtual.StateID);

            var newState = (ApprovalProcessState?) state;
            var updateHistoryGroup = stageSettings.Get<bool>(KrConstants.KrApprovalProcessManagementSettingsVirtual.UpdateHistoryGroup);
            var showRevokeButton = stageSettings.Get<bool>(KrConstants.KrApprovalProcessManagementSettingsVirtual.ShowRevokeButton);
            var secondaryRevokeProcessID = stageSettings.TryGet<Guid?>(KrConstants.KrApprovalProcessManagementSettingsVirtual.SecondaryRevokeProcessID);
            var storeDateTime = (context.CardExtensionContext as ICardStoreExtensionContext)?.StoreDateTime ?? DateTime.UtcNow;
            var infoMode = (ApprovalProcessInfoMode?) stageSettings.TryGet<int?>(KrConstants.KrApprovalProcessManagementSettingsVirtual.InfoModeID);

            foreach (var instance in instances)
            {
                var hasChanges = false;
                if (instance.Settings.ShowRevokeButton != showRevokeButton)
                {
                    instance.Settings.ShowRevokeButton = showRevokeButton;
                    hasChanges = true;
                }

                var actualSecondaryRevokeProcessID = instance.Settings.Info?.TryGet<Guid>(KrConstants.Keys.ApprovalProcessSecondaryRevokeProcessID);

                if (actualSecondaryRevokeProcessID != secondaryRevokeProcessID)
                {
                    instance.Settings.Info ??= [];
                    instance.Settings.Info[KrConstants.Keys.ApprovalProcessSecondaryRevokeProcessID] = secondaryRevokeProcessID;
                    hasChanges = true;
                }

                if (updateHistoryGroup)
                {
                    var historyGroupID = await HandlerHelper.GetTaskHistoryGroupAsync(
                        context,
                        this.KrScope,
                        context.ValidationResult,
                        context.CancellationToken);

                    if (!context.ValidationResult.IsSuccessful())
                    {
                        return StageHandlerResult.EmptyResult;
                    }

                    if (historyGroupID is not null
                        && instance.Settings.HistoryGroupID != historyGroupID)
                    {
                        instance.Settings.HistoryGroupID = historyGroupID;
                        instance.Settings.Cycle = context
                            .WorkflowProcess
                            .InfoStorage
                            .TryGet(KrConstants.Keys.Cycle, 1);
                        hasChanges = true;
                    }
                }

                if (newState.HasValue
                    && instance.State != newState.Value)
                {
                    instance.State = newState.Value;
                    hasChanges = true;
                }

                if (infoMode.HasValue
                    && instance.Settings.InfoMode != infoMode)
                {
                    instance.Settings.InfoMode = infoMode.Value;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    if (newState == ApprovalProcessState.Pending)
                    {
                        var result = await this.ApprovalProcessManager.ResetProcessStateAsync(
                            this.CreateApprovalProcessRequest(
                                context,
                                instance,
                                storeDateTime),
                            context.CancellationToken);

                        if (!result.IsSuccessful)
                        {
                            return StageHandlerResult.EmptyResult;
                        }
                    }
                    else
                    {
                        await this.ApprovalProcessInstanceRepository.StoreInstanceAsync(
                            instance,
                            storeDateTime,
                            context.CancellationToken);
                    }
                }
            }

            return StageHandlerResult.CompleteResult;
        }

        /// <summary>
        /// Выполняет отзыв процесса согласования.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns><inheritdoc cref="StageHandlerResult" path="/summary"/></returns>
        protected virtual async Task<StageHandlerResult> RevokeAsync(
            IStageTypeHandlerContext context)
        {
            var instances = await this.GetProcessInstancesAsync(context);
            if (instances is null)
            {
                return StageHandlerResult.EmptyResult;
            }

            await this.ProcessInstancesAsync(
                context,
                instances,
                async instance =>
                {
                    var result = await this.ApprovalProcessRunner.RevokeProcessAsync(
                        instance,
                        context.CancellationToken);

                    context.ValidationResult.Add(result);

                    return result.IsSuccessful;
                });

            return context.ValidationResult.IsSuccessful()
                ? StageHandlerResult.CompleteResult
                : StageHandlerResult.EmptyResult;
        }

        /// <summary>
        /// Выполняет удаление процесса согласования.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns><inheritdoc cref="StageHandlerResult" path="/summary"/></returns>
        protected virtual async Task<StageHandlerResult> DeleteAsync(
            IStageTypeHandlerContext context)
        {
            var instances = await this.GetProcessInstancesAsync(context);
            if (instances is null)
            {
                return StageHandlerResult.EmptyResult;
            }

            var storeDateTime = (context.CardExtensionContext as ICardStoreExtensionContext)?.StoreDateTime ?? DateTime.UtcNow;

            await this.ProcessInstancesAsync(
                context,
                instances,
                async instance =>
                {
                    var result = await this.ApprovalProcessRunner.DeleteProcessAsync(
                        instance,
                        context.CancellationToken);

                    context.ValidationResult.Add(result);

                    return result.IsSuccessful;
                });

            return context.ValidationResult.IsSuccessful()
                ? StageHandlerResult.CompleteResult
                : StageHandlerResult.EmptyResult;
        }

        /// <summary>
        /// Создаёт запрос для запуска обработки процесса согласования.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="instance"><inheritdoc cref="ApprovalProcessInstance" path="/summary"/></param>
        /// <param name="storeDateTime">Дата и время сохранения.</param>
        /// <returns><inheritdoc cref="ApprovalProcessExecutionRequest" path="/summary"/></returns>
        protected ApprovalProcessExecutionRequest CreateApprovalProcessRequest(
            IStageTypeHandlerContext context,
            ApprovalProcessInstance instance,
            DateTime storeDateTime) =>
            new()
            {
                CardID = instance.CardID,
                InstanceID = instance.ID,
                Instance = instance,
                StoreCard = (context.CardExtensionContext as ICardStoreExtensionContext)?.Request?.TryGetCard(),
                ExecutionDateTime = storeDateTime,
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

        /// <summary>
        /// Возвращает список обрабатываемых процессов согласования. Если в параметрах этапа по ключу <see cref="KrConstants.ApprovalProcessManagementInstanceIDsParam"/>
        /// задан список идентификаторов процессов согласования, то загружаются процессы согласования с соответствующими идентификаторами, иначе загружаются все процессы согласования карточки.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <returns>Список обрабатываемых процессов согласования или <c>null</c>, если при загрузке списка возникла ошибка.</returns>
        protected async ValueTask<IReadOnlyList<ApprovalProcessInstance>?> GetProcessInstancesAsync(IStageTypeHandlerContext context)
        {
            var instanceIDs = context.Stage.InfoStorage.TryGet<IList<Guid>?>(KrConstants.ApprovalProcessManagementInstanceIDsParam);
            if (instanceIDs is not null)
            {
                var instances = new List<ApprovalProcessInstance>(instanceIDs.Count);

                foreach (var instanceID in instanceIDs)
                {
                    var instance = await this.ApprovalProcessInstanceRepository.GetInstanceAsync(
                        instanceID,
                        context.CancellationToken);
                    if (instance is null)
                    {
                        context.ValidationResult.AddError(
                            this,
                            await LocalizeFormatAsync(
                                "$KrProcess_ErrorMessage_ErrorFormat2",
                                KrErrorHelper.GetTraceTextFromStage(context.Stage),
                                await LocalizeFormatAsync(
                                    "$ApprovalProcess_Errors_ProcessInstanceNotFound",
                                    instanceID)));

                        return null;
                    }

                    instances.Add(instance);
                }

                return instances;
            }

            return await this.ApprovalProcessInstanceRepository.GetInstancesForCardAsync(
                context.MainCardID!.Value,
                context.CancellationToken);
        }

        /// <summary>
        /// Выполняет действия над заданными экземплярами процессов согласования с учётом необходимых действий по подготовке и обновлении объектной модели маршрута.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeHandlerContext" path="/summary"/></param>
        /// <param name="instances">Список обрабатываемых экземпляров процессов.</param>
        /// <param name="actionAsync">Выполняемое действие. Возвращаемое значение: значение <see langword="true"/>, если выполнение должно быть продолжено, иначе - <see langword="false"/>.</param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        protected virtual async ValueTask ProcessInstancesAsync(
            IStageTypeHandlerContext context,
            IReadOnlyList<ApprovalProcessInstance> instances,
            Func<ApprovalProcessInstance, ValueTask<bool>> actionAsync)
        {
            if (instances.Count == 0)
            {
                return;
            }

            // Обновление карточек данными из объектной модели для возможности получения актуальных данных в запускаемом процессе.
            var mainCard = await context.MainCardAccessStrategy.GetCardAsync(
                context.ValidationResult,
                cancellationToken: context.CancellationToken);

            if (mainCard is null)
            {
                return;
            }

            switch (context.RunnerMode)
            {
                case KrProcessRunnerMode.Sync:
                    if (context.MainCardID.HasValue
                        && context.MainCardID != Guid.Empty)
                    {
                        this.ObjectModelMapper.ObjectModelToPci(
                            context.ProcessHolder.MainWorkflowProcess!,
                            context.ProcessHolder.MainProcessCommonInfo!,
                            context.ProcessHolder.MainProcessCommonInfo!,
                            context.ProcessHolder.PrimaryProcessCommonInfo);

                        await this.ObjectModelMapper.SetMainProcessCommonInfoAsync(
                            mainCard,
                            context.ContextualSatellite!,
                            context.ProcessHolder.PrimaryProcessCommonInfo!,
                            context.CancellationToken);
                    }

                    break;
                case KrProcessRunnerMode.Async:
                    await this.ObjectModelMapper.ObjectModelToCardRowsAsync(
                        context.ProcessHolder,
                        context.ProcessHolderSatellite!,
                        context.ContextualSatellite!,
                        mainCard,
                        context.CancellationToken);
                    break;
                default:
                    throw ArgumentOutOfRange(context.RunnerMode);
            }

            foreach (var instance in instances)
            {
                var result = await actionAsync(instance);

                if (!result
                    || !context.ValidationResult.IsSuccessful())
                {
                    return;
                }
            }

            // Полное обновление объектной модели маршрута.
            switch (context.RunnerMode)
            {
                case KrProcessRunnerMode.Sync:
                    if (context.ContextualSatellite is not null)
                    {
                        context.ProcessHolder.PrimaryProcessCommonInfo = this.ObjectModelMapper.GetMainProcessCommonInfo(context.ContextualSatellite);
                    }

                    this.ObjectModelMapper.FillWorkflowProcessFromPci(
                        context.WorkflowProcess,
                        null, // Значение не может быть изменено извне.
                        context.ProcessHolder.PrimaryProcessCommonInfo);
                    break;
                case KrProcessRunnerMode.Async:
                    await HandlerHelper.UpdateWorkflowProcessAsync(
                        context,
                        this.ObjectModelMapper);
                    break;
                default:
                    throw ArgumentOutOfRange(context.RunnerMode);
            }
        }

        #endregion
    }
}
