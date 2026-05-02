using System.IO;
using System.Threading.Tasks;
using NLog;
using Tessa.Localization;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;

namespace Tessa.Extensions.Default.Console.PrintJson
{
    public static class Command
    {
        [Verb("PrintJson")]
        [LocalizableDescription("Common_CLI_PrintJson")]
        public static async Task PrintJson(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument][LocalizableDescription("Common_CLI_FilePath")] string filePath,
            [Argument("m")][LocalizableDescription("Common_CLI_PrintJsonMode")] PrintJsonMode mode = PrintJsonMode.Config,
            [Argument("i")][LocalizableDescription("Common_CLI_Indented")] bool indented = false,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")][LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            IConsoleLogger logger = new ConsoleLogger(LogManager.GetLogger(nameof(PrintJson)), stdOut, stdErr, quiet);

            var result = await Operation.ExecuteAsync(logger, filePath, mode, indented);
            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
