using System;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Runtime;
using Tessa.Platform.Web;
using Unity;

namespace Tessa.Extensions.Default.Console.CheckCommand
{
    public static class Operation
    {
        public static async Task<int> ExecuteAsync(
            IConsoleLogger logger,
            bool checkHealth,
            string? address,
            int seconds,
            bool quiet)
        {
            if (address?.Length > 0)
            {
                address = address.Trim();

                if (address.Length > 0)
                {
                    if (address.EndsWith('/'))
                    {
                        address = address[..^1];
                    }

                    if (!address.Contains("://", StringComparison.Ordinal))
                    {
                        address = $"https://{address}";
                    }
                }
                else
                {
                    // используем значение из конфига
                    address = null;
                }
            }

            string? result;
            ConnectionSettings[] connectionSettings = [null!];

            try
            {
                await using var companion = new UnityContainerCompanion { UseConfiguration = true };
                result = await companion.ProcessAndGetAsync<string?>(
                    (c, ct) =>
                    {
                        c.Container
                            .RegisterPlatformSharedDependencies()
                            .RegisterFactory<IConnectionSettings>(_ => connectionSettings[0])
                            .RegisterWeb()
                            .RegisterWebDefaultHandlers();

                        if (checkHealth)
                        {
                            c.Container
                                .RegisterSingleton<IWebProxyErrorHandler, HealthCheckWebProxyEventHandler>(nameof(HealthCheckWebProxyEventHandler));
                        }

                        return ValueTask.CompletedTask;
                    },
                    async (c, ct) =>
                    {
                        var settings = ConnectionSettings.ParseFromConfigurationSettings(c.ConfigurationManager!.Configuration.Settings, address);
                        connectionSettings[0] = settings;

                        // отрицательное значение seconds использует таймаут по умолчанию в соответствии с конфигурационным файлом
                        if (seconds >= 0)
                        {
                            settings.Timeout = seconds == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(seconds);
                        }

                        await logger.InfoAsync("Retrieving {0} data from {1}", checkHealth ? "health" : "check", settings.BaseAddress);

                        var proxies = c.Container.Resolve<IWebProxyFactory>();
                        await using var proxy = await proxies.UseProxyAsync<CheckWebProxy>(cancellationToken: ct);

                        return await proxy.GetAsync(checkHealth, ct);
                    });
            }
            catch (HealthCheckResultException ex)
            {
                await logger.WriteAsync(ex.Message);
                return -2;
            }
            catch (Exception ex)
            {
                await logger.LogExceptionAsync($"Failed to retrieve {(checkHealth ? "health" : "check")} data from {connectionSettings[0].BaseAddress}", ex);
                return -1;
            }

            await logger.WriteAsync(result);
            return 0;
        }
    }
}
