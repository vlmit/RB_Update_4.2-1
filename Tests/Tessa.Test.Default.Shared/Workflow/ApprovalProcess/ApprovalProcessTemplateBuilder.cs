#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Test.Default.Shared.Kr;
using Tessa.Workflow.ApprovalProcess;
using AP = Tessa.Workflow.ApprovalProcess.ApprovalProcess;

namespace Tessa.Test.Default.Shared.Workflow.ApprovalProcess
{
    /// <summary>
    /// Предоставляет методы для создания и модификации карточки шаблона процесса согласования.
    /// </summary>
    public sealed class ApprovalProcessTemplateBuilder :
        CardLifecycleCompanion<ApprovalProcessTemplateBuilder>,
        INamedEntry
    {
        #region Fields

        private readonly IApprovalProcessMapper approvalProcessMapper;

        private ApprovalProcessBuilder approvalProcessBuilder = new();
        private ApprovalProcessTemplateSettings approvalProcessTemplateSettings = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/></param>
        /// <param name="approvalProcessMapper"><inheritdoc cref="IApprovalProcessMapper" path="/summary"/></param>
        public ApprovalProcessTemplateBuilder(
            ICardLifecycleCompanionDependencies deps,
            IApprovalProcessMapper approvalProcessMapper)
            : this(Guid.NewGuid(), deps, approvalProcessMapper)
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="cardID">Идентификатор шаблона процесса согласования.</param>
        /// <param name="deps"><inheritdoc cref="ICardLifecycleCompanionDependencies" path="/summary"/>.</param>
        /// <param name="approvalProcessMapper"><inheritdoc cref="IApprovalProcessMapper" path="/summary"/></param>
        public ApprovalProcessTemplateBuilder(
            Guid cardID,
            ICardLifecycleCompanionDependencies deps,
            IApprovalProcessMapper approvalProcessMapper)
            : base(
                  cardID,
                  CardHelper.ApprovalProcessTemplateTypeID,
                  CardHelper.ApprovalProcessTemplateTypeName,
                  deps) =>
            this.approvalProcessMapper = NotNullOrThrow(approvalProcessMapper);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override ApprovalProcessTemplateBuilder Load(
            Action<CardGetRequest>? modifyRequestAction = null)
        {
            base.Load(modifyRequestAction);

            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (action, ct) =>
                    {
                        var card = this.GetCardOrThrow();

                        var context = new ApprovalProcessMappingContext()
                        {
                            Card = card,
                        };

                        this.approvalProcessMapper.MapToProcess(context);

                        this.approvalProcessTemplateSettings = context.TemplateSettings ?? new();
                        this.approvalProcessBuilder = new(context.Process);

                        return new ValueTask<ValidationResult>(ValidationResult.Empty);
                    }));

            return this;
        }

        /// <inheritdoc/>
        public override ApprovalProcessTemplateBuilder Save(
            Action<CardStoreRequest>? modifyRequestAction = null)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (action, ct) =>
                    {
                        var card = this.GetCardOrThrow();
                        var process = this.GetApprovalProcess();

                        var context = new ApprovalProcessMappingContext()
                        {
                            Card = card,
                            Process = process,
                            TemplateSettings = this.approvalProcessTemplateSettings,
                        };

                        this.approvalProcessMapper.MapToCard(context);

                        return new ValueTask<ValidationResult>(ValidationResult.Empty);
                    }));

            return base.Save(modifyRequestAction);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Задаёт заголовок шаблона процесса согласования.
        /// </summary>
        /// <param name="title">Заголовок шаблона процесса согласования.</param>
        /// <returns>Текущий объект для построения цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public ApprovalProcessTemplateBuilder SetTitle(string title)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (action, ct) =>
                    {
                        this.approvalProcessBuilder.SetTitle(
                            title);

                        return new ValueTask<ValidationResult>(ValidationResult.Empty);
                    }));

            return this;
        }

        /// <summary>
        /// Возвращает заголовок шаблона процесса согласования.
        /// </summary>
        /// <returns>Заголовок шаблона процесса согласования.</returns>
        public string? GetTitle() =>
            this.approvalProcessBuilder.GetTitle();

        /// <summary>
        /// Задаёт описание шаблона процесса согласования.
        /// </summary>
        /// <param name="description">Описание шаблона процесса согласования.</param>
        /// <returns>Текущий объект для построения цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public ApprovalProcessTemplateBuilder SetDescription(string description)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (action, ct) =>
                    {
                        this.approvalProcessTemplateSettings.Description = description;

                        return new ValueTask<ValidationResult>(ValidationResult.Empty);
                    }));

            return this;
        }

        /// <summary>
        /// Возвращает описание шаблона процесса согласования.
        /// </summary>
        /// <returns>Описание шаблона процесса согласования.</returns>
        public string? GetDescription() =>
            this.approvalProcessTemplateSettings.Description;

        /// <summary>
        /// Добавляет следующий узел с указанными согласующими.
        /// </summary>
        /// <param name="text">Текст задания для согласующего.</param>
        /// <param name="duration">Срок выполнения задания.</param>
        /// <param name="approvers">Список согласующих. Не должен быть пустым.</param>
        /// <returns>Текущий объект для построения цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public ApprovalProcessTemplateBuilder AddNextNode(
            string? text = null,
            double duration = 1,
            params Guid[] approvers)
        {
            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (_, _) =>
                    {
                        this.approvalProcessBuilder.AddNextNode(
                            out _,
                            text,
                            duration,
                            approvers);

                        return new ValueTask<ValidationResult>(ValidationResult.Empty);
                    }));

            return this;
        }

        /// <summary>
        /// Изменяет процесс согласования.
        /// </summary>
        /// <param name="modifyFunc">Функция для модификации процесса согласования.</param>
        /// <returns>Текущий объект для построения цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public ApprovalProcessTemplateBuilder ModifyApprovalProcess(
            Func<ApprovalProcessBuilder, CancellationToken, ValueTask> modifyFunc)
        {
            ThrowIfNull(modifyFunc);

            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    async (_, ct) =>
                    {
                        await modifyFunc(this.approvalProcessBuilder, ct);

                        return ValidationResult.Empty;
                    }));

            return this;
        }

        /// <summary>
        /// Добавляет роли, которые могут использовать шаблон процесса согласования.
        /// </summary>
        /// <param name="users">Идентификаторы ролей, добавляемые в список ролей, которые могут использовать шаблон процесса согласования.</param>
        /// <returns>Текущий объект для построения цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public ApprovalProcessTemplateBuilder AddUsers(
            params Guid[] users)
        {
            ThrowIfNull(users);

            if (users.Length == 0)
            {
                return this;
            }

            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (_, _) =>
                    {
                        foreach (var user in users)
                        {
                            this.approvalProcessTemplateSettings.Users.Add(
                                new()
                                {
                                    ["RowID"] = Guid.NewGuid(),
                                    ["RoleID"] = user,
                                    ["RoleName"] = user.ToString(),
                                });
                        }

                        return new ValueTask<ValidationResult>(ValidationResult.Empty);
                    }));

            return this;
        }

        /// <summary>
        /// Удаляет роли, которые могут использовать шаблон процесса согласования.
        /// </summary>
        /// <param name="users">Идентификаторы ролей, удаляемые из списка ролей, которые могут использовать шаблон процесса согласования.</param>
        /// <returns>Текущий объект для построения цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public ApprovalProcessTemplateBuilder RemoveUsers(
            params Guid[] users)
        {
            ThrowIfNull(users);

            if (users.Length == 0)
            {
                return this;
            }

            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (_, _) =>
                    {
                        this.approvalProcessTemplateSettings.Users.RemoveAll(x =>
                            users.Contains(x.TryGet<Guid>("RoleID")));

                        return new ValueTask<ValidationResult>(ValidationResult.Empty);
                    }));

            return this;
        }

        /// <summary>
        /// Добавляет роли, которые могут редактировать шаблон процесса согласования.
        /// </summary>
        /// <param name="writers">Идентификаторы ролей, добавляемые в список ролей, которые могут редактировать шаблон процесса согласования.</param>
        /// <returns>Текущий объект для построения цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public ApprovalProcessTemplateBuilder AddWriters(
            params Guid[] writers)
        {
            ThrowIfNull(writers);

            if (writers.Length == 0)
            {
                return this;
            }

            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (_, _) =>
                    {
                        foreach (var user in writers)
                        {
                            this.approvalProcessTemplateSettings.Writers.Add(
                                new()
                                {
                                    ["RowID"] = Guid.NewGuid(),
                                    ["RoleID"] = user,
                                    ["RoleName"] = user.ToString(),
                                });
                        }

                        return new ValueTask<ValidationResult>(ValidationResult.Empty);
                    }));

            return this;
        }

        /// <summary>
        /// Удаляет роли, которые могут редактировать шаблон процесса согласования.
        /// </summary>
        /// <param name="writers">Идентификаторы ролей, удаляемые из списка ролей, которые могут редактировать шаблон процесса согласования.</param>
        /// <returns>Текущий объект для построения цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public ApprovalProcessTemplateBuilder RemoveWriters(
            params Guid[] writers)
        {
            ThrowIfNull(writers);

            if (writers.Length == 0)
            {
                return this;
            }

            this.AddPendingAction(
                new PendingAction(
                    TestHelper.GetCallerMemberFullName(this),
                    (_, _) =>
                    {
                        this.approvalProcessTemplateSettings.Writers.RemoveAll(x =>
                            writers.Contains(x.TryGet<Guid>("RoleID")));

                        return new ValueTask<ValidationResult>(ValidationResult.Empty);
                    }));

            return this;
        }

        /// <summary>
        /// Возвращает текущий шаблон процесса согласования.
        /// </summary>
        /// <returns><inheritdoc cref="AP" path="/summary"/></returns>
        public AP GetApprovalProcess()
        {
            var process = this.approvalProcessBuilder.Build();
            process.ID = this.CardID;
            return process;
        }

        /// <summary>
        /// Возвращает настройки текущего процесса согласования шаблона.
        /// </summary>
        /// <returns><inheritdoc cref="ApprovalProcessTemplateSettings" path="/summary"/></returns>
        public ApprovalProcessTemplateSettings GetSettings() =>
            this.approvalProcessTemplateSettings.DeepClone();

        #endregion

        #region INamedEntry Members

        /// <summary>
        /// Возвращает идентификатор объекта.
        /// </summary>
        /// <exception cref="NotSupportedException">Установка значения не поддерживается.</exception>
        Guid INamedEntry.ID
        {
            get => this.CardID;
            set => throw new NotSupportedException("Setting the value is not supported.");
        }

        /// <summary>
        /// Возвращает название объекта.
        /// </summary>
        /// <exception cref="NotSupportedException">Установка значения не поддерживается.</exception>
        string? INamedEntry.Name
        {
            get => this.GetTitle();
            set => throw new NotSupportedException("Setting the value is not supported.");
        }

        #endregion

        #region INamedItem Members

        /// <inheritdoc/>
        string INamedItem.Name => this.GetTitle() ?? string.Empty;

        #endregion
    }
}
