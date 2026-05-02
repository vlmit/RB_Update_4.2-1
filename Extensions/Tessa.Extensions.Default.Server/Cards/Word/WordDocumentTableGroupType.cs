#nullable enable

namespace Tessa.Extensions.Default.Server.Cards.Word
{
    /// <summary>
    /// Тип группы табличного блока.
    /// </summary>
    public enum WordDocumentTableGroupType
    {
        /// <summary>
        /// Табличный блок обозначает область строки таблицы.
        /// </summary>
        Row,

        /// <summary>
        /// Табличный блок обозначает область группировки таблицы.
        /// </summary>
        Group,

        /// <summary>
        /// Табличный блок обозначает область всей таблицы.
        /// </summary>
        Table,
    }
}
