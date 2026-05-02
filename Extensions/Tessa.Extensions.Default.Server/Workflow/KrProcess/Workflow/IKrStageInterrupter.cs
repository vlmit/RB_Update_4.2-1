#nullable enable

using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <summary>
    /// Объект, прерывающий выполнение этапа.
    /// </summary>
    public interface IKrStageInterrupter
    {
        /// <summary>
        /// Прервать выполнение этапа.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IKrStageInterrupterContext" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если выполнение этапа завершено и выполнение процесса может быть продолжено, иначе <see langword="false"/>.</returns>
        Task<bool> InterruptStageAsync(IKrStageInterrupterContext context);
    }
}
