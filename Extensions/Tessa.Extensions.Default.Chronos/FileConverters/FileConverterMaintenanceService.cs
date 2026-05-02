using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Tessa.Platform;

namespace Tessa.Extensions.Default.Chronos.FileConverters
{
    /// <summary>
    /// Сервис по техническому обслуживанию в рамках работы плагина <see cref="FileConverterPlugin"/>.
    /// </summary>
    public sealed class FileConverterMaintenanceService :
        IAsyncDisposable
    {
        #region Private Fields

        private long lastProcessedCount;

        private readonly Timer timer;
        private readonly FileConverterServiceDescriptor descriptor;
        private readonly Func<CancellationToken, Task> action;

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="FileConverterMaintenanceService"/>.
        /// </summary>
        /// <param name="descriptor"><inheritdoc cref="FileConverterServiceDescriptor" path="/summary"/></param>
        /// <param name="fileConverterSettings"><inheritdoc cref="IFileConverterSettings" path="/summary"/></param>
        /// <param name="action">Действие, выполняемое в период технического обслуживания.</param>
        public FileConverterMaintenanceService(
            FileConverterServiceDescriptor descriptor,
            IFileConverterSettings fileConverterSettings,
            Func<CancellationToken, Task> action)
        {
            ThrowIfNull(fileConverterSettings);
            var maintenancePeriod = fileConverterSettings.MaintenancePeriod;

            this.descriptor = NotNullOrThrow(descriptor);
            this.action = NotNullOrThrow(action);
            using (ExecutionContext.SuppressFlow())
            {
                // Don't capture the current ExecutionContext and its AsyncLocals onto the timer causing them to live forever
                this.timer = new(this.OnTimerTick, state: null, maintenancePeriod, maintenancePeriod);
            }
        }

        #endregion

        #region Private Methods

        private async void OnTimerTick(object? state)
        {
            using var _ = await this.descriptor.WriterLockAsync(this.descriptor.CancellationToken);

            var currentProcessedCount = this.descriptor.GetProcessedCount();
            if (currentProcessedCount <= this.lastProcessedCount)
            {
                return;
            }

            try
            {
                logger.Info("Performing maintenance: started.");

                await this.action(this.descriptor.CancellationToken);

                this.lastProcessedCount = currentProcessedCount;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogException(ex, LogLevel.Warn);
            }
            finally
            {
                logger.Info("Performing maintenance: completed.");
            }
        }

        #endregion

        #region IAsyncDisposable Implementation

        /// <inheritdoc/>
        public ValueTask DisposeAsync() => this.timer.DisposeAsync();

        #endregion
    }
}
