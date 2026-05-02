#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess
{
    /// <summary>
    /// Объект, определяющий наличие изменений в порядке и параметрах этапов.
    /// </summary>
    public interface IKrStageRowChecker
    {
        /// <summary>
        /// Определяет наличие изменений в порядке и параметрах этапов перед изменением основного сателлита.
        /// </summary>
        /// <param name="card">Карточка документа, содержащего маршрут.</param>
        /// <param name="krSatellite">Основной сателлит документа.</param>
        /// <param name="changedOrders">Список, в который записываются идентификаторы строк этапов с изменённым порядком.</param>
        /// <param name="changedSettings">Список, в который записываются идентификаторы строк этапов с изменёнными параметрами.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        ValueTask BeforeModifySatelliteAsync(
            Card card,
            Card krSatellite,
            ISet<Guid> changedOrders,
            ISet<Guid> changedSettings,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Определяет наличие изменений в порядке и параметрах этапов после изменения основного сателлита.
        /// </summary>
        /// <param name="card">Карточка документа, содержащего маршрут.</param>
        /// <param name="krSatellite">Основной сателлит документа.</param>
        /// <param name="changedOrders">Список, в который записываются идентификаторы строк этапов с изменённым порядком.</param>
        /// <param name="changedSettings">Список, в который записываются идентификаторы строк этапов с изменёнными параметрами.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        ValueTask AfterModifySatelliteAsync(
            Card card,
            Card krSatellite,
            ISet<Guid> changedOrders,
            ISet<Guid> changedSettings,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет, есть ли в карточке документа изменённые секции, содержащие параметры этапов.
        /// </summary>
        /// <param name="card">Проверяемая карточка.</param>
        /// <returns>Значение <see langword="true"/>, если карточка содержит изменённые секции, содержащие параметры этапов, иначе - <see langword="false"/>.</returns>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        ValueTask<bool> HasAnySettingsSectionAsync(
            Card card,
            CancellationToken cancellationToken = default);
    }
}
