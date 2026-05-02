using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.UpgradeWorkflowProcesses
{
    public static class Command
    {
        [Verb("UpgradeWorkflowProcesses")]
        [LocalizableDescription("Common_CLI_UpgradeWorkflowProcesses")]
        public static async Task UpgradeWorkflowProcesses(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument, LocalizableDescription("Common_CLI_TemplateIDList")] IEnumerable<string>? identifiers = null,
            [Argument("mode")] [LocalizableDescription("Common_CLI_UpgradeWorkflowProcesses_Mode")] Mode mode = Mode.All,
            [Argument("all")] [LocalizableDescription("Common_CLI_UpgradeAll")] bool upgradeAll = false,
            [Argument("a")] [LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u")] [LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p")] [LocalizableDescription("Common_CLI_Password")] string? password = null,
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
                        var logger = c.Container.Resolve<IConsoleLogger>();

                        var identifiersList = identifiers?.ToList();
                        if (upgradeAll && identifiersList is { Count: > 0 })
                        {
                            // ошибка параметров
                            await logger.ErrorAsync(
                                "Either specify identifiers with " +
                                (OperatingSystem.IsLinux()
                                    ? "-id"
                                    : "/id") +
                                " parameter, or use option " +
                                (OperatingSystem.IsLinux()
                                    ? "-all"
                                    : "/all") +
                                " to upgrade all process versions.");
                            return -1;
                        }

                        List<Guid>? ids = null;
                        if (!upgradeAll)
                        {
                            ids = await DefaultConsoleHelper.TryParseIdentifiersListAsync(identifiersList, logger, cancellationToken: ct);

                            if (ids is null)
                            {
                                // ошибка парсинга
                                return -2;
                            }
                        }

                        await using var operation = c.Container.Resolve<Operation>();
                        if (!await operation.LoginAsync(userName, password, ct))
                        {
                            return ConsoleAppHelper.FailedLoginExitCode;
                        }

                        return await operation.ExecuteAsync(
                            new()
                            {
                                Identifiers = upgradeAll ? null : ids?.ToArray(),
                                Mode = mode
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
