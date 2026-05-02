using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Tessa.Localization;
using Tessa.Notes;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;

namespace Tessa.Extensions.Default.Console.ConfigDiff
{
    public static class Command
    {
        [Verb(nameof(ConfigDiff)), LocalizableDescription("Common_CLI_ConfigDiff")]
        [Example("results/comparison-{0}.json -f:json -old:../Configuration.Base -new:../Configuration" +
            " -csf:PostgreSql -csf:Roles -csf:VirtualFiles -ex:../PrevReleaseNotes.txt -lang:en -warnAsError")]
        public static async Task ConfigDiff(
            [Input] TextReader input,
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument("old"), LocalizableDescription("Common_CLI_ConfigDiff_OldConfigurationPath")] string oldConfigurationPath,
            [Argument("new"), LocalizableDescription("Common_CLI_ConfigDiff_NewConfigurationPath")] string newConfigurationPath,
            [Argument, LocalizableDescription("Common_CLI_ConfigDiff_ResultsFilePath")] string? resultsFilePath = null,
            [Argument("ex"), LocalizableDescription("Common_CLI_ConfigDiff_OldNotesPath")] string? oldNotesPath = null,
            [Argument("csf"), LocalizableDescription("Common_CLI_ConfigDiff_CardSeparateFolders")] IEnumerable<string>? cardSeparateFolders = null,
            [Argument("f"), LocalizableDescription("Common_CLI_ConfigDiff_Format")] NoteWriteMode format = NoteWriteMode.Text,
            [Argument("lang"), LocalizableDescription("Common_CLI_ConfigDiff_Language")] string? language = null,
            [Argument("warnAsError"), LocalizableDescription("Common_CLI_ConfigDiff_WarnAsError")] bool warnAsError = false,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            var logger = new ConsoleLogger(LogManager.GetLogger(nameof(ConfigDiff)), stdOut, stdErr, quiet);
            var operation = new Operation(logger);

            var result = await operation.ExecuteAsync(
                new NoteProcessorRequest
                {
                    OldFolder = oldConfigurationPath.NormalizePathOnCurrentPlatform(),
                    NewFolder = newConfigurationPath.NormalizePathOnCurrentPlatform(),
                    ExistentNotesPath = oldNotesPath.NormalizePathOnCurrentPlatform(),
                    CardSeparateFolders = cardSeparateFolders?.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.NormalizePathOnCurrentPlatform()).ToArray() ?? [],
                    WriteMode = format,
                    WarnAsError = warnAsError
                },
                resultsFilePath.NormalizePathOnCurrentPlatform(),
                language);

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
