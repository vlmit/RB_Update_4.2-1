#nullable enable

using System.Collections.Generic;
using System.Threading;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Placeholders.Extensions;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Контекст обработки расширений <see cref="IPlaceholderReplaceExtension"/> в Word документах.
    /// </summary>
    public sealed class WordPlaceholderReplaceExtensionContext : OpenXmlPlaceholderReplaceExtensionContext<OpenXmlElement, IEnumerable<OpenXmlElement>, IEnumerable<Run>>
    {
        #region Fields

        private readonly List<OpenXmlElement> rowElements = [];
        private readonly List<Run> placeholderElements = [];

        #endregion

        #region Constructors

        public WordPlaceholderReplaceExtensionContext(
            IPlaceholderReplacementContext replacementContext,
            CancellationToken cancellationToken = default)
            : base(replacementContext, cancellationToken)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Текущий документ. Доступен в любом расширении, но структуру документа
        /// рекомендуется изменять только в <see cref="IPlaceholderReplaceExtension.AfterDocumentReplace(IPlaceholderReplaceExtensionContext)"/>.
        /// </summary>
        public WordprocessingDocument? Document { get; set; }

        /// <summary>
        /// Текущая обрабатываемая таблица. Доступна в методах обработки таблиц, строк и плейсхолдеров внутри строк.
        /// Может иметь тип <see cref="Table"/> для таблиц или любой другой тип, если используется перечисление.
        /// В виду особенностей Word, перечисление может быть внутри различных элементов, при том не все элементы будут к нему относится.
        /// </summary>
        public OpenXmlElement? TableElement { get; private set; }

        /// <summary>
        /// Набор элементов обрабатываемой строки. Доступен в методах обработки строк и плейсхолдеров внутри строк.
        /// Может иметь объекты типов <see cref="TableRow"/> для строк таблиц, <see cref="Paragraph"/> для строк перечисления и для кастомных строк, которые оперируют всем параграфом,
        /// или <see cref="Run"/> для кастомных строк, которые находятся внутри параграфа.
        /// </summary>
        public IReadOnlyList<OpenXmlElement> RowElements => this.rowElements.AsReadOnly();

        /// <summary>
        /// Текущий элемент обрабатываемой строки. Доступен в методах обработки плейсхолдеров внутри строк.
        /// Может иметь тип <see cref="TableRow"/> для строк таблиц, <see cref="Paragraph"/> для строк перечисления и для кастомных строк.
        /// </summary>
        public OpenXmlElement? CurrentRowElement { get; internal set; }

        /// <summary>
        /// Список элементов, в которых произошла замена плейсхолдера. Доступен в методах замены плейсхолдеров.
        /// Элементов может быть несколько, т.к. перенос строк в Word сохраняется как отдельный объект.
        /// Каждый элемент является <see cref="Run"/> с <see cref="Text"/> или <see cref="Break"/> внутри.
        /// </summary>
        public IReadOnlyList<Run> PlaceholderElements => this.placeholderElements.AsReadOnly();

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        public override IEnumerable<Run>? GetPlaceholderData()
        {
            return this.placeholderElements.Count > 0 ? [.. this.placeholderElements] : [];
        }

        /// <inheritdoc/>
        public override IEnumerable<OpenXmlElement>? GetRowData()
        {
            return this.rowElements.Count > 0 ? [.. this.rowElements] : [];
        }

        /// <inheritdoc/>
        public override OpenXmlElement? GetTableData()
        {
            return this.TableElement;
        }

        /// <inheritdoc/>
        public override void SetPlaceholderData(IEnumerable<Run>? placeholderData)
        {
            this.placeholderElements.Clear();
            if (placeholderData is not null)
            {
                this.placeholderElements.AddRange(placeholderData);
            }
        }

        /// <inheritdoc/>
        public override void SetRowData(IEnumerable<OpenXmlElement>? rowData)
        {
            this.rowElements.Clear();
            if (rowData is not null)
            {
                this.rowElements.AddRange(rowData);
            }
        }

        /// <inheritdoc/>
        public override void SetTableData(OpenXmlElement? tableData)
        {
            this.TableElement = tableData;
        }

        #endregion
    }
}
