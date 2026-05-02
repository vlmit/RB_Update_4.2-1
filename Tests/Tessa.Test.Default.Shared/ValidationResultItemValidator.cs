#nullable enable

using System;
using System.Linq.Expressions;
using NUnit.Framework;
using Tessa.Platform.Validation;
using Tessa.Test.Default.Shared.Validation;

namespace Tessa.Test.Default.Shared
{
    /// <summary>
    /// Объект выполняющий валидацию результатов валидации.
    /// </summary>
    public sealed class ValidationResultItemValidator :
        ValidatorBase<ValidationResultItemValidator, IValidationResultItem>
    {
        #region Constants

        private const int DefaultExpectedCount = 1;

        #endregion

        #region Properties

        /// <summary>
        /// Возвращает ожидаемое число срабатываний объекта валидации.
        /// </summary>
        public int ExpectedCount { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ValidationResultItemValidator"/>.
        /// </summary>
        /// <param name="expectedCount">Ожидаемое число срабатываний объекта валидации.</param>
        /// <param name="name">Имя объекта, выполняющего валидацию.</param>
        /// <exception cref="ArgumentOutOfRangeException">Expected number of operations with validation object is negative.</exception>
        private ValidationResultItemValidator(
            int expectedCount,
            string? name)
            : base(name) =>
            this.ExpectedCount = AssertOrThrow(expectedCount, expectedCount >= 0);

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ValidationResultItemValidator"/>.
        /// </summary>
        /// <param name="validationExpression">Выражение, проверяющее результат валидации.</param>
        /// <param name="expectedCount">Ожидаемое число срабатываний объекта валидации.</param>
        /// <param name="name">Имя объекта, выполняющего валидацию.</param>
        /// <exception cref="ArgumentOutOfRangeException">Expected number of operations with validation object is negative.</exception>
        public ValidationResultItemValidator(
            Expression<Func<IValidationResultItem, bool>> validationExpression,
            int expectedCount = DefaultExpectedCount,
            string? name = null)
            : this(expectedCount, name) =>
            this.CheckWithValidationFunc(validationExpression);

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ValidationResultItemValidator"/>.
        /// </summary>
        /// <param name="item">Сообщение о валидации на равенство которому выполняется проверка.</param>
        /// <param name="expectedCount">Ожидаемое число срабатываний объекта валидации.</param>
        /// <param name="name">Имя объекта, выполняющего валидацию.</param>
        /// <exception cref="ArgumentOutOfRangeException">Ожидаемое число срабатываний объекта валидации меньше нуля.</exception>
        public ValidationResultItemValidator(
            IValidationResultItem item,
            int expectedCount = DefaultExpectedCount,
            string? name = null)
            : this(expectedCount, name) =>
            this.CheckItem(item);

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="ValidationResultItemValidator"/>.
        /// </summary>
        /// <param name="type">Тип сообщения о валидации которому должно соответствовать проверяемое сообщение.</param>
        /// <param name="key">Ключ сообщения о результате валидации, которое должно иметь проверяемое сообщение.</param>
        /// <param name="expectedCount">Ожидаемое число срабатываний объекта валидации.</param>
        /// <param name="name">Имя объекта, выполняющего валидацию.</param>
        /// <exception cref="ArgumentOutOfRangeException">Ожидаемое число срабатываний объекта валидации меньше нуля.</exception>
        /// <remarks>
        /// Не рекомендуется использовать данный конструктор при задании значения <see cref="ValidationKey.Unknown"/> параметру <paramref name="key"/> без указания проверяемых свойств объекта <see cref="IValidationResultItem"/> из-за невозможности гарантирования правильности выполнения проверки.<para/>
        /// Данный конструктор имеет смысл использовать при задании значения параметра <paramref name="key"/> равным <see cref="ValidationKey.Unknown"/> только при проверке на отсутствие сообщений валидации с таким ключом. Для этого необходимо задать параметр <paramref name="expectedCount"/> равным 0.</remarks>
        public ValidationResultItemValidator(
            ValidationResultType type,
            ValidationKey key,
            int expectedCount = DefaultExpectedCount,
            string? name = null)
            : this(expectedCount, name)
        {
            this.CheckType(type)
                .CheckKey(key);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Проверяет тип сообщения валидации (<see cref="IValidationResultItem.Type"/>).
        /// </summary>
        /// <param name="expectedType">Ожидаемый тип сообщения о результате валидации.</param>
        /// <returns>Объект <see cref="ValidationResultItemValidator"/> для создания цепочки.</returns>
        public ValidationResultItemValidator CheckType(
            ValidationResultType expectedType)
        {
            this.Check(
                "Type",
                static i => i.Type,
                Is.EqualTo(expectedType));

            return this;
        }

        /// <summary>
        /// Проверяет ключ сообщения валидации (<see cref="IValidationResultItem.Key"/>).
        /// </summary>
        /// <param name="expectedKey">Ожидаемый ключ сообщения о результате валидации.</param>
        /// <returns>Объект <see cref="ValidationResultItemValidator"/> для создания цепочки.</returns>
        public ValidationResultItemValidator CheckKey(
            ValidationKey expectedKey)
        {
            ThrowIfNull(expectedKey);

            this.Check(
                "Key",
                static i => i.Key,
                Is.EqualTo(expectedKey));

            return this;
        }

        /// <summary>
        /// Проверяет сообщение валидации (<see cref="IValidationResultItem.Message"/>).
        /// </summary>
        /// <param name="expectedMessage">Ожидаемое сообщение валидации.</param>
        /// <param name="comparisonType">Способ сравнения строк.</param>
        /// <returns>Объект <see cref="ValidationResultItemValidator"/> для создания цепочки.</returns>
        public ValidationResultItemValidator CheckMessage(
            string expectedMessage,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            this.Check(
                "Message",
                static i => i.Message,
                Is.EqualTo(expectedMessage).Using(StringComparer.FromComparison(comparisonType)));

            return this;
        }

        /// <summary>
        /// Проверяет имя объекта, к которому относится сообщение валидации (<see cref="IValidationResultItem.ObjectName"/>).
        /// </summary>
        /// <param name="expectedObjectName">Ожидаемое имя объекта, к которому относится сообщение валидации</param>
        /// <param name="comparisonType">Способ сравнения строк.</param>
        /// <returns>Объект <see cref="ValidationResultItemValidator"/> для создания цепочки.</returns>
        public ValidationResultItemValidator CheckObjectName(
            string? expectedObjectName,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            this.Check(
                "ObjectName",
                static i => i.ObjectName,
                Is.EqualTo(expectedObjectName).Using(StringComparer.FromComparison(comparisonType)));

            return this;
        }

        /// <summary>
        /// Проверяет тип объекта, к которому относится сообщение валидации (<see cref="IValidationResultItem.ObjectType"/>).
        /// </summary>
        /// <param name="expectedObjectType">Ожидаемый тип объекта, к которому относится сообщение валидации.</param>
        /// <param name="comparisonType">Способ сравнения строк.</param>
        /// <returns>Объект <see cref="ValidationResultItemValidator"/> для создания цепочки.</returns>
        public ValidationResultItemValidator CheckObjectType(
            string? expectedObjectType,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            this.Check(
                "ObjectType",
                static i => i.ObjectType,
                Is.EqualTo(expectedObjectType).Using(StringComparer.FromComparison(comparisonType)));

            return this;
        }

        /// <summary>
        /// Проверяет имя поля объекта, к которому относится сообщение валидации (<see cref="IValidationResultItem.FieldName"/>).
        /// </summary>
        /// <param name="expectedFieldName">Ожидаемое имя поля объекта, к которому относится сообщение валидации.</param>
        /// <param name="comparisonType">Способ сравнения строк.</param>
        /// <returns>Объект <see cref="ValidationResultItemValidator"/> для создания цепочки.</returns>
        public ValidationResultItemValidator CheckFieldName(
            string? expectedFieldName,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            this.Check(
                "FieldName",
                static i => i.FieldName,
                Is.EqualTo(expectedFieldName).Using(StringComparer.FromComparison(comparisonType)));

            return this;
        }

        /// <summary>
        /// Проверяет дополнительную информацию (<see cref="IValidationResultItem.Details"/>).
        /// </summary>
        /// <param name="expectedDetails">Ожидаемая дополнительная информация.</param>
        /// <param name="comparisonType">Способ сравнения строк.</param>
        /// <returns>Объект <see cref="ValidationResultItemValidator"/> для создания цепочки.</returns>
        public ValidationResultItemValidator CheckDetails(
            string? expectedDetails,
            StringComparison comparisonType = StringComparison.Ordinal)
        {
            this.Check(
                "Details",
                static i => i.Details,
                Is.EqualTo(expectedDetails).Using(StringComparer.FromComparison(comparisonType)));

            return this;
        }

        /// <summary>
        /// Проверяет сообщение валидации с помощью указанного метода.
        /// </summary>
        /// <param name="validationExpression">Выражение, проверяющее результат валидации.</param>
        /// <returns>Объект <see cref="ValidationResultItemValidator"/> для создания цепочки.</returns>
        public ValidationResultItemValidator CheckWithValidationFunc(
            Expression<Func<IValidationResultItem, bool>> validationExpression)
        {
            ThrowIfNull(validationExpression);
            var func = validationExpression.Compile();

            this.Check(
                "ValidationFunc",
                value =>
                {
                    if (func(value))
                    {
                        return ValidationResult.Empty;
                    }

                    // Два пробела в начале соответствуют двум пробелам добавляемым при выводе строк "Expected:" и "But was:" NUnit.
                    return ValidationResult.FromText($"  ValidationFunc: {validationExpression}", ValidationResultType.Error);
                });

            return this;
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override string? GetValueDescription(IValidationResultItem value) => null; // Валидатор применяется совместно с методами из ValidationAssert. В них выводится оригинальный объект, а не проверяемый в данном валидаторе.

        #endregion

        #region Private Methods

        /// <summary>
        /// Проверяет сообщение валидации на равенство указанному сообщению валидации.
        /// </summary>
        /// <param name="expectedItem">Ожидаемое сообщение валидации.</param>
        private void CheckItem(
            IValidationResultItem expectedItem)
        {
            ThrowIfNull(expectedItem);

            this.Check(
                "Item",
                static i => i,
                Is.EqualTo(expectedItem));
        }

        #endregion
    }
}
