#nullable enable

using System;
using Unity;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager
{
    /// <inheritdoc cref="IKrTaskManagerContextFactory"/>
    /// <param name="unityContainer">Unity-контейнер.</param>
    public sealed class KrTaskManagerContextFactory(
        IUnityContainer unityContainer)
        : IKrTaskManagerContextFactory
    {
        #region Fields

        private readonly IUnityContainer unityContainer = NotNullOrThrow(unityContainer);

        #endregion

        #region IKrTaskManagerContextFactory Members

        /// <inheritdoc/>
        public TContext Create<TContext, TExternalContext>(
            TExternalContext externalContext,
            string? name = null,
            Action<TContext>? configureAction = null)
            where TContext : IKrTaskManagerContext, IExternalContextProvider<TExternalContext>
        {
            ThrowIfNull(externalContext);

            var context = this.unityContainer.Resolve<TContext>(name);

            context.ExternalContext = externalContext;
            configureAction?.Invoke(context);
            context.Seal();

            return context;
        }

        #endregion
    }
}
