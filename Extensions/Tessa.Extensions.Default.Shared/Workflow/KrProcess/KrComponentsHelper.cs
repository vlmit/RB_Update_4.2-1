#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.Data;

namespace Tessa.Extensions.Default.Shared.Workflow.KrProcess
{
    /// <summary>
    /// Предоставляет методы для работы с компонентами типового решения.
    /// </summary>
    public static class KrComponentsHelper
    {
        #region Public Methods

        /// <summary>
        /// Определяет включён ли тип в типовое решение.
        /// </summary>
        /// <param name="cardTypeID">Идентификатор типа карточки.</param>
        /// <param name="typesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Значение true, если тип включён в типовое решение, иначе - false.</returns>
        public static async ValueTask<bool> HasBaseAsync(
            Guid cardTypeID,
            IKrTypesCache typesCache,
            CancellationToken cancellationToken = default)
        {
            var components = await GetKrComponentsAsync(cardTypeID, typesCache, cancellationToken);
            return components.Has(KrComponents.Base);
        }

        /// <summary>
        /// Возвращает включенные компоненты типового решения только для типа карточки без учета типа документа.
        /// </summary>
        /// <param name="cardTypeID">Идентификатор типа карточки.</param>
        /// <param name="typesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Включенные компоненты типового решения.</returns>
        public static async ValueTask<KrComponents> GetKrComponentsAsync(
            Guid cardTypeID,
            IKrTypesCache typesCache,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(typesCache);

            // Исключаем рекурсию
            if (cardTypeID == Guid.Empty
                || cardTypeID == DefaultCardTypes.KrSettingsTypeID)
            {
                return KrComponents.None;
            }

            var typesComponents = await typesCache.GetKrComponentsAsync(cancellationToken).ConfigureAwait(false);
            return typesComponents.GetValueOrDefault(cardTypeID, KrComponents.None);
        }

        /// <summary>
        /// Возвращает включенные компоненты типового решения только для типа карточки по известному типу карточки и документа.
        /// </summary>
        /// <param name="cardTypeID">Идентификатор типа карточки.</param>
        /// <param name="docTypeID">Идентификатор типа документа.</param>
        /// <param name="typesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Включенные компоненты типового решения.</returns>
        public static async ValueTask<KrComponents> GetKrComponentsAsync(
            Guid cardTypeID,
            Guid? docTypeID,
            IKrTypesCache typesCache,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(typesCache);

            var result = await GetKrComponentsAsync(
                cardTypeID,
                typesCache,
                cancellationToken);

            if (result.Has(KrComponents.DocTypes))
            {
                result = await GetDocTypeComponentsAsync(
                    docTypeID,
                    typesCache,
                    cancellationToken);
            }

            return result;
        }

        /// <summary>
        /// Возвращает включенные компоненты типового решения для указанной карточки.
        /// </summary>
        /// <param name="card">Карточка, для которой необходимо получить включённые компоненты типового решения.</param>
        /// <param name="typesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Включенные компоненты типового решения для указанной карточки.</returns>
        public static async ValueTask<KrComponents> GetKrComponentsAsync(
            Card card,
            IKrTypesCache typesCache,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(card);
            ThrowIfNull(typesCache);

            var result = await GetKrComponentsAsync(
                card.TypeID,
                typesCache,
                cancellationToken);

            if (result.Has(KrComponents.DocTypes))
            {
                KrProcessSharedHelper.TryGetDocTypeID(card, out var docTypeID);

                result = await GetDocTypeComponentsAsync(
                    docTypeID,
                    typesCache,
                    cancellationToken);
            }

            return result;
        }

        /// <summary>
        /// Возвращает включенные компоненты типового решения для карточки с учетом типа документа.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки.</param>
        /// <param name="typesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Включенные компоненты типового решения.</returns>
        public static async Task<KrComponents> GetKrComponentsAsync(
            Guid cardID,
            IKrTypesCache typesCache,
            IDbScope dbScope,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(typesCache);
            ThrowIfNull(dbScope);

            Guid? cardTypeID;

            await using (dbScope.Create())
            {
                var query = dbScope.BuilderFactory
                    .Select()
                    .C(KrConstants.DocumentCommonInfo.CardTypeID)
                    .From(KrConstants.DocumentCommonInfo.Name).NoLock()
                    .Where().C(KrConstants.DocumentCommonInfo.ID).Equals().P("CardID")
                    .Build();

                cardTypeID = await dbScope.Db.SetCommand(
                        query,
                        dbScope.Db.Parameter("CardID", cardID))
                    .LogCommand()
                    .ExecuteAsync<Guid?>(cancellationToken);
            }

            return cardTypeID.HasValue
                ? await GetKrComponentsAsync(
                    cardID,
                    cardTypeID.Value,
                    typesCache,
                    dbScope,
                    cancellationToken)
                : KrComponents.None;
        }

        /// <summary>
        /// Возвращает включенные компоненты типового решения для указанной карточки с учетом типа документа.
        /// Тип документа получается из базы данных.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки для которой требуется получить включённые компоненты типового решения.</param>
        /// <param name="cardTypeID">Идентификатор типа карточки.</param>
        /// <param name="typesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Включенные компоненты типового решения.</returns>
        public static async ValueTask<KrComponents> GetKrComponentsAsync(
            Guid cardID,
            Guid cardTypeID,
            IKrTypesCache typesCache,
            IDbScope dbScope,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(typesCache);
            ThrowIfNull(dbScope);

            var result = await GetKrComponentsAsync(
                cardTypeID,
                typesCache,
                cancellationToken);

            if (result.Has(KrComponents.DocTypes))
            {
                var docTypeID = await KrProcessSharedHelper.GetDocTypeIDAsync(
                    cardID,
                    dbScope,
                    cancellationToken);

                result = await GetDocTypeComponentsAsync(
                    docTypeID,
                    typesCache,
                    cancellationToken);
            }

            return result;
        }

        /// <summary>
        /// Проверяет наличие необходимых настроек у карточки типового решения.
        /// </summary>
        /// <param name="cardID">Идентификатор карточки, по которой будет определён идентификатор типа документа, если он не задан.</param>
        /// <param name="cardTypeID">Идентификатор типа карточки.</param>
        /// <param name="docTypeID">Идентификатор типа документа или значение <see langword="null"/>, если он должен быть определён автоматически.</param>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="typesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
        /// <param name="required">Проверяемые компоненты.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Кортеж содержащий: значение <see langword="true"/>, если все проверяемые компоненты активны, иначе - <see langword="false"/>; строка содержащая информацию об ошибке, возникшей при выполнении проверки.</returns>
        public static async ValueTask<(bool IsSuccessful, string ErrorMessage)> CheckKrComponentsAsync(
            Guid cardID,
            Guid cardTypeID,
            Guid? docTypeID,
            IDbScope dbScope,
            IKrTypesCache typesCache,
            KrComponents required,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(dbScope);
            ThrowIfNull(typesCache);

            var used = await GetKrComponentsAsync(
                cardTypeID,
                typesCache,
                cancellationToken);

            if (used.Has(KrComponents.DocTypes))
            {
                if (!docTypeID.HasValue)
                {
                    docTypeID = await KrProcessSharedHelper.GetDocTypeIDAsync(
                        cardID,
                        dbScope,
                        cancellationToken);

                    if (!docTypeID.HasValue)
                    {
                        var errorMessage = await LocalizeNameAsync("KrMessages_UnableToGetSpecifiedDocType");
                        return (false, errorMessage);
                    }
                }

                used = await GetDocTypeComponentsAsync(
                    docTypeID,
                    typesCache,
                    cancellationToken);
            }

            return await GetErrorDescriptionAsync(
                required,
                used,
                docTypeID.HasValue,
                cancellationToken);
        }

        /// <summary>
        /// Проверяет наличие необходимых настроек у карточки типового решения.
        /// </summary>
        /// <param name="card">Карточка, по которой будет определён идентификатор типа документа, если он не задан.</param>
        /// <param name="docTypeID">Идентификатор типа документа.</param>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="typesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
        /// <param name="required">Проверяемые компоненты.</param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Кортеж содержащий: значение <see langword="true"/>, если все проверяемые компоненты активны, иначе - <see langword="false"/>; строка содержащая информацию об ошибке, возникшей при выполнении проверки.</returns>
        public static async ValueTask<(bool IsSuccessful, string ErrorMessage)> CheckKrComponentsAsync(
            Card card,
            Guid? docTypeID,
            IDbScope dbScope,
            IKrTypesCache typesCache,
            KrComponents required,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(dbScope);
            ThrowIfNull(typesCache);

            var used = await GetKrComponentsAsync(
                card.TypeID,
                typesCache,
                cancellationToken);

            if (used.Has(KrComponents.DocTypes))
            {
                if (!docTypeID.HasValue)
                {
                    docTypeID = await KrProcessSharedHelper.GetDocTypeIDAsync(
                        card,
                        dbScope,
                        cancellationToken);

                    if (!docTypeID.HasValue)
                    {
                        var errorMessage = await LocalizeNameAsync("KrMessages_UnableToGetSpecifiedDocType");
                        return (false, errorMessage);
                    }
                }

                used = await GetDocTypeComponentsAsync(
                    docTypeID,
                    typesCache,
                    cancellationToken);
            }

            return await GetErrorDescriptionAsync(
                required,
                used,
                docTypeID.HasValue,
                cancellationToken);
        }

        /// <summary>
        /// Возвращает включенные компоненты типового решения для указанной карточки.
        /// </summary>
        /// <param name="card">Карточка, для которой необходимо получить включённые компоненты типового решения.</param>
        /// <param name="krTypesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
        /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
        /// <param name="cancellationToken">Объект, посредством которого можно отменить асинхронную задачу.</param>
        /// <returns>Включенные компоненты типового решения для указанной карточки.</returns>
        public static async ValueTask<KrComponents> GetKrComponentsAsync(
            Card card,
            IKrTypesCache krTypesCache,
            IDbScope dbScope,
            CancellationToken cancellationToken = default)
        {
            ThrowIfNull(card);
            ThrowIfNull(krTypesCache);

            var result = await GetKrComponentsAsync(
                card.TypeID,
                krTypesCache,
                cancellationToken);

            if (result.Has(KrComponents.DocTypes))
            {
                var docTypeID = await KrProcessSharedHelper.GetDocTypeIDAsync(
                    card,
                    dbScope,
                    cancellationToken);

                result = await GetDocTypeComponentsAsync(
                    docTypeID,
                    krTypesCache,
                    cancellationToken);
            }

            return result;
        }

        #endregion

        #region Private Methods

        private static async ValueTask<KrComponents> GetDocTypeComponentsAsync(
            Guid? docTypeID,
            IKrTypesCache krTypesCache,
            CancellationToken cancellationToken = default)
        {
            if (!docTypeID.HasValue
                || docTypeID == Guid.Empty)
            {
                return KrComponents.None;
            }

            var typesComponents = await krTypesCache.GetKrComponentsAsync(cancellationToken).ConfigureAwait(false);
            return typesComponents.GetValueOrDefault(docTypeID.Value, KrComponents.None);
        }

        private static async ValueTask<(bool IsSuccessful, string ErrorMessage)> GetErrorDescriptionAsync(
            KrComponents required,
            KrComponents used,
            bool useDocTypes,
            CancellationToken cancellationToken = default)
        {
            if (used.Has(required))
            {
                return (true, string.Empty);
            }

            var result = true;
            var lostComponent = string.Empty;

            if (required.Has(KrComponents.Base) && used.HasNot(KrComponents.Base))
            {
                lostComponent += await LocalizeNameAsync("KrMessages_StandardSolution");
                result = false;
            }

            if (required.Has(KrComponents.Routes) && used.HasNot(KrComponents.Routes))
            {
                lostComponent += await LocalizeNameAsync("KrMessages_Approving");
                result = false;
            }

            if (required.Has(KrComponents.Registration) && used.HasNot(KrComponents.Registration))
            {
                lostComponent += (string.IsNullOrEmpty(lostComponent) ? string.Empty : ", ")
                    + await LocalizeNameAsync("KrMessages_Registration");
                result = false;
            }

            if (required.Has(KrComponents.Resolutions) && used.HasNot(KrComponents.Resolutions))
            {
                lostComponent += (string.IsNullOrEmpty(lostComponent) ? string.Empty : ", ")
                    + await LocalizeNameAsync("KrMessages_Resolutions");
                result = false;
            }

            if (required.Has(KrComponents.UseForum) && used.HasNot(KrComponents.UseForum))
            {
                lostComponent += (string.IsNullOrEmpty(lostComponent) ? string.Empty : ", ")
                    + await LocalizeNameAsync("KrMessages_Forums");
                result = false;
            }

            var errorMessage = result
                ? string.Empty
                : string.Format(await LocalizeNameAsync("KrMessages_TypeDoesntUse"),
                    useDocTypes
                    ? await LocalizeNameAsync("KrMessages_DocType")
                    : await LocalizeNameAsync("KrMessages_CardType"), lostComponent);

            return (result, errorMessage);
        }

        #endregion
    }
}
