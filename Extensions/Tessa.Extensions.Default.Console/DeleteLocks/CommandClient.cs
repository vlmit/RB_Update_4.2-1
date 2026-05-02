using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Discovery;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Tessa.Webbi;
using Unity;

namespace Tessa.Extensions.Default.Console.DeleteLocks
{
    public static class CommandClient
    {
        [Verb("DeleteLocksClient")]
        [LocalizableDescription("Common_CLI_DeleteLocksClient")]
        public static async Task DeleteLocks(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_LockGroups")] IEnumerable<string>? lockGroup = null,
            [Argument("k"), LocalizableDescription("Common_CLI_KeyPath")] string? keyPath = null,
            [Argument("kp"), LocalizableDescription("Common_CLI_KeyPassword")] string? keyPassword = null,
            [Argument("id")] [LocalizableDescription("Common_CLI_LockIDs")] IEnumerable<string>? lockID = null,
            [Argument("wa"), LocalizableDescription("Common_CLI_MaintenanceAddress")] string? webbiAddress = null,
            [Argument("wm"), LocalizableDescription("Common_CLI_WebbiManagementRoute")] string? webbiManagementRoute = null,
            [Argument("wt"), LocalizableDescription("Common_CLI_MaintenanceTimeout")] double? webbiTimeoutSeconds = null,
            [Argument("sc")] [LocalizableDescription("Common_CLI_ServerCode")] string? serverCode = null,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo"), LocalizableDescription("CLI_NoLogo")] bool nologo = false)
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

            var outLockIDs = lockID?.ToArray() ?? Array.Empty<string>();
            
            ThrowIf(lockID, lockGroupValues.Length != 1 && outLockIDs.Length > 0, "List of identifiers can only be specified for single lock group.");
            
            serverCode = serverCode?.Trim();
            
            int result;
            await using (var companion = new UnityContainerCompanion { UseConfiguration = true })
            {
                result = await companion.ProcessAndGetAsync(
                    (c, ct) => c.Container
                        .RegisterConsoleOperationLogger(stdOut, stdErr, quiet)
                        .RegisterSingleton<IConsoleSessionManager, FakeConsoleSessionManager>()
                        .RegisterDiscoveryCommon()
                        .RegisterServerForConsoleAsync(
                            modifyServerSettingsAction: ConsoleAppHelper.GetModifyServerSettingsAction(serverCode),
                            cancellationToken: ct),
                    async (c, ct) =>
                    {
                        var logger = c.Container.Resolve<IConsoleLogger>();

                        if (string.IsNullOrWhiteSpace(keyPath))
                        {
                            await logger.ErrorAsync("Invalid \"k\" argument value. Can't be empty string.");
                            return -4;
                        }

                        if (string.IsNullOrWhiteSpace(keyPassword))
                        {
                            await logger.ErrorAsync("Invalid \"kp\" argument value. Should be provided as non-empty string.");
                            return -5;
                        }
                        
                        var webbiSettings = c.Container.Resolve<IWebbiConnectionSettings>();
                        if (!string.IsNullOrWhiteSpace(webbiAddress))
                        {
                            webbiSettings.BaseAddress = webbiAddress;
                        }

                        if (!string.IsNullOrWhiteSpace(webbiManagementRoute))
                        {
                            webbiSettings.ManagementRoute = webbiManagementRoute;
                        }

                        if (webbiTimeoutSeconds.HasValue)
                        {
                            webbiSettings.Timeout = webbiTimeoutSeconds.Value == 0
                                ? Timeout.InfiniteTimeSpan
                                : TimeSpan.FromSeconds(webbiTimeoutSeconds.Value);
                        }

                        await using var operation = c.Container.Resolve<OperationClient>();
                        return await operation.ExecuteAsync(
                            new()
                            {
                                LockGroups = lockGroupValues,
                                LockIDs = outLockIDs,
                                KeyPath = keyPath,
                                KeyPassword = keyPassword,
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
