#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <summary>
    /// Объект, определяющий возможность отображения вторичных процессов, работающих в режиме "Кнопка".
    /// </summary>
    public interface IKrProcessButtonVisibilityEvaluator
    {
        /// <summary>
        /// Возвращает список отображаемых глобальных вторичных процессов, работающих в режиме "Кнопка".
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrProcessButtonVisibilityEvaluatorContext" path="/summary"/></param>
        /// <returns>Список отображаемых глобальных вторичных процессов, работающих в режиме "Кнопка".</returns>
        Task<IList<IKrProcessButton>> EvaluateGlobalButtonsAsync(
            IKrProcessButtonVisibilityEvaluatorContext context);

        /// <summary>
        /// Возвращает список отображаемых локальных вторичных процессов, работающих в режиме "Кнопка".
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrProcessButtonVisibilityEvaluatorContext" path="/summary"/></param>
        /// <returns>Список отображаемых локальных вторичных процессов, работающих в режиме "Кнопка".</returns>
        Task<IList<IKrProcessButton>> EvaluateLocalButtonsAsync(
            IKrProcessButtonVisibilityEvaluatorContext context);
    }
}
