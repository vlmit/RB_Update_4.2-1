#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Базовая реализация <see cref="IKrTaskWithActionsManagerDataProvider{T}"/>.
    /// </summary>
    /// <typeparam name="T"><inheritdoc cref="IKrTaskWithActionsManagerDataProvider{T}" path="/typeparam[@name='T']"/></typeparam>
    public abstract class KrTaskWithActionsManagerDataProviderBase<T> :
        KrTaskManagerDataProviderBase,
        IKrTaskWithActionsManagerDataProvider<T>
    {
        #region Fields

        private CreateTaskActionAsync<T>? createTaskActionAsync;

        private DelegateTaskActionAsync<T>? delegateTaskActionAsync;

        #endregion

        #region IKrTaskWithActionsManagerDataProvider<T> Members

        /// <inheritdoc/>
        public CreateTaskActionAsync<T>? CreateTaskActionAsync
        {
            get => this.createTaskActionAsync;
            set
            {
                ThrowIfSealed(this);
                this.createTaskActionAsync = value;
            }
        }

        /// <inheritdoc/>
        public DelegateTaskActionAsync<T>? DelegateTaskActionAsync
        {
            get => this.delegateTaskActionAsync;
            set
            {
                ThrowIfSealed(this);
                this.delegateTaskActionAsync = value;
            }
        }

        #endregion
    }
}
