using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NLog;
using Tessa.Localization;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Tessa.TextRecognition.Enums;

namespace Tessa.Extensions.Default.Console.TextRecognition.Async
{
    /// <summary>
    /// Асинхронная команда распознавания файла.
    /// </summary>
    public sealed class Command : Base.Command
    {
        [Verb(nameof(OcrAsync))]
        [LocalizableDescription("Common_CLI_OcrAsync")]
        public static async Task OcrAsync(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument, LocalizableDescription("Common_CLI_FileIdentifier")] string fileIdentifier,
            [Argument("lang"), LocalizableDescription("Common_CLI_Languages")] IEnumerable<OcrLanguage>? languages = null,
            [Argument("mode"), LocalizableDescription("Common_CLI_SegmentationMode")] OcrSegmentationMode segmentationMode = OcrSegmentationMode.AutoOsd,
            [Argument("cf"), LocalizableDescription("Common_CLI_Confidence")] int confidence = 50,
            [Argument("pp"), LocalizableDescription("Common_CLI_Preprocess")] bool preprocess = false,
            [Argument("dr"), LocalizableDescription("Common_CLI_DetectRotation")] bool detectRotation = true,
            [Argument("dt"), LocalizableDescription("Common_CLI_DetectTables")] bool detectTables = false,
            [Argument("db"), LocalizableDescription("Common_CLI_DetectBarcodes")] bool detectBarcodes = false,
            [Argument("ow"), LocalizableDescription("Common_CLI_Overwrite")] bool overwrite = false,
            [Argument("a"), LocalizableDescription("Common_CLI_Address")] string? address = null,
            [Argument("u"), LocalizableDescription("Common_CLI_UserName")] string? userName = null,
            [Argument("p"), LocalizableDescription("Common_CLI_Password")] string? password = null,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo"), LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            // Отображение лого
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            var logger = new ConsoleLogger(LogManager.GetLogger(nameof(OcrAsync)), stdOut, stdErr, quiet);

            // Проверка параметров
            if (string.IsNullOrEmpty(fileIdentifier))
            {
                await logger.ErrorAsync($"Pass the file identifier in the \"{nameof(fileIdentifier)}\" parameter.");
                ConsoleAppHelper.EnvironmentExit(-1);
                return;
            }

            var isParsed = Guid.TryParse(fileIdentifier, out var fileID);
            if (!isParsed)
            {
                await logger.ErrorAsync($"Can not parse file identifier to GUID. Please, check value and format in the \"{nameof(fileIdentifier)}\" parameter.");
                ConsoleAppHelper.EnvironmentExit(-1);
                return;
            }

            var ocrParameters = await GetOcrParametersAsync(logger, languages, segmentationMode, confidence, preprocess, detectRotation, detectTables, detectBarcodes, overwrite);
            if (ocrParameters is null)
            {
                ConsoleAppHelper.EnvironmentExit(-1);
                return;
            }

            // Запуск операции
            var result = await ExecuteWithCancelAsync<Operation, OperationContext>(
                (container, ct) => container.ConfigureConsoleForClientAsync(stdOut, stdErr, quiet, address, ct),
                async (operation, ct) =>
                {
                    // Вход в систему под пользователем
                    if (!await operation.LoginAsync(userName, password, cancellationToken: ct))
                    {
                        return ConsoleAppHelper.FailedLoginExitCode;
                    }

                    return await operation.ExecuteAsync(new(fileID, ocrParameters), ct);
                });

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
