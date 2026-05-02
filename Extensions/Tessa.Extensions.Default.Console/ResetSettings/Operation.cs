using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Web;
using Tessa.SettingsUnits;

namespace Tessa.Extensions.Default.Console.ResetSettings
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
                await this.Logger.InfoAsync("Resetting settings units...");

                await using var proxy = await this.webProxyFactory.UseProxyAsync<SettingsUnitsWebProxy>(cancellationToken: cancellationToken);

                var success =
                    (context.Names is null || await this.ResetUnitsAsync(context.Names, proxy, cancellationToken))
                    & (context.Categories is null || await this.ResetCategoriesAsync(context.Categories, proxy, cancellationToken));

                return success ? 0 : -2;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.Logger.LogExceptionAsync("Error resetting settings", ex);
                return -1;
            }
        }

        #endregion

        #region Private Methods

        private async Task<bool> ResetUnitsAsync(IEnumerable<string> names, SettingsUnitsWebProxy proxy, CancellationToken cancellationToken)
        {
            var success = true;

            foreach (var name in names)
            {
                try
                {
                    await this.Logger.InfoAsync("Resetting settings unit \"{0}\"", name);

                    var settingNames = name.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    var unitName = settingNames.ElementAt(0);
                    var fragmentName = settingNames.ElementAtOrDefault(1);

                    await proxy.ResetRecordAsync(unitName, fragmentName, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    success = false;
                    await this.Logger.LogExceptionAsync($"Failed to reset settings unit \"{name}\"", ex);
                }
            }

            return success;
        }

        private async Task<bool> ResetCategoriesAsync(IEnumerable<string> categories, SettingsUnitsWebProxy proxy, CancellationToken cancellationToken)
        {
            var success = true;

            foreach (var category in categories)
            {
                try
                {
                    await this.Logger.InfoAsync("Resetting settings unit for category \"{0}\"", category);

                    var nested = category.EndsWith('*');
                    await proxy.ResetRecordsAsync(category.TrimEnd('*'), nested, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    success = false;
                    await this.Logger.LogExceptionAsync($"Failed to reset settings unit for category \"{category}\"", ex);
                }
            }

            return success;
        }

        #endregion
    }
}
