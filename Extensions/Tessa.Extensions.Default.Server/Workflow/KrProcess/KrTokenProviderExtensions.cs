using System;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess
{
    /// <summary>
    /// Методы расширений для <see cref="IKrTokenProvider"/>.
    /// </summary>
    public static class KrTokenProviderExtensions
    {
        /// <summary>
        /// Метод для создания токена прав доступа, содержащего все права и рассчитанные расширенные настройки прав доступа. 
        /// </summary>
        /// <param name="krTokenProvider">Объект, обеспечивающий создание и валидацию токена безопасности для типового решения.</param>
        /// <param name="card">Карточка.</param>
        /// <returns>Токен безопасности, полученный для заданной информации по карточке.</returns>
        public static KrToken CreateFullToken(
            this IKrTokenProvider krTokenProvider,
            Card card)
        {
            ThrowIfNull(krTokenProvider);

            return krTokenProvider.CreateToken(
                card,
                modifyTokenAction: t =>
                {
                    t.ServerOnly = true;
                    t.FullAccess = true;

                    KrProcessSharedHelper.TryGetDocTypeID(card, out var docTypeID);

                    // Не сохраняем значение null, т.к. нельзя гарантировать, что карточка не имеет типа документа без запроса к базе данных.
                    if (docTypeID.HasValue)
                    {
                        t.SetDocTypeID(docTypeID);
                    }

                    t.SetCardTypeID(card.TypeID);
                });
        }

        /// <summary>
        /// Метод для создания токена прав доступа, содержащего все права и рассчитанные расширенные настройки прав доступа. 
        /// </summary>
        /// <param name="krTokenProvider">Объект, обеспечивающий создание и валидацию токена безопасности для типового решения.</param>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <returns>Токен безопасности, полученный для заданной информации по идентификатору карточки.</returns>
        public static KrToken CreateFullToken(
            this IKrTokenProvider krTokenProvider,
            Guid cardID)
        {
            ThrowIfNull(krTokenProvider);

            return krTokenProvider.CreateToken(
                cardID,
                modifyTokenAction: token =>
                { 
                    token.ServerOnly = true;
                    token.FullAccess = true;
                });
        }
    }
}
