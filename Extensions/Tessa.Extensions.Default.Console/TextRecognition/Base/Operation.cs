using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Jinni;
using Tessa.Platform.ConsoleApps;
using Tessa.TextRecognition;

namespace Tessa.Extensions.Default.Console.TextRecognition.Base
{
    /// <summary>
    /// Базовая операция распознавания файла.
    /// </summary>
    /// <typeparam name="TContext">Тип контекста операции распознавания файла.</typeparam>
    /// <typeparam name="TRequest">Тип запроса на создание операции распознавания файла.</typeparam>
    /// <typeparam name="TResponse">Тип ответа на результат операции распознавания файла.</typeparam>
    public abstract class Operation<TContext, TRequest, TResponse> :
        ConsoleOperation<TContext>
        where TContext : OperationContext
    {
        #region Fields

        protected readonly IOcrService<TRequest, TResponse> ocrService;

        #endregion

        #region Constructors

        public Operation(
            IConsoleLogger logger,
            IConsoleSessionManager sessionManager,
            IOcrService<TRequest, TResponse> ocrService,
            bool extendedInitialization = false)
            : base(logger, sessionManager, extendedInitialization)
        {
            this.ocrService = NotNullOrThrow(ocrService);
        }

        #endregion

        #region Protected Methods

        protected abstract Task<(Guid? OperationID, JinniBalancingToken? Token)> CreateOperationAsync(TContext context, CancellationToken cancellationToken = default);

        protected async Task CancelOperationAsync(Guid operationID, TContext context, JinniBalancingToken? token = null, CancellationToken cancellationToken = default)
        {
            await this.Logger.InfoAsync("Try cancel OCR operation...");

            if (!await this.ocrService.CancelOperationAsync(operationID, context.ValidationResult, token, cancellationToken))
            {
                await this.Logger.WriteLineAsync();
                await this.Logger.LogResultAsync(context.ValidationResult.Build());
            }
        }

        protected async Task<bool> WaitOperationAsync(Guid operationID, TContext context, JinniBalancingToken? token = null, CancellationToken cancellationToken = default)
        {
            await this.Logger.WriteLineAsync();
            await this.Logger.InfoAsync("Start monitoring OCR operation progress...");
            await this.Logger.InfoAsync("Press Ctrl + C or Ctrl + Break to stop and cancel operation.");
            await this.Logger.WriteLineAsync();

            try
            {
                int operationProgress = 0, delay = 500;
                while (operationProgress < 100)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var ocrOperationStatus = await this.ocrService.GetOperationStatusAsync(operationID, context.ValidationResult, token, cancellationToken);
                    if (ocrOperationStatus is null)
                    {
                        await this.Logger.WriteLineAsync();
                        await this.Logger.LogResultAsync(context.ValidationResult.Build());
                        return false;
                    }
                    else if (!ocrOperationStatus.IsSuccessful)
                    {
                        await this.Logger.WriteLineAsync();
                        await this.Logger.ErrorAsync(ocrOperationStatus.ToString());
                        return false;
                    }
                    else if (ocrOperationStatus.Progress > operationProgress)
                    {
                        operationProgress = ocrOperationStatus.Progress.Value;
                        await this.Logger.WriteAsync($"\rCurrent progress: {operationProgress}%");
                    }
                    else
                    {
                        delay = (int) Math.Min(delay * 1.3f, 10_000);
                        await Task.Delay(delay, cancellationToken);
                    }
                }

                await this.Logger.WriteLineAsync();
                return true;
            }
            catch (OperationCanceledException)
            {
                await this.Logger.WriteLineAsync();
                await this.Logger.InfoAsync("Requested OCR operation cancellation.");
                await this.CancelOperationAsync(operationID, context, token, CancellationToken.None);

                throw;
            }
        }

        protected async Task<TResponse?> GetOperationResultAsync(Guid operationID, TContext context, JinniBalancingToken? token = null, CancellationToken cancellationToken = default)
        {
            await this.Logger.WriteLineAsync();
            await this.Logger.InfoAsync("Try get result for OCR operation...");

            var response = await this.ocrService.GetOperationResultAsync(operationID, context.ValidationResult, token, cancellationToken);
            if (response is null)
            {
                await this.Logger.WriteLineAsync();
                await this.Logger.LogResultAsync(context.ValidationResult.Build());
                return default(TResponse);
            }

            return response;
        }

        #endregion
    }
}
