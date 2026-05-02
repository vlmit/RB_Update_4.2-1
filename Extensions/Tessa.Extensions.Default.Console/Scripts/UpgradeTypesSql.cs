using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using Tessa.Extensions.Default.Console.ConvertConfiguration;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Data;
using Tessa.Scheme;

namespace Tessa.Extensions.Default.Console.Scripts
{
    [ConsoleScript]
    public sealed class UpgradeTypesSql : ServerConsoleScriptBase
    {
        #region Private Classes

        private sealed class TableSource(string tableName, IEnumerable<string> columnNames)
        {
            #region Properties

            public string TableName { get; } = tableName;

            public string[] ColumnNames { get; } = columnNames.ToArray();

            #endregion

            #region Base Overrides

            public override string ToString() => $"{this.TableName}.{string.Join(',', this.ColumnNames)}";

            #endregion
        }

        #endregion

        #region Private Methods

        private async Task ProcessAsync(string schemePath, CancellationToken cancellationToken)
        {
            // открытие соединений для чтения и записи данных
            await using var dbReader = await this.CreateDbManagerAsync(cancellationToken);
            var dbmsReader = dbReader.Dbms;
            await using var dbWriter = await this.CreateDbManagerAsync(cancellationToken);
            var readerBuilderFactory = new QueryBuilderFactory(dbmsReader);
            var writerBuilderFactory = new QueryBuilderFactory(dbWriter.Dbms);

            var tableSource = new TableSource("Types", new List<string> { "ID", "Metadata", "Name" });

            var updateBuilder = writerBuilderFactory
                .Update(tableSource.TableName);

            for (var i = 1; i < tableSource.ColumnNames.Length - 1; i++) // Последняя колонка "Name" не нужна для апдейта.
            {
                updateBuilder
                    .C(tableSource.ColumnNames[i]).Equals().P(tableSource.ColumnNames[i]);
            }

            var updateSqlText = updateBuilder
                .Where().C(tableSource.ColumnNames[0]).Equals().P(tableSource.ColumnNames[0])
                .Build();

            var parameters = new DataParameter[2];
            parameters[0] = dbWriter.Parameter(tableSource.ColumnNames[0], DataType.Guid);
            parameters[1] = dbWriter.Parameter(tableSource.ColumnNames[1], DataType.BinaryJson);

            // подготовка запроса для чтения данных, не используем грязное чтение для одновременной записи
            // в ту же таблицу на MSSQL, чтобы не было артефактов при чтении из-за параллельных транзакций
            dbReader
                .SetCommand(readerBuilderFactory
                    .Select().C(null, tableSource.ColumnNames)
                    .From(tableSource.TableName).NoLock()
                    .Build())
                .LogCommand()
                .WithoutTimeout();

            var counter = 0;

            // Используется для преобразования связей колонок виртуальной схемы по имени таблиц и колонок в связи по идентификаторам.
            var tsdFilePath = NotEmptyOrThrow(DefaultConsoleHelper.GetSourceFiles(schemePath, "*.tsd").First());
            var fileSchemeService = new FileSchemeService(
                tsdFilePath,
                DefaultConsoleHelper.GetSchemePartitions(tsdFilePath));

            await fileSchemeService.UpdateStorageAsync(cancellationToken);

            var tessaDatabase = new SchemeDatabase(SchemeDatabaseNames.Original);
            await tessaDatabase.RefreshAsync(fileSchemeService, cancellationToken);

            // выполняются запросы по конвертации данных
            await using var reader = await dbReader.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);
                var metadata = await reader.GetSequentialNullableStringAsync(1, dbmsReader, cancellationToken);
                var name = reader.GetString(2);

                if (string.IsNullOrWhiteSpace(metadata))
                {
                    continue;
                }

                try
                {
                    // конвертируем метаданные
                    var convertedJson = await ConvertTypesService.RepairAndConvertAsync(metadata, tessaDatabase.Tables, cancellationToken);

                    // Если конвертация для типа не требовалась, то не надо писать лог, и апдейтить строку в БД.
                    if (convertedJson == metadata)
                    {
                        continue;
                    }

                    // записываем сконвертированные метаданные
                    parameters[0].Value = id;
                    parameters[1].Value = convertedJson;

                    await dbWriter
                        .SetCommand(updateSqlText, parameters)
                        .LogCommand()
                        .ExecuteNonQueryAsync(cancellationToken);

                    await this.Logger.InfoAsync($"Converted type \"{name}\", ID \"{id:D}\".");
                    counter++;
                }
                catch (Exception ex)
                {
                    await this.Logger.ErrorAsync($"Error while converting type ID={id:D}. Metadata:{Environment.NewLine}{metadata}, Exception: {ex.Message}");
                    throw;
                }
            }

            await this.Logger.InfoAsync($"{counter} types converted.");
        }

        #endregion

        #region Base Overrides

        protected override async ValueTask ExecuteCoreAsync(CancellationToken cancellationToken)
        {
            var schemePath = this.TryGetParameter("scheme");
            if (string.IsNullOrEmpty(schemePath))
            {
                await this.Logger.ErrorAsync(
                    "Pass \"scheme\" parameter specifying path to scheme folder or .tsd file" +
                    ", i.e.: -pp:scheme=C:/Tessa/Configuration/Scheme");
                this.Result = -1;
                return;
            }

            await this.ProcessAsync(schemePath, cancellationToken);
        }

        protected override async ValueTask ShowHelpCoreAsync(CancellationToken cancellationToken)
        {
            await this.Logger.WriteLineAsync("Converts card types metadata to latest format version.");
            await this.Logger.WriteLineAsync();
            await this.Logger.WriteLineAsync("Example:");
            await this.Logger.WriteLineAsync(
                $"{Assembly.GetEntryAssembly()?.GetName().Name} Script {nameof(UpgradeTypesSql)}");
        }

        #endregion
    }
}
