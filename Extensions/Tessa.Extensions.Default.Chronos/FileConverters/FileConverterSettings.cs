using System;
using Tessa.Platform;
using Tessa.Platform.Configuration;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Chronos.FileConverters
{
    public class FileConverterSettings :
        IFileConverterSettings
    {
        #region Constructors

        /// <doc path='info[@type="class" and @item=".ctor"]'/>
        public FileConverterSettings()
        {
            // setting up default value, so that created object can be used right away with initializing only non-default props
            this.MaxThreads = DefaultMaxThreads;
            this.PollingPeriod = DefaultPollingPeriod;
            this.RecyclePeriod = DefaultRecyclePeriod;
            this.OldestPreviewFilePeriod = DefaultOldestPreviewFilePeriod;
            this.MaintenancePeriod = DefaultMaintenancePeriod;
        }

        #endregion

        #region Fields and Constants

        /// <summary>
        /// Значение по умолчанию для свойства <see cref="MaxThreads"/>.
        /// </summary>
        public const int DefaultMaxThreads = 4;

        /// <summary>
        /// Значение по умолчанию для свойства <see cref="PollingPeriod"/>.
        /// </summary>
        public static readonly TimeSpan DefaultPollingPeriod = TimeSpan.FromSeconds(1.0);

        /// <summary>
        /// Значение по умолчанию для свойства <see cref="RecyclePeriod"/>.
        /// </summary>
        public static readonly TimeSpan DefaultRecyclePeriod = TimeSpan.FromHours(1.0);

        /// <summary>
        /// Значение по умолчанию для свойства <see cref="OldestPreviewFilePeriod"/>.
        /// </summary>
        public static readonly TimeSpan DefaultOldestPreviewFilePeriod = TimeSpan.FromDays(10.0);

        /// <summary>
        /// Значение по умолчанию для свойства <see cref="MaintenancePeriod"/>.
        /// </summary>
        public static readonly TimeSpan DefaultMaintenancePeriod = TimeSpan.FromHours(1.0);

        private const string FileConverterPrefixName = "FileConverter";

        private const string MaxThreadsPropertyName = $"{FileConverterPrefixName}.{nameof(MaxThreads)}";

        private const string PollingPeriodPropertyName = $"{FileConverterPrefixName}.{nameof(PollingPeriod)}";

        private const string RecyclePeriodPropertyName = $"{FileConverterPrefixName}.{nameof(RecyclePeriod)}";

        private const string OldestPreviewFilePeriodPropertyName = $"{FileConverterPrefixName}.{nameof(OldestPreviewFilePeriod)}";

        private const string MaintenancePeriodPropertyName = $"{FileConverterPrefixName}.{nameof(MaintenancePeriod)}";

        #endregion

        #region IFileConverterSettings Members

        /// <inheritdoc/>
        public int MaxThreads { get; set; }

        /// <inheritdoc/>
        public TimeSpan PollingPeriod { get; set; }

        /// <inheritdoc/>
        public TimeSpan RecyclePeriod { get; set; }

        /// <inheritdoc/>
        public TimeSpan OldestPreviewFilePeriod { get; set; }

        /// <inheritdoc/>
        public TimeSpan MaintenancePeriod { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Устанавливает значения настроек из файла конфигурации.
        /// </summary>
        /// <param name="configurationManager"><inheritdoc cref="IConfigurationManager" path="/summary"/></param>
        /// <returns>Текущий объект для цепочки вызовов.</returns>
        public FileConverterSettings SetFromConfig(IConfigurationManager configurationManager)
        {
            ThrowIfNull(configurationManager);

            // first of all, parse all values, because exceptions can mess up the props half-way
            var settings = configurationManager.Configuration.Settings;
            var maxThreads = (int?) settings.TryGet<long?>(MaxThreadsPropertyName) ?? DefaultMaxThreads;
            var pollingPeriod = ParseTimeSpan(PollingPeriodPropertyName, DefaultPollingPeriod);
            var recyclePeriod = ParseTimeSpan(RecyclePeriodPropertyName, DefaultRecyclePeriod);
            var oldestPreviewFilePeriod = ParseTimeSpan(OldestPreviewFilePeriodPropertyName, DefaultOldestPreviewFilePeriod);
            var maintenancePeriod = ParseTimeSpan(MaintenancePeriodPropertyName, DefaultMaintenancePeriod);

            this.MaxThreads = maxThreads;
            this.PollingPeriod = pollingPeriod;
            this.RecyclePeriod = recyclePeriod;
            this.OldestPreviewFilePeriod = oldestPreviewFilePeriod;
            this.MaintenancePeriod = maintenancePeriod;

            return this;

            static TimeSpan ParseTimeSpan(string key, TimeSpan defaultValue) =>
                ConfigurationManager.Settings.TryGet<string>(key) is { Length: > 0 } value
                    ? TimeSpan.Parse(value)
                    : defaultValue;
        }

        #endregion
    }
}
