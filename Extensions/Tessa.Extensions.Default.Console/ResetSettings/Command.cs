using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.ResetSettings
{
    public static class Command
    {
        [Verb("ResetSettings")]
        [LocalizableDescription("Common_CLI_ResetSettings")]
        public static async Task ResetSettings(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument, LocalizableDescription("Common_CLI_SettingNames")] IEnumerable<string>? names = null,
            [Argument("c"), LocalizableDescription("Common_CLI_SettingCategories")] IEnumerable<string>? categories = null,
            [Argument("a"), LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u"), LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p"), LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo"), LocalizableDescription("CLI_NoLogo")] bool nologo = false)
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

                        var context = new OperationContext(names, categories);
                        return await operation.ExecuteAsync(context, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
