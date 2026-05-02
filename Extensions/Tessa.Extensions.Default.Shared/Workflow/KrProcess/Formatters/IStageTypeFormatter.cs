#nullable enable

using System.Threading.Tasks;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess.Formatters
{
    /// <summary>
    /// Объект, выполняющий форматирование параметров этапа.
    /// </summary>
    public interface IStageTypeFormatter
    {
        /// <summary>
        /// Выполняет форматирование ячеек на клиенте.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeFormatterContext" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        /// <remarks>
        /// Выполняется при открытии карточки и при каждом закрытии редактора строки. В контексте доступны настройки этапа в виде виртуальных секций.
        /// </remarks>
        ValueTask FormatClientAsync(IStageTypeFormatterContext context);

        /// <summary>
        /// Выполняет форматирование ячеек на сервере.
        /// </summary>
        /// <param name="context"><inheritdoc cref="IStageTypeFormatterContext" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        /// <remarks>
        /// Выполняется при сохранении карточки. В контексте доступны настройки этапа в виде хранилища ключ-значение. Серверное форматирование может быть полезно для отображения этапов в представлениях и в легком клиенте.
        /// </remarks>
        ValueTask FormatServerAsync(IStageTypeFormatterContext context);
    }
}
