using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Tessa.FileConverters;
using Tessa.Platform.Operations;

namespace Tessa.Extensions.Default.Chronos.FileConverters
{
    /// <summary>
    /// Сервис по обработке операций конвертации файлов в рамках работы плагина <see cref="FileConverterPlugin"/>.
    /// </summary>
    public sealed class FileConverterOperationService
    {
        #region Private Fields

        private readonly FileConverterServiceDescriptor descriptor;
        private readonly IFileConverterOperationProcessor processor;

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="FileConverterOperationService"/>.
        /// </summary>
        /// <param name="descriptor"><inheritdoc cref="FileConverterServiceDescriptor" path="/summary"/></param>
        /// <param name="processor"><inheritdoc cref="IFileConverterOperationProcessor" path="/summary"/></param>
        public FileConverterOperationService(
            FileConverterServiceDescriptor descriptor,
            IFileConverterOperationProcessor processor)
        {
            this.descriptor = NotNullOrThrow(descriptor);
            this.processor = NotNullOrThrow(processor);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Выполняет обработку операций по конвертации файла.
        /// </summary>
        /// <param name="operation"><inheritdoc cref="IOperation" path="/summary"/></param>
        /// <param name="request"><inheritdoc cref="IOperation" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns><see langword="true"/> - если обработка операции успешна, иначе - <see langword="false"/></returns>
        public async Task<bool> TryProcessOperationAsync(
            IOperation operation,
            IFileConverterRequest request,
            CancellationToken cancellationToken)
        {
            using var _ = await this.descriptor.ReadLockAsync(cancellationToken);

            try
            {
                logger.Trace("Performing conversion: started.");

                return await this.processor.TryProcessOperationAsync(operation, request, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw;
            }
            finally
            {
                logger.Trace("Performing conversion: completed.");
            }
        }

        #endregion
    }
}
