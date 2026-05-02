using System;
using System.Threading;
using System.Threading.Tasks;
using Chronos.Plugins;
using Chronos.Plugins.Base;
using NLog;
using Tessa.Cards;
using Tessa.Discovery;
using Tessa.Extensions.Default.Server.Plugins;
using Tessa.Extensions.Platform.Server.Plugins;
using Tessa.FileConverters;
using Tessa.Platform;
using Tessa.Platform.Configuration;
using Tessa.Platform.Operations;
using Tessa.Platform.Runtime;
using Tessa.Platform.Validation;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Chronos.FileConverters
{
    /// <summary>
    /// Выполняет преобразование файлов в заданный формат и сохраняет их в карточку кэша.
    /// </summary>
    [Plugin(
        Version = 1,
        Name = "File converter plugin",
        Description = "Convert files to specific formats and stores them to the cache card",
        JsonName = DefaultPluginNames.FileConverterPlugin)]
    public sealed class FileConverterPlugin :
        OperationRunnerPluginBase<OperationBasePluginSettings>
    {
        #region Private Fields

        private IErrorManager? errorManager;
        private IFileConverterSettings? fileConverterSettings;
        private IFileConverterCache? fileConverterCache;
        private IFileConverterWorker? fileConverterWorker;
        private IChronosDiscoveryStrategy? chronosDiscoveryStrategy;
        private IFileConverterOperationProcessor? operationProcessor;

        private FileConverterServiceDescriptor? descriptor;
        private FileConverterOperationService? operationService;
        private FileConverterMaintenanceService? maintenanceService;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        protected override bool DeleteOperation => false;

        /// <inheritdoc/>
        protected override int MaxThreads => NotNullOrThrow(this.fileConverterSettings).MaxThreads;

        /// <inheritdoc/>
        protected override Guid OperationTypeID => OperationTypes.ConvertingFile;

        /// <inheritdoc/>
        protected override TimeSpan RecyclePeriod => NotNullOrThrow(this.fileConverterSettings).RecyclePeriod;

        /// <inheritdoc/>
        protected override TimeSpan PollingPeriod => NotNullOrThrow(this.fileConverterSettings).PollingPeriod;

        /// <inheritdoc/>
        public override async Task EntryPointAsync(CancellationToken cancellationToken = default)
        {
            logger.Info("File converter: starting plugin.");

            try
            {
                await base.EntryPointAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogException(ex);
            }
            finally
            {
                // Так как обработчик может быть не зарегистрирован в IUnityDisposableContainer,
                // то выполняется явное освобождение ресурсов обработчика при завершении работы
                await this.DisposeFileConverterWorkerAsync();
            }

            logger.Info("File converter: shutdown completed.");
        }

        /// <inheritdoc/>
        protected override ValueTask RegisterInContainerAsync(
            IUnityContainer container,
            CancellationToken cancellationToken = default)
        {
            container
                .RegisterFactory<IFileConverterSettings>(
                    c => new FileConverterSettings().SetFromConfig(c.Resolve<IConfigurationManager>()),
                    new ContainerControlledLifetimeManager());

            return base.RegisterInContainerAsync(container, cancellationToken);
        }

        /// <inheritdoc/>
        protected override async ValueTask<bool> InitializePluginAsync(
            IUnityContainer container,
            CancellationToken cancellationToken = default)
        {
            // Получение необходимых зависимостей из контейнера
            this.fileConverterSettings = container.Resolve<IFileConverterSettings>();
            this.errorManager = container.Resolve<IErrorManager>();
            this.fileConverterCache = container.Resolve<IFileConverterCache>();
            this.chronosDiscoveryStrategy = container.Resolve<IChronosDiscoveryStrategy>();
            this.operationProcessor = container.Resolve<IFileConverterOperationProcessor>();

            if (container.TryResolve<IFileConverterWorker>() is not { } fileConverterWorker)
            {
                logger.Error($"Can't find registration for {typeof(IFileConverterWorker).FullName}. Check if default extensions are registered.");
                return false;
            }

            this.fileConverterWorker = fileConverterWorker;
            await this.fileConverterWorker.PreprocessAsync(cancellationToken);

            // Инициализация сервисов по конвертации, техническому обслуживанию и очистки кэша
            this.descriptor = new FileConverterServiceDescriptor(cancellationToken);
            this.operationService = new FileConverterOperationService(this.descriptor, this.operationProcessor);
            this.maintenanceService = new FileConverterMaintenanceService(this.descriptor, this.fileConverterSettings, this.fileConverterWorker.PerformMaintenanceAsync);

            return true;
        }

        /// <inheritdoc/>
        protected override OperationBasePluginSettings InitializePluginSettings()
        {
            var settings = new OperationBasePluginSettings(this.PluginName);
            if (this.PluginSettingsProvider?.TryGetPluginSettingsData(this.PluginName) is { } pluginData)
            {
                settings.Deserialize(pluginData);
            }

            return settings;
        }

        /// <inheritdoc/>
        protected override async ValueTask ProcessOperationAsync(
            IOperation operation,
            CancellationToken cancellationToken = default)
        {
            logger.Debug($"Operation with ID={operation.ID:B} has been started.");

            IFileConverterRequest? request = null;
            var stateDescription = "conversion file";
            this.descriptor!.IncrementProcessedCount();

            try
            {
                request = new FileConverterRequest();
                request.Deserialize(NotNullOrThrow(operation.Request?.Info));

                stateDescription += $" \"{request.FileName}\" with CardID={request.CardID:B} and FileID={request.FileID:B}";
                await this.WritePluginStateAsync(DiscoveryHelper.CommandResponseStateOK, $"Performing {stateDescription}", cancellationToken);

                if (!await this.operationService!.TryProcessOperationAsync(operation, request, cancellationToken))
                {
                    await this.WritePluginStateAsync(DiscoveryHelper.CommandResponseStateError, $"Error during {stateDescription}", cancellationToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.WritePluginStateAsync(DiscoveryHelper.CommandResponseStateError, $"Error during {stateDescription}", cancellationToken);

                logger.LogException($"An exception occurred during operation with ID={operation.ID:B}.", ex);
                await this.CompleteWithUnhandledExceptionAsync(operation.ID, ex, request, cancellationToken);
            }
            finally
            {
                GCHelper.CollectAll(compactLargeObjectHeap: true);
                logger.Debug($"Operation with ID={operation.ID:B} was completed.");
            }
        }

        /// <inheritdoc/>
        public override async Task StopAsync(IPluginStopToken token)
        {
            logger.Info("File converter: shutting down.");

            await DisposeNullableInstanceAsync(this.maintenanceService);

            await base.StopAsync(token);
        }

        #endregion

        #region Private Methods

        private async Task WritePluginStateAsync(
            string state,
            string stateDescription,
            CancellationToken cancellationToken)
        {
            var pluginState = new PluginState
            {
                Cid = this.CidName!,
                State = state,
                StateDescription = stateDescription
            };
            await this.chronosDiscoveryStrategy!.NotifyAsync(pluginState, cancellationToken);
        }

        private async Task CompleteWithUnhandledExceptionAsync(
            Guid operationID,
            Exception exception,
            IFileConverterRequest? request,
            CancellationToken cancellationToken)
        {
            // ErrorException уже записывает ошибку в лог, не будем её дублировать

            var response = new OperationResponse();
            var result = response.ValidationResult.AddException(this, exception).Build();

            await this.errorManager!.ReportErrorSafeAsync(
                CardHelper.FileConverterCacheTypeID,
                request?.CardID ?? FileConverterHelper.CacheCardID,
                request is null ? "Unhandled exception" : request.EventName,
                new ErrorDescription(
                    result,
                    ErrorCategories.FileConverterFailed,
                    result.ToString(ValidationLevel.Message)),
                id: operationID);

            if (request is not null && request.Flags.Has(FileConverterRequestFlags.WithoutResponse))
            {
                try
                {
                    await this.OperationRepository!.DeleteAsync(operationID, OperationTypes.ConvertingFile, CancellationToken.None);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogException($"Error during removing file conversion operation with ID={operationID:B}:", ex, LogLevel.Warn);
                }
            }

            await this.OperationRepository!.CompleteAsync(operationID, OperationTypes.ConvertingFile, response, cancellationToken);
        }

        private ValueTask DisposeFileConverterWorkerAsync()
        {
            switch (this.fileConverterWorker)
            {
                case IAsyncDisposable asyncDisposable:
                    return asyncDisposable.DisposeAsync();

                // ReSharper disable once SuspiciousTypeConversion.Global
                case IDisposable disposable:
                    disposable.Dispose();
                    return ValueTask.CompletedTask;

                default:
                    return ValueTask.CompletedTask;
            }
        }

        private static ValueTask DisposeNullableInstanceAsync(IAsyncDisposable? instance) =>
            instance is not null ? instance.DisposeAsync() : ValueTask.CompletedTask;

        #endregion
    }
}
