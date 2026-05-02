#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards.Extensions;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess
{
    /// <summary>
    /// Объект, выполняющий запуск процессов.
    /// </summary>
    public interface IKrProcessLauncher
    {
        /// <summary>
        /// Запускает указанный процесс.
        /// </summary>
        /// <param name="krProcess">Запускаемый процесс.</param>
        /// <param name="cardContext">Контекст процесса взаимодействия с карточкой в рамках которого запускается процесс.</param>
        /// <param name="specificParameters"><inheritdoc cref="IKrProcessLauncherSpecificParameters" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="IKrProcessLaunchResult" path="/summary"/></returns>
        Task<IKrProcessLaunchResult> LaunchAsync(
            KrProcessInstance krProcess,
            ICardExtensionContext? cardContext = null,
            IKrProcessLauncherSpecificParameters? specificParameters = null,
            CancellationToken cancellationToken = default);
    }
}
