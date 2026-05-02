using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Data;

namespace Tessa.Extensions.Default.Console.CheckDatabase
{
    public static class Operation
    {
        public static async Task<int> ExecuteAsync(
            IConsoleLogger logger,
            string? configurationString,
            string? databaseName,
            int timeoutSeconds,
            bool outputDbms,
            bool outputConnectionString,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(configurationString))
            {
                await logger.InfoAsync("Checking database connection using default configuration string");
            }
            else
            {
                await logger.InfoAsync("Checking database connection using configuration string \"{0}\"", configurationString);
            }

            if (databaseName is null)
            {
                await logger.InfoAsync("Changes will be applied to master database");
            }
            else if (databaseName.Length == 0)
            {
                await logger.InfoAsync("Changes will be applied to database in configuration string");
            }
            else
            {
                await logger.InfoAsync("Changes will be applied to database \"{0}\"", databaseName);
            }

            if (timeoutSeconds < 0)
            {
                await logger.InfoAsync("Connecting with timeout specified in the connection string");
            }
            else
            {
                await logger.InfoAsync("Connecting with timeout {0} second(s)", timeoutSeconds);
            }

            Dbms dbms;
            await using (var companion = new UnityContainerCompanion { UseConfiguration = true })
            {
                dbms = await companion.ProcessAndGetAsync(
                    static (c, ct) => c.Container.RegisterDatabaseForConsoleAsync(cancellationToken: ct),
                    async (c, ct) =>
                        await DefaultConsoleHelper.CheckConnectionAsync(
                            logger,
                            c.ConfigurationManager!,
                            configurationString,
                            timeoutSeconds,
                            outputConnectionString,
                            databaseName,
                            keepOriginalDatabase: databaseName?.Length == 0,
                            connectionStringCleaner: c.Container.TryResolve<IDbConnectionStringCleaner>(),
                            cancellationToken: ct),
                    cancellationToken);
            }

            if (outputDbms)
            {
                switch (dbms)
                {
                    case Dbms.SqlServer:
                        await logger.WriteLineAsync("ms");
                        break;

                    case Dbms.PostgreSql:
                        await logger.WriteLineAsync("pg");
                        break;
                }
            }

            await logger.InfoAsync("Database connection check is successful");
            return 0;
        }
    }
}
