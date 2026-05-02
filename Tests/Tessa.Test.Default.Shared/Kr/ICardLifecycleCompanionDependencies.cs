using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;

namespace Tessa.Test.Default.Shared.Kr
{
    /// <summary>
    /// Зависимости, используемые объектами, управляющими жизненным циклом карточек.
    /// </summary>
    public interface ICardLifecycleCompanionDependencies
    {
        /// <inheritdoc cref="ICardRepository" path="/summary"/>
        ICardRepository CardRepository { get; }

        /// <inheritdoc cref="ICardManager" path="/summary"/>
        ICardManager CardManager { get; }

        /// <inheritdoc cref="ICardMetadata" path="/summary"/>
        ICardMetadata CardMetadata { get; }

        /// <inheritdoc cref="IDbScope" path="/summary"/>
        IDbScope DbScope { get; }

        /// <inheritdoc cref="ICardFileManager" path="/summary"/>
        ICardFileManager CardFileManager { get; }

        /// <inheritdoc cref="ICardStreamServerRepository" path="/summary"/>
        ICardStreamServerRepository CardStreamServerRepository { get; }

        /// <inheritdoc cref="ICardStreamClientRepository" path="/summary"/>
        ICardStreamClientRepository CardStreamClientRepository { get; }

        /// <inheritdoc cref="ICardCache" path="/summary"/>
        ICardCache CardCache { get; }

        /// <summary>
        /// <inheritdoc cref="ICardLifecycleCompanionRequestExtender" path="/summary"/>
        /// Может иметь значение по умолчанию для типа.
        /// </summary>
        ICardLifecycleCompanionRequestExtender RequestExtender { get; }

        /// <inheritdoc cref="ISession" path="/summary"/>
        ISession Session { get; }

        /// <summary>
        /// Возвращает признак, показывающий, что используются серверные зависимости.
        /// </summary>
        bool ServerSide { get; }
    }
}
