using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LinqToDB.Common;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.RecalcAcl
{
    public static class Command
    {
        [Verb("RecalcAcl")]
        [LocalizableDescription("Common_CLI_RecalcAcl")]
        public static async Task RecalcAcl(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument("ids")] [LocalizableDescription("Common_CLI_RecalcAclIDs")] IEnumerable<string>? ids = null,
            [Argument("all")] [LocalizableDescription("Common_CLI_RecalcAclAll")] bool all = false,
            [Argument("cards")] [LocalizableDescription("Common_CLI_RecalcAclCards")] IEnumerable<Guid>? cards = null,
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

                        var idsArray = ids?.AsArray();

                        if (all && idsArray is { Length: > 0 })
                        {
                            var allFlag = OperatingSystem.IsLinux()
                                ? "-all"
                                : "/all";

                            await logger.ErrorAsync(
                                $"Either provide a list of specific identifiers or use the \"{allFlag}\" option to recalculate all rules. These parameters cannot be used simultaneously.");
                            return -1;
                        }

                        List<Guid>? idsResult = null;
                        if (!all)
                        {
                            idsResult = await DefaultConsoleHelper.TryParseIdentifiersListAsync(idsArray, logger, cancellationToken: ct);
                            if (idsResult is not { Count: not 0 })
                            {
                                await logger.ErrorAsync("No ACL rules are defined to recalculate.");
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
                                Ids = idsResult?.ToArray(),
                                All = all,
                                Cards = cards?.AsArray(),
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
