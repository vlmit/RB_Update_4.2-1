#nullable enable

using System;
using System.Collections.Generic;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <inheritdoc cref="IWordDocumentParsingResult"/>
    public sealed class WordDocumentParsingResult : IWordDocumentParsingResult
    {
        #region IWordDocumentParsingResult Implementation

        /// <inheritdoc/>
        public IReadOnlyList<WordDocumentPlaceholderInfo> Placeholders { get; init; } = Array.Empty<WordDocumentPlaceholderInfo>();

        /// <inheritdoc/>
        public IReadOnlyList<IWordDocumentBlock> DocumentBlocks { get; init; } = Array.Empty<IWordDocumentBlock>();

        /// <inheritdoc/>
        public ValidationResult ValidationResult { get; init; } = ValidationResult.Empty;

        #endregion

        #region Static

        /// <summary>
        /// Возвращает пустой результат парсинга.
        /// </summary>
        public static IWordDocumentParsingResult Empty { get; } = new WordDocumentParsingResult();

        #endregion
    }
}
