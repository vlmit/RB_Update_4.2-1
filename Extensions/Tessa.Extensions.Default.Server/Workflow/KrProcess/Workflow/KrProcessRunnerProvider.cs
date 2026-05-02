#nullable enable

using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess.Workflow
{
    /// <inheritdoc cref="IKrProcessRunnerProvider"/>
    public sealed class KrProcessRunnerProvider :
        IKrProcessRunnerProvider
    {
        #region Fields

        private readonly IUnityContainer unityContainer;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="unityContainer">Unity-контейнер.</param>
        public KrProcessRunnerProvider(
            IUnityContainer unityContainer)
        {
            this.unityContainer = NotNullOrThrow(unityContainer);
        }

        #endregion

        #region IKrProcessRunnerProvider Members

        /// <inheritdoc />
        public IKrProcessRunner GetRunner(
            string runnerName) =>
            this.unityContainer.Resolve<IKrProcessRunner>(runnerName);

        #endregion
    }
}