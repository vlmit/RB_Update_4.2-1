using System.IO;
using System.Threading.Tasks;
using NLog;
using Tessa.Extensions.Platform.Shared.MobileClient;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;

namespace Tessa.Extensions.Default.Console.PackageMobileClientApp
{
    public static class Command
    {
        [Verb("PackageMobileClientApp")]
        [LocalizableDescription("Common_CLI_PackageMobileClientApp")]
        public static async Task MobileClient(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_WebAppSourceExecutable")] string executable,
            [Argument("out")] [LocalizableDescription("Common_CLI_WebAppOutputPackage")] string? jcardFile = null,
            [Argument("conf")] [LocalizableDescription("Common_CLI_MobileConfigPath")] string? configPath = null,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }
            
            ThrowIfNullOrEmpty(executable);

            IConsoleLogger logger = new ConsoleLogger(LogManager.GetLogger(nameof(MobileClient)), stdOut, stdErr, quiet);
            string appFolder = Path.GetDirectoryName(Path.GetFullPath(executable.NormalizePathOnCurrentPlatform())) ?? ".";
            string jcardDefault = string.IsNullOrEmpty(jcardFile) ? $"{appFolder}.jcard" : jcardFile.NormalizePathOnCurrentPlatform();
            string configDefault = string.IsNullOrEmpty(configPath) ? Path.Combine(appFolder, MobileClientHelper.FileNameConfig) : configPath.NormalizePathOnCurrentPlatform();

            int result = await Operation.ExecuteAsync(logger, executable, jcardDefault, configDefault);
            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
