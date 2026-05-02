#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Реализация <see cref="IKrTaskWithActionsManagerDataProvider{T}"/>, предоставляющая связь с внешней подсистемой через <see cref="IExternalContextProvider{T}.ExternalContext"/>.
    /// </summary>
    /// <typeparam name="TExternalContext"><inheritdoc cref="IExternalContextProvider{T}" path="/typeparam[@name='T']"/></typeparam>
    /// <typeparam name="TPerformer"><inheritdoc cref="KrTaskWithActionsManagerDataProviderBase{T}" path="/typeparam[@name='T']"/></typeparam>
    public abstract class KrTaskWithActionsManagerDataProviderBase<TExternalContext, TPerformer> :
        KrTaskWithActionsManagerDataProviderBase<TPerformer>,
        IExternalContextProvider<TExternalContext>
    {
        #region Fields

        private TExternalContext? externalContext;

        #endregion

        #region IExternalContextProvider<T> Members

        /// <inheritdoc/>
        public TExternalContext ExternalContext
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
    }
}
