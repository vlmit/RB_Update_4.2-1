using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Ai.TestEngine;
using Tessa.Ai.TestEngine.Deserialization;
using Tessa.Ai.TestEngine.Report;
using Tessa.Platform.ConsoleApps;

namespace Tessa.Extensions.Default.Console.AiTests
{
    /// <summary>
    /// Команда запуска автотестов ответов от моделей ИИ.
    /// </summary>
    public sealed class Operation(
        IConsoleLogger logger,
        IConsoleSessionManager sessionManager,
        IAiPromptTestEngine testEngine,
        bool extendedInitialization = false)
        : ConsoleOperation<OperationContext>(logger, sessionManager, extendedInitialization)
    {
        #region Private Fields

        private readonly IAiPromptTestEngine testEngine = NotNullOrThrow(testEngine);

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override async Task<int> ExecuteAsync(
            OperationContext context,
            CancellationToken cancellationToken = default)
        {
            List<IAiPromptTestCaseResult> testResults;
            try
            {
                await this.Logger.WriteLineAsync(await LocalizeFormatAsync("$Ai_TestEngine_TadminCommand_DeserializingMessage", context.TestPaths.Length));
                var testCases = new List<IAiPromptTestCase>();
                foreach (var path in context.TestPaths)
                {
                    using var reader = new StreamReader(path);
                    var deserializedFile = AiTestFile.Deserialize(reader, Path.GetFileName(path));
                    if (deserializedFile is null)
                    {
                        continue;
                    }

                    testCases.AddRange(await AiPromptTestConverter.ToPromptTestCaseAsync(deserializedFile, cancellationToken));
                }

                await this.Logger.WriteLineAsync(await LocalizeFormatAsync("$Ai_TestEngine_TadminCommand_PerformingMessage", testCases.Count));
                testResults = (await this.testEngine.RunAsync(
                    new()
                    {
                        HasFileAccess = true,
                        TestCases = testCases
                    },
                    cancellationToken))
                    .ToList();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await this.Logger.LogExceptionAsync(null, ex);
                return -1;
            }

            await this.Logger.WriteLineAsync(await LocalizeAsync("$Ai_TestEngine_TadminCommand_PreparingResultMessage"));

            if (context.JoinResults)
            {
                await CreateResultFileAsync(testResults, context.ResultFilePath, context.Format, cancellationToken);
            }
            else
            {
                foreach (var results in testResults.GroupBy(r => r.TestCase.FileName))
                {
                    await CreateResultFileAsync(
                        results,
                        string.Format(context.ResultFilePath, Path.GetFileNameWithoutExtension(results.Key)),
                        context.Format,
                        cancellationToken);
                }
            }

            await this.Logger.WriteLineAsync(await LocalizeAsync("$Ai_TestEngine_TadminCommand_DoneMessage"));
            return 0;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Создать файл-сводку по результатам тестирования.
        /// </summary>
        /// <param name="testResults">Список результатов тест-кейсов.</param>
        /// <param name="resultPath">Путь к результатам.</param>
        /// <param name="format"><inheritdoc cref="AiPromptTestReportFormat" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        private static async Task CreateResultFileAsync(
            IEnumerable<IAiPromptTestCaseResult> testResults,
            string resultPath,
            AiPromptTestReportFormat format,
            CancellationToken cancellationToken)
        {
            await using StreamWriter streamWriter = new StreamWriter(resultPath, new FileStreamOptions()
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write
            });

            await AiPromptTestReport.WriteReportAsync(testResults.ToList(), format, streamWriter, cancellationToken);
        }

        #endregion
    }
}
