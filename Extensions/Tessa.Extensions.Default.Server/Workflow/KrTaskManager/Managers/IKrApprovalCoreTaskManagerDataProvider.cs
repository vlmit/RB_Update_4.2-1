#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Объект, обеспечивающий передачу данных между внешней подсистемой и обработчиком <see cref="IKrApprovalCoreTaskManager"/>.
    /// </summary>
    public interface IKrApprovalCoreTaskManagerDataProvider :
        IKrApprovalAndSigningCoreTaskManagerDataProviderBase
    {
        #region Methods

        /// <summary>
        /// Возвращает значение параметра "Рекомендательное согласование".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если согласование рекомендательное, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetIsAdvisoryAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает значение параметра "Отключить автоматическое согласование".
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><see langword="true"/>, если автоматическое согласование отключено для отправляемых заданий согласования, иначе - <see langword="false"/>.</returns>
        ValueTask<bool> GetIsDisableAutoApprovalAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        #endregion
    }
}
