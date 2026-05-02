#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.ComponentModel;
using Tessa.Extensions.Default.Server.Workflow.KrProcess;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Views;

namespace Tessa.Extensions.Default.Server.Views
{
    /// <summary>
    /// Перехватчик для представления CardTasks.
    /// В случае наличия параметра CardIDParam проверяет в параметрах представления валидный токен по указанному идентификатору карточки.
    /// Если токен валиден - разрешает дальнейшую обработку. Если нет - возвращает пустой результат.
    /// Если параметр - CardIDParam не указан, тоже разрешает дальнейшую обработку, т.к. в этом случае представление ничего не вернёт.
    /// </summary>
    public sealed class CardTasksInterceptor(
        IKrTokenProvider krTokenProvider,
        IKrTypesCache typesCache,
        ICardRepository cardRepository,
        ICardMetadata cardMetadata,
        ICardGetStrategy getStrategy,
        IDbScope dbScope)
        : ViewInterceptorBase(["CardTasks"])
    {
        #region Private Fields

        private readonly IKrTokenProvider krTokenProvider = NotNullOrThrow(krTokenProvider);

        private readonly IKrTypesCache typesCache = NotNullOrThrow(typesCache);

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);

        private readonly ICardGetStrategy getStrategy = NotNullOrThrow(getStrategy);

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);

        #endregion

        #region Base Overrides

        public override async ValueTask<ITessaViewResult> GetDataAsync(ITessaViewRequest request, CancellationToken cancellationToken = default)
        {
            var view = this.GetInterceptedView(request.ViewAlias);

            await using (this.dbScope.Create())
            {
                if (request.Parameters.FindByName("CardIDParam")?.CriteriaValues.FirstOrDefault()?.Values.FirstOrDefault()?.Value is Guid cardID)
                {
                    var validationResult = new ValidationResultBuilder();

                    var getContext =
                        await this.getStrategy.TryLoadCardInstanceAsync(
                            cardID,
                            this.dbScope.Db,
                            this.cardMetadata,
                            validationResult,
                            cancellationToken: cancellationToken);

                    if (getContext is null
                        || !validationResult.IsSuccessful()
                        || !(await this.cardMetadata.GetCardTypesAsync(cancellationToken)).TryGetValue(getContext.CardTypeID, out var cardType)
                        || !cardType.Flags.Has(CardTypeFlags.AllowTasks))
                    {
                        // или карточки с таким идентификатором нет в базе, или метаинформация по этому типу повреждена,
                        // или тип не поддерживает задания - дальнейшие проверки не имеют смысла
                        return new TessaViewResult(await view.GetMetadataAsync(cancellationToken));
                    }

                    // карточки, входящие в типовое решение, должны подчиняться правилам доступа
                    if (await KrComponentsHelper.HasBaseAsync(cardType.ID, this.typesCache, cancellationToken))
                    {
                        var tokenString = request.Parameters.FindByName("Token")?.CriteriaValues.FirstOrDefault()?.Values.FirstOrDefault()?.Value as string;
                        if (!string.IsNullOrEmpty(tokenString))
                        {
                            try
                            {
                                var tokenSerialized = StorageHelper.DeserializeFromTypedJson(tokenString);
                                var token = new KrToken(tokenSerialized!);

                                if (token.IsValid()
                                    && token.CardID == cardID
                                    && token.CardVersion > 0
                                    && token.HasPermission(KrPermissionFlagDescriptors.ModifyAllTaskAssignedRoles)
                                    && token.ExpiryDate.ToUniversalTime() > DateTime.UtcNow)
                                {
                                    var tokenResult = new ValidationResultBuilder();
                                    int cardVersion = getContext.Card.Version;

                                    if (await this.krTokenProvider.ValidateTokenAsync(
                                            new Card { ID = cardID, Version = cardVersion },
                                            token, tokenResult, cancellationToken: cancellationToken) == KrTokenValidationResult.Success
                                        && tokenResult.IsSuccessful())
                                    {
                                        // токен валиден для актуальной версии карточки, в нём есть права на изменение ФРЗ;
                                        // поэтому возвращаем данные представления
                                        return await view.GetDataAsync(request, cancellationToken);
                                    }
                                }
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        // либо токен не был задан, либо был, но не прошёл проверку, например,
                        // если версия карточки изменилась с момента отправки токена;
                        // рассчитываем токен заново, выполняя загрузку карточки с расширениями;
                        // если карточка успешно загрузится, то права присутствуют
                        bool canSeeAllTasks = false;

                        try
                        {
                            var getRequest = new CardGetRequest
                            {
                                CardID = cardID,
                                CardTypeID = cardType.ID,
                                GetMode = CardGetMode.Edit,
                                RestrictionFlags = CardGetRestrictionFlags.RestrictFiles
                                    | CardGetRestrictionFlags.RestrictTaskCalendar
                                    | CardGetRestrictionFlags.RestrictTaskHistory,
                                Info = { [nameof(CardTasksInterceptor)] = BooleanBoxes.True }
                            };

                            CardGetResponse getResponse = await this.cardRepository.GetAsync(getRequest, cancellationToken);
                            if (getResponse.ValidationResult.IsSuccessful() &&
                                KrToken.TryGet(getResponse.Info) is { } token &&
                                token.IsValid() &&
                                token.HasPermission(KrPermissionFlagDescriptors.ModifyAllTaskAssignedRoles))
                            {
                                canSeeAllTasks = true;
                            }
                        }
                        catch
                        {
                            // ignored
                        }

                        return canSeeAllTasks
                            ? await view.GetDataAsync(request, cancellationToken)
                            : new TessaViewResult(await view.GetMetadataAsync(cancellationToken));
                    }
                    // карточка не связана с типовым решением
                }

                // не указан идентификатор карточки, значит View и так ничего не вернёт
                return await view.GetDataAsync(request, cancellationToken);
            }
        }

        #endregion
    }
}
