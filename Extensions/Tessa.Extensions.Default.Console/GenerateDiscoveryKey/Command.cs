using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.GenerateDiscoveryKey
{
    public static class Command
    {
        [Verb("GenerateDiscoveryKey")]
        [LocalizableDescription("Common_CLI_GenerateDiscoveryKey")]
        public static async Task GenerateCommandKey(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument, LocalizableDescription("Common_CLI_KeyScopes")] IEnumerable<string> scopes,
            [Argument("p"), LocalizableDescription("Common_CLI_KeyPassword")] string password,
            [Argument("s"), LocalizableDescription("Common_CLI_Subject")] string subject,
            [Argument("k"), LocalizableDescription("Common_CLI_Parent")] string? parent = null,
            [Argument("kp"), LocalizableDescription("Common_CLI_ParentPassword")] string? parentPassword = null,
            [Argument("o"), LocalizableDescription("Common_CLI_GenerateDiscoveryKeyOutput")] string? output = null,
            [Argument("em"), LocalizableDescription("Common_CLI_ExpirationMonths")] int expirationMonths = 3,
            [Argument("self"), LocalizableDescription("Common_CLI_SelfSigned")] bool selfSigned = false,
            [Argument("mode"), LocalizableDescription("Common_CLI_GenerateDiscoveryKeyMode")] Mode mode = Mode.Generate,
            [Argument("r")] [LocalizableDescription("Common_CLI_RedisConnectionString")] string? redisConnectionString = null,
            [Argument("sc")] [LocalizableDescription("Common_CLI_ServerCode")] string? serverCode = null,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo"), LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            ThrowIfNullOrEmpty(password);
            ThrowIf(password, password.Length < 4, "Password has to be at least 4 characters long");

            // TODO: should throw if expirationMonths > signingKey.ExpiredAt
            ThrowIf(expirationMonths, expirationMonths <= 0, "Expiration months has to be a positive number");
            ThrowIfNullOrEmpty(subject);

            var requestedScopes = scopes.ToArray();
            ThrowIf(requestedScopes, requestedScopes.Length == 0, "Scopes can't be empty", nameof(scopes));

            if (!selfSigned)
            {
                ThrowIfNullOrWhiteSpace(parent);
                ThrowIfNullOrWhiteSpace(parentPassword);
                // TODO: check signingKey format or something else?
                // TODO: check signingPassword format or length?
            }

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
                            new(requestedScopes, password, subject, stdOut)
                            {
                                Parent = parent,
                                ParentPassword = parentPassword,
                                Output = output,
                                ExpirationMonths = expirationMonths,
                                SelfSigned = selfSigned,
                                Mode = mode
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
