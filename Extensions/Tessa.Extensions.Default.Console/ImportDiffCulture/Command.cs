using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.ImportDiffCulture
{
    public static class Command
    {
        [Verb("ImportDiffCulture")]
        [LocalizableDescription("Common_CLI_ImportDiffCulture")]
        public static async Task ImportDiffCulture(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_ImportDiffCultureSource")] string source,
            [Argument("o")] [LocalizableDescription("Common_CLI_ImportDiffCultureOutput")] string output,
            [Argument("target")] [LocalizableDescription("Common_CLI_ImportDiffCultureTarget")] string target,
            [Argument("q")] [LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            ThrowIfNullOrEmpty(source);
            ThrowIfNullOrEmpty(output);
            ThrowIfNullOrEmpty(target);

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
                                Source = source.NormalizePathOnCurrentPlatform(),
                                Output = output.NormalizePathOnCurrentPlatform(),
                                TargetCulture = targetCulture
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
