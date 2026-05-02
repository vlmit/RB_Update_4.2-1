#nullable enable

using System.Collections.Generic;
using DocumentFormat.OpenXml.Packaging;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Контекст парсинга документа Word в <see cref="IWordDocumentParser"/>.
    /// </summary>
    public interface IWordDocumentParsingContext
    {
        /// <summary>
        /// Документ Word.
        /// </summary>
        WordprocessingDocument WordDocument { get; }

        /// <summary>
        /// Контейнер с правилами парсинга.
        /// </summary>
        IWordDocumentParsingRuleContainer RulesContainer { get; }

        /// <summary>
        /// Определяет, были ли добавлены блоки в контекст обработки.
        /// </summary>
        bool HasBlocks { get; }

        /// <summary>
        /// Отсортированные в порядке добавления блоки.
        /// </summary>
        IReadOnlyList<IWordDocumentBlock> OrderedBlocks { get; }

        /// <summary>
        /// Определяет, были ли добавлены плейсхолдеры в контекст обработки.
        /// </summary>
        bool HasPlaceholders { get; }

        /// <summary>
        /// Плейсхолдеры.
        /// </summary>
        IReadOnlyList<WordDocumentPlaceholderInfo> Placeholders { get; }

        /// <summary>
        /// Текущая позиция.
        /// </summary>
        IReadOnlyList<int> CurrentPosition { get; }

        /// <summary>
        /// Дополнительная информация.
        /// </summary>
        Dictionary<string, object?> Info { get; }

        /// <summary>
        /// Добавляет новый блок в контекст парсинга.
        /// </summary>
        /// <param name="block"><inheritdoc cref="IWordDocumentBlock" path="/summary"/></param>
        void AddBlock(IWordDocumentBlock block);

        /// <summary>
        /// Возвращает уже добавленный блок определённого типа по его идентификатору.
        /// </summary>
        /// <param name="blockID">Идентификатор блока.</param>
        /// <returns>Блок документа Word или <c>null</c>, если блок с заданным идентификатором отсутствует в контексте.</returns>
        T? TryGetBlock<T>(string blockID) where T : IWordDocumentBlock;

        /// <summary>
        /// Добавляет плейсхолдер в контекст парсинга. Устанавливает идентификатор плейсхолдера.
        /// </summary>
        /// <param name="placeholder">Плейсхолдер.</param>
        void AddPlaceholder(WordDocumentPlaceholderInfo placeholder);

        /// <summary>
        /// Возвращает копию текущей позиции в документе Word.
        /// </summary>
        /// <returns>Копия текущей позиции  в документе Word.</returns>
        List<int> GetCurrentPosition();
    }
}
