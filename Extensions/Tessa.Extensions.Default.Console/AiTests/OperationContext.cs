using Tessa.Ai.TestEngine.Report;

namespace Tessa.Extensions.Default.Console.AiTests
{
    /// <summary>
    /// Контекст команды запуска тестирования промптов.
    /// </summary>
    public class OperationContext
    {
        /// <summary>
        /// Путь к файлам тестов.
        /// </summary>
        public required string[] TestPaths { get; init; }

        /// <summary>
        /// Путь к файлу результатов тестирования.
        /// </summary>
        public required string ResultFilePath { get; init; }

        /// <summary>
        /// Признак объединения нескольких результатов в один общий файл.
        /// </summary>
        public bool JoinResults { get; init; }

        /// <summary>
        /// Формат выводимого результата.
        /// </summary>
        public AiPromptTestReportFormat Format { get; init; }
    }
}
