#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonSchema;
using Tessa.Ai;
using Tessa.Ai.Agent;
using Tessa.Ai.Agent.Models;
using Tessa.Ai.AiViewSearch;
using Tessa.Ai.MarkdownLinks;
using Tessa.Ai.Models;
using Tessa.Ai.Plugins;
using Tessa.Ai.Prompts;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Properties.Resharper;

namespace Tessa.Extensions.Default.Server.Ai.Plugins
{
    /// <summary>
    /// Плагин ИИ-агента для создания карточки контрагента.
    /// </summary>
    /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
    /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
    /// <param name="aiPartnerSearchService"><inheritdoc cref="IAiPartnerSearchService" path="/summary"/></param>
    /// <param name="krPermissionsManager"><inheritdoc cref="IKrPermissionsManager" path="/summary"/></param>
    /// <param name="mdLinkProvider"><inheritdoc cref="IMdLinkProvider" path="/summary"/></param>
    public sealed class PartnerCreateCardAiAgentPlugin(
        ICardRepository cardRepository,
        ICardMetadata cardMetadata,
        IAiPartnerSearchService aiPartnerSearchService,
        IKrPermissionsManager krPermissionsManager,
        IMdLinkProvider mdLinkProvider) : IAiAgentPlugin
    {
        #region Nested Types

        /// <summary>
        /// Информация о контрагенте заполняемая ИИ.
        /// </summary>
        [Description("Информация о контрагенте")]
        private sealed class AiPartnerDocumentInfo
        {
            [Description("Краткое наименование организации")]
            [UsedImplicitly]
            public string? ShortName { get; set; }

            [Description("Полное наименование организации")]
            [UsedImplicitly]
            public string? FullName { get; set; }

            [Description("Тип контрагента (Юридическое лицо, Физическое лицо, Индивидуальный предприниматель)")]
            [UsedImplicitly]
            public string? Type { get; set; }

            [Description("Руководитель - ФИО")]
            [UsedImplicitly]
            public string? Head { get; set; }

            [Description("Контактное лицо - ФИО")]
            [UsedImplicitly]
            public string? ContactPerson { get; set; }

            [Description("Телефон")]
            [UsedImplicitly]
            public string? Phone { get; set; }

            [Description("Email")]
            [UsedImplicitly]
            public string? Email { get; set; }

            [Description("Юридический адрес")]
            [UsedImplicitly]
            public string? LegalAddress { get; set; }

            [Description("Контактный адрес")]
            [UsedImplicitly]
            public string? ContactAddress { get; set; }

            [Description("ИНН")]
            [UsedImplicitly]
            public string? Inn { get; set; }

            [Description("КПП")]
            [UsedImplicitly]
            public string? Kpp { get; set; }

            [Description("ОГРН")]
            [UsedImplicitly]
            public string? Ogrn { get; set; }

            [Description("ОКПО")]
            [UsedImplicitly]
            public string? Okpo { get; set; }

            [Description("ОКВЭД")]
            [UsedImplicitly]
            public string? Okved { get; set; }

            [Description("Комментарий")]
            [UsedImplicitly]
            public string? Comment { get; set; }

            [Description("Банковские реквизиты")]
            [UsedImplicitly]
            public AiBankDetails? BankDetails { get; set; }

            [Description("Контактные лица - массив")]
            [UsedImplicitly]
            public List<AiPartnerContact>? PartnerContacts { get; set; }
        }

        /// <summary>
        /// Информация о банковских реквизитах заполняемая ИИ.
        /// </summary>
        [Description("Банковские реквизиты")]
        private sealed class AiBankDetails
        {
            [Description("Название банка")]
            [UsedImplicitly]
            public string? Name { get; set; }

            [Description("Расчетный счет")]
            [UsedImplicitly]
            public string? SettlementAccount { get; set; }

            [Description("БИК")]
            [UsedImplicitly]
            public string? Bik { get; set; }

            [Description("Корреспондентский счет")]
            [UsedImplicitly]
            public string? CorrAccount { get; set; }
        }

        /// <summary>
        /// Информация о контактном лице заполняемая ИИ.
        /// </summary>
        [Description("Контактное лицо")]
        private sealed class AiPartnerContact
        {
            [Description("ФИО")]
            [UsedImplicitly]
            public string? Name { get; set; }

            [Description("Подразделение")]
            [UsedImplicitly]
            public string? Department { get; set; }

            [Description("Должность")]
            [UsedImplicitly]
            public string? Position { get; set; }

            [Description("Email")]
            [UsedImplicitly]
            public string? Email { get; set; }

            [Description("Телефон")]
            [UsedImplicitly]
            public string? Phone { get; set; }
        }

        #endregion

        #region Static Fields And Constants

        private AiCachedToolSettings? baseSettings;
        private AiCachedPrompts? cachedPrompts;

        #endregion

        #region Fields

        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);
        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);
        private readonly IAiPartnerSearchService aiPartnerSearchService = NotNullOrThrow(aiPartnerSearchService);
        private readonly IKrPermissionsManager krPermissionsManager = NotNullOrThrow(krPermissionsManager);
        private readonly IMdLinkProvider mdLinkProvider = NotNullOrThrow(mdLinkProvider);

        #endregion

        #region IAiAgentPlugin Members

        /// <inheritdoc />
        public async ValueTask<IReadOnlyList<AiToolInfo>> GetToolsAsync(CancellationToken cancellationToken) =>
            [(await this.GetBaseSettingsAsync(cancellationToken)).Settings];

        /// <inheritdoc />
        public async ValueTask<AiToolSettings> GetToolSettingsAsync(string toolId, CancellationToken cancellationToken)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<PartnerCreateCardAiAgentPlugin>(toolId, toolInfo.ID);
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
            AiHelper.ThrowIfInvalidTool<PartnerCreateCardAiAgentPlugin>(toolId, toolInfo.ID);

            if (!context.Session.User.IsAdministrator())
            {
                var validationResult = new ValidationResultBuilder();
                var result = await this.CheckPermissionsAsync(
                    validationResult,
                    cancellationToken);

                AiHelper.Logger.LogResult(validationResult);

                if (!result)
                {
                    context.Response.Message = await LocalizeAsync("$Ai_PartnerCreateCardAiAgentPlugin_NotPermissions");
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
                .Build();

            return new(prompt, actual.Schema);
        }

        /// <inheritdoc />
        public async ValueTask<AiRecognitionResult> HandleRecognizedDataAsync(
            string toolId,
            string? data,
            AiAgentContext context,
            CancellationToken cancellationToken = default)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<PartnerCreateCardAiAgentPlugin>(toolId, toolInfo.ID);
            ThrowIfNullOrWhiteSpace(data);

            if (JsonConvert.DeserializeObject<AiPartnerDocumentInfo>(data) is not { } aiPartnerDocumentInfo)
            {
                context.ValidationResult.AddError(this, AiConstants.FailedToDeserializeAiLocalization);
                return AiRecognitionResult.Error;
            }

            if (string.IsNullOrWhiteSpace(aiPartnerDocumentInfo.ShortName))
            {
                context.Response.Message = StringBuilderHelper.Acquire()
                    .Append(await LocalizeAsync("$Ai_Plugin_Message_NoPartnerShortName"))
                    .Append(' ')
                    .Append(await LocalizeAsync("$Ai_Plugin_Message_Clarify"))
                    .ToStringAndRelease();

                return AiRecognitionResult.Clarification;
            }

            var resultByName = await this.aiPartnerSearchService.SearchAsync(
                new AiPartner()
                {
                    ShortName = aiPartnerDocumentInfo.ShortName,
                    FullName = aiPartnerDocumentInfo.FullName,
                    Inn = aiPartnerDocumentInfo.Inn,
                    Kpp = aiPartnerDocumentInfo.Kpp,
                },
                context.ValidationResult,
                cancellationToken: cancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                return AiRecognitionResult.Error;
            }

            if (resultByName?.Count > 0)
            {
                context.Response.Message = await LocalizeAsync("$Ai_PartnerCreateCardAiAgentPlugin_DuplicatesFound");
                context.Response.Data = new AiTableData
                {
                    Table = new AiTable(
                        [
                            new(
                                AiTableColumnNames.CardID,
                                "ID",
                                AiTableColumnType.Guid,
                                AiTableColumnOpenType.Tab),
                            new(
                                nameof(AiPartnerInfo.ShortName),
                                "$Ai_PartnerCreateCardAiAgentPlugin_PartnerShortName",
                                AiTableColumnType.String),
                            new(
                                nameof(AiPartnerInfo.FullName),
                                "$Ai_PartnerCreateCardAiAgentPlugin_PartnerFullName",
                                AiTableColumnType.String),
                            new(
                                nameof(AiPartnerInfo.Inn),
                                "$Ai_PartnerCreateCardAiAgentPlugin_PartnerInn",
                                AiTableColumnType.String),
                            new(
                                nameof(AiPartnerInfo.Kpp),
                                "$Ai_PartnerCreateCardAiAgentPlugin_PartnerKpp",
                                AiTableColumnType.String)
                        ],
                        [.. resultByName.Select(static i =>
                        {
                            var dict = new Dictionary<string, object?>()
                            {
                                [AiTableColumnNames.CardID] = i.ID,
                                [nameof(AiPartnerInfo.ShortName)] = i.ShortName,
                            };

                            dict.SetIfNotEmpty(nameof(AiPartnerInfo.FullName), i.FullName);
                            dict.SetIfNotEmpty(nameof(AiPartnerInfo.Inn), i.Inn);
                            dict.SetIfNotEmpty(nameof(AiPartnerInfo.Kpp), i.Kpp);

                            return dict;
                        })])
                }.ToSerializedDictionary();

                return AiRecognitionResult.Clarification;
            }

            context.Response.Data = data;
            context.Response.Message = StringBuilderHelper.Acquire()
                .Append($"{await LocalizeAsync("$Ai_PartnerCreateCardAiAgentPlugin_CardWillBeCreated")} {AiPluginHelper.FormatPartnerName(aiPartnerDocumentInfo.ShortName, aiPartnerDocumentInfo.FullName)}")
                .ToStringAndRelease();
            context.Response.Buttons = [
                AiActions.Create,
                AiActions.Reject];

            return AiRecognitionResult.Confirmation;
        }

        /// <inheritdoc />
        public async ValueTask ProcessAsync(
            string toolId,
            AiAgentContext context,
            CancellationToken cancellationToken = default)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<PartnerCreateCardAiAgentPlugin>(toolId, toolInfo.ID);

            if (context.Request.Data is not string data
                || JsonConvert.DeserializeObject<AiPartnerDocumentInfo>(data) is not { } aiPartnerDocumentInfo)
            {
                context.ValidationResult.AddError(this, AiConstants.FailedToDeserializeAiLocalization);
                return;
            }

            var newResponse = await this.cardRepository.NewAsync(
                new CardNewRequest()
                {
                    CardTypeID = DefaultCardTypes.PartnerTypeID,
                },
                cancellationToken: cancellationToken);

            context.ValidationResult.Add(newResponse.ValidationResult);

            if (!newResponse.ValidationResult.IsSuccessful())
            {
                return;
            }

            var card = newResponse.Card;

            await this.FillCardAsync(
                aiPartnerDocumentInfo,
                card,
                cancellationToken);

            card.RemoveAllButChanged(card.StoreMode);

            var storeResponse = await this.cardRepository.StoreAsync(
                new CardStoreRequest()
                {
                    Card = card,
                },
                cancellationToken: cancellationToken);

            context.ValidationResult.Add(storeResponse.ValidationResult);

            if (!storeResponse.ValidationResult.IsSuccessful())
            {
                return;
            }

            context.Response.Message = $"{await LocalizeAsync("$Ai_PartnerCreateCardAiAgentPlugin_CardCreated")} "
                + this.mdLinkProvider.GetCardLink(
                    card.ID,
                    AiPluginHelper.FormatPartnerName(
                        aiPartnerDocumentInfo.ShortName,
                        aiPartnerDocumentInfo.FullName)!);
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
            var baseScheme = await JsonSchema.FromType<AiPartnerDocumentInfo>().EmbedReferencesAsync(cancellationToken);

            var toolInfo = new AiToolSettings()
            {
                ID = "partner_create_card",
                Name = "Создание нового контрагента",
                Description = "Позволяет создать в системе новую организацию.",
                Roles = [new(DefaultRoles.Registrators)],

                PluginName = nameof(PartnerCreateCardAiAgentPlugin),
                Prompts = AiPromptsHelper.GetPrompts(AiPromptTemplates.AiTool.Templates),
                Scheme = baseScheme.ExtractDescription(),
            };

            settings = new(toolInfo, baseScheme);
            this.baseSettings = settings;
            return settings;
        }

        /// <summary>
        /// Проверяет разрешено ли выполнение инструмента в соответствии с правами доступа.
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
                    CardTypeID = DefaultCardTypes.PartnerTypeID,
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
        /// Заполняет карточку контрагента.
        /// </summary>
        /// <param name="aiPartnerDocumentInfo"><inheritdoc cref="AiPartnerDocumentInfo" path="/summary"/></param>
        /// <param name="card">Карточка.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        private async ValueTask FillCardAsync(
            AiPartnerDocumentInfo aiPartnerDocumentInfo,
            Card card,
            CancellationToken cancellationToken = default)
        {
            card.ID = Guid.NewGuid();

            var sections = card.Sections;
            var partnersFields = sections[KrConstants.Partners.SectionName].Fields;
            partnersFields[KrConstants.Partners.Name] = aiPartnerDocumentInfo.ShortName;
            partnersFields.SetIfNotDefault(KrConstants.Partners.FullName, aiPartnerDocumentInfo.FullName);

            var typeInfo = await this.TryGetPartnerTypeByLocalizedNameAsync(
                aiPartnerDocumentInfo.Type,
                cancellationToken: cancellationToken);

            if (typeInfo.HasValue)
            {
                partnersFields[KrConstants.Partners.TypeID] = Int32Boxes.Box(typeInfo.Value.ID);
                partnersFields[KrConstants.Partners.TypeName] = typeInfo.Value.Name;
            }

            partnersFields.SetIfNotDefault(KrConstants.Partners.Head, aiPartnerDocumentInfo.Head);
            partnersFields.SetIfNotDefault(KrConstants.Partners.ContactPerson, aiPartnerDocumentInfo.ContactPerson);
            partnersFields.SetIfNotDefault(KrConstants.Partners.Phone, aiPartnerDocumentInfo.Phone);
            partnersFields.SetIfNotDefault(KrConstants.Partners.Email, aiPartnerDocumentInfo.Email);
            partnersFields.SetIfNotDefault(KrConstants.Partners.LegalAddress, aiPartnerDocumentInfo.LegalAddress);
            partnersFields.SetIfNotDefault(KrConstants.Partners.ContactAddress, aiPartnerDocumentInfo.ContactAddress);
            partnersFields.SetIfNotDefault(KrConstants.Partners.INN, aiPartnerDocumentInfo.Inn);
            partnersFields.SetIfNotDefault(KrConstants.Partners.KPP, aiPartnerDocumentInfo.Kpp);
            partnersFields.SetIfNotDefault(KrConstants.Partners.OGRN, aiPartnerDocumentInfo.Ogrn);
            partnersFields.SetIfNotDefault(KrConstants.Partners.OKPO, aiPartnerDocumentInfo.Okpo);
            partnersFields.SetIfNotDefault(KrConstants.Partners.OKVED, aiPartnerDocumentInfo.Okved);
            partnersFields.SetIfNotDefault(KrConstants.Partners.Comment, aiPartnerDocumentInfo.Comment);

            if (aiPartnerDocumentInfo.BankDetails is not null)
            {
                partnersFields.SetIfNotDefault(KrConstants.Partners.Bank, aiPartnerDocumentInfo.BankDetails.Name);
                partnersFields.SetIfNotDefault(KrConstants.Partners.SettlementAccount, aiPartnerDocumentInfo.BankDetails.SettlementAccount);
                partnersFields.SetIfNotDefault(KrConstants.Partners.BIK, aiPartnerDocumentInfo.BankDetails.Bik);
                partnersFields.SetIfNotDefault(KrConstants.Partners.CorrAccount, aiPartnerDocumentInfo.BankDetails.CorrAccount);
            }

            if (aiPartnerDocumentInfo.PartnerContacts is { Count: > 0 })
            {
                var rows = sections[KrConstants.PartnersContacts.SectionName].Rows;

                foreach (var partnerContact in aiPartnerDocumentInfo.PartnerContacts)
                {
                    var row = rows.Add();
                    row.RowID = Guid.NewGuid();

                    var fields = row.Fields;
                    fields[KrConstants.PartnersContacts.Name] = partnerContact.Name;
                    fields[KrConstants.PartnersContacts.Department] = partnerContact.Department;
                    fields[KrConstants.PartnersContacts.Position] = partnerContact.Position;
                    fields[KrConstants.PartnersContacts.Email] = partnerContact.Email;
                    fields[KrConstants.PartnersContacts.PhoneNumber] = partnerContact.Phone;

                    row.State = CardRowState.Inserted;
                }
            }
        }

        /// <summary>
        /// Возвращает информацию по типу контрагента, соответствующую его локализованному названию.
        /// </summary>
        /// <param name="localizedName">Локализованное название типа контрагента.</param>
        /// <param name="languageCode">Код языка.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Кортеж, содержащий идентификатор и название (ключ локализации) типа контрагента, или значение <see langword="null"/>, если его не удалось определить.</returns>
        private async ValueTask<(int ID, string Name)?> TryGetPartnerTypeByLocalizedNameAsync(
            string? localizedName,
            string languageCode = "ru",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(localizedName))
            {
                return null;
            }

            var enums = await this.cardMetadata.GetEnumerationsAsync(cancellationToken);

            if (enums.TryGetValue(KrConstants.PartnersTypes.SectionName, out var type))
            {
                var culture = CultureInfo.GetCultureInfo(languageCode);
                var record = type.Records.FirstOrDefault(i => Localize((string) i[KrConstants.PartnersTypes.Name]!, culture) == localizedName);

                if (record is null)
                {
                    return null;
                }

                return ((int) record[KrConstants.PartnersTypes.ID]!, (string) record[KrConstants.PartnersTypes.Name]!);
            }

            return null;
        }

        #endregion
    }
}
