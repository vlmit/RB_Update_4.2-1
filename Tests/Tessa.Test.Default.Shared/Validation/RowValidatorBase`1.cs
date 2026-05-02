#nullable enable

using System;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Tessa.Cards;
using Tessa.Platform.Storage;

namespace Tessa.Test.Default.Shared.Validation
{
    /// <summary>
    /// Объект, предоставляющий методы для проверки значений полей <see cref="CardRow"/>.
    /// </summary>
    /// <typeparam name="T">Тип объекта, валидатора выполняющего проверку значений.</typeparam>
    public abstract class RowValidatorBase<T> :
        ValidatorBase<RowValidatorBase<T>, CardRow>
        where T : RowValidatorBase<T>
    {
        #region Public Methods

        /// <summary>
        /// Проверяет значение поля <see cref="CardRow.RowIDKey"/>.
        /// </summary>
        /// <param name="expectedValue">Ожидаемое значение.</param>
        /// <returns>Объект для создания цепочки вызовов.</returns>
        public T CheckRowID(Guid expectedValue)
        {
            return this.Check(
                CardRow.RowIDKey,
                Is.EqualTo(expectedValue));
        }

        /// <summary>
        /// Проверяет значение поля <paramref name="fieldName"/> с помощью выражения <paramref name="expression"/>.
        /// </summary>
        /// <param name="fieldName">Название проверяемого поля.</param>
        /// <param name="expression">Выражение, с помощью которого выполняется проверка.</param>
        /// <returns>Объект для создания цепочки вызовов.</returns><remarks>
        /// Этот метод реализуется с помощью отложенного выполнения. Для выполнения запрошенного действия необходимо вызвать метод
        /// <see cref="ValidatorBase{TObject,TValue}.ValidateAsync(TValue,System.Threading.CancellationToken)"/>.
        /// </remarks>
        public T Check(
            string fieldName,
            IResolveConstraint expression)
        {
            ThrowIfNull(fieldName);
            ThrowIfNull(expression);

            this.Check(
                fieldName,
                value => value.TryGet<object>(fieldName),
                expression);

            return (T) this;
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override string GetValueDescription(CardRow value) =>
            StorageHelper.Print(value);

        #endregion
    }
}
