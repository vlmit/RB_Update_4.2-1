#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Базовая абстрактная реализация <see cref="IKrTaskManagerDataProvider"/>.
    /// </summary>
    public abstract class KrTaskManagerDataProviderBase :
        IKrTaskManagerDataProvider
    {
        #region Fields

        private TaskActionAsync? completeTaskActionAsync;

        private TaskActionAsync? deleteTaskActionAsync;

        #endregion

        #region IKrTaskManagerDataProvider Members

        /// <inheritdoc/>
        public TaskActionAsync? CompleteTaskActionAsync
        {
            get => this.completeTaskActionAsync;
            set
            {
                ThrowIfSealed(this);
                this.completeTaskActionAsync = value;
            }
        }

        /// <inheritdoc/>
        public TaskActionAsync? DeleteTaskActionAsync
        {
            get => this.deleteTaskActionAsync;
            set
            {
                ThrowIfSealed(this);
                this.deleteTaskActionAsync = value;
            }
        }

        #endregion

        #region ISealable Members

        /// <inheritdoc/>
        public bool IsSealed { get; private set; }

        /// <inheritdoc/>
        public void Seal() => this.IsSealed = true;

        #endregion
    }
}
