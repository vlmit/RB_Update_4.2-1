#nullable enable

using Newtonsoft.Json;
using NJsonSchema;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tessa.Ai;
using Tessa.Ai.Agent;
using Tessa.Ai.Agent.Models;
using Tessa.Ai.AiViewSearch;
using Tessa.Ai.MarkdownLinks;
using Tessa.Ai.Models;
using Tessa.Ai.Prompts;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Platform.Server.Roles;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Properties.Resharper;
using Tessa.Roles;
using DataType = LinqToDB.DataType;

namespace Tessa.Extensions.Default.Server.Ai.Plugins
{
    /// <summary>
    /// Плагин ИИ-агента для создания карточки сотрудника.
    /// </summary>
    /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
    /// <param name="krPermissionsManager"><inheritdoc cref="IKrPermissionsManager" path="/summary"/></param>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="mdLinkProvider"><inheritdoc cref="IMdLinkProvider" path="/summary"/></param>
    /// <param name="userNamingStrategy"><inheritdoc cref="IUserNamingStrategy" path="/summary"/></param>
    /// <param name="aiDepartmentSearchService"><inheritdoc cref="IAiDepartmentSearchService" path="/summary"/></param>
    public sealed class UserCreateCardAiAgentPlugin(
        ICardRepository cardRepository,
        IKrPermissionsManager krPermissionsManager,
        IDbScope dbScope,
        IMdLinkProvider mdLinkProvider,
        IUserNamingStrategy userNamingStrategy,
        IAiDepartmentSearchService aiDepartmentSearchService) : IAiAgentPlugin
    {
        #region Nested Types

        /// <summary>
        /// Информация о пользователе, заполняемая ИИ.
        /// </summary>
        [Display(Name = "Информация о пользователе")]
        private sealed class AiUserInfo
        {
            [UsedImplicitly]
            [Display(Name = "Фамилия сотрудника", Description = "Фамилия сотрудника в именительном падеже.")]
            public string? LastName { get; set; }

            [UsedImplicitly]
            [Display(Name = "Имя сотрудника", Description = "Имя сотрудника в именительном падеже, должно быть явно указано более 1 буквы. Если указана только 1 буква, то верни пустое значение. ВАЖНО: не пытайся инициалы развернуть в полное имя, если нет полного имени, то верни пустой параметр.")]
            public string? FirstName { get; set; }

            [UsedImplicitly]
            [Display(Name = "Отчество сотрудника", Description = "Отчество сотрудника в именительном падеже, должно быть явно указано более 1 буквы. Если указана только 1 буква, то верни пустое значение. ВАЖНО: не пытайся инициалы развернуть в полное отчество, если нет полного отчества, то верни пустой параметр.")]
            public string? MiddleName { get; set; }

            [UsedImplicitly]
            [Display(Name = "Дата рождения сотрудника", Description = "Дата рождения, сотруднику должно быть 14 лет или более, иначе верни пустое значение.")]
            public DateTime? BirthDate { get; set; }

            [UsedImplicitly]
            [Display(Name = "Должность", Description = "Должность сотрудника в именительном падеже.")]
            public string? Position { get; set; }

            [UsedImplicitly]
            [Display(Name = "Адрес электронной почты.")]
            public string? Email { get; set; }

            [UsedImplicitly]
            [Display(Name = "Номер телефона.")]
            public string? Phone { get; set; }

            [UsedImplicitly]
            [Display(Name = "Номер мобильного телефона.")]
            public string? MobilePhone { get; set; }

            [UsedImplicitly]
            [Display(Name = "Тип учетной записи для входа в систему", Description = "Тип учетной записи для входа в систему")]
            public string? LoginTypeName { get; set; }

            [UsedImplicitly]
            [Display(Name = "Права (уровень доступа) сотрудника", Description = "Уровень доступа сотрудника, если не указано, не заполняй")]
            public string? AccessLevelName { get; set; }

            [UsedImplicitly]
            [Display(Name = "Учётная запись (логин)", Description = "Название учетной записи для входа в систему, логин.")]
            public string? Login { get; set; }

            [UsedImplicitly]
            [Display(Name = "Пароль для входа в систему", Description = "Комбинация из букв, чисел, символов.")]
            public string? Password { get; set; }

            [UsedImplicitly]
            [Display(Name = "Подразделение (отдел) сотрудника", Description = "Подразделение (отдел) организации, в который должен входить сотрудник, в именительном падеже.")]
            public string? DepartmentName { get; set; }
        }

        /// <summary>
        /// Информация о пользователе
        /// </summary>
        private sealed class UserInfo : StorageSerializable
        {
            #region Properties

            public string LastName { get; set; } = null!;

            public string FirstName { get; set; } = null!;

            public string? MiddleName { get; set; }

            public DateTime? BirthDate { get; set; }

            public string? Position { get; set; }

            public string? Email { get; set; }

            public string? Phone { get; set; }

            public string? MobilePhone { get; set; }

            public int? LoginTypeID { get; set; }

            public string? LoginTypeName { get; set; }

            public int? AccessLevelID { get; set; }

            public string? AccessLevelName { get; set; }

            public string? Login { get; set; }

            public string? Password { get; set; }

            public Guid? DepartmentID { get; set; }

            public string? DepartmentName { get; set; }

            #endregion

            #region Base Overrides

            /// <inheritdoc />
            protected override void SerializeCore(Dictionary<string, object?> storage)
            {
                storage.SetIfNotEmpty(nameof(this.LastName), this.LastName);
                storage.SetIfNotEmpty(nameof(this.FirstName), this.FirstName);
                storage.SetIfNotEmpty(nameof(this.MiddleName), this.MiddleName);
                storage.SetIfNotNull(nameof(this.BirthDate), this.BirthDate);
                storage.SetIfNotEmpty(nameof(this.Position), this.Position);
                storage.SetIfNotEmpty(nameof(this.Email), this.Email);
                storage.SetIfNotEmpty(nameof(this.Phone), this.Phone);
                storage.SetIfNotEmpty(nameof(this.MobilePhone), this.MobilePhone);
                storage.SetIfNotNull(nameof(this.LoginTypeID), this.LoginTypeID);
                storage.SetIfNotEmpty(nameof(this.LoginTypeName), this.LoginTypeName);
                storage.SetIfNotNull(nameof(this.AccessLevelID), this.AccessLevelID);
                storage.SetIfNotEmpty(nameof(this.AccessLevelName), this.AccessLevelName);
                storage.SetIfNotEmpty(nameof(this.Login), this.Login);
                storage.SetIfNotEmpty(nameof(this.Password), this.Password);
                storage.SetIfNotNull(nameof(this.DepartmentID), this.DepartmentID);
                storage.SetIfNotEmpty(nameof(this.DepartmentName), this.DepartmentName);
            }

            /// <inheritdoc />
            protected override void DeserializeCore(Dictionary<string, object?> storage)
            {
                this.FirstName = NotWhiteSpaceOrThrow(storage.TryGet<string>(nameof(this.FirstName)));
                this.LastName = NotWhiteSpaceOrThrow(storage.TryGet<string>(nameof(this.LastName)));
                this.MiddleName = storage.TryGet<string>(nameof(this.MiddleName));
                this.BirthDate = storage.TryGet<DateTime?>(nameof(this.BirthDate));
                this.Position = storage.TryGet<string>(nameof(this.Position));
                this.Email = storage.TryGet<string>(nameof(this.Email));
                this.Phone = storage.TryGet<string>(nameof(this.Phone));
                this.MobilePhone = storage.TryGet<string>(nameof(this.MobilePhone));
                this.LoginTypeID = storage.TryGet<int?>(nameof(this.LoginTypeID));
                this.LoginTypeName = storage.TryGet<string>(nameof(this.LoginTypeName));
                this.AccessLevelID = storage.TryGet<int?>(nameof(this.AccessLevelID));
                this.AccessLevelName = storage.TryGet<string>(nameof(this.AccessLevelName));
                this.Login = storage.TryGet<string>(nameof(this.Login));
                this.Password = storage.TryGet<string>(nameof(this.Password));
                this.DepartmentID = storage.TryGet<Guid?>(nameof(this.DepartmentID));
                this.DepartmentName = storage.TryGet<string>(nameof(this.DepartmentName));
            }

            #endregion
        }

        /// <summary>
        /// Возможные значения типа входа в систему.
        /// </summary>
        private static class AiLoginTypes
        {
            public const string Internal = "tessa";

            public const string Windows = "windows";

            public const string Ldap = "ldap";
        }

        /// <summary>
        /// Возможные значения уровней доступа.
        /// </summary>
        private static class AiAccessLevels
        {
            public const string Regular = "user";

            public const string Administrator = "admin";
        }

        #endregion

        #region Fields

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);
        private readonly IKrPermissionsManager krPermissionsManager = NotNullOrThrow(krPermissionsManager);
        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);
        private readonly IMdLinkProvider mdLinkProvider = NotNullOrThrow(mdLinkProvider);
        private readonly IUserNamingStrategy userNamingStrategy = NotNullOrThrow(userNamingStrategy);
        private readonly IAiDepartmentSearchService aiDepartmentSearchService = NotNullOrThrow(aiDepartmentSearchService);

        private AiCachedToolSettings? baseSettings;
        private AiCachedPrompts? cachedPrompts;

        #endregion

        #region IAiAgentPlugin Members

        /// <inheritdoc />
        public async ValueTask<IReadOnlyList<AiToolInfo>> GetToolsAsync(CancellationToken cancellationToken) =>
            [(await this.GetBaseSettingsAsync(cancellationToken)).Settings];

        /// <inheritdoc />
        public async ValueTask<AiToolSettings> GetToolSettingsAsync(string toolId, CancellationToken cancellationToken)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<UserCreateCardAiAgentPlugin>(toolId, toolInfo.ID);
            return toolInfo;
        }

        /// <inheritdoc />
        public async ValueTask<AiToolApplicability> IsApplicableAsync(string toolId, AiRequest? request, CancellationToken cancellationToken)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            if (toolId != toolInfo.ID)
            {
                return new AiToolApplicability();
            }

            if (request is not null)
            {
                return
                    request.Context?.Type is null or AiContextType.None
                        ? new AiToolApplicability { Available = true, Visible = true }
                        : new AiToolApplicability();
            }

            return new AiToolApplicability { Available = true };
        }
        
        /// <inheritdoc />
        public async ValueTask<AiInstructionsResult> GetInstructionsAsync(
            string toolId,
            AiAgentContext context,
            CancellationToken cancellationToken)
        {
            var (toolInfo, baseScheme) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<UserCreateCardAiAgentPlugin>(toolId, toolInfo.ID);

            if (!context.Session.User.IsAdministrator())
            {
                var validationResult = new ValidationResultBuilder();
                var result = await this.CheckPermissionsAsync(
                    validationResult,
                    cancellationToken);

                AiHelper.Logger.LogResult(validationResult);

                if (!result)
                {
                    context.Response.Message = await LocalizeAsync("$Ai_UserCreateCardPlugin_NotPermissions");
                    return new(AiInstructionResultCode.Error);
                }
            }

            var settings = await context.ToolManager.TryGetToolSettingsAsync(toolId, cancellationToken);
            ThrowIfNull(settings);
            var actual = await AiPromptsHelper.CacheAsync(this.cachedPrompts, settings, baseScheme, cancellationToken);
            this.cachedPrompts = actual;

            var prompt = new AiPromptBuilder()
                .WithTemplates(actual.Templates)
                .WithRoot(AiPromptTemplates.AiTool.RootPromptName)
                .WithCurrentDateTime(DateTime.UtcNow + context.Session.ClientUtcOffset, false, context.Session.ClientUICulture)
                .Build();

            return new(prompt, actual.Schema);
        }

        /// <inheritdoc />
        public async ValueTask<AiRecognitionResult> HandleRecognizedDataAsync(string toolId, string? data, AiAgentContext context, CancellationToken cancellationToken)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<UserCreateCardAiAgentPlugin>(toolId, toolInfo.ID);
            ThrowIfNullOrWhiteSpace(data);

            if (JsonConvert.DeserializeObject<AiUserInfo>(data) is not { } aiUserInfo)
            {
                context.ValidationResult.AddError(this, AiConstants.FailedToDeserializeAiLocalization);
                return AiRecognitionResult.Error;
            }

            var messageBuilder = StringBuilderHelper.Acquire();
            var userInfo = await this.FillUserInfoAsync(aiUserInfo, context.Session.User.IsAdministrator(), context.ValidationResult, messageBuilder, cancellationToken);

            if (userInfo is null)
            {
                context.Response.Message = messageBuilder.ToStringAndRelease();
                return AiRecognitionResult.Clarification;
            }

            messageBuilder
                .Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_CardWillBeCreated"))
                .AppendLine("  ");

            await FormatUserDataAsync(messageBuilder, aiUserInfo);

            context.Response.Data = userInfo.ToSerializedDictionary();
            context.Response.Message = messageBuilder.ToStringAndRelease();
            context.Response.Buttons = [
                AiActions.Create,
                AiActions.Reject];

            return AiRecognitionResult.Confirmation;
        }

        /// <inheritdoc />
        public async ValueTask ProcessAsync(string toolId, AiAgentContext context, CancellationToken cancellationToken)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<UserCreateCardAiAgentPlugin>(toolId, toolInfo.ID);

            if (context.Request.Data is not Dictionary<string, object?> storage)
            {
                context.ValidationResult.AddError(this, AiConstants.FailedToDeserializeAiLocalization);
                return;
            }

            var userInfo = new UserInfo();
            userInfo.Deserialize(storage);

            var newResponse = await this.cardRepository.NewAsync(
                new CardNewRequest()
                {
                    CardTypeID = RoleHelper.PersonalRoleTypeID,
                    ServiceType = CardServiceType.Client
                },
                cancellationToken: cancellationToken);

            context.ValidationResult.Add(newResponse.ValidationResult);

            if (!newResponse.ValidationResult.IsSuccessful())
            {
                return;
            }

            var card = newResponse.Card;

            await this.FillCardAsync(userInfo, card, cancellationToken);

            card.RemoveAllButChanged(card.StoreMode);

            var storeResponse = await this.cardRepository.StoreAsync(
                new CardStoreRequest()
                {
                    Card = card,
                    ServiceType = CardServiceType.Client
                },
                cancellationToken: cancellationToken);

            context.ValidationResult.Add(storeResponse.ValidationResult);

            if (!storeResponse.ValidationResult.IsSuccessful())
            {
                return;
            }

            var shortName = this.userNamingStrategy.GetShortName(
                new UserNamingInfo
                {
                    FirstName = userInfo.FirstName,
                    LastName = userInfo.LastName,
                    MiddleName = userInfo.MiddleName,
                });

            context.Response.Message = $"{await LocalizeAsync("$Ai_UserCreateCardPlugin_CardCreated")} {this.mdLinkProvider.GetCardLink(card.ID, shortName)}";
        }

        #endregion

        #region Private Methods

        private async ValueTask<AiCachedToolSettings> GetBaseSettingsAsync(CancellationToken cancellationToken)
        {
            if (this.baseSettings is { } settings)
            {
                return settings;
            }

            // если будет много конкурентных обращений, нужно будет добавить AsyncLock
            var baseScheme = JsonSchema.FromType<AiUserInfo>();
            // модифицируем схему, прописываем статические данные
            baseScheme.Properties[nameof(AiUserInfo.AccessLevelName)].Enumeration
                .AddRange([AiAccessLevels.Regular, AiAccessLevels.Administrator]);
            baseScheme.Properties[nameof(AiUserInfo.LoginTypeName)].Enumeration
                .AddRange([AiLoginTypes.Internal, AiLoginTypes.Windows, AiLoginTypes.Ldap]);
            // встариваем опеределения
            baseScheme = await baseScheme.EmbedReferencesAsync(cancellationToken);

            var toolInfo = new AiToolSettings()
            {
                ID = "user_create_card",
                Name = "Создание нового сотрудника",
                Description = "Позволяет добавить в справочник сотрудников нового пользователя",

                PluginName = nameof(UserCreateCardAiAgentPlugin),
                Prompts = AiPromptsHelper.GetPrompts(AiPromptTemplates.AiTool.Templates),
                Scheme = baseScheme.ExtractDescription(),
            };

            settings = new(toolInfo, baseScheme);
            this.baseSettings = settings;
            return settings;
        }

        /// <summary>
        /// Проверяет разрешено ли выполнение инструмента.
        /// </summary>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Значение <see langword="true"/>, если выполнение разрешено, иначе - <see langword="false"/>.</returns>
        private async Task<bool> CheckPermissionsAsync(
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var permissionsContextResult = await this.krPermissionsManager.TryCreateContextAsync(
                new KrPermissionsCreateContextParams
                {
                    CardTypeID = RoleHelper.PersonalRoleTypeID,
                    ValidationResult = validationResult,
                    ServiceType = CardServiceType.Client,
                },
                cancellationToken: cancellationToken);

            return permissionsContextResult.Status switch
            {
                KrPermissionsCreateContextStatus.Success =>
                    await this.krPermissionsManager.CheckRequiredPermissionsAsync(
                        permissionsContextResult.Context,
                        KrPermissionFlagDescriptors.CreateCard),
                KrPermissionsCreateContextStatus.Fail => false,
                KrPermissionsCreateContextStatus.NotAllowed => true,
                _ => throw ArgumentOutOfRange(permissionsContextResult.Status)
            };
        }

        /// <summary>
        /// Подготавливает данные сотрудника на основе данных, полученных от ИИ.
        /// </summary>
        /// <param name="aiUserInfo"><inheritdoc cref="AiUserInfo" path="/summary"/></param>
        /// <param name="isAdmin">Признак, что пользователь является администраторм системы.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="messageBuilder"><inheritdoc cref="StringBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Данные сотрудника или <see langword="null"/>, если есть ошибки.</returns>
        private async Task<UserInfo?> FillUserInfoAsync(
            AiUserInfo aiUserInfo,
            bool isAdmin,
            IValidationResultBuilder validationResult,
            StringBuilder messageBuilder,
            CancellationToken cancellationToken = default)
        {
            if (aiUserInfo.AccessLevelName == AiAccessLevels.Administrator
                && !isAdmin)
            {
                messageBuilder.Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_NoAdminAccess")).AppendLine("  ");
            }

            if (string.IsNullOrWhiteSpace(aiUserInfo.FirstName))
            {
                messageBuilder.Append(await LocalizeAsync("$Ai_Plugin_Message_NoEmployeeName")).AppendLine("  ");
            }

            if (string.IsNullOrWhiteSpace(aiUserInfo.LastName))
            {
                messageBuilder.Append(await LocalizeAsync("$Ai_Plugin_Message_NoEmployeeLastName")).AppendLine("  ");
            }

            if (aiUserInfo.LoginTypeName == AiLoginTypes.Internal
                && string.IsNullOrWhiteSpace(aiUserInfo.Password))
            {
                messageBuilder.Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_NoPassword")).AppendLine("  ");
            }

            if (!string.IsNullOrWhiteSpace(aiUserInfo.Login))
            {
                var duplicateUser = await this.GetDuplicateUserInfoAsync(aiUserInfo.Login, cancellationToken);

                if (duplicateUser.HasValue)
                {
                    messageBuilder
                        .Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_DuplicatesFound"))
                        .Append($" {aiUserInfo.Login}: ")
                        .Append(this.mdLinkProvider.GetCardLink(duplicateUser.Value.ID, duplicateUser.Value.ShortName))
                        .AppendLine("  ");
                }
            }

            var departmentInfo = await this.FindDepartmentOrAddErrorAsync(
                aiUserInfo.DepartmentName,
                validationResult,
                messageBuilder,
                cancellationToken);

            if (messageBuilder.Length > 0)
            {
                messageBuilder.AppendLine("  ").Append(await LocalizeAsync("$Ai_Plugin_Message_Clarify"));
                return null;
            }

            var userInfo = new UserInfo();

            userInfo.LastName = aiUserInfo.LastName!;
            userInfo.FirstName = aiUserInfo.FirstName!;
            userInfo.MiddleName = aiUserInfo.MiddleName;
            userInfo.BirthDate = aiUserInfo.BirthDate;
            userInfo.Position = aiUserInfo.Position;
            userInfo.Email = aiUserInfo.Email;
            userInfo.Phone = aiUserInfo.Phone;
            userInfo.MobilePhone = aiUserInfo.MobilePhone;

            var loginType = GetLoginType(aiUserInfo.LoginTypeName, !string.IsNullOrWhiteSpace(aiUserInfo.Password));
            userInfo.LoginTypeID = loginType?.ID;
            userInfo.LoginTypeName = loginType?.Name;

            var accessLevel = GetAccessLevel(aiUserInfo.AccessLevelName);
            userInfo.AccessLevelID = accessLevel.HasValue ? (int)accessLevel.Value.ID : null;
            userInfo.AccessLevelName = accessLevel?.Name;

            userInfo.Login = aiUserInfo.Login;

            if (!string.IsNullOrWhiteSpace(aiUserInfo.Password))
            {
                userInfo.Password = Convert.ToBase64String(Encoding.UTF8.GetBytes(aiUserInfo.Password));
            }

            userInfo.DepartmentID = departmentInfo?.ID;
            userInfo.DepartmentName = departmentInfo?.Name;

            return userInfo;
        }

        /// <summary>
        /// Получить из базы данных справочную информацию о подразделении, или <c>null</c> если есть ошибки.
        /// Если есть ошибки - они будут добавлены в outMessageBuilder.
        /// </summary>
        /// <param name="departmentName">Наименование подразделения, полученное от ИИ агента.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="messageBuilder"><inheritdoc cref="StringBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>
        /// Справочная информация о подразделении или <see langword="null"/>.
        /// </returns>
        private Task<AiDepartmentInfo?> FindDepartmentOrAddErrorAsync(
            string? departmentName,
            IValidationResultBuilder validationResult,
            StringBuilder messageBuilder,
            CancellationToken cancellationToken = default) =>
            string.IsNullOrWhiteSpace(departmentName)
            ? Task.FromResult<AiDepartmentInfo?>(null)
            : this.aiDepartmentSearchService.FindDepartmentOrAddErrorAsync(
                departmentName,
                validationResult,
                messageBuilder,
                [],
                cancellationToken);

        /// <summary>
        /// Получает тип входа в систему по полученным данным от ИИ.
        /// </summary>
        /// <param name="loginTypeName">Наименование типа входа в систему, полученное от ИИ.</param>
        /// <param name="hasPassword">Заполнен ли пароль сотрудника.</param>
        /// <returns>Кортеж, содержащий идентификатор типа входа в систему и ключ локализации наименования типа входа в систему, или <see langword="null"/>, если значение не найдено.</returns>
        private static (int ID, string Name)? GetLoginType(string? loginTypeName, bool hasPassword) =>
            loginTypeName switch
            {
                AiLoginTypes.Internal => (UserLoginTypes.Internal.ID, "$Enum_LoginTypes_Internal"),
                AiLoginTypes.Windows => (UserLoginTypes.Windows.ID, "$Enum_LoginTypes_Windows"),
                AiLoginTypes.Ldap => (UserLoginTypes.Ldap.ID, "$Enum_LoginTypes_Ldap"),
                _ => hasPassword ? (UserLoginTypes.Internal.ID, "$Enum_LoginTypes_Internal") : null
            };

        /// <summary>
        /// Получает уровень доступа по полученным данным от ИИ.
        /// </summary>
        /// <param name="accessLevelName">Наименование уровня доступа, полученное от ИИ.</param>
        /// <returns>Кортеж, содержащий идентификатор уровня доступа и ключ локализации наименования уровня доступа, или <see langword="null"/>, если значение не найдено.</returns>
        private static (UserAccessLevel ID, string Name)? GetAccessLevel(string? accessLevelName) =>
            accessLevelName switch
            {
                AiAccessLevels.Regular => (UserAccessLevel.Regular, "$Enum_AccessLevels_Regular"),
                AiAccessLevels.Administrator => (UserAccessLevel.Administrator, "$Enum_AccessLevels_Administrator"),
                _ => null,
            };


        /// <summary>
        /// Получение информации дублируемого сотрудника.
        /// </summary>
        /// <param name="login">Логин сотрудника.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Кортеж, содержащий идентификатор и полное имя пользователя, или значение <see langword="null"/>, если его не удалось определить.</returns>
        private async Task<(Guid ID, string ShortName)?> GetDuplicateUserInfoAsync(string login, CancellationToken cancellationToken = default)
        {
            await using var _ = this.dbScope.Create();
            var db = this.dbScope.Db;

            db.SetCommand(
                this.dbScope.BuilderFactory
                    .Select().Top(1)
                    .C("pr", "ID", "FirstName", "LastName", "MiddleName")
                    .From("PersonalRoles", "pr")
                    .Where()
                    .LowerC("pr", "Login").Equals().LowerP(nameof(login))
                    .Limit(1)
                    .Build(),
                db.Parameter(nameof(login), login, DataType.NVarChar))
            .LogCommand();

            await using var reader = await db.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetGuid(0);

                var shortName = this.userNamingStrategy.GetShortName(new UserNamingInfo
                {
                    FirstName = reader.GetString(1),
                    LastName = reader.GetNullableString(2),
                    MiddleName = reader.GetNullableString(3)
                });

                return new(id, shortName);
            }

            return null;
        }

        /// <summary>
        /// Получает данные, заполненные ИИ, в виде строки для вывода пользователю.
        /// </summary>
        /// <param name="stringBuilder"><inheritdoc cref="StringBuilder" path="/summary"/></param>
        /// <param name="aiUserInfo"><inheritdoc cref="AiUserInfo" path="/summary"/></param>
        /// <returns><inheritdoc cref="Task" path="/summary"/></returns>
        private static async Task FormatUserDataAsync(StringBuilder stringBuilder, AiUserInfo aiUserInfo)
        {
            stringBuilder.AppendLine("  ")
                .Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_LastName"))
                .Append(": ")
                .Append(aiUserInfo.LastName)
                .AppendLine("  ")
                .Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_FirstName"))
                .Append(": ")
                .Append(aiUserInfo.FirstName);

            if (!string.IsNullOrWhiteSpace(aiUserInfo.MiddleName))
            {
                stringBuilder.AppendLine("  ")
                    .Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_MiddleName"))
                    .Append(": ")
                    .Append(aiUserInfo.MiddleName);
            }

            if (aiUserInfo.BirthDate.HasValue)
            {
                stringBuilder.AppendLine("  ")
                    .Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_BirthDate"))
                    .Append(": ")
                    .Append(aiUserInfo.BirthDate.Value.ToShortDateString());
            }

            if (!string.IsNullOrWhiteSpace(aiUserInfo.Position))
            {
                stringBuilder.AppendLine("  ")
                    .Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_Position"))
                    .Append(": ")
                    .Append(aiUserInfo.Position);
            }

            if (!string.IsNullOrWhiteSpace(aiUserInfo.Email))
            {
                stringBuilder.AppendLine("  ")
                    .Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_Email"))
                    .Append(": ")
                    .Append(aiUserInfo.Email);
            }

            if (!string.IsNullOrWhiteSpace(aiUserInfo.Phone))
            {
                stringBuilder.AppendLine("  ")
                    .Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_Phone"))
                    .Append(": ")
                    .Append(aiUserInfo.Phone);
            }

            if (!string.IsNullOrWhiteSpace(aiUserInfo.MobilePhone))
            {
                stringBuilder.AppendLine("  ")
                    .Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_MobilePhone"))
                    .Append(": ")
                    .Append(aiUserInfo.MobilePhone);
            }

            if (!string.IsNullOrWhiteSpace(aiUserInfo.LoginTypeName))
            {
                stringBuilder.AppendLine("  ")
                    .Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_LoginTypeName"))
                    .Append(": ")
                    .Append(aiUserInfo.LoginTypeName);
            }

            if (!string.IsNullOrWhiteSpace(aiUserInfo.AccessLevelName))
            {
                stringBuilder.AppendLine("  ")
                    .Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_AccessLevelName"))
                    .Append(": ")
                    .Append(aiUserInfo.AccessLevelName);
            }

            if (!string.IsNullOrWhiteSpace(aiUserInfo.Login))
            {
                stringBuilder.AppendLine("  ")
                    .Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_Login"))
                    .Append(": ")
                    .Append(aiUserInfo.Login);
            }

            if (!string.IsNullOrWhiteSpace(aiUserInfo.Password))
            {
                stringBuilder.AppendLine("  ")
                    .Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_Password"))
                    .Append(": ")
                    .Append(aiUserInfo.Password);
            }

            if (!string.IsNullOrWhiteSpace(aiUserInfo.DepartmentName))
            {
                stringBuilder.AppendLine("  ")
                    .Append(await LocalizeAsync("$Ai_UserCreateCardPlugin_DepartmentName"))
                    .Append(": ")
                    .Append(aiUserInfo.DepartmentName);
            }
        }

        /// <summary>
        /// Заполняет карточку сотрудника.
        /// </summary>
        /// <param name="userInfo"><inheritdoc cref="AiUserInfo" path="/summary"/></param>
        /// <param name="card">Карточка сотрудника.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="Task" path="/summary"/></returns>
        private async Task FillCardAsync(UserInfo userInfo, Card card, CancellationToken cancellationToken = default)
        {
            card.ID = Guid.NewGuid();

            var mainFields = card.Sections[RoleStrings.PersonalRoles].Fields;
            var departmentSection = card.Sections[RoleStrings.PersonalRoleDepartmentsVirtual];

            mainFields.SetIfNotDefault("LastName", userInfo.LastName);
            mainFields.SetIfNotDefault("FirstName", userInfo.FirstName);
            mainFields.SetIfNotDefault("MiddleName", userInfo.MiddleName);
            mainFields.SetIfNotDefault("BirthDate", userInfo.BirthDate);
            mainFields.SetIfNotDefault("Position", userInfo.Position);
            mainFields.SetIfNotDefault("Email", userInfo.Email);
            mainFields.SetIfNotDefault("Phone", userInfo.Phone);
            mainFields.SetIfNotDefault("MobilePhone", userInfo.MobilePhone);
            mainFields.SetIfNotDefault("LoginTypeID", userInfo.LoginTypeID);
            mainFields.SetIfNotDefault("LoginTypeName", userInfo.LoginTypeName);
            mainFields.SetIfNotDefault("AccessLevelID", userInfo.AccessLevelID);
            mainFields.SetIfNotDefault("AccessLevelName", userInfo.AccessLevelName);
            mainFields.SetIfNotDefault("Login", userInfo.Login);

            if (!string.IsNullOrEmpty(userInfo.Password))
            {
                var prFieldsVirtual = card.Sections[RoleStrings.PersonalRolesVirtual].Fields;
                prFieldsVirtual.SetIfNotDefault("Password", userInfo.Password);
            }

            if (userInfo.DepartmentID.HasValue)
            {
                var newRow = departmentSection.Rows.Add();

                newRow.State = CardRowState.Inserted;
                newRow.RowID = Guid.NewGuid();
                newRow.Fields["DepartmentID"] = userInfo.DepartmentID;
                newRow.Fields["DepartmentName"] = userInfo.DepartmentName;
            }
        }

        #endregion
    }
}
