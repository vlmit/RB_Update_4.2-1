#nullable enable

namespace Tessa.Test.Default.Shared
{
    /// <summary>
    /// Параметры создания таблицы с помощью метода <see cref="TestTextHelper.PrintTable"/>.
    /// </summary>
    /// <param name="ColumnSeparator">Разделитель столбцов.</param>
    /// <param name="LineSeparator">Разделитель строк.</param>
    /// <param name="MinColumnWidth">Минимальная ширина столбца.</param>
    /// <param name="MaxColumnWidth">Максимальная ширина столбца.</param>
    public sealed record TableSettings(
        char ColumnSeparator = '|',
        char LineSeparator = '-',
        int MinColumnWidth = 1,
        int MaxColumnWidth = 80)
    {
        /// <summary>
        /// Проверяет корректность параметров.
        /// </summary>
        public void Validate()
        {
            ThrowIf(this.MinColumnWidth, this.MinColumnWidth < 0);
            ThrowIf(this.MaxColumnWidth, this.MaxColumnWidth < this.MinColumnWidth);
        }
    }
}
