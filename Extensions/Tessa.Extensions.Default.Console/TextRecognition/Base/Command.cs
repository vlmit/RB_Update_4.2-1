using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.TextRecognition;
using Tessa.TextRecognition.Enums;
using Unity;

namespace Tessa.Extensions.Default.Console.TextRecognition.Base
{
    /// <summary>
    /// Базовая команда распознавания файла.
    /// </summary>
    public abstract class Command
    {
        protected static async Task<OcrParameters?> GetOcrParametersAsync(
            IConsoleLogger logger,
            IEnumerable<OcrLanguage>? languages,
            OcrSegmentationMode segmentationMode,
            int confidence,
            bool preprocess,
            bool detectRotation,
            bool detectTables,
            bool detectBarcodes,
            bool overwrite)
        {
            try
            {
                var ocrLanguages = languages is null
                    ? Enum.GetValues<OcrLanguage>()
                    : languages.Distinct().ToArray();

                return new OcrParameters
                {
                    Languages = ocrLanguages,
                    SegmentationMode = segmentationMode,
                    Confidence = confidence,
                    Preprocess = preprocess,
                    DetectRotation = detectRotation,
                    DetectTables = detectTables,
                    DetectBarcodes = detectBarcodes,
                    Overwrite = overwrite
                };
            }
            catch (Exception ex)
            {
                await logger.LogExceptionAsync("An error occurred during validate recognition parameters.", ex);
                return null;
            }
        }

        protected static async ValueTask<int> ExecuteWithCancelAsync<TOperation, TContext>(
            Func<IUnityContainer, CancellationToken, ValueTask> initializeContainerAsync,
            Func<TOperation, CancellationToken, Task<int>> executeOperationAsync)
            where TContext : OperationContext
            where TOperation : ConsoleOperation<TContext>
        {
            // Инициализация возможности отмены операции
            using var cts = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelKeyPress = (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;

                try
                {
                    // ReSharper disable once AccessToDisposedClosure
                    cts.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // ignored
                }
            };

            try
            {
                System.Console.CancelKeyPress += cancelKeyPress;

                await using var companion = new UnityContainerCompanion { UseConfiguration = true };
                return await companion.ProcessAndGetAsync(
                    (c, ct) => initializeContainerAsync(c.Container, ct),
                    async (c, ct) =>
                    {
                        await using var operation = c.Container.Resolve<TOperation>();
                        return await executeOperationAsync(operation, ct);
                    },
                    cts.Token);
            }
            finally
            {
                System.Console.CancelKeyPress -= cancelKeyPress;
            }
        }
    }
}
