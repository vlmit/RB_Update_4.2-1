using LinqToDB.DataProvider;
using Tessa.Platform;
using Tessa.Platform.Data;

namespace Tessa.Test.Default.Shared
{
    /// <summary>
    /// Фабрика для создания объектов <see cref="DbManager"/> для строки подключения,
    /// заданной в конфигурационном файле. Используется в юнит-тестах.
    /// </summary>
    /// <remarks>
    /// Используется по умолчанию при задании типа <see cref="IDbFactory"/> в атрибутах.
    /// </remarks>
    public class DefaultDbFactory :
        IDbFactory,
        IConfigurationStringContainer
    {
        #region IDbFactory Members

        /// <summary>
        /// Создаёт экземпляр класса <see cref="DbManager"/> для строки подключения,
        /// заданной в конфигурационном файле.
        /// </summary>
        /// <returns>Созданный экземпляр класса <see cref="DbManager"/>.</returns>
        public DbManager Create()
        {
            var configuration = ConfigurationManager.Default.Configuration;

            var connectionString = this.ConnectionString;
            if (!string.IsNullOrEmpty(connectionString)
                && configuration.Connections.TryGetValue(this.ConfigurationString, out var connection))
            {
                IDataProvider dataProvider = configuration
                    .GetConfigurationDataProviderFromType(connection.DataProvider)
                    .GetDataProvider(connectionString);

                return new DbManager(dataProvider, connectionString);
            }

            return configuration.CreateDbManager(this.ConfigurationString);
        }

        #endregion

        #region IConfigurationStringContainer Members

        /// <inheritdoc/>
        public string ConfigurationString { get; set; }

        /// <inheritdoc/>
        public string ConnectionString { get; set; }

        #endregion
    }
}
