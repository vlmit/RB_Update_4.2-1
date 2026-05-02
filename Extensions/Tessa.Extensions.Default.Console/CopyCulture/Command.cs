using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.CopyCulture
{
    public static class Command
    {
        [Verb("CopyCulture")]
        [LocalizableDescription("Common_CLI_CopyCulture")]
        public static async Task CopyCulture(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_CopyCultureSource")] string source,
            [Argument("from")] [LocalizableDescription("Common_CLI_CopyCultureFrom")] string from,
            [Argument("to")] [LocalizableDescription("Common_CLI_CopyCultureTo")] string to,
            [Argument("o")] [LocalizableDescription("Common_CLI_CopyCultureOutputPath")] string? target = null,
            [Argument("detached"), LocalizableDescription("Common_CLI_CopyCultureDetached")] bool detached = false,
            [Argument("empty"), LocalizableDescription("Common_CLI_CopyCultureEmpty")] bool empty = false,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            ThrowIfNullOrEmpty(source);
            ThrowIfNullOrEmpty(from);
            ThrowIfNullOrEmpty(to);

            source = source.NormalizePathOnCurrentPlatform();
            target = target.NormalizePathOnCurrentPlatform();

            CultureInfo fromCulture = CultureInfo.GetCultureInfo(from.Trim());
            CultureInfo toCulture = CultureInfo.GetCultureInfo(to.Trim());

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
                                Source = source,
                                Target = target,
                                FromCulture = fromCulture,
                                ToCulture = toCulture,
                                ForceDetached = detached,
                                EmptyOnly = empty
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
