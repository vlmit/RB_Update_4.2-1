#nullable enable

namespace Tessa.Extensions.Default.Server.Workflow.KrCompilers
{
    /// <summary>
    /// Объект, выполняющий предварительную обработку кода перед компиляцией.
    /// </summary>
    public interface IKrPreprocessor
    {
        /// <summary>
        /// Выполняет обработку исходного кода метода.
        /// </summary>
        /// <param name="source">Обрабатываемый исходный код.</param>
        /// <returns>Обработанный исходный код.</returns>
        string Preprocess(string source);
    }
}
