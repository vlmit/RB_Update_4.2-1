#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow
{
    /// <summary>
    /// Объект, обеспечивающий работу с историей заданий.
    /// </summary>
    public interface IKrHistoryStrategy
    {
        /// <summary>
        /// Создаёт новую запись истории заданий и заполняет её информацией о типе задания по <paramref name="taskTypeID"/>.
        /// </summary>
        /// <param name="taskTypeID">Идентификатор типа задания.</param>
        /// <param name="optionID">Идентификатор варианта завершения.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="result">Результат выполнения задания.</param>
        /// <param name="groupRowID">Идентификатор группы истории заданий.</param>
        /// <param name="storeDateTime">Дата и время сохранения карточки или значение <see langword="null"/>, если должно использоваться значение <see cref="DateTime.UtcNow"/>.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Созданная запись или значение <see langword="null"/>, если произошла ошибка.</returns>
        ValueTask<CardTaskHistoryItem?> CreateTaskHistoryAsync(
            Guid taskTypeID,
            Guid optionID,
            string? result,
            IValidationResultBuilder validationResult,
            Guid? groupRowID = null,
            DateTime? storeDateTime = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Создаёт новую запись истории заданий.
        /// </summary>
        /// <param name="taskTypeID">Идентификатор типа задания.</param>
        /// <param name="taskTypeName">Название типа задания.</param>
        /// <param name="taskTypeCaption">Отображаемое имя типа задания.</param>
        /// <param name="optionID">Идентификатор варианта завершения.</param>
        /// <param name="result">Результат выполнения задания.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="groupRowID">Идентификатор группы истории заданий.</param>
        /// <param name="storeDateTime">Дата и время сохранения карточки или значение <see langword="null"/>, если должно использоваться значение <see cref="DateTime.UtcNow"/>.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Созданная запись или значение <see langword="null"/>, если произошла ошибка.</returns>
        ValueTask<CardTaskHistoryItem?> CreateTaskHistoryAsync(
            Guid taskTypeID,
            string? taskTypeName,
            string? taskTypeCaption,
            Guid optionID,
            string? result,
            IValidationResultBuilder validationResult,
            Guid? groupRowID = null,
            DateTime? storeDateTime = null,
            CancellationToken cancellationToken = default);
    }
}
