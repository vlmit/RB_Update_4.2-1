using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.ExportDiffCulture
{
    public static class Command
    {
        [Verb("ExportDiffCulture")]
        [LocalizableDescription("Common_CLI_ExportDiffCulture")]
        public static async Task ExportDiffCulture(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_ExportDiffCultureSources")] IEnumerable<string>? sources,
            [Argument("base")] [LocalizableDescription("Common_CLI_ExportDiffCultureBase")] string @base,
            [Argument("target")] [LocalizableDescription("Common_CLI_ExportDiffCultureTarget")] string target,
            [Argument("o")] [LocalizableDescription("Common_CLI_ExportDiffCultureOutput")] string output,
            [Argument("q")] [LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            ThrowIfNullOrEmpty(@base);
            ThrowIfNullOrEmpty(target);
            ThrowIfNullOrEmpty(output);

            CultureInfo baseCulture = CultureInfo.GetCultureInfo(@base.Trim());
            CultureInfo targetCulture = CultureInfo.GetCultureInfo(target.Trim());

            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            int result;
            await using (var companion = new UnityContainerCompanion { UseConfiguration = true })
            {
                result = await companion.ProcessAndGetAsync(
                    (c, ct) => c.Container.ConfigureConsoleForClientAsync(stdOut, stdErr, quiet, cancellationToken: ct),
                    async (c, ct) =>
                    {
                        await using var operation = c.Container.Resolve<Operation>();
                        return await operation.ExecuteAsync(
                            new()
                            {
                                Sources = sources?.Select(x => x.NormalizePathOnCurrentPlatform()).ToList() ?? [],
                                Output = output.NormalizePathOnCurrentPlatform(),
                                TargetCulture = targetCulture,
                                BaseCulture = baseCulture
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
