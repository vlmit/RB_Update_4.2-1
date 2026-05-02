#nullable enable

using System;
using Tessa.Roles;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Реализация <see cref="IKrSigningTaskManagerDataProvider"/>, предоставляющая связь с внешней подсистемой через <see cref="IExternalContextProvider{T}.ExternalContext"/>.
    /// </summary>
    /// <param name="dataProviderFactory"><inheritdoc cref="IKrTaskManagerDataProviderFactory" path="/summary"/></param>
    public abstract class KrSigningTaskManagerDataProviderBase<TExternalContext>(
        IKrTaskManagerDataProviderFactory dataProviderFactory) :
        KrTaskWithActionsManagerDataProviderBase<TExternalContext, IRoleUser>,
        IKrSigningTaskManagerDataProvider<TExternalContext>
    {
        #region Properties

        protected IKrTaskManagerDataProviderFactory DataProviderFactory { get; } = NotNullOrThrow(dataProviderFactory);

        #endregion

        #region IKrTaskManagerNestedDataProviderProvider Implementation

        /// <inheritdoc/>
        public virtual TDataProvider CreateNested<TDataProvider>(
            string? name = null,
            Action<TDataProvider>? configureAction = null) where TDataProvider : IKrTaskManagerDataProvider
        {
            var providerType = typeof(TDataProvider);
            if (providerType == typeof(IKrSigningCoreTaskManagerDataProvider))
            {
                return (TDataProvider) dataProviderFactory.Create<IKrSigningCoreTaskManagerDataProvider<TExternalContext>, TExternalContext>(
                    this.ExternalContext,
                    name,
                    (dataProvider) =>
                    {
                        this.ConfigureNested(dataProvider);
                        configureAction?.Invoke((TDataProvider) dataProvider);
                    });
            }
            else if (providerType == typeof(IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider))
            {
                return (TDataProvider) dataProviderFactory.Create<IKrAdditionalApprovalTaskManagerWithParentApprovalTaskDataProvider<TExternalContext>, TExternalContext>(
                    this.ExternalContext,
                    name,
                    (dataProvider) =>
                    {
                        this.ConfigureNested(dataProvider);
                        configureAction?.Invoke((TDataProvider) dataProvider);
                    });
            }

            return dataProviderFactory.Create<TDataProvider>(
                    name,
                    (dataProvider) =>
                    {
                        this.ConfigureNested(dataProvider);
                        configureAction?.Invoke(dataProvider);
                    });
        }

        #endregion


        #region Protected Methods

        /// <summary>
        /// Метод для конфигурации провайдера данных, основанного на текущем, при создании его через <see cref="CreateNested{TDataProvider}(string?, Action{TDataProvider}?)"/>.
        /// </summary>
        /// <typeparam name="TDataProvider">Тип объекта, обеспечивающего передачу данных между внешней подсистемой и <see cref="IKrTaskManager{T}"/>.</typeparam>
        /// <param name="dataProvider">Объект, обеспечивающий передачу данных между внешней подсистемой и <see cref="IKrTaskManager{T}"/>.</param>
        protected virtual void ConfigureNested<TDataProvider>(TDataProvider dataProvider) where TDataProvider : IKrTaskManagerDataProvider
        {
            // Do nothing by default
        }

        #endregion
    }
}
