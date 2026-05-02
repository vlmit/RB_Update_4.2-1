#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared.Settings;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform.Settings;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;

namespace Tessa.Extensions.Default.Server.Workflow.KrProcess
{
    /// <summary>
    /// Расширение для инициализации карточки при создании на основании другой карточки.
    /// </summary>
    public sealed class KrCreateBasedOnNewExtension : CardNewExtension
    {
        #region Fields

        private readonly ICardRepository extendedRepository;
        private readonly IKrCreateBasedOnHandler createBasedOnHandler;
        private readonly KrSettingsLazy krSettingsLazy;

        #endregion

        #region Constructors

        public KrCreateBasedOnNewExtension(
            ICardRepository extendedRepository,
            IKrCreateBasedOnHandler createBasedOnHandler,
            KrSettingsLazy krSettingsLazy)
        {
            this.extendedRepository = NotNullOrThrow(extendedRepository);
            this.createBasedOnHandler = NotNullOrThrow(createBasedOnHandler);
            this.krSettingsLazy = NotNullOrThrow(krSettingsLazy);
        }

        #endregion

        #region Base Overrides

        public override async Task AfterRequest(ICardNewExtensionContext context)
        {
            Card? newCard;
            Dictionary<string, object?>? info;

            if (context.CardType is null
                || context.CardType.InstanceType != CardInstanceType.Card
                || context.CardType.Flags.Has(CardTypeFlags.Singleton)
                || !context.RequestIsSuccessful
                || (info = context.Request.TryGetInfo()) is null
                || !context.ValidationResult.IsSuccessful()
                || (newCard = context.Response?.TryGetCard()) is null)
            {
                return;
            }

            Card baseCard;
            if (info.TryGet<object>(KrCreateBasedOnHelper.CardKey) is Dictionary<string, object> baseCardStorage)
            {
                baseCard = new Card(baseCardStorage!);
            }
            else
            {
                var baseCardID = info.TryGet<Guid?>(KrCreateBasedOnHelper.CardIDKey);
                if (!baseCardID.HasValue)
                {
                    return;
                }

                var getRequest = new CardGetRequest
                {
                    CardID = baseCardID,
                    GetMode = CardGetMode.ReadOnly,
                    Method = CardGetMethod.Export,
                    RestrictionFlags =
                        CardGetRestrictionFlags.RestrictTaskCalendar
                        | CardGetRestrictionFlags.RestrictTaskHistory,
                };

                var baseTokenStorage = info.TryGet<Dictionary<string, object?>?>(KrCreateBasedOnHelper.TokenKey);
                KrToken? baseToken = 
                    baseTokenStorage is not null 
                        ? KrToken.TryGet(baseTokenStorage) 
                        : null;

                if (baseToken is not null)
                {
                    baseToken.Set(getRequest.Info);
                }

                CardGetResponse getResponse = await this.extendedRepository.GetAsync(getRequest, context.CancellationToken);
                context.ValidationResult.Add(getResponse.ValidationResult);

                if (!getResponse.ValidationResult.IsSuccessful())
                {
                    return;
                }

                baseCard = getResponse.Card;
            }

            // Проверяем то, что карточка, на основании которой происходит создание, не административная.
            if (!(await context.CardMetadata.GetCardTypesAsync(context.CancellationToken)).TryGetValue(baseCard.TypeID, out var baseCardMetadata)
                || baseCardMetadata.Flags.Has(CardTypeFlags.Administrative))
            {
                return;
            }

            // Проверяем то, что тип карточки, на основании которой происходит создание включен в список допустимых к созданию на основании.
            // Проверку делаем отдельно, чтобы лезть в настройки только если карточка не была отсеяна выше, как административная.
            var krSettings = await this.krSettingsLazy.GetValueAsync(context.CancellationToken);
            if (krSettings is null || !krSettings.CreateBasedOnTypes.Contains(baseCard.TypeID))
            {
                return;
            }

            context.Info[KrCreateBasedOnHelper.CardInfoKey] = baseCard;

            ValidationResult infoResult = await createBasedOnHandler.CopyInfoAsync(baseCard, newCard, context.CancellationToken);
            context.ValidationResult.Add(infoResult);

            if (!infoResult.IsSuccessful)
            {
                return;
            }

            bool copyFiles = info.TryGet<bool>(KrCreateBasedOnHelper.CopyFilesKey);
            if (copyFiles)
            {
                // файлы копируются как псевдосоздание по шаблону, и у карточки newCard к этому моменту
                // должна быть корректная структура, в т.ч. должен быть задан непустой идентификатор карточки,
                // причём сам идентификатор при копировании не используется, но он влияет на валидацию структуры

                // поэтому если идентификатор пустой, то мы его создаём и по завершении копирования сбрасываем

                ValidationResult result = await createBasedOnHandler.CopyFilesAsync(baseCard, newCard, cancellationToken: context.CancellationToken);
                context.ValidationResult.Add(result);
            }
        }

        #endregion
    }
}
