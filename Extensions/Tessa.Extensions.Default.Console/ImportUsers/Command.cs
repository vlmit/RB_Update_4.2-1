using System.IO;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Data;
using Unity;
using Unity.Resolution;

namespace Tessa.Extensions.Default.Console.ImportUsers
{
    public static class Command
    {
        [Verb("ImportUsers")]
        [LocalizableDescription("Common_CLI_ImportUsers")]
        public static async Task ImportUsers(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_SourceUserFile")] string pathToUserFile,
            [Argument("sd")] [LocalizableDescription("Common_CLI_SourceDepartmentFile")] string? pathToDepartmentFile = null,
            [Argument("a")] [LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u")] [LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p")] [LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("cs"), LocalizableDescription("Common_CLI_ConfigurationString")] string? configurationString = null,
            [Argument("db"), LocalizableDescription("Common_CLI_DatabaseName")] string? databaseName = null,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            ThrowIfNull(pathToUserFile);

            var fileExt = Path.GetExtension(pathToUserFile);
            ImportType importType = fileExt.ToLowerInvariant() switch
            {
                ".xlsx" => ImportType.Excel,
                ".csv" => ImportType.Csv,
                _ => throw ArgumentOutOfRange(fileExt, "Only xlsx or csv files are supported", pathToUserFile)
            };

            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            int result;
            await using (var companion = new UnityContainerCompanion { UseConfiguration = true })
            {
                result = await companion.ProcessAndGetAsync(
                    (c, ct) => c.Container.ConfigureConsoleForClientAsync(stdOut, stdErr, quiet, address, cancellationToken: ct),
                    async (c, ct) =>
                    {
                        await using var databaseCompanion = new UnityContainerCompanion();
                        await databaseCompanion.Container.RegisterDatabaseForConsoleAsync(c.ConfigurationManager, ct);

                        await using var operation = c.Container.Resolve<Operation>(
                            new DependencyOverride<IDbConnectionStringCleaner>(
                                databaseCompanion.Container.TryResolve<IDbConnectionStringCleaner>()));

                        if (!await operation.LoginAsync(userName, password, ct))
                        {
                            return ConsoleAppHelper.FailedLoginExitCode;
                        }

                        return await operation.ExecuteAsync(
                            new()
                            {
                                PathToUserFile = pathToUserFile,
                                PathToDepartmentFile = pathToDepartmentFile,
                                ImportType = importType,
                                ConfigurationString = configurationString,
                                DatabaseName = databaseName
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
