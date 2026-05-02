#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonSchema;
using Tessa.Ai;
using Tessa.Ai.Agent;
using Tessa.Ai.Agent.Models;
using Tessa.Ai.AiViewSearch;
using Tessa.Ai.Files;
using Tessa.Ai.Models;
using Tessa.Ai.Prompts;
using Tessa.Cards;
using Tessa.Extensions.Default.Server.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared;
using Tessa.Extensions.Default.Shared.Workflow.KrPermissions;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Files;
using Tessa.Platform;
using Tessa.Platform.Json;
using Tessa.Platform.Runtime;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Properties.Resharper;
using Unity;
using ValidationResult = Tessa.Platform.Validation.ValidationResult;

namespace Tessa.Extensions.Default.Server.Ai.Plugins
{
    /// <summary>
    /// Плагин ИИ-агента для создания карточки входящего документа (файловый инструмент).
    /// </summary>
    /// <param name="aiEmployeeSearchService"><inheritdoc cref="IAiEmployeeSearchService" path="/summary"/></param>
    /// <param name="aiPartnerSearchService"><inheritdoc cref="IAiPartnerSearchService" path="/summary"/></param>
    /// <param name="signatureProvider"><inheritdoc cref="ISignatureProvider" path="/summary"/></param>
    /// <param name="cardRepositoryDef">Репозиторий для управления карточками без расширений (<see cref="CardRepositoryNames.Default"/>).</param>
    /// <param name="cardFileManager"><inheritdoc cref="ICardFileManager" path="/summary"/></param>
    /// <param name="aiCacheStorage"><inheritdoc cref="IAiCacheStorage" path="/summary"/></param>
    /// <param name="aiFileTokenProvider"><inheritdoc cref="IAiFileTokenProvider" path="/summary"/></param>
    /// <param name="krPermissionsManager"><inheritdoc cref="IKrPermissionsManager" path="/summary"/></param>
    public sealed class CreateIncomingAiAgentPlugin(
        IAiEmployeeSearchService aiEmployeeSearchService,
        IAiPartnerSearchService aiPartnerSearchService,
        ISignatureProvider signatureProvider,
        [Dependency(CardRepositoryNames.Default)] ICardRepository cardRepositoryDef,
        ICardFileManager cardFileManager,
        IAiCacheStorage aiCacheStorage,
        IAiFileTokenProvider aiFileTokenProvider,
        IKrPermissionsManager krPermissionsManager) : IAiAgentPlugin
    {
        #region Nested Types

        /// <summary>
        /// Информация о входящем документе заполняемая ИИ.
        /// </summary>
        [Display(Description = "Информация о входящем документе")]
        private sealed class AiIncomingDocumentInfo
        {
            [Display(
                Name = "Номер документа",
                Description = "Номер документа - номер текущего документа. Необходимо взять только номер документа, без расположенного до номера знака \"№\", или \"No\", или текста \"исх.\". Номер чаще всего после перечисленных символов. Не бери номер, который идет после слов \"на №\", \"в ответ на\" (обычно этот номер после основного и он не нужен)")]
            [UsedImplicitly]
            public string? Number { get; set; }

            [Display(
                Name = "Дата документа",
                Description = "Дата документа - дата текущего документа, обычно расположена рядом номером документа на одной строке. Не бери дату, которая расположена около второго номера, который начинается со слов \"на №\" или \"в ответ на\" и обычно расположен на следующей строке после основного номера документа")]
            [UsedImplicitly]
            [JsonConverter(typeof(ZeroIsoDateTimeConverter))]
            public DateTime? DocDate { get; set; }

            [Display(
                Name = "Тема документа",
                Description = "Тема документа - кратко сформулированная (без дат, номеров, названий организаций и прочих уточняющих деталей) информация по письму, отражающая ключевую суть документа. Сформулируй кратко в одно предложение")]
            [UsedImplicitly]
            public string? Subject { get; set; }

            [Display(
                Name = "Контрагент",
                Description = "Контрагент - отправитель, название организации, которая отправила данное письмо, обычно пишется в шапке документа (в самом начале документа). Не надо писать название организации - получателя, кому направлено данное письмо")]
            [UsedImplicitly]
            public AiPartner? Partner { get; set; }

            [Display(
                Name = "Адресат (получатель)",
                Description = "Адресат - ФИО сотрудника, кому направлено данное письмо. Может быть пустым. В конце документа обычно расположено ФИО того, кто подписал документ - это не то что надо, не бери фамилию из последней части документа. Возьми ФИО, кому адресовано письмо, из шапки документа (обычно в родительном падеже в виде И.О. Фамилия или Фамилия И.О.). Возьми только ФИО, без должности. В тексте может быть вежливое обращение к адресату в форме \"Уважаемый Иван Иванович\" или \"Добрый день, Иван Иванович\" или в аналогичных форматах. Если такое обращение есть, то возьми данные имя и отчество и добавь к полученной фамилии, чтобы получилось в виде Фамилия Имя Отчество. Если в тексте нет упоминания полного имени и отчества, не пытайся восстановить имя и отчество из инициалов, оставь только инициалы. Выведи отдельными параметрами ФИО.")]
            [UsedImplicitly]
            public AiEmployee? Recipient { get; set; }
        }

        /// <summary>
        /// Информация о входящем документе заполняемая инструментом.
        /// </summary>
        private sealed class IncomingDocumentInfo :
            StorageSerializable
        {
            #region Properties

            /// <summary>
            /// Номер документа.
            /// </summary>
            public string? Number { get; set; }

            /// <summary>
            /// Дата документа.
            /// </summary>
            public DateTime? DocDate { get; set; }

            /// <summary>
            /// Тема документа.
            /// </summary>
            public string? Subject { get; set; }

            /// <summary>
            /// Контрагент.
            /// </summary>
            public AiPartnerInfo? Partner { get; set; }

            /// <summary>
            /// Флаг, показывающий, что контрагент не найден.
            /// </summary>
            /// <remarks>
            /// При установленном флаге в свойстве <see cref="Partner"/> содержится информация по отсутствующему в справочнике контрагенту.
            /// </remarks>
            public bool PartnerNotFound { get; set; }

            /// <summary>
            /// Адресаты.
            /// </summary>
            public IList<AiEmployeeInfo>? Recipients { get; set; }

            #endregion

            #region Base Overrides

            /// <inheritdoc />
            protected override void SerializeCore(Dictionary<string, object?> storage)
            {
                storage.SetIfNotEmpty(nameof(this.Number), this.Number);
                storage.SetIfNotNull(nameof(this.DocDate), this.DocDate);
                storage.SetIfNotEmpty(nameof(this.Subject), this.Subject);
                storage.SetIfNotNull(nameof(this.Partner), this.Partner.ToSerializedDictionary());
                storage.SetIfNotDefault(nameof(this.PartnerNotFound), this.PartnerNotFound);

                if (this.Recipients is { Count: > 0 })
                {
                    storage[nameof(this.Recipients)] = ToObjectList(this.Recipients);
                }
            }

            /// <inheritdoc />
            protected override void DeserializeCore(Dictionary<string, object?> storage)
            {
                this.Number = storage.TryGet<string>(nameof(this.Number));
                this.DocDate = storage.TryGet<DateTime?>(nameof(this.DocDate));
                this.Subject = storage.TryGet<string>(nameof(this.Subject));
                this.Partner = storage.GetSerializedObject<AiPartnerInfo>(nameof(this.Partner));
                this.PartnerNotFound = storage.TryGet<bool>(nameof(this.PartnerNotFound));
                this.Recipients = GetObjectList<AiEmployeeInfo>(storage, nameof(this.Recipients));
            }

            #endregion
        }

        #endregion

        #region Static Fields And Constants

        private const string ToolPrompt =
            """
            Ты умный помощник, который может выполнять действия только при помощи настроенных инструментов.
            Для выполнения инструмента необходимо заполнить данные по предоставленной json схеме. Для каждого параметра json схемы указано его точное описание. Данные для json схемы надо получить из текста документа. Дополнительно учесть и запрос пользователя, если в запросе есть подходящие данные, то добавить их в итоговый json. ВАЖНО: данные для заполнения json схемы бери только из предоставленного текста, нельзя строить догадки, используй только то, что есть в тексте.
            Запрос пользователя:
            """;

        private AiCachedToolSettings? baseSettings;
        private AiCachedPrompts? cachedPrompts;

        #endregion

        #region Fields

        private readonly IAiEmployeeSearchService aiEmployeeSearchService = NotNullOrThrow(aiEmployeeSearchService);
        private readonly IAiPartnerSearchService aiPartnerSearchService = NotNullOrThrow(aiPartnerSearchService);
        private readonly ISignatureProvider signatureProvider = NotNullOrThrow(signatureProvider);
        private readonly ICardRepository cardRepositoryDef = NotNullOrThrow(cardRepositoryDef);
        private readonly ICardFileManager cardFileManager = NotNullOrThrow(cardFileManager);
        private readonly IAiCacheStorage aiCacheStorage = NotNullOrThrow(aiCacheStorage);
        private readonly IAiFileTokenProvider aiFileTokenProvider = NotNullOrThrow(aiFileTokenProvider);
        private readonly IKrPermissionsManager krPermissionsManager = NotNullOrThrow(krPermissionsManager);

        #endregion

        #region IAiAgentPlugin Members

        /// <inheritdoc />
        public async ValueTask<IReadOnlyList<AiToolInfo>> GetToolsAsync(CancellationToken cancellationToken) =>
            [(await this.GetBaseSettingsAsync(cancellationToken)).Settings];

        /// <inheritdoc />
        public async ValueTask<AiToolSettings> GetToolSettingsAsync(string toolId, CancellationToken cancellationToken)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<ContractInfoAiAgentPlugin>(toolId, toolInfo.ID);
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
                    // если нужно показывать инструмент если нет файла, убрать это условие здесь и проверять в GetInstructionsAsync, в случае чего писать сообщение пользователю, чтобы приложил файл
                    request.Context?.Type is null or AiContextType.None &&
                    LastMessageWithFileOrDefault(request.Messages) is not null
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
            AiHelper.ThrowIfInvalidTool<CreateIncomingAiAgentPlugin>(toolId, toolInfo.ID);

            // проверка разрешений, только для не админов
            if (!context.Session.User.IsAdministrator())
            {
                var validationResult = new ValidationResultBuilder();
                var result = await this.CheckPermissionsAsync(
                    validationResult,
                    cancellationToken);
                AiHelper.Logger.LogResult(validationResult);

                if (!result)
                {
                    context.Response.Message = await LocalizeAsync("$Ai_CreateIncomingAiAgentPlugin_NotPermissions");
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
            AiHelper.ThrowIfInvalidTool<CreateIncomingAiAgentPlugin>(toolId, toolInfo.ID);
            ThrowIfNullOrWhiteSpace(data);

            if (JsonConvert.DeserializeObject<AiIncomingDocumentInfo>(data) is not { } aiIncomingDocumentInfo)
            {
                context.ValidationResult.AddError(this, AiConstants.FailedToDeserializeAiLocalization);
                return AiRecognitionResult.Error;
            }

            AiPartnerInfo? partnerInfo = null;
            var partnerNotFound = false;

            if (aiIncomingDocumentInfo.Partner?.NameIsNullOrEmpty() == false)
            {
                partnerInfo = await this.SearchPartnerAsync(
                    aiIncomingDocumentInfo.Partner,
                    context.ValidationResult,
                    cancellationToken);

                if (!context.ValidationResult.IsSuccessful())
                {
                    return AiRecognitionResult.Error;
                }

                if (partnerInfo is null)
                {
                    partnerInfo = new AiPartnerInfo()
                    {
                        ID = Guid.Empty,
                        ShortName = aiIncomingDocumentInfo.Partner.ShortName
                            ?? aiIncomingDocumentInfo.Partner.ShortNameTrimmed ?? string.Empty,
                        FullName = aiIncomingDocumentInfo.Partner.FullName
                            ?? aiIncomingDocumentInfo.Partner.FullNameTrimmed,
                    };

                    partnerNotFound = true;
                }
            }

            var recipientsInfo = string.IsNullOrWhiteSpace(aiIncomingDocumentInfo.Recipient?.LastName)
                ? null
                : await this.aiEmployeeSearchService.SearchAsync(
                    aiIncomingDocumentInfo.Recipient,
                    context.ValidationResult,
                    cancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                return AiRecognitionResult.Error;
            }

            var incomingDocumentInfo = new IncomingDocumentInfo()
            {
                Number = aiIncomingDocumentInfo.Number?.Trim(),
                DocDate = aiIncomingDocumentInfo.DocDate.HasValue
                    ? DateTime.SpecifyKind(aiIncomingDocumentInfo.DocDate.Value.Date, DateTimeKind.Utc)
                    : null,
                Subject = aiIncomingDocumentInfo.Subject?.Trim(),
                Partner = partnerInfo,
                PartnerNotFound = partnerNotFound,
                Recipients = recipientsInfo,
            };

            context.Response.Data = incomingDocumentInfo.ToSerializedDictionary();
            context.Response.Buttons =
            [
                AiActions.Create,
                AiActions.Reject
            ];
            context.Response.Message = await LocalizeAsync("$Ai_CreateIncomingAiAgentPlugin_PropsTitle");
            context.Response.CustomView = true;

            return AiRecognitionResult.Confirmation;
        }

        /// <inheritdoc />
        public async ValueTask ProcessAsync(
            string toolId,
            AiAgentContext context,
            CancellationToken cancellationToken = default)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<CreateIncomingAiAgentPlugin>(toolId, toolInfo.ID);

            if (context.Request.Data is not Dictionary<string, object?> dataDict)
            {
                context.ValidationResult.AddError(this, AiConstants.FailedToDeserializeAiLocalization);
                return;
            }

            var incomingDocumentInfo = dataDict.FromSerializedDictionary<IncomingDocumentInfo>();

            var newRequest = new CardNewRequest();
            newRequest.CardTypeID = DefaultCardTypes.IncomingTypeID;
            newRequest.Info[KrConstants.Keys.DocTypeID] = DefaultDocTypes.IncomingDocTypeID;
            newRequest.Info[KrConstants.Keys.DocTypeTitle] = DefaultDocTypes.IncomingDocTypeTitle;

            var card = await CreateCardAsync(
                this.cardRepositoryDef,
                newRequest,
                context.ValidationResult,
                cancellationToken);

            if (card is null)
            {
                return;
            }

            var fillResult = await this.FillCardAsync(
                incomingDocumentInfo,
                card,
                context,
                cancellationToken);
            context.ValidationResult.Add(fillResult);
            if (!fillResult.IsSuccessful)
            {
                return;
            }

            var (preparedNewCard, preparedNewCardSignature) = AiHelper.SerializeCard(
                this.signatureProvider,
                card);

            context.Response.Action = new AiAutoAction(
                AiActions.CreateCardOnClientID,
                AiAutoActionType.CreateCard,
                new AiAutoActionData
                {
                    CardTypeID = DefaultCardTypes.IncomingTypeID,
                    DocTypeID = DefaultDocTypes.IncomingDocTypeID,
                    DocTypeTitle = DefaultDocTypes.IncomingDocTypeTitle,
                    PreparedNewCard = preparedNewCard,
                    PreparedNewCardSignature = preparedNewCardSignature,
                });

            var outMessageBuilder = StringBuilderHelper.Acquire();
            await AddSuccessMessageAsync(outMessageBuilder);

            context.Response.Message = outMessageBuilder.ToStringAndRelease();
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
            var baseScheme = await JsonSchema.FromType<AiIncomingDocumentInfo>().EmbedReferencesAsync(cancellationToken);

            var toolInfo = new AiToolSettings()
            {
                ID = "incoming_create_card",
                Name = "Создание нового входящего документа",
                Hint = "По приложенному файлу распознает основные реквизиты документа и создает карточку Входящего.",
                Roles = [new(DefaultRoles.Registrators)],
                RequireFile = true,

                PluginName = nameof(CreateIncomingAiAgentPlugin),
                Prompts = { [AiPromptTemplates.AiTool.RootPromptName] = ToolPrompt, },
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
                    CardTypeID = DefaultCardTypes.IncomingTypeID,
                    DocTypeID = DefaultDocTypes.IncomingDocTypeID,
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
        /// Выполняет поиск информацию о контрагенте.
        /// </summary>
        /// <param name="aiPartner"><inheritdoc cref="AiPartnerInfo" path="/summary"/></param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Информация контрагенте, в соответствии с заданным <paramref name="aiPartner"/>, или значение <see langword="null"/>, если он не найден.</returns>
        private async ValueTask<AiPartnerInfo?> SearchPartnerAsync(
            AiPartner aiPartner,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken)
        {
            var results = await this.aiPartnerSearchService.SearchAsync(
                aiPartner,
                validationResult,
                cancellationToken);

            return results is { Count: 1 } ? results[0] : null;
        }

        /// <summary>
        /// Добавляет в выходное сообщение информацию о созданном документе.
        /// </summary>
        /// <param name="sb">Билдер выходного сообщения.</param>
        /// <returns><inheritdoc cref="ValueTask" path="/summary"/></returns>
        private static async ValueTask AddSuccessMessageAsync(
            StringBuilder sb) =>
            sb.AppendLine(await LocalizeAsync("$Ai_CreateIncomingAiAgentPlugin_CardCreated"));

        /// <summary>
        /// Создаёт новую карточку.
        /// </summary>
        /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
        /// <param name="newRequest"><inheritdoc cref="CardNewRequest" path="/summary"/></param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Созданная карточка или значение <see langword="null"/>, если произошла ошибка.</returns>
        private static async Task<Card?> CreateCardAsync(
            ICardRepository cardRepository,
            CardNewRequest newRequest,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var newResponse = await cardRepository.NewAsync(
                newRequest,
                cancellationToken: cancellationToken);

            var validationResultResponse = newResponse.ValidationResult;
            validationResult.Add(validationResultResponse);

            return validationResultResponse.IsSuccessful()
                ? newResponse.Card
                : null;
        }

        /// <summary>
        /// Заполняет карточку входящего документа. Возвращает результат операции, который может быть неуспешен.
        /// </summary>
        /// <param name="incomingDocumentInfo"><inheritdoc cref="IncomingDocumentInfo" path="/summary"/></param>
        /// <param name="card">Карточка.</param>
        /// <param name="context"><inheritdoc cref="AiAgentContext" path="/summary"/></param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns><inheritdoc cref="Tessa.Platform.Validation.ValidationResult" path="/summary"/></returns>
        private async Task<ValidationResult> FillCardAsync(
            IncomingDocumentInfo incomingDocumentInfo,
            Card card,
            AiAgentContext context,
            CancellationToken cancellationToken = default)
        {
            card.ID = Guid.NewGuid();

            var sections = card.Sections;
            var dciFields = sections[KrConstants.DocumentCommonInfo.Name].Fields;

            if (!string.IsNullOrEmpty(incomingDocumentInfo.Subject))
            {
                dciFields[KrConstants.DocumentCommonInfo.Subject] = incomingDocumentInfo.Subject;
            }

            if (incomingDocumentInfo.DocDate.HasValue)
            {
                dciFields[KrConstants.DocumentCommonInfo.DocDate] = incomingDocumentInfo.DocDate;
            }

            if (!string.IsNullOrEmpty(incomingDocumentInfo.Number))
            {
                dciFields[KrConstants.DocumentCommonInfo.OutgoingNumber] = incomingDocumentInfo.Number;
            }

            if (incomingDocumentInfo.Partner is not null
                && !incomingDocumentInfo.PartnerNotFound)
            {
                dciFields[KrConstants.DocumentCommonInfo.PartnerID] = incomingDocumentInfo.Partner.ID;
                dciFields[KrConstants.DocumentCommonInfo.PartnerName] = incomingDocumentInfo.Partner.ShortName;
            }

            if (incomingDocumentInfo.Recipients is { Count: > 0 })
            {
                var rows = sections[KrConstants.Recipients.SectionName].Rows;

                foreach (var recipient in incomingDocumentInfo.Recipients)
                {
                    var row = rows.Add();
                    row.RowID = Guid.NewGuid();
                    row.State = CardRowState.Inserted;

                    var rowFields = row.Fields;
                    rowFields[KrConstants.Recipients.UserID] = recipient.ID;
                    rowFields[KrConstants.Recipients.UserName] = recipient.DisplayName;
                }
            }

            // Процесс создания карточки:
            // 1. Создание карточки-заготовки с использованием репозитория без расширений;
            // 2. Передача карточки-заготовки на клиент;
            // 3. Вызов создания карточки на клиенте. В запрос на создание передаётся карточка из п. 2.
            // 4. В расширении MergeWithBilletCardNewExtension информация из карточки-заготовки переносится в создаваемую карточку.

            // Для того, что бы в п. 4 не затереть данные в создаваемой карточке, из карточки-заготовки необходимо удалить не изменённые секции/поля с помощью Card.RemoveAllButChanged.
            // Нельзя указывать CardStoreMode.Inserted, т.к. в этом режиме не обрабатываются секции карточки.

            // RemoveAllButChanged удаляет информацию о версиях файлов.
            // Это приводит к созданию некорректного пакета карточки на клиенте, из-за этого добавление файлов выполняется после вызова RemoveAllButChanged().
            card.RemoveAllButChanged();

            var fileMessagePart = (AiFileMessagePart?) LastMessageWithFileOrDefault(context.Request.Messages)
                ?.Content
                !.Last(static i => i.Type == AiMessagePartType.File);

            if (fileMessagePart is null)
            {
                return ValidationResult.Empty;
            }

            if (string.IsNullOrEmpty(fileMessagePart.Token)
                || !await this.aiFileTokenProvider.ValidateTokenAsync(fileMessagePart.Token, fileMessagePart.FileID,
                    AiFileTokenPermission.Base, cancellationToken))
            {
                return ValidationResult.FromText(this, await AiFileHelper.GetInvalidTokenMessageAsync(fileMessagePart.FileID),
                    ValidationResultType.Error);
            }

            var aiCacheFileInfo = await this.aiCacheStorage.GetBaseFileAsync(
                fileMessagePart.FileID,
                cancellationToken);

            // Только создаём, но не сохраняем файл или карточку.
            // Объекты можно создать вручную, но создание через CardFileManager гарантирует правильность их заполнения при изменении логики.
            await using var container = await this.cardFileManager.CreateContainerAsync(
                card,
                cancellationToken: cancellationToken);

            // Создание пустого файла.
            var (_, result) = await container
                .FileContainer
                .BuildFile(aiCacheFileInfo.Name)
                .SetContent(Stream.Null)
                .AddWithNotificationAsync(cancellationToken: cancellationToken);

            context.ValidationResult.Add(result);

            if (!result.IsSuccessful)
            {
                return ValidationResult.Empty;
            }

            var file = card.Files.First();

            // Для версии размер не надо задавать. Он будет определён при сохранении.
            file.Size = aiCacheFileInfo.HasKnownSize ? aiCacheFileInfo.Content.Size : -1L;
            file.ExternalSource = new()
                { FileID = aiCacheFileInfo.ID, FileTypeName = AiFileHelper.BaseFileTypeName, StoreViaContentRequest = true };
            file.RequestInfo[AiFileHelper.TokenInfoKey] = fileMessagePart.Token;
            return ValidationResult.Empty;
        }

        /// <summary>
        /// Возвращает последнее сообщение пользователя с файлом.
        /// </summary>
        /// <param name="aiMessages">Сообщения.</param>
        /// <returns>Сообщение или <see langword="null"/>, если оно не найдено.</returns>
        private static AiMessage? LastMessageWithFileOrDefault(
            IEnumerable<AiMessage>? aiMessages) =>
            aiMessages?.LastOrDefault(static i => i.Role == AiRoles.User
                && (i.Content?.Any(static j => j.Type == AiMessagePartType.File) == true));

        #endregion
    }
}
