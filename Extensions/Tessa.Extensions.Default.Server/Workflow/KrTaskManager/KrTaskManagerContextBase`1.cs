#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Базовая абстрактная реализация <see cref="IKrTaskManagerContext"/>, инициализируемая внешним контекстом.
    /// </summary>
    /// <typeparam name="T"><inheritdoc cref="IKrTaskManagerContext{T}" path="/typeparam[@name='T']"/></typeparam>
    public abstract class KrTaskManagerContextBase<T> :
        KrTaskManagerContextBase,
        IKrTaskManagerContext<T>
    {
        #region Fields

        private T? externalContext;

        #endregion

        #region IKrTaskManagerContext<T> Members

        /// <inheritdoc/>
        public T ExternalContext
        {
            get
            {
                ThrowIfNull(this.externalContext);
                return this.externalContext;
            }
            set
            {
                ThrowIfSealed(this);
                ThrowIfNull(value);
                this.externalContext = value;
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
