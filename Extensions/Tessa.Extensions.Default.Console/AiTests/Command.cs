using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tessa.Ai.TestEngine.Report;
using Tessa.Localization;
using Tessa.Platform;
using Tessa.Platform.CommandLine;
using Tessa.Platform.ConsoleApps;
using Unity;

namespace Tessa.Extensions.Default.Console.AiTests
{
    public static class Command
    {
        [Verb("AiTests")]
        [LocalizableDescription("Common_CLI_AiTests")]
        public static async Task AiTests(
            [Output] TextWriter stdOut,
            [Error] TextWriter stdErr,
            [Argument, LocalizableDescription("Common_CLI_TestPath")] string path,
            [Argument("o"), LocalizableDescription("Common_CLI_ResultPath")] string? resultPath = null,
            [Argument("join"), LocalizableDescription("Common_CLI_JoinResults")] bool joinResults = false,
            [Argument("f"), LocalizableDescription("Common_CLI_ResultFormat")] AiPromptTestReportFormat format = AiPromptTestReportFormat.Yaml,
            [Argument("q"), LocalizableDescription("Common_CLI_Quiet")] bool quiet = false,
            [Argument("nologo")] [LocalizableDescription("CLI_NoLogo")] bool nologo = false)
        {
            if (!nologo && !quiet)
            {
                await ConsoleAppHelper.WriteLogoAsync(stdOut);
            }

            ThrowIfNullOrWhiteSpace(path);
            ThrowIf(path, !Path.Exists(path), "The specified path does not exist.");

            var testPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
            FileAttributes attr = File.GetAttributes(testPath);
            var isDirectory = attr.HasFlag(FileAttributes.Directory);
            var filePaths = isDirectory
                    ? Directory.EnumerateFiles(testPath, "*.y*ml").Where(x => x.EndsWith(".yaml") || x.EndsWith(".yml")).ToArray()
                    : [testPath];

            if (filePaths.Length == 0)
            {
                throw new ArgumentException("You must specify at least one test file.");
            }

            if (!isDirectory)
            {
                var fileExtension = Path.GetExtension(testPath);
                ThrowIf(fileExtension, fileExtension != ".yaml" && fileExtension != ".yml", "The test file must have the extension \".yaml\" or \".yml\".");
            }

            var resultFilePath = FormResultFileName(
                isDirectory ? testPath : Path.GetDirectoryName(testPath)!,
                resultPath, joinResults, format.ToString().ToLower());

            int result;
            await using (var companion = new UnityContainerCompanion { UseConfiguration = true })
            {
                result = await companion.ProcessAndGetAsync(
                    (c, ct) => c.Container
                        .RegisterConsoleOperationLogger(stdOut, stdErr, quiet)
                        .RegisterSingleton<IConsoleSessionManager, FakeConsoleSessionManager>()
                        .RegisterServerForConsoleAsync(cancellationToken: ct),
                    async (c, ct) =>
                    {
                        await using var operation = c.Container.Resolve<Operation>();
                        return await operation.ExecuteAsync(
                            new()
                            {
                                TestPaths = filePaths,
                                ResultFilePath = resultFilePath,
                                JoinResults = joinResults,
                                Format = format
                            },
                            ct);
                    });
            }

            ConsoleAppHelper.EnvironmentExit(result);
        }

        private static string FormResultFileName(string baseDirectory, string? proposedPath, bool join, string extension)
        {
            // если предполагаемый путь задан
            if (!string.IsNullOrWhiteSpace(proposedPath))
            {
                FileAttributes? attr = null;
                try
                {
                    // пытаемся получить атрибуты по заданному пути,
                    // но он может и не существовать
                    attr = File.GetAttributes(proposedPath);
                }
                catch
                {
                    // ничего не делаем
                }

                if (join)
                {
                    // если путь существует, то это должен быть файл
                    if (attr.HasValue && attr.Value.HasFlag(FileAttributes.Directory))
                    {
                        throw new ArgumentException("resultPath must be a file.");
                    }
                    // иначе, просто считаем, что так и есть
                    if (attr is null)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(proposedPath)!);
                    }
                    return proposedPath;
                }
                // здесь мы точно ничего не объединяем, значит путь должен быть директорией
                if (attr.HasValue && !attr.Value.HasFlag(FileAttributes.Directory))
                {
                    throw new ArgumentException("resultPath must be a directory.");
                }
                // если не существует, создаём
                if (attr is null)
                {
                    Directory.CreateDirectory(proposedPath);
                }

                return Path.Join(proposedPath, $"{{0}}_result.{extension}");
            }

            // путь не задан
            return Path.Join(baseDirectory, join ? $"result.{extension}" : $"{{0}}_result.{extension}");
        }
    }
}
