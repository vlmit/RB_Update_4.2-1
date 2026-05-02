using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.ManageRoles
{
    public static class Command
    {
        [Verb("ManageRoles")]
        [LocalizableDescription("Common_CLI_ManageRoles")]
        public static async Task ManageRoles(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument, LocalizableDescription("Common_CLI_ManageRoleCommands")] IEnumerable<CommandType>? command = null,
            [Argument("id")] [LocalizableDescription("Common_CLI_RoleIdentifiers")] string? identifiers = null,
            [Argument("srg")] [LocalizableDescription("Common_CLI_SmartRoleGeneratorNames")] IEnumerable<KnownSmartRoleGenerators>? smartRoleGeneratorNames = null,
            [Argument("bulk"), LocalizableDescription("Common_CLI_SyncDeputiesBulkSize")] int bulkSize = 500000,
            [Argument("changed"), LocalizableDescription("Common_CLI_SyncDeputiesSyncChangedOnly")] bool syncChangedOnly = false,
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

                        var commands = command?.ToArray() ?? [];
                        if (commands.Length == 0)
                        {
                            await logger.ErrorAsync("No commands are specified");
                            return -2;
                        }

                        if (smartRoleGeneratorNames?.Select(x => x.ToGuid()).ToArray() is { Length: not 0 } smartRoleGeneratorIds)
                        {
                            if (!commands.Any(x => x is CommandType.RecalcSmartRoleGenerators))
                            {
                                await logger.ErrorAsync($"Smart role generator names usage requires {nameof(CommandType.RecalcSmartRoleGenerators)} command to be specified");
                                return -3;
                            }

                            var strIds = string.Join(' ', smartRoleGeneratorIds);
                            identifiers = string.IsNullOrEmpty(identifiers) ? strIds : $"{strIds} {identifiers}";
                        }

                        var identifierArray = commands.Any(x => x
                            is CommandType.RecalcDynamicRoles or CommandType.RecalcRoleGenerators or CommandType.RecalcSmartRoleGenerators)
                            ? (await DefaultConsoleHelper.TryParseIdentifiersListAsync([identifiers], logger, cancellationToken: ct))?.ToArray() ?? []
                            : [];

                        foreach (var commandType in commands)
                        {
                            switch (commandType)
                            {
                                case CommandType.SyncAllDeputies:
                                case CommandType.RecalcAllDynamicRoles:
                                case CommandType.RecalcAllRoleGenerators:
                                case CommandType.RecalcAllSmartRoleGenerators:
                                    // дополнительные проверки отсутствуют
                                    break;

                                case CommandType.RecalcDynamicRoles:
                                case CommandType.RecalcRoleGenerators:
                                case CommandType.RecalcSmartRoleGenerators:
                                    if (identifierArray.Length == 0)
                                    {
                                        await logger.ErrorAsync("No identifiers or names are specified for command {0}", commandType);
                                        return -3;
                                    }

                                    break;

                                default:
                                    throw ArgumentOutOfRange(commandType);
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
                                Commands = commands,
                                Identifiers = identifierArray,
                                BulkSize = bulkSize,
                                SyncChangedOnly = syncChangedOnly
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
