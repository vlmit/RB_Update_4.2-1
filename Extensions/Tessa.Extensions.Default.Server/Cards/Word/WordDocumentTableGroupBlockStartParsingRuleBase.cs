#nullable enable

using DocumentFormat.OpenXml;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Базовое правило парсинга начала блока с таблицей.
    /// </summary>
    public abstract class WordDocumentTableGroupBlockStartParsingRuleBase<T> : WordDocumentParsingRuleBase<T>
        where T : OpenXmlElement
    {
        #region Base Overrides

        /// <inheritdoc/>
        protected override bool TryParseElementCore(T element, IWordDocumentParsingContext context)
        {
            var blockID = this.TryGetID(element, context);
            if (string.IsNullOrEmpty(blockID))
            {
                return false;
            }

            var blockName = this.TryGetName(element, context);
            if (string.IsNullOrEmpty(blockName))
            {
                return false;
            }

            var type = this.TryGetType(blockName);
            if (type is null)
            {
                return false;
            }

            context.AddBlock(new WordDocumentTableGroupBlock()
            {
                GroupType = type.Value,
                StartIndex = GetIndex(element),
                StartPosition = context.GetCurrentPosition(),
                ID = blockID,
                Name = blockName
            });

            return true;
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Возвращает идентификатор блока из элемента.
        /// </summary>
        /// <param name="element">Элемент.</param>
        /// <param name="context">Контекст парсинга документа Word.</param>
        /// <returns>Идентификатор блока или <c>null</c>, если его не удалось определить.</returns>
        protected abstract string? TryGetID(T element, IWordDocumentParsingContext context);

        /// <summary>
        /// Возвращает имя блока из элемента.
        /// </summary>
        /// <param name="element">Элемент.</param>
        /// <param name="context">Контекст парсинга документа Word.</param>
        /// <returns>Имя блока или <c>null</c>, если его не удалось определить.</returns>
        protected abstract string? TryGetName(T element, IWordDocumentParsingContext context);

        /// <summary>
        /// Возвращает тип группы блока по его имени.
        /// </summary>
        /// <param name="blockName">Имя блока.</param>
        /// <returns>Тип группы или <c>null</c>, если не элемент не является началом блока.</returns>
        protected abstract WordDocumentTableGroupType? TryGetType(string blockName);

        #endregion

        #region Protected Methods

        /// <summary>
        /// Возвращает тип группы по префиксу из двух символов.
        /// </summary>
        /// <param name="prefix">Префикс.</param>
        /// <returns>Тип группы или <c>null</c>, если не удалось определить тип блока по префиксу.</returns>
        protected WordDocumentTableGroupType? GetTypeFromNamePrefix(string prefix)
        {
            if (prefix is not { Length: 2 })
            {
                return null;
            }

            return prefix switch
            {
                "r_" => WordDocumentTableGroupType.Row,
                "g_" => WordDocumentTableGroupType.Group,
                "t_" => WordDocumentTableGroupType.Table,
                _ => null,
            };
        }

        #endregion
    }
}
