#nullable enable

using System.Collections.Generic;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Блок документа Word.
    /// </summary>
    public interface IWordDocumentBlock : ITypedSerializable
    {
        /// <summary>
        /// Идентификатор блока.
        /// </summary>
        string ID { get; }

        /// <summary>
        /// Наименование блока.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Индекс начала блока в тексте его родительского элемента.
        /// </summary>
        int StartIndex { get; set; }

        /// <summary>
        /// Индекс конца блока в тексте его родительского элемента.
        /// </summary>
        public int EndIndex { get; set; }

        /// <summary>
        /// Позиция начала блока закладки в структуре документа.
        /// </summary>
        public List<int> StartPosition { get; set; }

        /// <summary>
        /// Позиция конца блока закладки в структуре документа.
        /// </summary>
        public List<int> EndPosition { get; set; }

        /// <summary>
        /// Список дочерних блоков документа Word.
        /// </summary>
        List<IWordDocumentBlock> ChildBlocks { get; }

        /// <summary>
        /// Список идентификаторов плейсхолдеров, располагающихся в текущем блоке.
        /// </summary>
        List<int> Placeholders { get; }

        /// <summary>
        /// Определяет, располагается ли плейсхолдер в текущем блоке.
        /// </summary>
        /// <param name="placeholderInfo">Плейсхолдер.</param>
        /// <returns>Значение <c>true</c>, если плейсхолдер располагается в текущем блоке, иначе <c>false</c>.</returns>
        bool Contains(WordDocumentPlaceholderInfo placeholderInfo);
    }
}