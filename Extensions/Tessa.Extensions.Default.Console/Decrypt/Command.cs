using System.IO;
using System.Threading.Tasks;
using NLog;
using Tessa.Localization;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;

namespace Tessa.Extensions.Default.Console.Decrypt
{
    public static class Command
    {
        #region Static Members

        [Verb("Decrypt")]
        [LocalizableDescription("Common_CLI_Decrypt")]
        public static async Task Decrypt(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument] [LocalizableDescription("Common_CLI_FilePath")] string filePath,
            [Argument("c")] [LocalizableDescription("Common_CLI_CertificateFilePath")] string certificateFilePath,
            [Argument("p")] [LocalizableDescription("Common_CLI_CertificatePassword")] string? certificatePassword = null,
            [Argument("k")] [LocalizableDescription("Common_CLI_CertificateKeyPemFilePath")] string? certificateKeyPemFilePath = null,
            [Argument("out")] [LocalizableDescription("Common_CLI_DecryptedFilePath")] string? decryptedFilePath = null,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            ThrowIfNullOrEmpty(filePath);
            ThrowIfNullOrEmpty(certificateFilePath);

            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            IConsoleLogger logger = new ConsoleLogger(LogManager.GetLogger(nameof(Decrypt)), stdOut, stdErr, quiet);

            var result = await Operation.ExecuteAsync(
                logger,
                filePath,
                certificateFilePath,
                certificatePassword,
                certificateKeyPemFilePath,
                decryptedFilePath);
            ConsoleAppHelper.EnvironmentExit(result);
        }

        #endregion
    }
}
