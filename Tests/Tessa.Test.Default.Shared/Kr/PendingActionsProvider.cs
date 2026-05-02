#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared.Kr
{
    /// <summary>
    /// Объект, предоставляющий методы для работы с отложенными действиями.
    /// </summary>
    /// <typeparam name="TAction">Тип отложенного действия.</typeparam>
    /// <typeparam name="T">Тип объекта запланированные действия которого выполняются методом <see cref="IPendingActionsExecutor{T}.GoAsync(Action{ValidationResult}, CancellationToken)"/>.</typeparam>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "()}")]
    public class PendingActionsProvider<TAction, T> :
        IPendingActionsProvider<TAction, T>
        where TAction : IPendingAction
        where T : PendingActionsProvider<TAction, T>
    {
        #region Fields

        private readonly Lock pendingActionsLock = new();

        private readonly T thisObj;

        #endregion

        #region Properties

        /// <inheritdoc/>
        public bool HasPendingActions => this.Count > 0;

        /// <inheritdoc/>
        public bool IsSealed { get; protected set; }

        /// <inheritdoc/>
        public int Count => this.PendingActions.Count;

        /// <inheritdoc/>
        public TAction this[int index] => this.PendingActions[index];

        /// <summary>
        /// Список, содержащий запланированные для выполнения действия.
        /// </summary>
        protected List<TAction> PendingActions { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="PendingActionsProvider{TAction, T}"/> с пустым списком запланированных действий.
        /// </summary>
        public PendingActionsProvider()
        {
            this.PendingActions = new List<TAction>();
            this.thisObj = (T) this;
        }

        #endregion

        #region Public methods

        /// <inheritdoc/>
        public void AddPendingAction(TAction pendingAction, bool reverseOrder = false)
        {
            ThrowIfNull(pendingAction);
            ThrowIfSealed(this);

            lock (this.pendingActionsLock)
            {
                if (reverseOrder)
                {
                    this.PendingActions.Insert(0, pendingAction);
                }
                else
                {
                    this.PendingActions.Add(pendingAction);
                }
            }
        }

        /// <inheritdoc/>
        public TAction GetLastPendingAction()
        {
            if (!this.HasPendingActions)
            {
                throw new InvalidOperationException("Planned pending actions are absent.");
            }

            return this.PendingActions[^1];
        }

        /// <inheritdoc/>
        public int RemovePendingAction(Predicate<TAction> match)
        {
            ThrowIfNull(match);
            ThrowIfSealed(this);

            lock (this.pendingActionsLock)
            {
                return this.PendingActions.RemoveAll(match);
            }
        }

        /// <inheritdoc/>
        public void Seal() => this.IsSealed = true;

        /// <inheritdoc/>
        public IEnumerator<TAction> GetEnumerator() => this.PendingActions.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) this.PendingActions).GetEnumerator();

        /// <summary>
        /// Подготавливает запланированные действия к выполнению.
        /// </summary>
        /// <param name="pendingActions">Список запланированных действий.</param>
        /// <remarks>В реализации по умолчанию не выполняет никаких действий.</remarks>
        protected virtual void PreparePendingActions(
            List<TAction> pendingActions)
        {
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Логика обработки отложенных действий определяется в <see cref="GoCoreAsync(Action{ValidationResult}, CancellationToken)"/>.
        /// </remarks>
        public virtual async ValueTask<T> GoAsync(
            Action<ValidationResult>? validationFunc = null,
            CancellationToken cancellationToken = default)
        {
            if (!this.HasPendingActions)
            {
                return this.thisObj;
            }

            this.PreparePendingActions(this.PendingActions);
            this.Seal();

            try
            {
                await this.GoCoreAsync(validationFunc: validationFunc, cancellationToken: cancellationToken);
            }
            finally
            {
                this.PendingActions.Clear();
                this.IsSealed = false;
            }

            return this.thisObj;
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Выполняет все запланированные действия.
        /// </summary>
        /// <param name="validationFunc">Метод выполняющий дополнительную валидацию. Если метод не задан, то используется указанный в контексте <see cref="KrTestContext.ValidationFunc"/>, если указанное свойство возвращает значение <see langword="null"/>, то для проверки используется метод <see cref="ValidationAssert.IsSuccessful(ValidationResult)"/>.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Объект типа <typeparamref name="T"/> запланированные действия которого были выполнены.</returns>
        /// <remarks>
        /// Выполнение прерывается при обнаружении ошибки при выполнении запланированных действий.<para/>
        /// Результаты выполнения дополняются информационными сообщениями. Для их исключения, на итоговом результате выполнения, необходимо вызвать метод <see cref="TestValidationKeys.ExceptPendingActionValidationResult(IReadOnlyCollection{IValidationResultItem})"/>.
        /// </remarks>
        protected virtual async ValueTask<T> GoCoreAsync(
            Action<ValidationResult>? validationFunc = null,
            CancellationToken cancellationToken = default)
        {
            validationFunc ??= static (result) => (KrTestContext.CurrentContext.ValidationFunc ?? ValidationAssert.IsSuccessful)(result);

            await this.ExecutePendingActionsAsync(
                validationFunc,
                null,
                cancellationToken);

            return this.thisObj;
        }

        /// <summary>
        /// Выполняет и проверяет результат выполнения отложенных действий.
        /// </summary>
        /// <param name="validationAfterEachActionFunc">Метод, выполняющий валидацию результата выполнения после завершения каждого действия.</param>
        /// <param name="validationAfterAllActionsFunc">Метод, выполняющий валидацию результата выполнения после завершения всех действий.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        /// <exception cref="ArgumentException">Оба параметра <paramref name="validationAfterEachActionFunc"/> и <paramref name="validationAfterAllActionsFunc"/> не заданы.</exception>
        protected virtual async ValueTask ExecutePendingActionsAsync(
            Action<ValidationResult>? validationAfterEachActionFunc = null,
            Action<ValidationResult>? validationAfterAllActionsFunc = null,
            CancellationToken cancellationToken = default)
        {
            var validationAfterAllActions = validationAfterAllActionsFunc is not null;

            if (validationAfterEachActionFunc is null && !validationAfterAllActions)
            {
                throw new ArgumentException($"Both parameters {nameof(validationAfterEachActionFunc)} and {nameof(validationAfterAllActionsFunc)} is null.");
            }

            var pendingActionTrace = new PendingActionTrace(this.Count);

            IValidationResultBuilder? validationResults = null;

            if (validationAfterAllActions)
            {
                validationResults = new ValidationResultBuilder();
            }

            foreach (var pendingAction in this)
            {
                pendingActionTrace.Add(pendingAction);

                var result = await pendingAction
                    .ExecuteAsync(cancellationToken);

                result = this.AddTraceInformation(
                    result,
                    pendingActionTrace);

                validationAfterEachActionFunc?.Invoke(result);

                if (validationAfterAllActions)
                {
                    validationResults!.Add(result);
                }
            }

            if (validationAfterAllActions)
            {
                validationAfterAllActionsFunc!.Invoke(validationResults!.Build());
            }
        }

        /// <summary>
        /// Добавляет трассировку к результату выполнения действия.
        /// </summary>
        /// <param name="result">Результат выполнения действия.</param>
        /// <param name="pendingActionTrace"><inheritdoc cref="PendingActionTrace" path="/summary"/></param>
        /// <returns>Результат выполнения действия с трассировкой.</returns>
        protected virtual ValidationResult AddTraceInformation(
            ValidationResult result,
            PendingActionTrace pendingActionTrace)
        {
            if (result.Items.Count > 0)
            {
                var traceValidationResults = new ValidationResultBuilder(result.Items.Count + 1)
                    .Add(result);

                ValidationSequence
                    .Begin(traceValidationResults)
                    .SetObjectName(this)
                    .InfoText(
                        TestValidationKeys.PendingActionTrace,
                        string.Format(
                            TestValidationKeys.PendingActionTrace.Message!,
                            pendingActionTrace))
                    .End();

                result = traceValidationResults.Build();
            }

            return result;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Возвращает строковое представление объекта отображаемое в окне отладчика.
        /// </summary>
        /// <returns>Строковое представление объекта отображаемое в окне отладчика.</returns>
        private string GetDebuggerDisplay() =>
            $"{nameof(this.Count)} = {this.Count}";

        #endregion
    }
}
