#nullable enable
using System;
using System.Threading.Tasks;
using LinqToDB;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;

namespace Tessa.Extensions.Default.Server.Cards
{
    public sealed class CreateOrAddPartnerCardStoreExtension : CardStoreExtension
    {
        #region Constants

        private const string NewPartnerIDKey = StorageHelper.SystemKeyPrefix + "NewPartnerID";

        #endregion

        #region Fields

        private readonly ICardRepository cardRepository;

        #endregion

        #region Constructors

        public CreateOrAddPartnerCardStoreExtension(ICardRepository cardRepository)
        {
            this.cardRepository = cardRepository;
        }

        #endregion

        #region Base Overrides

        public override async Task BeforeRequestWhenTypeResolved(ICardStoreExtensionContext context)
        {
            // Если нормально заданный контрагент или вообще не задан, то выходим
            if (context.Request.TryGetCard() is not { } card
                || card.TryGetSections()?.TryGet("DocumentCommonInfo") is not { } dci
                || dci.RawFields.TryGet<Guid?>("PartnerID") is not { } partnerID
                || partnerID != Guid.Empty
                || dci.RawFields.TryGet<string>("PartnerName") is not { Length: > 0 } partnerName)
            {
                return;
            }

            await using (context.DbScope!.Create())
            {
                var db = context.DbScope.Db;
                var command =
                    db.SetCommand(context.DbScope.BuilderFactory
                                .Select().Top(1).C(null, "ID", "Name")
                                .From("Partners").NoLock()
                                .Where().LowerC("Name").Equals().LowerP("Name")
                                .Limit(1)
                                .Build(),
                            db.Parameter("Name", partnerName, DataType.NVarChar))
                        .LogCommand();

                await using var reader = await command.ExecuteReaderAsync(context.CancellationToken);
                if (await reader.ReadAsync(context.CancellationToken))
                {
                    // Если нашёлся такой же, то подставляем и выходим
                    dci.Fields["PartnerID"] = reader.GetValue<Guid>(0);
                    dci.Fields["PartnerName"] = reader.GetValue<string>(1);
                    return;
                }
            }

            var newPartnerID = Guid.NewGuid();

            card.Info.Add(NewPartnerIDKey, newPartnerID);

            dci.Fields["PartnerID"] = newPartnerID;
            dci.Fields["PartnerName"] = partnerName;
        }

        public override async Task AfterBeginTransaction(ICardStoreExtensionContext context)
        {
            if (context.Request.TryGetCard() is not { } card
                || card.TryGetInfo()?.TryGet<Guid?>(NewPartnerIDKey) is not { } newPartnerID
                || card.TryGetSections()?.TryGet("DocumentCommonInfo") is not { } dci)
            {
                return;
            }

            var partnerNewRequest = new CardNewRequest { CardTypeID = DefaultCardTypes.PartnerTypeID };
            var info = partnerNewRequest.Info;

            if (card.StoreMode == CardStoreMode.Update)
            {
                info[CreatePartnerKeys.MainCardIDKey] = card.ID;
            }

            info[CreatePartnerKeys.MainCardTypeIDKey] = context.CardType!.ID;

            if (dci.RawFields.TryGet<Guid?>("DocTypeID") is { } docTypeID)
            {
                info[CreatePartnerKeys.MainCardDocTypeIDKey] = docTypeID;
            }

            var partnerNewResponse = await this.cardRepository.NewAsync(partnerNewRequest, context.CancellationToken);
            if (!partnerNewResponse.ValidationResult.IsSuccessful())
            {
                context.ValidationResult.Add(partnerNewResponse.ValidationResult);
                return;
            }

            var partnerCard = partnerNewResponse.Card;

            partnerCard.ID = newPartnerID;
            partnerCard.Sections["Partners"].Fields["Name"] = dci.RawFields["PartnerName"];

            var partnerStoreResponse = await this.cardRepository.StoreAsync(
                new CardStoreRequest { Card = partnerCard }, context.CancellationToken);

            if (!partnerStoreResponse.ValidationResult.IsSuccessful())
            {
                context.ValidationResult.Add(partnerNewResponse.ValidationResult);
            }
        }

        #endregion
    }
}
