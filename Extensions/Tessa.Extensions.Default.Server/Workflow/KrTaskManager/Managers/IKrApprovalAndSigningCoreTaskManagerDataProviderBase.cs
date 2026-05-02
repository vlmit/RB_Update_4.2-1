#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Workflow;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Объект, обеспечивающий передачу общих данных между внешней подсистемой и обработчиками <see cref="IKrApprovalCoreTaskManager"/> и <see cref="IKrSigningCoreTaskManager"/>.
    /// </summary>
    public interface IKrApprovalAndSigningCoreTaskManagerDataProviderBase :
        IKrTaskWithParametersManagerDataProvider<RoleEntryStorage>
    {
        #region Properties

        /// <summary>
        /// Значение, показывающее, что действие было завершено с отрицательным вариантом завершения.
        /// </summary>
        bool IsNegativeActionResult { get; set; }

        /// <summary>
        /// Порядковый номер текущего исполнителя.
        /// </summary>
        int CurrentPerformerIndex { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Возвращает значение параметра "Параллельная отправка заданий".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если включена параллельная отправка заданий, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetIsParallelAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Вернуть после завершения с положительным вариантом завершения".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если документ должен быть возвращён на доработку инициатору после положительного решения всех исполнителей, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetReturnWhenPositiveActionResultAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Вернуть после завершения с отрицательным вариантом завершения".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если документ должен быть возвращён на доработку инициатору после отрицательного решения исполнителя, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetReturnWhenNegativeActionResultAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Редактировать карточку".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если исполнителям будут выданы права доступа на редактирование карточки, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetCanEditCardAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Редактировать любые файлы".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если исполнителям будут выданы права доступа на редактирование приложенных файлов, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetCanEditAnyFilesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Изменять состояние при старте".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если при отправке первого задания состояние должно быть изменено, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetChangeStateOnStartAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Изменять состояние при завершении".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если при завершении задания(й) согласования состояние должно быть изменено, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetChangeStateOnEndAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Не возвращать на доработку".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если отключен возврат на доработку, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetNotReturnEditAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Ожидать решения всех исполнителей".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если отрицательное решение одного исполнителя не прерывает отправку заданий оставшимся исполнителям, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetExpectAllPerformersAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Не создавать запись "Возврат на доработку" в истории заданий".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если отключено создание записи "Возврат на доработку" в истории заданий, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetNotCreateReturnEditTaskHistoryRecordAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        #endregion
    }
}
