using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Discovery;
using Tessa.Discovery.Senders;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Web;
using Tessa.Webbi;
using Unity;
using Unity.Injection;

namespace Tessa.Extensions.Default.Console.PrintDiscoveryInfo
{
    public static class CommandClient
    {
        [Verb("PrintDiscoveryInfoClient")]
        [LocalizableDescription("Common_CLI_PrintDiscoveryInfoClient")]
        public static async Task PrintComponents(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument("k"), LocalizableDescription("Common_CLI_KeyPath")] string keyPath,
            [Argument("kp"), LocalizableDescription("Common_CLI_KeyPassword")] string keyPassword,
            [Argument("wa"), LocalizableDescription("Common_CLI_MaintenanceAddress")] string? webbiAddress = null,
            [Argument("wm"), LocalizableDescription("Common_CLI_WebbiManagementRoute")] string? webbiManagementRoute = null,
            [Argument("wt"), LocalizableDescription("Common_CLI_MaintenanceTimeout")] double? webbiTimeoutSeconds = null,
            [Argument("e"), LocalizableDescription("Common_CLI_PrintDiscoveryInfoExport")] bool export = false,
            [Argument("i"), LocalizableDescription("Common_CLI_PrintInfo")] bool printInfo = false,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo"), LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            int result;
            await using (var companion = new UnityContainerCompanion { UseConfiguration = true })
            {
                result = await companion.ProcessAndGetAsync(
                    (c, ct) => c.Container
                        .RegisterConsoleOperationLogger(stdOut, stdErr, quiet)
                        .RegisterSingleton<IConsoleSessionManager, FakeConsoleSessionManager>()
                        .RegisterDiscoveryCommon()
                        .RegisterServerForConsoleAsync(cancellationToken: ct),
                    async (c, ct) =>
                    {
                        var logger = c.Container.Resolve<IConsoleLogger>();

                        if (string.IsNullOrWhiteSpace(keyPath))
                        {
                            await logger.ErrorAsync("Invalid \"k\" argument value. Can't be empty string.");
                            return -1;
                        }

                        if (string.IsNullOrWhiteSpace(keyPassword))
                        {
                            await logger.ErrorAsync("Invalid \"kp\" argument value. Should be provided as non-empty string.");
                            return -2;
                        }

                        var keySerializer = c.Container.Resolve<IDiscoveryKeySerializer>();
                        var signingKey = await DiscoverySenderHelper.LoadKeyAsync(keySerializer, keyPath, keyPassword, ct);
                        ThrowIfNull(signingKey);

                        c.Container
                            .RegisterSingleton<IComponentsProvider, WebbiComponentsProvider>(
                                new InjectionConstructor(
                                    typeof(IWebbiConnectionSettings),
                                    typeof(IWebProxyFactory),
                                    typeof(IConsoleLogger),
                                    signingKey));

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

                        var operation = c.Container.Resolve<Operation>();
                        return await operation.ExecuteAsync(
                            new(stdOut)
                            {
                                Export = export,
                                PrintInfo = printInfo
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
