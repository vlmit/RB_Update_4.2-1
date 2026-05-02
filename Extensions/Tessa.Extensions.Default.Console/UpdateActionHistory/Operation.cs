using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Scheme;
using Unity;

namespace Tessa.Extensions.Default.Console.UpdateActionHistory
{
    public static class Operation
    {
        #region Private Fields

        private static readonly Dictionary<string, string[]> cuttableTables = new(StringComparer.OrdinalIgnoreCase)
        {
            [Names.Files] = [Names.Table_ID, Names.Table_RowID],
            [Names.Tasks] = [Names.Table_ID, Names.Table_RowID],
            ["Views"] = ["ID", "Alias"],
            [Names.Instances] = [Names.Instances_ID, Names.Instances_Type],
            [Names.NormalizationSources] = [Names.NormalizationSources_ID, Names.NormalizationSources_Name]
        };

        #endregion

        #region Public Members

        public static async Task<int> ExecuteAsync(
            IConsoleLogger logger,
            string? source,
            string? sourceConfigurationString,
            string? targetConfigurationString)
        {
            if (string.IsNullOrEmpty(targetConfigurationString))
            {
                await logger.ErrorAsync("Target configuration string isn't specified");
                return -1;
            }

            if (!string.IsNullOrEmpty(source))
            {
                return await CreateOrUpdateTargetSchemeAsync(
                    logger,
                    async () => await GetSchemeDatabaseFromFilesAsync(logger, source),
                    targetConfigurationString);
            }

            return await CreateOrUpdateTargetSchemeAsync(
                logger,
                async () => await GetSchemeDatabaseFromDatabaseAsync(logger, sourceConfigurationString),
                targetConfigurationString);
        }

        #endregion

        #region Private Members

        private static async Task<SchemeDatabase?> GetSchemeDatabaseFromFilesAsync(
            IConsoleLogger logger,
            string source)
        {
            var filePath = DefaultConsoleHelper.GetSourceFiles(source, "*.tsd").FirstOrDefault();
            if (filePath is null)
            {
                await logger.ErrorAsync("Can't find database file *.tsd in \"{0}\"", source);
                return null;
            }

            await logger.InfoAsync("Importing the scheme using file source");

            var fileFullPath = Path.GetFullPath(filePath);
            await logger.InfoAsync("Reading scheme from: \"{0}\"", fileFullPath);

            var fileSchemeService = new FileSchemeService(fileFullPath);

            if (!await fileSchemeService.IsStorageUpToDateAsync())
            {
                await logger.InfoAsync("Scheme isn't up-to-date in the file folder, upgrading it...");
                await fileSchemeService.UpdateStorageAsync();
            }

            SchemeDatabase tessaDatabase = new(SchemeDatabaseNames.Original);
            await tessaDatabase.RefreshAsync(fileSchemeService);

            return tessaDatabase;
        }

        private static async Task<SchemeDatabase> GetSchemeDatabaseFromDatabaseAsync(
            IConsoleLogger logger,
            string? sourceConfigurationString)
        {
            await logger.InfoAsync("Importing the scheme using sql connection");

            await logger.InfoAsync(
                string.IsNullOrEmpty(sourceConfigurationString)
                    ? "Connection will be opened to default database"
                    : "Connection will be opened to database with connection \"{0}\"",
                sourceConfigurationString);

            await using var companion = new UnityContainerCompanion { UseConfiguration = true };
            return await companion.ProcessAndGetAsync(
                (c, ct) => c.Container
                    .RegisterDbManager(sourceConfigurationString)
                    .RegisterDatabaseForConsoleAsync(cancellationToken: ct),
                async (c, ct) =>
                {
                    var dbScope = c.Container.Resolve<IDbScope>();

                    var databaseSchemeService = new DatabaseSchemeService(
                        dbScope,
                        new ServerConfigurationVersionProvider(dbScope));

                    if (!await databaseSchemeService.IsStorageUpToDateAsync(ct))
                    {
                        await logger.InfoAsync("Scheme isn't up-to-date in the database, upgrading it...");
                        await databaseSchemeService.UpdateStorageAsync(ct);
                    }

                    SchemeDatabase tessaDatabase = new(SchemeDatabaseNames.Original);
                    await tessaDatabase.RefreshAsync(databaseSchemeService, ct);

                    return tessaDatabase;
                });
        }

        private static async Task<int> CreateOrUpdateTargetSchemeAsync(
            IConsoleLogger logger,
            Func<Task<SchemeDatabase?>> getFuncAsync,
            string targetConfigurationString)
        {
            var tessaDatabase = await getFuncAsync();
            if (tessaDatabase is null)
            {
                return -1;
            }

            if (!tessaDatabase.Tables.Contains("ActionHistory"))
            {
                await logger.InfoAsync("ActionHistory table isn't found.");
                return -1;
            }

            await logger.InfoAsync("Preparing the scheme");

            PrepareDatabase(tessaDatabase);

            return await StoreDatabaseAsync(logger, tessaDatabase, targetConfigurationString);
        }

        private static async Task<int> StoreDatabaseAsync(
            IConsoleLogger logger,
            SchemeDatabase tessaDatabase,
            string targetConfigurationString)
        {
            await logger.InfoAsync("Importing the scheme");

            await using var companion = new UnityContainerCompanion { UseConfiguration = true };
            return await companion.ProcessAndGetAsync(
                (c, ct) => c.Container
                    .RegisterDbManager(targetConfigurationString)
                    .RegisterDatabaseForConsoleAsync(cancellationToken: ct),
                async (c, ct) =>
                {
                    var dbScope = c.Container.Resolve<IDbScope>();

                    var databaseSchemeService = new DatabaseSchemeService(
                        dbScope,
                        new ServerConfigurationVersionProvider(dbScope));

                    if (!await databaseSchemeService.IsStorageExistsAsync(ct))
                    {
                        await logger.InfoAsync("Scheme doesn't exists in the database, creating it...");
                        await databaseSchemeService.CreateStorageAsync(ct);
                    }

                    if (!await databaseSchemeService.IsStorageUpToDateAsync(ct))
                    {
                        await logger.InfoAsync("Scheme isn't up-to-date in the database, upgrading it...");
                        await databaseSchemeService.UpdateStorageAsync(ct);
                    }

                    await tessaDatabase.SubmitChangesAsync(databaseSchemeService, ct);

                    await logger.InfoAsync("Scheme has been imported successfully");
                    return 0;
                });
        }

        private static void PrepareDatabase(SchemeDatabase tessaDatabase)
        {
            for (var i = tessaDatabase.Functions.Count - 1; i >= 0; i--)
            {
                var function = tessaDatabase.Functions[i];
                if (function.IsPermanent
                    || string.Equals(function.Name, "DropFunction", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                tessaDatabase.Functions.RemoveAt(i);
            }

            for (var i = tessaDatabase.Procedures.Count - 1; i >= 0; i--)
            {
                var procedure = tessaDatabase.Procedures[i];
                if (procedure.IsPermanent)
                {
                    continue;
                }

                tessaDatabase.Procedures.RemoveAt(i);
            }

            for (var i = tessaDatabase.Migrations.Count - 1; i >= 0; i--)
            {
                var migration = tessaDatabase.Migrations[i];
                if (migration.IsPermanent)
                {
                    continue;
                }

                tessaDatabase.Migrations.RemoveAt(i);
            }

            HashSet<string> systemTables = new(StringComparer.OrdinalIgnoreCase)
            {
                "CompiledViews"
            };

            foreach (var table in tessaDatabase.Tables)
            {
                if (cuttableTables.TryGetValue(table.Name, out var columns))
                {
                    for (var j = table.Columns.Count - 1; j >= 0; j--)
                    {
                        var column = table.Columns[j];
                        if (columns.Any(c => c.Equals(column.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }

                        table.Columns.Remove(column);
                    }
                }

                if (!table.IsPermanent
                    && !string.Equals(table.Name, "CompiledViews", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                systemTables.AddRange(
                    table
                        .GetReferencedTables()
                        .Select(rt => rt.Name));
            }

            for (var i = tessaDatabase.Tables.Count - 1; i >= 0; i--)
            {
                var table = tessaDatabase.Tables[i];
                if (table.IsPermanent
                    || systemTables.Contains(table.Name))
                {
                    continue;
                }

                if (string.Equals(table.Name, "ActionHistory", StringComparison.OrdinalIgnoreCase))
                {
                    for (var j = table.Columns.Count - 1; j >= 0; j--)
                    {
                        var column = table.Columns[j];

                        if (column is SchemeComplexColumn complexColumn)
                        {
                            table.Columns.RemoveAt(j);

                            for (var k = 0; k < complexColumn.Columns.Count; k++)
                            {
                                var subColumn = complexColumn.Columns[k];
                                table.Columns.Insert(j + k, new SchemePhysicalColumn(subColumn.ID, subColumn.Name, NotNullOrThrow(subColumn.Type)));
                            }
                        }
                    }

                    continue;
                }

                tessaDatabase.Tables.RemoveAt(i);
            }
        }

        #endregion
    }
}
