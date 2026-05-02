using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NLog;
using Tessa.Localization;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Tessa.TextRecognition.Enums;
using Unity;

namespace Tessa.Extensions.Default.Console.TextRecognition.Sync
{
    /// <summary>
    /// Синхронная команда распознавания файла.
    /// </summary>
    public sealed class Command : Base.Command
    {
        [Verb(nameof(OcrSync))]
        [LocalizableDescription("Common_CLI_OcrSync")]
        public static async Task OcrSync(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument, LocalizableDescription("Common_CLI_SourceFile")] string sourceFile,
            [Argument("target"), LocalizableDescription("Common_CLI_Target")] string? target = null,
            [Argument("lang"), LocalizableDescription("Common_CLI_Languages")] IEnumerable<OcrLanguage>? languages = null,
            [Argument("mode"), LocalizableDescription("Common_CLI_SegmentationMode")] OcrSegmentationMode segmentationMode = OcrSegmentationMode.AutoOsd,
            [Argument("cf"), LocalizableDescription("Common_CLI_Confidence")] int confidence = 50,
            [Argument("pp"), LocalizableDescription("Common_CLI_Preprocess")] bool preprocess = false,
            [Argument("dr"), LocalizableDescription("Common_CLI_DetectRotation")] bool detectRotation = true,
            [Argument("dt"), LocalizableDescription("Common_CLI_DetectTables")] bool detectTables = false,
            [Argument("db"), LocalizableDescription("Common_CLI_DetectBarcodes")] bool detectBarcodes = false,
            [Argument("ow"), LocalizableDescription("Common_CLI_Overwrite")] bool overwrite = false,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo"), LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            // Отображение лого
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            var logger = new ConsoleLogger(LogManager.GetLogger(nameof(OcrSync)), stdOut, stdErr, quiet);

            // Проверка параметров
            if (string.IsNullOrEmpty(sourceFile))
            {
                await logger.ErrorAsync($"Pass the path to the file to be recognized in the \"{nameof(sourceFile)}\" parameter.");
                ConsoleAppHelper.EnvironmentExit(-1);
                return;
            }

            if (!File.Exists(sourceFile))
            {
                await logger.ErrorAsync($"Can not find source file by path \"{sourceFile}\". Please, check if file exists and application has access to it.");
                ConsoleAppHelper.EnvironmentExit(-1);
                return;
            }

            var ocrParameters = await GetOcrParametersAsync(logger, languages, segmentationMode, confidence, preprocess, detectRotation, detectTables, detectBarcodes, overwrite);
            if (ocrParameters is null)
            {
                ConsoleAppHelper.EnvironmentExit(-1);
                return;
            }

            target = string.IsNullOrEmpty(target)
                ? Directory.GetCurrentDirectory()
                : DefaultConsoleHelper.NormalizeFolderAndCreateIfNotExists(target);

            // Запуск операции
            var result = await ExecuteWithCancelAsync<Operation, OperationContext>(
                (container, ct) => container
                    .RegisterConsoleOperationLogger(stdOut, stdErr, quiet)
                    .RegisterSingleton<IConsoleSessionManager, FakeConsoleSessionManager>()
                    .RegisterServerForConsoleAsync(cancellationToken: ct),
                (operation, ct) => operation.ExecuteAsync(new(sourceFile, target, ocrParameters), ct));

            ConsoleAppHelper.EnvironmentExit(result);
        }
    }
}
