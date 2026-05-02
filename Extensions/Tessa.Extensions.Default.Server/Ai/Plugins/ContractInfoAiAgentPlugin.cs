#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Tessa.Ai.MarkdownLinks;
using Tessa.Ai.Models;
using Tessa.Ai.Plugins;
using Tessa.Ai.Prompts;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.IO;
using Tessa.Platform.Json;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Properties.Resharper;
using Tessa.Views;
using Tessa.Views.Metadata;
using Tessa.Views.Metadata.Criteria;
using DataType = LinqToDB.DataType;

namespace Tessa.Extensions.Default.Server.Ai.Plugins
{
    /// <summary>
    /// Плагин ИИ-агента для получения данных по договорам.
    /// </summary>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="viewService"><inheritdoc cref="ICurrentUserViewService" path="/summary"/></param>
    /// <param name="mdLinkProvider"><inheritdoc cref="IMdLinkProvider" path="/summary"/></param>
    /// <param name="aiEmployeeSearchService"><inheritdoc cref="IAiEmployeeSearchService" path="/summary"/></param>
    /// <param name="aiPartnerSearchService"><inheritdoc cref="IAiPartnerSearchService" path="/summary"/></param>
    /// <param name="aiDepartmentSearchService"><inheritdoc cref="IAiDepartmentSearchService" path="/summary"/></param>
    /// <param name="aiStateSearchService"><inheritdoc cref="IAiStateSearchService" path="/summary"/></param>
    /// <param name="cardMetadata"><inheritdoc cref="ICardMetadata" path="/summary"/></param>
    public sealed class ContractInfoAiAgentPlugin(
        IDbScope dbScope,
        ICurrentUserViewService viewService,
        IMdLinkProvider mdLinkProvider,
        IAiEmployeeSearchService aiEmployeeSearchService,
        IAiPartnerSearchService aiPartnerSearchService,
        IAiDepartmentSearchService aiDepartmentSearchService,
        IAiStateSearchService aiStateSearchService,
        ICardMetadata cardMetadata) : IAiAgentPlugin
    {
        #region Nested Types

        private class AiContractsResult
        {
            [Description("Список запрашиваемых пользователем договоров")]
            [UsedImplicitly]
            public List<AiContractInfo>? Contracts { get; set; }
        }

        [UsedImplicitly]
        private class AiContractInfo
        {
            [Display(Name = "Номер")]
            [UsedImplicitly]
            public string? Number { get; set; }

            [Display(Name = "Сумма")]
            [UsedImplicitly]
            public string? Amount { get; set; }

            [Display(Name = "Тип поиска по сумме")]
            [UsedImplicitly]
            public string? AmountSearchType { get; set; }

            [Display(Name = "Контрагент (название организации)")]
            [UsedImplicitly]
            public AiPartner? Partner { get; set; }

            [Display(Name="Автор")]
            [UsedImplicitly]
            public AiEmployee? Author { get; set; }

            [Display(Name="Состояние документа", Description="Состояние документа по бизнес-процессу (выбери из представленного списка, если нет подходящего по названию состояния, то не строй догадок, не ищи подходящие по теме, просто выведи пустое значение)")]
            [UsedImplicitly]
            public string? StateName { get; set; }

            [Display(Name = "Дата документа", Description = "Дата документа (заполни, если указана конкретная дата, а не период, иначе оставь пустым)")]
            [UsedImplicitly]
            [JsonConverter(typeof(ZeroIsoDateTimeConverter))]
            public DateTime? DocDate { get; set; }

            [Display(Name = "Дата начала периода", Description = "Дата документа, начало периода (заполни только если указан период, если указана просто дата, то оставь пустым. Если нет явного указания даты начала периода, но указан месяц, квартал или год, то началом периода будет первый день указанного месяца, квартала или года соответственно).")]
            [UsedImplicitly]
            [JsonConverter(typeof(ZeroIsoDateTimeConverter))]
            public DateTime? DocDateStart { get; set; }

            [Display(Name = "Дата конца периода", Description = "Дата документа, окончание периода (заполни только если указан период, если указана просто дата, то оставь пустым. Если нет явного указания даты окончания периода, но указан месяц, квартал или год, то окончанием периода будет последний день указанного месяца, квартала или года соответственно).")]
            [UsedImplicitly]
            [JsonConverter(typeof(ZeroIsoDateTimeConverter))]
            public DateTime? DocDateEnd { get; set; }

            [Display(Name = "Подразделение", Description = "Подразделение - наименование подразделения или отдела, приведённое к именительному падежу.")]
            [UsedImplicitly]
            public string? DepartmentName { get; set; }

            public bool IsNull() =>
                this.Number is null &&
                this.Amount is null &&
                this.AmountSearchType is null &&
                (this.Partner is null || this.Partner.IsNullOrEmpty()) &&
                (this.Author is null || this.Author.IsNullOrEmpty()) &&
                this.StateName is null &&
                this.DocDate is null &&
                this.DocDateStart is null &&
                this.DocDateEnd is null &&
                this.DepartmentName is null;
        }

        private enum AmountSearchTypes
        {
            Equals = 0,
            GreaterThan = 1,
            LowerThan = 2
        }

        private class ContractInfo : StorageSerializable
        {
            public string? Number { get; set; }

            public decimal? Amount { get; set; }

            public AmountSearchTypes? AmountSearchType { get; set; }

            public AiPartnerInfo? Partner { get; set; }

            public AiEmployeeInfo? Author { get; set; }

            public int? StateID { get; set; }

            public string? StateName { get; set; }

            public DateTime? DocDate { get; set; }

            public DateTime? DocDateStart { get; set; }

            public DateTime? DocDateEnd { get; set; }

            public Guid? DepartmentID { get; set; }

            public string? DepartmentName { get; set; }

            #region Storage serializable

            /// <inheritdoc />
            protected override void SerializeCore(Dictionary<string, object?> storage)
            {
                storage.SetIfNotDefault(nameof(this.Number), this.Number);
                storage[nameof(this.Amount)] = DecimalBoxes.Box(this.Amount);
                storage.SetIfNotDefault(nameof(this.AmountSearchType), this.AmountSearchType.ToString());
                storage[nameof(this.Partner)] = this.Partner.ToSerializedDictionary();
                storage[nameof(this.Author)] = this.Author.ToSerializedDictionary();
                storage[nameof(this.StateID)] = Int32Boxes.Box(this.StateID);
                storage.SetIfNotDefault(nameof(this.StateName), this.StateName);
                storage.SetIfNotDefault(nameof(this.DocDate), this.DocDate);
                storage.SetIfNotDefault(nameof(this.DocDateStart), this.DocDateStart);
                storage.SetIfNotDefault(nameof(this.DocDateEnd), this.DocDateEnd);
                storage[nameof(this.DepartmentID)] = GuidBoxes.Box(this.DepartmentID);
                storage.SetIfNotDefault(nameof(this.DepartmentName), this.DepartmentName);
            }

            /// <inheritdoc />
            protected override void DeserializeCore(Dictionary<string, object?> storage)
            {
                this.Number = storage.TryGet<string?>(nameof(this.Number));
                this.Amount = storage.TryGet<decimal?>(nameof(this.Amount));
                this.AmountSearchType = storage.ConvertEnum<AmountSearchTypes>(nameof(this.AmountSearchType));
                this.Partner = storage.GetSerializedObject<AiPartnerInfo>(nameof(this.Partner));
                this.Author = storage.GetSerializedObject<AiEmployeeInfo>(nameof(this.Author));
                this.StateID = storage.TryGet<int?>(nameof(this.StateID));
                this.StateName = storage.TryGet<string?>(nameof(this.StateName));
                this.DocDate = storage.TryGet<DateTime?>(nameof(this.DocDate));
                this.DocDateStart = storage.TryGet<DateTime?>(nameof(this.DocDateStart));
                this.DocDateEnd = storage.TryGet<DateTime?>(nameof(this.DocDateEnd));
                this.DepartmentID = storage.TryGet<Guid?>(nameof(this.DepartmentID));
                this.DepartmentName = storage.TryGet<string?>(nameof(this.DepartmentName));
            }

            #endregion

            public bool IsNull() =>
                this.Number is null &&
                this.Amount is null &&
                this.AmountSearchType is null &&
                this.Partner is null &&
                this.Author is null &&
                this.StateName is null &&
                this.DocDate is null &&
                this.DocDateStart is null &&
                this.DocDateEnd is null &&
                this.DepartmentName is null;
        }

        #endregion

        #region Constants

        private const int MaximumResults = 10;

        private const string ToolPrompt =
            """
            Ты умный помощник, который из запроса пользователя вычленяет необходимые параметры, которые в дальнейшем используются в информационной системе.
            Тебе не нужно искать договоры, только дай ответ, по каким параметрам пользователь хочет искать информацию.
            Указанный ниже Дополнительный контекст к запросу пользователя используй только в случае, если в запросе пользователя есть личные местоимения. Если пользователь говорит о себе, то добавь ФИО пользователя, если говорит о своем подразделении, то добавь подразделение пользователя.
            """;

        #endregion

        #region Fields

        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);
        private readonly ICurrentUserViewService viewService = NotNullOrThrow(viewService);
        private readonly IMdLinkProvider mdLinkProvider = NotNullOrThrow(mdLinkProvider);
        private readonly IAiEmployeeSearchService aiEmployeeSearchService = NotNullOrThrow(aiEmployeeSearchService);
        private readonly IAiPartnerSearchService aiPartnerSearchService = NotNullOrThrow(aiPartnerSearchService);
        private readonly IAiDepartmentSearchService aiDepartmentSearchService = NotNullOrThrow(aiDepartmentSearchService);
        private readonly IAiStateSearchService aiStateSearchService = NotNullOrThrow(aiStateSearchService);
        private readonly ICardMetadata cardMetadata = NotNullOrThrow(cardMetadata);

        private AiCachedToolSettings? baseSettings;
        private AiCachedPrompts? cachedPrompts;

        private static readonly Dictionary<string, AmountSearchTypes> amountSearchTypesDictionary =
            new()
            {
                { Localize("$Ai_ContractInfoPlugin_EqualsAmountSearchType"), AmountSearchTypes.Equals },
                { Localize("$Ai_ContractInfoPlugin_GreaterThanAmountSearchType"), AmountSearchTypes.GreaterThan },
                { Localize("$Ai_ContractInfoPlugin_LowerThanAmountSearchType"), AmountSearchTypes.LowerThan },
            };

        private static readonly List<AiTableColumn> resultTableColumns =
        [
            new(AiTableColumnNames.CardID, "ID", AiTableColumnType.Guid, AiTableColumnOpenType.Tab),
            new("DocNumber", "$Views_Registers_Number", AiTableColumnType.String),
            new("DocAmount", "$Views_Registers_Sum", AiTableColumnType.String),
            new("PartnerName", "$Views_Registers_Partner", AiTableColumnType.String),
            new("AuthorName", "$Views_Registers_Author", AiTableColumnType.String),
            new("KrState", "$Views_Registers_State", AiTableColumnType.String),
            new("DocDate", "$Views_Registers_DocDate", AiTableColumnType.String),
            new("Department", "$Views_Registers_Department", AiTableColumnType.String),
            new(AiTableColumnNames.CardContext, "Context", AiTableColumnType.Object)
        ];

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
                    request.Context?.Type is null or AiContextType.None
                        ? new AiToolApplicability { Available = true, Visible = true }
                        : new AiToolApplicability();
            }

            return new AiToolApplicability { Available = true };
        }

        /// <inheritdoc />
        public async ValueTask<AiInstructionsResult> GetInstructionsAsync(string toolId, AiAgentContext context, CancellationToken cancellationToken)
        {
            var (toolInfo, baseScheme) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<ContractInfoAiAgentPlugin>(toolId, toolInfo.ID);

            var settings = await context.ToolManager.TryGetToolSettingsAsync(toolId, cancellationToken);
            ThrowIfNull(settings);
            var actual = await AiPromptsHelper.CacheAsync(this.cachedPrompts, settings, baseScheme, cancellationToken);
            this.cachedPrompts = actual;
            var fullName = await AiHelper.GetUserFullNameAsync(this.dbScope, context.Session.User.ID, cancellationToken);

            var promptBuilder = new AiPromptBuilder()
                .WithTemplates(actual.Templates)
                .WithRoot(AiPromptTemplates.AiTool.RootPromptName)
                .WithCurrentUser(fullName!)
                .WithCurrentDateTime(DateTime.UtcNow + context.Session.ClientUtcOffset, false, context.Session.ClientUICulture);

            var department = await AiPluginHelper.GetCurrentUserDepartmentFromDbAsync(
                context.Session.User.ID,
                this.dbScope,
                cancellationToken);

            if (department is not null)
            {
                promptBuilder.WithDepartments(department);
            }

            // копия, т.к. дальше идёт модификация
            var scheme = await actual.Schema!.CopyAsync(cancellationToken);
            // Определений больше нет!
            var aiContractInfoScheme = scheme.Properties[nameof(AiContractsResult.Contracts)].Item!.ActualSchema;
            // допустимые типы поиска
            aiContractInfoScheme.Properties[nameof(AiContractInfo.AmountSearchType)].Enumeration.AddRange(amountSearchTypesDictionary.Keys.Select(x => Localize(x)));
            // допустимые состояния
            var enumerations = await this.cardMetadata.GetEnumerationsAsync(cancellationToken);
            var availableStates = enumerations["KrDocState"].Records.Select(static x => Localize((string) x["Name"]!)).ToList();
            if (availableStates is { Count: > 0 })
            {
                aiContractInfoScheme.Properties[nameof(AiContractInfo.StateName)].Enumeration.AddRange(availableStates);
            }

            return new(promptBuilder.Build(), scheme);
        }

        /// <inheritdoc />
        public async ValueTask<AiRecognitionResult> HandleRecognizedDataAsync(string toolId, string? data, AiAgentContext context, CancellationToken cancellationToken)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<DeputyInfoAiAgentPlugin>(toolId, toolInfo.ID);
            ThrowIfNullOrWhiteSpace(data);

            var result = JsonConvert.DeserializeObject<AiContractsResult>(data);
            if (result is null)
            {
                context.ValidationResult.AddError(AiConstants.FailedToDeserializeAiLocalization);
                return AiRecognitionResult.Error;
            }

            if (result.Contracts is not { Count: > 0 } ||
                result.Contracts.All(p => p.IsNull()))
            {
                // Если никакой параметр не определился, то пишем в чат:
                // Для поиска информации по договорам напишите его дату, номер, контрагента или автора.
                // todo Предыдущую переписку нет смысла отправлять.
                context.Response.Message = await LocalizeAsync("$Ai_ContractInfoPlugin_Message_ClarifyParameters");

                return AiRecognitionResult.Clarification;
            }

            var outMessageBuilder = StringBuilderHelper.Acquire();
            var contractInfos = new List<ContractInfo>();

            var (hasErrors, isNeedToClarify) =
                await this.ParseAiContractInfosAsync(
                    context,
                    outMessageBuilder,
                    result.Contracts,
                    contractInfos,
                    cancellationToken);

            if (!context.ValidationResult.IsSuccessful())
            {
                outMessageBuilder.Release();
                return AiRecognitionResult.Error;
            }

            if (!hasErrors)
            {
                if (contractInfos is { Count: 0 })
                {
                    context.Response.Message = outMessageBuilder
                        .AppendLine()
                        .AppendLine(await LocalizeAsync("$Ai_ContractInfoPlugin_Message_ClarifyParameters"))
                        .ToStringAndRelease();
                    return AiRecognitionResult.Clarification;
                }

                outMessageBuilder.Release();
                context.Response.Data = contractInfos;
                return AiRecognitionResult.Processing;
            }

            if (isNeedToClarify)
            {
                outMessageBuilder.AppendLine().AppendLine(await LocalizeAsync("$Ai_Plugin_Message_Clarify"));
            }

            // При наличии ошибок в запросе пользователя (или распознавании) данные не передаем.
            context.Response.Message = outMessageBuilder.ToStringAndRelease();

            // Если есть прямое указание на то, что требуется уточнение данных. Признак isNeedToClarify.
            // Либо не было распознано ни одной записи.
            return
                isNeedToClarify || contractInfos is { Count: 0 }
                    ? AiRecognitionResult.Clarification
                    : AiRecognitionResult.Error;
        }

        /// <inheritdoc />
        public async ValueTask ProcessAsync(string toolId, AiAgentContext context, CancellationToken cancellationToken)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<AddDeputyAiAgentPlugin>(toolId, toolInfo.ID);

            if (context.Response.Data is not List<ContractInfo> contractInfos)
            {
                context.ValidationResult.AddError(AiConstants.FailedToDeserializeAiLocalization);
                return;
            }

            var getKeyFunc = new Func<Dictionary<string, object?>, Guid>(static value => value.Get<Guid>(AiTableColumnNames.CardID));

            var viewResults = new Dictionary<Guid, Dictionary<string, object?>>();
            var singleSearchParameters =
                new Dictionary<string, List<RequestCriteria>>
                {
                    {"Number", [] },
                    {"Amount", [] },
                    {"Partner", [] },
                    {"Author", [] },
                    {"State", [] },
                    {"DocDate", [] },
                    {"Department", [] }
                };

            var addOpenViewLink = false;
            foreach (var contractInfo in contractInfos)
            {
                var requestParameters = PrepareRequestParameters(contractInfo);
                foreach (var parameter in requestParameters)
                {
                    singleSearchParameters[parameter.Name!].Add(parameter.CriteriaValues[0]);
                }

                // Не выполняем поиск, если у нас уже >= MaximumResults.
                // При этом параметры для общего поиска мы подготовили.
                if (viewResults is { Count: >= MaximumResults })
                {
                    addOpenViewLink = true;
                    continue;
                }

                var contractResult =
                    await this.SearchContractInViewAsync(requestParameters, context.AiFileSettings, context.ValidationResult, cancellationToken);
                if (contractResult is not null)
                {
                    foreach (var result in contractResult)
                    {
                        // Не добавляем новых записей, если у нас уже >= MaximumResults.
                        // Выходим из цикла.
                        if (viewResults is { Count: >= MaximumResults })
                        {
                            addOpenViewLink = true;
                            break;
                        }

                        viewResults.TryAdd(getKeyFunc(result), result);
                    }
                }
            }

            if (viewResults is not { Count: > 0 })
            {
                // Если в результате поиска не нашлось ни одной строки, то в чат пишем, что ничего не нашли.
                context.Response.Message = await LocalizeAsync("$Ai_ContractInfoPlugin_Message_ContractsRowsNotFound");
                return;
            }

            if (context.Request.Messages is not { Count: > 0 })
            {
                context.ValidationResult.AddError(this, "$Ai_AiAgent_Validation_NoRequestMessages");
                return;
            }

            var csvRequestString = await this.GetCsvRequestString(viewResults.Values.ToList(), context, cancellationToken);
            var resultsTableLabel = await LocalizeAsync("$Ai_ContractInfoPlugin_Message_ResultTable");

            var localAgentContext =
                context.Copy(i =>
                    i.Messages?.Last().Content!.Add(
                        new AiTextMessagePart
                        {
                            Text =
                                $"""
                                 Данные для обработки в формате csv:
                                 {csvRequestString}
                                 """
                        }));

            // получение актульных настроек для используемой модели
            var settings = await context.ToolManager.TryGetToolSettingsAsync(toolId, cancellationToken);
            ThrowIfNull(settings);
            var toolModel = settings.Model?.ID;
            if (string.IsNullOrEmpty(toolModel))
            {
                toolModel = null;
            }

            //todo надо будет вынести этот промпт в отдельный хэлпер, когда возникнет прицендент его повторного использования.
            var chatResponseMessages = await context.ArtiService.SendArtiChatRequestAsync(
                localAgentContext,
                $"""
                    Ты умный помощник, от которого требуется выполнить анализ и обработку данных.
                    От пользователя могут поступить два вида запроса на обработку:
                    1. Посчитать сумму, уточнить состояние, уточнить какие-либо данные и т.п.
                    2. Вывести список найденных данных.
                    По запросу пользователя выведи понятный пользователю результат.
                    Если это запрос вида 2 (т.е. вывести список), то не выводи эти данные, вместо этого напиши просто строку: "{resultsTableLabel}".
                    Если это запрос вида 1 (расчет, или вопрос по конкретной строке таблицы, или вопрос по набору данных),
                    то просто ответь на вопрос, не включая в ответ список найденных строк, не выводи таблицу и маркированный список.
                    Запрос пользователя:
                """,
                null,
                toolModel ?? context.AiSettings.Model?.ID,
                cancellationToken: cancellationToken
            );

            var outMessageBuilder = StringBuilderHelper.Acquire();
            if (chatResponseMessages is { Length: > 0 })
            {
                outMessageBuilder.AppendJoin("  ", chatResponseMessages).AppendLine().AppendLine();

                if (!string.IsNullOrWhiteSpace(resultsTableLabel)
                    && !chatResponseMessages.Any(p => p.Contains(resultsTableLabel, StringComparison.Ordinal)))
                {
                    outMessageBuilder.AppendLine(resultsTableLabel);
                }
            }

            context.Response.Message = outMessageBuilder.ToStringAndRelease();

            var aiTable = new AiTable(
                resultTableColumns,
                viewResults.Values.OrderBy(p => p.Get<string?>("DocNumber")).ToList());

            if (addOpenViewLink)
            {
                var linkRequestParameters =
                    new ReadOnlyCollection<RequestParameter>(
                        singleSearchParameters.Where(p => p.Value is { Count: > 0 }).Select(p =>
                        {
                            var parameter = new RequestParameter(p.Key);

                            p.Value.ForEach(criteria => parameter.CriteriaValues.Add(criteria));
                            return parameter;
                        }).ToList());
                var caption = await LocalizeAsync("$Ai_ContractInfoPlugin_Messages_View");
                var link = this.mdLinkProvider.GetViewLink("ContractsDocuments", caption!, linkRequestParameters);
                var tableMessage = await LocalizeFormatAsync("$Ai_ContractInfoPlugin_Message_NotAllResultsDisplayed", link);

                context.Response.CustomView = true;
                context.Response.Data = new AiTableWithTextData
                {
                    Table = aiTable,
                    Text = tableMessage
                }.ToSerializedDictionary();
            }
            else
            {
                context.Response.Data = new AiTableData
                {
                    Table = aiTable
                }.ToSerializedDictionary();
            }
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
            var baseScheme = JsonSchema.FromType<AiContractsResult>();
            // убираем INN и KPP, потому что для плагина не нужен поиск по ним
            var aiPartnerInfoScheme = baseScheme.Definitions[nameof(AiPartner)];
            aiPartnerInfoScheme.Properties.Remove(nameof(AiPartner.Inn));
            aiPartnerInfoScheme.Properties.Remove(nameof(AiPartner.Kpp));
            baseScheme = await baseScheme.EmbedReferencesAsync(cancellationToken);

            var toolInfo = new AiToolSettings
            {
                ID = "contract_info",
                Name = "Поиск информации по договорам",
                Hint =
                    """
                    С помощью инструмента можно найти информацию по доступным договорным документам: по состоянию, номеру, дате или контрагенту.
                    Примеры: договор №12-09/25; сумма всех договоров за январь; в каком состоянии договор услуг с Синтеллектом.
                    """,
                Roles = [new(DefaultRoles.Registrators)],

                PluginName = nameof(ContractInfoAiAgentPlugin),
                Prompts = AiPromptsHelper.GetPrompts(AiPromptTemplates.AiTool.Templates
                    .Append(new(AiPromptTemplates.AiTool.AutoPromptName, ToolPrompt))
                    .Append(new(AiPromptTemplates.AiTool.CustomPromptName,
                        """
                        ВАЖНО: если в запросе пользователя нет просьбы видов: мои договоры, моего подразделения, созданные мной,
                        где я автор и т.п. (т.е. с использованием личного местоимения),
                        то не добавляй подразделение и сотрудника из данных указанного дополнительного контекста в json схему.
                        Если в запросе пользователя есть упоминание личного местоимения,
                        то из указанного дополнительного контекста добавь запрашиваемые данные сотрудника и/или подразделения в указанную json схему.
                        """))),
                Scheme = baseScheme.ExtractDescription(),
            };

            settings = new(toolInfo, baseScheme);
            this.baseSettings = settings;
            return settings;
        }

        private async Task<(bool HasErrors, bool NeedToClarify)> ParseAiContractInfosAsync(
            AiAgentContext context,
            StringBuilder outMessageBuilder,
            List<AiContractInfo> aiContractInfos,
            List<ContractInfo> parsedInfos,
            CancellationToken cancellationToken = default)
        {
            // Сохраняем полученные справочные данные в локальные кэши (словари),
            // чтобы не искать одни и те же записи извне по несколько раз.
            // Локальный кэш найденных сотрудников.
            // Ключ - ФИО сотрудника по шаблону: "Фамилия;Имя;Отчество".
            // Значение - список справочных данных найденных сотрудников.
            var usersLocalCache = new Dictionary<string, IList<AiEmployeeInfo>?>();
            // Локальный кэш найденных контрагентов.
            // Ключ - название контрагента.
            // Значение - список справочных данных найденных контрагентов.
            var partnersLocalCache = new Dictionary<string, IList<AiPartnerInfo>?>();
            // Локальный кэш найденных подразделений.
            // Ключ - название подразделения.
            // Значение - список справочных данных найденных подразделений.
            var departmentsLocalCache = new Dictionary<string, IList<AiDepartmentInfo>?>();
            // Локальный кэш найденных состояний.
            // Ключ - название состояния.
            // Значение - список справочных данных найденного состояния.
            var statesLocalCache = new Dictionary<string, IList<AiStateInfo>?>();

            var hasErrors = false;
            var isNeedToClarify = false;

            foreach (var aiContractInfo in aiContractInfos)
            {
                if (aiContractInfo.IsNull())
                {
                    continue;
                }

                var contractInfo = new ContractInfo
                {
                    Number = aiContractInfo.Number
                };

                if (aiContractInfo.Partner is not null && !aiContractInfo.Partner.IsNullOrEmpty())
                {
                    contractInfo.Partner = await this.FindPartnerAsync(aiContractInfo.Partner, context, partnersLocalCache, outMessageBuilder, cancellationToken);
                    if (contractInfo.Partner is null)
                    {
                        hasErrors = true;
                        isNeedToClarify = true;
                    }
                }

                if (aiContractInfo.Author is not null && !aiContractInfo.Author.IsNullOrEmpty())
                {
                    contractInfo.Author = await this.FindAuthorAsync(aiContractInfo.Author, context, usersLocalCache, outMessageBuilder, cancellationToken);
                    if (contractInfo.Author is null)
                    {
                        hasErrors = true;
                        isNeedToClarify = true;
                    }
                }

                if (!string.IsNullOrWhiteSpace(aiContractInfo.StateName))
                {
                    //Не передаём ValidationResult из контекста, т.к. если состояние не нашлось по названию,
                    //мы игнорируем наименование состояния, полученное от ИИ.
                    var stateInfo = await this.FindStateInfoAsync(aiContractInfo.StateName, new ValidationResultBuilder(), statesLocalCache, cancellationToken);
                    if (stateInfo is not null)
                    {
                        contractInfo.StateID = stateInfo.ID;
                        contractInfo.StateName = stateInfo.Name;
                    }
                }

                if (!string.IsNullOrWhiteSpace(aiContractInfo.DepartmentName))
                {
                    var departmentInfo =
                        await this.FindDepartmentInfoAsync(aiContractInfo.DepartmentName, context.ValidationResult, departmentsLocalCache, outMessageBuilder, cancellationToken);
                    if (departmentInfo is not null)
                    {
                        contractInfo.DepartmentID = departmentInfo.ID;
                        contractInfo.DepartmentName = departmentInfo.Name;
                    }
                    else
                    {
                        hasErrors = true;
                        isNeedToClarify = true;
                    }
                }

                if (decimal.TryParse(aiContractInfo.Amount, out var amount))
                {
                    contractInfo.Amount = amount;
                    contractInfo.AmountSearchType = await this.ParseAmountSearchTypeAsync(aiContractInfo.AmountSearchType, outMessageBuilder, cancellationToken);
                    if (contractInfo.AmountSearchType is null)
                    {
                        hasErrors = true;
                        isNeedToClarify = true;
                    }
                }

                switch (aiContractInfo.DocDate)
                {
                    case not null when
                        aiContractInfo.DocDateStart is null &&
                        aiContractInfo.DocDateEnd is null:
                        contractInfo.DocDate = aiContractInfo.DocDate;
                        break;
                    case null when
                        aiContractInfo.DocDateStart is not null ||
                        aiContractInfo.DocDateEnd is not null:
                        contractInfo.DocDateStart = aiContractInfo.DocDateStart;
                        contractInfo.DocDateEnd = aiContractInfo.DocDateEnd;
                        break;
                    case null when
                        aiContractInfo.DocDateStart is null &&
                        aiContractInfo.DocDateEnd is null:
                        // Просто не указаны никакие даты.
                        break;
                    default:
                        outMessageBuilder
                            .Append(await LocalizeAsync("$Ai_ContractInfoPlugin_Message_InvalidDocDate")).AppendLine("  ");
                        hasErrors = true;
                        break;
                }

                if (!contractInfo.IsNull())
                {
                    parsedInfos.Add(contractInfo);
                }
            }

            return (hasErrors, isNeedToClarify);
        }

        private async Task<AiPartnerInfo?> FindPartnerAsync(
            AiPartner partner,
            AiAgentContext context,
            Dictionary<string, IList<AiPartnerInfo>?> partnersLocalCache,
            StringBuilder outMessageBuilder,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(partner.FullName) &&
                string.IsNullOrWhiteSpace(partner.ShortName) &&
                string.IsNullOrWhiteSpace(partner.FullNameTrimmed) &&
                string.IsNullOrWhiteSpace(partner.ShortNameTrimmed))
            {
                outMessageBuilder
                    .Append(await LocalizeAsync("$Ai_Plugin_Message_NoPartnerName")).AppendLine("  ");

                return null;
            }

            // Ищем данные по контрагенту.
            var partnerInfo =
                await this.aiPartnerSearchService.FindPartnerInfoOrAddErrorAsync(
                    partner,
                    context.ValidationResult,
                    outMessageBuilder,
                    partnersLocalCache,
                    cancellationToken);

            return partnerInfo;
        }

        private async Task<AiEmployeeInfo?> FindAuthorAsync(
            AiEmployee employee,
            AiAgentContext context,
            Dictionary<string, IList<AiEmployeeInfo>?> usersLocalCache,
            StringBuilder outMessageBuilder,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(employee.LastName))
            {
                outMessageBuilder
                    .Append(await LocalizeAsync("$Ai_Plugin_Message_NoAuthorLastName")).AppendLine("  ");

                return null;
            }

            // Ищем данные по автору.
            var employeeInfo =
                await this.aiEmployeeSearchService.FindUserInfoOrAddErrorAsync(
                    employee,
                    context.ValidationResult,
                    outMessageBuilder,
                    usersLocalCache,
                    cancellationToken);

            return employeeInfo;
        }

        private async Task<AiStateInfo?> FindStateInfoAsync(
            string aiStateName,
            IValidationResultBuilder validationResult,
            Dictionary<string, IList<AiStateInfo>?> statesLocalCache,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aiStateName))
            {
                // Никакой ошибки нет, просто не указано подразделение
                return null;
            }

            // Ищем данные состояния.
            var stateInfo =
                await this.aiStateSearchService.FindStateOrAddErrorAsync(
                    aiStateName,
                    validationResult,
                    statesLocalCache,
                    cancellationToken);

            return stateInfo;
        }

        private async Task<AiDepartmentInfo?> FindDepartmentInfoAsync(
            string aiDepartmentName,
            IValidationResultBuilder validationResult,
            Dictionary<string, IList<AiDepartmentInfo>?> departmentsLocalCache,
            StringBuilder outMessageBuilder,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aiDepartmentName))
            {
                // Никакой ошибки нет, просто не указано подразделение
                return null;
            }

            // Ищем данные подразделения.
            var departmentInfo =
                await this.aiDepartmentSearchService.FindDepartmentOrAddErrorAsync(
                    aiDepartmentName,
                    validationResult,
                    outMessageBuilder,
                    departmentsLocalCache,
                    cancellationToken);

            return departmentInfo;
        }

        private async Task<AmountSearchTypes?> ParseAmountSearchTypeAsync(
            string? aiAmountSearchType,
            StringBuilder outMessageBuilder,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(aiAmountSearchType))
            {
                // Будем считать, что если тип поиска по сумме вообще не определён, то он "равно"
                return AmountSearchTypes.Equals;
            }

            var type = aiAmountSearchType.ToLower();
            foreach (var (key, state) in amountSearchTypesDictionary)
            {
                var localized = await LocalizeAsync(key);
                if (localized?.ToLower() == type)
                {
                    return state;
                }
            }

            outMessageBuilder
                .Append(await LocalizeFormatAsync("$Ai_ContractInfoPlugin_InvalidAmountSearchType", aiAmountSearchType)).AppendLine("  ");

            return null;
        }

        private async Task<string> GetCsvRequestString(
            List<Dictionary<string, object?>> results,
            AiAgentContext context,
            CancellationToken cancellationToken = default)
        {
            MemoryStream? memoryStream = null;
            CsvWriter? csvWriter = null;
            StreamReader? streamReader = null;

            try
            {
                memoryStream = new MemoryStream();
                csvWriter = new CsvWriter(memoryStream, Encoding.UTF8, 65535, leaveOpen: true) { NewLine = "\n", Separator = ',' };

                for (var i = 0; i < resultTableColumns.Count; i++)
                {
                    var column = resultTableColumns[i];
                    if (i > 0)
                    {
                        await csvWriter.WriteSeparatorAsync();
                    }

                    await csvWriter.WriteAsync(await LocalizeAsync(column.Caption));
                }

                await csvWriter.WriteLineAsync();

                foreach (var contractData in results)
                {
                    await csvWriter.WriteAsync(contractData.Get<string?>("DocNumber"));
                    await csvWriter.WriteSeparatorAsync();
                    await csvWriter.WriteAsync(contractData.Get<string?>("DocAmount"));
                    await csvWriter.WriteSeparatorAsync();
                    await csvWriter.WriteAsync(contractData.Get<string?>("PartnerName"));
                    await csvWriter.WriteSeparatorAsync();
                    await csvWriter.WriteAsync(contractData.Get<string?>("AuthorName"));
                    await csvWriter.WriteSeparatorAsync();
                    await csvWriter.WriteAsync(await LocalizeAsync(contractData.Get<string?>("KrState")));
                    await csvWriter.WriteSeparatorAsync();
                    await csvWriter.WriteAsync(contractData.Get<string?>("DocDate"));
                    await csvWriter.WriteSeparatorAsync();
                    await csvWriter.WriteAsync(contractData.Get<string?>("Department"));
                    await csvWriter.WriteLineAsync();
                }

                await csvWriter.DisposeAsync();
                csvWriter = null;

                memoryStream.Position = 0L;

                streamReader = new StreamReader(memoryStream, Encoding.UTF8);
                return await streamReader.ReadToEndAsync(cancellationToken);
            }
            finally
            {
                streamReader?.Dispose();

                if (csvWriter is not null)
                {
                    await csvWriter.DisposeAsync();
                }

                if (memoryStream is not null)
                {
                    await memoryStream.DisposeAsync();
                }
            }
        }

        private static List<RequestParameter> PrepareRequestParameters(ContractInfo contractInfo)
        {
            ThrowIfNull(contractInfo);
            var resultParameters = new List<RequestParameter>();
            if (contractInfo.Number is not null)
            {
                var numberParameter =
                    new RequestParameter("Number")
                        .Add(ContainsCriteriaOperator.Instance, contractInfo.Number);
                resultParameters.Add(numberParameter);
            }

            if (contractInfo.Amount is not null)
            {
                OneValueCriteriaOperator criteria =
                    contractInfo.AmountSearchType switch
                    {
                        AmountSearchTypes.GreaterThan => GreaterThanCriteriaOperator.Instance,
                        AmountSearchTypes.LowerThan => LessThanCriteriaOperator.Instance,
                        _ => EqualsToCriteriaOperator.Instance
                    };

                var amountParameter =
                    new RequestParameter("Amount")
                        .Add(criteria, contractInfo.Amount);
                resultParameters.Add(amountParameter);
            }

            if (contractInfo.Partner is not null)
            {
                var partnerParameter =
                    new RequestParameter("Partner")
                        .Add(EqualsToCriteriaOperator.Instance, contractInfo.Partner.ID, contractInfo.Partner.ShortName);
                resultParameters.Add(partnerParameter);
            }

            if (contractInfo.Author is not null)
            {
                var authorParameter =
                    new RequestParameter("Author")
                        .Add(EqualsToCriteriaOperator.Instance, contractInfo.Author.ID, contractInfo.Author.Name);
                resultParameters.Add(authorParameter);
            }

            if (contractInfo.StateID is not null)
            {
                var stateParameter =
                    new RequestParameter("State")
                        .Add(EqualsToCriteriaOperator.Instance, contractInfo.StateID, contractInfo.StateName);
                resultParameters.Add(stateParameter);
            }

            RequestParameter? docDateParameter = null;
            if (contractInfo.DocDate is not null)
            {
                docDateParameter =
                    new RequestParameter("DocDate")
                        .Add(EqualsToCriteriaOperator.Instance, contractInfo.DocDate);
            }
            else if (contractInfo.DocDateStart is not null && contractInfo.DocDateEnd is not null)
            {
                docDateParameter =
                    new RequestParameter("DocDate")
                        .Add(BetweenCriteriaOperator.Instance, contractInfo.DocDateStart, null, contractInfo.DocDateEnd, null);
            }
            else if (contractInfo.DocDateStart is not null && contractInfo.DocDateEnd is null)
            {
                docDateParameter =
                    new RequestParameter("DocDate")
                        .Add(GreaterThanOrEqualCriteriaOperator.Instance, contractInfo.DocDateStart);
            }
            else if (contractInfo.DocDateStart is null && contractInfo.DocDateEnd is not null)
            {
                docDateParameter =
                    new RequestParameter("DocDate")
                        .Add(LessThanOrEqualCriteriaOperator.Instance, contractInfo.DocDateEnd);
            }

            if (docDateParameter is not null)
            {
                resultParameters.Add(docDateParameter);
            }

            if (contractInfo.DepartmentID is not null)
            {
                var departmentParameter =
                    new RequestParameter("Department")
                        .Add(EqualsToCriteriaOperator.Instance, contractInfo.DepartmentID, contractInfo.DepartmentName);
                resultParameters.Add(departmentParameter);
            }

            return resultParameters;
        }

        private async ValueTask<List<Dictionary<string, object?>>?> SearchContractInViewAsync(
            List<RequestParameter> requestParameters,
            AiFileSettings aiFileSettings,
            IValidationResultBuilder validationResult,
            CancellationToken cancellationToken = default)
        {
            var view = await this.viewService.GetByNameAsync("ContractsDocuments", cancellationToken);
            if (view is null)
            {
                validationResult.AddError(this, "$UI_Cards_Exception_ViewNotFound", "ContractsDocuments");
                return null;
            }

            var request = new TessaViewRequest("ContractsDocuments") { Parameters = requestParameters };
            var viewResult = await view.GetDataAsync(request, cancellationToken);

            if (viewResult.Rows is not { Count: > 0 })
            {
                return null;
            }

            var supportedExtensions = GetSupportedFileExtensions(aiFileSettings);
            var searchResults = new List<Dictionary<string, object?>>();

            foreach (var row in viewResult.Rows)
            {
                var rowStorage = viewResult.CreateRowStorage(row);
                if (rowStorage.TryConvertGuid("DocID") is not { } cardID)
                {
                    continue;
                }

                var fileVersion = supportedExtensions.Count > 0
                    ? await this.GetAiFileInfoAsync(cardID, supportedExtensions, cancellationToken)
                    : null;

                var docNumber = rowStorage.TryGet<string>("DocNumber");
                var tableContext =
                    new AiTableContext(cardID, DefaultCardTypes.ContractTypeID)
                    {
                        CardDigest = docNumber,
                        Files = fileVersion is not null ? [fileVersion] : null,
                        ToolID = fileVersion is not null ? "file_discussion" : null
                    };

                searchResults.Add(new Dictionary<string, object?>
                {
                    { AiTableColumnNames.CardID, cardID },
                    { "DocNumber", docNumber },
                    { "DocAmount", rowStorage.TryGet<string>("DocAmount") },
                    { "PartnerName", rowStorage.TryGet<string>("PartnerName") },
                    { "AuthorName", rowStorage.TryGet<string>("AuthorName") },
                    { "KrState", rowStorage.TryGet<string>("KrState") },
                    { "DocDate", FormatDate(rowStorage.TryConvertDateTime("DocDate")) },
                    { "Department", rowStorage.TryGet<string>("Department") },
                    { AiTableColumnNames.CardContext, tableContext.ToSerializedDictionary() }
                });
            }

            return searchResults;
        }

        private static HashSet<string> GetSupportedFileExtensions(AiFileSettings settings)
        {
            var extensionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (settings.FileOperationSettings is { } fileSettings)
            {
                foreach (var fileSetting in fileSettings)
                {
                    if (fileSetting.FileExtensions is { Length: not 0 } fileExtensions)
                    {
                        extensionSet.AddRange(fileExtensions.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                    }
                }
            }

            return extensionSet;
        }

        private async Task<AiFileInfo?> GetAiFileInfoAsync(
            Guid cardID,
            HashSet<string> supportedExtensions,
            CancellationToken cancellationToken)
        {
            await using var _ = this.dbScope.Create();
            var db = this.dbScope.Db;
            var builderFactory = await this.dbScope.GetBuilderFactoryAsync(cancellationToken);

            var query = builderFactory.Cached(this, nameof(this.GetAiFileInfoAsync), static b => b
                .Select().C("fv", "Name", "RowID", "Size")
                .From("Files", "f").NoLock()
                .InnerJoin("FileVersions", "fv").NoLock()
                .On().C("f", "VersionRowID").Equals().C("fv", "RowID")
                .Where().C("f", "ID").Equals().P(nameof(cardID))
                .And().C("f", "OriginalFileID").IsNull() // не учитываем копии; если оригинал удалён, то копии перестают быть копиями
                .OrderBy("fv", "Name").By("fv", "RowID") // Name для очевидного порядка в большинстве случаев, RowID - для стабильного порядка при одноимённых файлах
                .Build());

            await using var reader = await db
                .SetCommand(query, db.Parameter(nameof(cardID), cardID, DataType.Guid))
                .LogCommand()
                .ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                // возвращается первый подходящий по расширению файл
                var fileName = reader.GetString(0);
                var fileExtension = FileHelper.GetExtension(fileName).TrimStart('.');

                if (supportedExtensions.Contains(fileExtension))
                {
                    return new AiFileInfo(
                        id: reader.GetGuid(1),
                        name: fileName,
                        size: reader.GetInt64(2));
                }
            }

            return null;
        }

        #endregion
    }
}
