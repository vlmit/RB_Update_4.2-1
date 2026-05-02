using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.DeleteCards
{
    public static class Command
    {
        [Verb("DeleteCards")]
        [LocalizableDescription("Common_CLI_DeleteCards")]
        public static async Task DeleteCards(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_CardIdentifiers")] IEnumerable<string>? identifiers = null,
            [Argument("a")] [LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u")] [LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p")] [LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("s")] [LocalizableDescription("Common_CLI_ColumnSeparator")] string? separatorChar = null,
            [Argument("c"), LocalizableDescription("Common_CLI_IgnoreAlreadyDeleted")] bool ignoreAlreadyDeleted = false,
            [Argument("b"), LocalizableDescription("Common_CLI_DeleteWithBackup")] bool withBackup = false,
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
                        if (await DefaultConsoleHelper.TryParseCardInfoListAsync(identifiers, logger, separatorChar, cancellationToken: ct)
                            is not { } cardInfoList)
                        {
                            // ошибка парсинга
                            return -2;
                        }

                        await using var operation = c.Container.Resolve<Operation>();
                        if (!await operation.LoginAsync(userName, password, ct))
                        {
                            return ConsoleAppHelper.FailedLoginExitCode;
                        }

                        return await operation.ExecuteAsync(
                            new()
                            {
                                CardInfoList = cardInfoList,
                                IgnoreAlreadyDeleted = ignoreAlreadyDeleted,
                                DeleteWithBackup = withBackup
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
