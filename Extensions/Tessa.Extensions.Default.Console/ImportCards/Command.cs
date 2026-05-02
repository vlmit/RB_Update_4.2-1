using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.ImportCards
{
    public static class Command
    {
        [Verb("ImportCards")]
        [LocalizableDescription("Common_CLI_ImportCards")]
        public static async Task ImportCards(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_SourceCardLibrary")] IEnumerable<string> sources,
            [Argument("a")] [LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u")] [LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p")] [LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("options"), LocalizableDescription("Common_CLI_MergeOptionsPath")] string? mergeOptionsPath = null,
            [Argument("bundled")] [LocalizableDescription("Common_CLI_ImportCards_Bundled")] bool bundled = false,
            [Argument("e")] [LocalizableDescription("Common_CLI_IgnoreExistentCards")] bool ignoreExistentCards = false,
            [Argument("r")] [LocalizableDescription("Common_CLI_IgnoreRepairMessages")] bool ignoreRepairMessages = false,
            [Argument("ignored")] [LocalizableDescription("Common_CLI_IgnoredFileList")] string? ignoredFilesPath = null,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            ThrowIfNull(sources);

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
                                Sources = sources,
                                IgnoreExistentCards = ignoreExistentCards,
                                IgnoreRepairMessages = ignoreRepairMessages,
                                MergeOptionsPath = mergeOptionsPath,
                                IgnoredFilesPath = ignoredFilesPath,
                                Bundled = bundled
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
