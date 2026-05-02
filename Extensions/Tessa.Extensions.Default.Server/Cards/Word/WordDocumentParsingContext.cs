#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using Tessa.Platform.Collections;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <inheritdoc cref="IWordDocumentParsingContext"/>
    public sealed class WordDocumentParsingContext : IWordDocumentParsingContext
    {
        #region Fields

        private IReadOnlyList<IWordDocumentBlock>? orderedBlocksReadonly;

        private List<IWordDocumentBlock>? orderedBlocks;

        private IReadOnlyList<WordDocumentPlaceholderInfo>? placeholdersReadonly;

        private List<WordDocumentPlaceholderInfo>? placeholders;

        private ReadOnlyCollection<int>? currentPositionReadOnly;

        private Dictionary<string, object?>? info;

        private int nextPlaceholderID = 0;

        #endregion

        #region Constructors

        public WordDocumentParsingContext(
            WordprocessingDocument wordDocument,
            IWordDocumentParsingRuleContainer rulesContainer)
        {
            this.WordDocument = NotNullOrThrow(wordDocument);
            this.RulesContainer = NotNullOrThrow(rulesContainer);
        }

        #endregion

        #region IWordDocumentParsingContext Implementation

        /// <inheritdoc/>
        public WordprocessingDocument WordDocument { get; }

        /// <inheritdoc/>
        public IWordDocumentParsingRuleContainer RulesContainer { get; set; }

        /// <inheritdoc/>
        public bool HasBlocks => this.orderedBlocks is { Count: > 0 };

        /// <inheritdoc/>
        public IReadOnlyList<IWordDocumentBlock> OrderedBlocks =>
            this.orderedBlocksReadonly ??= new ReadOnlyCollection<IWordDocumentBlock>(this.orderedBlocks ??= new());

        /// <inheritdoc/>
        public bool HasPlaceholders => this.placeholders is { Count: > 0 };

        /// <inheritdoc/>
        public IReadOnlyList<WordDocumentPlaceholderInfo> Placeholders =>
            this.placeholdersReadonly ??= new ReadOnlyCollection<WordDocumentPlaceholderInfo>(this.placeholders ??= new());

        /// <inheritdoc/>
        IReadOnlyList<int> IWordDocumentParsingContext.CurrentPosition => this.currentPositionReadOnly ??= this.CurrentPosition.AsReadOnly();

        /// <inheritdoc/>
        public Dictionary<string, object?> Info => this.info ??= new();

        /// <inheritdoc/>
        public void AddBlock(IWordDocumentBlock block)
        {
            this.orderedBlocks ??= new();
            int insertIndex = this.orderedBlocks.Count;
            if (this.orderedBlocks.Count > 0)
            {
                for (; insertIndex > 0; insertIndex--)
                {
                    var checkBlock = this.orderedBlocks[insertIndex - 1];

                    if (OpenXmlHelper.HasSamePart(checkBlock.StartPosition, block.StartPosition)
                        && OpenXmlHelper.IsLessOrEquals(checkBlock.StartPosition, block.StartPosition, false, checkBlock.StartIndex, block.StartIndex))
                    {
                        break;
                    }
                }
            }

            if (insertIndex == this.orderedBlocks.Count)
            {
                this.orderedBlocks.Add(block);
            }
            else
            {
                this.orderedBlocks.Insert(insertIndex, block);
            }
        }

        /// <inheritdoc/>
        public void AddPlaceholder(WordDocumentPlaceholderInfo placeholder)
        {
            placeholder.ID = nextPlaceholderID++;
            this.placeholders ??= new();
            this.placeholders.Add(placeholder);
        }

        /// <inheritdoc/>
        public List<int> GetCurrentPosition()
        {
            return this.CurrentPosition.ToList();
        }

        /// <inheritdoc/>
        public T? TryGetBlock<T>(string blockID) where T : IWordDocumentBlock
        {
            if (this.orderedBlocks?.TryFirst(x => x.ID == blockID && x is T, out var block) == true)
            {
                return (T) block;
            }

            return default;
        }

        #endregion

        #region Properties

        /// <inheritdoc cref="IWordDocumentParsingContext.CurrentPosition"/>
        public List<int> CurrentPosition { get; } = new List<int>();

        #endregion
    }
}
