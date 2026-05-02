#nullable enable
using System;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.AbTest;
using Tessa.Platform;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Roles;
using Tessa.Sequences;

namespace Tessa.Extensions.Default.Server.AbTest
{
    public sealed class AbTestDataRequestExtension(ICardRepository extendedRepository, ISequenceProvider sequenceProvider)
        : CardRequestExtension
    {
        #region Fields

        private readonly ICardRepository extendedRepository = NotNullOrThrow(extendedRepository);

        private readonly ISequenceProvider sequenceProvider = NotNullOrThrow(sequenceProvider);

        #endregion

        #region Base Overrides

        public override async Task AfterRequest(ICardRequestExtensionContext context)
        {
            if (!context.RequestIsSuccessful)
            {
                return;
            }

            if (!context.Session.User.IsAdministrator())
            {
                ValidationSequence
                    .Begin(context.ValidationResult)
                    .SetObjectName(this)
                    .Error(ValidationKeys.UserIsNotAdmin)
                    .End();

                return;
            }

            var info = context.Request.Info;

            // создаём сотрудников
            var userCount = info.TryGet<int?>("UserCount") ?? 0;
            if (userCount > 0)
            {
                var newRequest = new CardNewRequest { CardTypeID = RoleHelper.PersonalRoleTypeID };
                var newResponse = await this.extendedRepository.NewAsync(newRequest, context.CancellationToken);
                context.ValidationResult.Add(newResponse.ValidationResult);

                if (!context.ValidationResult.IsSuccessful())
                {
                    return;
                }

                var baseCard = newResponse.Card;

                var baseFields = baseCard.Sections["PersonalRoles"].RawFields;
                baseFields["LoginTypeID"] = Int32Boxes.Zero;
                baseFields["LoginTypeName"] = "$Enum_LoginTypes_Forbidden";

                for (var i = 0; i < userCount; i++)
                {
                    var number = await this.sequenceProvider.AcquireNumberAsync(AbSequenceNames.AbUsers, context.ValidationResult, context.CancellationToken);
                    if (!number.HasValue)
                    {
                        break;
                    }

                    var card = baseCard.Clone();
                    card.ID = Guid.NewGuid();

                    var name = string.Format(await LocalizeNameAsync("KrMessages_UserNameFormat"), number);

                    var fields = card.Sections["PersonalRoles"].RawFields;
                    fields["Name"] = name;
                    fields["FirstName"] = name;
                    fields["FullName"] = name;

                    card.RemoveAllButChanged(CardStoreMode.Insert);

                    var storeRequest = new CardStoreRequest { Card = card };
                    var storeResponse = await this.extendedRepository.StoreAsync(storeRequest, context.CancellationToken);
                    context.ValidationResult.Add(storeResponse.ValidationResult);

                    if (!storeResponse.ValidationResult.IsSuccessful())
                    {
                        break;
                    }
                }
            }

            // создаём контрагентов
            var partnerCount = info.TryGet<int?>("PartnerCount") ?? 0;
            if (partnerCount > 0)
            {
                var newRequest = new CardNewRequest { CardTypeID = DefaultCardTypes.PartnerTypeID };
                var newResponse = await this.extendedRepository.NewAsync(newRequest, context.CancellationToken);
                context.ValidationResult.Add(newResponse.ValidationResult);

                if (!context.ValidationResult.IsSuccessful())
                {
                    return;
                }

                var baseCard = newResponse.Card;
                baseCard.Sections["Partners"].RawFields["Phone"] = "+71234323232";

                for (var i = 0; i < partnerCount; i++)
                {
                    var number = await this.sequenceProvider.AcquireNumberAsync(AbSequenceNames.AbPartners, context.ValidationResult, context.CancellationToken);
                    if (!number.HasValue)
                    {
                        break;
                    }

                    var card = baseCard.Clone();
                    card.ID = Guid.NewGuid();

                    var name = string.Format(await LocalizeNameAsync("KrMessages_PartnerNameFormat"), number);

                    var fields = card.Sections["Partners"].RawFields;
                    fields["Name"] = name;
                    fields["FullName"] = name;

                    card.RemoveAllButChanged(CardStoreMode.Insert);

                    var storeRequest = new CardStoreRequest { Card = card };
                    var storeResponse = await this.extendedRepository.StoreAsync(storeRequest, context.CancellationToken);
                    context.ValidationResult.Add(storeResponse.ValidationResult);

                    if (!storeResponse.ValidationResult.IsSuccessful())
                    {
                        break;
                    }
                }
            }

            // добавляем сообщение с результатами
            var resultText = StringBuilderHelper.Acquire();
            if (userCount > 0)
            {
                resultText
                    .AppendFormat(await LocalizeNameAsync("KrMessages_UsersGenerated"), userCount);
            }

            if (partnerCount > 0)
            {
                if (userCount > 0)
                {
                    resultText
                        .AppendLine();
                }

                resultText
                    .AppendFormat(await LocalizeNameAsync("KrMessages_PartnersGenerated"), partnerCount);
            }

            if (resultText.Length > 0)
            {
                context.ValidationResult.AddInfo(this, resultText.ToString());
            }

            resultText.Release();
        }

        #endregion
    }
}
