using System.IO;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.PrintDiscoveryInfo
{
    public static class Command
    {
        [Verb("PrintDiscoveryInfo")]
        [LocalizableDescription("Common_CLI_PrintDiscoveryInfo")]
        public static async Task PrintComponents(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument("e"), LocalizableDescription("Common_CLI_PrintDiscoveryInfoExport")] bool export = false,
            [Argument("i"), LocalizableDescription("Common_CLI_PrintInfo")] bool printInfo = false,
            [Argument("r")] [LocalizableDescription("Common_CLI_RedisConnectionString")] string? redisConnectionString = null,
            [Argument("sc")] [LocalizableDescription("Common_CLI_ServerCode")] string? serverCode = null,
            [Argument("q")] [LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo"), LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            serverCode = serverCode?.Trim();
            redisConnectionString = redisConnectionString?.Trim();

            int result;
            await using (var companion = new UnityContainerCompanion { UseConfiguration = true })
            {
                result = await companion.ProcessAndGetAsync(
                    (c, ct) => c.Container
                        .RegisterConsoleOperationLogger(stdOut, stdErr, quiet)
                        .RegisterSingleton<IComponentsProvider, RedisComponentsProvider>()
                        .RegisterServerForConsoleAsync(
                            modifyServerSettingsAction: ConsoleAppHelper.GetModifyServerSettingsAction(serverCode, redisConnectionString),
                            cancellationToken: ct),
                    async (c, ct) =>
                    {
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
