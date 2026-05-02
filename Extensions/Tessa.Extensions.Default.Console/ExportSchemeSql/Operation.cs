using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Scheme;
using Tessa.Scheme.Differences;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Console.ExportSchemeSql
{
    public static class Operation
    {
        public static async Task<int> ExecuteAsync(
            IConsoleLogger logger,
            string? outputFolder,
            bool updateSchemeInDatabase,
            string? configurationString,
            string? databaseName)
        {
            await logger.InfoAsync("Exporting the scheme using sql connection");

            await logger.InfoAsync(
                string.IsNullOrEmpty(configurationString)
                    ? "Connection will be opened to default database"
                    : "Connection will be opened to database with connection \"{0}\"",
                configurationString);

            if (!string.IsNullOrEmpty(databaseName))
            {
                await logger.InfoAsync("Changes will be applied from database \"{0}\"", databaseName);
            }

            Func<DbManager>?[] createDbManagerFuncClosure = [null];
            await using var companion = new UnityContainerCompanion { UseConfiguration = true };
            return await companion.ProcessAndGetAsync(
                (c, ct) => c.Container
                    .RegisterFactory<DbManager>(
                        _ => NotNullOrThrow(createDbManagerFuncClosure[0])(),
                        new PerResolveLifetimeManager())
                    .RegisterDatabaseForConsoleAsync(cancellationToken: ct),
                async (c, ct) =>
                {
                    var configuration = c.ConfigurationManager!.Configuration;
                    var (configurationDataProvider, configurationConnection) = configuration.GetConfigurationDataProvider(configurationString);
                    var factory = configuration
                        .GetConfigurationDataProviderFromType(configurationConnection.DataProvider)
                        .GetDbProviderFactory();
                    var connectionString = configurationConnection.ConnectionString;

                    if (!string.IsNullOrEmpty(databaseName))
                    {
                        var builder = NotNullOrThrow(factory.CreateConnectionStringBuilder());
                        builder.ConnectionString = connectionString;
                        builder["Database"] = databaseName;
                        connectionString = builder.ToString();
                    }

                    createDbManagerFuncClosure[0] = () => new DbManager(configurationDataProvider.GetDataProvider(connectionString), connectionString);

                    var dbScope = c.Container.Resolve<IDbScope>();

                    var databaseSchemeService = new DatabaseSchemeService(
                        dbScope,
                        new ServerConfigurationVersionProvider(dbScope));

                    if (!await databaseSchemeService.IsStorageExistsAsync(ct))
                    {
                        await logger.ErrorAsync("Scheme doesn't exists in the database, can't continue");
                        return -1;
                    }

                    if (!await databaseSchemeService.IsStorageUpToDateAsync(ct))
                    {
                        if (!updateSchemeInDatabase)
                        {
                            await logger.ErrorAsync("Scheme isn't up-to-date in the database, can't continue");
                            return -1;
                        }

                        await logger.InfoAsync("Scheme isn't up-to-date in the database, upgrading it...");
                        await databaseSchemeService.UpdateStorageAsync(ct);
                    }

                    SchemeDatabase tessaDatabase = new(SchemeDatabaseNames.Original);
                    await tessaDatabase.RefreshAsync(databaseSchemeService, ct);

                    string exportPath = DefaultConsoleHelper.NormalizeFolderAndCreateIfNotExists(outputFolder);
                    if (string.IsNullOrEmpty(exportPath))
                    {
                        exportPath = Directory.GetCurrentDirectory();
                    }

                    string tsdFilePath = Directory.EnumerateFiles(exportPath, "*.tsd").MinBy(x => x)
                        ?? Path.Combine(exportPath, "Platform.tsd");

                    await logger.InfoAsync("Reading scheme from file \"{0}\"", tsdFilePath);

                    string[] partitions = FileSchemeService.GetPartitionPaths(tsdFilePath);
                    var fileSchemeService = new FileSchemeService(tsdFilePath, partitions);

                    if (!await fileSchemeService.IsStorageExistsAsync(ct))
                    {
                        await logger.InfoAsync("Scheme doesn't exist in the file folder, creating it...");
                        await fileSchemeService.CreateStorageAsync(ct);
                    }

                    if (!await fileSchemeService.IsStorageUpToDateAsync(ct))
                    {
                        await logger.InfoAsync("Scheme isn't up-to-date in the file folder, upgrading it...");
                        await fileSchemeService.UpdateStorageAsync(ct);
                    }

                    await logger.InfoAsync("Exporting the scheme using database to folder \"{0}\"", exportPath);

                    var sq = new SchemeSubmittingQueue(tessaDatabase, fileSchemeService);
                    try
                    {
                        await sq.RefreshAsync(ct);
                        await sq.SubmitAsync(ct);
                    }
                    catch (Exception e) when (sq.FaultedItem is null)
                    {
                        await logger.LogExceptionAsync("Error exporting scheme", e);
                        return -1;
                    }

                    if (sq.FaultedItem is { } faultedItem)
                    {
                        var message = SchemeHelper.GetFaultedItemMessage(sq.FaultedItem);
                        if (faultedItem.Exception is { } ex)
                        {
                            await logger.LogExceptionAsync(message, ex);
                        }
                        else
                        {
                            await logger.ErrorAsync(message);
                        }

                        return -1;
                    }

                    await logger.InfoAsync("Scheme has been exported successfully");
                    return 0;
                });
        }
    }
}
