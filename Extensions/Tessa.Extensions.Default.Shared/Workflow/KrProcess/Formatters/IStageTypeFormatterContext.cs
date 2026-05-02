#nullable enable

using System.Collections.Generic;
using System.Threading;
using Tessa.Cards;
using Tessa.Platform.Runtime;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess.Formatters
{
    /// <summary>
    /// Контекст <see cref="IStageTypeFormatter"/>.
    /// </summary>
    public interface IStageTypeFormatterContext
    {
        /// <inheritdoc cref="ISession" path="/summary"/>
        ISession Session { get; }

        /// <summary>
        /// Дополнительная информацию.
        /// </summary>
        Dictionary<string, object?> Info { get; }

        /// <summary>
        /// Карточка, содержащая этап.
        /// </summary>
        Card Card { get; }

        /// <summary>
        /// Строка, содержащая этап.
        /// </summary>
        CardRow StageRow { get; }

        /// <summary>
        /// Словарь, содержащий настройки этапа.
        /// </summary>
        /// <remarks>Значение не задано при форматировании параметров этапа на клиенте.</remarks>
        IDictionary<string, object?>? Settings { get; }

        /// <summary>
        /// Отображаемый срок исполнения.
        /// </summary>
        string DisplayTimeLimit { get; set; }

        /// <summary>
        /// Отображаемый список участников.
        /// </summary>
        string DisplayParticipants { get; set; }

        /// <summary>
        /// Отображаемые настройки.
        /// </summary>
        string DisplaySettings { get; set; }

        /// <inheritdoc cref="System.Threading.CancellationToken" path="/summary"/>
        CancellationToken CancellationToken { get; }
    }
}
