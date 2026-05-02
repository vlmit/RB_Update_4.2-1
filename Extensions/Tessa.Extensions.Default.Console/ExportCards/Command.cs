using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.ExportCards
{
    public static class Command
    {
        [Verb("ExportCards")]
        [LocalizableDescription("Common_CLI_ExportCards")]
        public static async Task ExportCards(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_CardIdentifiers")] IEnumerable<string>? identifiers = null,
            [Argument("a")] [LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u")] [LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p")] [LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("s")] [LocalizableDescription("Common_CLI_ColumnSeparator")] string? separatorChar = null,
            [Argument("o")] [LocalizableDescription("Common_CLI_CardsOutputFolder")] string? outputFolder = null,
            [Argument("l")] [LocalizableDescription("Common_CLI_CardLibraryPath")] string? libraryFilePath = null,
            [Argument("localize")] [LocalizableDescription("Common_CLI_CardLocalizationCulture")] string? culture = null,
            [Argument("overwrite")] [LocalizableDescription("Common_CLI_OverwriteModifiedValues")] bool overwriteModifiedValues = false,
            [Argument("mapping")] [LocalizableDescription("Common_CLI_StorageContentMapping")] string? storageContentMappingFileName = null,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            CultureInfo? localizationCulture = string.IsNullOrEmpty(culture) ? null : CultureInfo.GetCultureInfo(culture);

            int result;
            await using (var companion = new UnityContainerCompanion { UseConfiguration = true })
            {
                result = await companion.ProcessAndGetAsync(
                    (c, ct) => c.Container.ConfigureConsoleForClientAsync(stdOut, stdErr, quiet, address, cancellationToken: ct),
                    async (c, ct) =>
                    {
                        var logger = c.Container.Resolve<IConsoleLogger>();
                        if (await DefaultConsoleHelper.TryParseCardInfoListAsync(identifiers, logger, separatorChar, cancellationToken: ct)
                            is not { } cardInfoList)
                        {
                            // ошибка парсинга
                            return -2;
                        }

                        await using var operation = c.Container.Resolve<Operation>();
                        if (!await operation.LoginAsync(userName, password, ct))
                        {
                            return ConsoleAppHelper.FailedLoginExitCode;
                        }

                        return await operation.ExecuteAsync(
                            new()
                            {
                                CardInfoList = cardInfoList,
                                CardLibraryPath = libraryFilePath,
                                OutputFolder = outputFolder,
                                LocalizationCulture = localizationCulture,
                                OverwriteModifiedValues = overwriteModifiedValues,
                                StorageContentMappingFileName = storageContentMappingFileName
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
