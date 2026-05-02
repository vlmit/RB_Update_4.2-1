using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Redis;
using Unity;

namespace Tessa.Extensions.Default.Console.SendCommand
{
    public static class Command
    {
        #region Nested Types

        public sealed record CheckAndPrintRedisOperation(
            IConsoleLogger Logger,
            ITessaServerSettings ServerSettings,
            IRedisConnectionProvider RedisConnectionProvider,
            IRedisConnectionStringCleaner RedisConnectionStringCleaner)
        {
            public async Task ExecuteAsync(CancellationToken cancellationToken = default)
            {
                var serverCode = this.ServerSettings.ServerCode;
                var redisConnectionString = this.ServerSettings.RedisConnectionString;

                await this.Logger.InfoAsync("Server code: {0}", serverCode);
                await this.Logger.InfoAsync(
                    "Redis connection: {0}",
                    await this.RedisConnectionStringCleaner.CleanConnectionSafeAsync(redisConnectionString, cancellationToken));

                await this.Logger.InfoAsync("Connecting to Redis...");

                var connection = await this.RedisConnectionProvider.GetOpenedConnectionAsync(cancellationToken);
                connection.GetDatabase();

                await this.Logger.InfoAsync("Connected to Redis");
            }
        }

        #endregion

        [Verb("SendCommand")]
        [LocalizableDescription("Common_CLI_SendCommand")]
        public static async Task SendCommand(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument, LocalizableDescription("Common_CLI_DiscoveryCommand")] string command,
            [Argument("k"), LocalizableDescription("Common_CLI_KeyPath")] string keyPath,
            [Argument("kp"), LocalizableDescription("Common_CLI_KeyPassword")] string? keyPassword = null,
            [Argument("s"), LocalizableDescription("Common_CLI_CommandScopes")] string? scopes = null,
            [Argument("t"), LocalizableDescription("Common_CLI_Targets")] string? targets = null,
            [Argument("timeout"), LocalizableDescription("Common_CLI_CommandTimeout")] double commandTimeoutMinutes = 2,
            [Argument("pp"), LocalizableDescription("Common_CLI_CommandParameters")] IEnumerable<string>? parameters = null,
            [Argument("nowait"), LocalizableDescription("Common_CLI_NoWait")] bool nowait = false,
            [Argument("r")] [LocalizableDescription("Common_CLI_RedisConnectionString")] string? redisConnectionString = null,
            [Argument("sc")] [LocalizableDescription("Common_CLI_ServerCode")] string? serverCode = null,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo"), LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            if (string.IsNullOrWhiteSpace(keyPath))
            {
                throw new ArgumentException("Invalid argument value 'k'. Can't be empty string.");
            }

            if (string.IsNullOrWhiteSpace(keyPassword))
            {
                throw new ArgumentException("Invalid argument value 'kp'. Must be given and can't be empty string.");
            }

            var targetsArray = targets?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var scopesArray = scopes?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            serverCode = serverCode?.Trim();
            redisConnectionString = redisConnectionString?.Trim();

            var arguments = DefaultConsoleHelper.ParseParameters(parameters, StringComparer.OrdinalIgnoreCase);

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
                        var checkOperation = c.Container.Resolve<CheckAndPrintRedisOperation>();
                        await checkOperation.ExecuteAsync(ct);

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
                                Timeout = TimeSpan.FromMinutes(commandTimeoutMinutes)
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
