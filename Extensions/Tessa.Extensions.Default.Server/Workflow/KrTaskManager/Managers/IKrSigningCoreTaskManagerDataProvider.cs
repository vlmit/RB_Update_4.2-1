#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Files;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Объект, обеспечивающий передачу данных между внешней подсистемой и обработчиком <see cref="IKrSigningCoreTaskManager"/>.
    /// </summary>
    public interface IKrSigningCoreTaskManagerDataProvider :
        IKrApprovalAndSigningCoreTaskManagerDataProviderBase
    {
        /// <summary>
        /// Возвращает значение параметра "Разрешено дополнительное согласование".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если разрешена отправка заданий дополнительного согласования, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetAllowAdditionalApprovalAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Подписание ЭП".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если требуется подписание файла ЭП, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetSignFilesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Без диалога выбора файлов".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если подписание происходит без диалога выбора файлов для подписания ЭП, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetNoSignFileDialogAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Не подписывать копии".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если копии файлов не должны быть подписаны, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetDoNotSignFileCopiesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Без диалога добавления комментария ЭП".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, диалог добавления комментария не будет показан, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetNoCommentDialogAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Категории файлов для подписания ЭП".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Список категорий файлов, которые будут подписаны или выделены в диалоге выбора файлов для подписания ЭП.</returns>
        ValueTask<IReadOnlyList<IFileCategory>> GetFileCategoriesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Скрыть категории".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Список категорий файлов, которые не будут подписаны и будут скрыты в диалоге выбора файлов для подписания ЭП.</returns>
        ValueTask<IReadOnlyList<IFileCategory>> GetHiddenFileCategoriesAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает список файлов, предварительно выбранных для подписания.
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Список файлов, которые будут подписаны или выделены в диалоге выбора файлов для подписания ЭП.</returns>
        ValueTask<IReadOnlyList<Guid>> GetFilesToSelectAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает список файлов, которые не будут подписаны и будут скрыты в диалоге выбора файлов для подписания ЭП.
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Список файлов, которые не будут подписаны и будут скрыты в диалоге выбора файлов для подписания ЭП.</returns>
        ValueTask<IReadOnlyList<Guid>> GetFilesToHideAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);
    }
}
