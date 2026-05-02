#nullable enable

using System.Collections.Generic;
using DocumentFormat.OpenXml;
using Tessa.Platform;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Блок с таблицей в документе Word.
    /// </summary>
    public sealed class WordDocumentTableGroupBlock : WordDocumentBlockBase
    {
        #region Fields

        private List<OpenXmlElement>? baseElements;
        private List<IPlaceholder>? tablePlaceholders;
        private List<int>? tableStartPosition;
        private List<int>? tableEndPosition;

        #endregion

        #region Properties

        /// <summary>
        /// Тип группы табличного блока.
        /// </summary>
        public WordDocumentTableGroupType GroupType { get; set; }

        /// <summary>
        /// Определяет, что весь блок находится внутри параграфа.
        /// </summary>
        public bool InParagraph { get; set; }

        /// <summary>
        /// Определяет, является ли данный табличный блок опциональным.
        /// Если да, то данный блок игнорируется, если в нём есть хотя бы один другой блок, если он пересекается с другим блоком или если в нём нет ни одного табличного плейсхолдера.
        /// </summary>
        public bool IsOptional { get; set; }

        /// <summary>
        /// Позиция начала элемента таблицы.
        /// </summary>
        public List<int> TableStartPosition
        {
            get => this.tableStartPosition ??= new();
            set => this.tableStartPosition = NotNullOrThrow(value, nameof(this.TableStartPosition));
        }

        /// <summary>
        /// Позиция окончания элемента таблицы.
        /// </summary>
        public List<int> TableEndPosition
        {
            get => this.tableEndPosition ??= new();
            set => this.tableEndPosition = NotNullOrThrow(value, nameof(this.TableEndPosition));
        }

        /// <summary>
        /// Родительский элемент по отношению ко всем <see cref="BaseElements"/>
        /// </summary>
        public OpenXmlElement? TableElement { get; set; }

        /// <summary>
        /// Элементы, который копируются для каждой строки и в которых производится замена плейсхолдеров.
        /// </summary>
        public List<OpenXmlElement> BaseElements => this.baseElements ??= new List<OpenXmlElement>();

        /// <summary>
        /// Табличные плейсхолдеры данной группы.
        /// </summary>
        public List<IPlaceholder> TablePlaceholders => this.tablePlaceholders ??= new List<IPlaceholder>();

        #endregion

        #region Base Overrides

        protected override void SerializeCore(Dictionary<string, object?> storage)
        {
            base.SerializeCore(storage);

            storage[nameof(this.InParagraph)] = BooleanBoxes.Box(this.InParagraph);
            storage[nameof(this.IsOptional)] = BooleanBoxes.Box(this.IsOptional);
            storage[nameof(this.GroupType)] = Int32Boxes.Box((int) this.GroupType);
            storage[nameof(this.TableStartPosition)] = this.TableStartPosition;
            storage[nameof(this.TableEndPosition)] = this.TableEndPosition;
        }

        protected override void DeserializeCore(Dictionary<string, object?> storage)
        {
            base.DeserializeCore(storage);

            this.InParagraph = storage.TryGet<bool>(nameof(this.InParagraph));
            this.IsOptional = storage.TryGet<bool>(nameof(this.IsOptional));
            this.GroupType = (WordDocumentTableGroupType) storage.TryGet<int>(nameof(this.GroupType));
            this.tableStartPosition = storage.TryGet<List<int>>(nameof(this.TableStartPosition));
            this.tableEndPosition = storage.TryGet<List<int>>(nameof(this.TableEndPosition));
        }

        #endregion
    }
}
