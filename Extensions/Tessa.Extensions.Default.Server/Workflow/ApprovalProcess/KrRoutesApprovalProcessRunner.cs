#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Cards.Numbers;
using Tessa.Cards.Workflow;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Scope;
using Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Workflow.ApprovalProcess;

namespace Tessa.Extensions.Default.Server.Workflow.ApprovalProcess
{
    /// <summary>
    /// Реализация <see cref="IApprovalProcessRunner"/>, запускающая процесс подсистемы маршрутов для указанного процесса согласования.
    /// </summary>
    /// <param name="krProcessLauncher"><inheritdoc cref="IKrProcessLauncher" path="/summary"/></param>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
    /// <param name="cardTransactionStrategy"><inheritdoc cref="ICardTransactionStrategy" path="/summary"/></param>
    /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
    /// <param name="cardGetStrategy"><inheritdoc cref="ICardGetStrategy" path="/summary"/></param>
    /// <param name="cardServerPermissionsProvider"><inheritdoc cref="ICardServerPermissionsProvider" path="/summary"/></param>
    /// <param name="approvalProcessManager"><inheritdoc cref="IApprovalProcessManager" path="/summary"/></param>
    /// <param name="krScope"><inheritdoc cref="IKrScope" path="/summary"/></param>
    /// <param name="approvalProcessInstanceRepository"><inheritdoc cref="IApprovalProcessInstanceRepository" path="/summary"/></param>
    public sealed class KrRoutesApprovalProcessRunner(
        IKrProcessLauncher krProcessLauncher,
        IDbScope dbScope,
        ICardRepository cardRepository,
        ICardTransactionStrategy cardTransactionStrategy,
        ICardMetadata cardMetadata,
        ICardGetStrategy cardGetStrategy,
        ICardServerPermissionsProvider cardServerPermissionsProvider,
        IApprovalProcessManager approvalProcessManager,
        IKrScope krScope,
        IApprovalProcessInstanceRepository approvalProcessInstanceRepository) : IApprovalProcessRunner
    {
        #region Fields

        private readonly IKrProcessLauncher krProcessLauncher = NotNullOrThrow(krProcessLauncher);
        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);
        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);
        private readonly ICardTransactionStrategy cardTransactionStrategy = NotNullOrThrow(cardTransactionStrategy);
        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);
        private readonly ICardGetStrategy cardGetStrategy = NotNullOrThrow(cardGetStrategy);
        private readonly ICardServerPermissionsProvider cardServerPermissionsProvider = NotNullOrThrow(cardServerPermissionsProvider);
        private readonly IApprovalProcessManager approvalProcessManager = NotNullOrThrow(approvalProcessManager);
        private readonly IKrScope krScope = NotNullOrThrow(krScope);
        private readonly IApprovalProcessInstanceRepository approvalProcessInstanceRepository = NotNullOrThrow(approvalProcessInstanceRepository);

        #endregion

        #region IApporvalProcessRunner Members

        /// <inheritdoc/>
        public async ValueTask<bool> CanStartProcessAsync(
            ApprovalProcessInstance instance,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(instance);

            if (!instance.ExternalProcessID.HasValue)
            {
                return true;
            }

            await using var _ = this.dbScope.Create();

            return !await this.DoesProcessExistAsync(
                this.dbScope.Db,
                instance.ExternalProcessID.Value,
                cancellationToken);
        }

        /// <inheritdoc/>
        public async ValueTask<ValidationResult> StartProcessAsync(
            ApprovalProcessInstance instance,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(instance);
            ThrowIfNull(instance.ExternalStartID);

            var process = KrProcessBuilder
                .CreateProcess()
                .SetProcess(instance.ExternalStartID.Value)
                .SetCard(instance.CardID)
                .SetProcessInfo(new Dictionary<string, object?>()
                {
                    { KrConstants.ApprovalProcessSignalInstanceIDParam, instance.ID }
                })
                .Build();

            var validationResult = new ValidationResultBuilder();
            var level = this.krScope.EnterNewLevel();

            try
            {
                var launchResult = await this.krProcessLauncher.LaunchAsync(
                    process,
                    specificParameters: new KrProcessServerLauncher.SpecificParameters()
                    {
                        RaiseErrorWhenExecutionIsForbidden = true,
                    },
                    cancellationToken: cancellationToken);

                validationResult.Add(launchResult.ValidationResult);

                if (validationResult.IsSuccessful()
                    && !this.krScope.Info.TryGet<bool>(KrConstants.IsNotUpdateExternalProcess))
                {
                    await this.UpdateExternalProcessAsync(
                        launchResult,
                        instance.ID,
                        cancellationToken);
                }
            }
            finally
            {
                await level.ExitAsync(validationResult);
            }

            return validationResult.Build();
        }

        /// <inheritdoc/>
        public async ValueTask<IReadOnlyList<Guid>> GetExternalTasksAsync(
            ApprovalProcessInstance instance,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(instance);
            ThrowIfNull(instance.ExternalProcessID);

            await using var _ = this.dbScope.Create();

            var db = this.dbScope.Db;

            return await db.SetCommand(
                this.dbScope.BuilderFactory
                    .Select().C("RowID")
                    .From("WorkflowTasks", "wt").NoLock()
                    .Where().C("wt", "ProcessRowID").Equals().P("ProcessRowID")
                    .Build(),
                db.Parameter("ProcessRowID", instance.ExternalProcessID, DataType.Guid))
                .LogCommand()
                .ExecuteListAsync<Guid>(cancellationToken);
        }

        /// <inheritdoc/>
        public async ValueTask<ValidationResult> FinishProcessAsync(
            ApprovalProcessInstance instance,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(instance);
            ThrowIfNull(instance.ExternalProcessID);
            ThrowIfNullOrEmpty(instance.ExternalWorkflowType);

            var validationResult = new ValidationResultBuilder();

            await this.SendSignalAsync(
                instance.CardID,
                instance.ExternalProcessID.Value,
                instance.ExternalWorkflowType,
                KrConstants.ApprovalProcessCompletedSignal,
                new Dictionary<string, object?>
                {
                    { KrConstants.ApprovalProcessSignalInstanceIDParam, instance.ID },
                },
                validationResult,
                cancellationToken);

            return validationResult.Build();
        }

        /// <inheritdoc/>
        public async ValueTask<ValidationResult> RevokeProcessAsync(
            ApprovalProcessInstance instance,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(instance);
            ThrowIfNull(instance.ExternalProcessID);
            ThrowIfNullOrEmpty(instance.ExternalWorkflowType);

            var validationResult = new ValidationResultBuilder();

            await this.cardTransactionStrategy.ExecuteInWriterLockAsync(
                instance.CardID,
                CardComponentHelper.DoNotCheckVersion,
                validationResult,
                async p =>
                {
                    await this.RevokeProcessCoreAsync(
                        instance,
                        p);

                    if (!p.ValidationResult.IsSuccessful())
                    {
                        p.ReportError = true;
                    }
                },
                cancellationToken: cancellationToken);

            return validationResult.Build();
        }

        /// <inheritdoc/>
        public async ValueTask<ValidationResult> DeleteProcessAsync(
            ApprovalProcessInstance instance,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(instance);
            ThrowIfNull(instance.ExternalProcessID);
            ThrowIfNullOrEmpty(instance.ExternalWorkflowType);

            var validationResult = new ValidationResultBuilder();

            await this.cardTransactionStrategy.ExecuteInWriterLockAsync(
                instance.CardID,
                CardComponentHelper.DoNotCheckVersion,
                validationResult,
                async p =>
                {
                    await this.DeleteProcessCoreAsync(
                        instance,
                        p);

                    if (!p.ValidationResult.IsSuccessful())
                    {
                        p.ReportError = true;
                    }
                },
                cancellationToken: cancellationToken);

            return validationResult.Build();
        }

        /// <inheritdoc/>
        public async ValueTask ContinueProcessAsync(
            ApprovalProcessExecutionRequest request,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(request);

            this.InitRequestFromKrScope(request);

            var result = await this.approvalProcessManager.ContinueApprovalProcessAsync(
                request,
                cancellationToken);

            if (!result.IsSuccessful)
            {
                return;
            }

            if (result.Instance?.State
                is ApprovalProcessState.Approved
                or ApprovalProcessState.Disapproved
                or ApprovalProcessState.Pending
                && result.Instance.ExternalProcessID is not null)
            {
                var finishResult = await this.FinishProcessAsync(
                    result.Instance,
                    cancellationToken);
                request.ValidationResult.Add(finishResult);
            }
        }

        #endregion

        #region Private Methods

        private async Task SendSignalAsync(
            Guid cardID,
            Guid externalProcessID,
            string externalProcessType,
            string signalName,
            Dictionary<string, object?>? signalParameters,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default) =>
            await this.cardTransactionStrategy.ExecuteInReaderLockAsync(
                cardID,
                validationResult,
                async p =>
                {
                    await this.SendSignalCoreAsync(
                        externalProcessID,
                        externalProcessType,
                        signalName,
                        signalParameters,
                        p);

                    if (!p.ValidationResult.IsSuccessful())
                    {
                        p.ReportError = true;
                    }
                },
                cancellationToken: cancellationToken);

        private async Task SendSignalCoreAsync(
            Guid externalProcessID,
            string externalProcessType,
            string signalName,
            Dictionary<string, object?>? signalParameters,
            ICardTransactionParameter p,
            Card? cardForSignal = null)
        {
            if (cardForSignal is null)
            {
                var getContext = await this.cardGetStrategy.TryLoadCardInstanceAsync(
                    p.CardID!.Value,
                    this.dbScope.Db,
                    this.cardMetadata,
                    p.ValidationResult,
                    cancellationToken: p.CancellationToken);

                if (!p.ValidationResult.IsSuccessful())
                {
                    return;
                }

                cardForSignal = getContext!.Card;
            }

            cardForSignal
                .GetWorkflowQueue()
                .AddSignal(
                    externalProcessType,
                    signalName,
                    processID: externalProcessID,
                    parameters: signalParameters);

            var digest = WorkflowScopeContext.Current.StoreContext?.Request is { } workflowRequest
                ? workflowRequest.TryGetDigest()
                : await this.cardRepository.GetDigestAsync(
                    cardForSignal,
                    CardDigestEventNames.ActionHistoryStoreApprovalProcess,
                    p.CancellationToken);

            cardForSignal.RemoveAllButChanged(cardForSignal.StoreMode);

            var storeRequest = new CardStoreRequest
            {
                Card = cardForSignal,
            };
            storeRequest.SetDigest(digest);
            this.cardServerPermissionsProvider.SetFullPermissions(storeRequest);

            var storeResponse = await this.cardRepository.StoreAsync(
                storeRequest,
                p.CancellationToken);

            p.ValidationResult.Add(storeResponse.ValidationResult);
        }

        private async Task RevokeProcessCoreAsync(
            ApprovalProcessInstance instance,
            ICardTransactionParameter p)
        {
            // Алгоритм отзыва.
            // Алгоритм одинаковый независимо от инициатора отзыва. Типовые инициаторы отзыва:
            // 1. Этап "Процесс согласования" при обработке сигнала KrConstants.ApprovalProcessStopSignal.
            // 2. Этап "Управление процессом согласования" при отзыве активных процессов, если тип ExternalWorkflowType обрабатывается этим обработчиком.
            // 3. При отзыве по кнопке "Отозвать" из редактора процесса согласования, если тип ExternalWorkflowType обрабатывается этим обработчиком.

            // 1. Отзыв инициируется вызовом метода IApprovalProcessRunner.RevokeProcessAsync;
            // 2. Остановка процесса согласования;
            // 3. Выполнение вторичного процесса отзыва (если задан);
            // 4. Уведомление активных этапов о выполнении отзыва процесса согласования.
            //    Это необходимо для уведомления активных этапов в случае, если вторичный процесс отзыва не выполнял отзыв этапа "Процесс согласования" или аналогичного. Если отзыв был выполнен, то ничего не произойдёт.

            // 1. Остановка процесса согласования.
            var stopResult = await this.StopApprovalProcessAsync(
                instance,
                p.ValidationResult,
                p.CancellationToken);

            if (!stopResult.IsSuccessful)
            {
                return;
            }

            // 2. Выполнение вторичного процесса отзыва.
            await this.ExecuteSecondaryRevokeProcessAsync(
                instance,
                p.CardID!.Value,
                p.ValidationResult,
                p.CancellationToken);

            if (!p.ValidationResult.IsSuccessful())
            {
                return;
            }

            // 3. Уведомление активных этапов о выполнении отзыва процесса согласования.
            await this.SendApprovalProcessStopCompletedSignalAsync(
                instance,
                p);
        }

        private async ValueTask<ApprovalProcessExecutionResult> StopApprovalProcessAsync(
            ApprovalProcessInstance instance,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken)
        {
            var request = new ApprovalProcessExecutionRequest()
            {
                CardID = instance.CardID,
                Instance = instance,
                ValidationResult = validationResult,
                ExecutionDateTime = DateTime.UtcNow,
            };

            this.InitRequestFromKrScope(request);

            return await this.approvalProcessManager.StopApprovalProcessAsync(
                request,
                cancellationToken);
        }

        private void InitRequestFromKrScope(
            ApprovalProcessExecutionRequest request)
        {
            if (!this.krScope.Exists)
            {
                return;
            }

            request.GetCardFuncAsync = async (cardID, result, forceLoadTasks, ct) =>
            {
                var card = await this.krScope.GetMainCardAsync(
                    cardID,
                    validationResult: result,
                    cancellationToken: ct);

                if (forceLoadTasks
                    && card is not null)
                {
                    await this.krScope.EnsureTasksLoadedAsync(
                        cardID,
                        result,
                        ct);
                }

                return card;
            };
            request.GetCardFileContainerFuncAsync = (cardID, result, ct) =>
                new(this.krScope
                    .GetMainCardFileContainerAsync(
                        cardID,
                        validationResult: result,
                        cancellationToken: ct));
            request.GetSatelliteFuncAsync = (typeID, cardID, taskID, result, ct) =>
                this.krScope.GetSatelliteAsync(
                    cardID,
                    taskID,
                    typeID,
                    validationResult: result,
                    cancellationToken: ct);
            request.ModifyStoreRequestActionOverride = this.krScope.ModifyStoreRequest;
            request.PreventNextCardStore = true;
        }

        private async ValueTask ExecuteSecondaryRevokeProcessAsync(
            ApprovalProcessInstance instance,
            Guid cardID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken)
        {
            var secondaryRevokeProcessID = instance.Settings.Info?.TryGet<Guid>(KrConstants.Keys.ApprovalProcessSecondaryRevokeProcessID);

            if (!secondaryRevokeProcessID.HasValue)
            {
                return;
            }

            var secondaryRevokeProcess = KrProcessBuilder
                .CreateProcess()
                .SetProcess(secondaryRevokeProcessID.Value)
                .SetCard(cardID)
                .Build();

            var launchResult = await this.krProcessLauncher.LaunchAsync(
                secondaryRevokeProcess,
                specificParameters: new KrProcessServerLauncher.SpecificParameters()
                {
                    RaiseErrorWhenExecutionIsForbidden = true,
                },
                cancellationToken: cancellationToken);

            validationResult.Add(launchResult.ValidationResult);
        }

        private async Task SendApprovalProcessStopCompletedSignalAsync(
            ApprovalProcessInstance instance,
            ICardTransactionParameter p)
        {
            // Проверка наличия активного процесса.
            // Замечание: Проверка не сработает, если отмена была запланирована с помощью глобального сигнала. Т.к. он будет выполнен при обработке карточек в IKrScope. Проверять наличие глобального сигнала некорректно, т.к. он ещё должен выполниться. Также отмена выполнения этапа (отзыв) может выполняться с помощью разных сигналов и при разных условиях и учитывать их нет смысла.
            var isExistsProcess = await this.DoesProcessExistAsync(
                p.DbScope.Db,
                instance.ExternalProcessID!.Value,
                p.CancellationToken);

            if (!isExistsProcess)
            {
                return;
            }

            var cardForSignal = await this.TryGetCardForSignalAsync(
                p.CardID!.Value,
                p.ValidationResult,
                p.CancellationToken);

            if (!p.ValidationResult.IsSuccessful())
            {
                return;
            }

            await this.SendSignalCoreAsync(
                instance.ExternalProcessID!.Value,
                instance.ExternalWorkflowType!,
                KrConstants.ApprovalProcessStopCompletedSignal,
                new Dictionary<string, object?>
                {
                    { KrConstants.ApprovalProcessSignalInstanceIDParam, instance.ID },
                },
                p,
                cardForSignal);
        }

        private Task<bool> DoesProcessExistAsync(
            DbManager db,
            Guid processID,
            CancellationToken cancellationToken) =>
            db
                .SetCommand(
                    this.dbScope.BuilderFactory
                        .SelectExists(static b => b
                            .Select().V(null)
                            .From("WorkflowProcesses").NoLock()
                            .Where().C("RowID").Equals().P("ProcessID"))
                        .Build(),
                    db.Parameter("ProcessID", processID, DataType.Guid))
                .LogCommand()
                .ExecuteAsync<bool>(cancellationToken);

        private async ValueTask<Card?> TryGetCardForSignalAsync(
            Guid cardID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken)
        {
            if (this.krScope.Exists
                && this.krScope.CardIsLoaded(cardID))
            {
                var card = await this.krScope.GetMainCardAsync(
                    cardID,
                    validationResult: validationResult,
                    cancellationToken: cancellationToken);

                if (card is null)
                {
                    return null;
                }

                var cardForSignal = card.Clone();
                cardForSignal.RemoveChanges(CardRemoveChangesDeletedHandling.Remove);
                cardForSignal.RemoveWorkflowQueue();
                cardForSignal.RemoveNumberQueue();

                return cardForSignal;
            }

            return null;
        }

        private async ValueTask DeleteProcessCoreAsync(
            ApprovalProcessInstance instance,
            ICardTransactionParameter p)
        {
            // 1. Остановка процесса согласования.
            if (instance.State == ApprovalProcessState.Running)
            {
                var stopResult = await this.StopApprovalProcessAsync(
                    instance,
                    p.ValidationResult,
                    p.CancellationToken);

                if (!stopResult.IsSuccessful)
                {
                    return;
                }
            }

            // 2. Уведомление активных этапов о выполнении отзыва процесса согласования.
            await this.SendApprovalProcessStopCompletedSignalAsync(
                instance,
                p);

            if (!p.ValidationResult.IsSuccessful())
            {
                return;
            }

            // 3. Удаление экземпляра процесса согласования.
            await this.approvalProcessInstanceRepository.DeleteInstanceAsync(
                instance.ID,
                p.CancellationToken);
        }

        private async Task UpdateExternalProcessAsync(
            IKrProcessLaunchResult launchResult,
            Guid instanceID,
            CancellationToken cancellationToken = default)
        {
            // При запуске возможны следующие случаи:
            // 1. Запускается асинхронный вторичный процесс.
            //     Идентификатор запущенного процесса будет в IKrProcessLaunchResult.ProcessID.
            //
            // 2. Запускается синхронный вторичный процесс.
            // 2.1. Запущенный процесс не запускает основной процесс или не передаёт идентификатор запущенного процесса, который требуется контролировать.
            //     Информации о контролируемом процессе нет. Ничего делать ненужно.
            //
            // 2.2. Запущенный процесс с помощью этапа "Управление процессом" запускает основной процесс через отправку сигнала KrStartProcessSignal или KrStartProcessUnlessStartedGlobalSignal.
            //     Свойство IKrProcessLaunchResult.ProcessID будет равно null, т.к. процесс синхронный. При отправке сигнала этап "Управление процессом" сохранит идентификатор запущенного процесса в параметры выполненного вторичного процесса по ключу KrConstants.KrProcessID.
            //
            // 2.3. Частный случай 2.2. Запущенный процесс возвращает идентификатор запущенного альтернативным способом асинхронного процесса через KrConstants.KrProcessID.

            var processID = launchResult.ProcessID
                ?? launchResult.ProcessInfo?.TryGet<Guid?>(KrConstants.KrProcessID);

            if (!processID.HasValue)
            {
                return;
            }

            // Получение типа процесса по его ИД.
            await using var _ = this.dbScope.Create();

            var db = this.dbScope.Db;
            var externalProcessType = await db
                .SetCommand(
                    this.dbScope.BuilderFactory
                        .Select().C("TypeName")
                        .From("WorkflowProcesses").NoLock()
                        .Where().C("RowID").Equals().P("ProcessID")
                        .Build(),
                    db.Parameter("ProcessID", processID, DataType.Guid))
                .LogCommand()
                .ExecuteAsync<string>(cancellationToken);

            await this.approvalProcessInstanceRepository.UpdateInstanceExternalProcessAsync(
                instanceID,
                processID,
                externalProcessType,
                cancellationToken);
        }

        #endregion
    }
}
