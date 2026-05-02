#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonSchema;
using NJsonSchema.Annotations;
using Tessa.Ai;
using Tessa.Ai.Agent;
using Tessa.Ai.Agent.Models;
using Tessa.Ai.AiViewSearch;
using Tessa.Ai.MarkdownLinks;
using Tessa.Ai.Models;
using Tessa.Ai.Prompts;
using Tessa.Cards;
using Tessa.Extensions.Default.Shared.Workflow.KrProcess;
using Tessa.Platform;
using Tessa.Platform.Collections;
using Tessa.Platform.Data;
using Tessa.Platform.Json;
using Tessa.Platform.Storage;
using Tessa.Platform.Validation;
using Tessa.Properties.Resharper;
using Tessa.Roles;
using Tessa.Roles.Deputies;
using Unity;

namespace Tessa.Extensions.Default.Server.Ai.Plugins
{
    /// <summary>
    /// Плагин ИИ-агента для работы с замещениями. Содержит один инструмент для добавления замещения сотрудников.
    /// </summary>
    /// <param name="aiViewSearchService"><inheritdoc cref="IAiEmployeeSearchService" path="/summary"/></param>
    /// <param name="krTypesCache"><inheritdoc cref="IKrTypesCache" path="/summary"/></param>
    /// <param name="deputiesSettingsProvider"><inheritdoc cref="IDeputiesManagementSettingsProvider" path="/summary"/></param>
    /// <param name="mdLinkProvider"><inheritdoc cref="IMdLinkProvider" path="/summary"/></param>
    /// <param name="dbScope"><inheritdoc cref="IDbScope" path="/summary"/></param>
    /// <param name="cardRepository"><inheritdoc cref="ICardRepository" path="/summary"/></param>
    /// <param name="transactionStrategy"><inheritdoc cref="ITransactionStrategy" path="/summary"/></param>
    public class AddDeputyAiAgentPlugin(
        IAiEmployeeSearchService aiViewSearchService,
        IKrTypesCache krTypesCache,
        IDeputiesManagementSettingsProvider deputiesSettingsProvider,
        IMdLinkProvider mdLinkProvider,
        IDbScope dbScope,
        [Dependency(CardRepositoryNames.Extended)] ICardRepository cardRepository,
        ITransactionStrategy transactionStrategy) : IAiAgentPlugin
    {
        #region Nested Types

        /// <summary>
        /// Тип документа или карточки
        /// </summary>
        private sealed class AiDocType : StorageSerializable
        {
            #region Constructors

            /// <summary>
            /// Создает экземпляр <see cref="AiDocType"/>.
            /// </summary>
            public AiDocType()
            {
            }

            /// <summary>
            /// Создает экземпляр <see cref="AiDocType"/>.
            /// </summary>
            /// <param name="id"><inheritdoc cref="ID"/></param>
            /// <param name="caption"><inheritdoc cref="Caption"/></param>
            public AiDocType(Guid id, string caption)
            {
                this.ID = id;
                this.Caption = caption;
            }

            #endregion

            #region Properties

            /// <summary>
            /// Идентификатор типа.
            /// </summary>
            public Guid ID { get; set; }

            /// <summary>
            /// Заголовок типа.
            /// </summary>
            public string Caption { get; set; } = null!;

            #endregion

            #region Storage serializable

            /// <inheritdoc />
            protected override void SerializeCore(Dictionary<string, object?> storage)
            {
                storage[nameof(this.ID)] = GuidBoxes.Box(this.ID);
                storage[nameof(this.Caption)] = this.Caption;
            }

            /// <inheritdoc />
            protected override void DeserializeCore(Dictionary<string, object?> storage)
            {
                this.ID = storage.TryGet(nameof(this.ID), Guid.Empty);
                this.Caption = storage.TryGet<string>(nameof(this.Caption)) ?? string.Empty;
            }

            #endregion
        }

        private class DeputiesAiResult
        {
            [Description("Список запрашиваемых пользователем замещений.")]
            [UsedImplicitly]
            public List<AiSubstitutionInfo> SubstitutionsInfos { get; set; } = null!;
        }

        [UsedImplicitly]
        [Description(
            "Информация о запрашиваемом замещении. Замещение может быть на полную работу от имени замещаемого сотрудника, либо по конкретным типам документам, если явно указано в запросе пользователя."
        )]
        private class AiSubstitutionInfo
        {
            [UsedImplicitly]
            [Display(Name = "ФИО заместителя, т.е. кто должен замещать.")]
            public AiEmployee? Deputy { get; set; }

            [UsedImplicitly]
            [Display(Name = "ФИО замещаемого, т.е. кого должен замещать.")]
            public AiEmployee? Employee { get; set; }

            [UsedImplicitly]
            [Display(Name = "Типы документов", Description = "Типы документов (выбери из представленного списка, если нет подходящего по названию типа документа, то не строй догадок, не ищи подходящие по теме, просто выведи пустое значение).")]
            public List<string>? DocumentTypes { get; set; }

            [UsedImplicitly]
            [Display(Name = "Дата начала замещения", Description = "Дата начала замещения (если нет чёткого указания дат, но указана продолжительность, значит надо использовать текущую дату как дату начала, если нет никаких упоминаний о сроках и датах, то дату начала и окончания оставь пустыми)."
            )]
            [JsonConverter(typeof(ZeroIsoDateTimeConverter))]
            public DateTime? Start { get; set; }

            [UsedImplicitly]
            [Display(Name = "Дата окончания замещения.")]
            [JsonConverter(typeof(ZeroIsoDateTimeConverter))]
            public DateTime? End { get; set; }

            /// <summary>
            /// Признак, что замещение является постоянным.
            /// </summary>
            [JsonSchemaIgnore]
            public bool IsPermanent => this.Start is null && this.End is null;
        }

        private class SubstitutionInfo : StorageSerializable
        {
            /// <summary>
            /// Информация из TESSA о замещаемом.
            /// </summary>
            public AiEmployeeInfo? EmployeeInfo { get; set; }

            /// <summary>
            /// Информация из TESSA о заместителе.
            /// </summary>
            public AiEmployeeInfo? DeputyUserInfo { get; set; }

            /// <summary>
            /// Информация из TESSA о типах документах.
            /// </summary>
            public List<AiDocType>? DocumentTypesInfos { get; set; }

            /// <summary>
            /// Дата начала замещения.
            /// Если замещение постоянное, то <c>null</c> (но при этом тогда и дата окончания должна быть <c>null</c>).
            /// </summary>
            public DateTime? Start { get; set; }

            /// <summary>
            /// Дата окончания замещения.
            /// Если замещение постоянное, то <c>null</c> (но при этом тогда и дата начала должна быть <c>null</c>).
            /// </summary>
            public DateTime? End { get; set; }

            // Свойства ниже не используется при взаимодействии с arti и заполняются уже после распознавания свойств выше.
            // Поэтому игнорируем их при генерации схемы.

            /// <summary>
            /// Признак, что замещение является постоянным.
            /// </summary>
            public bool IsPermanent => this.Start is null && this.End is null;

            #region Storage serializable

            /// <inheritdoc />
            protected override void SerializeCore(Dictionary<string, object?> storage)
            {
                storage[nameof(this.DeputyUserInfo)] = this.DeputyUserInfo.ToSerializedDictionary();
                storage[nameof(this.EmployeeInfo)] = this.EmployeeInfo.ToSerializedDictionary();
                storage[nameof(this.DocumentTypesInfos)] = ToObjectList(this.DocumentTypesInfos);
                storage[nameof(this.Start)] = this.Start;
                storage[nameof(this.End)] = this.End;
            }

            /// <inheritdoc />
            protected override void DeserializeCore(Dictionary<string, object?> storage)
            {
                this.EmployeeInfo = storage.GetSerializedObject<AiEmployeeInfo>(nameof(this.EmployeeInfo));
                this.DeputyUserInfo = storage.GetSerializedObject<AiEmployeeInfo>(nameof(this.DeputyUserInfo));
                this.DocumentTypesInfos = GetObjectList<AiDocType>(storage, nameof(this.DocumentTypesInfos));
                this.Start = storage.TryGet<DateTime?>(nameof(this.Start));
                this.End = storage.TryGet<DateTime?>(nameof(this.End));
            }

            #endregion
        }

        #endregion

        #region Private fields and consts

        private readonly IAiEmployeeSearchService aiViewSearchService = NotNullOrThrow(aiViewSearchService);
        private readonly ICardRepository cardRepository = NotNullOrThrow(cardRepository);
        private readonly IDbScope dbScope = NotNullOrThrow(dbScope);
        private readonly IKrTypesCache krTypesCache = NotNullOrThrow(krTypesCache);
        private readonly IDeputiesManagementSettingsProvider deputiesSettingsProvider = NotNullOrThrow(deputiesSettingsProvider);
        private readonly ITransactionStrategy transactionStrategy = NotNullOrThrow(transactionStrategy);
        private readonly IMdLinkProvider mdLinkProvider = NotNullOrThrow(mdLinkProvider);

        private const string ToolPrompt =
            """
            Ты умный помощник, который из запроса пользователя вычленяет необходимые параметры, которые в дальнейшем используются в информационной системе. 
            Необходимо заполнить только указанные в запросе пользователя параметры (при их наличии), не строя дополнительных догадок.
            """;

        private const string AllRolesAndDepartmentCaption = "$Views_AvailableDeputyRoles_AllRolesAndDepartments_Sql";

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

            // Инструмент глобальный, то есть не содержит контекста (либо явное указание на это, либо его фактическое отсутствие)
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
            AiHelper.ThrowIfInvalidTool<AddDeputyAiAgentPlugin>(toolId, toolInfo.ID);

            var settings = await context.ToolManager.TryGetToolSettingsAsync(toolId, cancellationToken);
            ThrowIfNull(settings);
            var actual = await AiPromptsHelper.CacheAsync(this.cachedPrompts, settings, baseScheme, cancellationToken);
            this.cachedPrompts = actual;
            var fullName = await AiHelper.GetUserFullNameAsync(this.dbScope, context.Session.User.ID, cancellationToken);
            
            var prompt = new AiPromptBuilder()
                .WithTemplates(actual.Templates)
                .WithRoot(AiPromptTemplates.AiTool.RootPromptName)
                .WithCurrentDateTime(DateTime.UtcNow + context.Session.ClientUtcOffset, false, context.Session.ClientUICulture)
                .WithCurrentUser(fullName!)
                .Build();

            // копия, т.к.дальше идёт модификация
            var scheme = await actual.Schema!.CopyAsync(cancellationToken);
            var aiSubstitutionInfoScheme = scheme.Properties[nameof(DeputiesAiResult.SubstitutionsInfos)].Item!.ActualSchema;
            // допустимые типы документов
            var availableTypes = await this.krTypesCache.GetTypesAsync(cancellationToken);
            if (availableTypes is { Count: > 0 })
            {
                var availableTypesNames = availableTypes.Select(static x => Localize(x.Caption)).ToList();
                aiSubstitutionInfoScheme.Properties[nameof(AiSubstitutionInfo.DocumentTypes)].Enumeration.AddRange(availableTypesNames);
            }

            return new AiInstructionsResult(prompt, scheme);
        }

        /// <inheritdoc />
        public async ValueTask<AiRecognitionResult> HandleRecognizedDataAsync(string toolId, string? data, AiAgentContext context, CancellationToken cancellationToken)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<AddDeputyAiAgentPlugin>(toolId, toolInfo.ID);
            ThrowIfNullOrWhiteSpace(data);

            if (JsonConvert.DeserializeObject<DeputiesAiResult>(data)?.SubstitutionsInfos is not { Count: > 0 } aiSubstitutions)
            {
                context.ValidationResult.AddError(AiConstants.FailedToDeserializeAiLocalization);
                return AiRecognitionResult.Error;
            }

            var outMessageBuilder = StringBuilderHelper.Acquire();
            var insufficientData = false;
            var substitutionsInfos = new List<SubstitutionInfo>();

            // Сохраняем полученные справочные данные (пользователи и типы документов) в локальные кэши (словари),
            // чтобы не искать одни и те же записи извне по несколько раз.

            // Локальный кэш найденных сотрудников.
            // Ключ - ФИО сотрудника по шаблону: "Фамилия;Имя;Отчество".
            // Значение - список справочных данных найденных сотрудников.
            var usersLocalCache = new Dictionary<string, IList<AiEmployeeInfo>?>();

            // Локальный кэш типов документов.
            // Ключ - название, распознанное ИИ в запросе пользователя.
            // Значение - типы документа, найденные по данному названию.
            var documentTypesLocalCache = new Dictionary<string, List<AiDocType>?>();

            var currentUserInfo = new AiEmployeeInfo
            {
                ID = context.Session.User.ID,
                Name = context.Session.User.Name,
                DisplayName = context.Session.User.Name
            };

            foreach (var aiSubstitution in aiSubstitutions)
            {
                if (!await CheckAiSubstitutionAsync(aiSubstitution, context, outMessageBuilder))
                {
                    insufficientData = true;
                }

                // Ищем данные по замещаемому сотруднику.
                // Если он не заполнен ИИ - считаем, что это текущий пользователь.
                var employeeUserInfo = aiSubstitution.Employee is null || aiSubstitution.Employee.IsEmpty()
                    ? currentUserInfo
                    : string.IsNullOrWhiteSpace(aiSubstitution.Employee.LastName)
                        ? null
                        : await this.aiViewSearchService.FindUserInfoOrAddErrorAsync(
                            aiSubstitution.Employee,
                            context.ValidationResult,
                            outMessageBuilder,
                            usersLocalCache,
                            cancellationToken
                        );
                if (employeeUserInfo is null)
                {
                    insufficientData = true;
                }

                if (aiSubstitution is { IsPermanent: false, Start: null })
                {
                    aiSubstitution.Start = DateTime.UtcNow;
                }

                // Ищем данные по заместителю
                var deputyUserInfo = aiSubstitution.Deputy is null || string.IsNullOrWhiteSpace(aiSubstitution.Deputy.LastName)
                    ? null
                    : await this.aiViewSearchService.FindUserInfoOrAddErrorAsync(
                        aiSubstitution.Deputy,
                        context.ValidationResult,
                        outMessageBuilder,
                        usersLocalCache,
                        cancellationToken
                    );
                if (deputyUserInfo is null)
                {
                    insufficientData = true;
                }

                if (employeeUserInfo is not null && deputyUserInfo is not null && employeeUserInfo.ID == deputyUserInfo.ID)
                {
                    outMessageBuilder.Append(await LocalizeFormatAsync("$Ai_AddDeputyPlugin_SameEmployeeAndDeputy", employeeUserInfo.DisplayName)).AppendLine("  ");
                    insufficientData = true;
                }

                List<AiDocType>? substitutionDocTypes = null;
                // Ищем данные по типам документов
                if (aiSubstitution.DocumentTypes is { Count: > 0 })
                {
                    substitutionDocTypes = [];
                    foreach (var documentType in aiSubstitution.DocumentTypes)
                    {
                        var documentTypes = await this.GetTypesFromDbOrCacheAsync(documentType, context.Session.ClientUICulture, documentTypesLocalCache, cancellationToken);
                        if (documentTypes is not { Count: > 0 })
                        {
                            outMessageBuilder.Append(await LocalizeFormatAsync("$Ai_AddDeputyPlugin_InvalidDocumentType", documentType)).AppendLine("  ");
                            insufficientData = true;
                            continue;
                        }

                        substitutionDocTypes.AddRange(documentTypes);
                    }
                }

                if (!insufficientData)
                {
                    substitutionsInfos.Add(
                        new SubstitutionInfo
                        {
                            EmployeeInfo = employeeUserInfo,
                            DeputyUserInfo = deputyUserInfo,
                            Start = aiSubstitution.Start?.ToUniversalTime(),
                            End = aiSubstitution.End?.ToUniversalTime(),
                            DocumentTypesInfos = substitutionDocTypes
                        }
                    );
                }
            }

            if (!context.ValidationResult.IsSuccessful())
            {
                outMessageBuilder.Release();
                return AiRecognitionResult.Error;
            }

            if (insufficientData)
            {
                outMessageBuilder.Append(await LocalizeAsync("$Ai_Plugin_Message_Clarify")).AppendLine("  ");
            }
            else
            {
                // Данные передаем, только если нет ошибок в запросе пользователя
                await this.AddConfirmMessageToBuilderAsync(substitutionsInfos, outMessageBuilder);
                context.Response.Data = StorageSerializable.ToObjectList(substitutionsInfos);
            }

            context.Response.Message = outMessageBuilder.ToStringAndRelease();

            return insufficientData ? AiRecognitionResult.Clarification : AiRecognitionResult.Confirmation;
        }

        /// <inheritdoc />
        public async ValueTask ProcessAsync(string toolId, AiAgentContext context, CancellationToken cancellationToken)
        {
            var (toolInfo, _) = await this.GetBaseSettingsAsync(cancellationToken);
            AiHelper.ThrowIfInvalidTool<AddDeputyAiAgentPlugin>(toolId, toolInfo.ID);

            if (context.Request.Data is not List<object> objList)
            {
                context.ValidationResult.AddError(AiConstants.InvalidAiContextDataLocalization);
                return;
            }

            var substitutions = TryDeserializeSubstitutions(objList);
            if (substitutions is not { Count: > 0 })
            {
                context.ValidationResult.AddError(AiConstants.FailedToDeserializeAiLocalization);
                return;
            }

            if (!ValidateSubstitutions(substitutions, context.ValidationResult))
            {
                return;
            }

            // Проверяем используется ли старая система замещений
            var oldDeputySystem = (await this.deputiesSettingsProvider.GetSettingsAsync(cancellationToken)).UseDeputyRoleSeparation;

            // Группируем замещения по сотрудникам, чтобы для каждого сотрудника делать все замещения одним сохранением карточки.
            var substitutionsByEmployee = substitutions.GroupBy(x => x.EmployeeInfo!.ID);

            await this.transactionStrategy.ExecuteInTransactionAsync(
                context.ValidationResult,
                async _ =>
                {
                    foreach (var substitution in substitutionsByEmployee)
                    {
                        await this.AddDeputiesAsync(substitution.Key, context.Session.ClientUtcOffset, substitution, context.ValidationResult, oldDeputySystem, cancellationToken);
                    }
                },
                cancellationToken
            );

            if (!context.ValidationResult.IsSuccessful())
            {
                return;
            }

            context.Response.Message = await this.BuildSuccessMessageAsync(substitutions, cancellationToken);
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
            var baseScheme = await JsonSchema.FromType<DeputiesAiResult>().EmbedReferencesAsync(cancellationToken);

            var toolInfo = new AiToolSettings()
            {
                ID = "deputy_add",
                Name = "Настройка замещений",
                Description = "Добавление заместителя для сотрудника.",
                Hint = "Позволяет добавить заместителя как себе, так и тем сотрудникам, к которым настроен доступ для настройки замещений.",

                PluginName = nameof(AddDeputyAiAgentPlugin),
                Prompts = AiPromptsHelper.GetPrompts(AiPromptTemplates.AiTool.Templates
                    .Append(new(AiPromptTemplates.AiTool.AutoPromptName, ToolPrompt))),
                Scheme = baseScheme.ExtractDescription(),
            };

            settings = new(toolInfo, baseScheme);
            this.baseSettings = settings;
            return settings;
        }

        /// <summary>
        /// Проверить распознанные данные по замещениям от ИИ и вывести в сообщения текст ошибки, если данных не хватает или они не валидны.
        /// </summary>
        /// <param name="substitution">Распознанное замещение.</param>
        /// <param name="context"><inheritdoc cref="AiAgentContext" path="/summary"/></param>
        /// <param name="outMessageBuilder">Билдер выходного сообщения</param>
        /// <returns><c>true</c> если данные корректны, в противном случае <c>false</c></returns>
        private static async Task<bool> CheckAiSubstitutionAsync(
            AiSubstitutionInfo substitution,
            AiAgentContext context,
            StringBuilder outMessageBuilder)
        {
            var hasErrors = false;
            if (string.IsNullOrWhiteSpace(substitution.Deputy?.LastName))
            {
                outMessageBuilder.Append(await LocalizeAsync("$Ai_Plugin_Message_NoDeputyLastName")).AppendLine("  ");
                hasErrors = true;
            }

            if (substitution.Employee is not null && string.IsNullOrWhiteSpace(substitution.Employee.LastName))
            {
                outMessageBuilder.Append(await LocalizeAsync("$Ai_Plugin_Message_ReplacementEmployeeHasNoLastName")).AppendLine("  ");
                hasErrors = true;
            }

            if (!substitution.IsPermanent)
            {
                if (substitution.End is null)
                {
                    outMessageBuilder.Append(await LocalizeAsync("$Ai_AddDeputyPlugin_Validation_NoEndDate")).AppendLine("  ");
                    hasErrors = true;
                }

                if (substitution.End <= (substitution.Start ?? DateTime.UtcNow))
                {
                    outMessageBuilder.Append(await LocalizeAsync("$Ai_AddDeputyPlugin_Validation_InvalidEndDate")).AppendLine("  ");
                    hasErrors = true;
                }
            }

            return !hasErrors;
        }

        private async Task<List<AiDocType>?> GetTypesFromDbOrCacheAsync(
            string documentType,
            CultureInfo cultureInfo,
            Dictionary<string, List<AiDocType>?> documentTypesCache,
            CancellationToken cancellationToken)
        {
            documentType = documentType.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(documentType))
            {
                return null;
            }

            if (documentTypesCache.TryGetValue(documentType, out var documentTypeCached))
            {
                return documentTypeCached;
            }

            var types = await this.krTypesCache.GetTypesAsync(cancellationToken);
            var foundTypes = 
                types.Select(type => 
                    new AiDocType(type.ID, Localize(type.Caption, cultureInfo)))
                .Where(type => type.Caption.Contains(documentType, StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (foundTypes.Count == 0)
            {
                documentTypesCache[documentType] = null;
                return null;
            }

            documentTypesCache[documentType] = foundTypes;
            return foundTypes;
        }

        /// <summary>
        /// Добавить выходное сообщение о предлагаемом варианте замещений.
        /// </summary>
        /// <param name="substitutions">Список замещений.</param>
        /// <param name="outMessageBuilder">Билдер выходного сообщения.</param>
        /// <returns>Построенное выходное сообщение в формате markdown.</returns>
        private async Task AddConfirmMessageToBuilderAsync(List<SubstitutionInfo> substitutions, StringBuilder outMessageBuilder)
        {
            var empCaption = await LocalizeAsync("$Ai_AddDeputyPlugin_OutMessage_BlockCaption");
            var deputyCaption = await LocalizeAsync("$Ai_AddDeputyPlugin_OutMessage_DeputyCaption");
            var substitutionPeriodCaption = await LocalizeAsync("$Ai_AddDeputyPlugin_OutMessage_SubstitutionPeriodCaption");

            var docTypeCaption = await LocalizeAsync("$Ai_AddDeputyPlugin_OutMessage_DocTypeCaption");
            var permanentCaption = await LocalizeAsync("$Ai_Message_PermanentCaption");

            foreach (var substitution in substitutions)
            {
                outMessageBuilder
                    .Append(empCaption).Append(' ').AppendLine(this.GetEmployeeMdLink(substitution.EmployeeInfo!))
                    .Append(deputyCaption).Append(' ').AppendLine(this.GetEmployeeMdLink(substitution.DeputyUserInfo!));
                if (substitution.DocumentTypesInfos is { Count: > 0 })
                {
                    outMessageBuilder.Append(docTypeCaption).Append(' ').Append(await LocalizeAsync(substitution.DocumentTypesInfos[0].Caption));
                    for (var i = 1; i < substitution.DocumentTypesInfos.Count; i++)
                    {
                        outMessageBuilder.Append(", ").Append(await LocalizeAsync(substitution.DocumentTypesInfos[i].Caption));
                    }

                    outMessageBuilder.AppendLine();
                }

                outMessageBuilder.Append(substitutionPeriodCaption).Append(' ');
                if (substitution.IsPermanent)
                {
                    outMessageBuilder.Append(permanentCaption);
                }
                else
                {
                    outMessageBuilder.Append(FormatDate(substitution.Start)).Append(" - ").Append(FormatDate(substitution.End)).AppendLine();
                }

                outMessageBuilder.AppendLine();
            }
        }

        /// <summary>
        /// Получить markdown-ссылку на карточку сотрудника.
        /// </summary>
        /// <param name="employee"><inheritdoc cref="AiEmployeeInfo" path="/summary"/></param>
        /// <returns>Markdown-ссылка на карточку пользователя.</returns>
        private string GetEmployeeMdLink(AiEmployeeInfo employee) =>
            this.mdLinkProvider.GetCardLink(employee.ID, employee.DisplayName);

        /// <summary>
        /// Произвести базовую валидацию замещений.
        /// </summary>
        /// <param name="substitutions">Список замещений.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <returns> <c>true</c> - если валидация успешна, в противном случае <c>false</c>. Если валидация не успешна - ошибки будут записаны в validationResult.</returns>
        private static bool ValidateSubstitutions(List<SubstitutionInfo> substitutions, IValidationResultBuilder validationResult)
        {
            foreach (var substitution in substitutions)
            {
                if (substitution.EmployeeInfo is null)
                {
                    validationResult.AddError("$Ai_AddDeputyPlugin_Validation_NoEmployee");
                }

                if (substitution.DeputyUserInfo is null)
                {
                    validationResult.AddError("$Ai_AddDeputyPlugin_Validation_NoDeputy");
                }

                if (!substitution.IsPermanent)
                {
                    if (substitution.End is null)
                    {
                        validationResult.AddError("$Ai_AddDeputyPlugin_Validation_NoEndDate");
                    }
                    else if (substitution.End <= (substitution.Start ?? DateTime.Now))
                    {
                        validationResult.AddError("$Ai_AddDeputyPlugin_Validation_InvalidEndDate");
                    }
                }
            }

            return validationResult.IsSuccessful();
        }

        /// <summary>
        /// Добавить замещения в карточку сотрудника.
        /// </summary>
        /// <param name="employeeId">Идентификатор замещаемого сотрудника.</param>
        /// <param name="offset">Смещение времени временной зоны пользователя относительно UTC.</param>
        /// <param name="substitutions">Список замещений.</param>
        /// <param name="validationResult"><inheritdoc cref="IValidationResultBuilder" path="/summary"/></param>
        /// <param name="oldDeputySystem">Признак, что используется старая система замещений.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        private async Task AddDeputiesAsync(
            Guid employeeId,
            TimeSpan offset,
            IEnumerable<SubstitutionInfo> substitutions,
            IValidationResultBuilder validationResult,
            bool oldDeputySystem,
            CancellationToken cancellationToken)
        {
            var employeeGetCardResponse = await this.cardRepository.GetAsync(
                new CardGetRequest
                {
                    CardID = employeeId,
                    CardTypeID = RoleHelper.PersonalRoleTypeID,
                    CardTypeName = RoleHelper.PersonalRoleTypeName,
                    RestrictionFlags = CardGetRestrictionFlags.RestrictTaskHistory
                },
                cancellationToken
            );

            validationResult.Add(employeeGetCardResponse.ValidationResult);
            if (!validationResult.IsSuccessful())
            {
                return;
            }

            var employeeCard = employeeGetCardResponse.Card;
            ThrowIfNull(employeeCard);
            ThrowIfNull(employeeCard.Sections);

            var deputiesSection = employeeCard.Sections.GetOrAdd(RoleStrings.RoleDeputiesManagementVirtual);
            var deputiesNestedSection = employeeCard.Sections.GetOrAdd(RoleStrings.RoleDeputiesNestedManagementVirtual);

            var deputiesUsersSection = employeeCard.Sections.GetOrAdd(RoleStrings.RoleDeputiesManagementUsersVirtual);
            var deputiesUsersNestedSection = employeeCard.Sections.GetOrAdd(RoleStrings.RoleDeputiesNestedManagementUsersVirtual);

            var deputiesNestedTypesSection = employeeCard.Sections.GetOrAdd(RoleStrings.RoleDeputiesNestedManagementTypesVirtual);

            var roleDeputiesManagementRolesVirtual = oldDeputySystem
                ? employeeCard.Sections.GetOrAdd(RoleStrings.RoleDeputiesManagementRolesVirtual)
                : null;

            foreach (var substitution in substitutions)
            {
                var withDocType = substitution.DocumentTypesInfos is { Count: > 0 };

                var actualDeputiesSection = withDocType ? deputiesNestedSection : deputiesSection;
                var row = actualDeputiesSection.Rows.Add();
                row.RowID = Guid.NewGuid();
                row.State = CardRowState.Inserted;
                if (!withDocType)
                {
                    row.Fields["IsActive"] = false;
                }

                row.Fields["IsEnabled"] = true;
                row.Fields["IsPermanent"] = substitution.IsPermanent;

                // Данные колонки типа date (то есть абстрагируемся от часового пояса), а значит время просто будет отсечено.
                // Мы же оперируем с данными датами, как с UTC. Например, 12 сентября 0:00 по МСК в UTC виде будет 11 сентября 21:00 и в БД будет сохранено 11-09-2025
                // Поэтому перед сохранением дни нужно в явном виде привести в соответствии с видом по настройкам пользователя.
                row.Fields["MinDate"] = substitution.Start.HasValue ? substitution.Start.Value.ToUniversalTime() + offset : null;
                row.Fields["MaxDate"] = substitution.End.HasValue ? substitution.End.Value.ToUniversalTime() + offset : null;

                var actualDeputiesUsersSection = withDocType ? deputiesUsersNestedSection : deputiesUsersSection;

                var userRow = actualDeputiesUsersSection.Rows.Add();
                userRow.RowID = Guid.NewGuid();
                userRow.State = CardRowState.Inserted;
                userRow.ParentRowID = row.RowID;
                userRow.Fields["PersonalRoleID"] = substitution.DeputyUserInfo!.ID;
                userRow.Fields["PersonalRoleName"] = substitution.DeputyUserInfo!.DisplayName;

                // Если используется старая система замещений, то проставляем роль "Все роли и подразделения"
                if (!withDocType && oldDeputySystem)
                {
                    var roleRow = roleDeputiesManagementRolesVirtual!.Rows.Add();
                    roleRow.RowID = Guid.NewGuid();
                    roleRow.State = CardRowState.Inserted;
                    roleRow.ParentRowID = row.RowID;
                    roleRow.Fields["RoleID"] = RoleDeputiesManagementHelper.FakeAllRoles;
                    roleRow.Fields["RoleName"] = AllRolesAndDepartmentCaption;
                }

                if (!withDocType)
                {
                    continue;
                }

                foreach (var documentType in substitution.DocumentTypesInfos!)
                {
                    var docTypeRow = deputiesNestedTypesSection.Rows.Add();
                    docTypeRow.RowID = Guid.NewGuid();
                    docTypeRow.State = CardRowState.Inserted;
                    docTypeRow.ParentRowID = row.RowID;
                    docTypeRow.Fields["TypeID"] = documentType.ID;
                    docTypeRow.Fields["TypeCaption"] = documentType.Caption;
                }
            }

            employeeCard.RemoveAllButChanged();
            var cardStoreResponse = await this.cardRepository.StoreAsync(new CardStoreRequest { Card = employeeCard }, cancellationToken);
            validationResult.Add(cardStoreResponse.ValidationResult);
        }

        private static List<SubstitutionInfo>? TryDeserializeSubstitutions(List<object> objList)
        {
            try
            {
                var result = new List<SubstitutionInfo>(objList.Count);
                foreach (var obj in objList)
                {
                    if (obj is not Dictionary<string, object?> storage)
                    {
                        return null;
                    }

                    var substitution = new SubstitutionInfo();
                    substitution.Deserialize(storage);
                    result.Add(substitution);
                }

                return result;
            }
            catch (Exception ex)
            {
                AiHelper.Logger.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// Сформировать сообщение об успешно выполненных замещениях.
        /// </summary>
        /// <param name="substitutions">Замещения.</param>
        /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
        /// <returns>Сообщение о выполненных замещениях в markdown-формате.</returns>
        private async Task<string> BuildSuccessMessageAsync(List<SubstitutionInfo> substitutions, CancellationToken cancellationToken)
        {
            var sb = StringBuilderHelper.Acquire().AppendLine(await LocalizeAsync("$Ai_AddDeputyPlugin_SuccessMessage"));
            var hasActualSubstitutions = false;

            foreach (var substitution in substitutions)
            {
                // Формат markdown-ссылки [текст_ссылки](режим_открытия:card/id_карточки)
                // Префикс можно опустить, поскольку используем дефолтный режим открытия (т.е. во вкладке).
                sb.Append("- ")
                    .Append(this.GetEmployeeMdLink(substitution.EmployeeInfo!));

                if (substitution.IsPermanent || substitution.Start < DateTime.UtcNow)
                {
                    hasActualSubstitutions = true;
                }

                sb.AppendLine();
            }

            if (hasActualSubstitutions)
            {
                sb.Append("\n\n").Append(await LocalizeAsync("$Ai_AddDeputyPlugin_OutMessage_Remark"));
            }

            return sb.ToStringAndRelease();
        }

        #endregion
    }
}
