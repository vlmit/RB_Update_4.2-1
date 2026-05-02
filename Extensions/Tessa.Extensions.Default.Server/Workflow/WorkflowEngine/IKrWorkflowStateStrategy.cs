#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Validation;
using Tessa.Workflow;

namespace Tessa.Extensions.Default.Server.Workflow.WorkflowEngine
{
    /// <summary>
    /// Объект обеспечивающий работу с состояниями в наследниках Workflow действий для kr.
    /// Необходим, чтобы избежать дублирования кода и решить проблему "множественного наследования".
    /// </summary>
    public interface IKrWorkflowStateStrategy
    {
        /// <summary>
        /// Устанавливает состояние карточки.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="state">Устанавливаемое состояние.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        /// <remarks>Предыдущее состояние сохраняется в параметрах процесса (см. <see cref="StorePreviousState(IWorkflowEngineContext, int)"/>).</remarks>
        ValueTask SetStateIDAsync(
            IWorkflowEngineContext context,
            KrState state,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Сохраняет идентификатор предыдущего состояния карточки в параметрах процесса.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <param name="previousState">Идентификатор сохраняемого состояния.</param>
        void StorePreviousState(
            IWorkflowEngineContext context,
            int previousState);

        /// <summary>
        /// Возвращает идентификатор предыдущего состояния карточки из параметров процесса.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IWorkflowEngineContext" path="/summary"/></param>
        /// <returns>Идентификатор предыдущего состояния карточки из параметров процесса. Если не найден в параметрах действия, то считается равным идентификатору состояния <see cref="KrState.Draft"/>.</returns>
        int TryGetPreviousState(IWorkflowEngineContext context);
    }
}
