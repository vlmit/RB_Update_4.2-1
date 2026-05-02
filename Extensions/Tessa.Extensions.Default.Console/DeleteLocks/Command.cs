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

namespace Tessa.Extensions.Default.Console.DeleteLocks
{
    public static class Command
    {
        [Verb("DeleteLocks")]
        [LocalizableDescription("Common_CLI_DeleteLocks")]
        public static async Task DeleteLocks(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_LockGroups")] IEnumerable<string>? lockGroup = null,
            [Argument("id")] [LocalizableDescription("Common_CLI_LockIDs")] IEnumerable<string>? lockID = null,
            [Argument("r")] [LocalizableDescription("Common_CLI_RedisConnectionString")] string? redisConnectionString = null,
            [Argument("sc")] [LocalizableDescription("Common_CLI_ServerCode")] string? serverCode = null,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            var lockGroupValues = lockGroup?
                    .Select(x => x.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    .SelectMany(x => x)
                    .Select(x => x.Trim())
                    .ToArray()
                ?? Array.Empty<string>();

            // ReSharper disable PossibleMultipleEnumeration
            var outLockIDs = lockID?.ToArray() ?? Array.Empty<string>();

            ThrowIf(lockID, lockGroupValues.Length != 1 && outLockIDs.Length > 0, "List of identifiers can only be specified for single lock group.");
            // ReSharper restore PossibleMultipleEnumeration

            serverCode = serverCode?.Trim();
            redisConnectionString = redisConnectionString?.Trim();

            int result;
            await using (var companion = new UnityContainerCompanion { UseConfiguration = true })
            {
                result = await companion.ProcessAndGetAsync(
                    (c, ct) => c.Container
                        .RegisterConsoleOperationLogger(stdOut, stdErr, quiet)
                        .RegisterSingleton<IConsoleSessionManager, FakeConsoleSessionManager>()
                        .RegisterServerForConsoleAsync(
                            modifyServerSettingsAction: ConsoleAppHelper.GetModifyServerSettingsAction(serverCode, redisConnectionString),
                            cancellationToken: ct),
                    async (c, ct) =>
                    {
                        await using var operation = c.Container.Resolve<Operation>();
                        return await operation.ExecuteAsync(
                            new()
                            {
                                LockGroups = lockGroupValues,
                                LockIDs = outLockIDs
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
