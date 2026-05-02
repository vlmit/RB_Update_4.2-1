#nullable enable

using Tessa.Cards;

namespace Tessa.Test.Default.Shared.Cards
{
    /// <summary>
    /// Тестовые данные по местоположению контента файла <see cref="CardFileSourceType"/>.
    /// </summary>
    public static class TestCardFileSourceTypes
    {
        #region Static Fields

        /// <summary>
        /// Контент файла размещается в базе данных в соответствии с настройками по умолчанию для тестов.
        /// </summary>
        public static readonly CardFileSourceType Database = new(id: 1);

        /// <summary>
        /// Контент файла размещается в файловой системе в соответствии с настройками по умолчанию для тестов.
        /// </summary>
        public static readonly CardFileSourceType FileSystem = new(id: 2);

        #endregion

        #region Static Methods

        /// <summary>
        /// Создаёт настройки с местоположением контента файлов для файловой системы и базы данных по умолчанию.
        /// </summary>
        /// <param name="fileBasePath">Полный путь к файловой папке.</param>
        /// <param name="dbConfigurationString">
        /// Местоположение базы данных, соответствующее имени строки подключения в конфигурационном файле.
        /// Значение по умолчанию определяет, что соединение с базой данных будет также с параметрами по умолчанию
        /// (независимо от названия строки подключения).
        /// </param>
        /// <param name="useDatabaseAsDefault">Признак того, что в качестве источника файлов по умолчанию используется база данных.</param>
        /// <returns>Созданный объект настроек.</returns>
        public static CardFileSourceSettings CreateDefaultSettings(
            string fileBasePath,
            string dbConfigurationString = CardFileSourceType.DefaultDatabasePath,
            bool useDatabaseAsDefault = false)
        {
            ThrowIfNullOrEmpty(fileBasePath);
            ThrowIfNullOrEmpty(dbConfigurationString);

            var dbSource = new CardFileSource(Database, nameof(Database), dbConfigurationString, isDatabase: true);
            var fileSource = new CardFileSource(FileSystem, nameof(FileSystem), fileBasePath, isDatabase: false);

            return new(
                new[] { dbSource, fileSource },
                defaultSource: useDatabaseAsDefault ? dbSource : fileSource);
        }

        #endregion
    }
}
