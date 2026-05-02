#nullable enable

using System.Collections.Generic;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Результат парсинга документа Word.
    /// </summary>
    public interface IWordDocumentParsingResult
    {
        /// <summary>
        /// Список плейсхолдеров, найденных в документе.
        /// </summary>
        IReadOnlyList<WordDocumentPlaceholderInfo> Placeholders { get; }

        /// <summary>
        /// Список блоков документа.
        /// </summary>
        IReadOnlyList<IWordDocumentBlock> DocumentBlocks { get; }

        /// <summary>
        /// Результат валидации парсинга документа.
        /// </summary>
        ValidationResult ValidationResult { get; }
    }
}