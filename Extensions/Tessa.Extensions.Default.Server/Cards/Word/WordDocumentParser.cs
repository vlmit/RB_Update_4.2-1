#nullable enable

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <inheritdoc cref="IWordDocumentParser"/>
    public sealed class WordDocumentParser : IWordDocumentParser
    {
        #region Fields

        private readonly IWordDocumentParsingRuleResolver rulesResolver;

        private IWordDocumentParsingRuleContainer? rulesContainer;

        #endregion

        #region Constructors

        public WordDocumentParser(IWordDocumentParsingRuleResolver rulesResolver)
        {
            this.rulesResolver = NotNullOrThrow(rulesResolver);
        }

        #endregion

        #region IWordDocumentParser Implementation

        /// <inheritdoc/>
        public IWordDocumentParsingResult ParseDocument(WordprocessingDocument wordDocument)
        {
            ThrowIfNull(wordDocument);
            ThrowIfNull(wordDocument.RootPart, $"{nameof(wordDocument)}.{nameof(wordDocument.RootPart)}");

            this.rulesContainer ??= this.rulesResolver.ResolveAll();
            var parsingContext = new WordDocumentParsingContext(wordDocument, this.rulesContainer);

            this.ParsePart(wordDocument.RootPart, parsingContext);

            return parsingContext.HasBlocks || parsingContext.HasPlaceholders
                ? this.PrepareResult(parsingContext)
                : WordDocumentParsingResult.Empty;
        }

        #endregion

        #region Private Methods

        private void ParsePart(OpenXmlPart part, WordDocumentParsingContext context)
        {
            if (part.RootElement is null)
            {
                return;
            }

            ParseRelationships(part.HyperlinkRelationships, context);
            this.ParseElement(part.RootElement, context);

            if (part.Parts is not null)
            {
                context.CurrentPosition.Add(-1);
                foreach (var childPart in part.Parts)
                {
                    this.ParsePart(childPart.OpenXmlPart, context);
                    context.CurrentPosition[^1]--;
                }
                context.CurrentPosition.RemoveAt(context.CurrentPosition.Count - 1);
            }
        }

        private static void ParseRelationships(
            IEnumerable<HyperlinkRelationship> relationships,
            WordDocumentParsingContext context)
        {
            foreach (var relationship in relationships)
            {
                string codeText = relationship.Uri.ToString();
                string decodedText = Uri.UnescapeDataString(codeText);

                foreach (Match match in PlaceholderHelper.UnescapedPlaceholdersRegex.Matches(decodedText))
                {
                    var text = match.Value;
                    var value = PlaceholderHelper.TryGetValue(text);
                    if (value is not null)
                    {
                        var newPlaceholder = new WordDocumentPlaceholderInfo
                        {
                            Text = text,
                            Value = value,
                            Position = context.GetCurrentPosition(),
                            Index = match.Index,
                            HyperlinkID = relationship.Id
                        };

                        context.AddPlaceholder(newPlaceholder);
                    }
                }
            }
        }

        private void ParseElement(OpenXmlElement element, WordDocumentParsingContext context)
        {
            var rules = context.RulesContainer.GetRulesForType(element.GetType());
            if (rules.Count > 0)
            {
                foreach (var rule in rules)
                {
                    if (rule.TryParseElement(element, context))
                    {
                        break;
                    }
                }
            }

            if (element.HasChildren)
            {
                context.CurrentPosition.Add(0);
                foreach (var child in element.ChildElements)
                {
                    this.ParseElement(child, context);
                    context.CurrentPosition[^1]++;
                }
                context.CurrentPosition.RemoveAt(context.CurrentPosition.Count - 1);
            }

            if (rules.Count > 0)
            {
                foreach (var rule in rules)
                {
                    if (rule.TryFinishParseElement(element, context))
                    {
                        break;
                    }
                }
            }
        }

        private IWordDocumentParsingResult PrepareResult(
            WordDocumentParsingContext context)
        {
            List<IWordDocumentBlock>? resultBlocks = null;

            if (context.HasBlocks)
            {
                resultBlocks = new();
                var orderedBlocks = context.OrderedBlocks;

                Stack<IWordDocumentBlock> allElements = new(orderedBlocks.Count - 1);
                Stack<IWordDocumentBlock> checkElements = new();
                checkElements.Push(orderedBlocks[0]);

                for (int i = orderedBlocks.Count - 1; i >= 1; i--)
                {
                    allElements.Push(orderedBlocks[i]);
                }

                // Элементы в allElements никогда не стоят перед элементами checkElements, т.к. изначальный parts - отсортирован.
                while (allElements.TryPop(out var checkBlock2))
                {
                    Stack<IWordDocumentBlock>? innerStack = null;
                    while (checkElements.TryPop(out var checkBlock1))
                    {
                        bool isRootCheck = checkElements.Count == 0;
                        // Если второй элемент после первого или относится к другой части документа, то можно сразу завершать обработку первого
                        if (!OpenXmlHelper.HasSamePart(checkBlock1.StartPosition, checkBlock2.StartPosition)
                            || OpenXmlHelper.Compare(checkBlock1.EndPosition, checkBlock2.StartPosition, 1, 1, checkBlock1.EndIndex, checkBlock2.StartIndex) <= 0)
                        {
                            if (isRootCheck)
                            {
                                checkElements.Push(checkBlock2);
                                resultBlocks.Add(checkBlock1);
                                break;
                            }
                        }
                        // Если второй внутри первого, то следующий элемент нужно сперва сравнить на вложенность второго, затем первого
                        else if (OpenXmlHelper.IsLessOrEquals(checkBlock1.StartPosition, checkBlock2.StartPosition, false, checkBlock1.StartIndex, checkBlock2.StartIndex)
                            && OpenXmlHelper.IsLessOrEquals(checkBlock2.EndPosition, checkBlock1.EndPosition, true, checkBlock2.EndIndex, checkBlock1.EndIndex))
                        {
                            checkBlock1.ChildBlocks.Add(checkBlock2);
                            checkElements.Push(checkBlock1);
                            checkElements.Push(checkBlock2);
                            if (innerStack is { Count: > 0 })
                            {
                                while (innerStack.TryPop(out var childBlock))
                                {
                                    checkElements.Push(childBlock);
                                }
                            }
                            break;
                        }
                        // Если первый внутри второго, то следующий элемент нужно сперва сравнить на вложенность первого, затем второго
                        else if (OpenXmlHelper.IsLessOrEquals(checkBlock2.StartPosition, checkBlock1.StartPosition, false, checkBlock2.StartIndex, checkBlock1.StartIndex)
                            && OpenXmlHelper.IsLessOrEquals(checkBlock1.EndPosition, checkBlock2.EndPosition, true, checkBlock1.EndIndex, checkBlock2.EndIndex))
                        {
                            checkBlock2.ChildBlocks.Add(checkBlock1);

                            // Если в стеке есть родительский элемент, то нужно также провести сравнение с ним, т.к. текущий элемент может содержать и его
                            if (checkElements.TryPeek(out var checkBlock1Parent))
                            {
                                checkBlock1Parent.ChildBlocks.Remove(checkBlock1);

                                innerStack ??= new();
                                innerStack.Push(checkBlock1);
                            }
                            else
                            {
                                checkElements.Push(checkBlock2);
                                checkElements.Push(checkBlock1);
                                break;
                            }
                        }
                        else
                        {
                            return new WordDocumentParsingResult
                            {
                                ValidationResult = ValidationResult.FromText(
                                    this,
                                    LocalizeFormat("$KrMessages_WordTemplate_BlockInterSectionError", checkBlock1.Name, checkBlock2.Name),
                                    ValidationResultType.Error)
                            };
                        }
                    }
                }

                if (checkElements.Count > 0)
                {
                    IWordDocumentBlock? rootItem = null;
                    while (checkElements.TryPop(out var possibleRootItem))
                    {
                        rootItem = possibleRootItem;
                    }

                    resultBlocks.Add(rootItem!);
                }

                if (context.HasPlaceholders)
                {
                    static bool PlacePlaceholderInBlockTree(
                        WordDocumentPlaceholderInfo placeholderInfo,
                        IEnumerable<IWordDocumentBlock> blocks)
                    {
                        foreach (var block in blocks)
                        {
                            if (block.Contains(placeholderInfo))
                            {
                                if (block.ChildBlocks.Count == 0
                                    || !PlacePlaceholderInBlockTree(placeholderInfo, block.ChildBlocks))
                                {
                                    block.Placeholders.Add(placeholderInfo.ID);
                                }

                                return true;
                            }
                        }

                        return false;
                    }

                    foreach (var placeholder in context.Placeholders)
                    {
                        PlacePlaceholderInBlockTree(placeholder, resultBlocks);
                    }
                }
            }
            else if (!context.HasPlaceholders)
            {
                return WordDocumentParsingResult.Empty;
            }

            return new WordDocumentParsingResult
            {
                DocumentBlocks = resultBlocks ?? (IReadOnlyList<IWordDocumentBlock>) Array.Empty<IWordDocumentBlock>(),
                Placeholders = context.HasPlaceholders ? context.Placeholders : Array.Empty<WordDocumentPlaceholderInfo>()
            };
        }

        #endregion
    }
}
