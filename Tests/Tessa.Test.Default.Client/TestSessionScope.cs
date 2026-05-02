#nullable enable

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Tessa.Platform;
using Tessa.Test.Default.Shared;
using Tessa.Test.Default.Shared.Kr;
using Unity;

namespace Tessa.Test.Default.Client
{
    /// <summary>
    /// Область действия сессии.
    /// </summary>
    public sealed class TestSessionScope :
        TestBaseUnityContainerScope
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="outerUnityContainer">Unity контейнер внешней сессии.</param>
        public TestSessionScope(
            IUnityContainer outerUnityContainer)
            : base(outerUnityContainer)
        {
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask DisposeCoreAsync()
        {
            await base.DisposeCoreAsync();

            if (KrTestContext.CurrentContext.UnityContainer is { } currentUnityContainer)
            {
                try
                {
                    var sessionManager = currentUnityContainer.Resolve<ITestSessionManager>();
                    await sessionManager.CloseAsync();
                }
                catch (Exception ex)
                {
                    await TestContext.Error.WriteLineAsync($"Can't close session: {ex.GetFullText()}");
                    // do not throw when disposing
                }
            }
        }

        #endregion
    }
}
