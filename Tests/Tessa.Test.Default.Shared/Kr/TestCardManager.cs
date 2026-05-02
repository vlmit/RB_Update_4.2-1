using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared.Kr
{
    /// <inheritdoc cref="ITestCardManager"/>
    public class TestCardManager :
        PendingActionsProvider<ITestCardManagerPendingAction, TestCardManager>,
        ITestCardManager
    {
        #region Fields

        private readonly ICardLifecycleCompanionDependencies deps;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр объекта <see cref="TestCardManager"/>.
        /// </summary>
        /// <param name="deps">Зависимости, используемые объектами, управляющими жизненным циклом карточек.</param>
        public TestCardManager(
            ICardLifecycleCompanionDependencies deps)
        {
            ThrowIfNull(deps);

            this.deps = deps;
        }

        #endregion

        #region ITestCardManager Members

        /// <inheritdoc/>
        public ITestCardManager DeleteCardAfterTest<T>(
            T companion,
            Func<T, CancellationToken, ValueTask> successActionAsync = null)
            where T : ICardLifecycleCompanion
        {
            ThrowIfNull(companion);

            this.AddPendingAction(
                new TestCardManagerPendingAction<T>(
                    companion,
                    successActionAsync),
                reverseOrder: true);

            return this;
        }

        /// <inheritdoc/>
        public ITestCardManager DeleteCardAfterTest(
            Guid cardID,
            Func<ICardLifecycleCompanion, CancellationToken, ValueTask> successActionAsync = null)
        {
            return this.DeleteCardAfterTest(
                new CardLifecycleCompanion(
                    cardID,
                    null,
                    null,
                    this.deps),
                successActionAsync);
        }

        /// <inheritdoc/>
        public async Task AfterTestAsync(CancellationToken cancellationToken = default)
        {
            await this.GoAsync(
                ValidationAssert.IsSuccessful,
                cancellationToken);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override async ValueTask<TestCardManager> GoCoreAsync(
            Action<ValidationResult> validationFunc = null,
            CancellationToken cancellationToken = default)
        {
            validationFunc ??= static (result) => (KrTestContext.CurrentContext.ValidationFunc ?? ValidationAssert.IsSuccessful)(result);

            await this.ExecutePendingActionsAsync(
                null,
                validationFunc,
                cancellationToken);

            return this;
        }

        #endregion
    }
}
