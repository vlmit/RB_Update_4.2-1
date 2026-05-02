#nullable enable

using System.Collections.Generic;
using System.Threading;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Placeholders.Extensions;
using static Tessa.Extensions.Default.Server.Cards.ExcelPlaceholderReplaceExtensionContext;

namespace Tessa.Extensions.Default.Server.Cards
{
    /// <summary>
    /// Контекст обработки расширений <see cref="IPlaceholderReplaceExtension"/> в Excel документах
    /// </summary>
    public sealed class ExcelPlaceholderReplaceExtensionContext :
        OpenXmlPlaceholderReplaceExtensionContext<TableData, IEnumerable<Row>, PlaceholderData>
    {
        #region Nested Types

        /// <summary>
        /// Данные контекста обработки таблицы.
        /// </summary>
        /// <param name="TableElements">Перечисление строк таблицы или <c>null</c>, если набор строк таблицы неизвестен.</param>
        /// <param name="Worksheet">Страница документа, на котором располагается таблица, или <c>null</c>, если она неизвестна.</param>
        public record struct TableData(IEnumerable<Row>? TableElements, Worksheet? Worksheet);

        /// <summary>
        /// Данные контекста обработки плейсхолдера.
        /// </summary>
        /// <param name="Element">Элемент, в котором располагается плейсхолдера, или <c>null</c>, если он неизвестен.</param>
        /// <param name="TextElement">Текстовый элемент, в котором располагается плейсхолдер, или <c>null</c>, если он неизвестен.</param>
        public record struct PlaceholderData(OpenXmlElement? Element, OpenXmlLeafTextElement? TextElement);

        #endregion

        #region Fields

        private readonly List<Row> tableElements = [];
        private readonly List<Row> rowElements = [];

        #endregion

        #region Constructors

        public ExcelPlaceholderReplaceExtensionContext(
            IPlaceholderReplacementContext replacementContext,
            CancellationToken cancellationToken = default)
            : base(replacementContext, cancellationToken)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Текущий документ. Доступен в любом расширении, но структуру документа
        /// рекомендуется изменять только в <see cref="IPlaceholderReplaceExtension.AfterDocumentReplace(IPlaceholderReplaceExtensionContext)"/>
        /// </summary>
        public SpreadsheetDocument? Document { get; set; }

        /// <summary>
        /// Текущий лист в Excel, в котором производится замена плейсхолдеров.
        /// Доступен во всех методах обработки таблиц, строк и плейсхолдеров, если замена значения производится в листе.
        /// </summary>
        public Worksheet? Worksheet { get; set; }

        /// <summary>
        /// Набор всех строк, добавленных в таблицу. Заполняется по мере генерации этих строк.
        /// Полностью заполнен только в методе <see cref="IPlaceholderReplaceExtension.AfterTableReplace(IPlaceholderReplaceExtensionContext)"/>
        /// </summary>
        public IReadOnlyList<Row> TableElements => this.tableElements.AsReadOnly();

        /// <summary>
        /// Набор всех строк, определяющих строку с плейсхолдерами. Заполняется по мере генерации строки.
        /// Полностью заполнен только в методе <see cref="IPlaceholderReplaceExtension.AfterRowReplace(IPlaceholderReplaceExtensionContext)"/>
        /// </summary>
        public IReadOnlyList<Row> RowElements => this.rowElements.AsReadOnly();

        /// <summary>
        /// Текущая обрабатываемая строка. Доступна в методах обработки плейсхолдера.
        /// </summary>
        public Row? CurrentRowElement { get; set; }

        /// <summary>
        /// Текущая обрабатываемая ячейка. Доступна в методах обработки плейсхолдеров.
        /// </summary>
        public Cell? Cell { get; set; }

        /// <summary>
        /// Элемент, в котором фактически производится замена плейсхолдера. Может отличаться от <see cref="Cell"/>,
        /// т.к. значение ячейки в Excel может быть записано, например, в <see cref="SharedStringItem"/>
        /// </summary>
        public OpenXmlElement? PlaceholderElement { get; private set; }

        /// <summary>
        /// Элемент, хранящий замененный текст.
        /// Может быть <see cref="CellValue"/>, <see cref="Text"/> или <see cref="DocumentFormat.OpenXml.Drawing.Text"/>
        /// </summary>
        public OpenXmlLeafTextElement? PlaceholderTextElement { get; private set; }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override TableData GetTableData()
        {
            return new(this.tableElements.Count > 0 ? [.. this.tableElements] : null, this.Worksheet);
        }

        /// <inheritdoc/>
        public override void SetTableData(TableData tableData)
        {
            this.tableElements.Clear();
            if (tableData.TableElements is { } tableElements)
            {
                this.tableElements.AddRange(tableElements);
            }

            this.Worksheet = tableData.Worksheet;
        }

        /// <inheritdoc/>
        public override IEnumerable<Row>? GetRowData()
        {
            return this.rowElements.Count > 0 ? [.. this.rowElements] : null;
        }

        /// <inheritdoc/>
        public override void SetRowData(IEnumerable<Row>? rowData)
        {
            this.rowElements.Clear();
            if (rowData is not null)
            {
                this.rowElements.AddRange(rowData);
            }
        }

        /// <inheritdoc/>
        public override PlaceholderData GetPlaceholderData()
        {
            return new(this.PlaceholderElement, this.PlaceholderTextElement);
        }

        /// <inheritdoc/>
        public override void SetPlaceholderData(PlaceholderData placeholderData)
        {
            this.PlaceholderElement = placeholderData.Element;
            this.PlaceholderTextElement = placeholderData.TextElement;
        }

        #endregion
    }
}
