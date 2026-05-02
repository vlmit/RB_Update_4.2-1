using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Web;
using Unity;

namespace Tessa.Extensions.Default.Console.CheckService
{
    public static class Command
    {
        [Verb("CheckService")]
        [LocalizableDescription("Common_CLI_CheckService")]
        public static async Task CheckService(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument("a")] [LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u")] [LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p")] [LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("timeout")] [LocalizableDescription("Common_CLI_ConnectTimeout")] int seconds = 0,
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
                        // отрицательное значение seconds использует таймаут по умолчанию в соответствии с конфигурационным файлом
                        if (seconds >= 0)
                        {
                            TimeSpan timeoutSpan = seconds == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(seconds);

                            var connectionSettings = c.Container.Resolve<IConnectionSettings>();
                            connectionSettings.Timeout = timeoutSpan;
                        }

                        await using var operation = c.Container.Resolve<Operation>();
                        if (!await operation.LoginAsync(userName, password, ct))
                        {
                            return ConsoleAppHelper.FailedLoginExitCode;
                        }

                        return await operation.ExecuteAsync(ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
