#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Server.Cards
{
    /// <summary>
    /// Потокобезопасный кэш настроек группировки файлов по циклам согласования.
    /// </summary>
    public interface ICycleGroupingSettings
    {
        /// <summary>
        /// Возвращает набор идентификаторов типов групп истории заданий, используемых для обозначения циклов согласования.
        /// </summary>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Набор идентификаторов типов групп истории заданий, используемых для обозначения циклов согласования.</returns>
        ValueTask<IReadOnlySet<Guid>> GetCycleTaskGroupTypeIDListAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает набор идентификаторов типов заданий, которые не учитываются при расчёте информацию по циклам согласования.
        /// </summary>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Набор идентификаторов типов заданий, которые не учитываются при расчёте информацию по циклам согласования.</returns>
        ValueTask<IReadOnlySet<Guid>> GetIgnoreTaskTypeIDListAsync(
            CancellationToken cancellationToken = default);
    }
}
