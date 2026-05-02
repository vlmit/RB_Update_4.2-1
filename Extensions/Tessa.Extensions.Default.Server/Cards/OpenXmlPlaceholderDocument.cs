#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Cards
{
    /// <summary>
    /// Базовый класс для документа OpenXML.
    /// </summary>
    /// <param name="stream">Поток файла документа, в котором должны быть заменены плейсхолдеры.</param>
    /// <param name="templateID">ID карточки шаблона файла.</param>
    public abstract partial class OpenXmlPlaceholderDocument(MemoryStream stream, Guid templateID) : PlaceholderDocument
    {
        #region Regex Types

        [GeneratedRegex(@"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]", RegexOptions.Compiled)]
        private static partial Regex InvalidXmlCharsRegex();

        #endregion

        #region ReplacementStatus Protected Enum

        /// <summary>
        /// Перечислимый тип, обозначающий статус замены плейсхолдера в документе.
        /// </summary>
        protected enum ReplacementStatus
        {
            /// <summary>
            /// Плейсхолдер не найден.
            /// </summary>
            None = 0,

            /// <summary>
            /// Найдено начало плейсхолдера.
            /// </summary>
            PartFound = 1,

            /// <summary>
            /// Плейсхолдер найден и успешно заменен.
            /// </summary>
            Replaced = 2,

            /// <summary>
            /// Плейсхолдер найден и не заменен.
            /// </summary>
            NotReplaced = 3,

            /// <summary>
            /// Все плейсхолдеры заменены.
            /// </summary>
            AllReplaced = 4,
        }

        #endregion

        #region Consts

        protected const string HyperlinkRemoveHead = "https://tessalink/?link=";

        protected const int HyperlinkRemoveHeadLength = 24;

        #endregion

        #region Fields

        /// <summary>
        /// ID карточки шаблона файла.
        /// </summary>
        protected readonly Guid TemplateID = templateID;

        /// <summary>
        /// Регулярное выражение для поиска всех запрещенных в XML символов.
        /// https://stackoverflow.com/questions/397250/unicode-regex-invalid-xml-characters/961504#961504
        /// </summary>
        private static readonly Regex invalidXmlChars = InvalidXmlCharsRegex();

        #endregion

        #region Properties

        /// <summary>
        /// Поток файла документа, в которой должны быть или уже были заменены плейсхолдеры.
        /// </summary>
        public MemoryStream Stream { get; } = NotNullOrThrow(stream);

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Метод для получения информации о плейсхолдерах документа из базы данных.
        /// </summary>
        /// <param name="context">Контекст поиска плейсхолдеров.</param>
        /// <param name="dbScope">Объект dbScope текущего подключения к базе данных.</param>
        /// <returns>Возвращает список плейсхолдеров документа из базы данных.</returns>
        protected abstract Task<IList<IPlaceholderText>?> GetPlaceholdersFromDatabaseAsync(
            IPlaceholderFindingContext context,
            IDbScope dbScope);

        /// <summary>
        /// Метод для сохранения информации о плейсхолдерах документа в базу данных.
        /// </summary>
        /// <param name="dbScope">Объект dbScope текущего подключения к базе данных.</param>
        /// <param name="placeholders">Список плейсхолдеров для сохранения.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Асинхронная задача.</returns>
        protected abstract Task SavePlaceholdersInDatabaseAsync(
            IDbScope dbScope,
            IList<IPlaceholderText> placeholders,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Метод для инициализации документа.
        /// </summary>
        /// <returns>Возвращает инициализированный документ.</returns>
        protected abstract OpenXmlPackage InitDocument();

        /// <summary>
        /// Метод для подготовки документа к сохранению.
        /// </summary>
        protected abstract void PrepareDocumentForSave();

        /// <summary>
        /// Метод для сохранения инициализированного документа.
        /// </summary>
        protected abstract void SaveDocument();

        /// <summary>
        /// Метод для получения плейсхолдеров из объекта документа.
        /// </summary>
        /// <returns>Возвращает список плейсхолдеров, найденных в документе.</returns>
        protected abstract ValueTask<IList<IPlaceholderText>> GetPlaceholdersFromDocumentAsync(IPlaceholderFindingContext context);

        /// <summary>
        /// Метод для поиска плейсхолдеров внутри элемента документа.
        /// </summary>
        /// <param name="baseElement">Элемент, в котором производится поиск плейсхолдеров.</param>
        /// <param name="position">Позиция элемента в документе.</param>
        /// <returns>Возвращает список плейсхолдеров, найденных в текущем элементе.</returns>
        protected abstract IEnumerable<IPlaceholderText> GetPlaceholdersFromElementOverride(OpenXmlElement baseElement, IList position);

        /// <summary>
        /// Метод для подготовки документа к замене плейсхолдеров.
        /// </summary>
        /// <param name="context">Контекст замены плейсхолдеров.</param>
        /// <returns>Значение <c>true</c>, если при подготовке документа он был изменён, иначе <c>false</c>.</returns>
        protected abstract ValueTask<bool> PrepareDocumentForReplaceAsync(IPlaceholderReplacementContext context);

        /// <summary>
        /// Метод для замены плейсхолдеров типа Field.
        /// </summary>
        /// <param name="context">Контекст замены плейсхолдеров.</param>
        /// <returns>Возвращает true, если был заменен хотя бы один плейсхолдер.</returns>
        protected abstract Task<bool> ReplaceFieldPlaceholdersAsync(IPlaceholderReplacementContext context);

        /// <summary>
        /// Метод для замены плейсхолдеров типа Table.
        /// </summary>
        /// <param name="context">Контекст замены плейсхолдеров.</param>
        /// <returns>Возвращает true, если был заменен хотя бы один плейсхолдер.</returns>
        protected abstract Task<bool> ReplaceTablePlaceholdersAsync(IPlaceholderReplacementContext context);

        /// <summary>
        /// Метод, определяющий правила замены текста в элементе документа.
        /// </summary>
        /// <param name="firstBaseElement">Первый базовый элемент, в котором производится замена текста. Обычно это Run или сам Text.</param>
        /// <param name="firstTextElement">Первый объект текста для замены.</param>
        /// <param name="lastBaseElement">Последний базовый элемент, в котором производится замена текста. Обычно это Run или сам Text.</param>
        /// <param name="lastTextElement">Последний элемент текста для замены.</param>
        /// <param name="newText">Текст, на который выполняется замена.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <param name="placeholder"><inheritdoc cref="IPlaceholder" path="/summary"/></param>
        /// <param name="placeholderValue"><inheritdoc cref="PlaceholderValue" path="/summary"/></param>
        /// <returns>Асинхронная задача.</returns>
        protected abstract Task ReplaceTextAsync(
            OpenXmlElement firstBaseElement,
            OpenXmlLeafTextElement firstTextElement,
            OpenXmlElement? lastBaseElement,
            OpenXmlLeafTextElement? lastTextElement,
            string newText,
            IPlaceholder placeholder,
            PlaceholderValue placeholderValue,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Метод, определяющий правила замены изображения в элементе документа.
        /// Возвращает признак того, что замена выполнена успешно.
        /// </summary>
        /// <param name="baseElement">Базовый элемент, в котором производится замена текста.</param>
        /// <param name="placeholder"><inheritdoc cref="IPlaceholder" path="/summary"/></param>
        /// <param name="value"><inheritdoc cref="PlaceholderValue" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Признак того, что замена выполнена успешно.</returns>
        protected abstract Task<bool> ReplaceImageAsync(
            OpenXmlElement baseElement,
            IPlaceholder placeholder,
            PlaceholderValue value,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Метод для выполнения постобработки документа после изменений.
        /// </summary>
        protected virtual void CleanAfterChanges()
        {
        }

        /// <summary>
        /// Метод производит проверку, что данный текстовый элемент допустим для замены текста в нем.
        /// </summary>
        /// <param name="baseElementText">Проверяемый текстовый элемент.</param>
        /// <returns><see langword="true"/>, если текстовый элемент допустим для замены текста, иначе - <see langword="false"/>.</returns>
        protected virtual bool CheckTextElement(OpenXmlLeafTextElement baseElementText)
        {
            return true;
        }

        /// <summary>
        /// Определяет область выполнения расширения плейсхолдеров в рамках конкретного плейсхолдера.
        /// </summary>
        /// <param name="placeholder"><inheritdoc cref="IPlaceholder" path="/summary"/></param>
        /// <param name="placeholderValue"><inheritdoc cref="PlaceholderValue" path="/summary"/></param>
        /// <returns>Объект, вызов метода <see cref="IDisposable.Dispose"/> которого закрывает область выполнения.</returns>
        protected abstract IDisposable ExecuteInPlaceholderContext(
            IPlaceholder placeholder,
            PlaceholderValue? placeholderValue);

        #endregion

        #region Protected Methods

        /// <summary>
        /// Метод для удаления их строки всех запрещенных в XML символов.
        /// </summary>
        /// <param name="text">Переданная строка.</param>
        /// <returns></returns>
        protected static string RemoveInvalidChars(string text) =>
            string.IsNullOrEmpty(text) ? text : invalidXmlChars.Replace(text, string.Empty);

        /// <summary>
        /// Производит замену плейсхолдеров в гиперссылке.
        /// </summary>
        /// <param name="mainPart">Объект документа, хранящих список гиперссылок.</param>
        /// <param name="relID">ID гиперссылки.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <param name="replacements">Массив плейсхолдеров для замены.</param>
        /// <returns>Асинхронная задача.</returns>
        protected async Task ReplaceElementsInRelationshipsAsync(
            OpenXmlPart mainPart,
            string relID,
            params IEnumerable<IPlaceholderReplacement> replacements)
        {
            if (mainPart.HyperlinkRelationships.FirstOrDefault(a => a.Id == relID) is not { } relationship)
            {
                return;
            }

            var sb = new StringBuilder(Uri.UnescapeDataString(relationship.Uri.ToString()));
            sb.Replace(HyperlinkRemoveHead, string.Empty, 0, Math.Min(HyperlinkRemoveHeadLength, sb.Length));

            foreach (var replacement in replacements)
            {
                var newValue = replacement.NewValue;
                using var _ = this.WithExtensions
                    ? this.ExecuteInPlaceholderContext(replacement.Placeholder, newValue)
                    : null;

                if (this.WithExtensions)
                {
                    await this.BeforePlaceholderReplaceAsync(this.ExtensionContext.ReplacementContext);
                    newValue = this.ExtensionContext.PlaceholderValue;
                }

                sb.Replace(replacement.Placeholder.Text, newValue?.Text);

                if (this.WithExtensions)
                {
                    await this.AfterPlaceholderReplaceAsync(this.ExtensionContext.ReplacementContext);
                }
            }

            mainPart.DeleteReferenceRelationship(relationship);
            mainPart.AddHyperlinkRelationship(new Uri(sb.ToString(), UriKind.RelativeOrAbsolute), true, relID);
        }

        /// <summary>
        /// Производит замену плейсхолдеров в гиперссылке с ее копированием.
        /// </summary>
        /// <param name="mainPart">Объект документа, хранящих список гиперссылок.</param>
        /// <param name="originalRelID">Идентификатор оригинальной гиперссылки, в которой находился плейсхолдер.</param>
        /// <param name="relID">Идентификатор копируемой гиперссылки, в которой производится замена плейсхолдеров.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <param name="replacements">Плейсхолдеры для замены.</param>
        /// <returns>Идентификатор новой гиперссылки.</returns>
        protected async Task<string> ReplaceElementsInRelationshipsWithCopyAsync(
            OpenXmlPart mainPart,
            string originalRelID,
            string relID,
            params IEnumerable<IPlaceholderReplacement> replacements)
        {
            var relationship = mainPart.HyperlinkRelationships.FirstOrDefault(a => a.Id == relID);
            if (relationship is not null)
            {
                var sb = new StringBuilder(Uri.UnescapeDataString(relationship.Uri.ToString()));
                sb.Replace(HyperlinkRemoveHead, string.Empty, 0, Math.Min(HyperlinkRemoveHeadLength, sb.Length));

                foreach (var replacement in replacements)
                {
                    var newValue = replacement.NewValue;
                    using var _ = this.WithExtensions
                        ? this.ExecuteInPlaceholderContext(replacement.Placeholder, newValue)
                        : null;

                    if (this.WithExtensions)
                    {
                        await this.BeforePlaceholderReplaceAsync(this.ExtensionContext.ReplacementContext);
                        newValue = this.ExtensionContext.PlaceholderValue;
                    }

                    sb.Replace(replacement.Placeholder.Text, newValue?.Text);

                    if (this.WithExtensions)
                    {
                        await this.AfterPlaceholderReplaceAsync(this.ExtensionContext.ReplacementContext);
                    }
                }

                if (!string.Equals(relID, originalRelID, StringComparison.OrdinalIgnoreCase))
                {
                    mainPart.DeleteReferenceRelationship(relationship);
                }

                ReferenceRelationship newRelationsip = mainPart.AddHyperlinkRelationship(new Uri(sb.ToString(), UriKind.RelativeOrAbsolute), true);
                return newRelationsip.Id;
            }

            return relID;
        }

        /// <summary>
        /// Производит замену Replacement'ов в заданном элементе
        /// </summary>
        /// <param name="element">Параграф, в котором производится замена плейсхолдеров.</param>
        /// <param name="fromElement">Элемент в параграфе, начиная с которого выполняется замена, или <c>null</c>, если замена выполняется с начала элемента.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <param name="placeholders">Массив плейсхолдеров для замены.</param>
        /// <returns>Асинхронная задача.</returns>
        protected async Task ReplaceElementsInCompositeElementAsync(
            OpenXmlElement element,
            OpenXmlElement? fromElement = null,
            CancellationToken cancellationToken = default,
            params IReadOnlyList<IPlaceholderReplacement> placeholders)
        {
            var currentReplacement = placeholders[0];
            var newValue = currentReplacement.NewValue;
            IDisposable? placeholderContext = null;
            if (this.WithExtensions)
            {
                placeholderContext = this.ExecuteInPlaceholderContext(currentReplacement.Placeholder, currentReplacement.NewValue);
                await this.BeforePlaceholderReplaceAsync(this.ExtensionContext.ReplacementContext);
                newValue = this.ExtensionContext.PlaceholderValue ?? PlaceholderValue.Empty;
            }

            var number = 1;

            var placeholderElements = new List<OpenXmlElement>();
            foreach (var run in element.ChildElements.ToList())
            {
                if (fromElement is not null)
                {
                    if (run == fromElement)
                    {
                        fromElement = null;
                    }
                    else
                    {
                        continue;
                    }
                }

                ReplacementStatus res;

                do
                {
                    res = await this.ReplaceElementAsync(run, currentReplacement.Placeholder, newValue, cancellationToken, [.. placeholderElements]);

                    if (res == ReplacementStatus.Replaced)
                    {
                        if (placeholders.Count == number)
                        {
                            res = ReplacementStatus.AllReplaced;
                        }
                        else
                        {
                            currentReplacement = placeholders[number++];
                            newValue = currentReplacement.NewValue;

                            if (this.WithExtensions)
                            {
                                placeholderContext?.Dispose();
                                placeholderContext = this.ExecuteInPlaceholderContext(currentReplacement.Placeholder, currentReplacement.NewValue);
                                await this.BeforePlaceholderReplaceAsync(this.ExtensionContext.ReplacementContext);
                                newValue = this.ExtensionContext.PlaceholderValue ?? PlaceholderValue.Empty;
                            }

                            placeholderElements = [];
                        }
                    }

                    if (res == ReplacementStatus.NotReplaced)
                    {
                        placeholderElements = [];
                    }
                } while (res is ReplacementStatus.Replaced
                         or ReplacementStatus.NotReplaced);

                if (res == ReplacementStatus.PartFound)
                {
                    placeholderElements.Add(run);
                }
                else
                {
                    placeholderElements = [];
                }

                if (res == ReplacementStatus.AllReplaced)
                {
                    break;
                }
            }

            if (this.WithExtensions)
            {
                placeholderContext?.Dispose();
            }
        }

        /// <summary>
        /// Производит замену плейсхолдера в baseElement. Если текстовая часть baseElement содержит только начало плейсхолдера,
        /// то возвращаем ReplacementStatus.PartFound.
        /// </summary>
        /// <param name="baseElement">Базовый элемент, в котором производится замена плейсхолдера.</param>
        /// <param name="placeholder"><inheritdoc cref="IPlaceholder" path="/summary"/></param>
        /// <param name="newValue"><inheritdoc cref="PlaceholderValue" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <param name="partElements">Объекты, содержащие части плейсхолдера.</param>
        /// <returns>Результат замены плейсхолдера.</returns>
        protected async Task<ReplacementStatus> ReplaceElementAsync(
            OpenXmlElement baseElement,
            IPlaceholder placeholder,
            PlaceholderValue newValue,
            CancellationToken cancellationToken = default,
            params IReadOnlyList<OpenXmlElement> partElements)
        {
            if (newValue.Type == PlaceholderValueTypes.Image)
            {
                var success = await this.ReplaceImageAsync(baseElement, placeholder, newValue, cancellationToken);
                if (this.WithExtensions)
                {
                    await this.AfterPlaceholderReplaceAsync(this.ExtensionContext!.ReplacementContext);
                }

                return success ? ReplacementStatus.AllReplaced : ReplacementStatus.None;
            }

            var fromStart = partElements is null || partElements.Count == 0;
            var baseElementText = GetTextElement(baseElement);
            while (baseElementText is not null)
            {
                if (!this.CheckTextElement(baseElementText))
                {
                    baseElementText = baseElementText.NextSibling<OpenXmlLeafTextElement>();
                    continue;
                }

                if (baseElementText.Text.Contains(placeholder.Text, StringComparison.Ordinal))
                {
                    await this.ReplaceTextAsync(
                        baseElement,
                        baseElementText,
                        null,
                        null,
                        baseElementText.Text.Replace(
                            placeholder.Text,
                            newValue.Text,
                            StringComparison.Ordinal),
                        placeholder,
                        newValue,
                        cancellationToken);

                    return ReplacementStatus.Replaced;
                }

                // Если часть начала есть и она не имеет закрывающего тега в этой части текста, правее последнего открывающего тега
                if (fromStart
                    && baseElementText.Text.Contains(PlaceholderHelper.LeftBracket, StringComparison.Ordinal)
                    && baseElementText.Text.LastIndexOf(PlaceholderHelper.LeftBracket, StringComparison.Ordinal)
                    > baseElementText.Text.LastIndexOf(PlaceholderHelper.RightBracket, StringComparison.Ordinal))
                {
                    return ReplacementStatus.PartFound;
                }

                if (!fromStart && baseElementText.InnerXml.Contains(PlaceholderHelper.RightBracket, StringComparison.Ordinal))
                {
                    var firstElementText = GetTextElement(partElements![0]);
                    if (firstElementText is null)
                    {
                        // Начало плейсхолдера не найдено, значит, вероятно, он уже был удалён/изменён. Пропускаем его.
                        return ReplacementStatus.NotReplaced;
                    }

                    var startIndex = firstElementText.Text.LastIndexOf(PlaceholderHelper.LeftBracket, StringComparison.Ordinal);
                    var endIndex = baseElementText.Text.IndexOf(PlaceholderHelper.RightBracket, StringComparison.Ordinal);

                    var firstElementString = firstElementText.Text;
                    var foundPlaceholder = StringBuilderHelper.Acquire()
                        .Append(firstElementString, startIndex, firstElementString.Length - startIndex);

                    for (var j = 1; j < partElements.Count; j++)
                    {
                        var textElement = GetTextElement(partElements[j]);
                        if (textElement is not null)
                        {
                            foundPlaceholder
                                .Append(textElement.Text);
                        }
                    }

                    foundPlaceholder
                        .Append(baseElementText.Text, 0, endIndex + 1);

                    if (foundPlaceholder.ToString() != placeholder.Text)
                    {
                        // Плейсхолдер не совпадает, пропускаем его
                        return ReplacementStatus.NotReplaced;
                    }

                    // Удаляем часть placeholder из последнего элемента
                    baseElementText.Text = baseElementText.Text[(endIndex + 1)..];

                    // Удаляем все средние элементы между началом и концом плейсхолдера
                    for (var j = 1; j < partElements.Count; j++)
                    {
                        partElements[j].Remove();
                    }

                    // Записываем все в первый элемент с частью плейсхолдера
                    await this.ReplaceTextAsync(
                        partElements[0],
                        firstElementText,
                        baseElement,
                        baseElementText,
                        firstElementText.Text[..startIndex] + newValue.Text,
                        placeholder,
                        newValue,
                        cancellationToken);

                    return ReplacementStatus.Replaced;
                }

                baseElementText = baseElementText.NextSibling<OpenXmlLeafTextElement>();
            }

            return fromStart
                ? ReplacementStatus.None
                : ReplacementStatus.PartFound;
        }

        /// <summary>
        /// Находим во всех частях и элементах basePart плейсхолдеры.
        /// </summary>
        /// <param name="basePart">Базовая часть документа, в которой производится поиск.</param>
        /// <param name="position">Текущая позиция внутри документа. Позиция частей документов записывается с отрицательным знаком, а элементов - с положительным.</param>
        /// <returns>Возвращает все найденные в данной части документа плейсхолдеры.</returns>
        protected List<IPlaceholderText> GetPlaceholdersFromPart(OpenXmlPart basePart, params IReadOnlyCollection<object> position)
        {
            var result = this.GetPlaceholdersFromElement(basePart.RootElement, position);

            result.AddRange(GetPlaceholdersFromRelationships(basePart.HyperlinkRelationships, position));

            var curPos = -1;
            foreach (var part in basePart.Parts)
            {
                var newPosition = new List<object>(position) { curPos-- };
                result.AddRange(this.GetPlaceholdersFromPart(part.OpenXmlPart, [.. newPosition]));
            }

            return result;
        }


        /// <summary>
        /// Находим во всех дочерних элементах <value>baseElement</value> плейсхолдеры.
        /// </summary>
        /// <param name="baseElement">Базовый элемент, начиная с которого производим поиск.</param>
        /// <param name="position">Текущая позиция внутри документа. Позиция частей документов записывается с отрицательным знаком, а элементов - с положительным.</param>
        /// <returns>Возвращает все найденные в данном элементе плейсхолдеры.</returns>
        protected List<IPlaceholderText> GetPlaceholdersFromElement(OpenXmlElement? baseElement, params IReadOnlyCollection<object> position)
        {
            var result = new List<IPlaceholderText>();
            if (baseElement is null)
            {
                return result;
            }

            var curPos = 0;
            foreach (var element in baseElement.ChildElements)
            {
                var newPosition = new List<object>(position) { curPos++ };
                result.AddRange(this.GetPlaceholdersFromElementOverride(element, newPosition));

                if (element.HasChildren)
                {
                    result.AddRange(this.GetPlaceholdersFromElement(element, [.. newPosition]));
                }
            }

            return result;
        }

        #endregion

        #region Protected Static Methods

        /// <summary>
        /// Метод для получения элемента по плейсхолдеру в документе.
        /// </summary>
        /// <param name="mainPart">Объект документа, в котором производится поиск элемента.</param>
        /// <param name="placeholder">Плейсхолдер, для которого производится поиск соответствующего элемента в документе.</param>
        /// <returns>Возвращает базовый элемент, соответствующий заданному плейсхолдеру.</returns>
        protected static OpenXmlElement? GetElementByPlaceholder(OpenXmlPart mainPart, IPlaceholder placeholder)
        {
            if (IsRelationship(placeholder))
            {
                return null;
            }

            var position = placeholder.Info.Get<IList>(OpenXmlHelper.PositionField);
            return OpenXmlHelper.GetElementByPosition(mainPart, position);
        }

        /// <summary>
        /// Получает список плейсхолдеров из списка Relationships.
        /// </summary>
        /// <param name="relationships">Список Relationships.</param>
        /// <param name="position">Определяет позицию объектов relationships в документе.</param>
        /// <returns>Возвращает найденные в relationships плейсхолдеры.</returns>
        protected static List<IPlaceholderText> GetPlaceholdersFromRelationships(IEnumerable<ReferenceRelationship> relationships, params IReadOnlyCollection<object> position)
        {
            var result = new List<IPlaceholderText>();

            foreach (var referenceRelationship in relationships)
            {
                var relationship = (HyperlinkRelationship) referenceRelationship;

                var codeText = relationship.Uri.ToString();
                var decodedText = Uri.UnescapeDataString(codeText);

                foreach (Match match in PlaceholderHelper.UnescapedPlaceholdersRegex.Matches(decodedText))
                {
                    var text = match.Value;
                    var value = PlaceholderHelper.TryGetValue(text);
                    if (value is not null)
                    {
                        var newPlaceholder = new PlaceholderText(text, value);
                        var newPosition = new List<object>();
                        if (position is not null)
                        {
                            newPosition.AddRange(position);
                        }

                        newPosition.Add(OpenXmlHelper.HyperlinkPath);
                        newPosition.Add(relationship.Id);

                        newPlaceholder.Info[OpenXmlHelper.PositionField] = newPosition;
                        newPlaceholder.Info[OpenXmlHelper.IndexField] = match.Index;

                        result.Add(newPlaceholder);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Метод для очистки старых ссылок из документа.
        /// </summary>
        /// <param name="mainPart">Часть документа, из которой удаляются ссылки.</param>
        /// <param name="relationshipIDs">Список плейсхолдеров в ссылках для удаления.</param>
        protected static void ClearOldRelationships(OpenXmlPart mainPart, IList<string> relationshipIDs)
        {
            foreach (var relID in relationshipIDs.Distinct())
            {
                mainPart.DeleteReferenceRelationship(relID);
            }
        }

        /// <summary>
        /// Возвращает первый дочерний элемент с типом <see cref="OpenXmlLeafTextElement"/> среди всех дочерних элементов объекта <paramref name="baseElement"/>.
        /// </summary>
        /// <param name="baseElement">Базовый элемент, начиная с которого производим поиск.</param>
        /// <returns>Возвращает объект типа <see cref="OpenXmlLeafTextElement"/>, или null, если <paramref name="baseElement"/> не содержит дочерних элементов данного типа.</returns>
        protected static OpenXmlLeafTextElement? GetTextElement(OpenXmlElement baseElement)
        {
            var textElement = baseElement as OpenXmlLeafTextElement;
            if (textElement is not null)
            {
                return textElement;
            }

            if (!baseElement.HasChildren)
            {
                return null;
            }

            foreach (var e in baseElement.ChildElements)
            {
                textElement = e as OpenXmlLeafTextElement ?? GetTextElement(e);

                if (textElement is not null)
                {
                    break;
                }
            }

            return textElement;
        }

        /// <summary>
        /// Метод переводит позицию объекта в документе в строку.
        /// </summary>
        /// <param name="position">Позиция объекта в документе.</param>
        /// <returns>Возвращает строку вида "position[0]->position[1]->...->position[n]".</returns>
        protected static string TextPosition(IList? position)
        {
            return position is null ? string.Empty : OpenXmlHelper.TextPosition(position);
        }

        /// <summary>
        /// Метод для проверки принадлежности плейсхолдера к Relationship.
        /// </summary>
        /// <param name="placeholder">Проверяемый плейсхолдер.</param>
        /// <returns>Метод возвращает true, если плейсхолдер находится в ссылке.</returns>
        protected static bool IsRelationship(IPlaceholder placeholder)
        {
            if (placeholder is null
                || placeholder.Info.TryGet<IList>(OpenXmlHelper.PositionField, null) is not { Count: > 0 } position
               )
            {
                return false;
            }

            return position.Count > 0 && position.Cast<object>().Any(x => x.ToString() == OpenXmlHelper.HyperlinkPath);
        }


        /// <summary>
        /// Получает дочерний элемент <paramref name="newParent"/>, соответствующий элементу <paramref name="baseChild"/> относительно <paramref name="baseParent"/>. Элемент <paramref name="newParent"/> должен быть полной копией элемента <paramref name="baseParent"/>.
        /// </summary>
        /// <typeparam name="TType">Тип искомого объекта.</typeparam>
        /// <param name="baseParent">Родительский элемент, относительно которого ведется поиск.</param>
        /// <param name="baseChild">Дочерний элемент, соответствие которого мы ищем в <paramref name="newParent"/>.</param>
        /// <param name="newParent">Копия родительского элемента, дочерний элемент которого мы ищем.</param>
        /// <returns>Возвращает дочерний элемент <paramref name="newParent"/>, соответствующий элементу <paramref name="baseChild"/> относительно <paramref name="baseParent"/>, или null, если <paramref name="baseParent"/> отсутствует в <paramref name="baseParent"/>.</returns>
        protected static TType? TryGetRelativeElement<TType>(
            OpenXmlElement baseParent,
            OpenXmlElement baseChild,
            OpenXmlElement newParent
        )
            where TType : OpenXmlElement
        {
            if (baseParent == baseChild)
            {
                return (TType) newParent;
            }

            var position = baseParent.Descendants<TType>().IndexOf(baseChild);
            return position >= 0 ? newParent.Descendants<TType>().ElementAtOrDefault(position) : null;
        }

        #endregion

        #region Base Overrides

        /// <doc path='info[@type="IPlaceholderDocument" and @item="FindAsync"]'/>
        protected override async ValueTask<IList<IPlaceholderText>?> FindCoreAsync(IPlaceholderFindingContext context)
        {
            IList<IPlaceholderText>? placeholders;
            var dbScope = context.TryGetDbScope();

            if (dbScope is not null)
            {
                placeholders = await this.GetPlaceholdersFromDatabaseAsync(context, dbScope);
                if (placeholders is { Count: > 0 })
                {
                    return placeholders;
                }
            }

            using (this.InitDocument())
            {
                placeholders = await this.GetPlaceholdersFromDocumentAsync(context);
                this.PrepareDocumentForSave();
                this.SaveDocument();
            }

            if (!context.ValidationResult.IsSuccessful())
            {
                return Array.Empty<IPlaceholderText>();
            }

            if (dbScope is not null)
            {
                await this.SavePlaceholdersInDatabaseAsync(dbScope, placeholders, context.CancellationToken);
            }

            return placeholders;
        }

        /// <doc path='info[@type="IPlaceholderDocument" and @item="ReplaceAsync"]'/>
        protected override async ValueTask ReplaceCoreAsync(IPlaceholderReplacementContext context)
        {
            bool hasChanges;

            using (this.InitDocument())
            {
                var hasPrepareChanges = await this.PrepareDocumentForReplaceAsync(context);
                if (!context.ValidationResult.IsSuccessful())
                {
                    return;
                }

                if (this.WithExtensions)
                {
                    await this.BeforeDocumentReplaceAsync(context);
                    if (!context.ValidationResult.IsSuccessful())
                    {
                        return;
                    }
                }

                var hasFieldChanges = await this.ReplaceFieldPlaceholdersAsync(context);
                if (!context.ValidationResult.IsSuccessful())
                {
                    return;
                }

                var hasTableChanges = await this.ReplaceTablePlaceholdersAsync(context);
                if (!context.ValidationResult.IsSuccessful())
                {
                    return;
                }

                hasChanges = hasPrepareChanges || hasFieldChanges || hasTableChanges;

                if (hasChanges)
                {
                    this.CleanAfterChanges();
                }

                this.PrepareDocumentForSave();

                if (this.WithExtensions)
                {
                    await this.AfterDocumentReplaceAsync(context);
                    if (!context.ValidationResult.IsSuccessful())
                    {
                        return;
                    }
                }

                this.SaveDocument();
            }

            if (hasChanges)
            {
                await this.OnChangedAsync(cancellationToken: context.CancellationToken);
            }
        }

        #endregion
    }
}
