#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow
{
    /// <summary>
    /// Стратегия получения SQL исполнителей.
    /// </summary>
    public interface IKrGetSqlPerformersStrategy
    {
        /// <summary>
        /// Возвращает коллекцию SQL исполнителей.
        /// </summary>
        /// <param name="sqlPerformerScript">SQL скрипт, возвращающий список SQL исполнителей, представленный в виде двух столбцов: идентификатор роли, имя роли.</param>
        /// <param name="mainCardID">Идентификатор карточки документа.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Коллекция SQL исполнителей.</returns>
        Task<IReadOnlyList<RoleEntryStorage>> GetAsync(
            string? sqlPerformerScript,
            Guid mainCardID,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);
    }
}
