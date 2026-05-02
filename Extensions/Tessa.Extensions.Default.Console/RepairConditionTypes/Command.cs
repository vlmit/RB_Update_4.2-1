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

namespace Tessa.Extensions.Default.Console.RepairConditionTypes
{
    public static class Command
    {
        [Verb("RepairConditionTypes")]
        [LocalizableDescription("Common_CLI_RepairConditionTypes")]
        public static async Task RepairConditionTypes(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_ConditionTypeIDList")] IEnumerable<string>? identifiers = null,
            [Argument("a")] [LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u")] [LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p")] [LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("all"), LocalizableDescription("Common_CLI_RepairAll")] bool repairAll = false,
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
                        if (repairAll && identifiersList is { Count: > 0 })
                        {
                            // ошибка параметров
                            await logger.ErrorAsync(
                                "Either specify identifiers as command line parameters, or use option " +
                                (OperatingSystem.IsLinux()
                                    ? "-all"
                                    : "/all") +
                                " to repair all condition types.");
                            return -1;
                        }

                        List<Guid>? conditionTypeIds = null;
                        if (!repairAll)
                        {
                            conditionTypeIds = await DefaultConsoleHelper.TryParseIdentifiersListAsync(identifiersList, logger, cancellationToken: ct);

                            if (conditionTypeIds is null)
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
                                ConditionTypeIDs = repairAll ? null : conditionTypeIds?.ToArray()
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
