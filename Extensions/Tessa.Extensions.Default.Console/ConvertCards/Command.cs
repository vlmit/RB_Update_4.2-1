using System.IO;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.ConvertCards
{
    public static class Command
    {
        [Verb("ConvertCards")]
        [LocalizableDescription("Common_CLI_ConvertCards")]
        public static async Task ConvertCards(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_SourceConfiguration")] string source,
            [Argument("o")] [LocalizableDescription("Common_CLI_ConvertedConfigurationOutputFolder")] string? target = null,
            [Argument("a")] [LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u")] [LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p")] [LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("nd"), LocalizableDescription("Common_CLI_DoNotDeleteOldConfigurationFilesAfterConvert")] bool doNotDelete = false,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            ThrowIfNull(source);

            if (string.IsNullOrEmpty(target))
            {
                if (source == ".")
                {
                    target = Directory.GetCurrentDirectory();
                }
                else
                {
                    FileAttributes attr = File.GetAttributes(source);
                    bool sourcePathIsDirectory = (attr & FileAttributes.Directory) == FileAttributes.Directory;
                    target = sourcePathIsDirectory ? source : Path.GetDirectoryName(source);
                }
            }

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
                                Source = source,
                                Target = target ?? string.Empty,
                                DoNotDelete = doNotDelete
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
