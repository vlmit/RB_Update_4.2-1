#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Cards;

namespace Tessa.Extensions.Default.Shared.Workflow
{
    /// <inheritdoc cref="ICardTypePriorityComparer"/>
    /// <remarks>Реализует сортировку по убыванию.</remarks>
    public sealed class CardTypePriorityComparer :
        Comparer<Card>,
        ICardTypePriorityComparer
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CardTypePriorityComparer"/>.
        /// </summary>
        /// <param name="cardTypes">Перечисление типов карточек в соответствие с которым выполняется сортировка карточек.</param>
        public CardTypePriorityComparer(
            IEnumerable<Guid> cardTypes)
        {
            ThrowIfNull(cardTypes);

            var order = new Dictionary<Guid, int>();
            var index = 0;

            foreach (var cardType in cardTypes)
            {
                if (!order.ContainsKey(cardType))
                {
                    order.Add(cardType, index++);
                }
            }

            this.Order = order;
        }

        #endregion

        #region Properties

        /// <inheritdoc/>
        public IReadOnlyDictionary<Guid, int> Order { get; }

        #endregion

        #region Comparer{T} Implementation

        /// <inheritdoc/>
        public override int Compare(Card? x, Card? y)
        {
            if (x is null)
            {
                return y is null ? 0 : 1;
            }

            if (y is null)
            {
                return -1;
            }

            // Сравнение типов карточек по их порядковому номеру.
            var notContainsXTypeOrder = !this.Order.TryGetValue(x.TypeID, out var xTypeOrder);
            var notContainsYTypeOrder = !this.Order.TryGetValue(y.TypeID, out var yTypeOrder);

            if (notContainsXTypeOrder)
            {
                return notContainsYTypeOrder ? 0 : 1;
            }

            if (notContainsYTypeOrder)
            {
                return -1;
            }

            return xTypeOrder == yTypeOrder
                ? 0
                : xTypeOrder < yTypeOrder
                    ? 1
                    : -1;
        }

        #endregion
    }
}
