namespace Tessa.Extensions.Default.Console.ImportViews
{
    /// <summary>
    /// Контекст операции импорта представлений
    /// </summary>
    public class OperationContext
    {
        /// <summary>
        /// Признак очистки списка представлений.
        /// </summary>
        public bool ClearViews { get; set; }

        /// <summary>
        /// Признак импорта ролей.
        /// </summary>
        public bool ImportRoles { get; set; }

        /// <summary>
        /// Источник файлов представлений.
        /// </summary>
        public string? Source { get; set; }
    }
}
