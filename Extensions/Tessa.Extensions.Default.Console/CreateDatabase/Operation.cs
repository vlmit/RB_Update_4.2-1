using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;

namespace Tessa.Extensions.Default.Console.CreateDatabase
{
    public static class Operation
    {
        public static async Task<int> ExecuteAsync(
            IConsoleLogger logger,
            string? configurationString,
            string? databaseName,
            bool dropIfExists,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(configurationString))
            {
                await logger.InfoAsync("Creating database from default configuration string");
            }
            else
            {
                await logger.InfoAsync("Creating database from configuration string \"{0}\"", configurationString);
            }

            await using var companion = new UnityContainerCompanion { UseConfiguration = true };
            return await companion.ProcessAndGetAsync(
                static (c, ct) => c.Container.RegisterDatabaseForConsoleAsync(cancellationToken: ct),
                async (c, ct) =>
                {
                    await DefaultConsoleHelper.DropAndCreateDatabaseAsync(
                        logger,
                        c.ConfigurationManager!,
                        configurationString,
                        databaseName,
                        dropOld: dropIfExists,
                        createNew: true,
                        cancellationToken: ct);

                    await logger.InfoAsync("Database has been created");
                    return 0;
                },
                cancellationToken);
        }
    }
}
