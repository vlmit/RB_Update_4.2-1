#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Files;
using Tessa.Platform.Data;
using Tessa.Platform.Formatting;
using Tessa.Platform.Operations;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Test.Default.Shared.Kr;
using Tessa.Test.Default.Shared.Platform.Operations;
using Tessa.Workflow;
using Tessa.Workflow.Signals;
using Tessa.Workflow.Storage;
using Unity;

namespace Tessa.Test.Default.Shared.Workflow
{
    /// <summary>
    /// Предоставляет методы для управления жизненным циклом карточки, в которой запущен экземпляр бизнес-процесса.
    /// </summary>
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public class WeProcessInstanceLifecycleCompanion :
        ICardLifecycleCompanion<WeProcessInstanceLifecycleCompanion>
    {
        #region Fields

        private readonly CardLifecycleCompanion cardLifecycle;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="WeProcessInstanceLifecycleCompanion"/>.
        /// </summary>
        /// <param name="cardLifecycle">Объект, управляющий жизненным циклом карточки, в которой запущен бизнес-процесс.</param>
        /// <param name="dependencies"><inheritdoc cref="WeDependencies" path="/summary"/></param>
        public WeProcessInstanceLifecycleCompanion(
            CardLifecycleCompanion cardLifecycle,
            IWeLifecycleCompanionDependencies dependencies)
        {
            this.cardLifecycle = NotNullOrThrow(cardLifecycle);
            this.WeDependencies = NotNullOrThrow(dependencies);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Идентификатор экземпляра бизнес-процесса, которым управляет этот объект, или значение <see langword="null"/>, если он не задан.
        /// </summary>
        public Guid? ProcessInstanceID { get; set; }

        /// <inheritdoc cref="IWeLifecycleCompanionDependencies" path="/summary"/>
        public IWeLifecycleCompanionDependencies WeDependencies { get; }

        /// <inheritdoc/>
        public Guid CardID => this.cardLifecycle.CardID;

        /// <inheritdoc/>
        public Guid? CardTypeID => this.cardLifecycle.CardTypeID;

        /// <inheritdoc/>
        public string CardTypeName => this.cardLifecycle.CardTypeName;

        /// <inheritdoc/>
        public Card Card => this.cardLifecycle.Card;

        /// <inheritdoc/>
        public ICardLifecycleCompanionDependencies Dependencies => this.cardLifecycle.Dependencies;

        /// <inheritdoc/>
        public ICardLifecycleCompanionData LastData => this.cardLifecycle.LastData;

        /// <inheritdoc/>
        public Dictionary<string, object?> Info => this.cardLifecycle.Info;

        #endregion

        #region Public Methods

        /// <summary>
        /// Возвращает значение, показывающее, что процесс, с идентификатором <see cref="ProcessInstanceID"/>, является активным.
        /// </summary>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Значение <see langword="true"/>, если процесс является активным, иначе - <see langword="false"/>.</returns>
        public async Task<bool> IsAliveAsync(
            CancellationToken cancellationToken = default) =>
            (await this.GetProcessInstanceAsync(cancellationToken)).Item1 is not null;

        /// <summary>
        /// Возвращает экземпляр процесса с идентификатором <see cref="ProcessInstanceID"/>.
        /// </summary>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Кортеж: &lt;Экземпляр процесса WorkflowEngine или значение <see langword="null"/>, если процесс не активен; Результат выполнения&gt;.</returns>
        public Task<(WorkflowProcessStateStorage?, ValidationResult)> GetProcessInstanceAsync(
            CancellationToken cancellationToken = default) =>
            this.WeDependencies.WorkflowService.GetProcessStateAsync(this.GetProcessInstanceIDOrThrow(), cancellationToken);

        /// <summary>
        /// Отправляет указанный сигнал на все подписанные на него узлы процесса.
        /// </summary>
        /// <param name="signalType">Тип сигнала.</param>
        /// <param name="signalHash">Дополнительная информация.</param>
        /// <returns>Объект <see cref="WeProcessInstanceLifecycleCompanion"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="GoAsync(Action{ValidationResult}, CancellationToken)"/>.<para/>
        /// После отправки сигнала выполняется загрузка карточки.<para/>
        /// Обратите внимание: после выполнения метода, последним запланированным действием становится загрузка карточки.<para/>
        /// Последний запрос на обработку сигнала в <see cref="IWorkflowEngineProcessor"/> и ответ на него можно получить в <see cref="LastData"/> в свойствах <see cref="ICardLifecycleCompanionData.OtherRequests"/> и <see cref="ICardLifecycleCompanionData.OtherResponses"/> по ключам <see cref="WorkflowTestHelper.WorkflowEngineProcessRequestKey"/> и <see cref="WorkflowTestHelper.WorkflowEngineProcessResultKey"/>, соответственно.
        /// </remarks>
        public WeProcessInstanceLifecycleCompanion SendSignal(
            string signalType,
            Dictionary<string, object?>? signalHash = null)
        {
            var signal = new WorkflowEngineSignal(
                signalType,
                signalHash);

            this.Load()
                .GetLastPendingAction()
                .AddPreparationAction(new PendingAction(
                    TestHelper.GetCallerMemberFullName(this, nameof(String), "Dictionary<string, object>"),
                    async (_, ct) =>
                    {
                        var processRequest = new WorkflowEngineProcessRequest
                        {
                            ProcessFlag = WorkflowEngineProcessFlags.DefaultRuntime
                                | WorkflowEngineProcessFlags.SendToSubscribers,
                            StoreCard = this.GetCardOrThrow(),
                            Signal = signal,
                            ProcessInstanceID = this.ProcessInstanceID,
                        };

                        return await this.SendSignalCoreAsync(
                            processRequest,
                            ct);
                    }));

            return this;
        }

        /// <summary>
        /// Отправляет указанный сигнал, выполняющий создание нового экземпляра процесса.
        /// </summary>
        /// <param name="signal"><inheritdoc cref="IWorkflowEngineSignal" path="/summary"/></param>
        /// <param name="processTemplateID">Идентификатор шаблона бизнес процесса или значение <see langword="null"/>, если сигнал должен быть отправлен в соответствии с <paramref name="processTemplateVersionID"/>.</param>
        /// <param name="processTemplateVersionID">Идентификатор версии шаблона бизнес процесса или значение <see langword="null"/>, если сигнал должен быть отправлен версии по умолчанию.</param>
        /// <param name="processInstanceID">Идентификатор экземпляра бизнес-процесса или значение <see langword="null"/>, если он должен быть создан случайным.</param>
        /// <param name="processFlag"><inheritdoc cref="WorkflowEngineProcessFlags" path="/summary"/></param>
        /// <returns>Объект <see cref="WeProcessInstanceLifecycleCompanion"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="GoAsync(Action{ValidationResult}, CancellationToken)"/>.<para/>
        ///
        /// После отправки сигнала выполняется загрузка карточки.<para/>
        ///
        /// Обратите внимание: после выполнения метода, последним запланированным действием становится загрузка карточки.<para/>
        ///
        /// Последний запрос на обработку сигнала в <see cref="IWorkflowEngineProcessor"/> и ответ на него можно получить в <see cref="LastData"/> в свойствах <see cref="ICardLifecycleCompanionData.OtherRequests"/> и <see cref="ICardLifecycleCompanionData.OtherResponses"/> по ключам <see cref="WorkflowTestHelper.WorkflowEngineProcessRequestKey"/> и <see cref="WorkflowTestHelper.WorkflowEngineProcessResultKey"/>, соответственно.
        /// </remarks>
        public WeProcessInstanceLifecycleCompanion StartNew(
            IWorkflowEngineSignal signal,
            Guid? processTemplateID = null,
            Guid? processTemplateVersionID = null,
            Guid? processInstanceID = null,
            WorkflowEngineProcessFlags processFlag = WorkflowEngineProcessFlags.DefaultNew)
        {
            ThrowIfNull(signal);

            if (!processTemplateID.HasValue
                && !processTemplateVersionID.HasValue)
            {
                throw new ArgumentException($"The parameters {nameof(processTemplateID)} and {nameof(processTemplateVersionID)} are null.");
            }

            this.Load()
                .GetLastPendingAction()
                .AddPreparationAction(new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    async (_, ct) =>
                    {
                        var card = this.GetCardOrThrow();
                        this.ProcessInstanceID = processInstanceID ?? Guid.NewGuid();

                        var processRequest = new WorkflowEngineProcessRequest
                        {
                            ProcessFlag = processFlag,
                            StoreCard = card,
                            Signal = signal,
                            ProcessInstanceID = this.ProcessInstanceID,
                        };

                        if (processTemplateVersionID.HasValue)
                        {
                            processRequest.ProcessTemplateVersionID = processTemplateVersionID.Value;
                        }
                        else
                        {
                            processRequest.ProcessTemplateID = processTemplateID;
                        }

                        return await this.SendSignalCoreAsync(
                            processRequest,
                            ct);
                    }));

            return this;
        }

        /// <summary>
        /// Отправляет указанный сигнал.
        /// </summary>
        /// <param name="processRequest"><inheritdoc cref="IWorkflowEngineProcessRequest" path="/summary"/></param>
        /// <returns>Объект <see cref="WeProcessInstanceLifecycleCompanion"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="GoAsync(Action{ValidationResult}, CancellationToken)"/>.<para/>
        /// После отправки сигнала выполняется загрузка карточки.<para/>
        /// Обратите внимание: после выполнения метода, последним запланированным действием становится загрузка карточки.<para/>
        /// Последний запрос на обработку сигнала в <see cref="IWorkflowEngineProcessor"/> и ответ на него можно получить в <see cref="LastData"/> в свойствах <see cref="ICardLifecycleCompanionData.OtherRequests"/> и <see cref="ICardLifecycleCompanionData.OtherResponses"/> по ключам <see cref="WorkflowTestHelper.WorkflowEngineProcessRequestKey"/> и <see cref="WorkflowTestHelper.WorkflowEngineProcessResultKey"/>, соответственно.
        /// </remarks>
        public WeProcessInstanceLifecycleCompanion SendSignal(
            IWorkflowEngineProcessRequest processRequest)
        {
            ThrowIfNull(processRequest);

            this.Load()
                .GetLastPendingAction()
                .AddPreparationAction(new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (_, ct) =>
                        new(this.SendSignalCoreAsync(
                            processRequest,
                            ct))));

            return this;
        }

        /// <summary>
        /// Выполняет асинхронные операции созданные бизнес-процессами.
        /// </summary>
        /// <param name="executeNewOperations">Значение <see langword="true"/>, если необходимо выполнить операции созданные после выполнения других операций, иначе - <see langword="false"/>, если необходимо выполнить только текущие операции. Значение по умолчанию: <see langword="true"/>.</param>
        /// <param name="executeAllProcessOperations">Значение <see langword="true"/>, если должны быть выполнены все операции, иначе - <see langword="false"/>, если должны быть выполнены только операции созданные процессом с идентификатором <see cref="ProcessInstanceID"/> и его дочерними процессами. Значение по умолчанию: <see langword="false"/>.</param>
        /// <param name="maxParallelThreads"><inheritdoc cref="TestOperationExecutorOptions.MaxParallelThreads" path="/summary"/></param>
        /// <returns>Объект <see cref="WeProcessInstanceLifecycleCompanion"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="GoAsync(Action{ValidationResult}, CancellationToken)"/>.<para/>
        /// После обработки операций выполняется загрузка карточки.<para/>
        /// Обратите внимание: после выполнения метода, последним запланированным действием становится загрузка карточки.
        /// </remarks>
        public WeProcessInstanceLifecycleCompanion ProcessAsyncOperations(
            bool executeNewOperations = true,
            bool executeAllProcessOperations = false,
            int maxParallelThreads = TestOperationExecutorOptions.DefaultMaxParallelThreads)
        {
            this.Load()
                .GetLastPendingAction()
                .AddPreparationAction(new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (_, ct) => new(this.ProcessAsyncOperationsAsync(
                        executeNewOperations,
                        executeAllProcessOperations,
                        maxParallelThreads,
                        ct))));

            return this;
        }

        /// <summary>
        /// Обрабатывает все активные таймеры, расположенные в процессе с идентификатором <see cref="ProcessInstanceID"/> и его дочерних процессах.
        /// </summary>
        /// <returns>Объект <see cref="WeProcessInstanceLifecycleCompanion"/> для создания цепочки.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="GoAsync(Action{ValidationResult}, CancellationToken)"/>.<para/>
        /// Этот метод создаёт асинхронные операции, а не запускает их на обработку. Необходимо вызывать метод <see cref="ProcessAsyncOperations"/>, если требуется обработка сигналов, создаваемых таймером.<para/>
        /// После обработки таймеров выполняется загрузка карточки.<para/>
        /// Обратите внимание: после выполнения метода, последним запланированным действием становится загрузка карточки.<para/>
        /// </remarks>
        public WeProcessInstanceLifecycleCompanion ProcessTimerOperations()
        {
            this.Load()
                .GetLastPendingAction()
                .AddPreparationAction(
                    new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (_, ct) => new(this.ProcessTimerOperationsAsync(ct))));

            return this;
        }

        /// <summary>
        /// Возвращает идентификатор экземпляра бизнес-процесса, которым управляет этот объект или создаёт исключение, если он не задан.
        /// </summary>
        /// <returns>Идентификатор экземпляра бизнес-процесса.</returns>
        /// <exception cref="InvalidOperationException">Процесс не запущен. Запустить процесс можно с помощью метода <see cref="StartNew"/>.</exception>
        public Guid GetProcessInstanceIDOrThrow() =>
            this.ProcessInstanceID ?? throw new InvalidOperationException($"The process is not running. The process can be started using the {nameof(StartNew)} method.");

        /// <summary>
        /// Выполняет регистрацию обработчика <paramref name="executor"/> для выполнения действия с заданным именем.
        /// </summary>
        /// <param name="actionName">Имя действия.</param>
        /// <param name="executor"><inheritdoc cref="IWeInterprocessExecutor" path="/summary"/></param>
        /// <returns>Объект <see cref="WeProcessInstanceLifecycleCompanion"/> для создания цепочки.</returns>
        /// <remarks>Обработчик регистрируется для конкретного экземпляра процесса, поэтому метод должен быть вызван после того, как процесс был запущен.</remarks>
        public WeProcessInstanceLifecycleCompanion RegisterExecutor(
            string actionName,
            IWeInterprocessExecutor executor)
        {
            this.WeDependencies.UnityContainer.RegisterInstance<Func<IWorkflowEngineContext, object[], ValueTask>>($"{this.GetProcessInstanceIDOrThrow()}_{actionName}", executor.ExecuteAsync);

            return this;
        }

        #endregion

        #region ICardLifecycleCompanion<T> Members

        /// <inheritdoc/>
        public WeProcessInstanceLifecycleCompanion Create(Action<CardNewRequest>? modifyRequestAction = null)
        {
            this.cardLifecycle.Create(modifyRequestAction);
            return this;
        }

        /// <inheritdoc/>
        public WeProcessInstanceLifecycleCompanion Save(Action<CardStoreRequest>? modifyRequestAction = null)
        {
            this.cardLifecycle.Save(modifyRequestAction);
            return this;
        }

        /// <inheritdoc/>
        public WeProcessInstanceLifecycleCompanion Load(Action<CardGetRequest>? modifyRequestAction = null)
        {
            this.cardLifecycle.Load(modifyRequestAction);
            return this;
        }

        /// <inheritdoc/>
        public WeProcessInstanceLifecycleCompanion Delete(Action<CardDeleteRequest>? modifyRequestAction = null)
        {
            this.cardLifecycle.Delete(modifyRequestAction);
            return this;
        }

        /// <inheritdoc/>
        public WeProcessInstanceLifecycleCompanion WithInfoPair(string key, object val)
        {
            this.cardLifecycle.WithInfoPair(key, val);
            return this;
        }

        /// <inheritdoc/>
        public WeProcessInstanceLifecycleCompanion WithInfo(Dictionary<string, object> info)
        {
            this.cardLifecycle.WithInfo(info);
            return this;
        }

        /// <inheritdoc/>
        public WeProcessInstanceLifecycleCompanion CreateOrLoadSingleton()
        {
            _ = this.cardLifecycle.CreateOrLoadSingleton();
            return this;
        }

        /// <inheritdoc/>
        public WeProcessInstanceLifecycleCompanion Export(Action<CardGetRequest>? modifyRequestAction = null)
        {
            _ = this.cardLifecycle.Export();
            return this;
        }

        #endregion

        #region ICardLifecycleCompanion Members

        /// <inheritdoc/>
        public ValueTask<ICardFileContainer> GetCardFileContainerAsync(
            IFileRequest? request = null,
            IList<IFileTag>? additionalTags = null,
            CancellationToken cancellationToken = default)
        {
            return this.cardLifecycle.GetCardFileContainerAsync(
                request: request,
                additionalTags: additionalTags,
                cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public Card GetCardOrThrow() => this.cardLifecycle.GetCardOrThrow();

        #endregion

        #region IExecutePendingActions Members

        /// <inheritdoc/>
        public async ValueTask<WeProcessInstanceLifecycleCompanion> GoAsync(
            Action<ValidationResult>? validationFunc = null,
            CancellationToken cancellationToken = default)
        {
            await this.cardLifecycle.GoAsync(
                validationFunc: validationFunc,
                cancellationToken: cancellationToken);
            return this;
        }

        #endregion

        #region ISealable Members

        /// <inheritdoc/>
        public bool IsSealed => this.cardLifecycle.IsSealed;

        /// <inheritdoc/>
        public void Seal() => this.cardLifecycle.Seal();

        #endregion

        #region IProvidePendingActions Members

        /// <inheritdoc/>
        public bool HasPendingActions => this.cardLifecycle.HasPendingActions;

        /// <inheritdoc/>
        public int Count => this.cardLifecycle.Count;

        /// <inheritdoc/>
        public IPendingAction this[int index] => this.cardLifecycle[index];

        /// <inheritdoc/>
        public void AddPendingAction(IPendingAction pendingAction, bool reverseOrder = false) => this.cardLifecycle.AddPendingAction(pendingAction, reverseOrder);

        /// <inheritdoc/>
        public IPendingAction GetLastPendingAction() => this.cardLifecycle.GetLastPendingAction();

        /// <inheritdoc/>
        public int RemovePendingAction(Predicate<IPendingAction> match) => this.cardLifecycle.RemovePendingAction(match);

        /// <inheritdoc/>
        public IEnumerator<IPendingAction> GetEnumerator() => this.cardLifecycle.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) this.cardLifecycle).GetEnumerator();

        #endregion

        #region Private Methods

        private async Task<ValidationResult> SendSignalCoreAsync(
            IWorkflowEngineProcessRequest processRequest,
            CancellationToken cancellationToken = default)
        {
            this.LastData.OtherRequests[WorkflowTestHelper.WorkflowEngineProcessRequestKey] = processRequest;
            this.LastData.OtherResponses[WorkflowTestHelper.WorkflowEngineProcessResultKey] = null;

            var processResult = await this.WeDependencies.WorkflowEngineProcessor.ProcessSignalAsync(
                processRequest,
                cancellationToken);

            this.LastData.OtherResponses[WorkflowTestHelper.WorkflowEngineProcessResultKey] = processResult;

            return processResult.ValidationResult;
        }

        private async Task<ValidationResult> ProcessAsyncOperationsAsync(
            bool executeNewOperations,
            bool executeAllProcessOperations,
            int maxParallelThreads,
            CancellationToken cancellationToken = default)
        {
            var validationResult = new ValidationResultBuilder();
            List<Guid>? processIDs = null;

            await this.WeDependencies.TestOperationExecutor.ExecuteOperationsAsync(
                async context =>
                {
                    var operation = context.Operation;

                    if (operation.State == OperationState.InProgress)
                    {
                        // Такого быть не должно.
                        return false;
                    }

                    if (!executeAllProcessOperations)
                    {
                        var requestStorage = operation
                            .Request
                            ?.Info
                            .TryGet<Dictionary<string, object?>>("Request");

                        if (requestStorage is null)
                        {
                            context.ValidationResult.AddError(
                                this,
                                $"No {nameof(WorkflowEngineProcessRequest)} found in operation request info for operation {operation.ID:B}");
                            return false;
                        }

                        var request = requestStorage.FromSerializedDictionary<WorkflowEngineProcessRequest>();
                        var currentProcessInstanceID = request.ProcessInstanceID;

                        if (!currentProcessInstanceID.HasValue)
                        {
                            context.ValidationResult.AddError(
                                this,
                                $"Process ID is not specified in operation with ID = {operation.ID:B}.");
                            return false;
                        }

                        // Текущая операция относится к процессу, не входящему в группу процессов, управляемых этим объектом?
                        if (processIDs?.Contains(currentProcessInstanceID.Value) == false)
                        {
                            return false;
                        }
                    }

                    await context.OperationRepository.StartAsync(
                        operation.ID,
                        operation.TypeID,
                        context.CancellationToken);

                    var result = await this.WeDependencies.WorkflowOperationHandler.ProcessOperationAsync(
                        operation,
                        this.WeDependencies.WorkflowOperationHandler.ResolveSettings(),
                        context.CancellationToken);

                    context.ValidationResult.Add(result);

                    return true;
                },
                new TestOperationExecutorOptions(
                    OperationTypes.WorkflowEngineAsync,
                    validationResult)
                {
                    ExecuteNewOperations = executeNewOperations,
                    AllowErrors = true,
                    MaxParallelThreads = maxParallelThreads,
                },
                async context =>
                {
                    // Подготовка к выполнению операций.
                    // Метод выполняется один раз перед обработкой первой операции из предварительно сформированного списка операций.

                    // Здесь формируется список идентификаторов экземпляров процессов, которыми управляет экземпляр процесса с идентификатором processID.
                    // Список обновляется каждый раз после завершения выполнения предварительно отобранных по типу операции. Это позволяет учитывать возможный запуск подпроцесса Workflow Engine.
                    if (!executeAllProcessOperations)
                    {
                        processIDs = await this.GetTreeProcessesAsync(
                            this.GetProcessInstanceIDOrThrow(),
                            context.CancellationToken);
                    }
                },
                cancellationToken);

            return validationResult.Build();
        }

        /// <summary>
        /// Возвращает список идентификаторов экземпляров процессов, которыми управляет экземпляр процесса с заданным идентификатором.
        /// </summary>
        /// <param name="processID">Идентификатор родительского экземпляра процесса.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Список идентификаторов экземпляров процессов, которыми управляет экземпляр процесса с заданным идентификатором.</returns>
        private async Task<List<Guid>> GetTreeProcessesAsync(
            Guid processID,
            CancellationToken cancellationToken = default)
        {
            await using var _ = this.Dependencies.DbScope.Create();

            var db = this.Dependencies.DbScope.Db;

            return await db
                .SetCommand(
                    this.Dependencies.DbScope.BuilderFactory
                        .With("ChildProcess", e => e
                                .Select()
                                    .P("RootRowID")
                                .UnionAll()
                                .Select()
                                    .C("p", "RowID")
                                .From("WorkflowEngineProcesses", "p").NoLock()
                                .InnerJoin("ChildProcess", "cp")
                                .On().C("cp", "RowID").Equals().C("p", "ParentRowID"),
                            columnNames: new[] { "RowID" },
                            recursive: true)
                        .Select()
                            .C("p", "RowID")
                        .From("WorkflowEngineProcesses", "p").NoLock()
                        .InnerJoin("ChildProcess", "cp")
                            .On().C("cp", "RowID").Equals().C("p", "RowID")
                        .Build(),
                    db.Parameter("RootRowID", processID))
                .LogCommand()
                .ExecuteListAsync<Guid>(cancellationToken);
        }

        private async Task<ValidationResult> ProcessTimerOperationsAsync(
            CancellationToken cancellationToken = default)
        {
            // 1. Получение всех подписок таймеров.
            var allTimerSubscriptions = await this.WeDependencies.WorkflowService.GetAllModifiedTimerSubscriptionsAsync(
                null,
                cancellationToken);

            if (allTimerSubscriptions.Count == 0)
            {
                return ValidationResult.Empty;
            }

            // 2. Получение идентификаторов процессов, которыми выполняется управление.
            var processes = await this.GetTreeProcessesAsync(this.GetProcessInstanceIDOrThrow(), cancellationToken);

            // 3. Обработка подписок таймеров.
            var validationResult = new ValidationResultBuilder();

            foreach (var workflowTimerSubscriptionStorage in allTimerSubscriptions)
            {
                if (!processes.Contains(workflowTimerSubscriptionStorage.ProcessID))
                {
                    continue;
                }

                await this.ProcessTimerAsync(
                    workflowTimerSubscriptionStorage,
                    validationResult,
                    cancellationToken);

                if (!validationResult.IsSuccessful())
                {
                    break;
                }
            }

            return validationResult.Build();
        }

        private async Task ProcessTimerAsync(
            WorkflowTimerSubscriptionStorage workflowTimerSubscriptionStorage,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var timerID = workflowTimerSubscriptionStorage.ID;
            var nodeID = workflowTimerSubscriptionStorage.NodeID;
            var processID = workflowTimerSubscriptionStorage.ProcessID;
            var runOnce = workflowTimerSubscriptionStorage.RunOnce;
            var lockProcess = workflowTimerSubscriptionStorage.LockProcess;
            var retryAllowed = workflowTimerSubscriptionStorage.RetryAllowed;
            var syncObject = workflowTimerSubscriptionStorage.SyncObject;

            await using (this.Dependencies.DbScope.Create())
            {
                var db = this.Dependencies.DbScope.Db;
                var builderFactory = this.Dependencies.DbScope.BuilderFactory;

                // Проверяем наличие подписки.
                var timerExists = (
                    await db
                        .SetCommand(
                            builderFactory
                                .Select().Top(1).V(1).From("WorkflowEngineTimerSubscriptions", "ts").NoLock()
                                .Where().C("RowID").Equals().P("TimerID")
                                .Limit(1)
                                .Build(),
                            db.Parameter("TimerID", timerID))
                        .LogCommand()
                        .ExecuteAsync<int?>(cancellationToken)
                    )
                    .HasValue;

                if (!timerExists)
                {
                    return;
                }

                var request = new WorkflowEngineProcessRequest()
                {
                    ProcessInstanceID = processID,
                    Signal = new WorkflowEngineTimerSignal(new Dictionary<string, object?>(StringComparer.Ordinal))
                    {
                        TimerIDs = new List<object> { timerID },
                        Type = WorkflowSignalTypes.TimerTick,
                    },
                    NodeInstanceIDs = [nodeID],
                    ProcessFlag = lockProcess
                                ? WorkflowEngineProcessFlags.DefaultAsync | WorkflowEngineProcessFlags.LockProcess
                                : WorkflowEngineProcessFlags.DefaultAsync,
                    RetryInfo = new()
                    {
                        RetryAllowed = retryAllowed,
                    },
                    SyncObject = syncObject,
                };

                await this.WeDependencies.WorkflowEngineProcessor.SendAsyncSignalAsync(
                    request,
                    cancellationToken);
            }
        }

        /// <summary>
        /// Возвращает строковое представление объекта, отображаемое в окне отладчика.
        /// </summary>
        /// <returns>Строковое представление объекта, отображаемое в окне отладчика.</returns>
        private string GetDebuggerDisplay()
        {
            return $"{nameof(this.CardID)} = {this.CardID:B}, " +
                $"{nameof(this.CardTypeID)} = {FormatNullable(this.CardTypeID, "B")}, " +
                $"{nameof(this.CardTypeName)} = {FormatNullable(this.CardTypeName)}, " +
                $"CardIsSet = {this.Card is not null}";
        }

        #endregion
    }
}
