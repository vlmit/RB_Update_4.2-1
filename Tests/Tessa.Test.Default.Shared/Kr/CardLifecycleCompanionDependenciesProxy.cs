using System;
using Tessa.Cards;
using Tessa.Cards.Caching;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;

namespace Tessa.Test.Default.Shared.Kr
{
    /// <summary>
    /// Прокси для <see cref="ICardLifecycleCompanionDependencies"/>.
    /// </summary>
    /// <remarks>Объект позволяет использовать <see cref="ICardLifecycleCompanionDependencies"/>, соответствующий текущей области выполнения.</remarks>
    public class CardLifecycleCompanionDependenciesProxy :
        ICardLifecycleCompanionDependencies
    {
        #region Fields

        private readonly Func<ICardLifecycleCompanionDependencies> getDepsFunc;

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="CardLifecycleCompanionDependenciesProxy"/>.
        /// </summary>
        /// <param name="getDepsFunc">Функция, возвращающая объект <see cref="ICardLifecycleCompanionDependencies"/>, соответствующий текущей области выполнения.</param>
        public CardLifecycleCompanionDependenciesProxy(
            Func<ICardLifecycleCompanionDependencies> getDepsFunc)
        {
            this.getDepsFunc = NotNullOrThrow(getDepsFunc);
        }

        #endregion

        #region ICardLifecycleCompanionDependencies Members

        /// <inheritdoc/>
        public ICardRepository CardRepository => this.getDepsFunc().CardRepository;

        /// <inheritdoc/>
        public ICardManager CardManager => this.getDepsFunc().CardManager;

        /// <inheritdoc/>
        public ICardMetadata CardMetadata => this.getDepsFunc().CardMetadata;

        /// <inheritdoc/>
        public IDbScope DbScope => this.getDepsFunc().DbScope;

        /// <inheritdoc/>
        public ICardFileManager CardFileManager => this.getDepsFunc().CardFileManager;

        /// <inheritdoc/>
        public ICardStreamServerRepository CardStreamServerRepository => this.getDepsFunc().CardStreamServerRepository;

        /// <inheritdoc/>
        public ICardStreamClientRepository CardStreamClientRepository => this.getDepsFunc().CardStreamClientRepository;

        /// <inheritdoc/>
        public ICardCache CardCache => this.getDepsFunc().CardCache;

        /// <inheritdoc/>
        public ICardLifecycleCompanionRequestExtender RequestExtender => this.getDepsFunc().RequestExtender;

        /// <inheritdoc/>
        public ISession Session => this.getDepsFunc().Session;

        /// <inheritdoc/>
        public bool ServerSide => this.getDepsFunc().ServerSide;

        #endregion
    }
}
