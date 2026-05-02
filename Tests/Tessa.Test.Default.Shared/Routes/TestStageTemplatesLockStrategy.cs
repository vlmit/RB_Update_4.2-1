using System.Threading;
using System.Threading.Tasks;
using Tessa.Extensions.Default.Server.Workflow.KrCompilers;
using Tessa.Platform.Data;
using Tessa.Platform.Validation;

namespace Tessa.Test.Default.Shared.Routes
{
    public sealed class TestStageTemplatesLockStrategy :
        IKrStageTemplateLockStrategy
    {
        #region Constants And Static Fields

        private const int attemptCount = 100;
        private const int timeout = 100;
        private static int lockedState = 0;
        private static readonly Lock lockObject = new();

        #endregion

        #region IKrStageTemplateLockStrategy Implementation

        /// <inheritdoc />
        public async Task<ValidationResult> ObtainReaderLockAsync(CancellationToken cancellationToken = default)
        {
            for (var i = 0; i <= attemptCount; i++)
            {
                lock (lockObject)
                {
                    if (lockedState >= 0)
                    {
                        lockedState++;
                        TransactionScopeContext.Current.Handlers.Add(_ctx =>
                        {
                            lock (lockObject)
                            {
                                lockedState--;
                            }
                            return ValueTask.CompletedTask;
                        }
                        );
                        return ValidationResult.Empty;
                    }
                }
                await Task.Delay(timeout, cancellationToken);
            }
            var validationResult = new ValidationResultBuilder();
            ValidationSequence
                .Begin(validationResult)
                .SetObjectName(this)
                .Error(ValidationKeys.ObjectLockingStrategyReaderLock)
                .End();
            return validationResult.Build();
        }

        /// <inheritdoc />
        public async Task<ValidationResult> ObtainWriterLockAsync(CancellationToken cancellationToken = default)
        {
            for (var i = 0; i <= attemptCount; i++)
            {
                lock (lockObject)
                {
                    if (lockedState <= 0)
                    {
                        lockedState--;
                        TransactionScopeContext.Current.Handlers.Add(_ctx =>
                        {
                            lock (lockObject)
                            {
                                lockedState++;
                            }
                            return ValueTask.CompletedTask;
                        }
                        );
                        return ValidationResult.Empty;
                    }
                }
                await Task.Delay(timeout, cancellationToken);
            }
            var validationResult = new ValidationResultBuilder();
            ValidationSequence
                .Begin(validationResult)
                .SetObjectName(this)
                .Error(ValidationKeys.ObjectLockingStrategyWriterLock)
                .End();
            return validationResult.Build();
        }

        #endregion
    }
}
