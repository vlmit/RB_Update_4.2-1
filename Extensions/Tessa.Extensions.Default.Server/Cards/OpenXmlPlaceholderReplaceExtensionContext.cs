#nullable enable

using System.Threading;
using Tessa.Platform.Placeholders;
using Tessa.Platform.Placeholders.Extensions;

namespace Tessa.Extensions.Default.Server.Cards
{
    /// <summary>
    /// Базовый класс контекста обработки расширений <see cref="IPlaceholderReplaceExtension"/> в документах OpenXML.
    /// </summary>
    /// <typeparam name="TableData">Тип данных с дополнительными данными контекста обработки таблицы.</typeparam>
    /// <typeparam name="RowData">Тип данных с дополнительными данными контекста обработки строки.</typeparam>
    /// <typeparam name="PlaceholderData">Тип данных с дополнительными данными контекста обработки плейсхолдера.</typeparam>
    public abstract class OpenXmlPlaceholderReplaceExtensionContext<TableData, RowData, PlaceholderData> : PlaceholderReplaceExtensionContext<TableData, RowData, PlaceholderData>
    {
        #region Constructors

        protected OpenXmlPlaceholderReplaceExtensionContext(
            IPlaceholderReplacementContext replacementContext,
            CancellationToken cancellationToken = default)
            : base(replacementContext, cancellationToken)
        {
        }

        #endregion
    }
}
