#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <summary>
    /// Объект, предоставляющий <see cref="IKrProcessRunner"/>.
    /// </summary>
    public interface IKrProcessRunnerProvider
    {
        /// <summary>
        /// Возвращает <see cref="IKrProcessRunner"/>.
        /// </summary>
        /// <param name="runnerName">Название <see cref="IKrProcessRunner"/>.</param>
        /// <returns><inheritdoc cref="IKrProcessRunner" path="/summary"/></returns>
        IKrProcessRunner GetRunner(
            string runnerName);
    }
}