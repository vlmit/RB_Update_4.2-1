#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Tessa.Platform;
using Tessa.Platform.Validation;
using Tessa.Test.Default.Shared.Kr;

namespace Tessa.Test.Default.Shared.Validation
{
    /// <summary>
    /// Объект, предоставляющий методы для проверки значений.
    /// </summary>
    /// <typeparam name="TObject">Тип объекта, валидатора выполняющего проверку значений.</typeparam>
    /// <typeparam name="TValue">Тип проверяемого значения.</typeparam>
    public abstract class ValidatorBase<TObject, TValue> :
        ISealable
        where TObject : ValidatorBase<TObject, TValue>
        where TValue : notnull
    {
        #region Fields

        private readonly TObject thisObj;

        private TValue? value;

        private bool isSealed;

        private bool isTemporarySealed;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="name">Название валидатора. Если не указано, то используется имя класса.</param>
        protected ValidatorBase(
            string? name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = this.GetType().Name;
            }

            this.Name = name;
            this.thisObj = (TObject) this;
            this.PendingActions = new(
                this,
                this.GetValueDescription);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Название валидатора.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Отложенные действия.
        /// </summary>
        protected ValidatorPendingActionsProvider PendingActions { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Проверяет значение, получаемое посредством <paramref name="getCheckValueFunc"/>, с помощью выражения <paramref name="expression"/>.
        /// </summary>
        /// <param name="checkName">Название проверки.</param>
        /// <param name="getCheckValueFunc">Функция, возвращающая проверяемое значение.</param>
        /// <param name="expression">Выражение с помощью которого выполняется проверка.</param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="ValidateAsync(TValue,CancellationToken)"/>.
        /// </remarks>
        public TObject Check(
            string checkName,
            Func<TValue, object?> getCheckValueFunc,
            IResolveConstraint expression)
        {
            ThrowIfNullOrWhiteSpace(checkName);
            ThrowIfNull(getCheckValueFunc);
            ThrowIfNull(expression);
            ThrowIfSealed(this);

            return this.Check(
                checkName,
                value => TestHelper.That(getCheckValueFunc(value), expression));
        }

        /// <summary>
        /// Проверяет значение с помощью выражения <paramref name="checkValueFunc"/>.
        /// </summary>
        /// <param name="checkName">Название проверки.</param>
        /// <param name="checkValueFunc">Выражение с помощью которого выполняется проверка.</param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        /// <remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод <see cref="ValidateAsync(TValue,CancellationToken)"/>.
        /// </remarks>
        public TObject Check(
            string checkName,
            Func<TValue, ValidationResult> checkValueFunc)
        {
            ThrowIfNullOrWhiteSpace(checkName);
            ThrowIfNull(checkValueFunc);
            ThrowIfSealed(this);

            var name = $"{this.Name}: {checkName}";

            this.PendingActions.RemovePendingAction(i =>
                i.Name == name);

            this.PendingActions.AddPendingAction(
                new PendingAction(
                    name,
                    (_, _) =>
                    {
                        ThrowIfNull(this.value);

                        return ValueTask.FromResult(checkValueFunc(this.value));
                    }));

            return this.thisObj;
        }

        /// <summary>
        /// Проверяет значение <paramref name="value"/> и возвращает её результат.
        /// </summary>
        /// <param name="value">Проверяемое значение.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns><inheritdoc cref="ValidationResult" path="/summary"/></returns>
        public async ValueTask<ValidationResult> GetResultAsync(
            TValue value,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(value);

            if (!this.PendingActions.HasPendingActions)
            {
                return ValidationResult.Empty;
            }

            this.isTemporarySealed = true;
            this.value = value;
            var validationResult = new ValidationResultBuilder();

            try
            {
                await this.PendingActions.GoAsync(
                    result => validationResult.Add(result),
                    cancellationToken: cancellationToken);
            }
            finally
            {
                this.isTemporarySealed = false;
            }

            return validationResult.Build();
        }

        /// <summary>
        /// Проверяет значение <paramref name="value"/> и выбрасывает исключение <see cref="AssertionException"/> при нарушении проверки.
        /// </summary>
        /// <param name="value">Проверяемое значение.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        public async ValueTask ValidateAsync(
            TValue value,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(value);

            if (!this.PendingActions.HasPendingActions)
            {
                return;
            }

            this.isTemporarySealed = true;
            this.value = value;

            try
            {
                await this.PendingActions.GoAsync(
                    cancellationToken: cancellationToken);
            }
            finally
            {
                this.isTemporarySealed = false;
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Возвращает строковое представление проверяемого значения, выводимое при нарушении проверки.
        /// </summary>
        /// <param name="value">Проверяемое значение.</param>
        /// <returns>Строковое представление проверяемого значения или значение <see langword="null"/> или <see cref="string.Empty"/>, если оно не должно включаться в результаты проверки.</returns>
        protected virtual string? GetValueDescription(TValue value) =>
            FormatNullable(value.ToString());

        #endregion

        #region ISealable Members

        /// <doc path='info[@type="ISealable" and @item="IsSealed"]'/>
        public bool IsSealed
        {
            get => this.isTemporarySealed || this.isSealed;
            private set => this.isSealed = value;
        }

        /// <doc path='info[@type="ISealable" and @item="Seal"]'/>
        public void Seal() => this.IsSealed = true;

        #endregion

        #region Private Methods

        private string? GetValueDescription() =>
            this.GetValueDescription(this.value ?? throw new InvalidOperationException("The value to be checked is not set."));

        #endregion
    }
}
