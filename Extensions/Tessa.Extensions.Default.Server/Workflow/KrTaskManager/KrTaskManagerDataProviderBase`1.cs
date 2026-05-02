#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Базовая абстрактная реализация <see cref="IKrTaskManagerDataProvider"/>, инициализируемая внешним контекстом <see cref="IExternalContextProvider{T}.ExternalContext"/>.
    /// </summary>
    /// <typeparam name="T"><inheritdoc cref="IExternalContextProvider{T}" path="/typeparam[@name='T']"/></typeparam>
    public abstract class KrTaskManagerDataProviderBase<T> :
        KrTaskManagerDataProviderBase,
        IExternalContextProvider<T>
        where T : class
    {
        #region Fields

        private T? externalContext;

        #endregion

        #region IExternalContextProvider<T> Members

        /// <inheritdoc/>
        public T ExternalContext
        {
            get => NotNullOrThrow(this.externalContext);
            set
            {
                ThrowIfSealed(this);
                this.externalContext = NotNullOrThrow(value);
            }
        }

        #endregion
    }
}
