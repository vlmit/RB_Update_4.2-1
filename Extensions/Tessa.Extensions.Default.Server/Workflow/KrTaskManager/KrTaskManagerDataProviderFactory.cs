#nullable enable

using System;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <inheritdoc cref="IKrTaskManagerDataProviderFactory"/>
    /// <param name="unityContainer">Unity-контейнер.</param>
    public sealed class KrTaskManagerDataProviderFactory(
        IUnityContainer unityContainer)
        : IKrTaskManagerDataProviderFactory
    {
        #region Fields

        private readonly IUnityContainer unityContainer = NotNullOrThrow(unityContainer);

        #endregion

        #region IKrTaskManagerDataProviderFactory Members

        /// <inheritdoc/>
        public TDataProvider Create<TDataProvider>(
            string? name = null,
            Action<TDataProvider>? configureAction = null)
            where TDataProvider : IKrTaskManagerDataProvider
        {
            var dataProvider = this.unityContainer.Resolve<TDataProvider>(name);

            configureAction?.Invoke(dataProvider);
            dataProvider.Seal();

            return dataProvider;
        }

        /// <inheritdoc/>
        public TDataProvider Create<TDataProvider, TExternalContext>(
            TExternalContext externalContext,
            string? name = null,
            Action<TDataProvider>? configureAction = null)
            where TDataProvider : IKrTaskManagerDataProvider, IExternalContextProvider<TExternalContext>
        {
            ThrowIfNull(externalContext);

            var dataProvider = this.unityContainer.Resolve<TDataProvider>(name);

            dataProvider.ExternalContext = externalContext;
            configureAction?.Invoke(dataProvider);
            dataProvider.Seal();

            return dataProvider;
        }

        #endregion
    }
}
