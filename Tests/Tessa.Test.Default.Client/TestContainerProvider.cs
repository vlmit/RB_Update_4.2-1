#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Web.Services;
using Unity;

namespace Tessa.Test.Default.Client
{
    /// <summary>
    /// Предоставляет методы для создания Unity-контейнера используемого на сервере в тестах с настраиваемым сервером приложений.
    /// </summary>
    /// <param name="dependencies"><inheritdoc cref="WebUnityFactory.Dependencies" path="/summary"/></param>
    public sealed class TestWebUnityFactory(
        Func<WebContainerCreationOptions, object?, IWebContextAccessor, ValueTask<IUnityContainer>> createContainerFunc,
        IWebUnityFactoryDependencies dependencies)
        : WebUnityFactory(dependencies)
    {
        #region Fields

        private readonly Func<WebContainerCreationOptions, object?, IWebContextAccessor, ValueTask<IUnityContainer>> createContainerFuncAsync =
            NotNullOrThrow(createContainerFunc);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask<IWebUnityDescriptor> CreateContainerCoreAsync(
            WebContainerCreationOptions options,
            object? context,
            CancellationToken cancellationToken = default)
        {
            IUnityContainer? container = null;

            try
            {
                container = await this.createContainerFuncAsync(options, context, this.Dependencies.WebContextAccessor);

                var descriptor = new WebUnityDescriptor(container, options);
                container = null;
                return descriptor;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new WebUnityErrorDescriptor(ex, options);
            }
            finally
            {
                if (container is not null)
                {
                    await container.DisposeAllRegistrationsAsync();
                }
            }
        }

        #endregion
    }
}
