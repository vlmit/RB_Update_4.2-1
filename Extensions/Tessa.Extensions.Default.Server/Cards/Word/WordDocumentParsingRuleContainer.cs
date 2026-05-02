#nullable enable

using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <inheritdoc cref="IWordDocumentParsingRuleContainer"/>
    public sealed class WordDocumentParsingRuleContainer : IWordDocumentParsingRuleContainer
    {
        #region Fields

        private readonly Dictionary<Type, IReadOnlyList<IWordDocumentParsingRule>> rulesByType;

        #endregion

        #region Constructors

        public WordDocumentParsingRuleContainer(Dictionary<Type, IReadOnlyList<IWordDocumentParsingRule>> rulesByType)
        {
            this.rulesByType = NotNullOrThrow(rulesByType);
        }

        #endregion

        #region IWordDocumentParsingRuleContainer Implementation

        /// <inheritdoc/>
        public IReadOnlyList<IWordDocumentParsingRule<TElement>> GetRulesForType<TElement>()
             where TElement : OpenXmlElement
        {
            return this.rulesByType.TryGetValue(typeof(TElement), out var rules)
                ? (IReadOnlyList<IWordDocumentParsingRule<TElement>>) rules
                : Array.Empty<IWordDocumentParsingRule<TElement>>();
        }

        /// <inheritdoc/>
        public IReadOnlyList<IWordDocumentParsingRule> GetRulesForType(Type type)
        {
            return this.rulesByType.TryGetValue(type, out var rules)
                ? rules
                : Array.Empty<IWordDocumentParsingRule>();
        }

        #endregion
    }
}
