using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.ExportViews
{
    public static class Command
    {
        [Verb("ExportViews")]
        [LocalizableDescription("Common_CLI_ExportViews")]
        public static async Task ExportViews(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_ViewAliasesOrIdentifiers")] IEnumerable<string>? aliasOrIdentifier = null,
            [Argument("a")] [LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u")] [LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p")] [LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("o")] [LocalizableDescription("Common_CLI_OutputFolder")] string? outputFolder = null,
            [Argument("c")] [LocalizableDescription("Common_CLI_ClearOutputFolder")] bool clearOutputFolder = false,
            [Argument("s")] [LocalizableDescription("Common_CLI_GroupBySubfolders")] bool groupBySubfolders = false,
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
                                ViewAliasesOrIdentifiers = aliasOrIdentifier?.ToList(),
                                OutputFolder = outputFolder,
                                ClearOutputFolder = clearOutputFolder,
                                GroupBySubfolders = groupBySubfolders
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
