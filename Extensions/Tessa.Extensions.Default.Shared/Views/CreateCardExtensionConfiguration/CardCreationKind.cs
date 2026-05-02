namespace Tessa.Extensions.Default.Shared.Views
{
    /// <summary>
    /// Режим создания карточки.
    /// </summary>
    public enum CardCreationKind
    {
        /// <summary>
        /// По типу полученному из текущей строки
        /// </summary>
        ByTypeFromSelection,

        /// <summary>
        /// По алиасу типа
        /// </summary>
        ByTypeAlias,

        /// <summary>
        /// По идентификатору типа
        /// </summary>
        ByDocTypeIdentifier
    }
}
