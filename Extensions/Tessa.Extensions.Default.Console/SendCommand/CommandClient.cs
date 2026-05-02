using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Discovery;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Tessa.Webbi;
using Unity;
using Unity.Lifetime;

namespace Tessa.Extensions.Default.Console.SendCommand
{
    public static class CommandClient
    {
        [Verb("SendCommandClient")]
        [LocalizableDescription("Common_CLI_SendCommandClient")]
        public static async Task SendCommand(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument, LocalizableDescription("Common_CLI_DiscoveryCommand")] string command,
            [Argument("k"), LocalizableDescription("Common_CLI_KeyPath")] string keyPath,
            [Argument("kp"), LocalizableDescription("Common_CLI_KeyPassword")] string? keyPassword = null,
            [Argument("s"), LocalizableDescription("Common_CLI_CommandScopes")] string? scopes = null,
            [Argument("t"), LocalizableDescription("Common_CLI_Targets")] string? targets = null,
            [Argument("wa"), LocalizableDescription("Common_CLI_MaintenanceAddress")] string? webbiAddress = null,
            [Argument("wm"), LocalizableDescription("Common_CLI_WebbiManagementRoute")] string? webbiManagementRoute = null,
            [Argument("wt"), LocalizableDescription("Common_CLI_MaintenanceTimeout")] double? webbiTimeoutSeconds = null,
            [Argument("timeout"), LocalizableDescription("Common_CLI_CommandTimeout")] double commandTimeoutMinutes = 2.0,
            [Argument("pp"), LocalizableDescription("Common_CLI_CommandParameters")] IEnumerable<string>? parameters = null,
            [Argument("a"), LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u"), LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p"), LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("nowait"), LocalizableDescription("Common_CLI_NoWait")] bool nowait = false,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo"), LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            if (string.IsNullOrWhiteSpace(keyPath))
            {
                throw new ArgumentException("Invalid \"k\" argument value. Can't be empty string.");
            }

            if (string.IsNullOrWhiteSpace(keyPassword))
            {
                throw new ArgumentException("Invalid \"kp\" argument value. Should be provided as non-empty string.");
            }

            var targetsArray = targets?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var scopesArray = scopes?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            int result;
            await using (var companion = new UnityContainerCompanion { UseConfiguration = true })
            {
                result = await companion.ProcessAndGetAsync(
                    async (c, ct) =>
                    {
                        await c.Container
                            .ConfigureConsoleForClientAsync(stdOut, stdErr, quiet, address, cancellationToken: ct);

                        c.Container
                            .RegisterDiscoveryCommon()
                            .RegisterType<IDiscoveryCommandStrategy, ClientDiscoveryCommandStrategy>(new PerResolveLifetimeManager());
                    },
                    async (c, ct) =>
                    {
                        // adjust webbi settings
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

                        var arguments = DefaultConsoleHelper.ParseParameters(parameters, StringComparer.OrdinalIgnoreCase);

                        await using var operation = c.Container.Resolve<Operation>();
                        return await operation.ExecuteAsync(
                            new()
                            {
                                Command = command,
                                Targets = targetsArray,
                                Scopes = scopesArray,
                                Arguments = arguments,
                                KeyPath = keyPath,
                                KeyPassword = keyPassword,
                                Nowait = nowait,
                                Timeout = TimeSpan.FromMinutes(commandTimeoutMinutes),
                                IsClient = true,
                                UserName = userName,
                                Password = password
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
