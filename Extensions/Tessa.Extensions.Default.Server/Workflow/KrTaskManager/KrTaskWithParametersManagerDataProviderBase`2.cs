#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <summary>
    /// Реализация <see cref="KrTaskWithActionsManagerDataProviderBase{T}"/>, предоставляющая связь с внешней подсистемой через <see cref="IExternalContextProvider{T}.ExternalContext"/>.
    /// </summary>
    /// <typeparam name="TExternalContext"><inheritdoc cref="IExternalContextProvider{T}" path="/typeparam[@name='T']"/></typeparam>
    /// <typeparam name="TPerformer"><inheritdoc cref="KrTaskWithParametersManagerDataProviderBase{T}" path="/typeparam[@name='T']"/></typeparam>
    public abstract class KrTaskWithParametersManagerDataProviderBase<TExternalContext, TPerformer> :
        KrTaskWithParametersManagerDataProviderBase<TPerformer>,
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
