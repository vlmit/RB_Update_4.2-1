#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Cards;

namespace Tessa.Extensions.Default.Shared.Workflow
{
    /// <summary>
    /// Объект, выполняющий упорядочивание карточек в соответствии с их типом.
    /// </summary>
    public interface ICardTypePriorityComparer :
        IComparer<Card>
    {
        /// <summary>
        /// Словарь, содержащий порядок сортировки типов карточек. Ключ - тип карточки. Значение - порядок сортировки.
        /// </summary>
        IReadOnlyDictionary<Guid, int> Order { get; }
    }
}
