using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Cards.Repair;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.RepairTypes
{
    public static class Command
    {
        [Verb("RepairTypes")]
        [LocalizableDescription("Common_CLI_RepairTypes")]
        public static async Task RepairTypes(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument][LocalizableDescription("Common_CLI_TypeIDList")] IEnumerable<string>? identifiers = null,
            [Argument("lvl"), LocalizableDescription("Common_CLI_RepairLevel")] TypeRepairLevel repairLevel = TypeRepairLevel.Default,
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

            await using var companion = new UnityContainerCompanion { UseConfiguration = true };
            
            int result = await companion.ProcessAndGetAsync(
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
                            " to repair all card types.");
                        return -1;
                    }

                    List<Guid>? cardTypeIds = null;
                    if (!repairAll)
                    {
                        cardTypeIds = await DefaultConsoleHelper.TryParseIdentifiersListAsync(identifiersList, logger, cancellationToken: ct);

                        if (cardTypeIds is not { Count: > 0 })
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
                            CardTypeIDs = repairAll ? null : cardTypeIds?.ToArray(),
                            RepairLevel = repairLevel
                        }, ct);
                });
            

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
