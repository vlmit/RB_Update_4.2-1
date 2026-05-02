#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DocumentFormat.OpenXml;
using Tessa.Platform;
using Unity;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <inheritdoc cref="IWordDocumentParsingRuleResolver"/>
    public sealed class WordDocumentParsingRuleResolver : IWordDocumentParsingRuleResolver
    {
        #region Fields

        private readonly IUnityContainer unityContainer;

        private readonly ConcurrentDictionary<Type, ConcurrentBag<Type>> rulesByType = new();

        #endregion

        #region Constructors

        public WordDocumentParsingRuleResolver(IUnityContainer unityContainer)
        {
            this.unityContainer = NotNullOrThrow(unityContainer);
        }

        #endregion

        #region IWordDocumentParsingRuleResolver Implementation

        /// <inheritdoc/>
        public IWordDocumentParsingRuleResolver RegisterRule<TElement, TRule>()
            where TElement : OpenXmlElement
            where TRule : IWordDocumentParsingRule<TElement>
        {
            var rules = this.rulesByType.GetOrAdd(typeof(TElement), _ => new ConcurrentBag<Type>());
            rules.Add(typeof(TRule));

            return this;
        }

        /// <inheritdoc/>
        public IWordDocumentParsingRuleContainer ResolveAll()
        {
            return new WordDocumentParsingRuleContainer(
                this.rulesByType
                    .ToDictionary(
                        keyValue => keyValue.Key,
                        keyValue => (IReadOnlyList<IWordDocumentParsingRule>) keyValue.Value
                            .Select(x => (IWordDocumentParsingRule?) this.unityContainer.TryResolve(x))
                            .Where(x => x is not null)
                            .ToImmutableList()!));
        }

        #endregion
    }
}
