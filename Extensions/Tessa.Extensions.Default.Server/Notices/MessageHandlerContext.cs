#nullable enable
using System.Threading;
using Tessa.Cards;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;

namespace Tessa.Extensions.Default.Server.Notices
{
    public class MessageHandlerContext(
        IMessageInfo info,
        NoticeMessage message,
        ISession session,
        DbManager db,
        IQueryBuilderFactory builderFactory,
        Card? card = null,
        CardTask? task = null,
        CancellationToken cancellationToken = default)
        : IMessageHandlerContext
    {
        #region IMessageHandlerContext Members

        public IMessageInfo Info { get; } = NotNullOrThrow(info);

        public NoticeMessage Message { get; } = NotNullOrThrow(message);

        public ISession Session { get; } = NotNullOrThrow(session);

        public DbManager Db { get; } = NotNullOrThrow(db);

        public IQueryBuilderFactory BuilderFactory { get; } = NotNullOrThrow(builderFactory);

        public Card? Card { get; } = card;

        public CardTask? Task { get; } = task;

        public bool Cancel { get; set; } // = false

        /// <summary>
        /// Объект, посредством которого можно отменить асинхронную задачу.
        /// </summary>
        public CancellationToken CancellationToken { get; } = cancellationToken;

        #endregion
    }
}
