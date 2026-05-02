using System;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Scheme;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Console.SchemeUpdateSql
{
    public static class Operation
    {
        public static async Task<int> ExecuteAsync(
            IConsoleLogger logger,
            string? configurationString,
            string? databaseName)
        {
            await logger.InfoAsync("Updating the scheme to current version from database");

            await logger.InfoAsync(
                string.IsNullOrEmpty(configurationString)
                    ? "Connection will be opened to default database"
                    : "Connection will be opened to database with connection \"{0}\"",
                configurationString);

            if (!string.IsNullOrEmpty(databaseName))
            {
                await logger.InfoAsync("Changes will be applied to database \"{0}\"", databaseName);
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
                        await logger.ErrorAsync("Scheme doesn't exists in the database");
                        return -1;
                    }

                    if (await databaseSchemeService.IsStorageUpToDateAsync(ct))
                    {
                        await logger.InfoAsync("Scheme is up-to-date, skipping update");
                    }
                    else
                    {
                        await logger.InfoAsync("Scheme isn't up-to-date in the database, updating it...");
                        await databaseSchemeService.UpdateStorageAsync(ct);
                        await logger.InfoAsync("Scheme has been successfully updated");
                    }

                    return 0;
                });
        }
    }
}
