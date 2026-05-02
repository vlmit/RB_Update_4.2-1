using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.ExportSearchQueries
{
    public static class Command
    {
        [Verb("ExportSearchQueries")]
        [LocalizableDescription("Common_CLI_ExportSearchQueries")]
        public static async Task ExportSearchQueries(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_SearchQueriesNamesOrIdentifiers")] IEnumerable<string>? nameOrIdentifier = null,
            [Argument("a")] [LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u")] [LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p")] [LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("o")] [LocalizableDescription("Common_CLI_OutputFolder")] string? outputFolder = null,
            [Argument("c")] [LocalizableDescription("Common_CLI_ClearOutputFolder")] bool clearOutputFolder = false,
            [Argument("public")] [LocalizableDescription("Common_CLI_ExportPublicSearchQueriesOnly")] bool publicOnly = false,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
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
                        await using var operation = c.Container.Resolve<Operation>();
                        if (!await operation.LoginAsync(userName, password, ct))
                        {
                            return ConsoleAppHelper.FailedLoginExitCode;
                        }

                        return await operation.ExecuteAsync(
                            new()
                            {
                                SearchQueryNamesOrIdentifiers = nameOrIdentifier?.ToList(),
                                OutputFolder = outputFolder,
                                ClearOutputFolder = clearOutputFolder,
                                PublicQueriesOnly = publicOnly
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
