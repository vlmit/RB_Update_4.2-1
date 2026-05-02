using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Cards.Extensions;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Roles;
using static LinqToDB.Internal.Reflection.Methods.LinqToDB;

namespace Tessa.Extensions.Server.Cards.Store
{
    public class JSPersonalListStoreExtension : CardStoreExtension
    {
        private readonly ICardRepository cardRepository;
        private readonly IDbScope dbScope;
        private readonly Guid STATIC_ROLE_TYPE_ID = new Guid("825dbacc-ddec-00d1-a550-2a837792542e");
        private const string PERSONAL_LIST_SECTION = "PersonalList";
        private const string PERSONAL_LIST_ROLES = "PersonalListRoles";

        public JSPersonalListStoreExtension(
            ICardRepository cardRepository,
            IDbScope dbScope)
        {
            this.cardRepository = cardRepository;
            this.dbScope = dbScope;
        }
        public override async Task BeforeRequest(ICardStoreExtensionContext context)
        {
            var card = context.Request.Card;
            if (card is null)
            {
                return;
            }

            if (card.StoreMode == CardStoreMode.Update &&
                card.Sections.TryGetValue(PERSONAL_LIST_SECTION, out var plSection) &&
                plSection.Fields.TryGetValue("Title", out object titleValue))
            {
                card.Sections[PERSONAL_LIST_SECTION].Fields["LinkedStaticRoleName"] = titleValue;
                return;
            }

            if (card.StoreMode == CardStoreMode.Update)
            {
                return;
            }

            var staticRoleNewGuid = Guid.NewGuid();

            if (!card.Sections.TryGetValue(PERSONAL_LIST_SECTION, out var personalListSection) ||
                personalListSection.Fields["LinkedStaticRoleID"] is not null)
            {
                return;
            }

            var title = personalListSection.Fields["Title"].ToString();

            if (!await TryCreateStaticRole(staticRoleNewGuid, title))
            {
                context.ValidationResult.AddError("Роль не создана.");
                return;
            }

            card.Sections[personalListSection.Name].Fields["LinkedStaticRoleID"] = staticRoleNewGuid;
            card.Sections[personalListSection.Name].Fields["LinkedStaticRoleName"] = title;
        }


        public override async Task AfterRequest(ICardStoreExtensionContext context)
        {
            var card = context.Request.Card;

            var personalListSection = await GetSection(card, PERSONAL_LIST_SECTION);
            var personalListRolesSection = await GetSection(card, PERSONAL_LIST_ROLES);

            var roleID = personalListSection.Fields.TryGet<Guid>("LinkedStaticRoleID", Guid.Empty);

            if (roleID == Guid.Empty)
            {
                context.ValidationResult.AddError("Отсутствует ID статической роли");
                return;
            }
            var role = await GetStaticRoleAsync(roleID);

            var employees = await GetEmployees(personalListRolesSection, role);

            //TODO: Можно заменить на виртуальную секцию + GET расширение на заполнение при загрузке карточки
            role.Users!.Clear();
            try
            {
                role.Users = employees;
            }
            catch
            {

            }

            //Обновление имени статической роли при изменении
            role.Name = personalListSection.Fields.Get<string>("Title");
            role.Hidden = personalListSection.Fields.Get<bool>("Status");
            await StoreStaticRoleAsync(role.ID, role);

            //Скрытие статической роли при установленой галочке

        }

        public async Task<StaticRole> GetStaticRoleAsync(
            Guid roleID,
            CancellationToken cancellationToken = default)
        {
            var getRequest = new CardGetRequest
            {
                CardID = roleID,
                CardTypeID = RoleHelper.StaticRoleTypeID,
                CardTypeName = RoleHelper.StaticRoleTypeName
            };

            var getResponse = await cardRepository.GetAsync(getRequest, cancellationToken);
            var department = getResponse.TryGetCard();

            var rolesFields = department!.Sections["Roles"].Fields;
            return new StaticRole
            {
                ID = roleID,
                Name = rolesFields.TryGet<string>("Name") ?? string.Empty,
                ParentID = rolesFields.TryGet<Guid?>("ParentID"),
                ParentName = rolesFields.TryGet<string>("ParentName"),
                RoleType = (RoleType)rolesFields.TryGet<int>("TypeID"),
                Hidden = rolesFields.TryGet<bool>("Hidden"),
                DisableDeputies = rolesFields.TryGet<bool>("DisableDeputies"),
                Modified = department.Modified ?? DateTime.MinValue,
                ModifiedByID = department.ModifiedByID
            };
        }

        public async Task StoreStaticRoleAsync(
            Guid roleID,
            StaticRole staticRole,
            CancellationToken cancellationToken = default)
        {
            var getRequest = new CardGetRequest
            {
                CardID = roleID,
                CardTypeID = RoleHelper.StaticRoleTypeID,
                CardTypeName = RoleHelper.StaticRoleTypeName
            };

            var getResponse = await cardRepository.GetAsync(getRequest, cancellationToken);
            var department = getResponse.TryGetCard();
            var rolesFields = department!.Sections["Roles"].Fields;
            var roleUsers = department!.Sections["RoleUsers"];
            foreach (var row in roleUsers.Rows)
            {
                row.State = CardRowState.Deleted;
            }

            foreach(var user in staticRole.Users)
            {
                var row = new CardRow();
                row.State = CardRowState.Inserted;
                row.RowID = Guid.NewGuid();
                row.Fields["UserID"] = user.UserID;
                row.Fields["UserName"] = user.UserName;
                row.Fields["IsDeputy"] = false;
                row.Fields["TypeID"] = RoleType.Static;
                roleUsers.Rows.Add(row);
            }
            var storeRequest = new CardStoreRequest() { Card = department };
            var storeResponse = await cardRepository.StoreAsync(storeRequest, cancellationToken);
        }

        private async Task<CardSection> GetSection(Card card, string sectionName)
        {
            CardSection section = card.Sections.GetOrAdd(sectionName);

            if (card.StoreMode == CardStoreMode.Update)
            {
                var cardFromRepo = (await cardRepository.GetAsync(new CardGetRequest { CardID = card.ID })).Card;
                CardSection sectionFromRepo = cardFromRepo.Sections.TryGet(sectionName);

                if (sectionFromRepo.Type == CardSectionType.Entry && section.RawFields.Count != 0)
                {
                    CardHelper.MergeSection(section, sectionFromRepo);
                }

                if (sectionFromRepo.Type == CardSectionType.Table
                    && section.Type == CardSectionType.Table
                    && section.Rows.Count != 0)
                {
                    CardHelper.MergeSection(section, sectionFromRepo);
                }

                return sectionFromRepo;
            }

            return section;
        }

        private async Task<bool> TryCreateStaticRole(Guid roleID ,string title)
        {
            CardNewRequest request = new() { CardTypeID = STATIC_ROLE_TYPE_ID };
            var response = await cardRepository.NewAsync(request);
            if (!response.ValidationResult.IsSuccessful())
            {
                return false;
            }
            var roleCard = response.Card;

            if (roleCard is null)
                return false;

            roleCard.ID = roleID;

            if (roleCard.Sections.TryGetValue("Roles", out var rolesSection))
            {
                rolesSection.Fields["Name"] = title;
                roleCard.Sections[rolesSection.Name].Set(rolesSection);
            }

            CardStoreRequest storeRequest = new CardStoreRequest { Card = roleCard };
            var storeResponse = await cardRepository.StoreAsync(storeRequest);
            if (storeResponse.ValidationResult.IsSuccessful())
            {
                return true;
            }

            return false;
        }

        private async Task<List<RoleUserRecord>> GetEmployees(CardSection section, StaticRole role)
        {
            List<RoleUserRecord> roleUsers = new();
            section.Rows.ForEach(x =>
            {
                roleUsers.Add(new RoleUserRecord
                {
                    ID = role.ID,
                    RowID = Guid.NewGuid(),
                    UserID = (Guid)x.Fields["RoleID"],
                    UserName = x.Fields["RoleName"].ToString(),
                    RoleType = RoleType.Static,
                    Role = role,
                }) ;
            });
            return roleUsers;
        }
    }
}
