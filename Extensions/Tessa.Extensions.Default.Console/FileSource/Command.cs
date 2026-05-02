using System;
using System.IO;
using System.Threading.Tasks;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.FileSource
{
    public static class Command
    {
        [Verb("FileSource")]
        [LocalizableDescription("Common_CLI_FileSource")]
        public static async Task FileSource(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_FileSourceID")] int id,
            [Argument("a")] [LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u")] [LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p")] [LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("f")] [LocalizableDescription("Common_CLI_FileSourceLocation")] string? fileLocation = null,
            [Argument("db")] [LocalizableDescription("Common_CLI_FileSourceDatabase")] string? databaseLocation = null,
            [Argument("n")] [LocalizableDescription("Common_CLI_FileSourceName")] string? name = null,
            [Argument("ext")] [LocalizableDescription("Common_CLI_FileSourceExtensions")] string? fileExtensions = null,
            [Argument("c")] [LocalizableDescription("Common_CLI_RemoveFileSource")] bool remove = false,
            [Argument("default")] [LocalizableDescription("Common_CLI_FileSourceDefault")] bool isDefault = false,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            int paramCounter = 0;
            if (!string.IsNullOrEmpty(fileLocation))
            {
                paramCounter++;
            }

            if (!string.IsNullOrEmpty(databaseLocation))
            {
                paramCounter++;
            }

            if (remove && paramCounter == 0)
            {
                paramCounter++;
            }

            switch (paramCounter)
            {
                case 0:
                    throw new ArgumentException("No action specified to perform with file source.");
                case > 1:
                    throw new ArgumentException("More than one action specified to perform with file source.");
            }

            int result;
            await using (var companion = new UnityContainerCompanion { UseConfiguration = true })
            {
                result = await companion.ProcessAndGetAsync(
                    (c, ct) => c.Container.ConfigureConsoleForClientAsync(stdOut, stdErr, quiet, address, cancellationToken: ct),
                    async (c, ct) =>
                    {
                        await using var operation = c.Container.Resolve<Operation>();
                        if (!await operation.LoginAsync(userName, password, ct))
                        {
                            return ConsoleAppHelper.FailedLoginExitCode;
                        }

                        return await operation.ExecuteAsync(
                            new()
                            {
                                ID = id,
                                FileLocation = fileLocation,
                                DatabaseLocation = databaseLocation,
                                Remove = remove,
                                Name = name,
                                FileExtensions = fileExtensions,
                                IsDefault = isDefault
                            }, ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
