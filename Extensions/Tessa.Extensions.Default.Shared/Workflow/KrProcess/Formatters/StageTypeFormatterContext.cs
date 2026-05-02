#nullable enable

using System.Collections.Generic;
using System.Threading;
using Tessa.Cards;
using Tessa.Platform.Runtime;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess.Formatters
{
    /// <inheritdoc cref="IStageTypeFormatterContext"/>
    public class StageTypeFormatterContext :
        IStageTypeFormatterContext
    {
        /// <summary>
        /// Инициализирует новый экземпляр класса.
        /// </summary>
        /// <param name="session"><inheritdoc cref="Session" path="/summary"/></param>
        /// <param name="info"><inheritdoc cref="Info" path="/summary"/></param>
        /// <param name="card"><inheritdoc cref="Card" path="/summary"/></param>
        /// <param name="stageRow"><inheritdoc cref="StageRow" path="/summary"/></param>
        /// <param name="settings"><inheritdoc cref="Settings" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        public StageTypeFormatterContext(
            ISession session,
            Dictionary<string, object?> info,
            Card card,
            CardRow stageRow,
            IDictionary<string, object?>? settings,
            CancellationToken cancellationToken = default)
        {
            this.Session = NotNullOrThrow(session);
            this.Info = NotNullOrThrow(info);
            this.Card = NotNullOrThrow(card);
            this.StageRow = NotNullOrThrow(stageRow);
            this.Settings = settings;
            this.CancellationToken = cancellationToken;
        }

        /// <inheritdoc />
        public ISession Session { get; }

        /// <inheritdoc />
        public Dictionary<string, object?> Info { get; }

        /// <inheritdoc />
        public Card Card { get; }

        /// <inheritdoc />
        public CardRow StageRow { get; }

        /// <inheritdoc />
        public IDictionary<string, object?>? Settings { get; }

        /// <inheritdoc />
        public string DisplayTimeLimit { get; set; } = string.Empty;

        /// <inheritdoc />
        public string DisplayParticipants { get; set; } = string.Empty;

        /// <inheritdoc />
        public string DisplaySettings { get; set; } = string.Empty;

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; }
    }
}
