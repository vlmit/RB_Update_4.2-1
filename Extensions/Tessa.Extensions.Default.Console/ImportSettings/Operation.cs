using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.IO;
using Tessa.Platform.Web;
using Tessa.SettingsUnits;

namespace Tessa.Extensions.Default.Console.ImportSettings
{
    public sealed class Operation(
        IConsoleLogger logger,
        IConsoleSessionManager sessionManager,
        IWebProxyFactory webProxyFactory)
        : ConsoleOperation<OperationContext>(logger, sessionManager)
    {
        #region Fields

        private readonly IWebProxyFactory webProxyFactory = NotNullOrThrow(webProxyFactory);

        #endregion

        #region Base Overrides

        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            try
            {
                var statistics = new Statistics();
                await this.Logger.InfoAsync("Importing settings units from \"{0}\"", context.Source);

                if (DefaultConsoleHelper.GetSourceFiles(context.Source, "*.jsettings", throwIfNotFound: false) is { Count: > 0 } files)
                {
                    await this.Logger.InfoAsync("Found settings units ({0})", files.Count);

                    await using var proxy = await this.webProxyFactory.UseProxyAsync<SettingsUnitEditorWebProxy>(cancellationToken: cancellationToken);

                    foreach (var filePath in files)
                    {
                        await this.ImportAsync(proxy, filePath, statistics, cancellationToken);
                    }
                }
                else
                {
                    await this.Logger.InfoAsync("Not found settings units in \"{0}\"", context.Source);
                }

                await statistics.LogSummaryAsync(this.Logger);
                return statistics.Error > 0 ? -2 : 0;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.Logger.LogExceptionAsync("Error importing settings", ex);
                return -1;
            }
        }

        #endregion

        #region Private Methods

        private async Task ImportAsync(SettingsUnitEditorWebProxy proxy, string filePath, Statistics statistics, CancellationToken cancellationToken)
        {
            try
            {
                await this.Logger.InfoAsync("Importing settings unit from \"{0}\"", filePath);

                var fileName = FileHelper.GetFileNameWithoutExtension(filePath);
                var settingNames = fileName.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var unitName = settingNames.ElementAtOrDefault(0);
                var fragmentName = settingNames.ElementAtOrDefault(1);

                await using var stream = FileHelper.OpenRead(filePath, bufferSize: FileHelper.DefaultFileBufferSize);

                await proxy.ImportUnitAsync(stream, unitName!, fragmentName, cancellationToken);
                statistics.RegisterSuccess();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                statistics.RegisterError();
                await this.Logger.LogExceptionAsync($"Failed to import settings unit from \"{filePath}\"", ex);
            }
        }

        #endregion
    }
}
