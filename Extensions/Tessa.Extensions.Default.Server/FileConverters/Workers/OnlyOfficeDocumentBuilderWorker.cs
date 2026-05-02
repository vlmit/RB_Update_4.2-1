#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Tessa.Extensions.Default.Server.OnlyOffice;
using Tessa.FileConverters;
using Tessa.Platform;
using Tessa.Platform.IO;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.FileConverters.Workers
{
    /// <summary>
    /// Объект, ответственный за преобразование файла в формат <see cref="FileConverterFormat.Pdf"/>
    /// посредством сервиса конвертации OnlyOffice
    /// </summary>
    /// <remarks>
    /// Наследники класса могут переопределять методы интерфейса, например, добавив к ним обработку файлов других форматов.
    /// Класс может также реализовывать <see cref="IAsyncDisposable"/> для очистки ресурсов,
    /// для этого в наследнике переопределяется метод <see cref="DisposeAsync"/> и вызывается сначала его базовая реализация.
    /// </remarks>
    public class OnlyOfficeDocumentBuilderWorker :
        IFileConverterWorker,
        IAsyncDisposable
    {
        #region Fields

        private readonly IOnlyOfficeSettingsProvider settingsProvider;

        private IProcessManager? processManager;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Constructors

        /// <summary>
        /// Создаёт экземпляр класса с указанием его зависимостей.
        /// </summary>
        /// <param name="settingsProvider"><inheritdoc cref="IOnlyOfficeSettingsProvider" path="/summary"/></param>
        /// <param name="createProcessManagerFunc">Фабрика, выполняющая создание <see cref="IProcessManager"/> .</param>
        public OnlyOfficeDocumentBuilderWorker(
            IOnlyOfficeSettingsProvider settingsProvider,
            Func<IProcessManager> createProcessManagerFunc)
        {
            this.settingsProvider = NotNullOrThrow(settingsProvider);
            this.processManager = new LazyProcessManager(NotNullOrThrow(createProcessManagerFunc));
        }

        #endregion

        #region IFileConverterWorker Members

        /// <inheritdoc/>
        public virtual async Task ConvertFileAsync(IFileConverterContext context, CancellationToken cancellationToken = default)
        {
            ThrowIfNull(context);
            ThrowIfNull(context.Request);
            
            logger.Trace("Start converting");
            
            var processManager = this.processManager;
            ThrowIfDisposed(processManager is null, this);

            var settings = await this.settingsProvider.GetSettingsAsync(cancellationToken);
            var documentBuilderPath = NotNullOrThrow(settings.DocumentBuilderPath);

            ITempFile? outputFile = null;
            bool outputFileAddedToFinalizationQueue = false;

            try
            {
                using var inputFile = TempFile.Acquire($"{context.Request.VersionID}.{context.InputExtension}");
                outputFile = TempFile.Acquire($"{context.Request.VersionID}");

                await using (var input = File.OpenWrite(inputFile.Path))
                {
                    await using var stream = await context.GetInputContentAsync(cancellationToken);
                    await stream.CopyToAsync(input, cancellationToken);
                }

                var script = @$"
builder.OpenFile(""{inputFile.Path}"");
builder.SaveFile(""pdf"", ""{outputFile.Path}"");
builder.CloseFile();
";

                using var scriptFile = TempFile.Acquire("script");
                await File.WriteAllTextAsync(scriptFile.Path, script, Encoding.UTF8, cancellationToken);

                var startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    FileName = documentBuilderPath,
                    Arguments = $"\"{scriptFile.Path}\""
                };

                string output, error;
                int exitCode;
                using (Process process = processManager.StartProcess(startInfo))
                {
                    (output, error) = await process.ReadOutputAndErrorToEndAsync(cancellationToken);
                    await process.WaitForExitAsync(cancellationToken);

                    exitCode = process.ExitCode;
                }

                output = (output.Trim() + Environment.NewLine + error.Trim()).Trim();
                if (exitCode != 0 || output.Contains("error", StringComparison.OrdinalIgnoreCase))
                {
                    if (output.Length == 0)
                    {
                        output = "Unknown error";
                    }

                    context.ValidationResult.AddError(this,
                        "Conversion failed. Exit code: {0}. Output:{1}{2}",
                        exitCode, Environment.NewLine, output);

                    return;
                }

                logger.Trace("PDF file is generated from {0}.", context.InputExtension);

                var convertedFileStream = FileHelper.OpenRead(outputFile.Path);
                context.GetOutputContentAsync = _ => new((convertedFileStream, convertedFileStream.Length));
                context.FinalizationQueue.Add(() =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    outputFile.Dispose();
                    return ValueTask.CompletedTask;
                });
                outputFileAddedToFinalizationQueue = true;

                // пишем ключ, через который вызывающая сторона поймёт, что конвертация была выполнена через наш конвертер
                context.ResponseInfo[FileConverterWorkerNames.OnlyOfficeDocumentBuilderToPdf] = BooleanBoxes.True;
            }
            finally
            {
                if (!outputFileAddedToFinalizationQueue)
                {
                    outputFile?.Dispose();
                }
            }

            logger.Trace("End converting");
        }

        /// <inheritdoc/>
        public virtual Task PreprocessAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <inheritdoc/>
        public virtual Task PerformMaintenanceAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        #endregion

        #region IAsyncDisposable Members

        /// <inheritdoc/>
        public virtual ValueTask DisposeAsync()
        {
            this.processManager?.Dispose();
            this.processManager = null;
            return ValueTask.CompletedTask;
        }

        #endregion
    }
}
