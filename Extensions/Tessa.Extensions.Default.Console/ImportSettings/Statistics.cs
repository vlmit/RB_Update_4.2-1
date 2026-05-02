using System.Threading.Tasks;
using Tessa.Platform.ConsoleApps;

namespace Tessa.Extensions.Default.Console.ImportSettings
{
    public sealed class Statistics
    {
        public int Success { get; private set; }

        public int Error { get; private set; }

        public void RegisterSuccess() => this.Success++;

        public void RegisterError() => this.Error++;

        public async Task LogSummaryAsync(IConsoleLogger logger)
        {
            if (this.Success > 0)
            {
                await logger.InfoAsync("Settings units ({0}) are imported successfully", this.Success);
            }
            if (this.Error > 0)
            {
                await logger.ErrorAsync("Settings units ({0}) aren't imported", this.Error);
            }
            else if (this.Success == 0)
            {
                await logger.InfoAsync("No settings units to import");
            }
        }
    }
}
