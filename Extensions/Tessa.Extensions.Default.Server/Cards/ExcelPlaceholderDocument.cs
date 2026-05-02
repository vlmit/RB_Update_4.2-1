#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using LinqToDB;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Placeholders.Extensions;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using A = DocumentFormat.OpenXml.Drawing;
using Text = DocumentFormat.OpenXml.Spreadsheet.Text;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace Tessa.Extensions.Default.Server.Cards
{
    /// <summary>
    /// Объект, определяющий способы хранения и изменения текста с заменяемыми плейсхолдерами для документа Excel.
    /// </summary>
    /// <param name="stream">Поток файла документа, в котором должны быть заменены плейсхолдеры.</param>
    /// <param name="templateID">ID карточки шаблона файла.</param>
    /// <param name="parser">Парсер документа Excel.</param>
    public sealed class ExcelPlaceholderDocument(MemoryStream stream, Guid templateID, IExcelDocumentParser parser)
        : OpenXmlPlaceholderDocument(stream, templateID)
    {
        #region Fields

        /// <summary>
        /// Парсер документа Excel.
        /// </summary>
        private readonly IExcelDocumentParser parser = NotNullOrThrow(parser);

        /// <summary>
        /// Контекст расширений.
        /// </summary>
        private ExcelPlaceholderReplaceExtensionContext? extensionContext;

        /// <summary>
        /// Документ Excel.
        /// </summary>
        private SpreadsheetDocument? excelDocument;

        /// <summary>
        /// Элемент Stylesheet текущего документа. Хранит информацию о стилях ячеек.
        /// </summary>
        private Stylesheet? stylesheet;

        /// <summary>
        /// Элемент <see cref="SharedStringTableContainer"/>, являющийся оберткой над <see cref="SharedStringTable"/> текущего документа.
        /// </summary>
        private SharedStringTableContainer? sharedStringTable;

        /// <summary>
        /// Хеш всех WorksheetElement, содержащий таблицы.
        /// </summary>
        private HashSet<string, WorksheetElement>? worksheetHash;

        /// <summary>
        /// Справочник со всеми объектами Worksheet.
        /// </summary>
        private HashSet<Worksheet, WorksheetElement>? worksheetsDictionary;

        /// <summary>
        /// Показывает, есть ли в документе формулы.
        /// </summary>
        private bool hasFormulas;

        #endregion

        #region Properties

        /// <summary>
        /// Документ Excel.
        /// </summary>
        private SpreadsheetDocument ExcelDocument => this.excelDocument ?? throw NotInitializedException();

        /// <summary>
        /// Основная часть документа Excel.
        /// </summary>
        private WorkbookPart WorkbookPart => this.excelDocument?.WorkbookPart ?? throw NotInitializedException();

        /// <summary>
        /// Элемент Stylesheet текущего документа. Хранит информацию о стилях ячеек.
        /// </summary>
        private Stylesheet Stylesheet => this.stylesheet ?? throw NotInitializedException();

        /// <summary>
        /// Элемент <see cref="SharedStringTableContainer"/>, являющийся оберткой над <see cref="SharedStringTable"/> текущего документа.
        /// </summary>
        private SharedStringTableContainer SharedStringTable => this.sharedStringTable ?? throw NotInitializedException();

        /// <summary>
        /// Хеш всех WorksheetElement, содержащий таблицы.
        /// </summary>
        private HashSet<string, WorksheetElement> WorksheetHash => this.worksheetHash ?? throw NotInitializedException();

        /// <summary>
        /// Справочник со всеми объектами Worksheet.
        /// </summary>
        private HashSet<Worksheet, WorksheetElement> WorksheetsDictionary => this.worksheetsDictionary ?? throw NotInitializedException();

        #endregion

        #region Private Methods

        private static InvalidOperationException NotInitializedException() => new("Placeholder document not initialized.");

        /// <summary>
        /// Производит замену Replacement'ов в заданном элементе Cell.
        /// </summary>
        /// <param name="cell">Cell, в котором производится замена плейсхолдеров.</param>
        /// <param name="createNew">Определяет, создается ли новая ячейка или заменяется значение в существующей.</param>
        /// <param name="replacements">Массив плейсхолдеров для замены.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        private async Task ReplaceElementsInCellAsync(
            Cell cell,
            bool createNew,
            CancellationToken cancellationToken = default,
            params IReadOnlyList<IPlaceholderReplacement> replacements)
        {
            if (this.WithExtensions)
            {
                this.extensionContext.Cell = cell;
            }

            if (cell.DataType is not null
                && cell.DataType.HasValue
                && cell.DataType.Value == CellValues.SharedString)
            {
                var sharedStringID = int.Parse(cell.InnerText);

                var firstReplacement = replacements.First();
                var valueType = firstReplacement.NewValue.NetType;

                // Если формат ячейки задан явно (в его стиле NumberFormat отличен от 0) и в ячейке находится только данный плейсхолдер, то устанавливаем полученное значение в value
                if (valueType != typeof(string)
                    && valueType != typeof(DBNull)
                    && this.GetCellNumberFormat(cell) != 0
                    && this.GetSharedString(sharedStringID).Length == firstReplacement.Placeholder.Text.Length)
                {
                    using var _ = this.WithExtensions
                        ? this.extensionContext.ExecuteInPlaceholderContext(
                            firstReplacement.Placeholder,
                            firstReplacement.NewValue)
                        : null;

                    if (this.WithExtensions)
                    {
                        await this.BeforePlaceholderReplaceAsync(this.extensionContext.ReplacementContext);
                    }

                    var fields = firstReplacement.NewValue.Fields;
                    var value = fields.Count > 0 ? fields[0].Value : null;

                    string newCellValue;
                    if (value is null)
                    {
                        newCellValue = firstReplacement.NewValue.Text;
                    }
                    else if (valueType == typeof(DateTime))
                    {
                        newCellValue = ((DateTime) value).ToOADate().ToString(ExcelHelper.DoubleExcelFormat);
                    }
                    else if ((valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
                        && value is IFormattable formattable)
                    {
                        newCellValue = formattable.ToString(null, ExcelHelper.DoubleExcelFormat);
                    }
                    else
                    {
                        newCellValue = firstReplacement.NewValue.Text;
                    }

                    cell.DataType = null;
                    cell.CellValue ??= new();
                    cell.CellValue.Text = RemoveInvalidChars(newCellValue);

                    if (this.WithExtensions)
                    {
                        this.extensionContext.SetPlaceholderData(new(cell, cell.CellValue));
                        await this.AfterPlaceholderReplaceAsync(this.extensionContext.ReplacementContext);
                    }
                }
                else // Иначе заменяем значение в SharedString
                {
                    SharedStringItem? sharedString;
                    if (createNew)
                    {
                        (sharedString, sharedStringID) = this.CopySharedString(sharedStringID);
                    }
                    else
                    {
                        sharedString = this.SharedStringTable.ElementAt(sharedStringID);
                    }

                    if (sharedString is not null)
                    {
                        await this.ReplaceElementsInSharedStringAsync(sharedString, cancellationToken, replacements);
                    }

                    cell.CellValue ??= new();
                    cell.CellValue.Text = RemoveInvalidChars(sharedStringID.ToString());
                }
            }
            else if (cell.CellFormula is not null)
            {
                foreach (var replacement in replacements)
                {
                    var newValue = replacement.NewValue;
                    using var _ = this.WithExtensions
                        ? this.extensionContext.ExecuteInPlaceholderContext(
                            replacement.Placeholder,
                            replacement.NewValue)
                        : null;

                    if (this.WithExtensions)
                    {
                        await this.BeforePlaceholderReplaceAsync(this.extensionContext.ReplacementContext);
                        newValue = this.extensionContext.PlaceholderValue ??= PlaceholderValue.Empty;
                    }

                    await this.ReplaceElementAsync(
                        cell.CellFormula,
                        replacement.Placeholder,
                        newValue,
                        cancellationToken);
                }
                // Вместе с формулой может идти готовый текст, который при конвертации в PDF не будет перерассчитываться по формуле
                cell.CellValue = null;
            }
            else
            {
                await this.ReplaceElementsInCompositeElementAsync(cell, null, cancellationToken, replacements);
            }

            if (this.WithExtensions)
            {
                this.extensionContext.Cell = null;
            }
        }

        /// <summary>
        /// Метод для получения текста ячейки.
        /// </summary>
        /// <param name="cell">Элемент ячейки.</param>
        /// <returns>Возвращает текст ячейки.</returns>
        private string GetCellText(Cell cell)
        {
            return cell.DataType is not null
                && cell.DataType.HasValue
                && cell.DataType.Value == CellValues.SharedString
                && int.TryParse(cell.InnerText, out var num)
                    ? this.GetSharedString(num)
                    : cell.InnerText;
        }

        /// <summary>
        /// Возвращает все дочерние элементы с типом Text среди всех дочерних элементов объекта <c>baseElement</c>
        /// </summary>
        /// <param name="baseElement">Базовый элемент, начиная с которого производим поиск.</param>
        /// <returns>Список элементов типа Text, или null, если <c>baseElement</c> не содержит дочерних элементов типа <see cref="A.Text"/>.</returns>
        private static List<A.Text> GetTextElements(OpenXmlElement baseElement)
        {
            var result = new List<A.Text>();

            foreach (var e in baseElement.ChildElements)
            {
                var eType = e.GetType();
                if (eType == typeof(A.Text))
                {
                    result.Add((A.Text) e);
                }
                else if (eType != typeof(A.Hyperlink) && eType != typeof(A.Paragraph) && e.HasChildren)
                {
                    result.AddRange(GetTextElements(e));
                }
            }

            return result;
        }

        /// <summary>
        /// Метод для подготовки объектов Worksheet. Вызывается после инициализации Worksheets и TableGroups.
        /// Цель метода - заполнить свойства источников данных формул и организовать связи между элементами с учетом таблиц.
        /// </summary>
        private bool PrepareWorksheetFormulas()
        {
            var hasFormulas = false;
            foreach (var worksheet in this.WorksheetHash)
            {
                var tableGroupIndex = 0;
                // Игнорируем листы, у которых нет формул.
                if (worksheet.HasFormulas)
                {
                    for (var rowIndex = 0; rowIndex < worksheet.Rows.Count; rowIndex++)
                    {
                        var row = worksheet.Rows[rowIndex];
                        if (row.Formulas is null
                            || row.Formulas.Count == 0)
                        {
                            continue;
                        }

                        hasFormulas = true;
                        for (var formulaIndex = 0; formulaIndex < row.Formulas.Count; formulaIndex++)
                        {
                            var formula = row.Formulas[formulaIndex];
                            var formulaTableGroup = TryGetTableGroup(worksheet.Tables, ref tableGroupIndex, formula.Reference);

                            for (var sourceIndex = 0; sourceIndex < formula.FormulaSources.Count; sourceIndex++)
                            {
                                var source = formula.FormulaSources[sourceIndex].Item3;
                                var sourceTableGroup = TryGetTableGroup(source.Worksheet.Tables, source);
                                if (source.InFormulaWorksheet)
                                {
                                    // Установка свойства InFormulaTableGroup
                                    if (formulaTableGroup is null
                                        || formulaTableGroup == sourceTableGroup
                                        || (sourceTableGroup is not null && formulaTableGroup.IsInclude(sourceTableGroup)))
                                    {
                                        source.InFormulaTableGroup = true;
                                    }
                                }

                                // Установка свойства IsExpandable
                                if (sourceTableGroup is not null
                                    && formulaTableGroup != sourceTableGroup
                                    && source.Top == sourceTableGroup.Top
                                    && source.Bottom == sourceTableGroup.Bottom)
                                {
                                    source.TableGroup = sourceTableGroup;
                                }

                                // Установка связи сорса со строкой
                                // Если строка уже есть в текущей таблице, ищем в ней
                                if (sourceTableGroup is not null
                                    && source.Bottom >= sourceTableGroup.Bottom
                                    && source.Bottom <= sourceTableGroup.Top)
                                {
                                    var sourceRow = sourceTableGroup.Rows[source.Bottom - sourceTableGroup.Bottom];
                                    source.Row = sourceRow;
                                }
                                // Если нет, ищем в Worksheet
                                else if (source.Worksheet.Rows.TryFirst(x => x.Bottom == source.Bottom, out var sourceRow))
                                {
                                    source.Row = sourceRow;
                                }
                            }
                        }
                    }
                }
            }

            return hasFormulas;
        }

        private static TableGroup? TryGetTableGroup(IList<TableGroup> tables, ref int from, string reference)
        {
            for (; from < tables.Count; from++)
            {
                var table = tables[from];
                if (table.IsInclude(reference))
                {
                    var innerFrom = 0;
                    return TryGetTableGroup(table.InnerGroups, ref innerFrom, reference) ?? table;
                }
            }

            return null;
        }

        private static TableGroup? TryGetTableGroup(IList<TableGroup> tables, ICellsGroup cellsGroup)
        {
            for (var from = 0; from < tables.Count; from++)
            {
                var table = tables[from];
                if (table.IsInclude(cellsGroup))
                {
                    return TryGetTableGroup(table.InnerGroups, cellsGroup) ?? table;
                }
            }

            return null;
        }

        /// <summary>
        /// Метод производит обновление позиций всех внутренних объектов Worksheets.
        /// </summary>
        private void UpdateWorksheets()
        {
            foreach (var worksheet in this.WorksheetHash)
            {
                worksheet.Update();
            }
        }

        /// <summary>
        /// Метод для инициализации структуры Excel.
        /// </summary>
        private ValidationResult InitializeWorksheets()
        {
            var (result, worksheetHash) = this.parser.ParseDocument(this.ExcelDocument);

            if (worksheetHash is not null)
            {
                this.worksheetHash = worksheetHash;
                this.worksheetsDictionary = new HashSet<Worksheet, WorksheetElement>(x => x.Element, worksheetHash);
            }

            return result;
        }

        /// <summary>
        /// Метод производит привязку плейсхолдеров к таблицам.
        /// </summary>
        private void AttachPlaceholders(IList<IPlaceholder> placeholders)
        {
            foreach (var placeholder in placeholders)
            {
                var worksheet = this.TryGetWorksheetByPlaceholder(placeholder)
                    ?? throw new InvalidOperationException("Can't find Worksheet with position " +
                        TextPosition(placeholder.Info.Get<IList>(OpenXmlHelper.PositionField))); ;
                var baseElement = GetElementByPlaceholder(this.WorkbookPart, placeholder);

                // Если плейсхолдер относится к Relationship, то baseElement is null
                if (baseElement is null)
                {
                    var elementId = placeholder.Info.Get<IList>(OpenXmlHelper.PositionField)?.Cast<object>().Last().ToString();

                    var hyperlink = worksheet.Hyperlinks.FirstOrDefault(x => x.Element.Id == elementId)
                        ?? throw new InvalidOperationException("Can't find Hyperlink with position " +
                            TextPosition(placeholder.Info.Get<IList>(OpenXmlHelper.PositionField)));
                    baseElement = hyperlink.Element;

                    AddPlaceholderToTable(
                        worksheet.Tables,
                        hyperlink.Reference,
                        hyperlink,
                        placeholder,
                        (t, r, p) => t.AddHyperlinkPlaceholder(r, p));
                }
                else
                {
                    var type = baseElement.GetType();
                    if (type == typeof(Cell))
                    {
                        // плейсхолдер в ячейке с текстом
                        var cell = (Cell) baseElement;
                        if (cell.CellReference is not { Value: not null })
                        {
                            continue;
                        }

                        var row = worksheet.Rows.FirstOrDefault(x => x.IsInclude(cell.CellReference.Value))
                            ?? throw new InvalidOperationException("Can't find Row with position " +
                                TextPosition(placeholder.Info.Get<IList>(OpenXmlHelper.PositionField)));
                        AddPlaceholderToTable(
                            worksheet.Tables,
                            cell.CellReference.Value,
                            row,
                            placeholder,
                            (t, r, p) => t.AddRowPlaceholder(r, p));
                    }
                    else if (type == typeof(A.Paragraph))
                    {
                        // плейсхолдер в надписи
                        var paragraph = (A.Paragraph) baseElement;
                        var anchor = worksheet.Anchors.FirstOrDefault(x => x.HasChildElement(paragraph))
                            ?? throw new InvalidOperationException("Can't find Anchor with position " +
                                TextPosition(placeholder.Info.Get<IList>(OpenXmlHelper.PositionField)));
                        baseElement = anchor.Element;

                        AddPlaceholderToTable(
                            worksheet.Tables,
                            anchor.Reference,
                            anchor,
                            placeholder,
                            (t, r, p) => t.AddAnchorPlaceholder(r, p));
                    }
                }

                placeholder.Info.Add(OpenXmlHelper.BaseElementField, baseElement);
            }
        }

        private static void AddPlaceholderToTable<SourceType>(
            IList<TableGroup> tables,
            string reference,
            SourceType source,
            IPlaceholder placeholder,
            Action<TableGroup, SourceType, IPlaceholder> addPlaceholderFunc)
        {
            TableGroup? tableToAdd = null;
            bool continueSearch;

            do
            {
                continueSearch = false;
                foreach (var table in tables)
                {
                    if (table.IsInclude(reference))
                    {
                        tableToAdd = table;
                        if (table.InnerGroups.Count > 0)
                        {
                            tables = table.InnerGroups;
                            continueSearch = true;
                        }

                        break;
                    }
                }
            } while (continueSearch);

            if (tableToAdd is not null
                && tableToAdd.Type != TableGroupType.Table)
            {
                addPlaceholderFunc(tableToAdd, source, placeholder);
            }
        }

        /// <summary>
        /// Метод получает объект <see cref="WorksheetElement"/> по плейсхолдеру.
        /// </summary>
        /// <param name="placeholder">Плейсхолдер.</param>
        /// <returns>Объект <see cref="WorksheetElement"/>, к которому принадлежит данный плейсхолдер.</returns>
        private WorksheetElement? TryGetWorksheetByPlaceholder(IPlaceholder placeholder)
        {
            var position = placeholder.Info.TryGet<IList>(OpenXmlHelper.PositionField);
            if (position is null)
            {
                return null;
            }

            OpenXmlPart mainPart = this.WorkbookPart;
            for (var i = 0; i < position.Count; i++)
            {
                if (position[i] is int index)
                {
                    if (index < 0)
                    {
                        mainPart = mainPart.Parts.ToArray()[~index].OpenXmlPart;

                        if (mainPart.RootElement is Worksheet worksheet)
                        {
                            return this.WorksheetsDictionary[worksheet];
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            return null;
        }

        private WorksheetPart? TryGetWorksheetPartByPlaceholder(IPlaceholder placeholder)
        {
            var position = placeholder.Info.TryGet<IList>(OpenXmlHelper.PositionField);
            if (position is null)
            {
                return null;
            }

            OpenXmlPart mainPart = this.WorkbookPart;
            for (var i = 0; i < position.Count; i++)
            {
                if (position[i] is int index)
                {
                    if (index < 0)
                    {
                        mainPart = mainPart.Parts.ToArray()[~index].OpenXmlPart;

                        if (mainPart is WorksheetPart worksheetPart)
                        {
                            return worksheetPart;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            return null;
        }

        private async Task ReplacePlaceholdersInRowAsync(
            IPlaceholderReplacementContext context,
            TableGroup tableGroup,
            TableGroupInstance tableGroupInstance,
            IPlaceholderRow row)
        {
            Row? prevRow = null;
            for (var i = 0; i < tableGroupInstance.Rows.Length; i++)
            {
                var newRow = tableGroupInstance.Rows[i];
                var copyRow = tableGroup.Rows[i];

                prevRow = newRow.Element;

                if (this.WithExtensions)
                {
                    this.extensionContext.CurrentRowElement = prevRow;
                }

                // Берем все плейсхолдеры, относящиеся к текущей строке внутренней таблицы
                foreach (var placeholders in
                    tableGroup.GetRowPlaceholders(copyRow)
                        .GroupBy(x => x.Info.Get<OpenXmlElement>(OpenXmlHelper.BaseElementField)))
                {
                    var baseElement = placeholders.Key;
                    if (baseElement is null)
                    {
                        continue;
                    }

                    var newCell = TryGetRelativeElement<Cell>(copyRow.Element, baseElement, newRow.Element);

                    var replacements = new List<IPlaceholderReplacement>();
                    foreach (var placeholder in placeholders)
                    {
                        var newValue = await ((ITablePlaceholderType) placeholder.Type)
                                .ReplaceAsync(context, placeholder, row, context.CancellationToken)
                            ?? PlaceholderValue.Empty;

                        replacements.Add(new PlaceholderReplacement(placeholder, newValue));
                    }

                    if (newCell is not null)
                    {
                        await this.ReplaceElementsInCellAsync(newCell, true, context.CancellationToken, [.. replacements]);
                    }

                    if (this.WithExtensions)
                    {
                        this.extensionContext.CurrentRowElement = null;
                    }
                }
            }

            await this.ReplaceRowPlaceholdersForHyperlinksAsync(context, tableGroup, tableGroupInstance, row);
            await this.ReplaceRowPlaceholdersForAnchorsAsync(context, tableGroup, tableGroupInstance, row);
        }

        private async Task ReplaceRowPlaceholdersForHyperlinksAsync(
            IPlaceholderReplacementContext context,
            TableGroup tableGroup,
            TableGroupInstance tableGroupInstance,
            IPlaceholderRow row)
        {
            for (var i = 0; i < tableGroup.Hyperlinks.Count; i++)
            {
                var newHyperlink = tableGroupInstance.Hyperlinks[i];
                var hyperlink = tableGroup.Hyperlinks[i];

                var hypElem = hyperlink.Element;
                var newHypElem = newHyperlink.Element;
                var placeholders = tableGroup.GetHyperlinkPlaceholders(hyperlink);

                if (placeholders.Count > 0
                    && tableGroup.Worksheet.Element.WorksheetPart is { } worksheetPart
                    && hypElem.Id is { Value: not null } hypId)
                {
                    var replacements = await ReplaceRowPlaceholdersAsync(context, row, placeholders);
                    var position = replacements[0].Placeholder.Info.Get<IList>(OpenXmlHelper.PositionField);
                    newHypElem.Id = await this.ReplaceElementsInRelationshipsWithCopyAsync(
                        worksheetPart,
                        (string) position![position.Count - 1]!,
                        hypId.Value,
                        replacements);
                }
            }
        }

        private async Task ReplaceRowPlaceholdersForAnchorsAsync(
            IPlaceholderReplacementContext context,
            TableGroup tableGroup,
            TableGroupInstance tableGroupInstance,
            IPlaceholderRow row)
        {
            for (var i = 0; i < tableGroup.Anchors.Count; i++)
            {
                var newAnchor = tableGroupInstance.Anchors[i];
                var anchor = tableGroup.Anchors[i];

                var placeholders = tableGroup.GetAnchorPlaceholders(anchor);

                if (placeholders.Count > 0)
                {
                    var replacements = await ReplaceRowPlaceholdersAsync(context, row, placeholders);
                    await this.ReplaceElementsInCompositeElementAsync(newAnchor.GetPlaceholderBaseElement(), null, context.CancellationToken, replacements);
                }
            }
        }

        private static async Task<IPlaceholderReplacement[]> ReplaceRowPlaceholdersAsync(
            IPlaceholderReplacementContext context,
            IPlaceholderRow row,
            IList<IPlaceholder> placeholders)
        {
            var result = new List<IPlaceholderReplacement>(placeholders.Count);
            foreach (var placeholder in placeholders)
            {
                result.Add(
                    new PlaceholderReplacement(
                        placeholder,
                        await ((ITablePlaceholderType) placeholder.Type)
                            .ReplaceAsync(context, placeholder, row, context.CancellationToken)
                        ?? PlaceholderValue.Empty));
            }

            return [.. result];
        }

        private uint GetCellNumberFormat(Cell cell)
        {
            if (cell.StyleIndex is null)
            {
                return 0;
            }

            return this.Stylesheet.CellFormats?.Elements<CellFormat>().ElementAt(Convert.ToInt32(cell.StyleIndex.Value)).NumberFormatId ?? 0;
        }

        /// <summary>
        /// Создать объект Picture, который оборачивает. Бинарные данные при этом добавляются в страницу Excel.
        /// </summary>
        /// <param name="worksheetPart">Объект, который содержит страницу Excel (вкладка документа).</param>
        /// <param name="imageBytes">Содержимое изображения в байтах.</param>
        /// <param name="imagePartType">Тип изображения.</param>
        /// <param name="existingProperties">
        /// Существующие настройки формата фигуры, которые надо перенести, или <c>null</c>, если таких настроек нет.
        /// Обычно это настройки объекта "Надпись".
        /// </param>
        /// <param name="width">Ширина изображения в пикселях для 96dpi, заданная в плейсхолдере.</param>
        /// <param name="height">Высота изображения в пикселях для 96dpi, заданная в плейсхолдере.</param>
        /// <param name="reformat">
        /// Признак, заданный в плейсхолдере, который указывает на то, что исходные настройки форматирования
        /// <paramref name="existingProperties"/> придётся по большей части выбросить.
        /// </param>
        /// <param name="alternativeText">Замещающий текст.</param>
        private static Xdr.Picture CreatePictureFromImage(
            WorksheetPart worksheetPart,
            byte[] imageBytes,
            PartTypeInfo imagePartType,
            Xdr.ShapeProperties? existingProperties,
            double width,
            double height,
            bool reformat,
            string? alternativeText,
            ISerializableObject info)
        {
            // добавляем изображение в документ и получаем его relationshipId
            var drawingsPart = worksheetPart.DrawingsPart ?? worksheetPart.AddNewPart<DrawingsPart>();
            if (worksheetPart.Worksheet?.ChildElements.OfType<Drawing>().Any() is not true)
            {
                worksheetPart.Worksheet?.AppendChild(new Drawing { Id = worksheetPart.GetIdOfPart(drawingsPart) });
            }

            var worksheetDrawing = drawingsPart.WorksheetDrawing;
            if (worksheetDrawing is null)
            {
                worksheetDrawing = new Xdr.WorksheetDrawing();
                drawingsPart.WorksheetDrawing = worksheetDrawing;
            }

            var imagePart = drawingsPart.AddImagePart(imagePartType);
            imagePart.FeedData(new MemoryStream(imageBytes));

            // берём из надписи или генерируем свойства фигуры для картинки
            Xdr.ShapeProperties actualProperties;
            if (!reformat && existingProperties is not null)
            {
                actualProperties = (Xdr.ShapeProperties) existingProperties.CloneNode(deep: true);
            }
            else
            {
                actualProperties = new Xdr.ShapeProperties(
                    new A.Transform2D(
                        new A.Offset { X = 0L, Y = 0L },
                        new A.Extents { Cx = OpenXmlHelper.DefaultExtentsCx, Cy = OpenXmlHelper.DefaultExtentsCy }
                    ),
                    new A.PresetGeometry { Preset = A.ShapeTypeValues.Rectangle });
            }

            // определяем размеры картинки
            var extents = actualProperties.Transform2D?.Extents;
            if (extents is not null)
            {
                if (width > 0.0)
                {
                    extents.Cx = OpenXmlHelper.PixelsToEmu(width);
                }

                if (height > 0.0)
                {
                    extents.Cy = OpenXmlHelper.PixelsToEmu(height);
                }

                if (reformat && (width <= 0.0 || height <= 0.0))
                {
                    if (width <= 0.0)
                    {
                        var imageWidth = info.TryGet<int>("ActualWidth");
                        var imageResX = info.TryGet<double>("ActualVerticalResolution");

                        extents.Cx = OpenXmlHelper.PixelsToEmu(imageWidth, imageResX);
                    }

                    if (height <= 0.0)
                    {
                        var imageHeight = info.TryGet<int>("ActualHeight");
                        var imageResY = info.TryGet<double>("ActualHorizontalResolution");

                        extents.Cy = OpenXmlHelper.PixelsToEmu(imageHeight, imageResY);
                    }
                }
            }

            // определяем уникальный идентификатор картинки
            var nvps = worksheetDrawing
                .Descendants<Xdr.NonVisualDrawingProperties>()
                .ToArray();

            var nvpId = nvps.Length > 0 ? nvps.Max(p => p.Id?.Value ?? 0) + 1u : 1u;

            // создаём объект Picture, в который картинка обёрнута
            return new Xdr.Picture(
                new Xdr.NonVisualPictureProperties(
                    new Xdr.NonVisualDrawingProperties { Id = nvpId, Name = "Image " + nvpId, Title = LocalizeFormat(alternativeText) },
                    new Xdr.NonVisualPictureDrawingProperties(new A.PictureLocks { NoChangeAspect = true })
                ),
                new Xdr.BlipFill(
                    new A.Blip
                    {
                        Embed = drawingsPart.GetIdOfPart(imagePart),
                        CompressionState = A.BlipCompressionValues.Print
                    },
                    new A.Stretch(new A.FillRectangle())
                ),
                actualProperties);
        }

        private ValueTask<(bool, int)> ReplaceGroupPlaceholdersAsync(
            IPlaceholderReplacementContext context,
            TableGroup tableGroup,
            TableGroupInstance? parentTableGroup = null,
            IEditablePlaceholderTable? placeholderTable = null,
            IEnumerable<IPlaceholderRow>? rows = null,
            int groupLevel = 0)
        {
            return tableGroup.Type switch
            {
                TableGroupType.Jump => this.ReplaceJumpGroupPlaceholdersAsync(
                                        context,
                                        tableGroup,
                                        parentTableGroup,
                                        placeholderTable,
                                        rows),
                TableGroupType.Row => this.ReplaceRowGroupPlaceholdersAsync(
                                        context,
                                        tableGroup,
                                        parentTableGroup,
                                        placeholderTable,
                                        rows),
                TableGroupType.Group => this.ReplaceGroupGroupPlaceholdersAsync(
                                        context,
                                        tableGroup,
                                        parentTableGroup,
                                        placeholderTable,
                                        rows,
                                        groupLevel),
                TableGroupType.Table => this.ReplaceTableGroupPlaceholdersAsync(
                                        context,
                                        tableGroup,
                                        parentTableGroup),
                _ => throw new ArgumentException("TableGroup.Type has invalid value"),
            };
        }

        private async ValueTask<(bool, int)> ReplaceTableGroupPlaceholdersAsync(
            IPlaceholderReplacementContext context,
            TableGroup tableGroup,
            TableGroupInstance? parentTableGroup = null)
        {
            var hasTableRows = false;
            // Определяет, насколько была смещена таблица
            var moveByLocal = 0;
            TableGroupInstance? tableGroupInstance = null;
            if (parentTableGroup is not null)
            {
                // Не клонируем строки
                tableGroupInstance = new TableGroupInstance(tableGroup, parentTableGroup, Array.Empty<RowCellGroup>());
                tableGroupInstance.CreateClone();
            }

            if (tableGroup.InnerGroups.Count > 0)
            {
                foreach (var innerGroup in tableGroup.InnerGroups)
                {
                    var (hasGroupRows, moveByGroup) = await this.ReplaceGroupPlaceholdersAsync(
                        context,
                        innerGroup,
                        parentTableGroup);

                    moveByLocal += moveByGroup;
                    hasTableRows |= hasGroupRows;

                    if (parentTableGroup is not null
                        && moveByGroup != 0)
                    {
                        tableGroupInstance?.Move(moveByGroup, innerGroup.Top);

                        var from = innerGroup.Top - parentTableGroup.Rows[0].Bottom;
                        var to = tableGroup.Bottom - parentTableGroup.Rows[0].Bottom + tableGroup.Height;
                        for (var i = to - 1; i >= from; i--)
                        {
                            parentTableGroup.Rows[i]?.Move(moveByGroup);
                        }
                    }
                }
            }

            if (hasTableRows)
            {
                tableGroupInstance?.InsertClone();
            }
            // Если в таблице нет строк, удаляем таблицу целиком.
            else
            {
                // Если нет строк с результатом, то удаляем строки таблицы
                if (parentTableGroup is null)
                {
                    // Удаляем исходную таблицу
                    tableGroup.Clear();
                }
                else
                {
                    // Удаляем строки таблицы из копии
                    var from = tableGroup.Bottom - parentTableGroup.Rows[0].Bottom;
                    var to = from + tableGroup.Height;
                    var expandBy = 0;
                    for (var i = from; i < to; i++)
                    {
                        if (parentTableGroup.Rows[i] is not null)
                        {
                            parentTableGroup.Rows[i] = null;
                            expandBy--;
                        }
                    }

                    // Дополнительно смещаем на число удаленных таблицей строк
                    if (expandBy != 0)
                    {
                        tableGroupInstance?.ExpandFormulaSources(expandBy);
                    }
                }

                moveByLocal = -tableGroup.Height;
            }

            return (hasTableRows, moveByLocal);
        }

        private async ValueTask<(bool, int)> ReplaceGroupGroupPlaceholdersAsync(
            IPlaceholderReplacementContext context,
            TableGroup tableGroup,
            TableGroupInstance? parentTableGroup = null,
            IEditablePlaceholderTable? placeholderTable = null,
            IEnumerable<IPlaceholderRow>? rows = null,
            int groupLevel = 0)
        {
            var needTableExtensions = placeholderTable is null && this.WithExtensions;
            List<Row>? allElements = this.WithExtensions
                ? []
                : null;
            List<Row>? currentRowElements = null;

            placeholderTable ??= await FillTableAsync(
                context,
                tableGroup);

            using var _ = needTableExtensions
                ? this.extensionContext!.ExecuteInTableContext(placeholderTable, new(null, tableGroup.Worksheet.Element))
                : null;

            if (needTableExtensions)
            {
                await this.BeforeTableReplaceAsync(context);
            }

            rows ??= placeholderTable?.Rows;

            var hasRows = rows is not null
                && rows.Any();

            // Насколько текущая таблица сдвинула остальные
            var moveTotal = 0;

            var tableGroupInstance = new TableGroupInstance(tableGroup, parentTableGroup);
            if (hasRows)
            {
                // Насколько нужно сдвигать текущую строку
                var moveLoсal = 0;
                await placeholderTable!.FillHorizontalGroupsAsync(
                    context,
                    rows,
                    groupLevel);

                if (this.WithExtensions)
                {
                    currentRowElements = [];
                }

                foreach (var rowGroup in rows!.GroupBy(x => x.HorizontalGroup).ToArray())
                {
                    var row = rowGroup.First();
                    context.SetPerformingRow(placeholderTable!.Name, row);

                    using var rowContext = this.WithExtensions
                        ? this.extensionContext.ExecuteInRowContext(row, true)
                        : null;

                    if (this.WithExtensions)
                    {
                        await this.BeforeRowReplaceAsync(context);
                    }

                    tableGroupInstance.CreateClone();
                    tableGroupInstance.Move(moveLoсal);

                    await this.ReplacePlaceholdersInRowAsync(
                        context,
                        tableGroup,
                        tableGroupInstance,
                        row);

                    for (var i = 0; i < tableGroup.InnerGroups.Count; i++)
                    {
                        var innerGroup = tableGroup.InnerGroups[i];

                        var (_, moveGroup) = await this.ReplaceGroupPlaceholdersAsync(
                            context,
                            innerGroup,
                            tableGroupInstance,
                            placeholderTable,
                            rowGroup,
                            groupLevel + 1);

                        moveTotal += moveGroup;
                        tableGroupInstance.Move(moveGroup, innerGroup.Top);

                        if (this.WithExtensions)
                        {
                            currentRowElements!.AddRange(this.extensionContext.TableElements);
                        }
                    }

                    tableGroupInstance.InsertClone();
                    if (this.WithExtensions)
                    {
                        currentRowElements!.AddRange(tableGroupInstance.Rows.Where(x => x is not null).Select(x => x.Element));
                    }

                    moveTotal += tableGroup.Height;
                    moveLoсal = moveTotal;

                    if (this.WithExtensions)
                    {
                        this.extensionContext.SetRowData(currentRowElements);
                        await this.AfterRowReplaceAsync(context);
                        allElements!.AddRange(currentRowElements!);
                        currentRowElements!.Clear();
                    }
                }
            }

            tableGroupInstance.ExpandFormulaSources();

            // Вычитаем число строк текущей таблицы, т.к. всегда есть первая строка.
            moveTotal -= tableGroup.Height;

            if (needTableExtensions)
            {
                this.extensionContext!.SetTableData(new(allElements, tableGroup.Worksheet.Element));
                await this.AfterTableReplaceAsync(context);
            }

            // Очищаем старые элементы
            if (parentTableGroup is null)
            {
                tableGroup.Clear();
            }

            return (hasRows, moveTotal);
        }

        private async ValueTask<(bool, int)> ReplaceRowGroupPlaceholdersAsync(
            IPlaceholderReplacementContext context,
            TableGroup tableGroup,
            TableGroupInstance? parentTableGroup = null,
            IEditablePlaceholderTable? placeholderTable = null,
            IEnumerable<IPlaceholderRow>? rows = null)
        {
            var needTableExtensions = placeholderTable is null && this.WithExtensions;
            List<Row>? allElements = this.WithExtensions
                ? []
                : null;
            List<Row>? currentRowElements = null;

            placeholderTable ??= await FillTableAsync(
                context,
                tableGroup);

            using var _ = needTableExtensions
                ? this.extensionContext!.ExecuteInTableContext(placeholderTable, new(null, tableGroup.Worksheet.Element))
                : null;

            if (needTableExtensions)
            {
                await this.BeforeTableReplaceAsync(context);
            }

            rows ??= placeholderTable?.Rows;

            var hasRows = rows is not null
                && rows.Any();

            // Насколько текущая таблица сдвинула остальные
            var moveTotal = 0;

            if (this.WithExtensions)
            {
                currentRowElements = [];
            }

            var tableGroupInstance = new TableGroupInstance(tableGroup, parentTableGroup);
            if (hasRows)
            {
                var number = placeholderTable!.Info.TryGet<int>(PlaceholderHelper.NumberKey);

                // Насколько нужно сдвигать текущую строку
                var moveLoсal = 0;

                // для каждой строки заменяем плейсхолдеры в строке-шаблоне rowText
                foreach (var row in rows!)
                {
                    row.Number = ++number;
                    context.SetPerformingRow(placeholderTable.Name, row);

                    using var rowContext = this.WithExtensions
                        ? this.extensionContext.ExecuteInRowContext(row, false)
                        : null;

                    if (this.WithExtensions)
                    {
                        await this.BeforeRowReplaceAsync(context);
                    }

                    tableGroupInstance.CreateClone();
                    tableGroupInstance.Move(moveLoсal);

                    await this.ReplacePlaceholdersInRowAsync(
                        context,
                        tableGroup,
                        tableGroupInstance,
                        row);

                    for (var i = 0; i < tableGroup.InnerGroups.Count; i++)
                    {
                        var innerGroup = tableGroup.InnerGroups[i];
                        if (innerGroup.Type == TableGroupType.Table)
                        {
                            var (_, moveGroup) = await this.ReplaceTableGroupPlaceholdersAsync(
                                context,
                                innerGroup,
                                tableGroupInstance);

                            moveTotal += moveGroup;
                            tableGroupInstance.Move(moveGroup, innerGroup.Top);

                            if (this.WithExtensions)
                            {
                                currentRowElements!.AddRange(this.extensionContext.TableElements);
                            }
                        }
                        else
                        {
                            // TODO Placeholders - строки не содержат группы и другие строки
                        }
                    }

                    tableGroupInstance.InsertClone();
                    if (this.WithExtensions)
                    {
                        currentRowElements!.AddRange(tableGroupInstance.Rows.Where(x => x is not null).Select(x => x.Element));
                    }

                    moveTotal += tableGroup.Height;
                    moveLoсal = moveTotal;

                    if (this.WithExtensions)
                    {
                        this.extensionContext.SetRowData(currentRowElements);
                        await this.AfterRowReplaceAsync(context);
                        allElements!.AddRange(currentRowElements!);
                        currentRowElements!.Clear();
                    }
                }

                placeholderTable.Info[PlaceholderHelper.NumberKey] = number;
            }

            tableGroupInstance.ExpandFormulaSources();
            // Вычитаем число строк текущей таблицы, т.к. всегда есть первая строка.
            moveTotal -= tableGroup.Height;

            if (this.WithExtensions)
            {
                if (needTableExtensions)
                {
                    this.extensionContext.SetTableData(new(allElements, tableGroup.Worksheet.Element));
                    await this.AfterTableReplaceAsync(context);
                }
            }

            // Очищаем старые элементы
            if (parentTableGroup is null)
            {
                tableGroup.Clear();
            }

            return (hasRows, moveTotal);
        }

        private async ValueTask<(bool, int)> ReplaceJumpGroupPlaceholdersAsync(
            IPlaceholderReplacementContext context,
            TableGroup tableGroup,
            TableGroupInstance? parentTableGroup = null,
            IEditablePlaceholderTable? placeholderTable = null,
            IEnumerable<IPlaceholderRow>? rows = null)
        {
            var needTableExtensions = placeholderTable is null && this.WithExtensions;
            List<Row>? allElements = this.WithExtensions
                ? []
                : null;
            List<Row>? currentRowElements = null;

            placeholderTable ??= await FillTableAsync(
                context,
                tableGroup);

            using var _ = needTableExtensions
                ? this.extensionContext!.ExecuteInTableContext(placeholderTable, new(null, tableGroup.Worksheet.Element))
                : null;

            if (needTableExtensions)
            {
                await this.BeforeTableReplaceAsync(context);
            }

            rows ??= placeholderTable?.Rows;

            var hasRows = rows is not null
                && rows.Any();

            // Список строк, которые будут клонироваться для генерации строки текущей таблицы. Если null, то будут использоваться строки текущей таблицы
            IList<RowCellGroup> tableRows = parentTableGroup is null
                ? tableGroup.Rows
                : parentTableGroup.Rows.Where(x => x.Bottom >= tableGroup.Bottom && x.Top <= tableGroup.Top).ToArray();

            // Если нет строк для замены, то заменяем текст ячеек с табличными плейсхолдерами на String.Empty
            if (hasRows)
            {
                var number = placeholderTable!.Info.TryGet<int>(PlaceholderHelper.NumberKey);

                // Индекс смещения внутри таблицы
                var moveInTable = 0;

                var currentCopyRowElements = new Row[tableRows.Count];
                for (var i = 0; i < tableRows.Count; i++)
                {
                    currentCopyRowElements[i] = (Row) tableRows[i].Element.CloneNode(true);
                }

                var tableGroupInstance = new TableGroupInstance(tableGroup);

                if (this.WithExtensions)
                {
                    currentRowElements = [];
                }

                var notFirstRow = false;
                foreach (var row in rows!)
                {
                    row.Number = ++number;

                    using var rowContext = this.WithExtensions
                        ? this.extensionContext.ExecuteInRowContext(row, false)
                        : null;

                    if (this.WithExtensions)
                    {
                        await this.BeforeRowReplaceAsync(context);
                    }

                    // Обработку ведем по элементам строк
                    for (var i = 0; i < tableRows.Count; i++)
                    {
                        // Строка, в которую производится вставка
                        var copyRow = tableRows[i];
                        var originalRow = tableGroup.Rows[i];
                        var currentCopyRowElement = currentCopyRowElements[i];

                        // Если не первая строка, то ищем строку дальше в таблице
                        if (notFirstRow)
                        {
                            copyRow = parentTableGroup is null
                                ? tableGroup.Worksheet.GetRow(copyRow.Top + moveInTable)
                                : parentTableGroup.Rows.FirstOrDefault(x => x.Top == copyRow.Top + moveInTable);

                            // Если в таблице отсутствует элемент строки с данным индексом, то делаем копию оригинального элемента
                            if (copyRow is null)
                            {
                                // Создаем строку как копию оригинальной строки с плейсхолдерами.
                                // Сдвигаем строку на "смещение внутри jump таблицы"+"смещение jump таблицы"
                                copyRow = new RowCellGroup(
                                    (Row) currentCopyRowElement.CloneNode(false),
                                    tableGroup.Worksheet);
                                copyRow.ParseElement();
                                copyRow.Move(moveInTable);
                                copyRow.Update(); // Делаем Update сразу для новой строки в таблице Jump, чтобы другие Jump таблицы видели эту строку
                                copyRow.Move(tableRows[i].MoveBy);
                                copyRow.Insert();
                            }
                        }

                        if (this.WithExtensions)
                        {
                            this.extensionContext.CurrentRowElement = copyRow.Element;
                            currentRowElements!.Add(copyRow.Element);
                        }

                        // Берем все плейсхолдеры, относящиеся к текущей строке
                        foreach (var placeholders in
                            tableGroup.GetRowPlaceholders(originalRow)
                                .GroupBy(x => x.Info.Get<OpenXmlElement>(OpenXmlHelper.BaseElementField)))
                        {
                            var baseElement = placeholders.Key;
                            if (baseElement is null)
                            {
                                continue;
                            }

                            var replacements = new List<IPlaceholderReplacement>();
                            foreach (var placeholder in placeholders.ToArray())
                            {
                                var newValue = await ((ITablePlaceholderType) placeholder.Type)
                                        .ReplaceAsync(context, placeholder, row, context.CancellationToken)
                                    ?? PlaceholderValue.Empty;

                                replacements.Add(new PlaceholderReplacement(placeholder, newValue));
                            }

                            if (notFirstRow)
                            {
                                var originalCell = TryGetRelativeElement<Cell>(originalRow.Element, baseElement, currentCopyRowElement);
                                var newCell = (Cell) NotNullOrThrow(originalCell).CloneNode(true);
                                await this.ReplaceElementsInCellAsync(newCell, true, context.CancellationToken, [.. replacements]);
                                copyRow.SetCell(newCell);
                            }
                            else
                            {
                                // Если это отдельная таблица, меняем оригинальную ячейку, иначе ищем ее копию в копии строки
                                var newCell = parentTableGroup is null
                                    ? (Cell) baseElement
                                    : TryGetRelativeElement<Cell>(originalRow.Element, baseElement, copyRow.Element);
                                await this.ReplaceElementsInCellAsync(NotNullOrThrow(newCell), true, context.CancellationToken, [.. replacements]);
                            }
                        }
                    }

                    // Вручную копируем каждую гиперссылку и якорь, чтобы произвести в них замену плейсхолдеров.
                    for (var i = 0; i < tableGroupInstance.Hyperlinks.Length; i++)
                    {
                        var hyperlink = (HyperlinkCellGroup) tableGroup.Hyperlinks[i].Clone();
                        hyperlink.Insert();
                        hyperlink.Move(moveInTable);
                        tableGroupInstance.Hyperlinks[i] = hyperlink;
                    }

                    for (var i = 0; i < tableGroupInstance.Anchors.Length; i++)
                    {
                        var anchor = (AnchorCellGroup) tableGroup.Anchors[i].Clone();
                        anchor.Insert();
                        anchor.Move(moveInTable);
                        tableGroupInstance.Anchors[i] = anchor;
                    }

                    await this.ReplaceRowPlaceholdersForHyperlinksAsync(context, tableGroup, tableGroupInstance, row);
                    await this.ReplaceRowPlaceholdersForAnchorsAsync(context, tableGroup, tableGroupInstance, row);

                    if (this.WithExtensions)
                    {
                        this.extensionContext.SetRowData(currentRowElements);
                        await this.AfterRowReplaceAsync(context);
                        allElements!.AddRange(currentRowElements!);
                        currentRowElements!.Clear();
                    }

                    moveInTable += tableRows.Count;
                    notFirstRow = true;
                }

                placeholderTable.Info[PlaceholderHelper.NumberKey] = number;
            }
            else
            {
                // Если нет строк для замены, то заменяем текст ячеек с табличными плейсхолдерами на String.Empty
                // Обработку ведем по элементам строк
                for (var i = 0; i < tableRows.Count; i++)
                {
                    var row = tableRows[i];
                    var originalRow = tableGroup.Rows[i];
                    foreach (var placeholders in
                        tableGroup.GetRowPlaceholders(originalRow)
                            .GroupBy(x => x.Info.Get<OpenXmlElement>(OpenXmlHelper.BaseElementField)))
                    {
                        var baseElement = placeholders.Key;
                        if (baseElement is null)
                        {
                            continue;
                        }

                        var cell = (Cell) baseElement;
                        (var sharedString, var sharedStringID) = this.CopySharedString(int.Parse(cell.InnerText));
                        ClearTextInSharedString(sharedString);
                        cell.CellValue ??= new();
                        cell.CellValue.Text = sharedStringID.ToString();
                    }
                }
            }

            if (needTableExtensions)
            {
                this.extensionContext!.SetTableData(new(allElements, tableGroup.Worksheet.Element));
                await this.AfterTableReplaceAsync(context);
            }

            // Очищаем старые элементы
            if (parentTableGroup is null)
            {
                tableGroup.Clear();
            }

            // Данный вид групп никогда не сдвигает остальные группы
            return (hasRows, 0);
        }

        private static async ValueTask<IEditablePlaceholderTable?> FillTableAsync(
            IPlaceholderReplacementContext context,
            TableGroup group,
            IEditablePlaceholderTable? table = null,
            int groupLevel = 0)
        {
            var tablePlaceholders = group.GetAllPlaceholders();
            tablePlaceholders.Sort((x, y) =>
            {
                var result = string.Compare(
                    TextPosition(x.Info.Get<IList>(OpenXmlHelper.PositionField)),
                    TextPosition(y.Info.Get<IList>(OpenXmlHelper.PositionField)),
                    StringComparison.Ordinal);

                return result == 0
                    ? x.Info.TryGet<int>(OpenXmlHelper.OrderField).CompareTo(y.Info.TryGet<int>(OpenXmlHelper.OrderField))
                    : result;
            });

            for (var i = 0; i < tablePlaceholders.Count; i++)
            {
                var placeholder = tablePlaceholders[i];
                if (placeholder.Type is ITablePlaceholderType tableType)
                {
                    table = await tableType.FillTableAsync(
                        context,
                        placeholder,
                        table);

                    if (group.Type == TableGroupType.Group
                        && table is not null)
                    {
                        table.AddHorizontalGroupPlaceholder(
                            placeholder,
                            groupLevel);
                    }
                }
            }

            foreach (var innerGroup in group.InnerGroups)
            {
                if (innerGroup.Type != TableGroupType.Table)
                {
                    await FillTableAsync(
                        context,
                        innerGroup,
                        table,
                        innerGroup.Type == TableGroupType.Group ? groupLevel + 1 : 0);
                }
            }

            return table;
        }

        #endregion

        #region SharedStrings Private Methods

        /// <summary>
        /// Метод для получения значения <see cref="SharedStringItem"/> по его ID.
        /// </summary>
        /// <param name="id">ID искомой <see cref="SharedStringItem"/>.</param>
        /// <returns></returns>
        private string GetSharedString(int id)
        {
            return this.SharedStringTable.ElementAt(id).InnerText;
        }

        /// <summary>
        /// Метод для замены плейсхолдеров в <see cref="SharedStringItem"/>.
        /// </summary>
        /// <param name="sharedString">Объект <see cref="SharedStringItem"/></param>
        /// <param name="replacements">Плейсхолдеры для замены.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        private Task ReplaceElementsInSharedStringAsync(
            SharedStringItem sharedString,
            CancellationToken cancellationToken = default,
            params IReadOnlyList<IPlaceholderReplacement> replacements)
        {
            return this.ReplaceElementsInCompositeElementAsync(sharedString, null, cancellationToken, replacements);
        }

        /// <summary>
        /// Метод для очистки текста в в <see cref="SharedStringItem"/>.
        /// </summary>
        /// <param name="sharedString"><see cref="SharedStringItem"/>, в которой очищается текст.</param>
        private static void ClearTextInSharedString(SharedStringItem sharedString)
        {
            foreach (var text in sharedString.Descendants<Text>())
            {
                text.Text = string.Empty;
            }
        }

        /// <summary>
        /// Метод производит копирование <see cref="SharedStringItem"/> с указанным ID и возвращает новый объект <see cref="SharedStringItem"/> с его ID.
        /// </summary>
        /// <param name="id">ID копируемой <see cref="SharedStringItem"/>.</param>
        /// <returns></returns>
        private Tuple<SharedStringItem, int> CopySharedString(int id)
        {
            var oldItem = this.SharedStringTable.ElementAt(id);
            var newItem = (SharedStringItem) oldItem.CloneNode(true);
            this.SharedStringTable.Add(newItem);

            return new Tuple<SharedStringItem, int>(newItem, this.SharedStringTable.Count - 1);
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc/>
        [MemberNotNullWhen(true, nameof(extensionContext))]
        public new bool WithExtensions => base.WithExtensions;

        /// <inheritdoc/>
        protected override IPlaceholderReplaceExtensionContext? ExtensionContext => this.extensionContext;

        /// <inheritdoc/>
        protected override IPlaceholderReplaceExtensionContext CreateExtensionContext(IPlaceholderReplacementContext context) =>
            this.extensionContext = new ExcelPlaceholderReplaceExtensionContext(context, context.CancellationToken);

        /// <inheritdoc/>
        protected override async Task<IList<IPlaceholderText>?> GetPlaceholdersFromDatabaseAsync(
            IPlaceholderFindingContext context,
            IDbScope dbScope)
        {
            List<IPlaceholderText> placeholders = [];
            // Пытаемся получить плейсхолдеры из базы
            await using (dbScope.Create())
            {
                var db = dbScope.Db;
                var builderFactory = dbScope.BuilderFactory;

                var json = await db
                    .SetCommand(
                        builderFactory
                            .Select().Top(1).C("PlaceholdersInfo")
                            .From("FileTemplates").NoLock()
                            .Where().C("ID").Equals().P("ID")
                            .And().C("PlaceholdersInfo").IsNotNull()
                            .Limit(1)
                            .Build(),
                        db.Parameter("ID", this.TemplateID))
                    .LogCommand()
                    .ExecuteStringAsync(context.CancellationToken);

                var dictionary = string.IsNullOrEmpty(json)
                    ? []
                    : StorageHelper.DeserializeFromTypedJson(json);

                if (dictionary is null
                    || !dictionary.TryGetValue(nameof(ExcelPlaceholderDocument), out var listObj)
                    || listObj is not (IList list and not byte[]))
                {
                    return placeholders;
                }

                foreach (Dictionary<string, object?> values in list)
                {
                    if (values.Count == 0)
                    {
                        continue;
                    }

                    var text = values.TryGet<string>(OpenXmlHelper.TextField) ?? string.Empty;
                    var position = values.TryGet<object>(OpenXmlHelper.PositionField);
                    var value = PlaceholderHelper.TryGetValue(text);

                    if (value is not null && position is not null)
                    {
                        var newPlaceholder = new PlaceholderText(text, value);
                        newPlaceholder.Info[OpenXmlHelper.PositionField] = position;
                        newPlaceholder.Info[OpenXmlHelper.OrderField] = values.TryGet<int>(OpenXmlHelper.OrderField);

                        placeholders.Add(newPlaceholder);
                    }
                }
            }

            return placeholders;
        }

        /// <inheritdoc/>
        protected override OpenXmlPackage InitDocument()
        {
            this.excelDocument = SpreadsheetDocument.Open(this.Stream, true);

            // В документе XLSX пространство имён http://schemas.openxmlformats.org/spreadsheetml/2006/main считается пространством по умолчанию и пишется без префикса.
            // DocumentFormat.OpenXml для данного пространства имён проставляет константу "x". Установка префикса для данного пространства имён ломает логику работы XLSX файла в некоторых приложениях.
            // Известные проблемы:
            // 1) Ломаются XLSX файлы, созданные через OpenOffice (OpenOffice использует данный префикс для своих нужд).
            // 2) Ломаются сводные таблицы в Р7-Офис.
            OpenXmlHelper.TryUpdateNamespace(
                this.excelDocument,
                "http://schemas.openxmlformats.org/spreadsheetml/2006/main",
                string.Empty);

            var workbookPart = this.excelDocument.WorkbookPart ?? this.excelDocument.AddWorkbookPart();

            var sharedStringTablePart = workbookPart.SharedStringTablePart ??
                workbookPart.AddNewPart<SharedStringTablePart>();

            var workbookStylesPart = workbookPart.WorkbookStylesPart ??
                workbookPart.AddNewPart<WorkbookStylesPart>();

            this.sharedStringTable = new SharedStringTableContainer(sharedStringTablePart.SharedStringTable ??=
                (sharedStringTablePart.SharedStringTable = new SharedStringTable()));

            this.stylesheet = workbookStylesPart.Stylesheet ??=
                (workbookStylesPart.Stylesheet = new Stylesheet());

            if (this.WithExtensions)
            {
                this.extensionContext.Document = this.excelDocument;
            }

            return this.excelDocument;
        }

        /// <inheritdoc/>
        protected override ValueTask<IList<IPlaceholderText>> GetPlaceholdersFromDocumentAsync(IPlaceholderFindingContext context) =>
            ValueTask.FromResult<IList<IPlaceholderText>>(this.GetPlaceholdersFromPart(this.WorkbookPart));

        /// <inheritdoc/>
        protected override void PrepareDocumentForSave()
        {
            this.SharedStringTable.Save();

            if (this.hasFormulas && this.WorkbookPart.Workbook is not null)
            {
                this.WorkbookPart.Workbook.CalculationProperties ??= new();

                // Запускаем полный перерасчет формул при загрузке документа
                this.WorkbookPart.Workbook.CalculationProperties.ForceFullCalculation = true;
                this.WorkbookPart.Workbook.CalculationProperties.FullCalculationOnLoad = true;

                // Очищаем старую цепочку вычисления формул, она рассчитается заново при загрузке документа
                if (this.WorkbookPart.CalculationChainPart is not null)
                {
                    this.WorkbookPart.DeletePart(this.WorkbookPart.CalculationChainPart);
                }
            }
        }

        /// <inheritdoc/>
        protected override void SaveDocument()
        {
            FixMyOffice(this.ExcelDocument);
            this.ExcelDocument.Dispose();

            this.excelDocument = null;
            this.sharedStringTable = null;
            this.stylesheet = null;
            this.worksheetHash = null;
            this.worksheetsDictionary = null;
        }

        /// <inheritdoc/>
        protected override async Task SavePlaceholdersInDatabaseAsync(
            IDbScope dbScope,
            IList<IPlaceholderText> placeholders,
            CancellationToken cancellationToken = default)
        {
            await using (dbScope.Create())
            {
                var list = new List<object>(placeholders.Count);
                var dictionary = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    [nameof(ExcelPlaceholderDocument)] = list,
                };

                foreach (var placeholder in placeholders)
                {
                    var pDictionary = new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        { OpenXmlHelper.PositionField, placeholder.Info[OpenXmlHelper.PositionField] },
                        { OpenXmlHelper.TextField, placeholder.Text },
                        { OpenXmlHelper.OrderField, placeholder.Info.TryGet<int>(OpenXmlHelper.OrderField) }
                    };

                    list.Add(pDictionary);
                }

                var json = StorageHelper.SerializeToTypedJson(dictionary);

                var executor = dbScope.Executor;
                var builderFactory = dbScope.BuilderFactory;

                await executor
                    .ExecuteNonQueryAsync(
                        builderFactory
                            .Update("FileTemplates").C("PlaceholdersInfo").Assign().P("Data")
                            .Where().C("ID").Equals().P("ID")
                            .Build(),
                        cancellationToken,
                        executor.Parameter("ID", this.TemplateID),
                        executor.Parameter("Data", json, DataType.BinaryJson));
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> ReplaceFieldPlaceholdersAsync(IPlaceholderReplacementContext context)
        {
            var hasChanges = false;

            foreach (var replacements in
                context.Replacements
                    .Where(x => x.Placeholder.Info.ContainsKey(OpenXmlHelper.PositionField))
                    .GroupBy(x => TextPosition(x.Placeholder.Info.Get<IList>(OpenXmlHelper.PositionField)))
                    .OrderByDescending(x => x.Key))
            {
                OpenXmlElement? element = this.WorkbookPart.Workbook;
                OpenXmlPart part = this.WorkbookPart;
                var position = replacements.First().Placeholder.Info.Get<IList>(OpenXmlHelper.PositionField);
                if (position is null)
                {
                    continue;
                }

                for (var i = 0; i < position.Count; i++)
                {
                    if (position[i]?.ToString() == OpenXmlHelper.HyperlinkPath)
                    {
                        if (position.Count > i + 1)
                        {
                            await this.ReplaceElementsInRelationshipsAsync(part, (string) position[i + 1]!, [.. replacements]);
                        }

                        break;
                    }

                    var index = (int) position[i]!;
                    if (index < 0)
                    {
                        part = part.Parts.ElementAt(~index).OpenXmlPart;
                        element = part.RootElement;
                    }
                    else
                    {
                        element = element?.ChildElements[index];

                        if (element is Worksheet worksheet
                            && this.WithExtensions)
                        {
                            this.extensionContext.Worksheet = worksheet;
                        }
                    }
                }

                if (element is not null)
                {
                    var elementType = element.GetType();
                    if (elementType == typeof(Cell))
                    {
                        // ReSharper disable once PossibleInvalidCastException
                        await this.ReplaceElementsInCellAsync((Cell) element, false, context.CancellationToken, [.. replacements]);
                    }
                    else if (elementType == typeof(A.Paragraph))
                    {
                        await this.ReplaceElementsInCompositeElementAsync(element, null, context.CancellationToken, [.. replacements]);
                    }
                    else if (elementType == typeof(HeaderFooter))
                    {
                        await this.ReplaceElementsInCompositeElementAsync(element, null, context.CancellationToken, [.. replacements]);
                    }
                }

                hasChanges = true;

                if (this.WithExtensions)
                {
                    this.extensionContext.Worksheet = null;
                }
            }

            return hasChanges;
        }

        /// <inheritdoc/>
        protected override async Task<bool> ReplaceTablePlaceholdersAsync(IPlaceholderReplacementContext context)
        {
            var tablePlaceholders = context.TablePlaceholders;
            if (tablePlaceholders.Count == 0)
            {
                return false;
            }

            var hasChanges = false;
            this.AttachPlaceholders(tablePlaceholders);

            foreach (var worksheetElement in this.WorksheetHash)
            {
                worksheetElement.Tables.Reverse();
                foreach (var tableGroup in worksheetElement.Tables)
                {
                    var (hasTableChanges, moveTotal) = await this.ReplaceGroupPlaceholdersAsync(
                        context,
                        tableGroup);

                    hasChanges |= hasTableChanges;

                    // Добавление всем строкам, гиперссылкам, группам и смерженным ячейкам ниже данной таблицы нового индекса строки
                    tableGroup.Worksheet.Move(moveTotal, tableGroup.Top);
                    tableGroup.Remove();

                    foreach (var hyperlink in tableGroup.Hyperlinks)
                    {
                        if (tableGroup.Worksheet.Element.WorksheetPart is { } worksheetPart)
                        {
                            ClearOldRelationships(worksheetPart, [.. tableGroup.Hyperlinks.Select(x => x.Element.Id?.Value ?? string.Empty)]);
                        }
                    }
                }
            }

            this.UpdateWorksheets();

            return hasChanges;
        }

        /// <inheritdoc/>
        protected override ValueTask<bool> PrepareDocumentForReplaceAsync(IPlaceholderReplacementContext context)
        {
            context.ValidationResult.Add(this.InitializeWorksheets());

            if (context.ValidationResult.IsSuccessful())
            {
                this.hasFormulas = this.PrepareWorksheetFormulas();
            }

            return ValueTask.FromResult(false);
        }

        /// <inheritdoc/>
        protected override IEnumerable<IPlaceholderText> GetPlaceholdersFromElementOverride(OpenXmlElement baseElement, IList position)
        {
            List<IPlaceholderText> result = [];

            switch (baseElement)
            {
                case Cell cell:
                    {
                        if (cell.CellFormula is null)
                        {
                            var allTextString = this.GetCellText(cell);
                            OpenXmlHelper.AddPlaceholdersFromText(result, allTextString, position);
                        }
                        else
                        {
                            var formulaText = cell.CellFormula.Text;
                            OpenXmlHelper.AddPlaceholdersFromText(result, formulaText, position);
                        }

                        break;
                    }

                case A.Paragraph _:
                    {
                        var allText = StringBuilderHelper.Acquire();

                        var textElements = GetTextElements(baseElement);
                        if (textElements is not null)
                        {
                            foreach (var element in textElements)
                            {
                                allText.Append(element.Text);
                            }
                        }

                        var allTextString = allText.ToStringAndRelease();
                        OpenXmlHelper.AddPlaceholdersFromText(result, allTextString, position);
                        break;
                    }

                case HeaderFooter _:
                    {
                        OpenXmlHelper.AddPlaceholdersFromText(result, baseElement.InnerText, position);
                        break;
                    }
            }

            return result;
        }

        /// <inheritdoc/>
        protected override async Task ReplaceTextAsync(
            OpenXmlElement firstBaseElement,
            OpenXmlLeafTextElement firstTextElement,
            OpenXmlElement? lastBaseElement,
            OpenXmlLeafTextElement? lastTextElement,
            string newText,
            IPlaceholder placeholder,
            PlaceholderValue placeholderValue,
            CancellationToken cancellationToken = default)
        {
            // Text (Spreadsheet.Text): текст в ячейке Excel
            // A.Text (Drawing.Text): текст в объекте "Надпись"
            newText = RemoveInvalidChars(newText);

            switch (firstTextElement)
            {
                case Text text:
                    {
                        text.Text = newText.Replace(Environment.NewLine, "\n", StringComparison.Ordinal);
                        text.Space = SpaceProcessingModeValues.Preserve;

                        if (lastTextElement is not null)
                        {
                            var lastText = (Text) lastTextElement;
                            if (string.IsNullOrEmpty(lastText.Text))
                            {
                                lastBaseElement?.Remove();
                            }
                            else
                            {
                                lastText.Space = SpaceProcessingModeValues.Preserve;
                            }
                        }

                        break;
                    }

                case A.Text atext:
                    {
                        atext.Text = newText.Replace(Environment.NewLine, "\n", StringComparison.Ordinal);

                        if (lastTextElement is not null)
                        {
                            var lastText = (A.Text) lastTextElement;
                            if (string.IsNullOrEmpty(lastText.Text))
                            {
                                lastBaseElement?.Remove();
                            }
                        }

                        break;
                    }

                case CellFormula cellFormula:
                    {
                        cellFormula.Text = newText.Replace(Environment.NewLine, "\n", StringComparison.Ordinal);
                        break;
                    }

                case OddHeader:
                case OddFooter:
                    {
                        firstTextElement.Text = newText.Replace(Environment.NewLine, "\n", StringComparison.Ordinal);
                        break;
                    }
            }

            if (this.WithExtensions)
            {
                this.extensionContext.SetPlaceholderData(new(firstBaseElement, firstTextElement));
                await this.AfterPlaceholderReplaceAsync(this.extensionContext.ReplacementContext);
            }

            // Если в текстовом элементе текст пустой, то удаляем базовый элемент, в котором хранится этот текст (обычно это Run или сам Text)
            if (string.IsNullOrEmpty(firstTextElement.Text))
            {
                firstBaseElement.Remove();
            }
        }

        /// <inheritdoc/>
        protected override Task<bool> ReplaceImageAsync(
            OpenXmlElement baseElement,
            IPlaceholder placeholder,
            PlaceholderValue value,
            CancellationToken cancellationToken = default)
        {
            OpenXmlElement? shapeBase;

            var existingShape = OpenXmlHelper.FindParent<Xdr.Shape>(baseElement);
            if (existingShape is null
                || (shapeBase = existingShape.Parent) is null)
            {
                return TaskBoxes.False;
            }

            var data = value.Data;

            if (data is null || data.Length == 0)
            {
                // пустое изображение при замене удаляет надпись
                var anchor = OpenXmlHelper.FindParent<Xdr.TwoCellAnchor>(existingShape);
                if (anchor is not null)
                {
                    var element = this.TryGetWorksheetByPlaceholder(placeholder);

                    AnchorCellGroup? elementAnchor;
                    if (element is not null && (elementAnchor = element.Anchors.FirstOrDefault(x => ReferenceEquals(x.Element, anchor))) is not null)
                    {
                        // удаляем "надпись" из строки таблицы плейсхолдеров: {t:...}, {tv:...}, ...
                        elementAnchor.Remove();
                    }
                    else
                    {
                        // удаляем "надпись" снаружи таблицы плейсхолдеров: {f:...}, {fv:...}, ....
                        anchor.Remove();
                    }

                    return TaskBoxes.True;
                }

                return TaskBoxes.False;
            }

            var worksheetPart = this.TryGetWorksheetPartByPlaceholder(placeholder);
            if (worksheetPart is null)
            {
                return TaskBoxes.False;
            }

            var imageParameters = value.FormatResult.GetImageParameters();
            var imagePartType = OpenXmlHelper.GetImagePartType(imageParameters);

            var existingProperties = existingShape.Descendants<Xdr.ShapeProperties>().FirstOrDefault();

            var picture = CreatePictureFromImage(
                worksheetPart,
                data,
                imagePartType,
                existingProperties,
                imageParameters.Width,
                imageParameters.Height,
                imageParameters.Reformat,
                imageParameters.AlternativeText,
                value.FormatResult.Info);

            // мы должны расположить элемент Picture в ту же позицию, где был Shape,
            // здесь особенно важно положение относительно ClientData
            var index = shapeBase.IndexOf(existingShape);

            existingShape.Remove();
            shapeBase.InsertAt(picture, index);

            return TaskBoxes.True;
        }

        /// <inheritdoc/>
        protected override IDisposable ExecuteInPlaceholderContext(IPlaceholder placeholder, PlaceholderValue? placeholderValue)
        {
            return this.extensionContext!.ExecuteInPlaceholderContext(placeholder, placeholderValue);
        }

        #endregion

        #region Fix Methods

        private static void FixMyOffice(SpreadsheetDocument document)
        {
            // МойОфис Таблица добавляет пространство имён с данным алиасом, что приводит к полной поломке сохранения документа через DocumentFormat.OpenXml.
            // Актуально на момент версий:
            // МойОфис 2022.01 Сборка 21
            // DocumentFormat.OpenXml 2.18.0
            const string badMyOfficeNamespaceAlias = "x";

            static void FixParts(IEnumerable<IdPartPair> parts)
            {
                foreach (var idPartPair in parts)
                {
                    var part = idPartPair.OpenXmlPart;
                    var root = part.RootElement;
                    if (!string.IsNullOrEmpty(root?.LookupNamespace(badMyOfficeNamespaceAlias)))
                    {
                        root.RemoveNamespaceDeclaration(badMyOfficeNamespaceAlias);
                    }

                    FixParts(part.Parts);
                }
            }

            FixParts(document.Parts);

        }

        #endregion
    }
}
