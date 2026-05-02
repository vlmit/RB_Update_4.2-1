using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Cards;
using Tessa.Platform.ConsoleApps;
using Tessa.Platform.Runtime;
using Tessa.Roles;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;

namespace Tessa.Extensions.Default.Console.User
{
    public sealed class Operation(
        IConsoleSessionManager sessionManager,
        IConsoleLogger logger,
        ICardRepository cardRepository,
        ICardMetadata cardMetadata,
        IViewService viewService,
        IViewSpecialParameters viewSpecialParameters)
        : ConsoleOperation<OperationContext>(logger, sessionManager, extendedInitialization: true)
    {
        #region Fields

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);

        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);

        private readonly IViewService viewService = NotNullOrThrow(viewService);

        private readonly IViewSpecialParameters viewSpecialParameters = NotNullOrThrow(viewSpecialParameters);

        #endregion

        #region Private Methods

        private async Task<Guid?> TryFindUserByNameAsync(
            OperationContext context,
            string? userName,
            CancellationToken cancellationToken = default)
        {
            const string viewAlias = "Users";
            const string nameParamAlias = "Name";
            const string showHiddenParamAlias = "ShowHidden";
            const string userIDColumnAlias = "UserID";
            const int pageLimit = 2;

            var view = await this.viewService.GetByNameAsync(viewAlias, cancellationToken);
            if (view is null)
            {
                await this.Logger.ErrorAsync("Can't access view \"{0}\"", viewAlias);
                return null;
            }

            var viewMetadata = await view.GetMetadataAsync(cancellationToken);

            // параметр для поиска по имени
            if (!viewMetadata.Parameters.IsDefinedByName(nameParamAlias))
            {
                await this.Logger.ErrorAsync("Can't find parameter \"{0}\" for view \"{1}\"", nameParamAlias, viewAlias);
                return null;
            }

            var viewRequest = new TessaViewRequest(viewMetadata.Alias)
            {
                new RequestParameter(nameParamAlias).Add(StartsWithOperator.Instance, userName),
                // параметр для поиска всех сотрудников, включая скрытых (может отсутствовать в метаинформации)
                viewMetadata.Parameters.IsDefinedByName(showHiddenParamAlias) ? new RequestParameter(showHiddenParamAlias) { IsTrueCriteriaOperator.Instance } : null
            };

            if (viewMetadata.Paging != Paging.No)
            {
                this.viewSpecialParameters
                    .ProvidePageLimitParameter(viewRequest.Parameters, pageLimit)
                    .ProvidePageOffsetParameter(viewRequest.Parameters, 1, pageLimit);
            }

            // получаем данные представления
            var viewResult = await view.GetDataAsync(viewRequest, cancellationToken);
            switch (viewResult.Rows.Count)
            {
                case 0:
                    await this.Logger.ErrorAsync("Can't find user by name \"{0}\"", userName);
                    return null;

                case > 1:
                    await this.Logger.ErrorAsync("Found more than one user by name \"{0}\". Please, specify name to match exactly one user.", userName);
                    return null;
            }

            var userIDIndex = viewResult.GetColumnIndex(userIDColumnAlias);
            if (userIDIndex < 0)
            {
                await this.Logger.ErrorAsync("View \"{0}\" didn't return column with alias \"{1}\"", viewAlias, userIDColumnAlias);
                return null;
            }

            return (Guid?) viewResult.Rows.First()[userIDIndex];
        }


        private async ValueTask<string?> TryGetLoginTypeNameAsync(
            OperationContext context,
            int loginTypeID,
            CancellationToken cancellationToken = default)
        {
            const string tableName = "LoginTypes";

            var enumerations = await this.cardMetadata.GetEnumerationsAsync(cancellationToken);
            if (!enumerations.TryGetValue(tableName, out _))
            {
                await this.Logger.ErrorAsync("Can't find enumeration \"{0}\", scheme is corrupted", tableName);
                return null;
            }

            var loginTypes = enumerations["LoginTypes"].Records;
            var record = loginTypes.FirstOrDefault(x => (int?) x["ID"] == loginTypeID);
            if (record is null)
            {
                await this.Logger.ErrorAsync(
                    "Can't find record for login type ID={0} in enumeration \"{1}\", scheme is corrupted",
                    loginTypeID,
                    tableName);

                return null;
            }

            return (string?) record["Name"] ?? string.Empty;
        }

        #endregion

        #region Base Overrides

        /// <inheritdoc />
        public override async Task<int> ExecuteAsync(OperationContext context, CancellationToken cancellationToken = default)
        {
            if (!this.SessionManager.IsOpened)
            {
                return -1;
            }

            try
            {
                await this.Logger.InfoAsync("Updating user started");

                if (!Guid.TryParse(context.User, out var cardID))
                {
                    // ищем пользователя по представлению User
                    await this.Logger.InfoAsync("Searching for user by name \"{0}\"", context.User);

                    var nullableCardID = await this.TryFindUserByNameAsync(context, context.User, cancellationToken);
                    if (!nullableCardID.HasValue)
                    {
                        return -1;
                    }

                    cardID = nullableCardID.Value;
                }

                await this.Logger.InfoAsync("Loading user ID={0:B}", cardID);

                var getRequest = new CardGetRequest
                {
                    CardID = cardID,
                    CardTypeID = RoleHelper.PersonalRoleTypeID,
                    GetMode = CardGetMode.ReadOnly
                };

                var getResponse = await this.cardRepository.GetAsync(getRequest, cancellationToken);
                var getResult = getResponse.ValidationResult.Build();

                await this.Logger.LogResultAsync(getResult);
                if (!getResult.IsSuccessful)
                {
                    return -1;
                }

                var card = getResponse.Card;
                var sections = card.Sections;
                var fields = sections[RoleStrings.PersonalRoles].Fields;
                var virtualFields = sections["PersonalRolesVirtual"].Fields;

                var loginTypeID = UserLoginTypes.Forbidden.ID;
                if (!string.IsNullOrEmpty(context.Account))
                {
                    // аутентификация Windows или LDAP
                    await this.Logger.InfoAsync("Setting {0} auth with Account: \"{1}\"", context.Ldap ? "LDAP" : "Windows", context.Account);
                    loginTypeID = context.Ldap ? (int) UserLoginTypes.Ldap : (int) UserLoginTypes.Windows;

                    fields["Login"] = context.Account.Trim();
                    virtualFields["Password"] = null;
                }
                else if (!string.IsNullOrEmpty(context.Login) && !string.IsNullOrEmpty(context.Password))
                {
                    // аутентификация Tessa
                    await this.Logger.InfoAsync("Setting {0} auth with login: \"{1}\"", UserLoginTypes.Internal, context.Login);
                    loginTypeID = UserLoginTypes.Internal.ID; // Tessa

                    fields["Login"] = context.Login.Trim();

                    var password = context.Password.Trim();
                    virtualFields["Password"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
                }
                else if (context.NoLogin)
                {
                    // аутентификация запрещена
                    await this.Logger.InfoAsync("Setting auth as forbidden, user can't login");
                    loginTypeID = UserLoginTypes.Forbidden.ID;

                    fields["Login"] = null;
                    virtualFields["Password"] = null;
                }

                // устанавливаем тип логина, используя строковые имена из перечисления LoginTypes
                var loginTypeName = await this.TryGetLoginTypeNameAsync(context, loginTypeID, cancellationToken);
                if (loginTypeName is null)
                {
                    return -1;
                }

                fields["LoginTypeID"] = loginTypeID;
                fields["LoginTypeName"] = loginTypeName;

                await this.Logger.InfoAsync("Saving changes for user");

                var storeRequest = new CardStoreRequest { Card = card };
                var storeResponse = await this.cardRepository.StoreAsync(storeRequest, cancellationToken);
                var storeResult = storeResponse.ValidationResult.Build();

                await this.Logger.LogResultAsync(storeResult);
                if (!storeResult.IsSuccessful)
                {
                    return -1;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                await this.Logger.LogExceptionAsync("Error updating user", e);
                return -1;
            }

            await this.Logger.InfoAsync("User has been updated successfully");
            return 0;
        }

        #endregion
    }
}
