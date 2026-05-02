using System;
using System.Collections.Generic;
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

namespace Tessa.Extensions.Default.Console.ImportSchemeSql
{
    public static class Operation
    {
        public static async Task<int> ExecuteAsync(
            IConsoleLogger logger,
            string? source,
            string? configurationString,
            string? databaseName,
            IEnumerable<string>? includedPartitions,
            IEnumerable<string>? excludedPartitions)
        {
            ThrowIfNull(logger);

            string? filePath = DefaultConsoleHelper.GetSourceFiles(source, "*.tsd").FirstOrDefault();
            if (filePath is null)
            {
                await logger.ErrorAsync("Can't find database file *.tsd in \"{0}\"", source);
                return -2;
            }

            await logger.InfoAsync("Importing the scheme using sql connection");

            await logger.InfoAsync(
                string.IsNullOrEmpty(configurationString)
                    ? "Connection will be opened to default database"
                    : "Connection will be opened to database with connection \"{0}\"",
                configurationString);

            if (!string.IsNullOrEmpty(databaseName))
            {
                await logger.InfoAsync("Changes will be applied to database \"{0}\"", databaseName);
            }

            string fileFullPath = Path.GetFullPath(filePath);
            await logger.InfoAsync("Reading scheme from: \"{0}\"", fileFullPath);

            var fileSchemeService = new FileSchemeService(
                fileFullPath,
                DefaultConsoleHelper.GetSchemePartitions(fileFullPath, includedPartitions, excludedPartitions));

            foreach (string partitionFileName in fileSchemeService.PartitionFileNames)
            {
                await logger.InfoAsync("Partition: \"{0}\"", partitionFileName);
            }

            if (!await fileSchemeService.IsStorageUpToDateAsync())
            {
                await logger.InfoAsync("Scheme isn't up-to-date in the file folder, upgrading it...");
                await fileSchemeService.UpdateStorageAsync();
            }

            SchemeDatabase tessaDatabase = new(SchemeDatabaseNames.Original);
            await tessaDatabase.RefreshAsync(fileSchemeService);

            await logger.InfoAsync("Importing the scheme");

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
                        await logger.InfoAsync("Scheme doesn't exists in the database, creating it...");
                        await databaseSchemeService.CreateStorageAsync(ct);
                    }

                    if (!await databaseSchemeService.IsStorageUpToDateAsync(ct))
                    {
                        await logger.InfoAsync("Scheme isn't up-to-date in the database, upgrading it...");
                        await databaseSchemeService.UpdateStorageAsync(ct);
                    }

                    var sq = new SchemeSubmittingQueue(tessaDatabase, databaseSchemeService);
                    try
                    {
                        await sq.RefreshAsync(ct);
                        await sq.SubmitAsync(ct);
                    }
                    catch (Exception e) when (sq.FaultedItem is null)
                    {
                        await logger.LogExceptionAsync("Error importing scheme", e);
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

                    await logger.InfoAsync("Scheme has been imported successfully");
                    return 0;
                });
        }
    }
}
