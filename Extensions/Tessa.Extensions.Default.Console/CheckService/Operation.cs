using System.Threading;
using System.Threading.Tasks;
using Tessa.Platform.ConsoleApps;

namespace Tessa.Extensions.Default.Console.CheckService
{
    public sealed class Operation(IConsoleSessionManager sessionManager, IConsoleLogger logger)
        : ConsoleOperation(logger, sessionManager)
    {
        #region Base Overrides

        public override async Task<int> ExecuteAsync(
            ConsoleOperationContext context,
            CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            await this.Logger.InfoAsync("Service connection check is successful");
            return 0;
        }

        #endregion
    }
}
