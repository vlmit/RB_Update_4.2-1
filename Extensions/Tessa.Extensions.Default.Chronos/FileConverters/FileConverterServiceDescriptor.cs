using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;

namespace Tessa.Extensions.Default.Chronos.FileConverters
{
    /// <summary>
    /// Объект, который хранит информацию о выполнении плагина <see cref="FileConverterPlugin"/>.
    /// </summary>
    public sealed class FileConverterServiceDescriptor
    {
        #region Private Fields

        private long processedCount;
        private readonly IAsyncReaderWriterLock readerWriterLock;

        #endregion

        #region Constructors

        /// <summary>
        /// Создает экземпляр класса <see cref="FileConverterServiceDescriptor"/>.
        /// </summary>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        public FileConverterServiceDescriptor(CancellationToken cancellationToken)
        {
            this.CancellationToken = cancellationToken;
            this.readerWriterLock = new AsyncReaderWriterLock();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Объект, посредством которого можно отменить асинхронную задачу.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Увеличивает количество обработанных запросов на 1.
        /// </summary>
        /// <returns>Измененное значение.</returns>
        public long IncrementProcessedCount() => Interlocked.Increment(ref this.processedCount);

        /// <summary>
        /// Возвращает количество обработанных запросов.
        /// </summary>
        /// <returns>количество успешно обработанных запросов.</returns>
        public long GetProcessedCount() => Interlocked.Read(ref this.processedCount);

        /// <inheritdoc cref="IAsyncReaderWriterLock.ReaderLockAsync"/>
        public Task<IDisposable> ReadLockAsync(CancellationToken cancellationToken = default) => this.readerWriterLock.ReaderLockAsync(cancellationToken);

        /// <inheritdoc cref="IAsyncReaderWriterLock.WriterLockAsync"/>
        public Task<IDisposable> WriterLockAsync(CancellationToken cancellationToken = default) => this.readerWriterLock.WriterLockAsync(cancellationToken);

        #endregion
    }
}
