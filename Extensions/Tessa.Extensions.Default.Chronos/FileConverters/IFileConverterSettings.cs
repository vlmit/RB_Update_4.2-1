using System;

namespace Tessa.Extensions.Default.Chronos.FileConverters
{
    /// <summary>
    /// Настройки API по конвертации файлов в Chronos.
    /// </summary>
    public interface IFileConverterSettings
    {
        /// <summary>
        /// Максимальное число потоков параллельной обработки операций плагином.
        /// </summary>
        int MaxThreads { get; }

        /// <summary>
        /// Интервал времени, через который плагин выполняет опрос таблицы операций для получения операции для выполнения.
        /// </summary>
        TimeSpan PollingPeriod { get; }

        /// <summary>
        /// Интервал времени, через который плагин выполняет остановку для освобождения ресурсов, используемых плагином.
        /// </summary>
        TimeSpan RecyclePeriod { get; }

        /// <summary>
        /// Период хранения файла с момента последнего обращения.
        /// </summary>
        TimeSpan OldestPreviewFilePeriod { get; }

        /// <summary>
        /// Частота технического обслуживания плагина.
        /// </summary>
        TimeSpan MaintenancePeriod { get; }
    }
}
