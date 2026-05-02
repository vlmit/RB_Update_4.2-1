#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Expressions;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Placeholders.Extensions;
using Tessa.Platform.Storage;
using A = DocumentFormat.OpenXml.Drawing;
using DataType = LinqToDB.DataType;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using Wp14 = DocumentFormat.OpenXml.Office2010.Word.Drawing;
using WPS = DocumentFormat.OpenXml.Office2010.Word.DrawingShape;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Объект, определяющий способы хранения и изменения текста с заменяемыми плейсхолдерами для документа Word.
    /// </summary>
    /// <param name="stream">Поток файла документа, в котором должны быть заменены плейсхолдеры.</param>
    /// <param name="templateID">ID карточки шаблона файла.</param>
    /// <param name="wordDocumentParser"><inheritdoc cref="IWordDocumentParser" path="/summary"/></param>
    /// <param name="expressionInterpreterProvider"><inheritdoc cref="IExpressionInterpreterProvider" path="/summary"/></param>
    /// <param name="moveProcessor"><inheritdoc cref="IWordDocumentMoveProcessor" path="/summary"/></param>
    public sealed class WordPlaceholderDocument(
        MemoryStream stream,
        Guid templateID,
        IWordDocumentParser wordDocumentParser,
        IExpressionInterpreterProvider expressionInterpreterProvider,
        IWordDocumentMoveProcessor moveProcessor)
        : OpenXmlPlaceholderDocument(stream, templateID)
    {
        #region Fields

        /// <inheritdoc cref="IWordDocumentParser" path="/summary"/>
        private readonly IWordDocumentParser wordDocumentParser = NotNullOrThrow(wordDocumentParser);

        /// <inheritdoc cref="IExpressionInterpreterProvider" path="/summary"/>
        private readonly IExpressionInterpreterProvider expressionInterpreterProvider = NotNullOrThrow(expressionInterpreterProvider);

        /// <inheritdoc cref="IWordDocumentMoveProcessor" path="/summary"/>
        private readonly IWordDocumentMoveProcessor moveProcessor = NotNullOrThrow(moveProcessor);

        /// <inheritdoc cref="WordPlaceholderReplaceExtensionContext" path="/summary"/>
        private WordPlaceholderReplaceExtensionContext? extensionContext;

        /// <summary>
        /// Документ Word.
        /// </summary>
        private WordprocessingDocument? wordDocument;

        /// <inheritdoc cref="IExpressionInterpreter" path="/summary"/>
        private IExpressionInterpreter? interpreter;

        /// <summary>
        /// Идентификатор очередного объекта docPr, который должен быть уникален в пределах документа.
        /// </summary>
        private uint docPropertiesNextId = 1u;

        /// <summary>
        /// Информация о блоках документа.
        /// </summary>
        private IReadOnlyList<IWordDocumentBlock>? blocks;

        /// <summary>
        /// Информация о плейсхолдерах, доступная по его идентификатору.
        /// </summary>
        private Dictionary<int, WordDocumentPlaceholderInfo>? placeholderInfoByID;

        /// <summary>
        /// Информация о таблицах.
        /// </summary>
        private readonly List<WordDocumentTableGroupBlock> tableGroups = [];

        /// <summary>
        /// Список идентификаторов объектов-гиперссылок, которые нужно удалить в конце обработки.
        /// </summary>
        private readonly List<string> relationshipsIDs = [];

        /// <summary>
        /// Результат парсинга документа, выполняемый при поиске плейсхолдеров. Может быть равен <c>null</c>, если плейсхолдеры были загружены из кэша.
        /// </summary>
        private IWordDocumentParsingResult? parsingResult;

        /// <summary>
        /// Текущая версия хранения кэша в базе.
        /// </summary>
        private static readonly object currentVersion = Int32Boxes.Two;

        /// <summary>
        /// Имя ключа, по которому документ сохраняет версию хранимого кэша.
        /// </summary>
        private const string VersionKey = "Version";

        /// <summary>
        /// Имя ключа, по которому документ сохраняет плейсхолдеры в базе данных.
        /// </summary>
        private const string PlaceholdersKey = "Placeholders";

        /// <summary>
        /// Имя ключа, по которому документ сохраняет блоки документа Word в базе данных.
        /// </summary>
        private const string DocumentBlocksKey = "DocumentBlocks";

        #endregion

        #region Private Methods

        /// <summary>
        /// Производит замену <paramref name="replacements"/> в заданном элементе <paramref name="fieldCode"/>.
        /// </summary>
        /// <param name="fieldCode">Объект <see cref="FieldCode"/>, в котором производится замена плейсхолдеров.</param>
        /// <param name="replacements">Массив плейсхолдеров для замены.</param>
        /// <returns>Асинхронная задача.</returns>
        private async Task ReplaceElementsInFieldCodeAsync(
            FieldCode fieldCode,
            params IEnumerable<IPlaceholderReplacement> replacements)
        {
            var codeText = fieldCode.Text;

            var decodedText = new StringBuilder(Uri.UnescapeDataString(codeText));
            decodedText.Replace(HyperlinkRemoveHead, string.Empty, 0, Math.Min(HyperlinkRemoveHeadLength, decodedText.Length));

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
                    newValue = this.extensionContext.PlaceholderValue;
                }

                decodedText.Replace(replacement.Placeholder.Text, newValue?.Text);

                if (this.WithExtensions)
                {
                    await this.AfterPlaceholderReplaceAsync(this.extensionContext.ReplacementContext);
                }
            }

            fieldCode.Text = decodedText.ToString();
        }

        /// <summary>
        /// Возвращает дочерний элемент относительно <paramref name="newParent"/>, соответствующий элементу <paramref name="baseChild"/> относительно <paramref name="baseParent"/>.
        /// Элемент <paramref name="newParent"/> должен быть полной копией элемента <paramref name="baseParent"/>.
        /// </summary>
        /// <param name="baseChildType">Тип искомого элемента.</param>
        /// <param name="baseParent">Родительский элемент, относительно которого ведется поиск.</param>
        /// <param name="baseChild">Дочерний элемент, соответствие которого мы ищем в <paramref name="newParent"/>.</param>
        /// <param name="newParent">Копия родительского элемента, дочерний элемент которого мы ищем.</param>
        /// <returns>Дочерний элемент или <c>null</c>, если его не удалось найти.</returns>
        private static OpenXmlElement? GetRelativeElement(
            OpenXmlElement baseParent,
            OpenXmlElement baseChild,
            Type baseChildType,
            OpenXmlElement newParent)
        {
            if (baseParent == baseChild)
            {
                return newParent;
            }

            var position = baseParent.Descendants().Where(x => x.GetType() == baseChildType).IndexOf(baseChild);
            return position >= 0 ? newParent.Descendants().Where(x => x.GetType() == baseChildType).ElementAtOrDefault(position) : null;
        }

        private static A.Graphic CreateGraphicFromImage(
            byte[] imageBytes,
            PartTypeInfo imagePartType,
            OpenXmlPart partElement,
            WPS.ShapeProperties? existingProperties,
            DW.Extent? existingExtent,
            double width,
            double height,
            bool reformat,
            ISerializableObject info)
        {
            // добавляем изображение в документ и получаем его relationshipId
            var imagePart = NotNullOrThrow(partElement.TryAddImagePart(imagePartType));
            imagePart.FeedData(new MemoryStream(imageBytes));

            var relationshipId = partElement.GetIdOfPart(imagePart);

            // берём из надписи или генерируем свойства фигуры для картинки
            PIC.ShapeProperties actualProperties;
            if (!reformat && existingProperties is not null)
            {
                var childElements = existingProperties.ChildElements
                    .Select(x => x.CloneNode(deep: true))
                    .ToArray();

                var transform2D = (A.Transform2D) existingProperties.Transform2D!.CloneNode(deep: true);

                actualProperties = new PIC.ShapeProperties(childElements)
                {
                    BlackWhiteMode = existingProperties.BlackWhiteMode,
                    Transform2D = transform2D,
                };
            }
            else
            {
                actualProperties = new PIC.ShapeProperties(
                    new A.Transform2D(
                        new A.Offset { X = 0L, Y = 0L },
                        new A.Extents { Cx = OpenXmlHelper.DefaultExtentsCx, Cy = OpenXmlHelper.DefaultExtentsCy }),
                    new A.PresetGeometry(new A.AdjustValueList())
                    { Preset = A.ShapeTypeValues.Rectangle });
            }

            // определяем и проставляем размеры картинки
            var extents = actualProperties.Transform2D?.Extents;
            if (extents is not null || existingExtent is not null)
            {
                Int64Value cx = OpenXmlHelper.DefaultExtentsCx;
                Int64Value cy = OpenXmlHelper.DefaultExtentsCy;

                if (width > 0.0)
                {
                    cx = OpenXmlHelper.PixelsToEmu(width);
                }

                if (height > 0.0)
                {
                    cy = OpenXmlHelper.PixelsToEmu(height);
                }

                if (reformat && (width <= 0.0 || height <= 0.0))
                {
                    if (width <= 0)
                    {
                        var imageWidth = info.TryGet<int>("ActualWidth");
                        var imageResX = info.TryGet<double>("ActualVerticalResolution");

                        cx = OpenXmlHelper.PixelsToEmu(imageWidth, imageResX);
                    }

                    if (height <= 0.0)
                    {
                        var imageHeight = info.TryGet<int>("ActualHeight");
                        var imageResY = info.TryGet<double>("ActualHorizontalResolution");

                        cy = OpenXmlHelper.PixelsToEmu(imageHeight, imageResY);
                    }
                }

                if (extents is not null)
                {
                    extents.Cx = cx;
                    extents.Cy = cy;
                }

                if (existingExtent is not null)
                {
                    existingExtent.Cx = cx;
                    existingExtent.Cy = cy;
                }
            }

            // создаём объект Graphic, в который картинка обёрнута
            return new A.Graphic(
                new A.GraphicData(
                    new PIC.Picture(
                        new PIC.NonVisualPictureProperties(
                            new PIC.NonVisualDrawingProperties { Id = 0U, Name = "Image" },
                            new PIC.NonVisualPictureDrawingProperties()),
                        new PIC.BlipFill(
                            new A.Blip(
                                new A.BlipExtensionList(
                                    new A.BlipExtension { Uri = "{28A0092B-C50C-407E-A947-70E740481C1C}" }))
                            {
                                Embed = relationshipId,
                                CompressionState = A.BlipCompressionValues.Print
                            },
                            new A.Stretch(new A.FillRectangle())),
                        actualProperties))
                {
                    Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
                });
        }

        /// <summary>
        /// Метод для подготовки табличных плейсхолдеров.
        /// </summary>
        /// <param name="tablePlaceholders">Табличные плейсхолдеры.</param>
        /// <returns>Справочник табличных плейсхолдеров, доступных по идентификатору объекта с информацией о плейсхолдере в документе Word.</returns>
        private Dictionary<int, IPlaceholder> PrepareTablePlaceholders(IList<IPlaceholder> tablePlaceholders)
        {
            var placeholdersByWordInfo = new Dictionary<int, IPlaceholder>(tablePlaceholders.Count);
            foreach (var tablePlaceholder in tablePlaceholders)
            {
                var wordInfoID = WordDocumentPlaceholderInfo.TryGetWordInfoID(tablePlaceholder);
                if (wordInfoID is null
                    || !this.placeholderInfoByID!.TryGetValue(wordInfoID.Value, out var wordInfo))
                {
                    continue;
                }

                placeholdersByWordInfo[wordInfoID.Value] = tablePlaceholder;

                if (wordInfo.IsHyperlinkPlaceholder)
                {
                    this.relationshipsIDs.Add(wordInfo.HyperlinkID);
                }

                var baseElement = OpenXmlHelper.GetElementByPosition(this.wordDocument!.MainDocumentPart, wordInfo.Position);
                tablePlaceholder.Info.Add(OpenXmlHelper.BaseElementField, baseElement);
            }

            return placeholdersByWordInfo;
        }

        /// <summary>
        /// Метод для создания и добавления новых элементов, созданных из копий элементов таблицы <paramref name="currentTableGroup"/>.
        /// </summary>
        /// <param name="currentTableGroup">Таблица.</param>
        /// <param name="insertBeforeElement">Объект, перед которым происходит вставка новых элементов.</param>
        /// <param name="parentElement">
        /// Родительский объект, в который производится вставка новых элементов. Используется, если <paramref name="insertBeforeElement"/> равен <c>null</c>.
        /// </param>
        /// <returns>Возвращает список новых элементов.</returns>
        private static List<OpenXmlElement> CreateNewElements(
            WordDocumentTableGroupBlock currentTableGroup,
            OpenXmlElement? insertBeforeElement,
            OpenXmlElement parentElement)
        {
            var newTableRows = currentTableGroup.BaseElements.Select(x => x.CloneNode(true)).ToList();

            // Добавляем новые строки в таблицу
            if (insertBeforeElement is not null)
            {
                foreach (var newTableRow in newTableRows)
                {
                    insertBeforeElement.InsertBeforeSelf(newTableRow);
                }
            }
            else
            {
                parentElement.Append(newTableRows);
            }

            return newTableRows;
        }

        /// <summary>
        /// Метод для замены плейсхолдеров строки для заданной таблицы.
        /// </summary>
        /// <param name="context">Контекст замены плейсхолдеров.</param>
        /// <param name="currentTableGroup">Таблица.</param>
        /// <param name="newTableRows">Новые элементы строки таблицы, где производится замена.</param>
        /// <param name="replacements">Список объектов с информацией о способе замены плейсхолдера.</param>
        /// <returns>Асинхронная задача.</returns>
        private async Task ReplaceInNewElementsAsync(
            IPlaceholderReplacementContext context,
            WordDocumentTableGroupBlock currentTableGroup,
            List<OpenXmlElement> newTableRows,
            IPlaceholderReplacement[] replacements)
        {
            var rowPlaceholders = currentTableGroup.TablePlaceholders;
            (OpenXmlElement? baseElement, OpenXmlElement? rowElement, OpenXmlElement? fromElement)[] newBaseElementArray = [.. rowPlaceholders
                .Select(placeholder =>
                {
                    var baseElement = placeholder.Info.TryGet<OpenXmlElement>(OpenXmlHelper.BaseElementField);
                    if (baseElement is null)
                    {
                        return default;
                    }

                    if (currentTableGroup.InParagraph)
                    {
                        return (newTableRows[0].Parent, newTableRows[0].Parent, (OpenXmlElement?) newTableRows[0]);
                    }

                    var baseElementType = baseElement.GetType();

                    var i = 0;
                    foreach (var newTableRow in newTableRows)
                    {
                        var relativeElement = GetRelativeElement(currentTableGroup.BaseElements[i++], baseElement, baseElementType, newTableRow);
                        if (relativeElement is not null)
                        {
                            return (relativeElement, newTableRow, null);
                        }
                    }

                    return default;
                })];

            for (var index = 0; index < replacements.Length; index++)
            {
                var (newBaseElement, currentTableRow, fromElement) = newBaseElementArray[index];
                if (newBaseElement is null
                    || currentTableRow is null)
                {
                    continue;
                }

                if (this.WithExtensions)
                {
                    this.extensionContext.CurrentRowElement = currentTableRow;
                }

                var replacement = replacements[index];
                var wordInfoID = WordDocumentPlaceholderInfo.TryGetWordInfoID(replacement.Placeholder);
                if (wordInfoID is null
                    || !this.placeholderInfoByID!.TryGetValue(wordInfoID.Value, out var wordInfo))
                {
                    continue;
                }

                if (wordInfo.IsHyperlinkPlaceholder
                    && newBaseElement is Hyperlink { Id.Value: not null } hyperlink)
                {
                    hyperlink.Id = await this.ReplaceElementsInRelationshipsWithCopyAsync(
                        this.wordDocument!.MainDocumentPart!,
                        wordInfo.HyperlinkID,
                        hyperlink.Id.Value,
                        replacement);
                }
                else
                {
                    // Заменяем плейсхолдер в новом базовом элементе
                    var baseElementType = newBaseElement.GetType();
                    if (baseElementType == typeof(Paragraph)
                        || baseElementType == typeof(Hyperlink))
                    {
                        await this.ReplaceElementsInCompositeElementAsync(newBaseElement, fromElement, context.CancellationToken, replacement);
                    }
                    else if (baseElementType == typeof(FieldCode))
                    {
                        await this.ReplaceElementsInFieldCodeAsync((FieldCode) newBaseElement, replacement);
                    }
                }
            }
        }

        private ValueTask<bool> ReplaceGroupPlaceholdersAsync(
            IPlaceholderReplacementContext context,
            WordDocumentTableGroupBlock tableGroup,
            IList<OpenXmlElement>? parentElements = null,
            IList<OpenXmlElement>? originalParentElements = null,
            IEditablePlaceholderTable? placeholderTable = null,
            IEnumerable<IPlaceholderRow>? rows = null,
            int groupLevel = 0)
        {
            return tableGroup.GroupType switch
            {
                WordDocumentTableGroupType.Row => this.ReplaceRowGroupPlaceholdersAsync(
                    context,
                    tableGroup,
                    parentElements,
                    originalParentElements,
                    placeholderTable,
                    rows),
                WordDocumentTableGroupType.Group => this.ReplaceGroupGroupPlaceholdersAsync(
                    context,
                    tableGroup,
                    parentElements,
                    originalParentElements,
                    placeholderTable,
                    rows,
                    groupLevel),
                WordDocumentTableGroupType.Table => this.ReplaceTableGroupPlaceholdersAsync(
                    context,
                    tableGroup,
                    parentElements,
                    originalParentElements),
                _ => throw new ArgumentException("TableGroup.GroupType has invalid value"),
            };
        }

        private async ValueTask<bool> ReplaceTableGroupPlaceholdersAsync(
            IPlaceholderReplacementContext context,
            WordDocumentTableGroupBlock tableGroup,
            IList<OpenXmlElement>? parentElements = null,
            IList<OpenXmlElement>? originalParentElements = null)
        {
            var hasTableRows = false;

            List<WordDocumentConditionalBlock> conditionalBlocks = [];
            List<WordDocumentTableGroupBlock> tableGroupBlocks = [];
            FillTypedBlocks(tableGroup.ChildBlocks, conditionalBlocks, tableGroupBlocks);

            if (conditionalBlocks.Count > 0)
            {
                await this.ProcessTableConditionalBlocksAsync(
                    context,
                    tableGroupBlocks,
                    conditionalBlocks,
                    parentElements ?? tableGroup.BaseElements);
            }

            foreach (var innerGroup in tableGroupBlocks)
            {
                hasTableRows |= await this.ReplaceGroupPlaceholdersAsync(
                    context,
                    innerGroup,
                    parentElements,
                    originalParentElements);
            }

            // Если нет строк, удаляем таблицу целиком.
            if (!hasTableRows)
            {
                DeleteTableGroupElements(
                    tableGroup,
                    parentElements,
                    originalParentElements);
            }

            return hasTableRows;
        }

        private async ValueTask<bool> ReplaceGroupGroupPlaceholdersAsync(
            IPlaceholderReplacementContext context,
            WordDocumentTableGroupBlock tableGroup,
            IList<OpenXmlElement>? parentElements = null,
            IList<OpenXmlElement>? originalParentElements = null,
            IEditablePlaceholderTable? placeholderTable = null,
            IEnumerable<IPlaceholderRow>? rows = null,
            int groupLevel = 0)
        {
            var needTableExtensions = placeholderTable is null && this.WithExtensions;
            placeholderTable ??= await this.FillTableAsync(
                context,
                tableGroup);

            using var _ = needTableExtensions
                ? this.extensionContext!.ExecuteInTableContext(placeholderTable, tableGroup.TableElement)
                : null;

            if (needTableExtensions)
            {
                await this.BeforeTableReplaceAsync(context);
            }

            rows ??= placeholderTable?.Rows;

            var hasRows = rows?.Any() == true;

            if (hasRows)
            {
                var originalLastElement = tableGroup.BaseElements[^1];
                OpenXmlElement? lastElement = null;

                var originalLastElementType = originalLastElement.GetType();

                if (parentElements is not null
                    && originalParentElements is not null)
                {
                    for (var i = 0; i < parentElements.Count; i++)
                    {
                        var parentElement = parentElements[i];
                        var originalParentElement = originalParentElements[i];

                        lastElement = GetRelativeElement(originalParentElement, originalLastElement, originalLastElementType, parentElement);
                        if (lastElement is not null)
                        {
                            break;
                        }
                    }
                }

                lastElement ??= originalLastElement;

                var insertBeforeElement = lastElement.NextSibling();
                var lastElementParent = NotNullOrThrow(lastElement.Parent);

                await placeholderTable!.FillHorizontalGroupsAsync(
                    context,
                    rows,
                    groupLevel);

                List<WordDocumentConditionalBlock> conditionalBlocks = [];
                List<WordDocumentTableGroupBlock> tableGroupBlocks = [];
                FillTypedBlocks(tableGroup.ChildBlocks, conditionalBlocks, tableGroupBlocks);

                IPlaceholderReplacement[]? allReplacements = null;
                var replacements = new IPlaceholderReplacement[tableGroup.TablePlaceholders.Count];

                foreach (var rowGroup in rows!.GroupBy(x => x.HorizontalGroup).ToArray())
                {
                    var row = rowGroup.First();
                    context.SetPerformingRow(placeholderTable!.Name, row);

                    var rowElements = CreateNewElements(
                        tableGroup,
                        insertBeforeElement,
                        lastElementParent);

                    using var rowContext = this.WithExtensions
                        ? this.extensionContext.ExecuteInRowContext(row, true)
                        : null;

                    if (this.WithExtensions)
                    {
                        await this.BeforeRowReplaceAsync(context);
                    }

                    for (var i = 0; i < tableGroup.TablePlaceholders.Count; i++)
                    {
                        var placeholder = tableGroup.TablePlaceholders[i];

                        // заменяем текст, который может быть равен null в результате замены
                        var newValue = await ((ITablePlaceholderType) placeholder.Type)
                                .ReplaceAsync(context, placeholder, row, context.CancellationToken)
                            ?? PlaceholderValue.Empty;

                        replacements[i] = new PlaceholderReplacement(placeholder, newValue);
                    }

                    List<WordDocumentTableGroupBlock> groupTableGroupBlocks;
                    if (conditionalBlocks.Count > 0)
                    {
                        if (allReplacements is null)
                        {
                            var index = 0;
                            allReplacements = new IPlaceholderReplacement[context.Replacements.Count + tableGroup.TablePlaceholders.Count];
                            foreach (var replacement in context.Replacements)
                            {
                                allReplacements[index++] = replacement;
                            }
                        }

                        Array.Copy(replacements, 0, allReplacements, context.Replacements.Count, replacements.Length);

                        groupTableGroupBlocks = [.. tableGroupBlocks];
                        await this.ProcessTableConditionalBlocksAsync(
                            context,
                            groupTableGroupBlocks,
                            conditionalBlocks,
                            rowElements,
                            allReplacements,
                            placeholderTable,
                            row);
                    }
                    else
                    {
                        groupTableGroupBlocks = tableGroupBlocks;
                    }

                    foreach (var innerGroup in groupTableGroupBlocks)
                    {
                        await this.ReplaceGroupPlaceholdersAsync(
                            context,
                            innerGroup,
                            rowElements,
                            tableGroup.BaseElements,
                            placeholderTable,
                            rowGroup,
                            groupLevel + 1);
                    }

                    await this.ReplaceInNewElementsAsync(
                        context,
                        tableGroup,
                        rowElements,
                        replacements);

                    if (this.WithExtensions)
                    {
                        this.extensionContext.SetRowData(rowElements);
                        await this.AfterRowReplaceAsync(context);
                    }
                }
            }

            // Удаляем оригинальные элементы группы
            DeleteTableGroupElements(tableGroup, parentElements, originalParentElements);

            if (needTableExtensions)
            {
                await this.AfterTableReplaceAsync(context);
            }

            return hasRows;
        }

        private async ValueTask<bool> ReplaceRowGroupPlaceholdersAsync(
            IPlaceholderReplacementContext context,
            WordDocumentTableGroupBlock tableGroup,
            IList<OpenXmlElement>? parentElements = null,
            IList<OpenXmlElement>? originalParentElements = null,
            IEditablePlaceholderTable? placeholderTable = null,
            IEnumerable<IPlaceholderRow>? rows = null)
        {
            var needTableExtensions = placeholderTable is null && this.WithExtensions;
            placeholderTable ??= await this.FillTableAsync(
                context,
                tableGroup);

            using var _ = needTableExtensions
                ? this.extensionContext!.ExecuteInTableContext(placeholderTable, tableGroup.TableElement)
                : null;

            if (needTableExtensions)
            {
                await this.BeforeTableReplaceAsync(context);
            }

            rows ??= placeholderTable?.Rows;

            var hasRows = rows is not null
                && rows.Any();

            if (hasRows)
            {
                var number = placeholderTable!.Info.TryGet<int>(PlaceholderHelper.NumberKey);
                OpenXmlElement? originalLastElement = tableGroup.BaseElements[^1],
                    lastElement = null;

                var originalLastElementType = originalLastElement.GetType();

                if (parentElements is not null
                    && originalParentElements is not null)
                {
                    for (var i = 0; i < parentElements.Count; i++)
                    {
                        var parentElement = parentElements[i];
                        var originalParentElement = originalParentElements[i];

                        lastElement = GetRelativeElement(originalParentElement, originalLastElement, originalLastElementType, parentElement);
                        if (lastElement is not null)
                        {
                            break;
                        }
                    }
                }

                lastElement ??= originalLastElement;

                var insertBeforeElement = lastElement.NextSibling();
                var lastElementParent = NotNullOrThrow(lastElement.Parent);

                List<WordDocumentConditionalBlock> conditionalBlocks = [];
                List<WordDocumentTableGroupBlock> tableGroupBlocks = [];
                FillTypedBlocks(tableGroup.ChildBlocks, conditionalBlocks, tableGroupBlocks);

                IPlaceholderReplacement[]? allReplacements = null;
                var replacements = new IPlaceholderReplacement[tableGroup.TablePlaceholders.Count];

                foreach (var row in rows!)
                {
                    row.Number = ++number;
                    context.SetPerformingRow(placeholderTable.Name, row);

                    var rowElements = CreateNewElements(
                        tableGroup,
                        insertBeforeElement,
                        lastElementParent);

                    using var rowContext = this.WithExtensions
                        ? this.extensionContext.ExecuteInRowContext(row, false)
                        : null;

                    if (this.WithExtensions)
                    {
                        await this.BeforeRowReplaceAsync(context);
                    }

                    for (var i = 0; i < tableGroup.TablePlaceholders.Count; i++)
                    {
                        var placeholder = tableGroup.TablePlaceholders[i];

                        // заменяем текст, который может быть равен null в результате замены
                        var newValue = await ((ITablePlaceholderType) placeholder.Type)
                                .ReplaceAsync(context, placeholder, row, context.CancellationToken)
                            ?? PlaceholderValue.Empty;

                        replacements[i] = new PlaceholderReplacement(placeholder, newValue);
                    }

                    List<WordDocumentTableGroupBlock> rowTableGroupBlocks;
                    if (conditionalBlocks.Count > 0)
                    {
                        if (allReplacements is null)
                        {
                            var index = 0;
                            allReplacements = new IPlaceholderReplacement[context.Replacements.Count + tableGroup.TablePlaceholders.Count];
                            foreach (var replacement in context.Replacements)
                            {
                                allReplacements[index++] = replacement;
                            }
                        }

                        Array.Copy(replacements, 0, allReplacements, context.Replacements.Count, replacements.Length);

                        rowTableGroupBlocks = [.. tableGroupBlocks];
                        await this.ProcessTableConditionalBlocksAsync(
                            context,
                            rowTableGroupBlocks,
                            conditionalBlocks,
                            rowElements,
                            replacements,
                            placeholderTable,
                            row);
                    }
                    else
                    {
                        rowTableGroupBlocks = tableGroupBlocks;
                    }

                    foreach (var innerGroup in rowTableGroupBlocks)
                    {
                        if (innerGroup.GroupType == WordDocumentTableGroupType.Table)
                        {
                            await this.ReplaceTableGroupPlaceholdersAsync(
                                context,
                                innerGroup,
                                rowElements,
                                tableGroup.BaseElements);
                        }
                        else
                        {
                            // TODO Placeholders - строки не содержат группы и другие строки
                        }
                    }

                    await this.ReplaceInNewElementsAsync(
                        context,
                        tableGroup,
                        rowElements,
                        replacements);

                    if (this.WithExtensions)
                    {
                        this.extensionContext.SetRowData(rowElements);
                        await this.AfterRowReplaceAsync(context);
                    }
                }

                placeholderTable.Info[PlaceholderHelper.NumberKey] = number;
            }

            // Удаляем оригинальные элементы строки
            DeleteTableGroupElements(tableGroup, parentElements, originalParentElements);

            if (needTableExtensions)
            {
                await this.AfterTableReplaceAsync(context);
            }

            return hasRows;
        }

        private static void DeleteTableGroupElements(
            WordDocumentTableGroupBlock tableGroup,
            IList<OpenXmlElement>? parentElements,
            IList<OpenXmlElement>? originalParentElements)
        {
            // Если нет родителей, то удаляем оригинальные элементы
            if (parentElements is null
                || originalParentElements is null)
            {
                foreach (var element in tableGroup.BaseElements)
                {
                    element.SafeRemove();
                }
            }
            else // Если таблица вложенная (есть родители), удаляем копии оригинальных элементов
            {
                var elementsToDelete = new OpenXmlElement[tableGroup.BaseElements.Count];
                for (var i = 0; i < tableGroup.BaseElements.Count; i++)
                {
                    var element = tableGroup.BaseElements[i];
                    var elementType = element.GetType();
                    for (var j = 0; j < parentElements.Count; j++)
                    {
                        var parentElement = parentElements[j];
                        var originalParentElement = originalParentElements[j];

                        var elementToDelete = GetRelativeElement(originalParentElement, element, elementType, parentElement);
                        if (elementToDelete is not null)
                        {
                            elementsToDelete[i] = elementToDelete;
                            break;
                        }
                    }
                }

                for (var i = 0; i < elementsToDelete.Length; i++)
                {
                    var elementToDelete = elementsToDelete[i];
                    elementToDelete?.SafeRemove();
                }
            }
        }

        private static void RemoveBlockElements(
            OpenXmlElement rootElement,
            IEnumerable<IWordDocumentBlock> blocks)
        {
            static void FillIDs(HashSet<string> ids, IEnumerable<IWordDocumentBlock> blocks)
            {
                foreach (var block in blocks)
                {
                    ids.Add(block.ID);

                    if (block.ChildBlocks.Count > 0)
                    {
                        FillIDs(ids, block.ChildBlocks);
                    }
                }
            }

            HashSet<string> ids = [];
            FillIDs(ids, blocks);

            if (ids.Count == 0)
            {
                return;
            }

            static void RemoveBlockElements(
                IEnumerable<OpenXmlElement> elements,
                HashSet<string> ids,
                List<OpenXmlElement> elementsToRemove)
            {
                foreach (var element in elements)
                {
                    var id = element switch
                    {
                        BookmarkStart elementWithID => elementWithID.Id?.Value,
                        BookmarkEnd elementWithID => elementWithID.Id?.Value,
                        CommentRangeStart elementWithID => elementWithID.Id?.Value,
                        CommentRangeEnd elementWithID => elementWithID.Id?.Value,
                        CommentReference elementWithID => elementWithID.Id?.Value,
                        _ => null
                    };

                    if (!string.IsNullOrEmpty(id)
                        && ids.Contains(id))
                    {
                        elementsToRemove.Add(element);
                    }
                    else if (element.HasChildren)
                    {
                        RemoveBlockElements(element, ids, elementsToRemove);
                    }
                }
            }

            List<OpenXmlElement> elementsToRemove = [];
            RemoveBlockElements(rootElement, ids, elementsToRemove);

            if (elementsToRemove.Count > 0)
            {
                foreach (var element in elementsToRemove)
                {
                    element.Remove();
                }
            }
        }

        private async ValueTask<IEditablePlaceholderTable?> FillTableAsync(
            IPlaceholderReplacementContext context,
            WordDocumentTableGroupBlock group,
            IEditablePlaceholderTable? table = null,
            int groupLevel = 0)
        {
            for (var i = 0; i < group.TablePlaceholders.Count; i++)
            {
                var placeholder = group.TablePlaceholders[i];
                table = await ((ITablePlaceholderType) placeholder.Type).FillTableAsync(
                    context,
                    placeholder,
                    table);

                if (table is not null && group.GroupType == WordDocumentTableGroupType.Group)
                {
                    table.AddHorizontalGroupPlaceholder(
                        placeholder,
                        groupLevel);
                }
            }

            await this.FillChildBlocksAsync(
                group.ChildBlocks,
                context,
                table,
                groupLevel);

            return table;
        }

        private async ValueTask FillChildBlocksAsync(
            IEnumerable<IWordDocumentBlock> childBlocks,
            IPlaceholderReplacementContext context,
            IEditablePlaceholderTable? table,
            int groupLevel)
        {
            List<WordDocumentConditionalBlock> conditionalBlocks = [];
            List<WordDocumentTableGroupBlock> tableGroupBlocks = [];
            FillTypedBlocks(childBlocks, conditionalBlocks, tableGroupBlocks);

            foreach (var innerGroup in tableGroupBlocks)
            {
                if (innerGroup.GroupType != WordDocumentTableGroupType.Table)
                {
                    await this.FillTableAsync(
                        context,
                        innerGroup,
                        table,
                        innerGroup.GroupType == WordDocumentTableGroupType.Group ? groupLevel + 1 : 0);
                }
            }

            foreach (var conditionalBlock in conditionalBlocks)
            {
                if (conditionalBlock.ChildBlocks.Count > 0)
                {
                    await this.FillChildBlocksAsync(
                        conditionalBlock.ChildBlocks,
                        context,
                        table,
                        groupLevel);
                }
            }
        }

        private ValueTask<bool> ProcessConditionalBlockByMainPartAsync(
            IPlaceholderReplacementContext replacementContext,
            IExpressionExecutionContext expressionExecutionContext,
            MainDocumentPart mainDocumentPart,
            WordprocessingCommentsPart commentsPart,
            WordDocumentConditionalBlock conditionalBlock,
            List<WordDocumentMoveRule> moveRules,
            List<WordDocumentTableGroupBlock> tableGroupBlocks,
            Dictionary<int, WordDocumentPlaceholderInfo>? placeholderInfoByID = null,
            CancellationToken cancellationToken = default)
        {
            return this.ProcessConditionalBlockAsync(
                replacementContext,
                expressionExecutionContext,
                (block) => OpenXmlHelper.GetElementByPosition(mainDocumentPart, block.StartPosition),
                (block) => OpenXmlHelper.GetElementByPosition(mainDocumentPart, block.EndPosition),
                commentsPart,
                conditionalBlock,
                moveRules,
                tableGroupBlocks,
                placeholderInfoByID,
                cancellationToken);
        }

        private ValueTask<bool> ProcessConditionalBlockByElementsAsync(
            IPlaceholderReplacementContext replacementContext,
            IExpressionExecutionContext expressionExecutionContext,
            IList<OpenXmlElement> blockElements,
            WordprocessingCommentsPart commentsPart,
            WordDocumentConditionalBlock conditionalBlock,
            List<WordDocumentMoveRule> moveRules,
            List<WordDocumentTableGroupBlock> tableGroupBlocks,
            CancellationToken cancellationToken = default)
        {
            return this.ProcessConditionalBlockAsync(
                replacementContext,
                expressionExecutionContext,
                (block) => blockElements.SelectMany(x => x.Descendants<CommentRangeStart>()).First(x => x.Id?.Value == block.ID),
                (block) => blockElements.SelectMany(x => x.Descendants<CommentRangeEnd>()).First(x => x.Id?.Value == block.ID),
                commentsPart,
                conditionalBlock,
                moveRules,
                tableGroupBlocks,
                cancellationToken: cancellationToken);
        }

        private async ValueTask<bool> ProcessConditionalBlockAsync(
            IPlaceholderReplacementContext replacementContext,
            IExpressionExecutionContext expressionExecutionContext,
            Func<WordDocumentConditionalBlock, OpenXmlElement> getCommentStart,
            Func<WordDocumentConditionalBlock, OpenXmlElement> getCommentEnd,
            WordprocessingCommentsPart commentsPart,
            WordDocumentConditionalBlock conditionalBlock,
            List<WordDocumentMoveRule> moveRules,
            List<WordDocumentTableGroupBlock> tableGroupBlocks,
            Dictionary<int, WordDocumentPlaceholderInfo>? placeholderInfoByID = null,
            CancellationToken cancellationToken = default)
        {
            var interpreter = await this.GetInterpreterAsync(cancellationToken);
            if (interpreter is null)
            {
                // Не удалось получить интерпретатор, не скрываем блок, т.к. нет возможности проверить условие
                return true;
            }

            var expressionParseResult = await interpreter.ParseExpressionAsync<bool>(conditionalBlock.Expression, expressionExecutionContext);
            replacementContext.ValidationResult.Add(expressionParseResult.ValidationResult);
            if (!expressionParseResult.ValidationResult.IsSuccessful
                || expressionParseResult.Expression is null)
            {
                return false;
            }

            var expressionResult = await interpreter.ExecuteExpressionAsync(expressionParseResult.Expression, expressionExecutionContext);
            replacementContext.ValidationResult.Add(expressionResult.ValidationResult);
            if (!expressionResult.ValidationResult.IsSuccessful)
            {
                return false;
            }

            var showBlock = expressionResult.Result;

            var commentStart = getCommentStart(conditionalBlock);
            var commentEnd = getCommentEnd(conditionalBlock);
            var commentElement = commentsPart.Comments?.FirstOrDefault(x => ((Comment) x).Id?.Value == conditionalBlock.ID);
            var commentReferenceRun = commentEnd.NextSibling();
            var commentReferenceRunDepth = conditionalBlock.EndPosition.Count;
            // Нужно найти Run с CommentReference, который остаётся в тексте. Он всегда лежит после commentEnd, но может быть внутри другого элемента, т.к. обязательно располагается внутри run.
            while (commentReferenceRun is not null)
            {
                if (commentReferenceRun is CommentReference commentReference
                    && commentReference.Id?.Value == conditionalBlock.ID)
                {
                    commentReferenceRun = commentReferenceRun.Parent;
                    commentReferenceRunDepth--;
                    break;
                }

                if (commentReferenceRun.HasChildren)
                {
                    commentReferenceRun = commentReferenceRun.FirstChild;
                    commentReferenceRunDepth++;
                }
                else
                {
                    var nextCommentReferenceRun = commentReferenceRun.NextSibling();
                    while (commentReferenceRun is not null && nextCommentReferenceRun is null)
                    {
                        commentReferenceRun = commentReferenceRun.Parent;
                        commentReferenceRunDepth--;
                        nextCommentReferenceRun = commentReferenceRun?.NextSibling();
                    }

                    commentReferenceRun = nextCommentReferenceRun;
                }
            }

            if (commentReferenceRun is not null)
            {
                var removeCommentReferenceParentDepth = RemoveEmptyParent(commentReferenceRun, true);
                if (removeCommentReferenceParentDepth > 0)
                {
                    moveRules.Add(
                        new WordDocumentMoveRule
                        {
                            MoveBy = 1,
                            MoveFrom = [.. conditionalBlock.EndPosition.Take(commentReferenceRunDepth - removeCommentReferenceParentDepth + 1)]
                        });
                }
            }

            if (showBlock)
            {
                moveRules.Add(
                    new WordDocumentMoveRule
                    {
                        MoveBy = 1,
                        MoveFrom = [.. conditionalBlock.EndPosition],
                    });

                await this.ProcessBlockChildrenAsync(
                    replacementContext,
                    expressionExecutionContext,
                    getCommentStart,
                    getCommentEnd,
                    commentsPart,
                    conditionalBlock,
                    moveRules,
                    tableGroupBlocks,
                    placeholderInfoByID,
                    cancellationToken);

                moveRules.Add(
                    new WordDocumentMoveRule
                    {
                        MoveBy = 1,
                        MoveFrom = [.. conditionalBlock.StartPosition],
                    });
            }
            else
            {
                if (placeholderInfoByID is not null)
                {
                    static void RemovePlaceholders(Dictionary<int, WordDocumentPlaceholderInfo> placeholderInfoByID, IWordDocumentBlock block)
                    {
                        if (block.Placeholders.Count > 0)
                        {
                            foreach (var placeholder in block.Placeholders)
                            {
                                placeholderInfoByID.Remove(placeholder);
                            }
                        }

                        if (block.ChildBlocks.Count > 0)
                        {
                            foreach (var childBlock in block.ChildBlocks)
                            {
                                RemovePlaceholders(placeholderInfoByID, childBlock);
                            }
                        }
                    }

                    RemovePlaceholders(placeholderInfoByID, conditionalBlock);
                }

                var currentLevel = conditionalBlock.StartPosition.Count;
                var originalParentLevel = currentLevel - 1;
                var currentElement = commentStart.NextSibling();
                if (currentElement is null)
                {
                    currentElement = commentStart.Parent;
                    currentLevel--;
                }

                while (currentElement is not null && currentElement != commentEnd)
                {
                    if (currentLevel == originalParentLevel)
                    {
                        originalParentLevel--;
                        var nextCurrentElement = currentElement.NextSibling();
                        while (currentElement is not null && nextCurrentElement is null)
                        {
                            currentElement = currentElement.Parent;
                            nextCurrentElement = currentElement?.NextSibling();
                            currentLevel--;
                            originalParentLevel--;
                        }

                        currentElement = nextCurrentElement;
                    }
                    else if (currentElement.HasChildren)
                    {
                        currentElement = currentElement.FirstChild;
                        currentLevel++;
                    }
                    else
                    {
                        var deleteElement = currentElement;
                        currentElement = currentElement.NextSibling();
                        if (currentElement is null)
                        {
                            currentElement = deleteElement.Parent;
                            currentLevel--;
                        }

                        deleteElement.SafeRemove();
                    }

                    Debug.Assert(currentElement is not null);
                }

                var updateLevel = originalParentLevel;
                var moveBy = conditionalBlock.EndPosition[updateLevel] - conditionalBlock.StartPosition[updateLevel] - 1;

                if (conditionalBlock.StartOfElement)
                {
                    var removeDepth = RemoveEmptyParent(commentStart);
                    var removeLevel = conditionalBlock.StartPosition.Count - removeDepth - 1;

                    if (removeLevel == updateLevel)
                    {
                        moveBy++;
                    }
                    else if (removeLevel < updateLevel)
                    {
                        updateLevel = removeLevel;
                        moveBy = 1;
                    }
                }

                if (conditionalBlock.EndOfElement)
                {
                    var removeDepth = RemoveEmptyParent(commentEnd);
                    var removeLevel = conditionalBlock.EndPosition.Count - removeDepth - 1;

                    if (removeLevel == updateLevel)
                    {
                        moveBy++;
                    }
                    else if (removeLevel < updateLevel)
                    {
                        updateLevel = removeLevel;
                        moveBy = 1;
                    }
                }

                if (MergeParents(commentStart, commentEnd))
                {
                    moveBy++;
                }

                if (moveBy > 0)
                {
                    var moveFrom = conditionalBlock.EndPosition.Take(updateLevel + 1).ToList();
                    moveRules.Add(new WordDocumentMoveRule { MoveBy = moveBy, MoveFrom = moveFrom });
                }
            }

            commentStart.SafeRemove();
            commentEnd.SafeRemove();
            commentReferenceRun?.SafeRemove();
            commentElement?.SafeRemove();

            return showBlock;
        }

        private async ValueTask ProcessBlockChildrenAsync(
            IPlaceholderReplacementContext replacementContext,
            IExpressionExecutionContext expressionExecutionContext,
            Func<WordDocumentConditionalBlock, OpenXmlElement> getCommentStart,
            Func<WordDocumentConditionalBlock, OpenXmlElement> getCommentEnd,
            WordprocessingCommentsPart commentsPart,
            IWordDocumentBlock block,
            List<WordDocumentMoveRule> moveRules,
            List<WordDocumentTableGroupBlock> tableGroupBlocks,
            Dictionary<int, WordDocumentPlaceholderInfo>? placeholderInfoByID = null,
            CancellationToken cancellationToken = default)
        {
            for (var i = block.ChildBlocks.Count - 1; i >= 0; i--)
            {
                var childBlock = block.ChildBlocks[i];

                switch (childBlock)
                {
                    case WordDocumentConditionalBlock childConditionalBlock:
                        await this.ProcessConditionalBlockAsync(
                            replacementContext,
                            expressionExecutionContext,
                            getCommentStart,
                            getCommentEnd,
                            commentsPart,
                            childConditionalBlock,
                            moveRules,
                            tableGroupBlocks,
                            placeholderInfoByID,
                            cancellationToken);
                        break;

                    case WordDocumentTableGroupBlock childTableGroupBlock:
                        if (!childTableGroupBlock.IsOptional || childTableGroupBlock.TablePlaceholders.Count > 0)
                        {
                            tableGroupBlocks.Add(childTableGroupBlock);
                        }
                        else if (childTableGroupBlock.ChildBlocks.Count > 0)
                        {
                            await this.ProcessBlockChildrenAsync(
                                replacementContext,
                                expressionExecutionContext,
                                getCommentStart,
                                getCommentEnd,
                                commentsPart,
                                childBlock,
                                moveRules,
                                tableGroupBlocks,
                                placeholderInfoByID,
                                cancellationToken);
                        }

                        break;
                }
            }
        }

        private static int RemoveEmptyParent(OpenXmlElement element, bool onlyFirstParagraph = false)
        {
            OpenXmlElement? itemToRemove = null;

            var checkCompleted = false;
            var checkItem = element.Parent;
            var depth = 1;
            var removeDepth = 0;
            while (!checkCompleted && checkItem is not (null or Body))
            {
                switch (checkItem)
                {
                    case TableCell cell:
                        if (cell.Parent is TableRow { InnerText.Length: 0 }
                            && !onlyFirstParagraph)
                        {
                            itemToRemove = checkItem;
                            removeDepth = depth;
                        }
                        else
                        {
                            itemToRemove = null;
                            checkCompleted = true;
                        }

                        break;

                    default:
                        if (checkItem.InnerText.Length == 0)
                        {
                            itemToRemove = checkItem;
                            removeDepth = depth;
                        }
                        else
                        {
                            checkCompleted = true;
                        }

                        break;
                }

                checkItem = checkItem.Parent;
                depth++;
            }

            if (itemToRemove?.Parent is not null)
            {
                itemToRemove.SafeRemove();
                return removeDepth;
            }

            return 0;
        }

        private static bool MergeParents(OpenXmlElement commentStart, OpenXmlElement commentEnd)
        {
            if (commentStart.Parent != commentEnd.Parent
                && commentStart.Parent is Paragraph { Parent: not null } startParagraph
                && commentEnd.Parent is Paragraph { Parent: not null } endParagraph
                && startParagraph.Parent == endParagraph.Parent)
            {
                for (var i = 0; i < endParagraph.ChildElements.Count; i++)
                {
                    var item = endParagraph.ChildElements[i];
                    if (item is not ParagraphProperties)
                    {
                        item.SafeRemove();
                        startParagraph.AppendChild(item);
                        i--;
                    }
                }

                endParagraph.SafeRemove();
                return true;
            }

            return false;
        }

        private static void FillTypedBlocks(
            IEnumerable<IWordDocumentBlock> blocks,
            List<WordDocumentConditionalBlock> conditionalBlocks,
            List<WordDocumentTableGroupBlock> tableGroupBlocks)
        {
            foreach (var block in blocks)
            {
                switch (block)
                {
                    case WordDocumentConditionalBlock conditionalBlock:
                        conditionalBlocks.Add(conditionalBlock);
                        break;

                    case WordDocumentTableGroupBlock tableGroupBlock:
                        tableGroupBlocks.Add(tableGroupBlock);
                        break;
                }
            }
        }

        private async ValueTask<bool> ProcessTableConditionalBlocksAsync(
            IPlaceholderReplacementContext context,
            List<WordDocumentTableGroupBlock> childTableGroupBlocks,
            List<WordDocumentConditionalBlock> conditionalBlocks,
            IList<OpenXmlElement> rowElements,
            IList<IPlaceholderReplacement>? replacements = null,
            IEditablePlaceholderTable? table = null,
            IPlaceholderRow? row = null)
        {
            var expressionExecutionContext = await context.GetOrCreateExpressionExecutionContextAsync();
            PlaceholderHelper.PrepareExpressionExecutionContext(
                expressionExecutionContext,
                context,
                replacements ?? context.Replacements,
                table,
                row);

            if (!context.ValidationResult.IsSuccessful())
            {
                return false;
            }

            var commentsPart = (WordprocessingCommentsPart) this.wordDocument!.MainDocumentPart!.Parts.First(p => p.OpenXmlPart is WordprocessingCommentsPart).OpenXmlPart;
            var moveRules = new List<WordDocumentMoveRule>();

            foreach (var conditionalBlock in conditionalBlocks)
            {
                await this.ProcessConditionalBlockByElementsAsync(
                    context,
                    expressionExecutionContext!,
                    rowElements,
                    commentsPart,
                    conditionalBlock,
                    moveRules,
                    childTableGroupBlocks,
                    context.CancellationToken);
            }

            return true;
        }

        private void StoreParsingResultInDocument(
            IReadOnlyList<WordDocumentPlaceholderInfo> placeholders,
            IReadOnlyList<IWordDocumentBlock> documentBlocks)
        {
            this.placeholderInfoByID = placeholders.ToDictionary(x => x.ID, x => x);
            this.blocks = documentBlocks;
        }

        private static void FillTableGroups(WordprocessingDocument wordDocument, IEnumerable<WordDocumentTableGroupBlock> tableGroups)
        {
            // Заполняем таблицы-по закладкам элементами документа
            // Закладка может целиком лежать внутри одного параграфа, тогда базовыми элементами будут являться объекты внутри параграфа.
            // Если закладка лежит между несколькими параграфами, то базовыми элементами считаем все параграфы и таблицы от первого до последнего
            // Поиском ведем на самом верхнем уровне среди этих двух элементов
            foreach (var table in tableGroups)
            {
                var firstElement = OpenXmlHelper.GetElementByPosition(wordDocument.MainDocumentPart, table.TableStartPosition);

                if (table.InParagraph)
                {
                    table.TableElement = firstElement;

                    var blockStart = OpenXmlHelper.GetElementByPosition(wordDocument.MainDocumentPart, table.StartPosition);
                    var blockEnd = OpenXmlHelper.GetElementByPosition(wordDocument.MainDocumentPart, table.EndPosition);

                    var element = blockStart.NextSibling();
                    while (element is not null
                           && element != blockEnd)
                    {
                        if (IsAllowedBaseElement(element))
                        {
                            table.BaseElements.Add(element);
                        }

                        element = element.NextSibling();
                    }
                }
                else
                {
                    var lastElement = OpenXmlHelper.GetElementByPosition(wordDocument.MainDocumentPart, table.TableEndPosition);
                    if (firstElement == lastElement)
                    {
                        if (IsAllowedBaseElement(firstElement))
                        {
                            table.BaseElements.Add(firstElement);
                        }
                    }
                    else
                    {
                        var nextElement = firstElement;

                        do
                        {
                            if (IsAllowedBaseElement(nextElement))
                            {
                                table.BaseElements.Add(nextElement);
                            }

                            nextElement = nextElement.NextSibling();
                        } while (nextElement is not null
                                 && nextElement != lastElement);

                        if (IsAllowedBaseElement(lastElement))
                        {
                            table.BaseElements.Add(lastElement);
                        }
                    }
                }

                if (table.ChildBlocks.Count > 0)
                {
                    static void FillBlocksInner(
                        WordprocessingDocument wordDocument,
                        IEnumerable<IWordDocumentBlock> blocks)
                    {
                        FillTableGroups(
                            wordDocument,
                            blocks.OfType<WordDocumentTableGroupBlock>());

                        foreach (var otherBlock in blocks)
                        {
                            if (otherBlock is WordDocumentTableGroupBlock
                                || otherBlock.ChildBlocks.Count == 0)
                            {
                                continue;
                            }

                            FillBlocksInner(
                                wordDocument,
                                otherBlock.ChildBlocks);
                        }
                    }

                    FillBlocksInner(wordDocument, table.ChildBlocks);
                }
            }
        }

        private static bool IsAllowedBaseElement(OpenXmlElement element)
        {
            return element is not BookmarkStart and not BookmarkEnd and not CommentRangeStart and not CommentRangeEnd;
        }

        private async ValueTask<IExpressionInterpreter> GetInterpreterAsync(CancellationToken cancellationToken)
        {
            return this.interpreter ??= await this.expressionInterpreterProvider.GetExpressionInterpreterAsync(ExpressionInterpreterNames.FileTemplates, cancellationToken);
        }

        #endregion

        #region Base Overrides

        [MemberNotNullWhen(true, nameof(extensionContext))]
        public new bool WithExtensions => base.WithExtensions;

        /// <inheritdoc/>
        protected override IPlaceholderReplaceExtensionContext? ExtensionContext => this.extensionContext;

        /// <inheritdoc/>
        protected override IPlaceholderReplaceExtensionContext CreateExtensionContext(IPlaceholderReplacementContext context)
        {
            return this.extensionContext = new WordPlaceholderReplaceExtensionContext(context, context.CancellationToken);
        }

        /// <inheritdoc/>
        protected override IEnumerable<IPlaceholderText> GetPlaceholdersFromElementOverride(OpenXmlElement baseElement, IList position)
        {
            return [];
        }

        /// <inheritdoc/>
        protected override async Task<IList<IPlaceholderText>?> GetPlaceholdersFromDatabaseAsync(
            IPlaceholderFindingContext context,
            IDbScope dbScope)
        {
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

                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                var dictionary = StorageHelper.DeserializeFromTypedJson(json);
                if (dictionary?.TryGet<int>(VersionKey) != (int) currentVersion)
                {
                    return null;
                }

                var placeholders = dictionary.GetSerializedList<WordDocumentPlaceholderInfo>(PlaceholdersKey) ??
                    (IReadOnlyList<WordDocumentPlaceholderInfo>) [];
                var documentBlocks = dictionary.GetTypedList<IWordDocumentBlock>(DocumentBlocksKey) ?? (IReadOnlyList<IWordDocumentBlock>) [];

                this.StoreParsingResultInDocument(
                    placeholders,
                    documentBlocks);

                return [.. placeholders.Select(x => x.ToPlaceholderText())];
            }
        }

        /// <inheritdoc/>
        protected override OpenXmlPackage InitDocument()
        {
            this.wordDocument = WordprocessingDocument.Open(this.Stream, true);

            if (this.WithExtensions)
            {
                this.extensionContext.Document = this.wordDocument;
            }

            return this.wordDocument;
        }

        /// <inheritdoc/>
        protected override ValueTask<IList<IPlaceholderText>> GetPlaceholdersFromDocumentAsync(IPlaceholderFindingContext context)
        {
            this.parsingResult = this.wordDocumentParser.ParseDocument(this.wordDocument!);
            context.ValidationResult.Add(this.parsingResult.ValidationResult);
            if (!this.parsingResult.ValidationResult.IsSuccessful)
            {
                return ValueTask.FromResult<IList<IPlaceholderText>>(Array.Empty<IPlaceholderText>());
            }

            this.StoreParsingResultInDocument(
                this.parsingResult.Placeholders,
                this.parsingResult.DocumentBlocks);

            return ValueTask.FromResult<IList<IPlaceholderText>>(
                [.. this.parsingResult.Placeholders.Select(x => x.ToPlaceholderText())]);
        }

        /// <inheritdoc/>
        protected override void PrepareDocumentForSave()
        {
        }

        /// <inheritdoc/>
        protected override void SaveDocument()
        {
            this.wordDocument?.Dispose();
            this.wordDocument = null;
        }

        /// <inheritdoc/>
        protected override async Task SavePlaceholdersInDatabaseAsync(
            IDbScope dbScope,
            IList<IPlaceholderText> placeholders,
            CancellationToken cancellationToken = default)
        {
            if (this.parsingResult is null)
            {
                return;
            }

            await using (dbScope.Create())
            {
                var dictionary = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    [PlaceholdersKey] = this.parsingResult.Placeholders.ToSerializedList(),
                    [DocumentBlocksKey] = this.parsingResult.DocumentBlocks.ToTypedList(),
                    [VersionKey] = currentVersion,
                };

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
                         .GroupBy(x =>
                             WordDocumentPlaceholderInfo.TryGetWordInfoID(x.Placeholder) is { } wordInfoID
                             && this.placeholderInfoByID!.TryGetValue(wordInfoID, out var wordInfo)
                                 ? TextPosition(wordInfo.Position)
                                 : string.Empty)
                         .OrderByDescending(x => x.Key))
            {
                if (replacements.Key == string.Empty)
                {
                    continue;
                }

                OpenXmlElement paragraph = this.wordDocument!.MainDocumentPart!.Document!;
                OpenXmlPart part = this.wordDocument.MainDocumentPart;

                var wordInfoID = WordDocumentPlaceholderInfo.TryGetWordInfoID(replacements.First().Placeholder);
                var wordPlaceholderInfo = this.placeholderInfoByID![wordInfoID!.Value];

                if (wordPlaceholderInfo.IsHyperlinkPlaceholder)
                {
                    await this.ReplaceElementsInRelationshipsAsync(part, wordPlaceholderInfo.HyperlinkID, [.. replacements]);
                }
                else
                {
                    var position = wordPlaceholderInfo.Position;
                    for (var i = 0; i < position.Count; i++)
                    {
                        var index = position[i];
                        if (index < 0)
                        {
                            part = part.Parts.ToArray()[~index].OpenXmlPart;
                            paragraph = part.RootElement!;
                        }
                        else
                        {
                            paragraph = paragraph.ChildElements[index];
                        }
                    }

                    var elemType = paragraph.GetType();
                    if (elemType == typeof(Paragraph) || elemType == typeof(Hyperlink))
                    {
                        await this.ReplaceElementsInCompositeElementAsync(paragraph, null, context.CancellationToken, [.. replacements]);
                    }

                    if (elemType == typeof(FieldCode))
                    {
                        // ReSharper disable once PossibleInvalidCastException
                        await this.ReplaceElementsInFieldCodeAsync((FieldCode) paragraph, [.. replacements]);
                    }
                }

                hasChanges = true;
            }

            return hasChanges;
        }

        /// <inheritdoc/>
        protected override async Task<bool> ReplaceTablePlaceholdersAsync(IPlaceholderReplacementContext context)
        {
            var tablePlaceholders = context.TablePlaceholders;
            if (tablePlaceholders.Count == 0
                || this.tableGroups.Count == 0)
            {
                return false;
            }

            // Для каждой таблицы организуем получение данных и производим замену
            foreach (var tableGroup in this.tableGroups)
            {
                await this.ReplaceGroupPlaceholdersAsync(
                    context,
                    tableGroup);
            }

            RemoveBlockElements(this.wordDocument!.MainDocumentPart!.RootElement!, this.tableGroups);
            ClearOldRelationships(this.wordDocument.MainDocumentPart, this.relationshipsIDs);
            return true;
        }

        /// <inheritdoc/>
        protected override async ValueTask<bool> PrepareDocumentForReplaceAsync(IPlaceholderReplacementContext context)
        {
            if (this.placeholderInfoByID is null
                || this.blocks is null)
            {
                // Если мы не выполняли операцию Find, то нужно выполнить парсинг документа, чтобы определить позиции блоков и плейсхолдеров.
                this.parsingResult = this.wordDocumentParser.ParseDocument(this.wordDocument!);
                context.ValidationResult.Add(this.parsingResult.ValidationResult);
                if (!this.parsingResult.ValidationResult.IsSuccessful)
                {
                    return false;
                }

                this.StoreParsingResultInDocument(
                    this.parsingResult.Placeholders,
                    this.parsingResult.DocumentBlocks);
            }

            static void FillTable(WordDocumentTableGroupBlock tableGroupBlock, IWordDocumentBlock source, Dictionary<int, IPlaceholder> placeholdersByWordInfo)
            {
                foreach (var wordInfoID in source.Placeholders)
                {
                    if (placeholdersByWordInfo.TryGetValue(wordInfoID, out var placeholder)
                        && placeholder is { Type: ITablePlaceholderType })
                    {
                        tableGroupBlock.TablePlaceholders.Add(placeholder);
                    }
                }

                for (var i = 0; i < source.ChildBlocks.Count; i++)
                {
                    var block = source.ChildBlocks[i];
                    if (block is WordDocumentTableGroupBlock childTableGroupBlock)
                    {
                        FillTable(childTableGroupBlock, childTableGroupBlock, placeholdersByWordInfo);

                        if (childTableGroupBlock.IsOptional
                            && childTableGroupBlock.TablePlaceholders.Count == 0)
                        {
                            source.ChildBlocks.RemoveAt(i--);
                            if (childTableGroupBlock.ChildBlocks.Count > 0)
                            {
                                source.ChildBlocks.InsertRange(i, childTableGroupBlock.ChildBlocks);
                            }
                        }
                    }
                    else
                    {
                        FillTable(tableGroupBlock, block, placeholdersByWordInfo);
                    }
                }
            }

            static void FillChildBlocks(IEnumerable<IWordDocumentBlock> blocks, Dictionary<int, IPlaceholder> placeholdersByWordInfo)
            {
                foreach (var block in blocks)
                {
                    switch (block)
                    {
                        case WordDocumentConditionalBlock conditionalBlock when conditionalBlock.ChildBlocks.Count > 0:
                            FillChildBlocks(conditionalBlock.ChildBlocks, placeholdersByWordInfo);
                            break;

                        case WordDocumentTableGroupBlock tableGroupBlock:
                            FillTable(tableGroupBlock, tableGroupBlock, placeholdersByWordInfo);
                            break;
                    }
                }
            }

            var hasChanges = false;

            var placeholdersByWordInfo = this.PrepareTablePlaceholders(context.TablePlaceholders);
            var conditionalBlocks = new List<WordDocumentConditionalBlock>();

            foreach (var block in this.blocks!)
            {
                if (block is WordDocumentConditionalBlock conditionalBlock)
                {
                    conditionalBlocks.Add(conditionalBlock);
                    if (conditionalBlock.ChildBlocks.Count > 0)
                    {
                        FillChildBlocks(conditionalBlock.ChildBlocks, placeholdersByWordInfo);
                    }
                }
                else if (block is WordDocumentTableGroupBlock tableGroupBlock)
                {
                    FillTable(tableGroupBlock, tableGroupBlock, placeholdersByWordInfo);

                    if (!tableGroupBlock.IsOptional
                        || tableGroupBlock.TablePlaceholders.Count > 0)
                    {
                        this.tableGroups.Add(tableGroupBlock);
                    }
                    else
                    {
                        if (tableGroupBlock.ChildBlocks.Count > 0)
                        {
                            foreach (var childBlock in tableGroupBlock.ChildBlocks)
                            {
                                switch (childBlock)
                                {
                                    case WordDocumentTableGroupBlock childTableGroup:
                                        this.tableGroups.Add(childTableGroup);
                                        break;

                                    case WordDocumentConditionalBlock childConditionalBlock:
                                        conditionalBlocks.Add(childConditionalBlock);
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            if (conditionalBlocks.Count > 0)
            {
                hasChanges = true;

                var expressionParameters = await context.GetOrCreateExpressionExecutionContextAsync();
                if (!context.ValidationResult.IsSuccessful())
                {
                    return false;
                }

                var mainDocumentPart = this.wordDocument!.MainDocumentPart!;
                var commentsPart = (WordprocessingCommentsPart) this.wordDocument!.MainDocumentPart!.Parts.First(p => p.OpenXmlPart is WordprocessingCommentsPart).OpenXmlPart;
                var moveRules = new List<WordDocumentMoveRule>();

                // Обрабатываем список в обратном порядке.
                for (var i = conditionalBlocks.Count - 1; i >= 0; i--)
                {
                    var conditionalBlock = conditionalBlocks[i];
                    await this.ProcessConditionalBlockByMainPartAsync(
                        context,
                        expressionParameters!,
                        mainDocumentPart,
                        commentsPart,
                        conditionalBlock,
                        moveRules,
                        this.tableGroups,
                        this.placeholderInfoByID,
                        context.CancellationToken);
                }

                if (moveRules.Count > 0)
                {
                    this.moveProcessor.MoveBlocks(this.blocks, moveRules);
                    this.moveProcessor.MovePlaceholder(this.placeholderInfoByID!.Values, moveRules);
                }
            }

            FillTableGroups(this.wordDocument!, this.tableGroups);

            return hasChanges;
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
            newText = RemoveInvalidChars(newText);
            var placeholderElements = this.WithExtensions
                ? new List<Run>()
                : null;

            var textRows = newText
                .Replace(Environment.NewLine, "\n", StringComparison.Ordinal)
                .Split('\n');

            var text = (Text) firstTextElement;
            text.Text = textRows[0];
            text.Space = SpaceProcessingModeValues.Preserve;

            if (lastTextElement is not null)
            {
                var lastText = (Text) lastTextElement;
                if (string.IsNullOrEmpty(lastText.Text))
                {
                    lastBaseElement?.SafeRemove();
                }
                else
                {
                    lastText.Space = SpaceProcessingModeValues.Preserve;
                }
            }

            if (firstBaseElement is Run run)
            {
                placeholderElements?.Add(run);
            }

            if (placeholder.Info.TryGet<bool>(SplitParPlaceholderFormatter.SplitParKey))
            {
                var firstBaseElementParagraph = OpenXmlHelper.FindParent<Paragraph>(firstBaseElement);

                for (var i = textRows.Length - 1; i > 0; i--)
                {
                    var newParagraph = new Paragraph();

                    if (firstBaseElementParagraph.GetFirstChild<ParagraphProperties>() is { } properties)
                    {
                        newParagraph.AppendChild(properties.CloneNode(true));
                    }

                    if (string.IsNullOrEmpty(textRows[i]))
                    {
                        var nextRow = new Run();
                        placeholderElements?.Add(nextRow);

                        newParagraph.AppendChild(nextRow);
                    }
                    else
                    {
                        var nextElement = firstBaseElement.CloneNode(true);

                        var nextTextElement = (Text?) GetTextElement(nextElement);
                        if (nextTextElement is not null)
                        {
                            nextTextElement.Text = textRows[i];
                        }

                        newParagraph.AppendChild(nextElement);
                        if (nextElement is Run nextRun)
                        {
                            placeholderElements?.Add(nextRun);
                        }
                    }

                    // Перенос в последний параграф всего, что расположено после текущего firstBaseElement в firstBaseElementParagraph.
                    if (i == textRows.Length - 1)
                    {
                        var isFoundFirstBaseElementParagraphChild = false;

                        foreach (var firstBaseElementParagraphChild in firstBaseElementParagraph.ChildElements.ToArray())
                        {
                            if (isFoundFirstBaseElementParagraphChild)
                            {
                                firstBaseElementParagraph.RemoveChild(firstBaseElementParagraphChild);
                                newParagraph.AppendChild(firstBaseElementParagraphChild);

                                continue;
                            }

                            if (firstBaseElement == firstBaseElementParagraphChild)
                            {
                                isFoundFirstBaseElementParagraphChild = true;
                            }
                        }
                    }

                    firstBaseElementParagraph.InsertAfterSelf(newParagraph);
                }
            }
            else
            {
                for (var i = textRows.Length - 1; i > 0; i--)
                {
                    if (string.IsNullOrEmpty(textRows[i]))
                    {
                        var nextRow = new Run(new Break());
                        placeholderElements?.Add(nextRow);

                        firstBaseElement.InsertAfterSelf(nextRow);
                    }
                    else
                    {
                        var nextElement = firstBaseElement.CloneNode(true);

                        var nextTextElement = (Text?) GetTextElement(nextElement);
                        if (nextTextElement is not null)
                        {
                            nextTextElement.Text = textRows[i];
                        }
                        firstBaseElement.InsertAfterSelf(nextElement);

                        if (nextElement is Run nextRun)
                        {
                            placeholderElements?.Add(nextRun);
                        }

                        // Т.к. внутри объекта Run может хранится и Text и Break одновременно, то при наличии Break, добавлять новый Break не нужно
                        if (!firstBaseElement.Descendants<Break>().Any())
                        {
                            var nextRow = new Run(new Break());
                            placeholderElements?.Add(nextRow);

                            firstBaseElement.InsertAfterSelf(nextRow);
                        }
                    }
                }
            }

            if (this.WithExtensions)
            {
                this.extensionContext.SetPlaceholderData(placeholderElements);
                await this.AfterPlaceholderReplaceAsync(this.extensionContext.ReplacementContext);
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> ReplaceImageAsync(
            OpenXmlElement baseElement,
            IPlaceholder placeholder,
            PlaceholderValue value,
            CancellationToken cancellationToken = default)
        {
            var graphicBase = OpenXmlHelper.FindInParentChildren<A.Graphic>(baseElement)?.Parent;
            if (graphicBase is null)
            {
                return false;
            }

            var data = value.Data;

            if (data is null || data.Length == 0)
            {
                // пустое изображение при замене удаляет надпись
                var run = OpenXmlHelper.FindParent<Run>(graphicBase);
                if (run is not null)
                {
                    run.SafeRemove();
                    return true;
                }

                return false;
            }

            var imageParameters = value.FormatResult.GetImageParameters();
            var imagePartType = OpenXmlHelper.GetImagePartType(imageParameters);

            var existingGraphic = graphicBase.GetFirstChild<A.Graphic>();
            var existingExtent = graphicBase.GetFirstChild<DW.Extent>();
            var existingProperties = existingGraphic?.GraphicData?.Descendants<WPS.ShapeProperties>().FirstOrDefault();

            var graphic = CreateGraphicFromImage(
                data,
                imagePartType,
                graphicBase.GetPart()!,
                existingProperties,
                existingExtent,
                imageParameters.Width,
                imageParameters.Height,
                imageParameters.Reformat,
                value.FormatResult.Info);

            existingGraphic?.Remove();

            graphicBase.AppendChild(graphic);

            // замещающий текст
            var docProperties = graphicBase.GetFirstChild<DW.DocProperties>();
            if (docProperties is not null)
            {
                docProperties.Title = await LocalizeFormatAsync(imageParameters.AlternativeText);
            }

            // теперь магия, если надпись была на якоре
            if (graphicBase is DW.Anchor anchor)
            {
                // обнуляем свойства distT="0" distB="0" distL="0" distR="0"
                anchor.DistanceFromTop = 0u;
                anchor.DistanceFromBottom = 0u;
                anchor.DistanceFromLeft = 0u;
                anchor.DistanceFromRight = 0u;

                // удаляем детей <wp14:sizeRelH/> и <wp14:sizeRelV/>
                anchor.RemoveAllChildren<Wp14.RelativeWidth>();
                anchor.RemoveAllChildren<Wp14.RelativeHeight>();
            }

            var alternateContent = OpenXmlHelper.FindParent<AlternateContent>(graphicBase);
            if (alternateContent is not null)
            {
                // мы внутри т.н. альтернативного контента, где есть наша надпись/картинка,
                // а ещё некое Fallback-значение, которое мы хотим выпилить, потому что для картинок оно бесполезно,
                // и Word по умолчанию его не генерирует

                var alternateContentBase = alternateContent.Parent;

                if (alternateContentBase is not null
                    && alternateContent.GetFirstChild<AlternateContentChoice>()?.FirstChild is { } choiceChild)
                {
                    // было: alternateContentBase -> alternateContent -> choice -> choiceChild -> ... -> graphicBase -> graphic
                    // нужно: alternateContentBase -> choiceChild -> ... -> graphicBase -> graphic

                    choiceChild.Remove();
                    alternateContent.Remove();

                    alternateContentBase.AppendChild(choiceChild);
                }
            }

            return true;
        }

        /// <inheritdoc/>
        protected override void CleanAfterChanges()
        {
            // исправляем объекты "Надпись": идентификаторы DocProperties для всех строк должны быть уникальны
            foreach (var docProperties
                     in this.wordDocument!.MainDocumentPart?.Document?.Descendants<DW.DocProperties>() ?? [])
            {
                docProperties.Id = this.docPropertiesNextId++;
            }
        }

        /// <inheritdoc/>
        protected override bool CheckTextElement(OpenXmlLeafTextElement baseElementText)
        {
            return baseElementText is not FieldCode;
        }

        /// <inheritdoc/>
        protected override IDisposable ExecuteInPlaceholderContext(IPlaceholder placeholder, PlaceholderValue? placeholderValue)
        {
            return this.extensionContext!.ExecuteInPlaceholderContext(placeholder, placeholderValue);
        }

        #endregion
    }
}
